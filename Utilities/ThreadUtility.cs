/*
AdamMil.Utilities is a library providing generally useful utilities for
.NET development.

http://www.adammil.net/
Copyright (C) 2010-2011 Adam Milazzo

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
using System.Threading;

namespace AdamMil.Utilities
{

public static class ReaderWriterLockExtensions
{
  /// <summary>Performs the typical read-and-maybe-write lock pattern.</summary>
  /// <param name="lockObject">The lock to use.</param>
  /// <param name="read">A delegate executed within a read lock that returns a boolean specifying whether the write operation
  /// (<paramref name="write"/>) is unnecessary. If it returns false, the write operation will be executed.
  /// </param>
  /// <param name="write">The write operation, which is only executed if <paramref name="read"/> returns false. The write
  /// operation will be executed in a write lock.
  /// </param>
  /// <remarks>There is a small gap of time between the read and write invocations where another thread could take the lock. If
  /// this is unacceptable, use <see cref="ReaderWriterLockSlim.EnterUpgradeableReadLock"/> instead of this method.
  /// </remarks>
  public static void ReadWrite(this ReaderWriterLockSlim lockObject, Func<bool> read, Action write)
  {
    if(lockObject == null || read == null || write == null) throw new ArgumentNullException();
    lockObject.EnterReadLock();
    bool inReadLock = true;
    try
    {
      if(!read())
      {
        lockObject.ExitReadLock();
        inReadLock = false;
        lockObject.EnterWriteLock();
        write();
      }
    }
    finally
    {
      if(inReadLock) lockObject.ExitReadLock();
      else lockObject.ExitWriteLock();
    }
  }
}

} // namespace AdamMil.Utilities
