/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2013 Adam Milazzo

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
using System.Security.Permissions;
using System.Threading;

// WARNING: the lock classes in this file are exquisitely and painstakingly tuned. changes that seem completely trivial can have a large
// effect on the performance of the locks, so be sure to retest carefully with many different workloads and threading configurations if you
// change anything. or better yet, don't change anything. :-)

namespace AdamMil.Utilities
{

  #region CrossThreadExclusiveLock
  /// <summary>This class implements a fast, exclusive, non-recursive lock that can be used across threads, such that one thread can acquire
  /// the lock and another thread can release it. If you don't need cross-thread locking, it's better to use the <see cref="Monitor"/> class
  /// (i.e. the <c>lock</c> keyword in C#).
  /// </summary>
  /// <remarks>
  /// The <see cref="CrossThreadExclusiveLock"/> is almost the same speed as the <see cref="Monitor"/> class -- slightly slower when the lock
  /// is not under contention and slightly faster when the lock is under contention.
  /// <para>Because this lock is designed for cross-thread operation, it cannot use <see cref="Thread.BeginCriticalRegion"/> to inform the
  /// .NET runtime that the thread is performing an operation that could affect the stability of the application domain -- acquiring a lock
  /// on a shared resource. As a result, the application may become unstable in exceptional situations (such as when a thread is aborted
  /// while holding the lock). If this is unacceptable, you must use the <see cref="Thread.BeginCriticalRegion"/> and
  /// <see cref="Thread.EndCriticalRegion"/> methods yourself at the appropriate times.
  /// </para>
  /// </remarks>
  [HostProtection(SecurityAction.LinkDemand, Synchronization=true, ExternalThreading=true)]
  public sealed class CrossThreadExclusiveLock
  {
    #region LockReleaser
    /// <summary>An <see cref="IDisposable"/> object that calls <see cref="Exit"/> the first time <see cref="Dispose"/> is called.</summary>
    public struct LockReleaser : IDisposable
    {
      internal LockReleaser(CrossThreadExclusiveLock owner)
      {
        this.owner = owner;
      }

      /// <inheritdoc/>
      public void Dispose()
      {
        if(owner != null)
        {
          owner.Exit();
          owner = null;
        }
      }

      CrossThreadExclusiveLock owner;
    }
    #endregion

    /// <summary>Acquires the lock, waiting until it becomes available.</summary>
    /// <returns>Returns an <see cref="IDisposable"/> object that calls <see cref="Exit"/> the first time its
    /// <see cref="IDisposable.Dispose"/> method is called.
    /// </returns>
    public LockReleaser Enter()
    {
      // we don't use Thread.BeginCriticalRegion() because the lock is designed to be used across threads, so we can't guarantee that
      // Thread.EndCriticalRegion() will be called on the same thread
      int spinCount = 0;
      while(true)
      {
        int state = this.state;
        if(Interlocked.CompareExchange(ref this.state, state | OwnedFlag, state) == state && (state & OwnedFlag) == 0) break;
        ThreadUtility.Spin1(spinCount++);
        if((state & OwnedFlag) != 0 && Interlocked.CompareExchange(ref this.state, state + OneWaiting, state) == state)
        {
          spinCount = 0; // presumably a relatively long period will pass, so reset our spin count
          waitLock.Wait(); // then block until we're released
        }
      }
      return new LockReleaser(this);
    }

    /// <summary>Attempts to acquire the lock, waiting for up to the given time period to do so. Returns true if the lock was acquired and
    /// false if not.
    /// </summary>
    /// <param name="timeoutMs">The number of milliseconds to wait for the lock to be acquired. If equal to <see cref="Timeout.Infinite"/>,
    /// it is equivalent to calling <see cref="Enter"/>. If equal to 0, the method will return immediately if the lock cannot be acquired.
    /// Otherwise, the method will wait for up to the given number of milliseconds.
    /// </param>
    public bool TryEnter(int timeoutMs)
    {
      // we don't use Thread.BeginCriticalRegion() because the lock is designed to be used across threads, so we can't guarantee that
      // Thread.EndCriticalRegion() will be called on the same thread
      if(timeoutMs == Timeout.Infinite)
      {
        Enter();
        return true;
      }
      else
      {
        TimeoutTimer timer = new TimeoutTimer(timeoutMs);
        int spinCount = 0;
        do
        {
          int state = this.state;
          if(Interlocked.CompareExchange(ref this.state, state | OwnedFlag, state) == state && (state & OwnedFlag) == 0) return true;
          if(timeoutMs == 0) break;
          ThreadUtility.Spin1(spinCount++);
          if((state & OwnedFlag) != 0 && Interlocked.CompareExchange(ref this.state, state + OneWaiting, state) == state)
          {
            spinCount = 0; // presumably a relatively long period will pass, so reset our spin count
            if(!waitLock.Wait((int)timer.RemainingTime)) break;
          }
        } while(!timer.HasExpired);

        return false;
      }
    }

