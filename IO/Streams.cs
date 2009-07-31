using System;
using System.IO;

namespace AdamMil.IO
{

/// <summary>Wraps a stream using a reference count, so the wrapped stream won't be closed until the
/// <see cref="ReferenceCountedStream"/> has been closed enough times. This class is useful for passing to methods
/// that close the stream you give them, when you don't want the stream to be closed or you want to pass the same
/// stream to several such methods and only have it closed when all the methods have finished with it.
/// </summary>
public sealed class ReferenceCountedStream : Stream
{
  public ReferenceCountedStream(Stream underlyingStream, int referenceCount)
  {
    if(underlyingStream == null) throw new ArgumentNullException();
    if(referenceCount < 1) throw new ArgumentOutOfRangeException();
    stream = underlyingStream;
    this.referenceCount = referenceCount;
  }

  public override bool CanRead
  {
    get { return stream.CanRead; }
  }

  public override bool CanSeek
  {
    get { return stream.CanSeek; }
  }

  public override bool CanTimeout
  {
    get { return stream.CanTimeout; }
  }

  public override bool CanWrite
  {
    get { return stream.CanWrite; }
  }

  public override long Length
  {
    get { return stream.Length; }
  }

  public override long Position
  {
    get { return stream.Position; }
    set { stream.Position = value; }
  }

  public override int ReadTimeout
  {
    get { return stream.ReadTimeout; }
    set { stream.ReadTimeout = value; }
  }

  public override int WriteTimeout
  {
    get { return stream.WriteTimeout; }
    set { stream.WriteTimeout = value; }
  }

  public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
  {
    return stream.BeginRead(buffer, offset, count, callback, state);
  }

  public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
  {
    return stream.BeginWrite(buffer, offset, count, callback, state);
  }

  public override void Close()
  {
    if(referenceCount != 0) referenceCount--;
    else base.Close();
  }

  public override int EndRead(IAsyncResult asyncResult)
  {
    return stream.EndRead(asyncResult);
  }

  public override void EndWrite(IAsyncResult asyncResult)
  {
    stream.EndWrite(asyncResult);
  }

  public override void Flush()
  {
    stream.Flush();
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    return stream.Read(buffer, offset, count);
  }

  public override int ReadByte()
  {
    return stream.ReadByte();
  }

  public override long Seek(long offset, SeekOrigin origin)
  {
    return stream.Seek(offset, origin);
  }

  public override void SetLength(long value)
  {
    stream.SetLength(value);
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    stream.Write(buffer, offset, count);
  }

  public override void WriteByte(byte value)
  {
    stream.WriteByte(value);
  }

  protected override void Dispose(bool disposing)
  {
    if(referenceCount != 0) referenceCount--;
    else base.Dispose(disposing);
  }

  readonly Stream stream;
  int referenceCount;
}


} // namespace AdamMil.IO
