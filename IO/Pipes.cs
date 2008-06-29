/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2008 Adam Milazzo

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
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AdamMil.IO
{

/// <summary>Implements a full-duplex pipe that can be used by child processes started after the pipe's creation.</summary>
/// <remarks>
/// <para>A full-duplex pipe is an interprocess communication channel that allows data to be exchanged in both
/// directions. This class implements a simple pipe that is usable by the current process and child processes. The
/// end of the pipe controlled by the current process is called the server end, and is accessed via the
/// <see cref="ServerHandle"/>, while the end of the pipe controlled by a child process is called the client end, and
/// is accessed via the <see cref="ClientHandle"/>. The client handle value is typically passed to the child process
/// on its command line. To convert a handle into a 
/// </para>
/// <para>To convert a handle into a <see cref="Stream"/>, use a <see cref="FileStream"/>. First create a
/// <see cref="SafeFileHandle"/>, passing false so that the pipe handle is not owned by the
/// <see cref="SafeFileHandle"/>, and then pass it to the <see cref="FileStream(SafeFileHandle,FileAccess)"/>
/// constructor.
/// </para>
/// <para>If both ends of the pipe are open, reading will block until the other end has written data, and if the pipe's
/// buffer is full, writing will block until the other end has read the data that's already been written. Also note
/// that the <see cref="FileStream"/> buffers output, so data written to the <see cref="FileStream"/> will not actually
/// be sent through the pipe until its buffer is full or <see cref="Stream.Flush"/> is called.
/// If the pipe has been closed at one end, reading from the other will not block, and writing to it will fail.
/// </para>
/// <para>Finally, note that the pipe will be closed when the <see cref="InheritablePipe"/> object is destroyed, so
/// a reference must be kept to it until both sides are finished.
/// </para>
/// </remarks>
public class InheritablePipe : IDisposable
{
  /// <summary>Creates a new <see cref="InheritablePipe"/>.</summary>
  public InheritablePipe()
  {
    CreatePipe(out server, out client);
  }

  ~InheritablePipe()
  {
    Dispose(true);
  }

  /// <summary>Gets the handle of the server side of the pipe.</summary>
  /// <remarks>See <see cref="InheritablePipe"/> for a description of how to use this value.</remarks>
  public IntPtr ServerHandle
  {
    get { return server; }
  }

  /// <summary>Gets the handle of the client side of the pipe.</summary>
  /// <remarks>See <see cref="InheritablePipe"/> for a description of how to use this value.</remarks>
  public IntPtr ClientHandle
  {
    get { return client; }
  }

  /// <summary>Closes the client side of the pipe.</summary>
  /// <remarks>Normally, closing the client side of the pipe is the client's job, and so this method is not used.</remarks>
  public void CloseClient()
  {
    if(client != IntPtr.Zero)
    {
      CloseSide(client);
      client = IntPtr.Zero;
    }
  }

  /// <summary>Closes the server side of the pipe.</summary>
  public void CloseServer()
  {
    if(server != IntPtr.Zero)
    {
      CloseSide(server);
      server = IntPtr.Zero;
    }
  }

  /// <summary>Closes both sides of the pipe.</summary>
  public void Dispose()
  {
    GC.SuppressFinalize(this);
    Dispose(false);
  }

  /// <summary>Called to create the pipe and return the operating system handles for both ends of it.</summary>
  protected virtual void CreatePipe(out IntPtr server, out IntPtr client)
  {
    if(Environment.OSVersion.Platform == PlatformID.Win32NT)
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
      const string chars = "abcdefghijklmnopqrstuvwxyz1234567890`~!@#$%^&*()-=_+[]{};,.<>";
      Random rand = new Random();

      SecurityAttributes security = new SecurityAttributes();
      security.Length = (uint)Marshal.SizeOf(typeof(SecurityAttributes));
      security.InheritHandle = true;

      server = IntPtr.Zero;
      for(int tries=0; tries<20; tries++)
      {
        sb.Append(@"\\.\pipe\");
        for(int i=0; i<64; i++) sb.Append(chars[rand.Next(chars.Length)]);
        server = CreateNamedPipe(sb.ToString(), PIPE_ACCESS_DUPLEX | FIRST_PIPE_INSTANCE | FILE_FLAG_OVERLAPPED,
                                 0, 1, 4096, 4096, 0, ref security);
        if(server.ToInt64() != INVALID_HANDLE) break;
        sb.Remove(0, sb.Length);
      }
      if(server.ToInt64() == INVALID_HANDLE) throw new Exception("Unable to create named pipe.");

      client = CreateFile(sb.ToString(), GENERIC_READ | GENERIC_WRITE, 0, ref security, OPEN_EXISTING, 0, IntPtr.Zero);
      if(client.ToInt64() == INVALID_HANDLE)
      {
        CloseHandle(server);
        throw new Exception("Unable to connect to named pipe.");
      }
    }
    else throw new NotImplementedException("Unsupported operating system.");
  }

  /// <summary>Called to close a side of the pipe.</summary>
  protected virtual void CloseSide(IntPtr side)
  {
    if(Environment.OSVersion.Platform == PlatformID.Win32NT)
    {
      CloseHandle(side);
    }
    else throw new NotImplementedException("Unsupported operating system.");
  }

  /// <summary>Closes both sides of the pipe.</summary>
  void Dispose(bool finalizing)
  {
    CloseClient();
    CloseServer();
  }

  IntPtr server, client;

  #region Win32 Interop
  const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, PIPE_ACCESS_DUPLEX = 3,
             FIRST_PIPE_INSTANCE = 0x80000, OPEN_EXISTING = 3, FILE_FLAG_OVERLAPPED = 0x40000000;
  const int INVALID_HANDLE = -1;

  struct SecurityAttributes
  {
    public uint Length;
    IntPtr SecurityDescriptor;
    public bool InheritHandle;
  }

  [DllImport("kernel32.dll")]
  static extern bool CloseHandle(IntPtr handle);
  [DllImport("kernel32.dll")]
  static extern IntPtr CreateFile(string name, uint desiredAccess, uint shareMode, [In] ref SecurityAttributes secAttr,
                                  uint creationDisposition, uint flags, IntPtr template);
  [DllImport("kernel32.dll")]
  static extern IntPtr CreateNamedPipe(string name, uint openMode, uint pipeMode, uint maxInstances,
                                       uint inputBufferSize, uint outputBufferSize, uint defaultTimeout,
                                       [In] ref SecurityAttributes security);
  #endregion
}

} // namespace AdamMil.IO