    /// <summary>Releases the lock.</summary>
    /// <exception cref="SynchronizationLockException">Thrown if the lock is not currently held.</exception>
    public void Exit()
    {
      if(this.state != 1 || Interlocked.CompareExchange(ref this.state, 0, 1) != 1)
      {
        if((this.state & OwnedFlag) == 0) throw new SynchronizationLockException(); // if the lock wasn't owned, throw an exception

        int state = this.state, freeState = state & ~OwnedFlag;
        while(Interlocked.CompareExchange(ref this.state, freeState == 0 ? freeState : freeState - OneWaiting, state) != state)
        {
          dummy++;
          state     = this.state;
          freeState = state & ~OwnedFlag;
          // release the lock by clearing the 'owned' bit and simultaneously subtracting one from the number waiting if it's nonzero
        }
        if(freeState != 0) waitLock.Release(); // if there was a thread waiting, release one of the waiters
      }
    }

    // for whatever reason, incrementing this variable at spin points improves lock performance in some cases. perhaps it has some
    // effect on the processor cache. so don't remove it.
    static int dummy;

    const int OwnedFlag = 1, OneWaiting = 2;

    int state; // bit 0 indicates whather the lock is currently being held. the other bits contain the number of threads waiting for the lock

    readonly FastSemaphore waitLock = new FastSemaphore();
  }
  #endregion

  #region UpgradableReadWriteLock
  /// <summary>This class implements a fast, non-recursive lock that grants concurrent access to readers and exclusive access to writers,
  /// while allowing a reader to upgrade itself to a writer without releasing the lock. This lock is superior to the built-in
  /// <see cref="ReaderWriterLock"/> and <see cref="ReaderWriterLockSlim"/> classes because they only allow a single thread to enter the
  /// lock in upgradable read mode. This lock allows any number of threads to enter the lock in upgradable read mode. Furthermore, this lock
  /// is faster in most circumstances.
  /// </summary>
  /// <remarks>By default, the thread that acquires the lock must be the same thread that releases the lock. This is not enforced by the
  /// lock, but failing to adhere to this can cause reliability problems within your application. The benefit of this restriction is that
  /// the application domain will be more robust against exceptional situations (such as when threads are aborted while holding the lock).
  /// If you desire cross-thread operation, you can use the <see cref="UpgradableReadWriteLock(bool)"/> constructor and pass true. As a
  /// result, the lock can be used across threads, but the application domain will be less robust against exceptional situations.
  /// <para>In general, you should implement locking with a <see cref="Monitor"/> (or <see cref="CrossThreadExclusiveLock"/> if you need
  /// cross-thread locking). Reader/writer locks such as this one are only beneficial when 1) multiple threads read data concurrently (i.e.
  /// multiple threads would be inside the lock at the same time), 2) writes are rare, and 3) reading the data takes a very substantial
  /// amount of time. In most scenarios, the amount of time spent within the lock is too small to make a reader/writer lock worthwhile, so be
  /// sure to test whether using a reader/writer lock improves performance versus a <see cref="Monitor"/> before deciding to use it.
  /// </para>
  /// </remarks>
  /// <example>
  /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/ReadExample/node()" />
  /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/WriteExample/node()" />
  /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/UpgradeExample/node()" />
  /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/DowngradeExample/node()" />
  /// </example>
  [HostProtection(SecurityAction.LinkDemand, Synchronization=true, ExternalThreading=true)]
  public sealed class UpgradableReadWriteLock
  {
    /// <summary>Initializes a new <see cref="UpgradableReadWriteLock"/> without support for cross-thread operation.</summary>
    /// <remarks>If this constructor is used, the thread that acquires the lock must be the same thread that releases the lock. This is not
    /// enforced by the lock, but failing to adhere to this can cause reliability problems within your application. The benefit of this
    /// restriction is that the application domain will be more robust against exceptional situations (such as when threads are aborted while
    /// holding the lock). If you desire support for cross-thread operation, use the <see cref="UpgradableReadWriteLock(bool)"/> constructor
    /// and pass true.
    /// </remarks>
    public UpgradableReadWriteLock() : this(false) { }

    /// <summary>Initializes a new <see cref="UpgradableReadWriteLock"/> with the option to support cross-thread operation.</summary>
    /// <param name="crossThread">If true, the thread that acquires the lock must be the same thread that releases the lock. This is not
    /// enforced by the lock, but failing to adhere to this can cause reliability problems within your application. The benefit of this
    /// restriction is that the application domain will be more robust against exceptional situations (such as when threads are aborted while
    /// holding the lock). If false, the lock can be used across threads and will be slightly faster, but the application domain will be less
    /// robust against exceptional situations.
    /// </param>
    public UpgradableReadWriteLock(bool crossThread)
    {
      this.crossThread = crossThread;
    }

