/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

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
using System.Drawing;

namespace AdamMil.Mathematics.Geometry
{
  /// <summary>Implements an algorithm to pack a number of rectangles into a larger rectangle. This can be used, for
  /// instance, to pack small images into a single texture. It is not guaranteed to find the optimal packing (that
  /// problem is NP-hard), but it works quite well and very quickly. It is more efficient to add all of the rectangles
  /// at once using <see cref="TryAdd(Size[])"/>, rather than adding them individually, since that gives the algorithm
  /// more information to work with.
  /// </summary>
  public class RectanglePacker
  {
    /// <summary>Initializes a new <see cref="RectanglePacker"/> that will attempt to pack rectangles into a larger
    /// rectangle of the given dimensions.
    /// </summary>
    public RectanglePacker(int width, int height)
    {
      if(width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
      root = new Node(null, 0, 0, width, height);
    }

    #region SizeComparer
    /// <summary>A comparer that can be used to sort <see cref="Size"/> objects prior to passing them to
    /// <see cref="TryAdd(Size)"/>. This comparer is automatically used by <see cref="TryAdd(Size[])"/>.
    /// </summary>
    /// <remarks>This comparer should not be used to compare <see cref="Size"/> objects in general. It is only suitable for
    /// comparing <see cref="Size"/> objects that will be passed to <see cref="TryAdd(Size[])"/>.
    /// </remarks>
    public sealed class SizeComparer : IComparer<Size>
    {
      SizeComparer() { }

      /// <summary>Compares two sizes, ordering them first by height descending and then by width descending.</summary>
      public int Compare(Size a, Size b)
      {
        // NOTE: comparing integers by subtraction is not safe in general, because int.MinValue - int.MaxValue == 1. similarly,
        // int.MaxValue - int.MinValue = -1, even though int.MaxValue > int.MinValue. in general, it only works if the difference
        // between the two values is less than 2^31. but we validate later that all sizes are non-negative, so it's okay.

        int cmp = b.Height - a.Height;
        return cmp == 0 ? b.Width - a.Width : cmp;
      }

      /// <summary>A singleton instance of a <see cref="SizeComparer"/>.</summary>
      public static readonly SizeComparer Instance = new SizeComparer();
    }
    #endregion

    /// <summary>Gets the amount of space used within the larger rectangle. All of the smaller rectangles can be
    /// contained within a region of this size.
    /// </summary>
    public Size Size
    {
      get { return size; }
    }

    /// <summary>Gets the total size of the larger rectangle, which is equal to the dimensions passed to the constructor.</summary>
    public Size TotalSize
    {
      get { return new Size(root.Width, root.Height); }
    }

    /// <summary>Adds a rectangle of the given size, and returns point where the rectangle was placed, or null if the
    /// rectangle didn't fit.
    /// </summary>
    public Point? TryAdd(Size size)
    {
      return TryAdd(size.Width, size.Height);
    }

    /// <summary>Adds a rectangle of the given size, and returns point where the rectangle was placed, or null if the
    /// rectangle didn't fit.
    /// </summary>
    public Point? TryAdd(int width, int height)
    {
      if(width < 0 || height < 0) throw new ArgumentOutOfRangeException();

      Point? pt;
      if(width == 0 || height == 0)
      {
        pt = Point.Empty;
      }
      else
      {
        pt = root.TryAdd(width, height);
        if(pt.HasValue)
        {
          int right = pt.Value.X + width, bottom = pt.Value.Y + height;
          if(right  > size.Width) size.Width  = right;
          if(bottom > size.Height) size.Height = bottom;
        }
      }
      return pt;
    }

    /// <summary>Adds the given rectangles, and returns an array containing the points where they were added. If not all
    /// rectangles could be added, the corresponding points will be null.
    /// </summary>
    public Point?[] TryAdd(Size[] sizes)
    {
      Point?[] points;
      TryAdd(sizes, out points);
      return points;
    }

    /// <summary>Adds the given rectangles, and returns an array containing the points where they were added, and a
    /// boolean value that indicates whether all rectangles were added successfully. If not all rectangles could be
    /// added, the corresponding points will be null.
    /// </summary>
    public bool TryAdd(Size[] sizes, out Point?[] points)
    {
      ValidateSizes(sizes);
      sizes = (Size[])sizes.Clone(); // clone the array so we don't modify the original
      Array.Sort(sizes, SizeComparer.Instance);
      points = new Point?[sizes.Length];
      bool allAdded = true;
      for(int i=0; i<sizes.Length; i++)
      {
        Point? point = TryAdd(sizes[i]);
        if(!point.HasValue) allAdded = false;
        points[i] = point;
      }
      return allAdded;
    }

    static void ValidateSizes(Size[] sizes)
    {
      if(sizes == null) throw new ArgumentNullException();

      for(int i=0; i<sizes.Length; i++)
      {
        if(sizes[i].Width < 0 || sizes[i].Height < 0) throw new ArgumentOutOfRangeException();
      }
    }

    #region Node
    /// <summary>Represents a region within the larger rectangle, and its subdivision using a binary tree.</summary>
    /// <remarks>The children of a node are arranged spatially as in the following diagram. The node encompasses the
    /// entire area. The rectangle stored at the node occupies the region labeled "rect". The children consume
    /// the rest of the area, with the first child taking all the space to the right of the rectangle and the second
    /// child taking all the space below it.
    /// <code>
    /// +------+-----------+
    /// | rect | child 1   |
    /// +------+-----------+
    /// |                  |
    /// |      child 2     |
    /// |                  |
    /// +------------------+
    /// </code>
    /// </remarks>
    sealed class Node
    {
      /// <summary>Initializes a new <see cref="Node"/> with the given size.</summary>
      public Node(Node parent, int x, int y, int width, int height)
      {
        this.Parent = parent;
        this.X      = x;
        this.Y      = y;
        this.Width  = width;
        this.Height = height;
      }

      /// <summary>Attempts to add a rectangle of the given size to this node. The X and Y offsets keep track of the
      /// offset of this node from the origin.
      /// </summary>
      public Point? TryAdd(int width, int height)
      {
        if(width > this.Width || height > this.Height) return null;

        if(RectangleStored)
        {
          // if this node has a rectangle stored here already, delegate to the children
          if(Child1 != null) // try adding it to the right first
          {
            Point? pt = Child1.TryAdd(width, height);
            // as an optimization, we'll prevent degenerate subtrees (linked lists) from forming by replacing this
            // child with our grandchild if it's an only child, or removing this child if we have no grandchildren
            if(pt.HasValue && (Child1.Child1 == null || Child1.Child2 == null))
            {
              Child1 = Child1.Child1 == null ? Child1.Child2 : Child1.Child1;
              if(Child1 != null) Child1.Parent = this;
            }
            if(pt.HasValue || Child2 == null) return pt;
          }

          if(Child2 != null) // if we couldn't add it to the first child, try adding it to the second
          {
            Point? pt = Child2.TryAdd(width, height);
            if(pt.HasValue)
            {
              // prevent degenerate subtrees (linked lists) from forming (see comment above for details)
              if(Child2.Child1 == null || Child2.Child2 == null)
              {
                Child2 = Child2.Child1 == null ? Child2.Child2 : Child2.Child1;
                if(Child2 != null) Child2.Parent = this;
              }
            }
            return pt;
          }
          else return null;
        }
        else // this node does not have a rectangle stored here yet, so store it here and subdivide this space
        {
          // only add children if they'd have a non-empty area
          if(this.Width  != width) Child1 = new Node(this, X + width, Y, this.Width - width, height);
          if(this.Height != height) Child2 = new Node(this, X, Y + height, this.Width, this.Height - height);
          RectangleStored = true;
          return new Point(X, Y);
        }
      }

      public Node Child1, Child2, Parent;
      public int X, Y, Width, Height;
      public bool RectangleStored;
    }
    #endregion

    readonly Node root;
    Size size;
  }
}