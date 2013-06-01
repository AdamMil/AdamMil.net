/*
AdamMil.Collections is a library that provides useful collection classes for
the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2013 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AdamMil.Utilities;

namespace AdamMil.Collections
{

#region CacheEntry
sealed class CacheEntry<T>
{
  public CacheEntry(T value, uint currentTime)
  {
    Value = value;
    CreationTime = LastAccessTime = currentTime;
  }

  public readonly T Value;
  public readonly uint CreationTime;
  public uint LastAccessTime;
}
#endregion

#region DictionaryCache
/// <summary>Implements an optionally thread-safe cache of key/value pairs. The cache supports three methods of lifetime management: fixed
/// expirations, which cause entries to expire a fixed amount of time after they are added; floating expirations, which cause entries to
/// expire an amount of time since they were last accessed; and an item limit, which limits the total number of items in the cache. Any
/// combination of these methods may be used, but the default is to use floating expirations only.
/// </summary>
/// <remarks>By default, the cache has a five-minute <see cref="CheckTime">garbage collection time</see>, a five-minute
/// <see cref="FloatingTimeLimit"/>, no absolute <see cref="TimeLimit"/>, and no <see cref="MaximumItems">maximum item limit</see>. The
/// cache is also <see cref="ThreadSafe"/> by default.
/// </remarks>
public sealed class DictionaryCache<TKey, TValue>
{
  /// <summary>Constructs a new <see cref="DictionaryCache{TKey,TValue}"/> with default settings.</summary>
  public DictionaryCache() : this(null) { }

  /// <summary>Constructs a new <see cref="DictionaryCache{TKey,TValue}"/> with default settings, but using the given
  /// <see cref="IEqualityComparer{K}"/> to compare keys.
  /// </summary>
  public DictionaryCache(IEqualityComparer<TKey> comparer)
  {
    dict = new Dictionary<TKey, CacheEntry<TValue>>(comparer);
    if(SafeNativeMethods.IsWindowsVistaOrLater)
    {
      startTicks64 = SafeNativeMethods.GetTickCount64();
    }
    else
    {
      startTicks64 = Environment.TickCount;
      stopwatch    = Stopwatch.StartNew();
    }

    CheckTime         = 5 * 60;
    FloatingTimeLimit = 5 * 60;
    ThreadSafe        = true;
  }

  /// <summary>Sets the value of the given key within the cache.</summary>
  public TValue this[TKey key]
  {
    set { Set(key, value); }
  }

  /// <summary>Gets or sets the minimum time, in seconds, between garbage collections that remove expired or excess items from the cache.
  /// The default is 300, which represents a period of five minutes. Setting this to a lower value causes expired and excess items to be
  /// removed more often, reducing the memory used by the cache but increasing the amount of time spent maintaining it. Setting it to a
  /// higher value does the opposite.
  /// </summary>
  public int CheckTime
  {
    get { return (int)_checkTime; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      bool reduced = value < CheckTime;
      _checkTime = (uint)value;
      if(reduced) TryCheckCacheExpiration(GetCurrentTime(), true);
    }
  }

  /// <summary>Gets the number of items in the cache. Note that it is possible for this to exceed <see cref="MaximumItems"/> because
  /// <see cref="MaximumItems"/> does not establish a hard limit.
  /// </summary>
  public int Count
  {
    get
    {
      try
      {
        Lock(); // we have to lock because internally dict.Count does more than just read a field
        return dict.Count;
      }
      finally
      {
        Unlock();
      }
    }
  }

  /// <summary>Gets or sets the time from the last successful access, in seconds, after which a key/value pair will expire. If set to 0,
  /// there will be no floating time limit. The default is 300, which represents a period of five minutes.
  /// </summary>
  public int FloatingTimeLimit
  {
    get { return (int)_floatingLimit; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      bool reduced = value != 0 && value < FloatingTimeLimit;
      _floatingLimit = (uint)value;
      if(reduced) CheckCacheExpiration(GetCurrentTime(), true);
    }
  }

  /// <summary>Gets or sets the maximum number of cached objects allowed in the dictionary. If set to 0, there will be no maximum item
  /// limit. The default is 0. This is not a hard limit, so the number of items may temporarily exceed the limit by a modest amount.
  /// </summary>
  public int MaximumItems
  {
    get { return _maxItems; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      bool reduced = value != 0 && value < _maxItems;
      _maxItems = value;
      if(reduced) TrimExcessItems(true);
    }
  }

  /// <summary>Gets or sets whether the dictionary will be locked during access for thread safety. The default is true. You should not
  /// change this while the cache is in use.
  /// </summary>
  public bool ThreadSafe { get; set; }

  /// <summary>Gets or sets the time from when a key/value pair was added, in seconds, after which it will expire. If set to 0, there will
  /// be no fixed time limit. The default is 0.
  /// </summary>
  public int TimeLimit
  {
    get { return (int)_absoluteLimit; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      bool reduced = value != 0 && value < _absoluteLimit;
      _absoluteLimit = (uint)value;
      if(reduced) CheckCacheExpiration(GetCurrentTime(), true);
    }
  }

  /// <summary>Removes all items from the cache.</summary>
  public void Clear()
  {
    try
    {
      Lock();
      dict.Clear();
    }
    finally
    {
      Unlock();
    }
  }

  /// <summary>Checks whether the cache contains the given key.</summary>
  public bool ContainsKey(TKey key)
  {
    uint currentTime = GetCurrentTime();
    try
    {
      Lock();
      CacheEntry<TValue> entry;
      if(dict.TryGetValue(key, out entry))
      {
        bool expired = IsExpired(entry, currentTime);
        if(expired) dict.Remove(key);
        return !expired;
      }
      return false;
    }
    finally
    {
      Unlock();
    }
  }

  /// <summary>Retrieves all unexpired items from the cache without resetting their expiration times.</summary>
  public Dictionary<TKey, TValue> GetAllEntries()
  {
    uint currentTime = GetCurrentTime();
    try
    {
      Lock();
      Dictionary<TKey, TValue> entries = new Dictionary<TKey, TValue>(dict.Count);
      List<TKey> deadKeys = null;
      foreach(KeyValuePair<TKey, CacheEntry<TValue>> pair in dict)
      {
        if(!IsExpired(pair.Value, currentTime))
        {
          entries.Add(pair.Key, pair.Value.Value);
        }
        else
        {
          if(deadKeys == null) deadKeys = new List<TKey>();
          deadKeys.Add(pair.Key);
        }
      }
      if(deadKeys != null) dict.RemoveRange(deadKeys);
      return entries;
    }
    finally
    {
      Unlock();
    }
  }

  /// <summary>Removes the value with the given key from the cache.</summary>
  public bool Remove(TKey key)
  {
    try
    {
      Lock();
      return dict.Remove(key);
    }
    finally
    {
      Unlock();
    }
  }

  /// <summary>Immediately removes all expired items from the cache.</summary>
  public void RemoveExpiredItems()
  {
    CheckCacheExpiration(GetCurrentTime(), true);
  }

  /// <summary>Sets the value of the given key within the cache. If an entry with the given key already exists, its expiration time will
  /// be reset.
  /// </summary>
  public void Set(TKey key, TValue value)
  {
    uint currentTime = GetCurrentTime();
    CacheEntry<TValue> entry = new CacheEntry<TValue>(value, currentTime);
    try
    {
      Lock();
      TryCheckCacheExpiration(currentTime, false);
      dict[key] = entry;
      TrimExcessItems(false);
    }
    finally
    {
      Unlock();
    }
  }

  /// <summary>Attempts to retrieve the value with the given key from the cache.</summary>
  public bool TryGetValue(TKey key, out TValue value)
  {
    uint currentTime = GetCurrentTime();
    try
    {
      Lock();
      CacheEntry<TValue> entry;
      if(dict.TryGetValue(key, out entry))
      {
        if(CheckReadAccess(entry, currentTime))
        {
          value = entry.Value;
          return true;
        }
        dict.Remove(key);
      }

      TryCheckCacheExpiration(currentTime, false);
      value = default(TValue);
      return false;
    }
    finally
    {
      Unlock();
    }
  }

  void CheckCacheExpiration(uint currentTime, bool shouldLock)
  {
    try
    {
      if(shouldLock) Lock();

      lastExpirationCheck = currentTime;
      List<TKey> deadKeys = null;
      foreach(KeyValuePair<TKey, CacheEntry<TValue>> pair in dict)
      {
        if(IsExpired(pair.Value, currentTime))
        {
          if(deadKeys == null) deadKeys = new List<TKey>();
          deadKeys.Add(pair.Key);
        }
      }

      if(deadKeys != null)
      {
        if(deadKeys.Count == dict.Count) dict.Clear();
        else dict.RemoveRange(deadKeys);
      }
    }
    finally
    {
      if(shouldLock) Unlock();
    }
  }

  bool CheckReadAccess(CacheEntry<TValue> entry, uint currentTime)
  {
    if(IsExpired(entry, currentTime)) return false;
    entry.LastAccessTime = currentTime;
    return true;
  }

  uint GetCurrentTime()
  {
    // this method uses a 32-bit value for the time, but it won't overflow for 136 years, which should be good enough
    if(SafeNativeMethods.IsWindowsVistaOrLater)
    {
      return (uint)((SafeNativeMethods.GetTickCount64() - startTicks64) / 1000);
    }
    else
    {
      // the Stopwatch class is really slow, so we want to avoid using it. instead, we'll only query the stopwatch once every 30 seconds or
      // so. otherwise, we'll use Environment.TickCount. Environment.TickCount will overflow after about 49.7 days, so it's possible that
      // we won't detect the overflow, but there's only a 0.0007% chance of that, and the consequences -- some objects disappearing too
      // soon or hanging around for up to 30 seconds longer than they should -- aren't too severe
      long time = Interlocked.Read(ref startTicks64);
      int elapsedMs = Environment.TickCount - (int)(uint)time;
      if((uint)elapsedMs < 30000)
      {
        return (uint)(time>>32) + (uint)elapsedMs/1000;
      }
      else
      {
        uint currentTime = (uint)(stopwatch.ElapsedTicks / Stopwatch.Frequency);
        time = ((long)currentTime << 32) | (uint)Environment.TickCount;
        Interlocked.Exchange(ref startTicks64, time);
        return currentTime;
      }
    }
  }

  bool IsExpired(CacheEntry<TValue> entry, uint currentTime)
  {
    return TimeLimit != 0 && currentTime - entry.CreationTime >= TimeLimit ||
           FloatingTimeLimit != 0 && currentTime - entry.LastAccessTime > FloatingTimeLimit;
  }

  /// <summary>Locks the current <see cref="DictionaryCache{TKey,TValue}"/> if <see cref="ThreadSafe"/> is true.</summary>
  void Lock()
  {
    if(ThreadSafe)
    {
      Monitor.Enter(this);
      locked = true;
    }
  }

  void Unlock()
  {
    if(locked)
    {
      locked = false;
      Monitor.Exit(this);
    }
  }

  void TrimExcessItems(bool shouldLock)
  {
    if(MaximumItems != 0)
    {
      int threshold = MaximumItems/8;
      if(dict.Count - MaximumItems > threshold) // wait until we've exceeded the threshold by 1/8th
      {
        try
        {
          if(shouldLock) Lock();
          int overrun = dict.Count - MaximumItems; // check this again because the previous check was outside the lock
          if(overrun > threshold)
          {
            overrun = Math.Min(dict.Count, overrun + threshold); // drop down to 7/8ths of the threshold
            uint currentTime = GetCurrentTime(); // remove the least recently accessed items
            dict.RemoveRange(dict.TakeGreatest(overrun, pair => currentTime - pair.Value.LastAccessTime)
                                 .Select(pair => pair.Key).ToList());
          }
        }
        finally
        {
          if(shouldLock) Unlock();
        }
      }
    }
  }

  void TryCheckCacheExpiration(uint currentTime, bool shouldLock)
  {
    if(currentTime-lastExpirationCheck >= CheckTime && (FloatingTimeLimit != 0 || TimeLimit != 0))
    {
      CheckCacheExpiration(currentTime, shouldLock);
    }
  }

  readonly Dictionary<TKey, CacheEntry<TValue>> dict;
  readonly Stopwatch stopwatch;

  long startTicks64;
  uint _absoluteLimit, _floatingLimit, _checkTime, lastExpirationCheck;
  int _maxItems;
  bool locked;
}
#endregion

} // namespace AdamMil.Collections