    #region LockReleaser
    /// <summary>An <see cref="IDisposable"/> object that exits the lock the first time its <see cref="IDisposable.Dispose"/> method is
    /// called. The object provides <see cref="Upgrade"/> and <see cref="Downgrade"/> methods. If used to upgrade or downgrade the lock, the
    /// Dispose method will be able to exit the lock appropriately even if it has been upgraded or downgraded.
    /// </summary>
    public struct LockReleaser : IDisposable
    {
      internal LockReleaser(UpgradableReadWriteLock owner, bool writeMode)
      {
        this.owner = owner;
        this.writeMode = writeMode;
      }

      /// <summary>Gets whether the lock is held in write (exclusive) mode.</summary>
      public bool WriteMode
      {
        get { return writeMode; }
      }

      /// <summary>Exits the lock.</summary>
      public void Dispose()
      {
        if(owner != null)
        {
          if(writeMode) owner.ExitWrite();
          else owner.ExitRead();
          owner = null;
        }
      }

      /// <summary>Downgrades the lock from write (exclusive) to read (shared) mode and keeps track of the fact that the lock has been
      /// downgraded so it can be released properly.
      /// </summary>
      public void Downgrade()
      {
        AssertNotDisposed();
        if(!writeMode) throw new SynchronizationLockException();
        owner.Downgrade();
        writeMode = false;
      }

      /// <summary>Upgrades the lock from write (exclusive) to read (shared) mode and keeps track of the fact that the lock has been
      /// upgraded so it can be released properly.
      /// </summary>
      /// <returns>Returns the value from <see cref="UpgradableReadWriteLock.Upgrade"/>.</returns>
      public bool Upgrade()
      {
        AssertNotDisposed();
        if(writeMode) throw new SynchronizationLockException();
        bool firstUpgrader = owner.Upgrade();
        writeMode = true;
        return firstUpgrader;
      }

      void AssertNotDisposed()
      {
        if(owner == null) throw new ObjectDisposedException(GetType().FullName);
      }

      UpgradableReadWriteLock owner;
      bool writeMode;
    }
    #endregion

    /// <summary>Downgrades a lock held in write (exclusive) mode to read (shared) mode.</summary>
    /// <remarks>After calling this method, you must call <see cref="ExitRead"/> instead of <see cref="ExitWrite"/> to release the lock.</remarks>
    /// <example><include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/DowngradeExample/node()" /></example>
    public void Downgrade()
    {
      if(lockState >= 0) throw new SynchronizationLockException(); // (lockState & OwnedByWriter) == 0
      int state;
      do state = lockState;
      while(Interlocked.CompareExchange(ref lockState, state + unchecked(OneReader-OwnedByWriter), state) != state);
      if(!crossThread) Thread.EndCriticalRegion();
    }

    /// <summary>Acquires the lock in upgradable read (shared) mode. Multiple threads can acquire the lock in this mode. The method will
    /// block if any thread has acquired the lock for writing.
    /// </summary>
    /// <returns>Returns an <see cref="IDisposable"/> object that exits the lock the first time its <see cref="IDisposable.Dispose"/>
    /// method is called. The object exposes <see cref="LockReleaser.Upgrade"/> and <see cref="LockReleaser.Downgrade"/> methods that
    /// simplify the use of upgradable locks by keeping track of whether the lock has been upgraded or downgraded and using the appropriate
    /// exit method when Dispose is called. If you upgrade or downgrade the lock without using the methods on the <see cref="LockReleaser"/>,
    /// then you should also exit the lock yourself rather than using the <see cref="LockReleaser"/>'s Dispose method.
    /// </returns>
    /// <remarks>To upgrade the lock to write (exclusive) mode, call <see cref="Upgrade"/>. After doing so, you must call
    /// <see cref="ExitWrite"/> instead of <see cref="ExitRead"/> (assuming you don't downgrade it back to read mode). If you do not upgrade
    /// the lock to write mode, you must call <see cref="ExitRead"/> to release the lock. If you use the methods on the returned
    /// <see cref="LockReleaser"/>, it will keep track of this for you.
    /// </remarks>
    /// <example>
    /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/ReadExample/node()" />
    /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/UpgradeExample/node()" />
    /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/DowngradeExample/node()" />
    /// </example>
    public LockReleaser EnterRead()
    {
      int spinCount = 0;
      while(true)
      {
        int state = lockState;
        if((uint)state < (uint)ReaderMask) // if it's free or owned by a reader, and we're not currently at the reader limit...
        {
          if(Interlocked.CompareExchange(ref lockState, state + OneReader, state) == state) return new LockReleaser(this, false);
          ThreadUtility.VeryBriefWait();
        }
        else if(spinCount < 15)
        {
          ThreadUtility.Spin2(spinCount++);
        }
        else if((state & ReaderWaitingMask) == ReaderWaitingMask) // try to block the thread. if there aren't any wait slots left...
        {
          Thread.Sleep(10); // sleep a little bit while waiting for a slot
        }
        else if(Interlocked.CompareExchange(ref lockState, state + OneReaderWaiting, state) == state) // if we could get a wait slot...
        {
          spinCount = 0; // presumably a relatively long period will pass, so reset our spin count
          readWait.Wait(); // wait until we're awoken
        }
      }
    }

