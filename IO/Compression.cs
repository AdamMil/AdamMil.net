/*
AdamMil.IO is a library that provides high performance and high level IO
tools for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2011 Adam Milazzo

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
using System.IO.Compression;
using AdamMil.Utilities;
using AdamMil.Utilities.Encodings;

namespace AdamMil.IO.Compression
{

// TODO: make these work on big-endian architectures if they don't (but where am i gonna get one of those? :-)

#region CompressionHelpers
static class CompressionHelpers
{
  // most matches in compressed data are short, so copy/fill algorithms tuned for small amounts of data can be helpful

  // TODO: incorporate this logic into Unsafe.Copy() if it's useful
  internal static unsafe void Copy(byte* src, byte* dest, int count)
  {
    restart:
    switch(count)
    {
      case 0: break;
      case 1: dest[0] = src[0]; break;
      case 2: *(ushort*)dest = *(ushort*)src; break;
      case 3:
        *(ushort*)dest = *(ushort*)src;
        dest[2] = src[2];
        break;
      case 4: *(uint*)dest = *(uint*)src; break;
      case 5:
        *(uint*)dest = *(uint*)src;
        dest[4] = src[4];
        break;
      case 6:
        *(uint*)dest = *(uint*)src;
        *(ushort*)(dest+4) = *(ushort*)(src+4);
        break;
      case 7:
        *(uint*)dest = *(uint*)src;
        *(ushort*)(dest+4) = *(ushort*)(src+4);
        dest[6] = src[6];
        break;
      case 8:
        *(uint*)dest = *(uint*)src;
        *(uint*)(dest+4) = *(uint*)(src+4);
        break;
      default:
        if(count < 32)
        {
          int copied;
          *(uint*)dest     = *(uint*)src;
          *(uint*)(dest+4) = *(uint*)(src+4);
          if(count < 16)
          {
            copied = 8;
          }
          else
          {
            *(uint*)(dest+8)  = *(uint*)(src+8);
            *(uint*)(dest+12) = *(uint*)(src+12);
            if(count < 24)
            {
              copied = 16;
            }
            else
            {
              *(uint*)(dest+16) = *(uint*)(src+16);
              *(uint*)(dest+20) = *(uint*)(src+20);
              copied = 24;
            }
          }

          if(copied != count)
          {
            src   += copied;
            dest  += copied;
            count -= copied;
            goto restart;
          }
        }
        else
        {
          Unsafe.Copy(src, dest, count);
        }
        break;
    }
  }

  // TODO: incorporate this logic into Unsafe.Fill() if it's useful
  internal static unsafe void Fill(byte* dest, byte value, int count)
  {
    if(count >= 32)
    {
      Unsafe.Fill(dest, value, count);
    }
    else
    {
      ushort shortValue = (ushort)((value<<8) | value);
      uint uintValue = (uint)((shortValue<<16) | shortValue);
      restart:
      switch(count)
      {
        case 0: break;
        case 1: dest[0] = value; break;
        case 2: *(ushort*)dest = shortValue; break;
        case 3:
          *(ushort*)dest = shortValue;
          dest[2] = value;
          break;
        case 4: *(uint*)dest = uintValue; break;
        case 5:
          *(uint*)dest = uintValue;
          dest[4] = value;
          break;
        case 6:
          *(uint*)dest = uintValue;
          *(ushort*)(dest+4) = shortValue;
          break;
        case 7:
          *(uint*)dest = uintValue;
          *(ushort*)(dest+4) = shortValue;
          dest[6] = value;
          break;
        case 8:
          *(uint*)dest = uintValue;
          *(uint*)(dest+4) = uintValue;
          break;
        default:
          int filled;
          *(uint*)dest     = uintValue;
          *(uint*)(dest+4) = uintValue;
          if(count < 16)
          {
            filled = 8;
          }
          else
          {
            *(uint*)(dest+8)  = uintValue;
            *(uint*)(dest+12) = uintValue;
            if(count < 24)
            {
              filled = 16;
            }
            else
            {
              *(uint*)(dest+16) = uintValue;
              *(uint*)(dest+20) = uintValue;
              filled = 24;
            }
          }

          if(filled != count)
          {
            dest  += filled;
            count -= filled;
            goto restart;
          }
          break;
      }
    }
  }
}
#endregion

#region PKWareDCLCompressor
/// <summary>Implements a <see cref="BinaryEncoder"/> that compresses data in the format used by the PKWare Data Compression Library.</summary>
public sealed class PKWareDCLCompressor : BinaryEncoder
{
  public PKWareDCLCompressor() : this(0) { }

  public PKWareDCLCompressor(int dictionarySize)
  {
    if(dictionarySize == 0) dictionarySize = 2048;
    if(dictionarySize != 1024 && dictionarySize != 2048 && dictionarySize != 4096) throw new ArgumentOutOfRangeException();
    this.dictionarySize = dictionarySize;
    dictionarySizeSelector = dictionarySize == 4096 ? 6 : dictionarySize == 2048 ? 5 : 4;
  }

  /// <inheritdoc/>
  public unsafe override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex, bool flush)
  {
    Utility.ValidateRange(source, sourceIndex, sourceCount);
    Utility.ValidateRange(destination, destinationIndex, 0);
    fixed(byte* srcBase=source)
    fixed(byte* destBase=destination)
    {
      byte sourceByte;
      return Encode(srcBase == null ? &sourceByte : srcBase+sourceIndex, sourceCount, // srcBase is null when source is zero bytes
                    destBase+destinationIndex, destination.Length-destinationIndex, flush);
    }
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public override unsafe int Encode(byte* source, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    if(source == null || destination == null) throw new ArgumentNullException();
    if(sourceCount < 0 || destinationCapacity < 0) throw new ArgumentOutOfRangeException();

    byte* destPtr = destination, destEnd = destination + destinationCapacity;

    if(dictionary == null)
    {
      dictionary = new byte[dictionarySize];
      _hashStart = new ushort[HashSize];
      _hashPrev  = new ushort[dictionarySize];
    }

    fixed(byte* dict=dictionary)
    fixed(ushort* hashStart=_hashStart)
    fixed(ushort* hashPrev=_hashPrev)
    {
      int bytesWritten = s.Update(source, sourceCount, destPtr, destEnd, dict, dictionarySize, hashStart, hashPrev, flush);
      if(flush) Reset();
      return bytesWritten;
    }
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public override unsafe int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    if(data == null) throw new ArgumentNullException();
    if(count < 0) throw new ArgumentOutOfRangeException();

    byte* dict = stackalloc byte[dictionarySize];
    ushort* hashStart = stackalloc ushort[HashSize];
    ushort* hashPrev  = stackalloc ushort[dictionarySize];

    if(dictionary != null)
    {
      fixed(byte* dictSrc=dictionary) Unsafe.Copy(dictSrc, dict, dictionarySize);
      fixed(ushort* hashStartSrc=_hashStart) Unsafe.Copy(hashStartSrc, hashStart, HashSize*sizeof(ushort));
      fixed(ushort* hashPrevSrc=_hashPrev) Unsafe.Copy(hashPrevSrc, hashPrev, dictionarySize*sizeof(ushort));
    }

    State s = this.s; // make a copy of the compressor state
    return s.Update(data, count, null, null, dict, dictionarySize, hashStart, hashPrev, simulateFlush);
  }

  /// <inheritdoc/>
  public unsafe override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(data, index, count);
    fixed(byte* dataPtr=data)
    {
      byte dataByte; // dataPtr is null when source is zero bytes, so provide /some/ pointer
      return GetByteCount(dataPtr == null ? &dataByte : dataPtr+index, count, simulateFlush);
    }
  }

  /// <inheritdoc/>
  public override int GetMaxBytes(int unencodedByteCount)
  {
    if(unencodedByteCount < 0) throw new ArgumentOutOfRangeException();
    // assume all bytes are stored literally (multiplying the number of bits by 9/8), plus 32 bits for the header and footer and 7 bits to
    // ensure that we round up to the next byte. above 1908874349, this overflows an int
    return unencodedByteCount > 1908874349 ? int.MaxValue : (int)(((long)unencodedByteCount*9 + 39 + s.bitsInBuffer)/8);
  }

  /// <inheritdoc/>
  public override void Reset()
  {
    if(_hashStart != null)
    {
      Array.Clear(_hashStart, 0, _hashStart.Length);
      Array.Clear(_hashPrev, 0, _hashPrev.Length);
    }
    s = new State();
  }

  const int HashShift = 3, HashSize = 512, HashMask = HashSize-1; // HashSize should equal 2^(HashShift*3)

  #region State
  unsafe struct State
  {
    public int Update(byte* source, int sourceCount, byte* destPtr, byte* destEnd, byte* dict, int dictionarySize,
                      ushort* hashStart, ushort* hashPrev, bool flush)
    {
      this.byteCount = 0;
      this.destEnd   = destEnd;
      this.dict      = dict;
      this.hashPrev  = hashPrev;
      this.hashStart = hashStart;
      this.srcBase   = source;
      this.dictionarySize = dictionarySize;
      this.dictionarySizeSelector = dictionarySize == 4096 ? 6 : dictionarySize == 2048 ? 5 : 4;

      if(!wroteHeader)
      {
        if(destPtr != null)
        {
          if(destPtr+2 > destEnd) throw new ArgumentException("There is insufficient space in the output buffer.");
          *destPtr++ = 0;
          *destPtr++ = (byte)dictionarySizeSelector;
        }
        byteCount   = 2;
        wroteHeader = true;
      }

      byte* srcPtr = source, srcEnd = source + sourceCount;
      if(matchLength == 0 && sourceCount != 0)
      {
        matchByte   = matchHash = *srcPtr++;
        matchLength = 1;
      }

      while(srcPtr < srcEnd)
      {
        byte value = *srcPtr++;

        if(matchLength < 3) // if we aren't currently tracking a match in the dictionary...
        {
          if(matchLength == 1) // if we have only 2 bytes so far (including the new value), buffer up more input data
          {
            matchByte |= value << 8;
            matchHash  = (matchHash<<HashShift) ^ value;
            matchLength++;
            continue;
          }
          else // we have 3 bytes, so we can begin searching with the hash
          {
            matchHash = (matchHash<<HashShift) ^ value;
            int index = hashStart[matchHash & HashMask];
            bool wrapped = index >= writeIndex;
            byte a = (byte)matchByte, b = (byte)(matchByte>>8);
            while(true)
            {
              if(dict[index] == value)
              {
                int prevIndex = index-1;
                if(WrapBack(ref prevIndex) && dict[prevIndex] == b && prevIndex-- != writeIndex &&
                   WrapBack(ref prevIndex) && dict[prevIndex] == a && index != writeIndex)
                {
                  matchOffset = prevIndex - writeIndex;
                  matchLength = 3;
                  matchByte   = a == b && a == value ? value : -1;
                  goto foundMatch;
                }
              }

              int nextIndex = hashPrev[index];
              if(nextIndex >= index)
              {
                if(wrapped) break;
                wrapped = true;
              }
              if(wrapped && (nextIndex < writeIndex || nextIndex == index)) break;
              index = nextIndex;
            }

            // if we couldn't find a match of length 3+ using the hash table, look for a repeating match or a recent 2-byte match
            if(a == b && a == value) // if we have a repeating match of distance 0...
            {
              index = writeIndex - 1;
              if(!WrapBack(ref index) || dict[index] != value) // if the match isn't in the dictionary yet...
              {
                if(bitsInBuffer > 32-9) destPtr = FlushBitBuffer(destPtr);
                OutputLiteralByte(a); // put it there and retry
                continue;
              }
              matchByte   = value;
              matchLength = 3;
              matchOffset = index - writeIndex;
            }
            else // otherwise, output a byte that we had buffered and search again with the most recent two bytes
            {
              if(bitsInBuffer > 32-9) destPtr = FlushBitBuffer(destPtr);
              OutputLiteralByte(a);
              matchByte = b | (value<<8);
              matchHash = (b<<HashShift) ^ value;
              continue;
            }

            foundMatch:
            matchIndex = index;
            continue; // if we found a match, continue to the next input byte
          }
        }
        else // we have a match of at least length 3, so try to extend it
        {
          matchHash = (matchHash<<HashShift) ^ value;
          int index = matchIndex + 1;
          if(index == dictionarySize) index = 0; // if it hit the end of the dictionary, make it wrap around
          if(index == writeIndex) index += matchOffset; // if the index ran into the write pointer, move it back to its start position
          if(dict[index] == value) // if it matched...
          {
            if(matchByte != value) matchByte = -1; // update the match byte
          }
          else // if a mismatch occurred, see if there are any better matches
          {
            // now scan backwards for another match by looking for the new, mismatched character. i believe that if the first match failed,
            // it is impossible for any other match to repeat. so i will make that assumption
            int matchStart = writeIndex + matchOffset;

            // we need to determine where to start looking. if the match included the byte just before the write pointer, then it is my
            // belief that a new match cannot be found that overlaps it, so we can start at the byte before it began. otherwise, the new
            // match may overlap, and we should start at the end of it
            if(~(uint)matchOffset < (uint)matchLength) // if the match abuts the write pointer (this is the same as a<0 && -a <= b)
            {
              index     = matchStart;
              matchByte = -1;
            }
            else
            {
              if(matchByte != value)
              {
                matchByte = -1;
              }
              else if(matchOffset != 0) // if all the bytes so far have the same value and the match may be able to be extended backwards..
              {
                index = matchStart - 1;
                if(index >= 0) // if we haven't hit the start of the dictionary yet...
                {
                  if(dict[index] == value)
                  {
                    matchOffset--;
                    index = matchIndex;
                    goto searchSucceeded;
                  }
                }
                else if(bytesInDictionary == dictionarySize) // otherwise if we have, but we can wrap around...
                {
                  index = bytesInDictionary-1;
                  if(dict[index] == value)
                  {
                    matchOffset = index - writeIndex;
                    index = matchIndex;
                    goto searchSucceeded;
                  }
                }
                else // otherwise, we've run out of data and won't find a match in any case
                {
                  goto searchFailed;
                }

                matchByte = -1;
              }
              else // otherwise, we've run out of data and won't find a match in any case
              {
                goto searchFailed;
              }

              index = matchIndex;
            }

            bool wrapped = index >= writeIndex;
            index = hashStart[matchHash & HashMask];
            if(index >= writeIndex) wrapped = true;
            else if(wrapped) goto searchFailed;

            while(true)
            {
              if(dict[index] == value) // if the new, mismatched value was found here, it may be the end of a new match that includes it
              {
                int newIndex = index - matchLength;
                if(!WrapBack(ref newIndex)) goto noMatch;
                for(int len=0, oldIndex=matchStart; len<matchLength; len++) // see if the other bytes match
                {
                  if(dict[newIndex] != dict[oldIndex]) goto noMatch;
                  if(++newIndex == bytesInDictionary) newIndex = 0;
                  if(++oldIndex == dictionarySize) oldIndex = 0;
                  if(oldIndex == writeIndex) oldIndex += matchOffset;
                }

                // all the bytes match, so we probably have our new match. we just have to make sure it doesn't straddle the write pointer
                newIndex = index - matchLength;
                if(newIndex < 0) newIndex += dictionarySize;
                if(newIndex < index ? writeIndex <= newIndex || writeIndex > index : writeIndex <= newIndex && writeIndex > index)
                {
                  matchStart = newIndex;
                  if(matchStart < 0) matchStart += bytesInDictionary;
                  matchOffset = matchStart - writeIndex;
                  goto searchSucceeded;
                }
              }

              noMatch:
              int nextIndex = hashPrev[index];
              if(nextIndex >= index)
              {
                if(wrapped) goto searchFailed;
                wrapped = true;
              }
              if(wrapped && (nextIndex < writeIndex || nextIndex == index)) goto searchFailed;
              index = nextIndex;
            }
          }

          searchSucceeded: // the match could be extended
          matchIndex = index;
          // if we've reached the maximum match length, output it
          if(++matchLength == 518)
          {
            destPtr = OutputMatch(srcPtr, destPtr);
            if(srcPtr < srcEnd) // since we've even consumed the new value, we need to prime the match again with the next value
            {
              matchByte   = matchHash = *srcPtr++;
              matchLength = 1;
            }
          }
          continue; // in any case, we've consumed the character so go to the next one

          // at this point, the input byte does not match the dictionary. output the match and add the mismatched byte to the buffer
          searchFailed:
          destPtr = OutputMatch(srcPtr-1, destPtr);
          matchByte   = matchHash = value;
          matchLength = 1;
        }
      }

      // now we've reached the end of the input. if we're supposed to flush the stream, output the final bytes
      if(flush)
      {
        if(matchLength != 0) // if we have a match that we haven't output yet, write it now
        {
          if(matchLength < 3) // if the match is just stored in the match byte buffer, output the literal bytes...
          {
            if(bitsInBuffer > 32-18) destPtr = FlushBitBuffer(destPtr); // if the buffer would overflow, write it out first
            OutputLiteralByte((byte)matchByte);
            if(matchLength == 2) OutputLiteralByte((byte)(matchByte>>8));
          }
          else // otherwise, it's stored in the dictionary, so output it there
          {
            destPtr = OutputMatch(srcPtr, destPtr);
          }
        }

        if(bitsInBuffer > 16) destPtr = FlushBitBuffer(destPtr);
        bitBuffer    |= 0xFF01u << bitsInBuffer; // output the end-of-stream marker
        bitsInBuffer += 16;
        // now add whatever bits are needed to make the buffer an even multiple of 8 bits
        int extra = bitsInBuffer & 7;
        if(extra != 0) bitsInBuffer += 8 - extra;
        // write the final bits and reset the encoder
        destPtr = FlushBitBuffer(destPtr);
      }

      return byteCount;
    }

    unsafe void AddStringsToHash(int count)
    {
      System.Diagnostics.Debug.Assert(count > 0);
      do
      {
        writeHash = ((writeHash<<HashShift) ^ dict[writeIndex]) & HashMask;
        int previousIndex = hashStart[writeHash];
        hashStart[writeHash] = (ushort)writeIndex;
        hashPrev[writeIndex] = (ushort)previousIndex;

        writeIndex++;
        if(writeIndex > bytesInDictionary) bytesInDictionary = writeIndex;
        if(writeIndex == dictionarySize) writeIndex = 0;
      } while(--count != 0);
    }

    unsafe void CopyMatchToDictionary(byte* src, bool mayOverlap)
    {
      int chunkSize = Math.Min(matchLength, dictionarySize-writeIndex);
      if(mayOverlap) Unsafe.Copy(src, dict+writeIndex, chunkSize);
      else CompressionHelpers.Copy(src, dict+writeIndex, chunkSize);
      AddStringsToHash(chunkSize);
    
      if(chunkSize < matchLength) // write the second part of the data if it needs to be wrapped
      {
        src += chunkSize;
        chunkSize = matchLength - chunkSize;
        if(mayOverlap) Unsafe.Copy(src, dict, chunkSize);
        else CompressionHelpers.Copy(src, dict, chunkSize);
        AddStringsToHash(chunkSize);
      }
    }

    unsafe byte* FlushBitBuffer(byte* dest)
    {
      if(dest == null)
      {
        do
        {
          byteCount++;
          bitBuffer   >>= 8;
          bitsInBuffer -= 8;
        } while(bitsInBuffer > 7);
      }
      else
      {
        do
        {
          if(dest == destEnd) throw new ArgumentException("There is insufficient space in the output buffer.");
          byteCount++;
          *dest++ = (byte)bitBuffer;
          bitBuffer   >>= 8;
          bitsInBuffer -= 8;
        } while(bitsInBuffer > 7);
      }
      return dest;
    }

    unsafe byte* OutputMatch(byte* src, byte* dest)
    {
      // otherwise, we'll try to output a length-distance pair describing the data to copy from the dictionary
      int distance = -matchOffset - 1; // the distance is counted in reverse from the last byte written
      if(distance < 0) distance += bytesInDictionary;

      // output the match. at this point, there may be up to 7 bits in the buffer. first we need to encode the length,
      // which will add up to 16 bits
      int bits, bitCount;
      switch(matchLength)
      {
        case 2:  bits = 11; bitCount = 4; break; //    1011
        case 3:  bits = 7;  bitCount = 3; break; //     111
        case 4:  bits = 3;  bitCount = 4; break; //    0011
        case 5:  bits = 13; bitCount = 4; break; //    1101
        case 6:  bits = 21; bitCount = 5; break; //   10101
        case 7:  bits = 5;  bitCount = 5; break; //   00101
        case 8:  bits = 25; bitCount = 5; break; //   11001
        case 9:  bits = 41; bitCount = 6; break; //  101001
        case 10: bits = 9;  bitCount = 7; break; // 0001001
        case 11: bits = 73; bitCount = 7; break; // 1001001
        default:
          if(matchLength < 16) // 12-15: xx110001
          {
            bits     = ((matchLength-12)<<6) | 49;
            bitCount = 8;
          }
          else if(matchLength < 24) // 16-23: xxx010001
          {
            bits     = ((matchLength-16)<<6) | 17;
            bitCount = 9;
          }
          else if(matchLength < 40) // 24-39: xxxx1100001
          {
            bits     = ((matchLength-24)<<7) | 97;
            bitCount = 11;
          }
          else if(matchLength < 72) // 40-71: xxxxx0100001
          {
            bits     = ((matchLength-40)<<7) | 33;
            bitCount = 12;
          }
          else if(matchLength < 136) // 72-135: xxxxxx1000001
          {
            bits     = ((matchLength-72)<<7) | 65;
            bitCount = 13;
          }
          else if(matchLength < 264) // 136-263: xxxxxxx10000001
          {
            bits     = ((matchLength-136)<<8) | 129;
            bitCount = 15;
          }
          else // 264-518: xxxxxxxx00000001
          {
            bits     = ((matchLength-264)<<8) | 1;
            bitCount = 16;
          }
          break;
      }

      if(bitsInBuffer + bitCount > 32) dest = FlushBitBuffer(dest);
      bitBuffer    |= (uint)bits << bitsInBuffer;
      bitsInBuffer += bitCount;

      // now we need to encode the distance. the upper 6 bits and the lower bits are encoded separately. first, we encode the upper bits
      int lowerBits = matchLength == 2 ? 2 : dictionarySizeSelector; // as a special case, there are 2 lower bits when the length is 2
      bits = distance >> lowerBits;
      if(bits == 0) bitCount = 2;
      else if(bits < 3) bitCount = 4;
      else if(bits < 7) bitCount = 5;
      else if(bits < 22) bitCount = 6;
      else if(bits < 48) bitCount = 7;
      else bitCount = 8;
      bits = distTable[bits];

      // now add the lower bits
      bits |= (distance & ((1<<lowerBits)-1)) << bitCount;
      bitCount += lowerBits;

      // and add the whole thing to the buffer
      if(bitsInBuffer + bitCount > 32) dest = FlushBitBuffer(dest); // up to 14 more bits were added, so check for overflow
      bitBuffer    |= (uint)bits << bitsInBuffer;
      bitsInBuffer += bitCount;

      // copy the match to the dictionary
      if(distance == 0) // if we're writing a run of a single byte, we can do a fill rather than a copy
      {
        byte value = *(dict + writeIndex + matchOffset);
        int chunkSize = Math.Min(matchLength, dictionarySize-writeIndex);
        CompressionHelpers.Fill(dict+writeIndex, value, chunkSize);
        AddStringsToHash(chunkSize);

        if(chunkSize < matchLength)
        {
          chunkSize = matchLength - chunkSize;
          CompressionHelpers.Fill(dict, value, chunkSize);
          AddStringsToHash(chunkSize);
        }
      }
      else if(src - matchLength >= srcBase) // if all the data came from this call, it can be found together in the source buffer
      {
        CopyMatchToDictionary(src-matchLength, false);
      }
      // otherwise, the data came from multiple calls and we need to copy it from one part of the dictionary to another
      else if(matchLength >= distance) // if the match contained no repeats, we can copy it without looping
      {
        CopyMatchToDictionary(dict + writeIndex + matchOffset, true);
      }
      else // otherwise, we'll need to copy the data in a loop
      {
        // it's possible that one iteration may overwrite source data needed by another iteration, so we'll copy to a temporary buffer
        if(scratchBuffer == null || scratchBuffer.Length < matchLength) scratchBuffer = new byte[matchLength];
        fixed(byte* temp=scratchBuffer)
        {
          int offset = distance+1, readIndex = writeIndex - offset;
          if(readIndex < 0) readIndex += dictionarySize;
          for(byte* tempDest=temp, end=tempDest+matchLength; tempDest < end; )
          {
            int chunkSize = Math.Min(offset, (int)(end-tempDest));
            int partSize = Math.Min(chunkSize, dictionarySize-readIndex);
            CompressionHelpers.Copy(dict+readIndex, tempDest, partSize);
            if(partSize < chunkSize) CompressionHelpers.Copy(dict, tempDest+partSize, chunkSize-partSize);
            tempDest += chunkSize;
          }
          CopyMatchToDictionary(temp, false);
        }
      }

      // reset the match data
      matchLength = 0;
      return dest;
    }

    unsafe void OutputLiteralByte(byte value)
    {
      bitBuffer    |= (uint)value << (bitsInBuffer+1);
      bitsInBuffer += 9;

      // add it to the dictionary
      dict[writeIndex] = value;

      // and add it to the hash table
      writeHash = ((writeHash<<HashShift) ^ value) & HashMask;
      int previousIndex = hashStart[writeHash];
      hashStart[writeHash] = (ushort)writeIndex;
      hashPrev[writeIndex] = (ushort)previousIndex;

      writeIndex++;
      if(writeIndex > bytesInDictionary) bytesInDictionary = writeIndex;
      if(writeIndex == dictionarySize) writeIndex = 0;
    }

    bool WrapBack(ref int index)
    {
      if(index < 0)
      {
        if(bytesInDictionary < dictionarySize) return false;
        index += dictionarySize;
      }
      return true;
    }

    byte[] scratchBuffer;
    byte* dict, srcBase, destEnd;
    ushort* hashStart, hashPrev;
    uint bitBuffer;
    public int bitsInBuffer;
    int bytesInDictionary, writeIndex, writeHash, matchIndex, matchLength, matchOffset, matchHash, matchByte, byteCount;
    int dictionarySize, dictionarySizeSelector;
    bool wroteHeader;
  }
  #endregion

  byte[] dictionary;
  ushort[] _hashStart, _hashPrev;
  readonly int dictionarySize, dictionarySizeSelector;
  State s;

  // these are the encodings of the upper six bits of the distance
  static readonly byte[] distTable = new byte[64]
  {
    3, 13, 5, 25, 9, 17, 1, 62, 30, 46, 14, 54, 22, 38, 6, 58, 26, 42, 10, 50, 18, 34, 66, 2, 124, 60, 92, 28, 108, 44, 76, 12, 116, 52,
    84, 20, 100, 36, 68, 4, 120, 56, 88, 24, 104, 40, 72, 8, 240, 112, 176, 48, 208, 80, 144, 16, 224, 96, 160, 32, 192, 64, 128, 0,
  };
}
#endregion

#region PKWareDCLDecompressor
/// <summary>Implements a <see cref="BinaryEncoder"/> that decompresses data compressed by the PKWare Data Compression Library.</summary>
public sealed class PKWareDCLDecompressor : BinaryEncoder
{
  /// <inheritdoc/>
  [CLSCompliant(false)]
  public override unsafe int Encode(byte* src, int sourceCount, byte* destination, int destinationCapacity, bool flush)
  {
    if(src == null || destination == null) throw new ArgumentNullException();
    if(sourceCount < 0 || destinationCapacity < 0) throw new ArgumentOutOfRangeException();

    int bytesWritten = 0;
    uint buffer = bitBuffer; // copy the bit buffer into local variables so the compiler may do fewer writes to memory
    int bits = bitsInBuffer;

    byte* srcEnd = src + sourceCount;

    while(bits <= 24 && src != srcEnd) // copy as much into the bit buffer as we can
    {
      buffer |= (uint)*src++ << bits;
      bits += 8;
    }

    if(state == State.Start)
    {
      if(bits < 16) return 0; // if there isn't enough data to read the header, we can't do anything
      byte literalHandling = (byte)buffer, dictionarySizeSelector = (byte)(buffer>>8);

      buffer >>= 16;
      bits    -= 16;

      // validate the header
      if(literalHandling > 1 || dictionarySizeSelector < 4 || dictionarySizeSelector > 6)
      {
        throw new InvalidDataException("The header is invalid.");
      }

      fixedLengthLiterals = literalHandling == 0;
      lowerBitCount       = dictionarySizeSelector;

      // the dictionary size is 1k, 2k, or 4k depending on whether the size selector is 4, 5, or 6
      int dictionarySize = 64 << dictionarySizeSelector;
      if(dictionary == null || dictionary.Length != dictionarySize) dictionary = new byte[dictionarySize];
      bytesInDictionary = 0;
      writeIndex        = 0;
      state = State.GotHeader;
    }

    if(state != State.End)
    {
      // at this point, we're in the GotHeader or GotLength state, which means we should read code words and write them to the output
      fixed(byte* dict=dictionary)
      fixed(byte* tableBase=tables)
      {
        byte* dest = destination, destEnd = dest + destinationCapacity;

        while(true)
        {
          while(bits <= 24 && src != srcEnd) // copy as much into the bit buffer as we can
          {
            buffer |= (uint)*src++ << bits;
            bits   += 8;
          }

          if(state == State.GotLength)
          {
            // we're in the middle of copying data out of the dictionary. we know how many bytes to copy, but not the distance to them
            int distance = tableBase[(byte)buffer + (128+8+64)], bitsConsumed;
            if(distance < 3)
            {
              bitsConsumed = distance == 0 ? 2 : 4;
            }
            else
            {
              bitsConsumed = (distance>>6) + 5;
              distance &= 63;
            }

            // now that we have the upper six bits, we need the lower bits. if the copy length is 2, there are two lower order bits.
            // otherwise, there are a number of bits equal to second byte of the header
            int lowBits = bytesToCopy == 2 ? 2 : lowerBitCount;
            distance = (distance<<lowBits) | (int)((buffer>>bitsConsumed) & ((1u<<lowBits)-1));
            bitsConsumed += lowBits;

            if(bitsConsumed > bits) break; // if the read was not legitimate, wait for more data
            buffer >>= bitsConsumed;
            bits -= bitsConsumed;

            // now we have all the information we need to start copying data to the output. the offset into the dictionary is taken
            // from the end, so an offset of 0 refers to the last byte inserted into the dictionary, 1 is the previous byte, etc.
            // it is possible for the length to refer to an amount of data greater than that after the offset. in that case, the
            // data after the offset is repeatedly copied. in any case, whatever data is copied to the output is also inserted into
            // the dictionary so that after we're done, the dictionary will contain the last N bytes written to the output stream,
            // where N is the dictionary size
            if(dest + bytesToCopy > destEnd) throw new ArgumentException("There is insufficient space in the output buffer.");
            if(distance >= bytesInDictionary) throw new InvalidDataException("Insufficient data in the dictionary.");

            distance++;
            int readIndex = writeIndex - distance, chunkSize;
            if(readIndex < 0) readIndex += dictionary.Length;

            // copy the data to the output array, since we may be copying a very small amount of memory a large number of times, we'll
            // optimize for various common cases
            if(distance == 1) // if we're doing a fill with a single byte, use a specialized method for that
            {
              CompressionHelpers.Fill(dest, dict[readIndex], bytesToCopy);
              dest += bytesToCopy;
            }
            else if(distance >= bytesToCopy) // if we don't have to loop, we can just copy
            {
              chunkSize = Math.Min(bytesToCopy, dictionary.Length-readIndex);
              CompressionHelpers.Copy(dict+readIndex, dest, chunkSize);
              if(chunkSize < bytesToCopy) CompressionHelpers.Copy(dict, dest+chunkSize, bytesToCopy-chunkSize);
              dest += bytesToCopy;
            }
            else if(distance == 2) // it's common to fill a run of two identical bytes, so special case that also
            {
              byte a = dict[readIndex++], b = dict[readIndex == dictionary.Length ? 0 : readIndex];
              if(a == b)
              {
                CompressionHelpers.Fill(dest, a, bytesToCopy);
                dest += bytesToCopy;
              }
              else
              {
                for(byte* end = dest + (bytesToCopy & ~1); dest < end; dest += 2)
                {
                  dest[0] = a;
                  dest[1] = b;
                }
                if((bytesToCopy & 1) != 0) *dest++ = a;
              }
            }
            else if(distance <= dictionary.Length-readIndex) // if it won't wrap, we can simplify the loop...
            {
              if(distance == 4) // 4 bytes is also a common distance
              {
                uint value = *(uint*)(dict+readIndex);
                for(byte* end = dest + (bytesToCopy & ~3); dest < end; dest += 4) *(uint*)dest = value;

                int remainder = bytesToCopy & 3;
                if(remainder != 0)
                {
                  CompressionHelpers.Copy(dict+readIndex, dest, remainder);
                  dest += remainder;
                }
              }
              else
              {
                for(byte* end=dest+bytesToCopy; dest < end; )
                {
                  chunkSize = Math.Min(distance, (int)(end-dest));
                  CompressionHelpers.Copy(dict+readIndex, dest, chunkSize);
                  dest += chunkSize;
                }
              }
            }
            else // if it will wrap, do the slow loop (it's probably a large number of bytes anyway, so it's not that bad)
            {
              for(byte* end=dest+bytesToCopy; dest < end; )
              {
                chunkSize = Math.Min(distance, (int)(end-dest));
                int partSize = Math.Min(chunkSize, dictionary.Length-readIndex);
                CompressionHelpers.Copy(dict+readIndex, dest, partSize);
                CompressionHelpers.Copy(dict, dest+partSize, chunkSize-partSize);
                dest += chunkSize;
              }
            }

            // copy the data we wrote back into the dictionary
            chunkSize = Math.Min(bytesToCopy, dictionary.Length-writeIndex);
            CompressionHelpers.Copy(dest-bytesToCopy, dict+writeIndex, chunkSize);
            writeIndex += chunkSize;
            if(writeIndex > bytesInDictionary) bytesInDictionary = writeIndex;
            if(writeIndex == dictionary.Length) writeIndex = 0;

            if(chunkSize < bytesToCopy) // if the data must wrap around, copy the second part
            {
              chunkSize = bytesToCopy - chunkSize;
              CompressionHelpers.Copy(dest-chunkSize, dict, chunkSize);
              writeIndex = chunkSize;
            }

            state = State.GotHeader;
          }
          else if((buffer & 1) != 0) // if the first bit of the code word is one, then we read data from the dictionary
          {
            int length = tableBase[(buffer>>1) & 127], bitsConsumed;
            if((length & 128) == 0)
            {
              bitsConsumed = length >> 4;
              length &= 15;
            }
            else
            {
              int codeBits = ((length>>4) & 7) + 1, extraBits = length & 15;
              length = (int)((buffer>>codeBits) & ((1u<<extraBits)-1)) + (tableBase[extraBits + (128-1)] << 1);

              if(length == 519 && bits >= 16) // if we're legitimately at the end of the stream
              {
                bits -= 16;
                state = State.End;
                break;
              }

              bitsConsumed = codeBits + extraBits;
            }

            if(bitsConsumed > bits) break; // if the read was not legitimate, wait for more data

            buffer >>= bitsConsumed;
            bits -= bitsConsumed;
            bytesToCopy = length;
            state = State.GotLength;
          }
          else // the first bit of the code was 0, so a literal byte follows
          {
            byte value;

            int bitsConsumed;
            if(fixedLengthLiterals)
            {
              value = (byte)(buffer >> 1);
              bitsConsumed = 9;
            }
            else
            {
              value = DecodeLiteralByte(buffer, tableBase, out bitsConsumed);
            }

            if(bitsConsumed > bits) break; // if the read was not legitimate, wait for more data

            if(dest == destEnd) throw new ArgumentException("There is insufficient space in the output buffer.");
            *dest++ = value;

            buffer >>= bitsConsumed;
            bits -= bitsConsumed;

            dictionary[writeIndex++] = value;
            if(writeIndex > bytesInDictionary) bytesInDictionary = writeIndex;
            if(writeIndex == dictionary.Length) writeIndex = 0;
          }
        }

        bytesWritten = (int)(dest - destination);
      }

      bitBuffer    = buffer;
      bitsInBuffer = bits;

      if(flush)
      {
        if(bits > 7) throw new InvalidDataException("Unexpected bytes after the end of the data.");
        else if(state != State.End) throw new InvalidDataException("The data stream was truncated.");
        else Reset();
      }
    }

    return bytesWritten;
  }

  /// <inheritdoc/>
  public unsafe override int Encode(byte[] source, int sourceIndex, int sourceCount, byte[] destination, int destinationIndex,
                                    bool flush)
  {
    Utility.ValidateRange(source, sourceIndex, sourceCount);
    Utility.ValidateRange(destination, destinationIndex, 0);
    fixed(byte* srcBase=source)
    fixed(byte* destBase=destination)
    {
      byte sourceByte;
      return Encode(srcBase == null ? &sourceByte : srcBase+sourceIndex, sourceCount, // srcBase is null when source is zero bytes
                    destBase+destinationIndex, destination.Length-destinationIndex, flush);
    }
  }

  /// <inheritdoc/>
  [CLSCompliant(false)]
  public override unsafe int GetByteCount(byte* data, int count, bool simulateFlush)
  {
    if(data == null) throw new ArgumentNullException();
    if(count < 0) throw new ArgumentOutOfRangeException();

    int byteCount = 0;
    // copy all the state into variables so we can simulate the operation without actually changing the decoder state
    uint buffer = bitBuffer;
    int bits = bitsInBuffer, bytesInDictionary = this.bytesInDictionary, bytesToCopy = 0;
    int dictionarySize = dictionary == null ? 0 : dictionary.Length, lowerBitCount = this.lowerBitCount;
    State state = this.state;
    bool fixedLengthLiterals = this.fixedLengthLiterals;

    byte* dataEnd = data + count;

    while(bits <= 24 && data != dataEnd) // copy as much into the bit buffer as we can
    {
      buffer |= (uint)*data++ << bits;
      bits += 8;
    }

    if(state == State.Start)
    {
      if(bits < 16) return 0;
      byte literalHandling = (byte)buffer, dictionarySizeSelector = (byte)(buffer>>8);

      buffer >>= 16;
      bits -= 16;

      // validate the header
      if(literalHandling > 1 || dictionarySizeSelector < 4 || dictionarySizeSelector > 6)
      {
        throw new InvalidDataException("The header is invalid.");
      }

      fixedLengthLiterals = literalHandling == 0;
      lowerBitCount       = dictionarySizeSelector;

      // the dictionary size is 1k, 2k, or 4k depending on whether the size selector is 4, 5, or 6
      dictionarySize = 64 << dictionarySizeSelector;
      bytesInDictionary = 0;
      state = State.GotHeader;
    }

    if(state != State.End)
    {
      // at this point, we're in the GotHeader or GotLength state, which means we should read code words and write them to the output
      fixed(byte* tableBase=tables)
      {
        while(true)
        {
          while(bits <= 24 && data != dataEnd) // copy as much into the bit buffer as we can
          {
            buffer |= (uint)*data++ << bits;
            bits += 8;
          }

          if(state == State.GotLength)
          {
            // we're in the middle of copying data out of the dictionary. we know how many bytes to copy, but not the distance to them
            int distance = tableBase[(byte)buffer + (128+8+64)], bitsConsumed;
            if(distance < 3)
            {
              bitsConsumed = distance == 0 ? 2 : 4;
            }
            else
            {
              bitsConsumed = (distance>>6) + 5;
              distance &= 63;
            }

            // now that we have the upper six bits, we need the lower bits. if the copy length is 2, there are two lower order bits.
            // otherwise, there are a number of bits equal to second byte of the header
            int lowBits = bytesToCopy == 2 ? 2 : lowerBitCount;
            distance = (distance<<lowBits) | (int)((buffer>>bitsConsumed) & ((1u<<lowBits)-1));
            bitsConsumed += lowBits;

            if(bitsConsumed > bits) break; // if the read was not legitimate, wait for more data
            buffer >>= bitsConsumed;
            bits -= bitsConsumed;

            bytesInDictionary += bytesToCopy;
            if(bytesInDictionary > dictionarySize) bytesInDictionary = dictionarySize;
            byteCount += bytesToCopy;

            state = State.GotHeader;
          }
          else if((buffer & 1) != 0) // if the first bit of the code word is one, then we read data from the dictionary
          {
            int length = tableBase[(buffer>>1) & 127], bitsConsumed;
            if((length & 128) == 0)
            {
              bitsConsumed = length >> 4;
              length &= 15;
            }
            else
            {
              int codeBits = ((length>>4) & 7) + 1, extraBits = length & 15;
              length = (int)((buffer>>codeBits) & ((1u<<extraBits)-1)) + (tableBase[extraBits + (128-1)] << 1);

              if(length == 519 && bits >= 16) // if we're legitimately at the end of the stream
              {
                bits -= 16;
                state = State.End;
                break;
              }

              bitsConsumed = codeBits + extraBits;
            }

            if(bitsConsumed > bits) break; // if the read was not legitimate, wait for more data

            buffer >>= bitsConsumed;
            bits -= bitsConsumed;
            bytesToCopy = length;
            state = State.GotLength;
          }
          else // the first bit of the code was 0, so a literal byte follows
          {
            int bitsConsumed;
            if(fixedLengthLiterals) bitsConsumed = 9;
            else DecodeLiteralByte(buffer, tableBase, out bitsConsumed);

            if(bitsConsumed > bits) break; // if the read was not legitimate, wait for more data

            buffer >>= bitsConsumed;
            bits -= bitsConsumed;

            if(++bytesInDictionary == dictionarySize) bytesInDictionary = dictionarySize;
            byteCount++;
          }
        }
      }
    }

    if(simulateFlush)
    {
      if(bits > 7) throw new InvalidDataException("Unexpected bytes after the end of the data.");
      else if(state != State.End) throw new InvalidDataException("The data stream was truncated.");
    }

    return byteCount;
  }

  /// <inheritdoc/>
  public unsafe override int GetByteCount(byte[] data, int index, int count, bool simulateFlush)
  {
    Utility.ValidateRange(data, index, count);
    fixed(byte* dataPtr=data)
    {
      byte dataByte; // dataPtr is null when source is zero bytes, so provide /some/ pointer
      return GetByteCount(dataPtr == null ? &dataByte : dataPtr+index, count, simulateFlush);
    }
  }

  /// <inheritdoc/>
  public override int GetMaxBytes(int unencodedByteCount)
  {
    if(unencodedByteCount < 0) throw new ArgumentOutOfRangeException();
    // we can get 518 bytes out of every 17 bits. that's about 244 decoded bytes per input byte. don't assume the data contains the header
    // or footer, because this method may be called for portions of data in the middle of the stream
    unencodedByteCount += (bitsInBuffer+7) / 8;
    return unencodedByteCount > 8801162 ? int.MaxValue : unencodedByteCount * 244; // above 8801162 input bytes, it would overflow
  }

  /// <inheritdoc/>
  public override void Reset()
  {
    bitsInBuffer = 0;
    bitBuffer    = 0;
    state        = State.Start;
  }

  enum State
  {
    Start=0, GotHeader, GotLength, End
  }

  byte[] dictionary;
  uint bitBuffer;
  int bitsInBuffer, lowerBitCount, bytesInDictionary, bytesToCopy, writeIndex;
  State state;
  bool fixedLengthLiterals;

  static unsafe byte DecodeLiteralByte(uint buffer, byte* tableBase, out int bitsConsumed)
  {
    byte value = tableBase[((buffer>>1) & 63) + (128+8)];
    if(value != 0)
    {
      bitsConsumed = (value & 0x80) != 0 ? 7 : value == 32 ? 5 : 6;
      value &= 0x7F;
    }
    else
    {
      int prefixBits, extraBits, offset;
      if((buffer & 4) != 0 || (buffer & 8) != 0 || (byte)buffer == 0xF0) // 0xxxxxx representing 0-20
      {                                                                  // (the first 15 and last 28 values are missing)
        prefixBits = 2;
        extraBits  = 6;
        offset     = 0 - 15;
      }
      else if((buffer & 16) != 0 || (byte)buffer == 0xE0) // 000xxxxx representing 21-36 (the first 14 and last 2 values are missing)
      {
        prefixBits = 4;
        extraBits  = 5;
        offset     = 21 - 14;
      }
      else if((buffer & 32) != 0) // 00001* representing 37-58
      {
        if((buffer & 64) != 0 || (buffer & 128) != 0 && (buffer & 0x300) != 0) // 00001xxxx representing 37-43
        {                                                                      // (the first 5 and last 4 values are missing)
          prefixBits = 6;
          extraBits  = 4;
          offset     = 37 - 5; // account for the missing first five values
        }
        else if((buffer & 128) != 0 || (buffer & 256) !=0 && (buffer & 0x600) != 0) // 000010xxxx representing 44-48
        {                                                                           // (first 5 and last 6 values are missing)
          prefixBits = 7;
          extraBits  = 4;
          offset     = 44 - 5; // account for the missing first five values
        }
        else // 0000100xxxx representing 49-58
        {
          prefixBits = 8;
          extraBits  = 4;
          offset     = 49;
        }
      }
      else if((buffer & 64) != 0 || // 00000xxxxxxx representing 59-149 (the first 37 values are missing)
              (buffer & 0x8080) == 0x80 && ((buffer & 0x300) != 0 || (buffer & 0x400) != 0 && (buffer & 0x1800) != 0))
      {
        prefixBits = 6;
        extraBits  = 7;
        offset     = 59 - 37;
      }
      else // 000000xxxxxxx representing 150-223 (the last 54 values are missing)
      {
        prefixBits = 7;
        extraBits  = 7;
        offset     = 150;
      }

      // take the value from the buffer and reverse it
      buffer = (buffer>>prefixBits) & ((1u<<extraBits)-1);
      int reversed = 0;
      for(int i=0; i<extraBits; i++) reversed = (reversed<<1) | (int)((buffer>>i)&1);

      // then use it to index into the table
      value = tableBase[reversed + offset + (128+8+64+256)];
      bitsConsumed = prefixBits + extraBits;
    }

    return value;
  }

  static readonly byte[] tables = new byte[128+8+64+256+224]
  {
    // the first 128 bytes use the low 7 bits of the bit buffer to help us find the length. if the byte in the table has the high bit
    // clear, then the upper nibble is the number of bits consumed (including the leading code bit) and the lower nibble is the length.
    // otherwise, if the high bit is set, then the upper nibble (minus the high bit) is the number of bits consumed for the prefix
    // (excluding the leading code bit) and the lower nibble is the number of additional bits that we must read to obtain the length.
    // the length must then be added to the beginning of the range which is found by indexing into the next table with the lower nibble
    248, 68, 87, 51, 122, 66, 69, 51, 211, 68, 86, 51, 88, 66, 69, 51, 229, 68, 87, 51, 105, 66, 69, 51, 210, 68, 86, 51, 88, 66, 69, 51,
    230, 68, 87, 51, 123, 66, 69, 51, 211, 68, 86, 51, 88, 66, 69, 51, 228, 68, 87, 51, 105, 66, 69, 51, 210, 68, 86, 51, 88, 66, 69, 51,
    247, 68, 87, 51, 122, 66, 69, 51, 211, 68, 86, 51, 88, 66, 69, 51, 229, 68, 87, 51, 105, 66, 69, 51, 210, 68, 86, 51, 88, 66, 69, 51,
    230, 68, 87, 51, 123, 66, 69, 51, 211, 68, 86, 51, 88, 66, 69, 51, 228, 68, 87, 51, 105, 66, 69, 51, 210, 68, 86, 51, 88, 66, 69, 51,
    // the next 8 bytes are the beginnings of the ranges specified by the number of extra bits (minus 1) from the previous table,
    // divided by 2 to make them fit within a byte
    0, 6, 8, 12, 20, 36, 68, 132,
    // the next 64 bytes help us find the literal byte values decoded by the low 6 bits of the bit buffer, or else are 0 if more bits are
    // required. if not zero, the high bit indicates whether 5 or 6 bits were consumed (with the special case that if the value is 32, only
    // 4 bits were consumed)
    0, 201, 0, 110, 0, 116, 227, 97, 0, 177, 232, 105, 0, 114, 210, 32, 0, 195, 240, 108, 0, 115, 212, 69, 0, 117, 230, 101, 0, 111, 206,
    32, 0, 196, 0, 110, 0, 116, 226, 97, 0, 173, 231, 105, 0, 114, 207, 32, 0, 193, 237, 108, 0, 115, 211, 69, 0, 117, 228, 101, 0, 111,
    204, 32,
    // the next 256 bytes convert the last byte of the bit buffer into the upper six bits of the corresponding distance. if the distance
    // is less than 3, the upper two bits are the number of bits consumed minus 5.
    255, 6, 151, 0, 167, 2, 78, 0, 175, 4, 82, 0, 159, 1, 74, 0, 247, 5, 84, 0, 163, 2, 76, 0, 171, 3, 80, 0, 155, 1, 72, 0, 251, 6, 85,
    0, 165, 2, 77, 0, 173, 4, 81, 0, 157, 1, 73, 0, 243, 5, 83, 0, 161, 2, 75, 0, 169, 3, 79, 0, 153, 1, 71, 0, 253, 6, 150, 0, 166, 2,
    78, 0, 174, 4, 82, 0, 158, 1, 74, 0, 245, 5, 84, 0, 162, 2, 76, 0, 170, 3, 80, 0, 154, 1, 72, 0, 249, 6, 85, 0, 164, 2, 77, 0, 172,
    4, 81, 0, 156, 1, 73, 0, 241, 5, 83, 0, 160, 2, 75, 0, 168, 3, 79, 0, 152, 1, 71, 0, 254, 6, 151, 0, 167, 2, 78, 0, 175, 4, 82, 0,
    159, 1, 74, 0, 246, 5, 84, 0, 163, 2, 76, 0, 171, 3, 80, 0, 155, 1, 72, 0, 250, 6, 85, 0, 165, 2, 77, 0, 173, 4, 81, 0, 157, 1, 73,
    0, 242, 5, 83, 0, 161, 2, 75, 0, 169, 3, 79, 0, 153, 1, 71, 0, 252, 6, 150, 0, 166, 2, 78, 0, 174, 4, 82, 0, 158, 1, 74, 0, 244, 5,
    84, 0, 162, 2, 76, 0, 170, 3, 80, 0, 154, 1, 72, 0, 248, 6, 85, 0, 164, 2, 77, 0, 172, 4, 81, 0, 156, 1, 73, 0, 240, 5, 83, 0, 160,
    2, 75, 0, 168, 3, 79, 0, 152, 1, 71, 0,
    // the next 224 bytes are the literal values that need more than 6 bits to encode, when they're encoded with a variable length
    0x77, 0x6b, 0x55, 0x50, 0x4d, 0x46, 0x42, 0x3d, 0x38, 0x37, 0x35, 0x34, 0x33, 0x32, 0x30, 0x2e, 0x2c, 0x29, 0x28, 0x0d, 0x0a, 0x79,
    0x78, 0x76, 0x5f, 0x5b, 0x57, 0x48, 0x47, 0x3a, 0x39, 0x36, 0x2f, 0x2a, 0x27, 0x22, 0x09, 0x5d, 0x59, 0x58, 0x56, 0x4b, 0x3e, 0x2b,
    0x7a, 0x71, 0x26, 0x24, 0x21, 0x7c, 0x7b, 0x6a, 0x5c, 0x5a, 0x51, 0x4a, 0x3f, 0x3c, 0x00, 0xf4, 0xf3, 0xf2, 0xee, 0xe9, 0xe5, 0xe1,
    0xdf, 0xde, 0xdd, 0xdc, 0xdb, 0xda, 0xd9, 0xd8, 0xd7, 0xd6, 0xd5, 0xd4, 0xd3, 0xd2, 0xd1, 0xd0, 0xcf, 0xce, 0xcd, 0xcc, 0xcb, 0xca,
    0xc9, 0xc8, 0xc7, 0xc6, 0xc5, 0xc4, 0xc3, 0xc2, 0xc1, 0xc0, 0xbf, 0xbe, 0xbd, 0xbc, 0xbb, 0xba, 0xb9, 0xb8, 0xb7, 0xb6, 0xb5, 0xb4,
    0xb3, 0xb2, 0xb1, 0xb0, 0x7f, 0x7e, 0x7d, 0x60, 0x5e, 0x40, 0x3b, 0x25, 0x23, 0x1f, 0x1e, 0x1d, 0x1c, 0x1b, 0x19, 0x18, 0x17, 0x16,
    0x15, 0x14, 0x13, 0x12, 0x11, 0x10, 0x0f, 0x0e, 0x0c, 0x0b, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0xff, 0xfe, 0xfd, 0xfc,
    0xfb, 0xfa, 0xf9, 0xf8, 0xf7, 0xf6, 0xf5, 0xf1, 0xf0, 0xef, 0xed, 0xec, 0xeb, 0xea, 0xe8, 0xe7, 0xe6, 0xe4, 0xe3, 0xe2, 0xe0, 0xaf,
    0xae, 0xad, 0xac, 0xab, 0xaa, 0xa9, 0xa8, 0xa7, 0xa6, 0xa5, 0xa4, 0xa3, 0xa2, 0xa1, 0xa0, 0x9f, 0x9e, 0x9d, 0x9c, 0x9b, 0x9a, 0x99,
    0x98, 0x97, 0x96, 0x95, 0x94, 0x93, 0x92, 0x91, 0x90, 0x8f, 0x8e, 0x8d, 0x8c, 0x8b, 0x8a, 0x89, 0x88, 0x87, 0x86, 0x85, 0x84, 0x83,
    0x82, 0x81, 0x80, 0x1a,
  };
}
#endregion

#region PKWareDCLEncoding
/// <summary>Implements a <see cref="BinaryEncoding"/> that can compress (encode) and decompress (decode) data using the PKWare Data
/// Compression Library format.
/// </summary>
public sealed class PKWareDCLEncoding : EncoderDecoderBinaryEncoding
{
  /// <summary>Initializes a new <see cref="PKWareDCLEncoding"/> with a 4k dictionary size.</summary>
  public PKWareDCLEncoding() : this(0) { }
  /// <summary>Initializes a new <see cref="PKWareDCLEncoding"/> with the given size, which must be 1024, 2048, 4096, or 0 (indicating a
  /// default size).
  /// </summary>
  /// <param name="dictionarySize"></param>
  public PKWareDCLEncoding(int dictionarySize) : base(new PKWareDCLCompressor(dictionarySize), new PKWareDCLDecompressor()) { }
}
#endregion

#region PKWareDCLStream
/// <summary>Implements a <see cref="Stream"/> that compresses or decompresses data using the PKWare Data Compression Library format.</summary>
public sealed class PKWareDCLStream : EncodedStream
{
  /// <summary>Initializes a new <see cref="PKWareDCLStream"/> given the base stream containing the data to decompress. The underlying
  /// stream will be closed when this stream is closed.
  /// </summary>
  public PKWareDCLStream(Stream baseStream, CompressionMode mode) : this(baseStream, mode, true) { }

  /// <summary>Initializes a new <see cref="PKWareDCLStream"/> given the base stream containing the data to decompress. If
  /// <paramref name="ownStream"/> is true, the underlying stream will be closed when this stream is closed.
  /// </summary>
  public PKWareDCLStream(Stream baseStream, CompressionMode mode, bool ownStream)
    : base(baseStream, mode == CompressionMode.Decompress ? new PKWareDCLDecompressor() : null,
           mode == CompressionMode.Compress ? new PKWareDCLCompressor() : null, true, ownStream) { }
}
#endregion

} // namespace AdamMil.IO.Compression