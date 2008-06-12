/*
AdamMil.UI is a library that provides useful user interface controls for the
.NET framework.

http://www.adammil.net/
Copyright (C) 2008 Adam Milazzo

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
using System.Text;

namespace AdamMil.UI.TextEditing
{

#region LineStorage
/// <summary>Provides the data structure to convert character indices to and from line and column numbers in a text
/// document.
/// </summary>
public abstract class LineStorage
{
  /// <include file="documentation.xml" path="/UI/LineStorage/LineCount/*"/>
  public abstract int LineCount { get; }

  /// <include file="documentation.xml" path="/UI/LineStorage/CharToLine2/*"/>
  public abstract void CharToLine(int charIndex, out int line, out int column);

  /// <include file="documentation.xml" path="/UI/LineStorage/CharToLine/*"/>
  public virtual int CharToLine(int charIndex)
  {
    int line, column;
    CharToLine(charIndex, out line, out column);
    return line;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineInfo/*"/>
  public abstract void GetLineInfo(int line, out int offset, out int length);

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLength/*"/>
  public abstract int GetLineLength(int line);

  /// <include file="documentation.xml" path="/UI/LineStorage/AddLine/*"/>
  public void AddLine(int length)
  {
    InsertLine(LineCount, length);
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/AlterLength/*"/>
  public abstract void AlterLength(int line, int lengthDelta);

  /// <include file="documentation.xml" path="/UI/LineStorage/Clear/*"/>
  public void Clear()
  {
    SetAllLengths(new int[0]);
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/InsertLine/*"/>
  public abstract void InsertLine(int line, int length);

  /// <include file="documentation.xml" path="/UI/LineStorage/DeleteLine/*"/>
  public abstract void DeleteLine(int line);

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLengths/*"/>
  public virtual int[] GetLineLengths()
  {
    int[] lengths = new int[LineCount];
    for(int i=0; i<lengths.Length; i++) lengths[i] = GetLineLength(i);
    return lengths;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/SetAllLengths/*"/>
  public abstract void SetAllLengths(int[] lineLengths);

  /// <include file="documentation.xml" path="/UI/LineStorage/SetLength/*"/>
  public virtual void SetLength(int line, int newLength)
  {
    AlterLength(line, newLength - GetLineLength(line));
  }

  /// <summary>Returns a comma-separated list of line lengths.</summary>
  public override string ToString()
  {
    StringBuilder sb = new StringBuilder();
    for(int i=0; i<LineCount; i++)
    {
      if(sb.Length != 0) sb.Append(", ");
      sb.Append(GetLineLength(i));
    }
    return sb.ToString();
  }

  /// <include file="documentation.xml" path="/UI/Common/TrimExcess/*"/>
  public abstract void TrimExcess();

  /// <summary>Given an array of line lengths, validates the line lengths.</summary>
  protected void ValidateLineLengths(int[] lineLengths)
  {
    if(lineLengths == null) throw new ArgumentNullException();
    for(int i=0; i<lineLengths.Length; i++)
    {
      if(lineLengths[i] < 0) throw new ArgumentOutOfRangeException("Line lengths cannot be negative.");
      if(lineLengths[i] == 0 && i != lineLengths.Length-1)
      {
        throw new ArgumentOutOfRangeException("Zero-length lines are not supported (except at the final position).");
      }
    }
  }
}
#endregion

#region ArrayLineStorage
/// <summary>Implements a line storage structure that holds the line lengths in a simple array.</summary>
/// <remarks>The main benefits of the array-based line storage structure are its simplicity and its fast updating.
/// However, it is slow to convert line numbers to and from character indexes. This makes the
/// <see cref="TreeLineStorage"/> a better all-around data structure. The array-based line storage is primarily
/// used as a reference implementation to test the correctness of other line storage structures.
/// </remarks>
public sealed class ArrayLineStorage : LineStorage
{
  /// <summary>Initializes a new <see cref="ArrayLineStorage"/>.</summary>
  public ArrayLineStorage()
  {
    lineLengths = new int[0];
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/LineCount/*"/>
  public override int LineCount
  {
    get { return count; }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/CharToLine2/*"/>
  public override void CharToLine(int charIndex, out int line, out int column)
  {
    if(charIndex < 0) throw new ArgumentOutOfRangeException();

    for(int i=0; i<count; i++)
    {
      if(charIndex < lineLengths[i])
      {
        line   = i;
        column = charIndex;
        return;
      }

      charIndex -= lineLengths[i];
    }

    if(charIndex > 0) throw new ArgumentOutOfRangeException();

    line = count-1;
    column = lineLengths[line];
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineInfo/*"/>
  public override void GetLineInfo(int line, out int offset, out int length)
  {
    if(line < 0 || line >= count) throw new ArgumentOutOfRangeException();

    offset = 0;
    for(int i=0; i<line; i++) offset += lineLengths[i];
    length = lineLengths[line];
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLength/*"/>
  public override int GetLineLength(int line)
  {
    if(line < 0 || line >= count) throw new ArgumentOutOfRangeException();
    return lineLengths[line];
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLengths/*"/>
  public override int[] GetLineLengths()
  {
    int[] lengths = new int[LineCount];
    Array.Copy(lineLengths, lengths, LineCount);
    return lengths;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/AlterLength/*"/>
  public override void AlterLength(int line, int lengthDelta)
  {
    if(line < 0 || line >= count || lineLengths[line]+lengthDelta < 0) throw new ArgumentOutOfRangeException();
    lineLengths[line] += lengthDelta;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/InsertLine/*"/>
  public override void InsertLine(int line, int length)
  {
    if(line < 0 || line > count || length < 0) throw new ArgumentOutOfRangeException();

    if(count == lineLengths.Length)
    {
      int[] newArray = new int[count == 0 ? 16 : count*2];
      Array.Copy(lineLengths, newArray, count);
      lineLengths = newArray;
    }

    Array.Copy(lineLengths, line, lineLengths, line+1, count-line);
    lineLengths[line] = length;
    count++;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/DeleteLine/*"/>
  public override void DeleteLine(int line)
  {
    if(line < 0 || line >= count) throw new ArgumentOutOfRangeException();
    Array.Copy(lineLengths, line+1, lineLengths, line, count-line-1);
    count--;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/SetAllLengths/*"/>
  public override void SetAllLengths(int[] lineLengths)
  {
    ValidateLineLengths(lineLengths);
    this.lineLengths = (int[])lineLengths.Clone();
    count = lineLengths.Length;
  }

  /// <include file="documentation.xml" path="/UI/Common/TrimExcess/*"/>
  public override void TrimExcess()
  {
    int capacity = 16;
    while(capacity < count) capacity *= 2;
    
    if(capacity < lineLengths.Length)
    {
      int[] newLengths = new int[capacity];
      Array.Copy(newLengths, lineLengths, count);
      lineLengths = newLengths;
    }
  }

  int[] lineLengths;
  int count;
}
#endregion

#region TreeLineStorage
/// <summary>Implements a line storage structure that stores the line lengths in a binary search tree.</summary>
/// <remarks>The main benefit of the tree line storage are its fast query capabilities, although it is slower to update
/// than the <see cref="ArrayLineStorage"/>. It is a better all-around data structure.
/// </remarks>
public sealed class TreeLineStorage : LineStorage
{
  /// <summary>Initializes a new <see cref="TreeLineStorage"/>.</summary>
  public TreeLineStorage()
  {
    SetAllLengths(new int[0]);
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/LineCount/*"/>
  public override int LineCount
  {
    get { return count; }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/CharToLine2/*"/>
  public override void CharToLine(int charIndex, out int line, out int column)
  {
    if(charIndex < 0 || charIndex > array[0]) throw new ArgumentOutOfRangeException();

    int index;
    if(charIndex == array[0])
    {
      // if the character index is equal to the total length, then strictly speaking it's out of bounds, but it's more
      // friendly to consider it to be at the end of the last line. we have a special case for that because the
      // algorithm below fails due to the index being out of bounds
      if(count == 0) throw new ArgumentOutOfRangeException(); // but if there are no lines at all, we can't put it at
                                                              // the end of the last line, so it's really out of bounds
      GetLineInfo(count-1, out index, out line);
      line   = count-1;
      column = charIndex - index;
    }
    else
    {
      // we can convert a character to line index by starting near the root and traversing the tree downwards. the two
      // nodes under the root (starting with index 1) store the total lengths of the left and right halfs of the lines.
      // if the character index is less than the the left node, then it lies within the left side and we examine the
      // children of the left node and repeat. if instead, the character index was not less than the left node, then we
      // know it's in the right half of the lines, and we can subtract the value of the left node to get the offset
      // into the right side. we then examine the children of the right node and repeat.
      index = 1; // start by examining the left child of the root
      while(index < array.Length) // loop until the index is no longer valid -- ie, we've gone past all leaf nodes.
      {
        int value = array[index];
        if(charIndex < value)
        {
          index = index*2+1; // move to the left child
        }
        else
        {
          charIndex -= value;
          index = index*2+3; // move to the left child of the right child
        }
      }

      line   = GetLineIndex(index/2);
      column = charIndex;
    }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineInfo/*"/>
  public override void GetLineInfo(int line, out int offset, out int length)
  {
    if(line < 0 || line >= count) throw new ArgumentOutOfRangeException();

    int nodeIndex = GetLeafIndex(line); // get the leaf node that holds the line
    length = array[nodeIndex];          // the length of the line is stored there.

    offset = 0;
    
    // we can use the bitwise structure of the node index to tell us which nodes to add to generate the offset.
    // imagine a tree containing information about 8 lines (numbered 0 through 7). then there are interior nodes
    // containing the sums (0+1), (2+3), (4+5), (6+7), (0+1+2+3), and (4+5+6+7), where those numbers represent line
    // numbers. the indices of the leaf nodes range from 7 (for line 0) to 14 (for line 7). consider the leaf index for
    // line number 6. we can determine the offset by adding the nodes containing (0+1+2+3) plus (4+5). the binary
    // representation of the leaf indices tell us which direction to go when we traverse the tree. line number 6's node
    // index is 13. in binary, that's 1101. we'll do the following: if the least significant bit is zero, then we
    // subtract one from the index and add the offset of the node there. in either case, we'll then shift it to the
    // right. we'll repeat until the index is zero. for line number six with index 13, this works as follows: 13 is
    // 1101. we shift. 110. the LSB is zero, so we subtract one yielding 101. this is 5, the index of the node storing
    // (4+5), so we add that to the offset. we shift and get 10. we subtract one yielding 1, the index of the node
    // storing (0+1+2+3). we add that and shift again, producing zero, so we're done. the offset is (0+1+2+3) + (4+5),
    // which is correct.
    if(line != 0)
    {
      do
      {
        if((nodeIndex & 1) == 0)
        {
          nodeIndex--;
          offset += array[nodeIndex];
        }
        nodeIndex >>= 1;
      } while(nodeIndex != 0);
    }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLength/*"/>
  public override int GetLineLength(int line)
  {
    if(line < 0 || line >= count) throw new ArgumentOutOfRangeException();
    return array[GetLeafIndex(line)];
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLengths/*"/>
  public override int[] GetLineLengths()
  {
    int[] lengths = new int[LineCount];
    if(LineCount != 0) Array.Copy(array, GetLeafIndex(0), lengths, 0, LineCount);
    return lengths;
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/AlterLength/*"/>
  public override void AlterLength(int line, int lengthDelta)
  {
    if(line < 0 || line > count) throw new ArgumentOutOfRangeException();

    int nodeIndex = GetLeafIndex(line), newLength = array[nodeIndex]+lengthDelta;
    if(newLength < 0) throw new ArgumentOutOfRangeException();

    if(array[nodeIndex] != newLength) // if the new length is different
    {
      array[nodeIndex] = newLength;   // set the new length
      while(nodeIndex != 0)           // and recalculate the nodes up the tree to the root
      {
        nodeIndex = GetParent(nodeIndex);
        RecalculateNode(nodeIndex);
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/InsertLine/*"/>
  public override void InsertLine(int line, int length)
  {
    if(line < 0 || line > count || length < 0) throw new ArgumentOutOfRangeException();

    int leafIndex = GetLeafIndex(line);

    if(count == maxLines) // if there isn't enough space to insert the new line, we need to rebuild the whole tree
    {
      int[] newLengths = new int[count+1];
      Array.Copy(array, GetLeafIndex(0), newLengths, 0, line);      // copy the line lengths up to the new line
      newLengths[line] = length;                                    // add the new line length
      Array.Copy(array, leafIndex, newLengths, line+1, count-line); // copy the line lengths after the new line
      SetAllLengths(newLengths);                                    // build a new tree given those line lengths
    }
    else
    {
      Array.Copy(array, leafIndex, array, leafIndex+1, count-line); // make space for the new line length
      array[leafIndex] = length;                                    // add the new line
      FixupTree(leafIndex);                                         // fix up the interior nodes
      count++;
    }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/DeleteLine/*"/>
  public override void DeleteLine(int line)
  {
    if(line < 0 || line >= count) throw new ArgumentOutOfRangeException();

    // TODO: if the tree shrinks significantly (ie, to 1/8th of the maximum capacity), it may be best to resize and
    // rebuild the tree at this point to speed up future traversal operations

    int leafIndex = GetLeafIndex(line);
    Array.Copy(array, leafIndex+1, array, leafIndex, count-line-1); // shift over all the other lengths
    array[GetLeafIndex(--count)] = 0;                               // set the length of the previously-last leaf to 0
    FixupTree(leafIndex);                                           // fix up the interior nodes
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/SetAllLengths/*"/>
  public override void SetAllLengths(int[] lineLengths)
  {
    ValidateLineLengths(lineLengths);

    // we'll build a tree where the root node is the total length of all lines, the left child is the total length
    // of the first half of the lines, and the right child is the total length of the second half of the lines. the
    // subdivision continues until we have the individual line lengths on the bottom layer.

    // the bottom layer of the tree contains a power of 2 number of leaves. the line lengths are all stored in the
    // bottom layer, so we need to find a power of 2 at least as big as the number of lines. that power of 2 is the
    // maximum number of lines that can be stored in the tree
    count    = lineLengths.Length;
    maxLines = 1;
    while(maxLines < count) maxLines *= 2;

    // fill in the bottom layer of the tree with the line lengths. the bottom layer starts at index array.Length/2
    array = new int[maxLines*2-1];
    for(int i=0, j=array.Length/2; i<lineLengths.Length; j++, i++) array[j] = lineLengths[i];

    FixupTree(GetLeafIndex(0)); // calculate the rest of the tree
  }

  /// <include file="documentation.xml" path="/UI/Common/TrimExcess/*"/>
  public override void TrimExcess()
  {
    if(count <= maxLines/2)
    {
      int[] lines = new int[count];
      for(int i=0; i<lines.Length; i++) lines[i] = array[maxLines+i];
      SetAllLengths(lines);
    }
  }

  /// <summary>Fixes the interior nodes of the tree, assuming that all leaves from <paramref name="nodeIndex"/> to the
  /// end have changed.
  /// </summary>
  void FixupTree(int nodeIndex)
  {
    if(nodeIndex == 0) return; // if we're already at the root, there's no work to do. (this simplifies the logic below)

    int layerEnd = maxLines-1;
    do
    {
      nodeIndex = GetParent(nodeIndex); // move to the previous layer in the tree
      for(int i=nodeIndex; i<layerEnd; i++) RecalculateNode(i); // recalculate the rest of the nodes in this layer
      layerEnd /= 2; // calculate the end of the next layer
    } while(layerEnd != 0);
  }

  /// <summary>Converts the given line number, which must be within bounds, to the index of the leaf which stores
  /// its length.
  /// </summary>
  int GetLeafIndex(int line)
  {
    return line + maxLines - 1;
  }

  /// <summary>Converts the given leaf index to its corresponding line number.</summary>
  int GetLineIndex(int leafIndex)
  {
    return leafIndex - maxLines + 1;
  }

  /// <summary>Given an node index, recalculates the node's value based on its two children. No other nodes are
  /// visited or updated.
  /// </summary>
  void RecalculateNode(int nodeIndex)
  {
    array[nodeIndex] = array[nodeIndex*2+1] + array[nodeIndex*2+2];
  }

  /// <summary>An array that contains a binary tree.</summary>
  int[] array;
  /// <summary>The number of lines currently stored in the tree.</summary>
  int count;
  /// <summary>The maximum number of lines that can be stored in the tree before it needs to be resized.</summary>
  int maxLines;

  /// <summary>Given a node index other than the root, returns the index of its parent node.</summary>
  static int GetParent(int nodeIndex)
  {
    return (nodeIndex-1) / 2;
  }
}
#endregion

#region EditableTextBuffer
/// <summary>A base class for text buffers designed for interactive text editing.</summary>
public abstract class EditableTextBuffer
{
  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Indexer/*"/>
  public abstract char this[int index] { get; set; }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Capacity/*"/>
  public abstract int Capacity { get; set; }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Length/*"/>
  public abstract int Length { get; }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/CopyTo2/*"/>
  public void CopyTo(char[] destArray, int destIndex)
  {
    CopyTo(0, destArray, destIndex, Length);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/CopyTo4/*"/>
  public abstract void CopyTo(int srcIndex, char[] destArray, int destIndex, int count);

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/CopyTo3/*"/>
  public virtual void CopyTo(TextWriter writer, int srcIndex, int count)
  {
    if(srcIndex < 0 || count < 0 || srcIndex+count > Length) throw new ArgumentOutOfRangeException();
    if(writer == null) throw new ArgumentNullException();

    char[] buffer = new char[4096];
    while(count != 0)
    {
      int toCopy = Math.Min(count, buffer.Length);
      CopyTo(srcIndex, buffer, 0, toCopy);
      writer.Write(buffer, 0, toCopy);
      srcIndex += toCopy;
      count -= toCopy;
    }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Delete/*"/>
  public virtual void Delete(int index)
  {
    Delete(index, 1);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Delete2/*"/>
  public abstract void Delete(int index, int count);

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/FindNext/*"/>
  public abstract int FindNext(char c, int startIndex);

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/GetText/*"/>
  public virtual string GetText()
  {
    return GetText(0, Length);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/GetText2/*"/>
  public virtual string GetText(int index, int count)
  {
    if(count < 0) throw new ArgumentOutOfRangeException();
    char[] buffer = new char[count];
    CopyTo(index, buffer, 0, count);
    return new string(buffer);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertChar/*"/>
  public abstract void Insert(int index, char c);

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertArray/*"/>
  public abstract void Insert(int destIndex, char[] srcArray, int srcIndex, int count);

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertString/*"/>
  public abstract void Insert(int index, string str);

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertReader/*"/>
  public virtual void Insert(int index, TextReader reader)
  {
    string line;
    while(true)
    {
      line = reader.ReadLine();
      if(line == null) break;
      Insert(index, line);
      Insert(index, '\n');
      index += line.Length+1;
    }
  }

  /// <summary>Returns the text within the text buffer.</summary>
  public override string ToString()
  {
    return GetText();
  }

  /// <summary>Reduces the capacity of the text buffer to eliminate unused space.</summary>
  public void TrimExcess()
  {
    Capacity = Length;
  }
  
  /// <summary>Creates an empty editable text buffer using the default implementation.</summary>
  public static EditableTextBuffer Create() { return Create(null, 0); }
  
  /// <summary>Creates an editable text buffer using the default implementation, initialized with text from the given
  /// <see cref="TextReader"/>.
  /// </summary>
  public static EditableTextBuffer Create(TextReader initialText) { return Create(initialText, 0); }
  
  /// <summary>Creates an empty editable text buffer using the default implementation and the given capacity.</summary>
  public static EditableTextBuffer Create(int capacity) { return Create(null, capacity); }

  /// <summary>Creates an editable text buffer using the default implementation, initialized with text from the given
  /// <see cref="TextReader"/>, and using the given capacity.
  /// </summary>
  /// <param name="initialText">A <see cref="TextReader"/> containing the initial text. If null, the text buffer will
  /// be empty.
  /// </param>
  /// <param name="capacity">The initial capacity, or zero to use the default capacity.</param>
  public static EditableTextBuffer Create(TextReader initialText, int capacity)
  {
    return new GapTextBuffer(initialText, capacity);
  }
}
#endregion

#region GapTextBuffer
/// <summary>This is a text buffer implementation that works by maintaining a character array with a gap around the
/// edit point.
/// </summary>
/// <remarks>Insertions and deletions near the gap are relatively efficient, as most edits don't need to shift the
/// entire document, but only the characters near the edge of the gap. Only when the gap is completely full or the
/// edit point moves significantly does a large portion of the document need to be moved in memory.
/// </remarks>
public class GapTextBuffer : EditableTextBuffer
{
  /// <summary>The size of the gap maintained near the last insertion point.</summary>
  const int SmallGapSize = 8, BigGapSize = 256;
  /// <summary>The default capacity of the buffer, in characters.</summary>
  const int DefaultCapacity = 32;
  
  /// <summary>Initializes an empty <see cref="GapTextBuffer"/> with the default capacity.</summary>
  public GapTextBuffer() : this(null, 0) { }

  /// <summary>Initializes a <see cref="GapTextBuffer"/> with text loaded from the given <see cref="TextReader"/>,
  /// and the default capacity.
  /// </summary>
  public GapTextBuffer(TextReader reader) : this(reader, 0) { }
  
  /// <summary>Initializes a <see cref="GapTextBuffer"/> with the given capacity.</summary>
  /// <param name="capacity">The initial capacity, or zero to use the default capacity.</param>
  public GapTextBuffer(int capacity) : this(null, capacity) { }

  /// <summary>Initializes a <see cref="GapTextBuffer"/> with text loaded from the given <see cref="TextReader"/>,
  /// and with the given capacity.
  /// </summary>
  /// <param name="reader">The <see cref="TextReader"/> containing the initial text, or null to leave the buffer
  /// empty.
  /// </param>
  /// <param name="capacity">The initial capacity, or zero to use the default capacity.</param>
  public GapTextBuffer(TextReader reader, int capacity)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", "Capacity cannot be negative.");
    if(capacity == 0) capacity = DefaultCapacity;
    if(capacity < SmallGapSize) capacity = SmallGapSize;

    buffer = new char[capacity];
    gapEnd = buffer.Length;

    if(reader != null) Insert(0, reader);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Indexer/*"/>
  public override char this[int index]
  {
    get { return buffer[GetRawIndex(index)]; }
    set { buffer[GetRawIndex(index)] = value; }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Capacity/*"/>
  public override int Capacity
  {
    get { return buffer.Length; }
    set
    {
      if(value < length) throw new ArgumentOutOfRangeException("Capacity cannot be less than Length.");
      int newCapacity = Math.Max(value, length+SmallGapSize);
      if(newCapacity < Capacity)
      {
        char[] newBuffer = new char[newCapacity];
        CopyTo(0, newBuffer, 0, length);
        buffer   = newBuffer;
        gapStart = length;
        gapEnd   = newCapacity;
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Length/*"/>
  public override int Length
  {
    get { return length; }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/CopyTo4/*"/>
  public override void CopyTo(int srcIndex, char[] destArray, int destIndex, int count)
  {
    if(destArray == null) throw new ArgumentNullException();
    if(srcIndex < 0 || destIndex < 0 || count < 0 || srcIndex + count > length || destIndex + count > destArray.Length)
    {
      throw new ArgumentOutOfRangeException();
    }

    // if the source index is before the gap, copy the text up to the gap
    if(srcIndex < gapStart)
    {
      int toCopy = Math.Min(count, gapStart - srcIndex);
      Array.Copy(buffer, srcIndex, destArray, destIndex, toCopy);
      srcIndex   = gapStart;
      count     -= toCopy;
      destIndex += toCopy;
    }

    // now the source index is at or after the gap, so just copy the rest
    srcIndex += GapSize;
    Array.Copy(buffer, srcIndex, destArray, destIndex, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Delete2/*"/>
  public override void Delete(int index, int count)
  {
    if(index < 0 || count < 0 || index+count >= length) throw new ArgumentOutOfRangeException();

    if(index <= gapStart && index+count >= gapStart) // we're deleting from the edges of the gap,
    {                                                // so we can do it quickly by just updating the gap boundaries
      // the portion removed from the right side is the portion not removed from the left side
      gapEnd  += count - (gapStart - index);
      gapStart = index; // we're removing all of the left side starting from the index
    }
    else if(index < gapStart) // we're deleting an inner portion of the left side. we want to center the gap on the
    {                         // deleted area, so move the data after the deleted portion to the right side of the gap
      int leftOver = gapStart - (index+count);
      Array.Copy(buffer, index+count, buffer, gapEnd-leftOver, leftOver);
      gapStart = index;
      gapEnd  -= leftOver;
    }
    else // we're deleting an inner potion of the right side, so to center the gap on the index, we'll move the data
    {    // before it to the left side of the gap
      int leftOver = index - gapStart;
      Array.Copy(buffer, gapEnd, buffer, gapStart, leftOver);
      gapStart += leftOver;
      gapEnd    = index+count;
    }

    length -= count;
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/FindNext/*"/>
  public override int FindNext(char c, int startIndex)
  {
    int index;

    if(startIndex < gapStart) // search the portion before the gap
    {
      index = Array.IndexOf(buffer, c, startIndex, gapStart-startIndex);
      if(index != -1) return index;
    }

    startIndex += GapSize;
    return Array.IndexOf(buffer, c, startIndex, buffer.Length-startIndex); // search the portion after the gap
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertChar/*"/>
  public override void Insert(int index, char c)
  {
    if(index < 0 || index > length) throw new ArgumentOutOfRangeException();
    MoveAndResizeGap(index, 1);
    buffer[gapStart++] = c;
    length++;
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertArray/*"/>
  public override void Insert(int destIndex, char[] srcArray, int srcIndex, int count)
  {
    if(srcArray == null) throw new ArgumentNullException();
    if(destIndex < 0 || destIndex > length || srcIndex < 0 || count < 0 || srcIndex+count > srcArray.Length)
    {
      throw new ArgumentOutOfRangeException();
    }

    MoveAndResizeGap(destIndex, count);
    Array.Copy(srcArray, srcIndex, buffer, gapStart, count);
    gapStart += count;
    length   += count;
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertString/*"/>
  public override void Insert(int index, string str)
  {
    if(str == null) throw new ArgumentNullException();
    if(index < 0 || index > length) throw new ArgumentOutOfRangeException();

    MoveAndResizeGap(index, str.Length);
    str.CopyTo(0, buffer, gapStart, str.Length);
    gapStart += str.Length;
    length   += str.Length;
  }

  /// <summary>Gets the size of the gap.</summary>
  int GapSize
  {
    get { return gapEnd - gapStart; }
  }

  /// <summary>Converts a logical index into a raw index.</summary>
  int GetRawIndex(int logicalIndex)
  {
    if(logicalIndex < 0 || logicalIndex >= length) throw new ArgumentOutOfRangeException();
    return logicalIndex < gapStart ? logicalIndex : logicalIndex + GapSize;
  }

  /// <summary>Forces the gap to begin at the given logical index and be at least as big as the given size.</summary>
  void MoveAndResizeGap(int index, int size)
  {
    if(size > GapSize) // if the gap is not big enough, we can enlarge and center it in one action
    {
      // make the new gap as big as the desired size, plus some extra, under the assumption that the given
      // size is about to be consumed by an insertion
      int newGapSize = size + CalculateGapSize(length+size);
      char[] newBuffer = new char[length + newGapSize];
      CopyTo(0, newBuffer, 0, index);
      CopyTo(index, newBuffer, index + newGapSize, length-index);

      buffer   = newBuffer;
      gapStart = index;
      gapEnd   = index + newGapSize;
    }
    else if(index < gapStart) // the index is within the left portion, so move the data to the right side
    {
      int toCopy = gapStart - index;
      gapStart = index;
      gapEnd  -= toCopy;
      Array.Copy(buffer, index, buffer, gapEnd, toCopy);
    }
    else if(index > gapStart) // the index is within the right portion, so move the data to the left side
    {
      int toCopy = index - gapStart;
      Array.Copy(buffer, gapEnd, buffer, gapStart, toCopy);
      gapStart = index;
      gapEnd  += toCopy;
    }
  }

  /// <summary>The buffer that holds character data. The buffer has a gap from indices gapStart (inclusive) to gapEnd
  /// (exclusive).
  /// </summary>
  char[] buffer;
  /// <summary>The number of characters stored in <see cref="buffer"/>.</summary>
  int length;
  /// <summary>The starting index of the gap.</summary>
  int gapStart;
  /// <summary>The index just after the end of the gap.</summary>
  int gapEnd;

  /// <summary>Returns an appropriate gap size for the given text length.</summary>
  static int CalculateGapSize(int textLength)
  {
    return Math.Max(SmallGapSize, Math.Min(BigGapSize, textLength/8));
  }
}
#endregion

#region TextDocument
/// <summary>Combines an <see cref="EditableTextBuffer"/> and a <see cref="LineStorage"/> to produce a class that
/// represents a text document.
/// </summary>
public class TextDocument
{
  /// <summary>Initializes an empty <see cref="TextDocument"/> with the default storage objects.</summary>
  public TextDocument() : this(null, null, null) { }
  /// <summary>Initializes an empty <see cref="TextDocument"/> with the given storage objects.</summary>
  public TextDocument(EditableTextBuffer textBuffer, LineStorage lineStorage) : this(null, textBuffer, lineStorage) { }
  /// <summary>Initializes an <see cref="TextDocument"/> with the given initial text and the default storage objects.</summary>
  public TextDocument(TextReader initialText) : this(initialText, null, null) { }
  /// <summary>Initializes an <see cref="TextDocument"/> with the given initial text and storage objects.</summary>
  public TextDocument(TextReader initialText, EditableTextBuffer textBuffer, LineStorage lineStorage)
  {
    this.textBuffer  = textBuffer == null ? new GapTextBuffer() : textBuffer;
    this.lineStorage = lineStorage == null ? new TreeLineStorage() : lineStorage;
    if(initialText != null) Insert(Length, initialText);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Indexer/*"/>
  public char this[int index]
  { 
    get { return textBuffer[index]; }
    set
    {
      char previousChar = textBuffer[index];

      if(previousChar != value)
      {
        textBuffer[index] = value;

        if(previousChar == '\n') // if we removed a line terminator, join the two lines around it
        {
          int line = lineStorage.CharToLine(index);

          if(line != LineCount-1) // if there is a line afterwards, join the two by changing the length of the first
          {                       // and deleting the second
            lineStorage.AlterLength(line, lineStorage.GetLineLength(line+1));
            lineStorage.DeleteLine(line+1);
          }
        }
        else if(value == '\n') // we added a line terminator, so split the line containing it
        {
          int line, column, length;
          lineStorage.CharToLine(index, out line, out column);
          length = lineStorage.GetLineLength(line);

          lineStorage.SetLength(line, column+1);
          lineStorage.InsertLine(line+1, length-(column+1));
        }
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Length/*"/>
  public int Length
  {
    get { return textBuffer.Length; }
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/Length/*"/>
  public int LineCount
  {
    get { return lineStorage.LineCount; }
  }

  /// <summary>Removes all text from the text document.</summary>
  public void Clear()
  {
    textBuffer.Delete(0, textBuffer.Length);
    lineStorage.Clear();
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Copy3/*"/>
  public void CopyTo(TextWriter writer, int srcIndex, int count)
  {
    textBuffer.CopyTo(writer, srcIndex, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Copy4/*"/>
  public void CopyTo(int srcIndex, char[] destArray, int destIndex, int count)
  {
    textBuffer.CopyTo(srcIndex, destArray, destIndex, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Delete/*"/>
  public void Delete(int index)
  {
    char c = textBuffer[index];
    textBuffer.Delete(index);

    int line = lineStorage.CharToLine(index);
    if(c == '\n' && line != LineCount-1) // if we're deleting a newline character, join the two lines around it
    {                                    // (assuming there are two lines to join)
      lineStorage.AlterLength(line, lineStorage.GetLineLength(line+1) - 1); // -1 because the newline is removed
      lineStorage.DeleteLine(line+1);
    }
    else // we're deleting a regular character, or there are not two lines to join, so just shorten the line
    {
      lineStorage.AlterLength(line, -1);
    }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Delete2/*"/>
  public void Delete(int index, int count)
  {
    if(index < 0 || count < 0 || index+count > Length) throw new ArgumentOutOfRangeException();

    int end = index+count, newlineAt = index, line, column;
    lineStorage.CharToLine(index, out line, out column);

    // loop through all line breaks found, performing the deletions from the start point to the end of the those lines
    while(newlineAt != end)
    {
      int nextNewlineAt = textBuffer.FindNext('\n', newlineAt);
      if(nextNewlineAt >= end || nextNewlineAt == -1) break;
      
      // a newline is being deleted, so first delete the remainder of the first line, and then join the second line to
      // it and continue
      lineStorage.SetLength(line, column + lineStorage.GetLineLength(line+1));
      lineStorage.DeleteLine(line+1);

      newlineAt = nextNewlineAt+1;
    }

    lineStorage.AlterLength(line, newlineAt-end); // then delete from the start of the last line to the end point

    textBuffer.Delete(index, count); // now remove the text from the buffer
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLength/*"/>
  public int GetLineLength(int line)
  {
    return lineStorage.GetLineLength(line);
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLengths/*"/>
  public int[] GetLineLengths()
  {
    return lineStorage.GetLineLengths();
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineInfo/*"/>
  public void GetLineInfo(int line, out int offset, out int length)
  {
    lineStorage.GetLineInfo(line, out offset, out length);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertChar/*"/>
  public void Insert(int index, char c)
  {
    textBuffer.Insert(index, c);

    // if the line storage is empty, add a new, empty line to hold the character
    if(lineStorage.LineCount == 0) lineStorage.AddLine(0);

    int line, column;
    lineStorage.CharToLine(index, out line, out column);

    if(c == '\n') // if we're inserting a newline, we need to split the line it contains
    {
      int length = lineStorage.GetLineLength(line);
      lineStorage.SetLength(line, column+1);
      lineStorage.InsertLine(line+1, length-column);
    }
    else // otherwise, we're just adding a regular character, so increase the length of the line
    {
      lineStorage.AlterLength(line, 1);
    }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertArray/*"/>
  public void Insert(int destIndex, char[] srcArray, int srcIndex, int count)
  {
    // insert the text into the buffer
    textBuffer.Insert(destIndex, srcArray, srcIndex, count);

    // if the line storage is empty, add a new, empty line to hold the text
    if(lineStorage.LineCount == 0) lineStorage.AddLine(0);

    // then update the line storage with the correct lengths
    int line, column;
    lineStorage.CharToLine(destIndex, out line, out column);

    // see if there's a newline anywhere in the source buffer
    int newlineAt = Array.IndexOf(srcArray, '\n', srcIndex, count);
    if(newlineAt != -1) // if so, the line into which the text is inserted must be split
    {
      int length = lineStorage.GetLineLength(line), lengthUpToNewline = newlineAt-srcIndex+1;
      lineStorage.SetLength(line, column + lengthUpToNewline);
      lineStorage.InsertLine(++line, length-column);

      // all of the other newlines in the source buffer represent lines to be inserted as-is after the split point
      while(true)
      {
        srcIndex += lengthUpToNewline;
        count    -= lengthUpToNewline;
        newlineAt = Array.IndexOf(srcArray, '\n', srcIndex, count);
        if(newlineAt == -1) break;

        lengthUpToNewline = newlineAt-srcIndex+1;
        lineStorage.InsertLine(line++, lengthUpToNewline);
      }
    }

    // there's no newline (left) in the buffer, so just insert the text into the current line
    lineStorage.AlterLength(line, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertString/*"/>
  public void Insert(int index, string str)
  {
    if(str == null) throw new ArgumentNullException();
    char[] buffer = str.ToCharArray();
    Insert(index, buffer, 0, buffer.Length);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertReader/*"/>
  public void Insert(int index, TextReader reader)
  {
    if(index < 0 || index > Length) throw new ArgumentOutOfRangeException();
    if(reader == null) throw new ArgumentNullException();

    char[] buffer = new char[4096];
    while(true)
    {
      int read = reader.Read(buffer, 0, buffer.Length);
      if(read == 0) break;
      Insert(index, buffer, 0, read);
      index += read;
    }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/GetText/*"/>
  public string GetText()
  {
    return textBuffer.GetText();
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/GetText2/*"/>
  public string GetText(int start, int length)
  {
    return textBuffer.GetText(start, length);
  }

  /// <summary>Returns the text within the text document.</summary>
  public override string ToString()
  {
    return GetText();
  }

  /// <include file="documentation.xml" path="/UI/Common/TrimExcess/*"/>
  public void TrimExcess()
  {
    textBuffer.TrimExcess();
    lineStorage.TrimExcess();
  }

  readonly EditableTextBuffer textBuffer;
  readonly LineStorage lineStorage;
}
#endregion

} // namespace AdamMil.UI.TextEditing