    /// <summary>Acquires the lock in write (exclusive) mode. The method will block if any other thread has acquired the lock in any mode.</summary>
    /// <returns>Returns an <see cref="IDisposable"/> object that exits the lock the first time its <see cref="IDisposable.Dispose"/>
    /// method is called. The object exposes <see cref="LockReleaser.Upgrade"/> and <see cref="LockReleaser.Downgrade"/> methods that
    /// simplify the use of upgradable locks by keeping track of whether the lock has been upgraded or downgraded and using the appropriate
    /// exit method when Dispose is called. If you upgrade or downgrade the lock without using the methods on the <see cref="LockReleaser"/>,
    /// then you should also exit the lock yourself rather than using the <see cref="LockReleaser"/>'s Dispose method.
    /// </returns>
    /// <remarks>You must call <see cref="ExitWrite"/> to release the lock.</remarks>
    /// <example><include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/WriteExample/node()" /></example>
    public LockReleaser EnterWrite()
    {
      if(!crossThread) Thread.BeginCriticalRegion();
      EnterWriteCore();
      return new LockReleaser(this, true);
    }

    /// <summary>Releases a lock that was held in read (shared) mode. Do not call this method if you have previously upgraded the lock to
    /// write (exclusive) mode.
    /// </summary>
    /// <remarks>If you upgrade a read lock to write (exclusive) mode, you must call <see cref="ExitWrite"/> instead of
    /// <see cref="ExitRead"/> to release the lock.
    /// </remarks>
    public void ExitRead()
    {
      if((lockState & ReaderMask) == 0) throw new SynchronizationLockException();

      // if there are other readers, just subtract one reader. otherwise, if we're the only reader and there are no writers waiting, free
      // the lock. otherwise, we'll wake up one of the waiting writers, so subtract one and reserve it for the writer. we must be careful
      // to preserve the ReservedForWriter flag since it can be set by a writer before it decides to wait
      int state;
      do state = lockState;
      while(Interlocked.CompareExchange(ref lockState, (state & ReaderMask) != OneReader ? state - OneReader :
                                                       (state & WriterWaitingMask) == 0  ? state & ReservedForWriter :
                                                       (state | ReservedForWriter) - (OneReader+OneWriterWaiting), state) != state);
      if((state & ReaderMask) == OneReader) ReleaseWaitingThreads(state); // if we just removed the last reader, release waiting threads
    }

    /// <summary>Releases a lock that was held in write (exclusive) mode, either due to calling <see cref="EnterWrite"/> or due to calling
    /// <see cref="Upgrade"/>.
    /// </summary>
    public void ExitWrite()
    {
      if(lockState >= 0) throw new SynchronizationLockException(); // (lockState & OwnedByWriter) == 0

      // if no writers are waiting, mark the lock as free. otherwise, subtract one waiter and reserve the lock for it
      int state;
      do state = lockState;
      while(Interlocked.CompareExchange(ref lockState,
        (state & WriterWaitingMask) == 0 ? 0 : state + unchecked(ReservedForWriter-OwnedByWriter-OneWriterWaiting), state) != state);

      ReleaseWaitingThreads(state);
      if(!crossThread) Thread.EndCriticalRegion();
    }

    /// <summary>Upgrades a lock held in read (shared) mode to write (exclusive) mode. Returns true if the lock was immediately upgraded and
    /// false if another thread may have taken the lock in write mode first.
    /// </summary>
    /// <remarks>After calling this method, you must call <see cref="ExitWrite"/> instead of <see cref="ExitRead"/> to release the lock
    /// (assuming you don't downgrade it back to read mode).
    /// </remarks>
    /// <returns>Returns true if the lock was upgraded directly from read mode to write mode. In that case, any protected data read while the
    /// lock was held in read mode is still valid. If false is returned, the lock could not be upgraded directly, and it's possible that
    /// another thread upgraded to write mode first. In that case, you may wish to reread any protected data to check if it's still
    /// valid, or to check if you still need to perform the write operation.
    /// </returns>
    /// <example>
    /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/UpgradeExample/node()" />
    /// <include file="documentation.xml" path="/Utilities/UpgradableReadWriteLock/DowngradeExample/node()" />
    /// </example>
    public bool Upgrade()
    {
      if((lockState & ReaderMask) == 0) throw new SynchronizationLockException();
      if(!crossThread) Thread.BeginCriticalRegion();

      bool reserved = false;
      while(true)
      {
        int state = lockState;
        if((state & ReaderMask) == OneReader) // if we're the only reader...
        {
          // try to convert the lock to be owned by us in write mode
          if(Interlocked.CompareExchange(ref lockState, state & ~(ReservedForWriter|ReaderMask) | OwnedByWriter, state) == state)
          {
            return true; // if we succeeded, then we're done and we were the first upgrader to do so
          }
        }
        else if(reserved)
        {
          ThreadUtility.BriefWait();
        }
        else if((state & ReservedForWriter) == 0)
        {
          reserved = (Interlocked.CompareExchange(ref lockState, state | ReservedForWriter, state) == state);
        }
        // there are other readers and the lock is already reserved for another upgrader, so convert ourself from
        // a reader to a waiting writer. (otherwise, two readers trying to upgrade would deadlock.)
        else if((state & WriterWaitingMask) == WriterWaitingMask) // if there aren't any slots left for waiting writers...
        {
          Thread.Sleep(10); // massive contention. sleep while we await one
        }
        else if(Interlocked.CompareExchange(ref lockState, state + (OneWriterWaiting-OneReader), state) == state)
        {
          writeWait.Wait(); // wait until we're awoken
          EnterWriteCore(); // do the normal loop to enter write mode
          return false; // return false because we weren't the first reader to upgrade the lock
        }
      }
    }

    const int OwnedByWriter = unchecked((int)0x80000000), ReservedForWriter = 0x40000000;
    const int WriterWaitingMask = 0x3FC00000, ReaderMask = 0x3FF800, ReaderWaitingMask = 0x7FF;
    const int OneWriterWaiting = 1<<22, OneReader = 1<<11, OneReaderWaiting = 1;

    void EnterWriteCore()
    {
      int spinCount = 0;
      while(true)
      {
        int state = lockState, ownership = state & (OwnedByWriter|ReservedForWriter|ReaderMask);
        if(ownership == 0 || ownership == ReservedForWriter) // if the lock is free or reserved for a writer...
        {
          if(Interlocked.CompareExchange(ref lockState, state & ~ReservedForWriter | OwnedByWriter, state) == state) return; // take it
        }
        else if((state & (OwnedByWriter|ReservedForWriter)) == 0) // if it's not owned by or reserved for a writer...
        {
          Interlocked.CompareExchange(ref lockState, state | ReservedForWriter, state); // try to reserve it
        }
        else if(spinCount < 15)
        {
          ThreadUtility.Spin2(spinCount++);
        }
        else if((state & WriterWaitingMask) == WriterWaitingMask) // if we want to wait but there aren't any slots left...
        {
          Thread.Sleep(10); // massive contention. sleep while we wait for a slot
        }
        else if(Interlocked.CompareExchange(ref lockState, state + OneWriterWaiting, state) == state) // if we got a wait slot...
        {
          spinCount = 0; // presumably a relatively long period will pass, so reset our spin count
          writeWait.Wait();
        }
      }
    }

    void ReleaseWaitingThreads(int state)
    {
      // if any writers were waiting, release one of them. otherwise, if any readers were waiting, release all of them
      if((state & WriterWaitingMask) != 0) writeWait.Release();
      else if((state & ReaderWaitingMask) != 0) readWait.Release((uint)(state & ReaderWaitingMask));
    }

    // the high bit is set if the lock is owned by a writer. the next bit is set if the lock is reserved for a writer. the next 8 bits are
    // the number of threads waiting to write. the next 11 bits are the number of threads reading. the low 11 bits are the number of
    // threads waiting to read. this lets us check if the lock is free for writing simply by checking whether it's less than the read mask
    int lockState;

    readonly FastSemaphore readWait = new FastSemaphore(), writeWait = new FastSemaphore();
    readonly bool crossThread;
  }
  #endregion

  #region FastSemaphore
  sealed class FastSemaphore
  {
    public void Release()
    {
      lock(this)
      {
        if(count == uint.MaxValue) throw new InvalidOperationException();
        count++;
        Monitor.Pulse(this);
      }
    }

    public void Release(uint count)
    {
      if(count != 0)
      {
        lock(this)
        {
          this.count += count;
          if(this.count < count) // if it overflowed, undo the addition and throw an exception
          {
            this.count -= count;
            throw new InvalidOperationException();
          }

          if(count == 1) Monitor.Pulse(this);
          else Monitor.PulseAll(this);
        }
      }
    }

    public void Wait()
    {
      lock(this)
      {
        while(count == 0) Monitor.Wait(this);
        count--;
      }
    }

    public bool Wait(int timeoutMs)
    {
      if(timeoutMs == Timeout.Infinite)
      {
        Wait();
      }
      else
      {
        TimeoutTimer timer = new TimeoutTimer(timeoutMs);
        lock(this)
        {
          while(count == 0)
          {
            if(!Monitor.Wait(this, timer.RemainingTime)) return false;
          }
          count--;
        }
      }
      return true;
    }

    uint count;
  }
  #endregion

  #region ForegroundThreadPool
  /// <summary>Implements a simple thread pool that executes tasks using foreground threads, preventing the application from exiting until
  /// all of the work is complete.
  /// </summary>
  /// <remarks>This class can be used in place of the .NET <see cref="ThreadPool"/> class when you want to help ensure that the work will
  /// complete before the application terminates. The standard <see cref="ThreadPool"/> class uses background threads, which will terminate
  /// when the main application thread finishes, whereas the <see cref="ForegroundThreadPool"/> uses foreground threads.
  /// </remarks>
  public static class ForegroundThreadPool
  {
    /// <summary>Executes the given action on a separate worker thread.</summary>
    public static void EnqueueTask(Action action)
    {
      if(action == null) throw new ArgumentNullException();

      lock(actions)
      {
        // we want to ensure that a thread is a foreground thread before the method returns. ideally we would put the thread that will
        // run the action in the foreground, since that would lead to the simplest code, but we don't know which thread that will be. so
        // instead we'll ensure that the first thread in the pool is in the foreground. that means the first thread's IsBackground property
        // isn't a valid indicator of whether it's busy. instead, we'll use firstThreadIsBusy to keep track of that, with the assumption that
        // if firstThreadIsBusy is true, threads[0].IsBackground is false and it's not waiting. otherwise, it is waiting but
        // threads[0].IsBackground may be true or false
        if(threads.Count == 0) AddThread(); // new threads are foreground threads by default
        else if(!firstThreadIsBusy) threads[0].IsBackground = false;

        actions.Enqueue(action);

        // if we added the first action, start a timer to check on the threads. the timer will create additional threads (up to the maximum)
        // every 500 ms until the queue is empty, at which point
        if(actions.Count == 1) timer.Change(MsPerThread, MsPerThread);

        Monitor.Pulse(actions); // wake up a thread if any are waiting
      }
    }

    static void AddThread()
    {
      Thread thread = new Thread(ThreadFunc) { Name = "HiA Worker Thread" };
      threads.Add(thread);
      if(threads.Count == 1) firstThreadIsBusy = true; // if we just added the first thread, then indicate that it's busy
      thread.Start();
    }

    static void CheckThreads(object context)
    {
      lock(actions)
      {
        // if there are more tasks in the queue and we can add a new thread to handle them, then do so. otherwise, either there are no more
        // tasks or we have all the threads we're allowed to create, so stop the timer as there's no point in running it anymore
        if(actions.Count != 0 && threads.Count < MaxThreads) AddThread();
        else timer.Change(Timeout.Infinite, Timeout.Infinite);
      }
    }

    static void RemoveThread(Thread thread)
    {
      int index = threads.IndexOf(thread);
      threads.RemoveAt(index);
      // if we removed the first thread, then we need to make firstThreadIsBusy represent the state of the new first thread (if any)
      if(index == 0) firstThreadIsBusy = threads.Count != 0 && !threads[0].IsBackground;
    }

    static void ThreadFunc()
    {
      Thread currentThread = Thread.CurrentThread;
      try
      {
        while(true)
        {
          Action action;
          lock(actions)
          {
            if(currentThread == threads[0]) firstThreadIsBusy = false;

            while(actions.Count == 0) // if there's nothing to do...
            {
              currentThread.IsBackground = true; // background the thread while waiting so it doesn't prevent the application from ending
              if(!Monitor.Wait(actions, 60*1000)) // if the thread remains idle for 60 seconds, terminate it
              {
                RemoveThread(currentThread);
                return;
              }
              currentThread.IsBackground = false; // the thread was awakened, so assume there's work to do and reenter the foreground
            }

            action = actions.Dequeue();

            // if we're the first thread, then we're busy now because we have an action to execute. otherwise, if there are no actions left and
            // the first thread isn't busy, put it back into the background to allow the application to exit if it wants to
            if(currentThread == threads[0]) firstThreadIsBusy = true;
            else if(actions.Count == 0 && !firstThreadIsBusy) threads[0].IsBackground = true;
          }

          try { action(); }
          catch { } // TODO: log the exception in the future
        }
      }
      catch(ThreadAbortException)
      {
        lock(actions)
        {
          RemoveThread(currentThread); // if the thread was aborted, make sure it gets properly removed from the list of threads
          if(actions.Count != 0) // also, if there's work left to do...
          {
            if(threads.Count == 0) AddThread(); // and if there aren't any threads to do it, create one

            try { timer.Change(MsPerThread, MsPerThread); } // ensure the timer is running to create more threads as needed.
            catch(ObjectDisposedException) { }              // (it may have stopped itself if the thread count previously hit the maximum.)
          }
        }
      }
    }

    const int MsPerThread = 500; // limit thread creation to one every 500 ms

    static readonly Queue<Action> actions = new Queue<Action>();
    static readonly List<Thread> threads = new List<Thread>();
    static readonly Timer timer = new Timer(CheckThreads, null, Timeout.Infinite, Timeout.Infinite);
    static readonly int MaxThreads = Environment.ProcessorCount * 20;
    static bool firstThreadIsBusy;
  }
  #endregion

  #region ThreadUtility
  // TODO: NET4: try using Thread.Yield() instead of SwitchToThread() on .NET 4
  static class ThreadUtility
  {
    public static void BriefWait()
    {
      if(MultiProcessor) Thread.SpinWait(20);
      else SafeNativeMethods.SwitchToThread();
      dummy++;
    }

    public static void Spin1(int count)
    {
      if((count & 1) == 0)
      {
        if(MultiProcessor) Thread.SpinWait(1);
        else SafeNativeMethods.SwitchToThread();
      }
      else
      {
        Thread.Sleep(count & 1);
      }
      dummy++;
    }

    public static void Spin2(int spinCount)
    {
      if(spinCount < 5 && MultiProcessor) Thread.SpinWait(20*(spinCount+6));
      else if(spinCount < 10) Thread.Sleep(0); // or use Thread.Yield() in .NET 4
      else Thread.Sleep(1);
      dummy++;
    }

    public static void VeryBriefWait()
    {
      if(MultiProcessor) Thread.SpinWait(1);
      else SafeNativeMethods.SwitchToThread();
      dummy++;
    }

    static readonly bool MultiProcessor = Environment.ProcessorCount > 1;

    // for whatever reason, incrementing this variable at spin points greatly improves lock performance in some cases. perhaps it has some
    // effect on the processor cache. i donno. but don't remove it.
    static int dummy;
  }
  #endregion

  #region TickTimer
  /// <summary>A class similar to <see cref="Stopwatch"/> that uses a much faster but lower-resolution time source if possible. The tick
  /// timer still maintains a resolution of about 10-15 milliseconds.
  /// </summary>
  public sealed class TickTimer
  {
    /// <summary>Initializes a new <see cref="TickTimer"/> with support for very long run durations.</summary>
    public TickTimer() : this(true) { }

    /// <summary>Initializes a new <see cref="TickTimer"/>.</summary>
    /// <param name="requireLongRun">If true, the timer is guaranteed to support very long run durations, even if it must use a
    /// much slower time source. If false, the timer is only guaranteed to run correctly for about 49.7 days. Note that this limitation only
    /// affects the length of time the timer can spend actively running. You can still stop and start the timer and accumulate an elapsed
    /// time of much greater duration, as long as no single run exceeds 49.7 days.
    /// </param>
    public TickTimer(bool requireLongRun)
    {
      stopwatch = SafeNativeMethods.IsWindowsVistaOrLater || !requireLongRun ? null : new Stopwatch();
    }

    /// <summary>Gets a <see cref="TimeSpan"/> representing the total elapsed time, to within a resolution of about 10 to 20 milliseconds.</summary>
    public TimeSpan Elapsed
    {
      get { return stopwatch != null ? stopwatch.Elapsed : new TimeSpan(ElapsedMilliseconds * TimeSpan.TicksPerMillisecond); }
    }

    /// <summary>Gets the total number of elapsed milliseconds, to within a resolution of about 10 to 20 milliseconds.</summary>
    public long ElapsedMilliseconds
    {
      get { return stopwatch != null ? stopwatch.ElapsedMilliseconds : IsRunning ? elapsed + (GetTicks() - startTime) : elapsed; }
    }

    /// <summary>Gets whether the timer is currently running.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Starts the timer if it's currently stopped. This does not reset any accumulated elapsed time from a previous run.</summary>
    public void Start()
    {
      if(!IsRunning)
      {
        if(stopwatch != null) stopwatch.Start();
        else startTime = GetTicks();
        IsRunning = true;
      }
    }

    /// <summary>Stops the timer if it's currently running. This does not reset the elapsed time.</summary>
    public void Stop()
    {
      if(IsRunning)
      {
        if(stopwatch != null) stopwatch.Stop();
        else elapsed += GetTicks() - startTime;
        IsRunning = false;
      }
    }

    /// <summary>Stops the timer if it's currently running and resets the elapsed time to zero.</summary>
    public void Reset()
    {
      if(stopwatch != null) stopwatch.Reset();
      else elapsed = 0;
      IsRunning = false;
    }

    /// <summary>Resets the elapsed time to zero and starts the timer if it's not currently running.</summary>
    public void Restart()
    {
      if(stopwatch != null)
      {
        stopwatch.Reset();
        stopwatch.Start();
      }
      else
      {
        elapsed   = 0;
        startTime = GetTicks();
      }
      IsRunning = true;
    }

    /// <summary>Initializes a new <see cref="TickTimer"/> with support for very long run times.</summary>
    public static TickTimer StartNew()
    {
      return StartNew(true);
    }

    /// <summary>Initializes a new <see cref="TickTimer"/>.</summary>
    /// <param name="requireLongRun">If true, the timer is guaranteed to support very long run durations, even if it must use a
    /// much slower time source. If false, the timer is only guaranteed to run correctly for about 49.7 days. Note that this limitation only
    /// affects the length of time the timer can spend actively running. You can still stop and start the timer and accumulate an elapsed
    /// time of much greater duration, as long as no single run exceeds 49.7 days.
    /// </param>
    public static TickTimer StartNew(bool requireLongRun)
    {
      TickTimer timer = new TickTimer(requireLongRun);
      timer.Start();
      return timer;
    }

    long elapsed, startTime;
    Stopwatch stopwatch;

    static long GetTicks()
    {
      return SafeNativeMethods.IsWindowsVistaOrLater ? SafeNativeMethods.GetTickCount64() : (uint)Environment.TickCount;
    }
  }
  #endregion

  #region TimeoutTimer
  /// <summary>Implements an efficient timer to assist in the creation of methods that support timing out.</summary>
  /// <remarks>The timer internally uses a slow but high-resolution method if the timeout is between 0 and 200 milliseconds, and a
  /// fast but low-resolution method otherwise. This increases performance when the error induced by using the low-resolution timer would be
  /// small. Note that the timer is a value type (i.e. struct), not a reference type, so avoid unnecessary copies of it.
  /// </remarks>
  [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)] // the memory layout doesn't matter
  public struct TimeoutTimer
  {
    /// <summary>Initializes a new <see cref="TimeoutTimer"/> with the given timeout value.</summary>
    /// <param name="timeoutMs">The number of milliseconds before the time expires, or <see cref="System.Threading.Timeout.Infinite"/>
    /// if the time never expires.
    /// </param>
    /// <remarks>The timer internally uses a slow but high-resolution method if the timeout is between 0 and 200 milliseconds, and a
    /// fast but low-resolution method otherwise. This increases performance when the error induced by using the low-resolution timer would be
    /// small.
    /// </remarks>
    public TimeoutTimer(int timeoutMs)
    {
      if(timeoutMs != System.Threading.Timeout.Infinite && timeoutMs < 0) throw new ArgumentOutOfRangeException();
      if(timeoutMs > 0 && timeoutMs < 200)
      {
        stopwatch  = Stopwatch.StartNew();
        startTicks = 0;
      }
      else
      {
        stopwatch  = null;
        startTicks = Environment.TickCount;
      }
      _timeout = timeoutMs;
    }

    /// <summary>Gets the number of milliseconds that have elapsed since the timer was created.</summary>
    [CLSCompliant(false)]
    public uint ElapsedTime
    {
      get { return stopwatch == null ? (uint)(Environment.TickCount - startTicks) : (uint)stopwatch.ElapsedTicks; }
    }

    /// <summary>Gets whether the time has expired.</summary>
    public bool HasExpired
    {
      get { return Timeout >= 0 && ElapsedTime >= (uint)Timeout; }
    }

    /// <summary>Gets whether an infinite timeout period was specified.</summary>
    public bool IsInfinite
    {
      get { return Timeout == System.Threading.Timeout.Infinite; }
    }

    /// <summary>Gets the remaining time, in milliseconds, before the time expires. If the time has already expired, 0 is returned. If the
    /// timeout is infinite, <see cref="int.MaxValue"/> is returned.
    /// </summary>
    public int RemainingTime
    {
      get
      {
        if(Timeout > 0)
        {
          uint elapsed = ElapsedTime;
          return elapsed < (uint)Timeout ? Timeout - (int)elapsed : 0;
        }
        else
        {
          return Timeout == 0 ? 0 : int.MaxValue;
        }
      }
    }

    /// <summary>Gets the timeout value (in milliseconds) used to initialize this <see cref="TimeoutTimer"/>.</summary>
    public int Timeout
    {
      get { return _timeout; }
    }

    readonly Stopwatch stopwatch;
    readonly int startTicks, _timeout;
  }
  #endregion

} // namespace AdamMil.Utilities
