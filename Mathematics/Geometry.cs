/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2019 Adam Milazzo

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
using System.Runtime.Serialization;
using AdamMil.Utilities;
using SysPoint      = System.Drawing.Point;
using SysPointF     = System.Drawing.PointF;
using SysRectangle  = System.Drawing.Rectangle;
using SysRectangleF = System.Drawing.RectangleF;

#warning Document quaternions!

namespace AdamMil.Mathematics.Geometry
{

#region MathConst
/// <summary>This class provides some useful constants for math operations.</summary>
public static class MathConst
{
  /// <summary>A value that can be used to convert degrees to radians.</summary>
  /// <remarks>If you multiply a degree value by this constant, it will be converted to radians.</remarks>
  public const double DegreesToRadians = Math.PI/180;
  /// <summary>A value that can be used to convert radians to degrees.</summary>
  /// <remarks>If you multiply a radian value by this constant, it will be converted to degrees.</remarks>
  public const double RadiansToDegrees = 180/Math.PI;
  /// <summary>The square root of 2.</summary>
  public const double Sqrt2 = 1.4142135623730950488;
  /// <summary>Half of the square root of 2, and also the sine and cosine of 45 degrees (pi/4).</summary>
  public const double HalfSqrt2 = 0.7071067811865475244;
}
#endregion

#region Math2D
/// <summary>This class contains general two-dimensional math functions, such as tests for containment and
/// intersection of various primitives, finding intersection points and areas, and rotating points.
/// </summary>
public static class Math2D
{
  /// <include file="documentation.xml" path="//Geometry/Math2D/AngleBetween/node()"/>
  public static double AngleBetween(Point2 start, Point2 end) { return (end-start).Angle; }
  /// <include file="documentation.xml" path="//Geometry/Math2D/AngleBetween/node()"/>
  public static double AngleBetween(SysPoint start, SysPoint end)
  {
    return (new Point2(end)-new Point2(start)).Angle;
  }

  /// <summary>Calculates the inverse hyperbolic cosine of the given value.</summary>
  public static double Acosh(double x)
  {
    return Math.Log(x + Math.Sqrt(x*x-1));
  }

  /// <summary>Calculates the inverse hyperbolic sine of the given value.</summary>
  public static double Asinh(double x)
  {
    return Math.Log(x + Math.Sqrt(x*x+1));
  }

  /// <summary>Calculates the inverse hyperbolic tangent of the given value.</summary>
  public static double Atanh(double x)
  {
    return Math.Log((x+1)/(x-1)) * 0.5;
  }

  #region Contains
  /// <summary>Determines if one circle fully contains another.</summary>
  public static bool Contains(ref Circle outer, ref Circle inner)
  {
    // a circle A contains a circle B if a new circle centered around the A's center, but with a radius of
    // A.Radius - B.Radius contains the B's center point.
    double xd=outer.Center.X-inner.Center.X, yd=outer.Center.Y-inner.Center.Y, radius=outer.Radius-inner.Radius;
    return xd*xd + yd*yd <= radius*radius;
  }

  /// <summary>Determines if a circle fully contains a given line segment.</summary>
  public static bool Contains(ref Circle circle, ref Line2 segment)
  {
    // a circle contains a line segment if it contains both endpoints
    if(Contains(ref circle, ref segment.Start))
    {
      Point2 endPoint = segment.End;
      return Contains(ref circle, ref endPoint);
    }
    else
    {
      return false;
    }
  }

  /// <summary>Determines if a circle contains a given point.</summary>
  public static bool Contains(ref Circle circle, ref Point2 point)
  {
    // a circle contains a point if the distance from the point to the circle's center is less than or equal to
    // the radius. we can square both sides of the comparison to eliminate the Math.Sqrt() operation on the left
    double xd=point.X-circle.Center.X, yd=point.Y-circle.Center.Y;
    return xd*xd + yd*yd <= circle.RadiusSquared;
  }

  /// <summary>Determines if a circle fully contains a given rectangle.</summary>
  public static bool Contains(ref Circle circle, ref Rectangle rect)
  {
    // a circle contains a rectangle if it contains all four corners
    Point2 point = rect.TopLeft;
    if(!Contains(ref circle, ref point)) return false;
    point.X += rect.Width; // top-right
    if(!Contains(ref circle, ref point)) return false;
    point.Y += rect.Height; // bottom-right
    if(!Contains(ref circle, ref point)) return false;
    point.X = rect.X; // bottom-left
    return Contains(ref circle, ref point);
  }

  /// <summary>Determines if a circle fully contains a given possibly-concave polygon.</summary>
  public static bool Contains(ref Circle circle, Polygon poly)
  {
    // a circle contains a polygon if it contains all the polygon's vertices
    for(int i=poly.PointCount-1; i>=0; i--)
    {
      Point2 point = poly[i];
      if(!Contains(ref circle, ref point)) return false;
    }
    return true;
  }

  /// <summary>Determines if a rectangle fully contains a given circle.</summary>
  public static bool Contains(ref Rectangle rect, ref Circle circle)
  {
    // a rectangle contains a circle if the rectangle with its width and height both shrunk by the circle's diameter
    // contains the circle's center
    return rect.X+circle.Radius <= circle.Center.X && rect.Y+circle.Radius <= circle.Center.Y &&
         rect.Right-circle.Radius >= circle.Center.X && rect.Bottom-circle.Radius >= circle.Center.Y;
  }

  /// <summary>Determines if a rectangle fully contains a given line segment.</summary>
  public static bool Contains(ref Rectangle rect, ref Line2 segment)
  {
    // a rectangle contains a line segment if it contains both endpoints
    if(Contains(ref rect, ref segment.Start))
    {
      Point2 end = segment.End;
      return Contains(ref rect, ref end);
    }
    else
    {
      return false;
    }
  }

  /// <summary>Determines if a rectangle contains a given point.</summary>
  public static bool Contains(ref Rectangle rect, ref Point2 point)
  {
    return point.X>=rect.X && point.Y>=rect.Y && point.X<=rect.Right && point.Y<=rect.Bottom;
  }

  /// <summary>Determines if one rectangle fully contains another.</summary>
  public static bool Contains(ref Rectangle outer, ref Rectangle inner)
  {
    // a rectangle contains another if it contains both corners of the other.
    Point2 point = inner.TopLeft;
    if(!Contains(ref outer, ref point)) return false;
    point = inner.BottomRight;
    return Contains(ref outer, ref point);
  }

  /// <summary>Determines if a rectangle fully contains a given possibly-concave polygon.</summary>
  public static bool Contains(ref Rectangle rect, Polygon poly)
  {
    // a rectangle contains a polygon if it contains all the polygon's vertices
    for(int i=poly.PointCount-1; i>=0; i--)
    {
      Point2 point = poly[i];
      if(!Contains(ref rect, ref point)) return false;
    }
    return true;
  }

  /// <summary>Determines if a convex polygon fully contains a given circle.</summary>
  public static bool Contains(Polygon convexPoly, ref Circle circle)
  {
    // a convex polygon contains a circle if it contains the center point and the distance from the circle's center to
    // each bounding line is greater than or equal to the circle's radius.

    if(convexPoly.PointCount < 3) return false; // degenerate polygons don't contain anything, we'll say.

    if(!Contains(convexPoly, ref circle.Center)) return false;

    double radiusSquared = circle.RadiusSquared;
    for(int i=convexPoly.PointCount; i>=0; i--)
    {
      Line2 edge = convexPoly.GetEdge(i);
      // see Intersects(Circle, Line) for the explanation of the following
      double scaledDist = edge.Vector.CrossVector.DotProduct(circle.Center - edge.Start);
      if(radiusSquared*edge.LengthSqr < scaledDist*scaledDist) return false;
    }

    return true;
  }

  /// <summary>Determines if a convex polygon fully contains a given line segment.</summary>
  public static bool Contains(Polygon convexPoly, ref Line2 line)
  {
    // a convex polygon contains a line segment if it contains both endpoints
    if(convexPoly.PointCount < 3) return false; // degenerate polygons don't contain anything, we'll say.
    if(Contains(convexPoly, ref line.Start))
    {
      Point2 end = line.End;
      return Contains(convexPoly, ref end);
    }
    else
    {
      return false;
    }
  }

  /// <summary>Determines if a convex polygon contains a given point.</summary>
  /// <remarks>The polygon can be defined clockwise or counter-clockwise, but must be a convex polygon. If the polygon
  /// is not convex, the results of this method are undefined.
  /// </remarks>
  public static bool Contains(Polygon convexPoly, ref Point2 point)
  {
    // a convex polygon contains a point if the point is on the "inside" of every bounding line. see Line.WhichSide()
    // for the meaning of "inside". we don't really know which side is the inside, because the polygon could be
    // clockwise or counter-clockwise, but if the point is contained, it will consistently be on the same side of each
    // line. if it's not contained, it will not be on the same side of each line.

    if(convexPoly.PointCount < 3) return false; // degenerate polygons don't contain anything, we'll say.

    bool pos=false, neg=false; // these variables track which sides of the lines we've seen the point on.
    for(int i=convexPoly.PointCount-1; i>=0; i--)
    {
      double side = convexPoly.GetEdge(i).WhichSide(point);
      if(side<0) // it was on the negative side.
      {
        if(pos) return false; // if it was previously on the positive side, then it's not contained.
        neg = true; // mark it as having been on the negative side
      }
      else if(side>0) // it was on the positive side.
      {
        if(neg) return false; // if it was previously on the negative side, then it's not contained.
        pos = true; // mark it as having been on the negative side.
      }
      // otherwise, it was colinear with one of the edges. we'll count that as inside.
    }

    return true; // there was no disparity in the side of the lines that the point was on, so it's contained.
  }

  /// <summary>Determines if a convex polygon fully contains a given rectangle.</summary>
  public static bool Contains(Polygon convexPoly, ref Rectangle rect)
  {
    // a convex polygon contains a rectangle if it contains each corner
    if(convexPoly.PointCount < 3) return false; // degenerate polygons don't contain anything, we'll say.

    Point2 point = rect.TopLeft;
    if(!Contains(convexPoly, ref point)) return false;
    point.X += rect.Width; // top-right
    if(!Contains(convexPoly, ref point)) return false;
    point.Y += rect.Height; // bottom-right
    if(!Contains(convexPoly, ref point)) return false;
    point.X = rect.X; // bottom-left
    return Contains(convexPoly, ref point);
  }

  /// <summary>Determines if a convex polygon fully contains a possibly-concave polygon.</summary>
  public static bool Contains(Polygon convexPoly, Polygon poly)
  {
    // a convex polygon contains another polygon if it contains all the polygon's vertices.
    if(convexPoly.PointCount < 3) return false; // degenerate polygons don't contain anything, we'll say.

    for(int i=poly.PointCount-1; i>=0; i--)
    {
      Point2 point = poly[i];
      if(!Contains(convexPoly, ref point)) return false;
    }
    return true;
  }
  #endregion

  #region Intersects
  /// <summary>Determines if two circles intersect.</summary>
  public static bool Intersects(ref Circle a, ref Circle b)
  {
    // two circles intersect if the distance between their centers is less than or equal to the sum of their radii
    double radius = a.Radius+b.Radius, xd = a.Center.X-b.Center.X, yd = a.Center.Y-b.Center.Y;
    return xd*xd+yd*yd <= radius*radius;
  }

  /// <summary>Determines if a circle intersects a line (not a line segment).</summary>
  public static bool Intersects(ref Circle circle, ref Line2 line)
  {
    // a circle intersects a line if the distance from the center to the line is less than or equal to the radius
    double scaledDist = line.Vector.CrossVector.DotProduct(circle.Center - line.Start);
    // 'scaledDist' is the distance, scaled by the length of the line segment. normally, we'd check for:
    // circle.Radius <= scaledDist / line.Length (1 mul, 1 add, 1 div, 1 sqrt). but mul both sides by line.Length:
    // circle.Radius * line.Length <= scaledDist (2 mul, 1 add, 1 sqrt). and then square both sides:
    // circle.RadiusSqr * line.LengthSqr <= scaledDist * scaledDist (4 mul, 1 add)
    return circle.RadiusSquared * line.LengthSqr <= scaledDist * scaledDist;
  }

  /// <summary>Determines if a circle intersects a line segment.</summary>
  public static bool SegmentIntersects(ref Circle circle, ref Line2 segment)
  {
    // a circle intersects a line segment if the circle contains the point on the segment closest to its center
    return circle.Contains(segment.ClosestPointOnSegment(circle.Center));
  }

  /// <summary>Determines if a circle intersects a rectangle.</summary>
  public static bool Intersects(ref Circle circle, ref Rectangle rect)
  {
    // a circle intersects a rectangle if it contains the point within the rectangle nearest the circle's center.
    // if the rectangle is fully to the right of the circle's center, then the X distance from the center to the
    // nearest point will simply be the distance between the rectangle's left edge and the circle's center. if the
    // rectangle is fully on the left, the X distance to the nearest point is the distance between the circle's
    // center and the rectangle's right edge. if neither of those are true, then the rectangle "straddles" the
    // circle's center horizontally. in that case, the X distance to the nearest rectangle point is zero, because the
    // circle definitely intersects the rectangle, at least horizontally. repeating the previous two checks
    // vertically (top/bottom instead of left/right) yields the Y distance to the nearest point within the rectangle.
    // then, we can use the simple check: Xdist^2 + Ydist^2 <= Radius^2

    double squaredDist, dist;

    if(circle.Center.X < rect.X) // if the rectangle is fully on the right side of the circle center
    {
      dist = rect.X - circle.Center.X; // the X distance to the rectangle's left edge
      squaredDist = dist * dist;       // accumulate the squared X distance
    }
    else
    {
      dist = rect.Right - circle.Center.X; // X distance to the rectangle's right edge
      if(dist < 0) // if the rectangle is fully on the left side of the circle center
      {
        squaredDist = dist * dist; // accumulate the squared X distance
      }
      else
      {
        squaredDist = 0; // the rectangle intersects the circle horizontally
      }
    }

    if(circle.Center.Y < rect.Y) // if the rectangle is fully below the circle's center
    {
      dist = rect.Y - circle.Center.Y; // the Y distance to the rectangle's top
      squaredDist += dist * dist;      // accumulate the squared Y distance
    }
    else
    {
      dist = rect.Bottom - circle.Center.Y; // Y distance to the rectangle's bottom
      if(dist < 0) // if the rectangle is fully above the circle's center
      {
        squaredDist += dist * dist; // accumulate the squared Y distance
      }
    }

    return squaredDist <= circle.RadiusSquared; // Xdist^2 + Ydist^2 <= Radius^2
  }

  /// <summary>Determines if a circle intersects a (possibly convex) polygon.</summary>
  public static bool Intersects(ref Circle circle, Polygon poly)
  {
    // a circle intersects a polygon if it intersects any of the polygon's edges or the circle's center point is within
    // the polygon.

    if(Contains(poly, ref circle.Center)) return true;

    for(int i=poly.PointCount-1; i>=0; i--)
    {
      Line2 edge = poly.GetEdge(i);
      if(SegmentIntersects(ref circle, ref edge)) return true;
    }
    return false;
  }

  /// <summary>Determines if two lines (not segments) intersect.</summary>
  /// <remarks>This method can be used to determine if two lines are parallel. If the method returns false, they are
  /// parallel.
  /// </remarks>
  public static bool Intersects(ref Line2 a, ref Line2 b)
  {
    // two lines intersect if they are not parallel. they are not parallel if they have different slopes.
    // using the standard slope formula ("rise over run"), we get vector.Y / vector.X. but this runs into
    // problems with divide-by-zero and performance, so we restructure the equation to use multiplication:
    // aY/aX != bY/bX   multiply both sides by aX and bY:
    // aY*bX != bY*aX
    return a.Vector.Y*b.Vector.X != b.Vector.Y*a.Vector.X;
  }

  /// <summary>Determines if two line segments intersect.</summary>
  public static bool SegmentIntersects(ref Line2 a, ref Line2 b)
  {
    // see SegmentIntersection(Line, Line) for the explanation.
    double denominator = b.Vector.Y*a.Vector.X - b.Vector.X*a.Vector.Y; // calculate the denominator
    if(denominator == 0) return false; // return false for parallel lines
    double xd = a.Start.X-b.Start.X, yd = a.Start.Y-b.Start.Y; // these parts are the same in both equations
    double Nv = (b.Vector.X*yd - b.Vector.Y*xd) / denominator; // calculate Na
    if(Nv<0 || Nv>1) return false; // if Na lies outside 0-1, the intersection point is not on the 'a' segment
    Nv = (a.Vector.X*yd - a.Vector.Y*xd) / denominator;        // calculate Nb
    return Nv>=0 && Nv<=1; // if Nb lies outside 0-1, the intersection point is not on the 'b' segment either
  }

  /// <summary>Returns information about the intersection of two lines, either of which can be infinite or a segment.</summary>
  /// <returns>Returns a <see cref="LineIntersection"/> structure containing information about the intersection.</returns>
  public static LineIntersection IntersectionInfo(ref Line2 a, ref Line2 b)
  {
    // see SegmentIntersection(Line, Line) for the explanation.
    double denominator = b.Vector.Y*a.Vector.X - b.Vector.X*a.Vector.Y; // calculate the denominator
    if(denominator == 0) return new LineIntersection(Point2.Invalid, false, false); // return invalid for parallel lines

    double xd = a.Start.X-b.Start.X, yd = a.Start.Y-b.Start.Y; // these parts are the same in both equations
    double Na = (b.Vector.X*yd - b.Vector.Y*xd) / denominator; // calculate Na
    double Nb = (a.Vector.X*yd - a.Vector.Y*xd) / denominator; // calculate Nb
    return new LineIntersection(new Point2(a.Start.X + a.Vector.X*Na, a.Start.Y + a.Vector.Y*Na),
                                Na>=0 && Na<=1, Nb>=0 && Nb<=1);
  }

  /// <summary>Determines if a line (not a segment) intersects a rectangle.</summary>
  public static bool Intersects(ref Line2 line, ref Rectangle rect)
  {
    // a line intersects a rectangle if it intersects any side.
    for(int i=0; i<4; i++)
    {
      Line2 edge = rect.GetEdge(i);
      if(IntersectionInfo(ref line, ref edge).OnSecond) return true;
    }
    return false;
  }

  /// <summary>Determines if a line segment intersects a rectangle.</summary>
  public static bool SegmentIntersects(ref Line2 segment, ref Rectangle rect)
  {
    return SegmentIntersection(ref segment, ref rect).IsValid;
  }

  /// <summary>Determines if a line (not a segment) intersects a possibly-concave polygon.</summary>
  public static bool Intersects(ref Line2 line, Polygon poly)
  {
    // a line intersects a polygon if it intersects any of the polygon's edges
    for(int i=poly.PointCount-1; i>=0; i--)
    {
      Line2 edge = poly.GetEdge(i);
      if(IntersectionInfo(ref line, ref edge).OnSecond) return true;
    }
    return false;
  }

  /// <summary>Determines if a line segment intersects a convex polygon.</summary>
  public static bool SegmentIntersects(ref Line2 segment, Polygon convexPoly)
  {
    // a line segment intersects a polygon if the polygon contains both endpoints, or the segment intersects any edge.

    // we only need to check one point to test for intersection
    if(Contains(convexPoly, ref segment.Start)) return true;

    for(int i=convexPoly.PointCount-1; i>=0; i--) // the polygon doesn't fully contain the segment, so test the edges
    {
      Line2 edge = convexPoly.GetEdge(i);
      if(SegmentIntersects(ref segment, ref edge)) return true;
    }
    return false;
  }

  /// <summary>Determines if two rectangles intersect.</summary>
  public static bool Intersects(ref Rectangle a, ref Rectangle b)
  {
    // two rectangles A and B intersect if A's left edge is not to B's right, A's top is not below above B's bottom,
    // A's right is not to B's left, and A's bottom is not above B's top.
    return a.X <= b.Right && a.Y <= b.Bottom && a.Right >= b.X && a.Bottom >= b.Y;
  }

  /// <summary>Determines if a rectangle intersects a convex polygon.</summary>
  public static bool Intersects(ref Rectangle rect, Polygon convexPoly)
  {
    // a rectangle intersects a convex polygon if all corners of either object are contained within the other, or any
    // edge of either object intersect any edge of the other object.

    if(convexPoly.PointCount == 0) return false;

    // if one point of either is inside the other, there's intersection.
    Point2 point = convexPoly[0];
    if(Contains(ref rect, ref point)) return true;
    point = rect.TopLeft;
    if(Contains(convexPoly, ref point)) return true;

    for(int i=0; i<4; i++) // if it's not fully contained, then at least one edge must intersect
    {
      Line2 edge = rect.GetEdge(i);
      if(SegmentIntersects(ref edge, convexPoly)) return true;
    }

    return false;
  }

  /// <summary>Determines if a convex polygon intersects another convex polygon.</summary>
  public static bool Intersects(Polygon convexA, Polygon convexB)
  {
    // two convex polygons A and B intersect if B contains all of A's vertices, or any of A's edges intersects B

    // we only need to check one vertex to determine whether A is fully contained
    Point2 point = convexA[0];
    if(Contains(convexB, ref point)) return true;

    for(int i=convexA.PointCount-1; i>=0; i--) // if it's not fully contained, then at least one edge must intersect
    {
      Line2 edge = convexA.GetEdge(i);
      if(SegmentIntersects(ref edge, convexB)) return true;
    }

    return false;
  }
  #endregion

  #region Intersection
  /// <summary>Determines if and where two lines (not segments) intersect.</summary>
  /// <remarks>If no intersection occurs, <see cref="Point2.Invalid"/> will be returned.</remarks>
  public static Point2 Intersection(ref Line2 a, ref Line2 b)
  {
    // see SegmentIntersection(Line, Line) for the explanation
    double denominator = b.Vector.Y*a.Vector.X - b.Vector.X*a.Vector.Y; // calculate the denominator
    if(denominator == 0) return Point2.Invalid; // return false for parallel lines
    double Na = (b.Vector.X*(a.Start.Y-b.Start.Y) - b.Vector.Y*(a.Start.X-b.Start.X)) / denominator; // calculate Na
    return new Point2(a.Start.X + a.Vector.X*Na, a.Start.Y + a.Vector.Y*Na);
  }

  /// <summary>Determines if and where two line segments intersect.</summary>
  /// <remarks>If no intersection occurs, <see cref="Point2.Invalid"/> will be returned.</remarks>
  public static Point2 SegmentIntersection(ref Line2 a, ref Line2 b)
  {
    // line segments are represented as starting points and vectors. any point Pn on a line segment can be
    // represented by Start + Vector*N where N is from 0 to 1. if we solve for Pa (a point on segment a) == Pb (a
    // point on segment b), we get the following two formulas:
    // a.Start.X + a.Vector.X * Na == b.Start.X + b.Vector.X * Nb and
    // a.Start.Y + a.Vector.Y * Na == b.Start.Y + b.Vector.Y * Nb
    // solving for Na and Nb produces:
    // Na = (b.Vector.X*(a.Start.Y-b.Start.Y) - b.Vector.Y*(a.Start.X-b.Start.X)) /
    //          (b.Vector.Y*a.Vector.X - b.Vector.X*a.Vector.Y)
    // Nb = (a.Vector.X*(a.Start.Y-b.Start.Y) - a.Vector.Y*(a.Start.X-b.Start.X)) /
    //          (b.Vector.Y*a.Vector.X - b.Vector.X*a.Vector.Y)
    // note that they have the same denominator and the same a.Start.X-b.Start.X and a.Start.Y-b.Start.Y parts.
    // if the denominator is zero, the lines are parallel. (See the line intersection method for an explanation of why)

    double denominator = b.Vector.Y*a.Vector.X - b.Vector.X*a.Vector.Y; // calculate the denominator
    if(denominator == 0) return Point2.Invalid; // return false for parallel lines
    double xd = a.Start.X-b.Start.X, yd = a.Start.Y-b.Start.Y; // these parts are the same in both equations
    double Nv = (a.Vector.X*yd - a.Vector.Y*xd) / denominator; // calculate Nb
    if(Nv<0 || Nv>1) return Point2.Invalid; // if Nb lies outside 0-1, the intersection point is not on the 'b' segment
    Nv = (b.Vector.X*yd - b.Vector.Y*xd) / denominator;        // calculate Na
    if(Nv<0 || Nv>1) return Point2.Invalid; // if Na lies outside 0-1, the intersection point is not on the 'a' segment
    // the intersection point can be calculated from either segment if we have the appropriate value of N.
    // since we have Na sitting in 'Nv', we'll use the 'a' vector.
    return new Point2(a.Start.X + a.Vector.X*Nv, a.Start.Y + a.Vector.Y*Nv);
  }

  /// <summary>Determines if and where a line (not segment) and a rectangle intersect.</summary>
  /// <remarks>If no intersection occurs, <see cref="Line2.Invalid"/> will be returned. If the line intersects only
  /// tangentially, a degenerate (zero-length) line will be returned.
  /// </remarks>
  public static unsafe Line2 Intersection(ref Line2 line, ref Rectangle rect)
  {
    // a line intersects a rectangle if it intersects any of the edges.
    Point2* points  = stackalloc Point2[2]; // allocate 2 points on the stack
    int pointIndex = 0;

    for(int i=0; i<4 && pointIndex<2; i++)
    {
      Line2 edge = rect.GetEdge(0);
      LineIntersection info = IntersectionInfo(ref line, ref edge);
      if(info.OnSecond) points[pointIndex++] = info.Point; // if it intersected the edge, add the point to the list
    }

    if(pointIndex == 2) // if two points intersected, return the segment between them
    {
      return new Line2(points[0], points[1]);
    }
    else if(pointIndex == 1) // if one point intersected, return a degenerate (zero-length) line segment.
    {
      return new Line2(points[0].X, points[0].Y, 0, 0);
    }
    else // if no edges intersected, return an invalid line.
    {
      return Line2.Invalid;
    }
  }

  /// <summary>Determines if and where a line (not segment) and a convex polygon intersect.</summary>
  /// <remarks>If no intersection occurs, <see cref="Line2.Invalid"/> will be returned. If the line intersects only
  /// tangentially, a degenerate (zero-length) line will be returned.
  /// </remarks>
  public static unsafe Line2 Intersection(ref Line2 line, Polygon convexPoly)
  {
    // a line intersects a convex polygon if it intersects any of the edges.
    Point2* points  = stackalloc Point2[2];
    int pointIndex = 0;

    // test the intersection of the line with each edge, or until we find two intersection points
    for(int i=convexPoly.PointCount; i>=0 && pointIndex<2; i--)
    {
      Line2 edge = convexPoly.GetEdge(i);
      LineIntersection info = IntersectionInfo(ref line, ref edge);
      if(info.OnSecond) points[pointIndex++] = info.Point; // if the intersection was on the polygon edge
    }

    if(pointIndex == 2) // if two points intersected, return the segment between them
    {
      return new Line2(points[0], points[1]);
    }
    else if(pointIndex == 1) // if one point intersected, return a degenerate (zero-length) line segment.
    {
      return new Line2(points[0].X, points[0].Y, 0, 0);
    }
    else // if no edges intersected, return an invalid line.
    {
      return Line2.Invalid;
    }
  }

  /// <summary>Determines if and where a line segment and a rectangle intersect.</summary>
  /// <remarks>If no intersection occurs, <see cref="Line2.Invalid"/> will be returned. If the line intersects only
  /// tangentially, a degenerate (zero-length) line will be returned.
  /// </remarks>
  public static Line2 SegmentIntersection(ref Line2 segment, ref Rectangle rect)
  {
    // this is the Cohen-Sutherland line-clipping algorithm

    double rectRight=rect.Right, rectBottom=rect.Bottom;
    Point2 start=segment.Start, end=segment.End; // copy the segment points so as not to modify the input value
    int startCode, endCode;

    // 'startCode' is a bitfield:
    // bit 0: segment starts above the rectangle
    // bit 1: segment starts below the rectangle
    // bit 2: segment starts to the left of the rectangle
    // bit 3: segment starts to the right of the rectangle
    // if c==0, the start point of the segment is within the rectangle
    startCode = start.Y<rect.Y ? 1 : start.Y>rectBottom ? 2 : 0;
    if(start.X < rect.X) startCode |= 4;
    else if(start.X > rectRight) startCode |= 8;

    // 'endCode' is a bitfield:
    // bit 0: segment ends above the rectangle
    // bit 1: segment ends below the rectangle
    // bit 2: segment ends to the left of the rectangle
    // bit 3: segment ends to the right of the rectangle
    // if c2==0, the end point of the segment is within the rectangle
    endCode = end.Y<rect.Y ? 1 : end.Y>rectBottom ? 2 : 0;
    if(end.X < rect.X) endCode |= 4;
    else if(end.X > rectRight) endCode |= 8;

    while(true)
    {
      // if both points are within the rectangle, return the entire line.
      if(startCode==0 && endCode==0) return new Line2(start, end);
      // if the segment can't possibly intersect the rectangle (eg, both the start and end are to the left of it),
      // return invalid.
      if((startCode&endCode) != 0) return Line2.Invalid;

      // for each endpoint, we can only clip in one direction each iteration, which is why the clipping statements use
      // "else if" clauses.

      if(startCode != 0) // if the start point is outside the rectangle, clip it to the rectangle.
      {
        if((startCode&1) != 0) // if it starts above it, interpolate to clip the line against the line colinear with the top
        {
          start.X += (rect.Y-start.Y) * segment.Vector.X / segment.Vector.Y;
          start.Y = rect.Y;
        }
        else if((startCode&2) != 0) // bottom
        {
          start.X -= (start.Y-rectBottom) * segment.Vector.X / segment.Vector.Y;
          start.Y = rectBottom;
        }
        else if((startCode&4) != 0) // left
        {
          start.Y += (rect.X-start.X) * segment.Vector.Y / segment.Vector.X;
          start.X = rect.X;
        }
        else // right
        {
          start.Y -= (start.X-rectRight) * segment.Vector.Y / segment.Vector.X;
          start.X = rectRight;
        }

        // after clipping, check again.
        startCode = start.Y<rect.Y ? 1 : start.Y>rectBottom ? 2 : 0;
        if(start.X < rect.X) startCode |= 4;
        else if(start.X > rectRight) startCode |= 8;
      }

      if(endCode != 0) // if the end point is outside the rectangle
      {
        if((endCode&1) != 0) // if it starts above it, interpolate to clip the line against the line colinear with the top
        {
          end.X += (rect.Y-end.Y) * segment.Vector.X / segment.Vector.Y;
          end.Y = rect.Y;
        }
        else if((endCode&2) != 0) // bottom.
        {
          end.X -= (end.Y-rectBottom) * segment.Vector.X / segment.Vector.Y;
          end.Y = rectBottom;
        }
        else if((endCode&4) != 0) // left
        {
          end.Y += (rect.X-end.X) * segment.Vector.Y / segment.Vector.X;
          end.X = rect.X;
        }
        else // right
        {
          end.Y -= (end.X-rectRight) * segment.Vector.Y / segment.Vector.X;
          end.X = rectRight;
        }

        endCode = end.Y<rect.Y ? 1 : end.Y>rectBottom ? 2 : 0;
        if(end.X < rect.X) endCode |= 4;
        else if(end.X > rectRight) endCode |= 8;
      }
    }
  }

  /// <summary>Determines if and where a line segment and a convex polygon intersect.</summary>
  /// <remarks>If no intersection occurs, <see cref="Line2.Invalid"/> will be returned. If the line intersects only
  /// tangentially, a degenerate (zero-length) line will be returned.
  /// </remarks>
  public static Line2 SegmentIntersection(ref Line2 segment, Polygon convexPoly)
  {
    Point2 start = segment.Start, end = segment.End; // copy the endpoints so as to not modify the input line
    int sign = convexPoly.IsClockwise() ? 1 : -1; // determine which side of the bounding lines is "outside"

    // for each polygon bounding line, clip the endpoints of the segment that lie "outside" the line to the line.
    for(int i=convexPoly.PointCount; i>=0; i--)
    {
      Line2 edge = convexPoly.GetEdge(i);
      bool startOutside = Math.Sign(edge.WhichSide(start)) == sign, // determine whether either of the endpoints are
           endOutside   = Math.Sign(edge.WhichSide(end))   == sign; // "outside" the bounding line.
      if(startOutside)
      {
        if(endOutside) return Line2.Invalid; // if both endpoints are outside the clipping line, there's no intersection
        Line2 tempSegment = new Line2(start, end); // construct a temporary line to hold the start-end segment
        // we can use infinite line intersection because at this point one endpoint is outside and one is inside,
        //so we know the segment intersects the bounding line
        start = Intersection(ref edge, ref tempSegment);
      }
      else if(endOutside)
      {
        Line2 tempSegment = new Line2(start, end); // construct a temporary line to hold the start-end segment
        // we can use infinite line intersection because at this point one endpoint is outside and one is inside,
        // so we know the segment intersects the bounding line
        end = Intersection(ref edge, ref tempSegment);
      }
    }

    return new Line2(start, end);
  }

  /// <summary>Determines if and where two rectangles intersect.</summary>
  /// <remarks>If no intersection occurs, a rectangle with a width and height of zero will be returned.</remarks>
  public static Rectangle Intersection(ref Rectangle a, ref Rectangle b)
  {
    // for an explanation of these checks, see Intersects(Rectangle, Rectangle)
    double bRight = b.Right;
    if(a.X > bRight) goto noIntersection;
    double bBottom = b.Bottom;
    if(a.Y > bBottom) goto noIntersection;
    double aRight = a.Right;
    if(aRight < b.X) goto noIntersection;
    double aBottom = a.Bottom;
    if(aBottom < b.Y) goto noIntersection;

    // they intersect, so return a new rectangle with the inward-most of each of the coordinates
    return Rectangle.FromPoints(a.X < b.X ? b.X : a.X, a.Y < b.Y ? b.Y : a.Y,
                                aRight < bRight ? aRight : bRight, aBottom < bBottom ? aBottom : bBottom);
    noIntersection:
    return new Rectangle(); // no intersection, so return an empty rectangle
  }
  #endregion

  #region Rotation
  /// <summary>Precalculates the sine and cosine factors used for rotation by a given angle, so that multiple points
  /// can be rotated without recalculating the factors.
  /// </summary>
  /// <param name="angle">The angle to rotate by, in radians.</param>
  /// <param name="sin">A variable that will be set to the sine factor.</param>
  /// <param name="cos">A variable that will be set to the cosine factor.</param>
  /// <remarks>These factors can be used with <see cref="Math2D.Rotate"/> functions to rotate points and vertices.</remarks>
  public static void GetRotationFactors(double angle, out double sin, out double cos)
  {
    sin = Math.Sin(angle);
    cos = Math.Cos(angle);
  }

  /// <summary>Rotates a 2D point using precalculated sine and cosine factors.</summary>
  /// <remarks>The sine and cosine factors can be obtained from the <see cref="GetRotationFactors"/> function.</remarks>
  public static Point2 Rotate(Point2 point, double sin, double cos)
  {
    return new Point2(point.X*cos - point.Y*sin, point.X*sin + point.Y*cos);
  }

  /// <summary>Rotates a 2D point in place using precalculated sine and cosine factors.</summary>
  /// <remarks>The sine and cosine factors can be obtained from the <see cref="GetRotationFactors"/> function.</remarks>
  [CLSCompliant(false)]
  public static void Rotate(ref Point2 point, double sin, double cos)
  {
    point = new Point2(point.X*cos - point.Y*sin, point.X*sin + point.Y*cos);
  }

  /// <summary>Rotates an array of points.</summary>
  /// <param name="points">The array of <see cref="Point2"/> to rotate.</param>
  /// <param name="start">The index at which to start rotating points.</param>
  /// <param name="length">The number of points to rotate.</param>
  /// <param name="angle">The angle by which to rotate the points, in radians.</param>
  public static void Rotate(Point2[] points, int start, int length, double angle)
  {
    if(angle == 0) return;
    double sin, cos;
    GetRotationFactors(angle, out sin, out cos);
    for(int end=start+length; start<end; start++) Rotate(ref points[start], sin, cos);
  }

  /// <summary>Rotates a 2D vector using precalculated sine and cosine factors.</summary>
  /// <remarks>The sine and cosine factors can be obtained from the <see cref="GetRotationFactors"/> function.</remarks>
  public static Vector2 Rotate(Vector2 vector, double sin, double cos)
  {
    return new Vector2(vector.X*cos - vector.Y*sin, vector.X*sin + vector.Y*cos);
  }

  /// <summary>Rotates a 2D vector in place using precalculated sine and cosine factors.</summary>
  /// <remarks>The sine and cosine factors can be obtained from the <see cref="GetRotationFactors"/> function.</remarks>
  [CLSCompliant(false)]
  public static void Rotate(ref Vector2 vector, double sin, double cos)
  {
    vector = new Vector2(vector.X*cos - vector.Y*sin, vector.X*sin + vector.Y*cos);
  }

  /// <summary>Rotates an array of vectors.</summary>
  /// <param name="vectors">The array of <see cref="Vector2"/> to rotate.</param>
  /// <param name="start">The index at which to start rotating vectors.</param>
  /// <param name="length">The number of vectors to rotate.</param>
  /// <param name="angle">The angle by which to rotate the vectors, in radians.</param>
  public static void Rotate(Vector2[] vectors, int start, int length, double angle)
  {
    if(angle == 0) return;
    double sin, cos;
    GetRotationFactors(angle, out sin, out cos);
    for(int end=start+length; start<end; start++) Rotate(ref vectors[start], sin, cos);
  }
  #endregion

  internal static double Clamp1(double v)
  {
    if(v > 1) v = 1;
    else if(v < -1) v =  -1;
    return v;
  }
}
#endregion

#region Vector2
/// <summary>This structure represents a mathematical vector in two-dimensional space.</summary>
[Serializable]
public struct Vector2
{
  /// <summary>Initializes this vector from magnitudes along the X and Y axes.</summary>
  /// <param name="x">The magnitude along the X axis.</param>
  /// <param name="y">The magnitude along the Y axis.</param>
  public Vector2(double x, double y) { X=x; Y=y; }
  /// <summary>Initializes this vector from a <see cref="Point2"/>.</summary>
  /// <param name="pt">A <see cref="Point2"/>. The point's X and Y coordinates will become the corresponding
  /// X and Y magnitudes of the vector.
  /// </param>
  public Vector2(Point2 pt) { X=pt.X; Y=pt.Y; }
  /// <summary>Initializes this vector from a <see cref="System.Drawing.Point"/>.</summary>
  /// <param name="pt">A <see cref="System.Drawing.Point"/>. The point's X and Y coordinates will become the
  /// corresponding X and Y magnitudes of the vector.
  /// </param>
  public Vector2(SysPoint pt) { X=pt.X; Y=pt.Y; }
  /// <summary>Initializes this vector from a <see cref="System.Drawing.Size"/>.</summary>
  /// <param name="size">A <see cref="System.Drawing.Size"/>. The size's Width and Height respectively will become
  /// the X and Y magnitudes of the vector.
  /// </param>
  public Vector2(System.Drawing.Size size) { X=size.Width; Y=size.Height; }

  /// <summary>Calculates and returns the angle of the vector.</summary>
  /// <value>The angle of the vector, in radians.</value>
  /// <remarks>An angle of zero points directly towards right (towards the positive side of the X axis). Other values
  /// are radian offsets counterclockwise from there.
  /// </remarks>
  public double Angle
  {
    get
    {
      double angle = Math.Atan2(Y, X);
      if(angle < 0) angle += Math.PI*2;
      return angle;
    }
  }

  /// <summary>Gets the cross vector, analagous to the three-dimensional cross product.</summary>
  /// <value>A <see cref="Vector2"/> perpendicular to this vector.</value>
  /// <remarks>While there is no real cross product in two dimensions, this property is analogous in that it
  /// returns a perpendicular vector.
  /// </remarks>
  public Vector2 CrossVector { get { return new Vector2(Y, -X); } }
  /// <include file="documentation.xml" path="//Geometry/Vector/Length/node()"/>
  public double Length
  {
    get { return Math.Sqrt(X*X+Y*Y); }
    set { this = GetNormal(value); }
  }
  /// <summary>Returns the length of this vector, squared.</summary>
  public double LengthSqr { get { return X*X+Y*Y; } }
  /// <include file="documentation.xml" path="//Geometry/Vector/Normal/node()"/>
  public Vector2 Normal { get { return this/Length; } }
  /// <summary>Determines whether the vector is not <see cref="Invalid"/>.</summary>
  /// <remarks>Invalid vectors are returned by some mathematical functions to signal that the function is undefined
  /// given the input. A vector returned by such a function can be tested for validity using this property.
  /// </remarks>
  public bool IsValid { get { return !double.IsNaN(X); } }
  /// <include file="documentation.xml" path="//Geometry/Vector/DotProduct/node()"/>
  public double DotProduct(Vector2 v) { return X*v.X + Y*v.Y; }
  /// <include file="documentation.xml" path="//Geometry/Vector/Equals/node()"/>
  public override bool Equals(object obj) { return obj is Vector2 && Equals((Vector2)obj); }
  /// <include file="documentation.xml" path="//Geometry/Vector/Equals2/node()"/>
  public bool Equals(Vector2 other)
  {
    return X.Equals(other.X) && Y.Equals(other.Y);
  }
  /// <include file="documentation.xml" path="//Geometry/Vector/Equals3/node()"/>
  public bool Equals(Vector2 vect, double epsilon)
  {
    return Math.Abs(vect.X-X)<=epsilon && Math.Abs(vect.Y-Y)<=epsilon;
  }
  /// <summary>Returns the angle from this vector to the given vector, measured counterclockwise.</summary>
  public double GetAngle(Vector2 v)
  {
    double angle = Math.Acos(Math2D.Clamp1(DotProduct(v)/(Length*v.Length)));
    if(X*v.Y < Y*v.X) angle = Math.PI*2 - angle;
    return angle;
  }
  /// <include file="documentation.xml" path="//Geometry/Vector/Normalize2/node()"/>
  public Vector2 GetNormal(double length) { return this * (length/Length); }
  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public unsafe override int GetHashCode()
  {
    return X.GetHashCode() ^ Y.GetHashCode();
  }
  /// <summary>Returns a copy of this vector, rotated by the given number of radians.</summary>
  /// <param name="angle">The angle to rotate by, in radians.</param>
  /// <returns>A new vector with the same magnitude as this one, and rotated by the given angle.</returns>
  public Vector2 Rotate(double angle)
  {
    double sin = Math.Sin(angle), cos = Math.Cos(angle);
    return new Vector2(X*cos-Y*sin, X*sin+Y*cos);
  }
  /// <summary>Converts this <see cref="Vector2"/> into an equivalent <see cref="Point2"/>.</summary>
  /// <returns>Returns a <see cref="Point2"/> with X and Y coordinates equal to the X and Y magnitudes of this
  /// vector.
  /// </returns>
  public Point2 ToPoint() { return new Point2(X, Y); }
  /// <summary>Converts this vector into a human-readable string.</summary>
  /// <returns>A human-readable string representation of this vector.</returns>
  public override string ToString() { return string.Format("[{0:f2},{1:f2}]", X, Y); }

  /// <summary>Returns a new vector with the magnitudes negated.</summary>
  public static Vector2 operator-(Vector2 v) { return new Vector2(-v.X, -v.Y); }
  /// <summary>Returns a new vector with the magnitudes equal to the sums of the operand's magnitudes.</summary>
  public static Vector2 operator+(Vector2 a, Vector2 b) { return new Vector2(a.X+b.X, a.Y+b.Y); }
  /// <summary>Returns a new vector with the magnitudes equal to the differences between the operand's magnitudes.</summary>
  public static Vector2 operator-(Vector2 a, Vector2 b) { return new Vector2(a.X-b.X, a.Y-b.Y); }
  /// <summary>Returns a new vector with both magnitudes multiplied by a scalar value.</summary>
  public static Vector2 operator*(Vector2 v, double f) { return new Vector2(v.X*f, v.Y*f); }
  /// <summary>Returns a new vector with both magnitudes multiplied by a scalar value.</summary>
  public static Vector2 operator*(double f, Vector2 v) { return new Vector2(v.X*f, v.Y*f); }
  /// <summary>Returns a new vector with both magnitudes divided by a scalar value.</summary>
  public static Vector2 operator/(Vector2 v, double f) { return new Vector2(v.X/f, v.Y/f); }

  /// <summary>Determines whether two vectors are exactly equal. You cannot use this method to compare against
  /// <see cref="Invalid"/>. Use the <see cref="IsValid"/> property for that.
  /// </summary>
  public static bool operator==(Vector2 a, Vector2 b) { return a.X==b.X && a.Y==b.Y; }

  /// <summary>Determines whether two vectors are not exactly equal. You cannot use this method to compare against
  /// <see cref="Invalid"/>. Use the <see cref="IsValid"/> property for that.
  /// </summary>
  public static bool operator!=(Vector2 a, Vector2 b) { return a.X!=b.X || a.Y!=b.Y; }

  /// <summary>Returns an invalid vector.</summary>
  /// <remarks>When a function is presented with input for which it is mathematically undefined, it can return an
  /// invalid vector instead of raising an exception. This property will return an invalid vector.
  /// </remarks>
  public static readonly Vector2 Invalid = new Vector2(double.NaN, double.NaN);

  /// <summary>Returns a vector with zero for all components.</summary>
  public static readonly Vector2 Zero = new Vector2();
  /// <summary>Returns a vector with an X component of 1.</summary>
  public static readonly Vector2 X1 = new Vector2(1, 0);
  /// <summary>Returns a vector with an Y component of 1.</summary>
  public static readonly Vector2 Y1 = new Vector2(0, 1);
  /// <summary>Returns a vector with an X component of -1.</summary>
  public static readonly Vector2 XM1 = new Vector2(-1, 0);
  /// <summary>Returns a vector with an Y component of -1.</summary>
  public static readonly Vector2 YM1 = new Vector2(0, -1);

  /// <summary>The magnitude of this vector along the X axis.</summary>
  public double X;
  /// <summary>The magnitude of this vector along the Y axis.</summary>
  public double Y;
}
#endregion

#region Point2
/// <summary>This structure represents a point in two-dimensional space.</summary>
[Serializable]
public struct Point2
{
  /// <summary>Initializes this <see cref="Point2"/> from a <see cref="System.Drawing.Point"/>.</summary>
  /// <param name="pt">The <see cref="System.Drawing.Point"/> from which this point will be initialized.</param>
  public Point2(SysPoint pt) { X=pt.X; Y=pt.Y; }
  /// <summary>Initializes this <see cref="Point2"/> from a <see cref="System.Drawing.PointF"/>.</summary>
  /// <param name="pt">The <see cref="System.Drawing.PointF"/> from which this point will be initialized.</param>
  public Point2(SysPointF pt) { X=pt.X; Y=pt.Y; }
  /// <summary>Initializes this <see cref="Point2"/> from a set of coordinates.</summary>
  /// <param name="x">The point's X coordinate.</param>
  /// <param name="y">The point's Y coordinate.</param>
  public Point2(double x, double y) { X=x; Y=y; }

  /// <summary>Determines whether the point is not <see cref="Invalid"/>.</summary>
  /// <remarks>Invalid points are returned by some mathematical functions to signal that the function is undefined
  /// given the input. A point returned by such a function can be tested for validity using this property.
  /// </remarks>
  public bool IsValid { get { return !double.IsNaN(X); } }
 
  /// <include file="documentation.xml" path="//Geometry/Point/DistanceTo/node()"/>
  public double DistanceTo(Point2 point)
  {
    double xd=point.X-X, yd=point.Y-Y;
    return Math.Sqrt(xd*xd+yd*yd);
  }
  /// <include file="documentation.xml" path="//Geometry/Point/DistanceSquaredTo/node()"/>
  public double DistanceSquaredTo(Point2 point)
  {
    double xd=point.X-X, yd=point.Y-Y;
    return xd*xd+yd*yd;
  }
  /// <summary>Offsets this point by a given amount, translating it in space.</summary>
  /// <param name="xd">The value to add to the point's X coordinate.</param>
  /// <param name="yd">The value to add to the point's Y coordinate.</param>
  public void Offset(double xd, double yd) { X+=xd; Y+=yd; }
  /// <summary>Converts this point to a <see cref="System.Drawing.Point"/>.</summary>
  /// <returns>A <see cref="System.Drawing.Point"/> containing approximately the same coordinates. The coordinates
  /// will be rounded using <see cref="Math.Round(double)"/> in order to convert them to integers.
  /// </returns>
  public SysPoint ToPoint() { return new SysPoint((int)Math.Round(X), (int)Math.Round(Y)); }
  /// <summary>Converts this point to a <see cref="System.Drawing.PointF"/>.</summary>
  /// <returns>A <see cref="System.Drawing.PointF"/> containing approximately the same coordinates.</returns>
  public SysPointF ToPointF() { return new SysPointF((float)X, (float)Y); }
  /// <summary>Returns a vector with the same components as the coordinates of the point.</summary>
  public Vector2 ToVector()
  {
    return new Vector2(X, Y);
  }
  /// <include file="documentation.xml" path="//Geometry/Point/Equals/node()"/>
  public override bool Equals(object obj) { return obj is Point2 && (Point2)obj==this; }
  /// <include file="documentation.xml" path="//Geometry/Point/Equals3/node()"/>
  public bool Equals(Point2 point, double epsilon)
  {
    return Math.Abs(point.X-X)<=epsilon && Math.Abs(point.Y-Y)<=epsilon;
  }
  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public unsafe override int GetHashCode()
  {
    return X.GetHashCode() ^ Y.GetHashCode();
  }
  /// <summary>Converts this <see cref="Point2"/> into a human-readable string.</summary>
  /// <returns>A human-readable string representation of this <see cref="Point2"/>.</returns>
  public override string ToString() { return string.Format("({0:f2},{1:f2})", X, Y); }

  /// <summary>Returns a point at 0, 0.</summary>
  public static readonly Point2 Empty = new Point2();

  /// <summary>Returns an invalid point.</summary>
  /// <remarks>When a function is presented with input for which it is mathematically undefined, it can return an
  /// invalid point instead of raising an exception. This property will return an invalid point.
  /// </remarks>
  public static readonly Point2 Invalid = new Point2(double.NaN, double.NaN);

  /// <summary>Returns a <see cref="Vector2"/> representing the distance between the two points.</summary>
  public static Vector2 operator-(Point2 lhs, Point2 rhs) { return new Vector2(lhs.X-rhs.X, lhs.Y-rhs.Y); }
  /// <summary>Subtracts a <see cref="Vector2"/> from a point and returns the new point.</summary>
  public static Point2 operator-(Point2 lhs, Vector2 rhs) { return new Point2(lhs.X-rhs.X, lhs.Y-rhs.Y); }
  /// <summary>Adds a <see cref="Vector2"/> to a point and returns the new point.</summary>
  public static Point2 operator+(Point2 lhs, Vector2 rhs) { return new Point2(lhs.X+rhs.X, lhs.Y+rhs.Y); }
  /// <summary>Adds a <see cref="Vector2"/> to a point and returns the new point.</summary>
  public static Point2 operator+(Vector2 lhs, Point2 rhs) { return new Point2(lhs.X+rhs.X, lhs.Y+rhs.Y); }
  /// <summary>Determines whether two points are exactly equal. You cannot use this method to compare against
  /// <see cref="Invalid"/>. Use the <see cref="IsValid"/> property for that.
  /// </summary>
  public static bool operator==(Point2 lhs, Point2 rhs) { return lhs.X==rhs.X && lhs.Y==rhs.Y; }
  /// <summary>Determines whether two points are exactly equal. You cannot use this method to compare against
  /// <see cref="Invalid"/>. Use the <see cref="IsValid"/> property for that.
  /// </summary>
  public static bool operator!=(Point2 lhs, Point2 rhs) { return lhs.X!=rhs.X || lhs.Y!=rhs.Y; }

  /// <summary>Implicitly converts a <see cref="System.Drawing.Point"/> to a <see cref="Point2"/>.</summary>
  /// <param name="point">The <see cref="System.Drawing.Point"/> to convert.</param>
  /// <returns>A <see cref="Point2"/> containing the same coordinates as <paramref name="point"/>.</returns>
  public static implicit operator Point2(SysPoint point) { return new Point2(point.X, point.Y); }

  /// <summary>This point's X coordinate.</summary>
  public double X;
  /// <summary>This point's Y coordinate.</summary>
  public double Y;
}
#endregion

#region LineIntersection
/// <summary>This structure contains information about the intersection of two lines or line segments.</summary>
/// <remarks>The structure is returned from some line intersection functions. If the intersection is not valid
/// (ie, the lines given were parallel), the <see cref="Point"/> member will be invalid. You can use
/// <see cref="AdamMil.Mathematics.Geometry.Point2.IsValid"/> to check for this condition.
/// </remarks>
public struct LineIntersection
{
  /// <summary>Initializes this structure from an intersection point and information about where the intersection
  /// occurred.
  /// </summary>
  /// <param name="point">The point where the lines intersected, or if the lines didn't intersect, an invalid point.
  /// </param>
  /// <param name="onFirst">This should be true if the intersection point lies on the first line segment.</param>
  /// <param name="onSecond">This should be true if the intersection point lies on the second line segment.</param>
  public LineIntersection(Point2 point, bool onFirst, bool onSecond)
  {
    Point=point; OnFirst=onFirst; OnSecond=onSecond;
  }
  /// <summary>Determines whether the intersection point lies on both line segments.</summary>
  /// <value>True if the intersection point lies on both line segments. This indicates that a segment intersection
  /// has occurred.
  /// </value>
  public bool OnBoth { get { return OnFirst && OnSecond; } }
  /// <summary>The intersection point, or an invalid point if the lines did not intersect.</summary>
  /// <remarks>If the lines did not intersect (because they were invalid or parallel), this will be an invalid
  /// point. You can use <see cref="AdamMil.Mathematics.Geometry.Point2.IsValid"/> to check for this condition.
  /// </remarks>
  public Point2 Point;
  /// <summary>Determines whether the intersection point lies on the first line segment.</summary>
  /// <remarks>If true, the second line intersected the first line segment. If both <see cref="OnFirst"/> and
  /// <see cref="OnSecond"/> are true (<see cref="OnBoth"/> is true), then both segments intersected each other.
  /// </remarks>
  public bool OnFirst;
  /// <summary>Determines whether the intersection point lies on the second line segment.</summary>
  /// <remarks>If true, the first line intersected the second line segment. If both <see cref="OnFirst"/> and
  /// <see cref="OnSecond"/> are true (<see cref="OnBoth"/> is true), then both segments intersected each other.
  /// </remarks>
  public bool OnSecond;
}
#endregion

#region Line2
/// <summary>This structure represents a line or line segment in two dimensions.</summary>
/// <remarks>The line is stored in parametric form, which means that it's stored as a point and a vector.</remarks>
[Serializable]
public struct Line2
{
  /// <summary>Initializes this line from a point's coordinates and a vector's axis magnitudes.</summary>
  /// <param name="x">The X coordinate of a point on the line (or the start of the line segment).</param>
  /// <param name="y">The Y coordinate of a point on the line (or the start of the line segment).</param>
  /// <param name="xd">The magnitude along the X axis of the line's direction. If you're defining a line segment,
  /// this should be the distance from <paramref name="x"/> to the endpoint's X coordinate.
  /// </param>
  /// <param name="yd">The magnitude along the Y axis of the line's direction. If you're defining a line segment,
  /// this should be the distance from <paramref name="y"/> to the endpoint's Y coordinate.
  /// </param>
  public Line2(double x, double y, double xd, double yd) { Start=new Point2(x, y); Vector=new Vector2(xd, yd); }
  /// <include file="documentation.xml" path="//Geometry/Line/Line/node()"/>
  public Line2(Point2 start, Vector2 vector) { Start=start; Vector=vector; }
  /// <include file="documentation.xml" path="//Geometry/Line/Line2/node()"/>
  public Line2(Point2 start, Point2 end) { Start=start; Vector=end-start; }

  /// <summary>Returns the endpoint of the line segment.</summary>
  /// <remarks>This is equivalent to <see cref="Start"/> + <see cref="Vector"/>.</remarks>
  public Point2 End { get { return Start+Vector; } set { Vector=value-Start; } }
  /// <summary>Calculates and returns the line segment's length.</summary>
  /// <remarks>This returns the length of <see cref="Vector"/>.</remarks>
  public double Length { get { return Vector.Length; } set { Vector.Length=value; } }
  /// <summary>Calculates and returns the square of the line segment's length.</summary>
  /// <remarks>This returns the square of the length of <see cref="Vector"/>.</remarks>
  public double LengthSqr { get { return Vector.LengthSqr; } }
  /// <summary>Determines whether the line is valid.</summary>
  /// <remarks>Invalid lines are returned by some mathematical functions to signal that the function is undefined
  /// given the input. A line returned by such a function can be tested for validity using this property.
  /// </remarks>
  public bool IsValid { get { return Start.IsValid; } }

  /// <summary>Returns the point on the line segment closest to the given point.</summary>
  public Point2 ClosestPointOnSegment(Point2 point)
  {
    // our line is represented as a vector from a starting point. get the vector from the starting point to 'point'
    Vector2 pvect = point - Start;
    double dot   = pvect.DotProduct(Vector); // get the dot product of the two vectors
    // if it's negative, the vectors are pointing in roughly opposite directions, so we return the start point.
    if(dot <= 0) return Start;
    double lenSqr = LengthSqr;
    // the dot product is ourLen * otherLen * cos(angle). if we compare dot >= ourLen * ourLen, it's the same as
    // comparing otherLen * cos(angle) >= ourLen. the cosine times otherLen produces the place along our vector that
    // a line perpendicular to us, intersecting 'point' would be. if that place is further than the length of our
    // vector, it's beyond the end, so just return the end.
    if(dot >= lenSqr) return End;
    // dot/lenSqr is a value between 0 and 1 specifying the place along our vector
    return Start + Vector * (dot/lenSqr);
  }

  /// <summary>Returns the signed distance from the line to a given point.</summary>
  /// <param name="point">The <see cref="Point2"/> to find the distance to.</param>
  /// <returns>Returns the distance from the point to the nearest point on the line. The distance may be positive or
  /// negative, with the sign indicating which side of the line the point is on. For a line defined in a clockwise
  /// manner, a positive value means that the point is "outside" the line and a negative value indicates that the
  /// point is "inside" the line. You can envision it this way: if this line was one of the clipping lines defining
  /// a convex polygon, a point would be "outside" the line if it was on the side that would put it outside the
  /// polygon. The point would be inside the polygon if it was "inside" all of the lines defining it. If you simply
  /// want the distance to the line, use <see cref="Math.Abs(double)"/> to get the absolute value. If you simply want
  /// to know which side of the line a point is on, use <see cref="WhichSide"/>, which is more efficient.
  /// </returns>
  public double DistanceTo(Point2 point) { return Vector.CrossVector.Normal.DotProduct(point-Start); }

  /// <include file="documentation.xml" path="//Geometry/Line/GetPoint/node()"/>
  public Point2 GetPoint(int point)
  {
    if(point<0 || point>1) throw new ArgumentOutOfRangeException("point", point, "must be 0 or 1");
    return point==0 ? Start : End;
  }

  /// <summary>Determines whether this line (not segment) intersects the given circle.</summary>
  public bool Intersects(Circle circle)
  {
    return Math2D.Intersects(ref circle, ref this);
  }

  /// <summary>Returns the distance from the point to the nearest point on the line segment.</summary>
  public double SegmentDistanceTo(Point2 point)
  {
    return point.DistanceTo(ClosestPointOnSegment(point));
  }

  /// <summary>Determines whether this line segment intersects the given circle.</summary>
  public bool SegmentIntersects(Circle circle)
  {
    return Math2D.SegmentIntersects(ref circle, ref this);
  }

  /// <summary>Determines whether this line (not segment) intersects the given line (not segment).</summary>
  public bool Intersects(Line2 line)
  {
    return Math2D.Intersects(ref this, ref line);
  }

  /// <summary>Determines whether this line segments intersects the given line segment.</summary>
  public bool SegmentIntersects(Line2 segment)
  {
    return Math2D.SegmentIntersects(ref this, ref segment);
  }

  /// <summary>Returns the intersection point of two lines (not segments).</summary>
  /// <returns>The intersection point of the two lines, or an invalid point if the lines do not intersect.
  /// You can check if the point is valid with <see cref="Point2.IsValid"/>.
  /// </returns>
  public Point2 Intersection(Line2 line)
  {
    return Math2D.Intersection(ref this, ref line);
  }

  /// <summary>Returns the intersection point of two lines segments.</summary>
  /// <returns>The intersection point of the two line segments, or an invalid point if the lines do not intersect.
  /// You can check if the point is valid with <see cref="Point2.IsValid"/>.
  /// </returns>
  public Point2 SegmentIntersection(Line2 segment)
  {
    return Math2D.Intersection(ref this, ref segment);
  }

  /// <summary>Returns information about the intersection of this line or line segment with another line or line
  /// segment.
  /// </summary>
  /// <param name="line">The line or line segment to test for intersection.</param>
  /// <returns>A <see cref="LineIntersection"/> containing information about the intersection of the two
  /// lines or line segments.
  /// </returns>
  public LineIntersection GetIntersectionInfo(Line2 line)
  {
    return Math2D.IntersectionInfo(ref this, ref line);
  }

  /// <summary>Determines whether this line (not segment) intersects the given <see cref="Rectangle"/>.</summary>
  public bool Intersects(Rectangle rect)
  {
    return Math2D.Intersects(ref this, ref rect);
  }

  /// <summary>Determines whether this line segment intersects the given rectangle.</summary>
  public bool SegmentIntersects(Rectangle rect)
  {
    return Math2D.SegmentIntersects(ref this, ref rect);
  }

  /// <summary>Returns the intersection of this line (not segment) with the given <see cref="Rectangle"/>.</summary>
  /// <returns>The portion of the line inside the rectangle, or an <see cref="Invalid"/> if there is no intersection.</returns>
  public Line2 Intersection(Rectangle rect)
  {
    return Math2D.Intersection(ref this, ref rect);
  }

  /// <summary>Returns the intersection of this line segment with the given <see cref="Rectangle"/>.</summary>
  /// <returns>The portion of the line inside the rectangle, or an <see cref="Invalid"/> if there is no intersection.</returns>
  public Line2 SegmentIntersection(Rectangle rect)
  {
    return Math2D.SegmentIntersection(ref this, ref rect);
  }

  /// <summary>Determines whether this line (not segment) intersects the given possibly-concave polygon.</summary>
  public bool Intersects(Polygon poly)
  {
    return Math2D.Intersects(ref this, poly);
  }

  /// <summary>Determines whether this line segment intersects the given convex polygon.</summary>
  public bool SegmentIntersects(Polygon convexPoly)
  {
    return Math2D.SegmentIntersects(ref this, convexPoly);
  }

  /// <summary>Returns the intersection of the line (not segment) with a convex polygon.</summary>
  /// <returns>The portion of the line inside the polygon, or an <see cref="Invalid"/> if there is no intersection.</returns>
  public Line2 Intersection(Polygon convexPoly)
  {
    return Math2D.Intersection(ref this, convexPoly);
  }

  /// <summary>Returns the intersection of the line segment with a convex polygon.</summary>
  /// <returns>The portion of the line segment inside the polygon, or an <see cref="Invalid"/> if there is no
  /// intersection.
  /// </returns>
  public Line2 SegmentIntersection(Polygon convexPoly)
  {
    return Math2D.SegmentIntersection(ref this, convexPoly);
  }

  /// <summary>Determines which side of a line the given point is on.</summary>
  /// <param name="point">The <see cref="Point2"/> to test.</param>
  /// <returns>A value indicating which side of the line the point is on. The value's sign indicates which side of
  /// the line the point is on. For a line defined in a clockwise
  /// manner, a positive value means that the point is "outside" the line and a negative value indicates that the
  /// point is "inside" the line. You can envision it this way: if this line was one of the clipping lines defining
  /// a convex polygon, a point would be "outside" the line if it was on the side that would put it outside the
  /// polygon. The point would be inside the polygon if it was "inside" all of the lines defining it.
  /// </returns>
  public double WhichSide(Point2 point) { return Vector.CrossVector.DotProduct(point-Start); }
  /// <include file="documentation.xml" path="//Geometry/Line/Equals/node()"/>
  public override bool Equals(object obj) { return obj is Line2 && Equals((Line2)obj); }
  /// <include file="documentation.xml" path="//Geometry/Line/Equals2/node()"/>
  public bool Equals(Line2 other)
  {
    return Start.Equals(other.Start) && Vector.Equals(other.Vector);
  }
  /// <include file="documentation.xml" path="//Geometry/Line/Equals3/node()"/>
  public bool Equals(Line2 line, double epsilon)
  {
    return Start.Equals(line.Start, epsilon) && Vector.Equals(line.Vector, epsilon);
  }
  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public override int GetHashCode() { return Start.GetHashCode() ^ Vector.GetHashCode(); }
  /// <summary>Converts this <see cref="Line2"/> into a human-readable string.</summary>
  /// <returns>A human-readable string representing this line.</returns>
  public override string ToString() { return string.Format("{0}->{1}", Start, Vector); }
  /// <summary>Creates a <see cref="Line2"/> from two points.</summary>
  /// <param name="x1">The X coordinate of the first point (a point on the line, or the start of the line segment).</param>
  /// <param name="y1">The Y coordinate of the first point (a point on the line, or the start of the line segment).</param>
  /// <param name="x2">The X coordinate of the second point (another point on the line, or the end of the line
  /// segment).
  /// </param>
  /// <param name="y2">The Y coordinate of the second point (another point on the line, or the end of the line
  /// segment).
  /// </param>
  /// <returns>A <see cref="Line2"/> initialized with those values.</returns>
  /// <remarks>Since the end point will need to be converted into a vector, some miniscule accuracy may be lost.
  /// Most notably, the <see cref="End"/> property may not be exactly equal to the point defined by
  /// <paramref name="x2"/> and <paramref name="y2"/>.
  /// </remarks>
  public static Line2 FromPoints(double x1, double y1, double x2, double y2) { return new Line2(x1, y1, x2-x1, y2-y1); }
  /// <summary>Determines whether two line segments are exactly equal. You cannot use this method to compare against
  /// <see cref="Invalid"/>. Use the <see cref="IsValid"/> property for that.
  /// </summary>
  public static bool operator==(Line2 lhs, Line2 rhs) { return lhs.Start==rhs.Start && lhs.Vector==rhs.Vector; }
  /// <summary>Determines whether two line segments are not exactly equal. You cannot use this method to compare against
  /// <see cref="Invalid"/>. Use the <see cref="IsValid"/> property for that.
  /// </summary>
  public static bool operator!=(Line2 lhs, Line2 rhs) { return lhs.Start!=rhs.Start || lhs.Vector!=rhs.Vector; }
  /// <summary>Returns an invalid line.</summary>
  /// <remarks>When a function is presented with input for which it is mathematically undefined, it can return an
  /// invalid line instead of raising an exception. This property will return an invalid line.
  /// </remarks>
  public static readonly Line2 Invalid = new Line2(Point2.Invalid, Vector2.Invalid);
  /// <summary>A point on the line, or the start point of the line segment.</summary>
  public Point2 Start;
  /// <summary>The line's direction, or the vector from the start point to the end point of the line segment.</summary>
  public Vector2 Vector;
}
#endregion

#region Circle
/// <summary>This structure represents a circle.</summary>
[Serializable]
public struct Circle
{
  /// <summary>Initializes this circle from a center point and a radius.</summary>
  /// <param name="centerX">The X coordinate of the circle's center point.</param>
  /// <param name="centerY">The Y coordinate of the circle's center point.</param>
  /// <param name="radius">The radius of the circle.</param>
  public Circle(double centerX, double centerY, double radius)
  {
    Center = new Point2(centerX, centerY);
    Radius = radius;
  }
  /// <summary>Initializes this circle from a center point and a radius.</summary>
  /// <param name="center">The circle's center point.</param>
  /// <param name="radius">The radius of the circle.</param>
  public Circle(Point2 center, double radius)
  {
    Center = center;
    Radius = radius;
  }

  /// <summary>Calculates and returns the area of the circle.</summary>
  public double Area { get { return RadiusSquared*Math.PI; } }

  /// <summary>Returns the radius, squared.</summary>
  public double RadiusSquared
  {
    get { return Radius * Radius; }
  }

  /// <summary>Determines whether the given circle is contained within this circle.</summary>
  public bool Contains(Circle circle)
  {
    return Math2D.Contains(ref this, ref circle);
  }

  /// <summary>Determines whether the given line segment is contained within this circle.</summary>
  public bool Contains(Line2 segment)
  {
    return Math2D.Contains(ref this, ref segment);
  }

  /// <summary>Determines whether the given point is fully contained within this circle.</summary>
  public bool Contains(Point2 point)
  {
    return Math2D.Contains(ref this, ref point);
  }

  /// <summary>Determines whether the given rectangle is fully contained within this circle.</summary>
  public bool Contains(Rectangle rect)
  {
    return Math2D.Contains(ref this, ref rect);
  }

  /// <summary>Determines whether the given possibly-concave polygon is fully contained within this circle.</summary>
  public bool Contains(Polygon poly)
  {
    return Math2D.Contains(ref this, poly);
  }

  /// <summary>Determines whether the object is a <see cref="Circle"/> exactly equal to this one.</summary>
  public override bool Equals(object obj)
  {
    return obj is Circle && Equals((Circle)obj);
  }

  /// <summary>Determines given <see cref="Circle"/> is exactly equal to this one.</summary>
  public bool Equals(Circle other)
  {
    return Center.Equals(other.Center) && Radius.Equals(other.Radius);
  }

  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public override int GetHashCode()
  {
    return Center.GetHashCode() ^ Radius.GetHashCode();
  }

  /// <summary>Determines whether the given circle intersects this circle.</summary>
  public bool Intersects(Circle circle)
  {
    return Math2D.Intersects(ref this, ref circle);
  }

  /// <summary>Determines whether the given line (not segment) intersects this circle.</summary>
  public bool Intersects(Line2 line)
  {
    return Math2D.Intersects(ref this, ref line);
  }

  /// <summary>Determines whether the given line segment intersects this circle.</summary>
  public bool SegmentIntersects(Line2 segment)
  {
    return Math2D.SegmentIntersects(ref this, ref segment);
  }

  /// <summary>Determines whether the given rectangle intersects this circle.</summary>
  public bool Intersects(Rectangle rect)
  {
    return Math2D.Intersects(ref this, ref rect);
  }

  /// <summary>Determines whether the given convex polygon intersects this circle.</summary>
  public bool Intersects(Polygon convexPoly)
  {
    return Math2D.Intersects(ref this, convexPoly);
  }

  /// <summary>The center point of this circle.</summary>
  public Point2 Center;

  /// <summary>The radius of this circle.</summary>
  public double Radius;

  /// <summary>Determines whether two circles are exactly equal.</summary>
  public static bool operator==(Circle a, Circle b)
  {
    return a.Center.X == b.Center.X && a.Center.Y == b.Center.Y && a.Radius == b.Radius;
  }

  /// <summary>Determines whether two circles are not exactly equal.</summary>
  public static bool operator!=(Circle a, Circle b)
  {
    return a.Center.X != b.Center.X || a.Center.Y != b.Center.Y || a.Radius != b.Radius;
  }
}
#endregion

#region Corner
/// <summary>This structure represents a corner (a point, and two connected edges).</summary>
/// <remarks>The two connected edges are stored as vectors from the corner point.</remarks>
[Serializable]
public struct Corner
{
  /// <summary>Gets the first edge of the corner. The edge's end point should be approximately equal to
  /// <see cref="Point"/> (the edge ends at <see cref="Point"/>).
  /// </summary>
  public Line2 Edge0 { get { return new Line2(Point+Vector0, -Vector0); } }
  /// <summary>Gets the second edge of the corner.</summary>
  /// <remarks>The edge's start point will be equal to <see cref="Point"/>.</remarks>
  public Line2 Edge1 { get { return new Line2(Point, Vector1); } }
  /// <summary>Gets the signed magnitude of the cross product of the two edge vectors.</summary>
  /// <remarks>Given that the two edges both lie on the same plane, their cross product will be a vector perpendicular
  /// to that plane. The sign of the value determines from which side of the plane the vector extends. This can be
  /// used to determine whether the two corner edges are defined in a clockwise or counter-clockwise manner. A
  /// positive value means the edges indicates a clockwise ordering, and a negative value indicates a
  /// counter-clockwise ordering. A zero value indicates that the two edge vectors are coincident.
  /// </remarks>
  public double CrossZ
  {
    get
    {
      Point2 p0 = Point+Vector0, p2 = Point+Vector1;
      return (Point.X-p0.X)*(p2.Y-Point.Y) - (Point.Y-p0.Y)*(p2.X-Point.X);
    }
  }
  /// <summary>Gets the specified edge.</summary>
  /// <param name="edge">The index of the edge to retrieve, either 0 or 1.</param>
  /// <returns>Returns <see cref="Edge0"/> or <see cref="Edge1"/> depending on whether <paramref name="edge"/> is
  /// 0 or 1, respectively.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="edge"/> is not 0 or 1.</exception>
  public Line2 GetEdge(int edge)
  {
    if(edge<0 || edge>1) throw new ArgumentOutOfRangeException("GetEdge", edge, "must be 0 or 1");
    return edge==0 ? Edge0 : Edge1;
  }
  /// <summary>Gets one of the three points that make up this corner.</summary>
  /// <param name="point">The index of the point to retrieve, from -1 to 1.</param>
  /// <returns>
  /// <list type="table">
  /// <listheader><term><paramref name="point"/></term><description>Return value</description></listheader>
  /// <item><term>-1</term><description><see cref="Point"/> + <see cref="Vector0"/></description></item>
  /// <item><term>0</term><description><see cref="Point"/></description></item>
  /// <item><term>1</term><description><see cref="Point"/> + <see cref="Vector1"/></description></item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="point"/> is less than -1 or greater
  /// than 1.
  /// </exception>
  public Point2 GetPoint(int point)
  {
    if(point<-1 || point>1) throw new ArgumentOutOfRangeException("GetPoint", point, "must be from -1 to 1");
    return point==0 ? Point : (point==-1 ? Point+Vector0 : Point+Vector1);
  }
  /// <summary>The corner point.</summary>
  public Point2 Point;
  /// <summary>The vector from the corner point (<see cref="Point"/>) to the beginning of the first edge.</summary>
  public Vector2 Vector0;
  /// <summary>The vector from the corner point (<see cref="Point"/>) to the end of the second edge.</summary>
  public Vector2 Vector1;
}
#endregion

#region Polygon
/// <summary>This class represents a polygon in two dimensions.</summary>
[Serializable]
public sealed class Polygon : ICloneable, ISerializable
{
  /// <summary>Initializes this polygon with no points.</summary>
  public Polygon() { points=new Point2[4]; }

  /// <summary>Initializes this polygon with three given points.</summary>
  /// <param name="p1">The first <see cref="Point2"/>.</param>
  /// <param name="p2">The second <see cref="Point2"/>.</param>
  /// <param name="p3">The third <see cref="Point2"/>.</param>
  public Polygon(Point2 p1, Point2 p2, Point2 p3) { points = new Point2[3] { p1, p2, p3 }; pointCount=3; }

  /// <summary>Initializes this polygon from an array of points.</summary>
  /// <param name="points">The array containing the points to use.</param>
  public Polygon(IList<Point2> points) : this(points.Count) { AddPoints(points); }

  /// <summary>Initializes this polygon from an array of points.</summary>
  public Polygon(Point2[] points, int index, int count) : this(count) { AddPoints(points, index, count); }

  /// <summary>Initializes this polygon with the given starting capacity.</summary>
  /// <param name="capacity">The number of points the polygon can initially hold.</param>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
  public Polygon(int capacity)
  {
    if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", capacity, "must not be negative");
    points = new Point2[Math.Max(3, capacity)];
  }

  /// <summary>Deserializes this polygon.</summary>
  /// <param name="info">A <see cref="SerializationInfo"/> object.</param>
  /// <param name="context">A <see cref="StreamingContext"/> object.</param>
  /// <remarks>This constructor is used to deserialize a polygon, and generally does not need to be called from user
  /// code.
  /// </remarks>
  private Polygon(SerializationInfo info, StreamingContext context)
  {
    pointCount = info.GetInt32("length");
    points = new Point2[Math.Max(3, pointCount)];
    for(int i=0; i<pointCount; i++) points[i] = (Point2)info.GetValue(i.ToString(), typeof(Point2));
  }

  /// <summary>Gets or sets one of the polygon's points.</summary>
  /// <param name="index">The index of the point to get or set.</param>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than zero or greater
  /// than or equal to <see cref="PointCount"/>.
  /// </exception>
  public Point2 this[int index]
  {
    get
    {
      if((uint)index >= (uint)pointCount) throw new ArgumentOutOfRangeException();
      return points[index];
    }
    set
    {
      if((uint)index >= (uint)pointCount) throw new ArgumentOutOfRangeException();
      points[index]=value;
    }
  }

  /// <summary>Gets or sets the number of points that this polygon is capable of holding without reallocating memory.</summary>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if you try to set the capacity to a value less than
  /// <see cref="PointCount"/>.
  /// </exception>
  public int Capacity
  {
    get { return points.Length; }
    set
    {
      if(value < pointCount)
      {
        throw new ArgumentOutOfRangeException("value", value, "The capacity cannot be less than PointCount.");
      }
      if(value < 3) value = 3;
      if(value != points.Length)
      {
        Point2[] newArray = new Point2[value];
        Array.Copy(points, newArray, pointCount);
        points = newArray;
      }
    }
  }

  /// <summary>Copies the points from this polygon into an array.</summary>
  /// <param name="array">The array to copy into.</param>
  /// <param name="index">The index at which to begin copying.</param>
  public void CopyTo(Point2[] array, int index) { Array.Copy(points, 0, array, index, pointCount); }

  /// <summary>Gets the number of points in the polygon.</summary>
  public int PointCount { get { return pointCount; } }

  #region ICloneable Members
  /// <summary>Returns a clone of this polygon.</summary>
  /// <returns>Returns a new <see cref="Polygon"/> with the same points as this one.</returns>
  public Polygon Clone() { return new Polygon(points, 0, pointCount); }

  object ICloneable.Clone() { return Clone(); }
  #endregion

  #region ISerializable Members
  /// <summary>This method is used to serialize this polygon.</summary>
  /// <param name="info">A <see cref="SerializationInfo"/> object.</param>
  /// <param name="context">A <see cref="StreamingContext"/> object.</param>
  /// <remarks>This method is used to serialize a polygon, and generally does not need to be called from user code.</remarks>
  public void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    info.AddValue("length", pointCount);
    for(int i=0; i<pointCount; i++) info.AddValue(i.ToString(), points[i]);
  }
  #endregion

  /// <summary>Adds a new point to the polygon.</summary>
  /// <param name="x">The X coordinate of the point.</param>
  /// <param name="y">The Y coordinate of the point.</param>
  /// <returns>Returns the index of the new point.</returns>
  public int AddPoint(double x, double y) { return AddPoint(new Point2(x, y)); }

  /// <summary>Adds a new point to the polygon.</summary>
  /// <param name="point">The <see cref="Point2"/> to add.</param>
  /// <returns>Returns the index of the new point.</returns>
  public int AddPoint(Point2 point)
  {
    if(pointCount == points.Length) EnlargeArray(1);
    points[pointCount] = point;
    return pointCount++;
  }

  /// <summary>Adds a list of points to the polygon.</summary>
  /// <param name="points">An array of points that will be added to the polygon.</param>
  public void AddPoints(IList<Point2> points)
  {
    if(points == null) throw new ArgumentNullException();
    EnlargeArray(points.Count);
    for(int i=0, len=points.Count; i<len; i++) this.points[pointCount++] = points[i];
  }

  /// <summary>Adds a list of points to the polygon.</summary>
  public void AddPoints(Point2[] points, int index, int count)
  {
    Utility.ValidateRange(points, index, count);
    EnlargeArray(count);
    for(int end=index+count; index<end; pointCount++,index++) this.points[pointCount] = points[index];
  }

  /// <summary>Asserts that this is a valid polygon.</summary>
  /// <exception cref="InvalidOperationException">Thrown if the polygon contains less than three points.</exception>
  public void AssertValid()
  {
    if(pointCount<3) throw new InvalidOperationException("Not a valid polygon [not enough points]!");
  }

  /// <summary>Removes all points from the polygon.</summary>
  public void Clear() { pointCount = 0; }

  /// <summary>Returns true if the given circle is fully contained within this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexContains(Circle circle)
  {
    return Math2D.Contains(this, ref circle);
  }

  /// <summary>Returns true if the given line segment is fully contained within this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexContains(Line2 segment)
  {
    return Math2D.Contains(this, ref segment);
  }

  /// <summary>Returns true if the given point is contained within this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexContains(Point2 point)
  {
    return Math2D.Contains(this, ref point);
  }

  /// <summary>Returns true if the given rectangle is fully contained within this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexContains(Rectangle rect)
  {
    return Math2D.Contains(this, ref rect);
  }

  /// <summary>Returns true if the given possibly-concave polygon is fully contained within this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexContains(Polygon poly)
  {
    return Math2D.Contains(this, poly);
  }

  /// <summary>Determines whether the given circle intersects this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexIntersects(Circle circle)
  {
    return Math2D.Intersects(ref circle, this);
  }

  /// <summary>Determines whether the given line (not segment) intersects this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexIntersects(Line2 line)
  {
    return Math2D.Intersects(ref line, this);
  }

  /// <summary>Determines whether the given line segment intersects this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexSegmentIntersects(Line2 segment)
  {
    return Math2D.Intersects(ref segment, this);
  }

  /// <summary>Determines whether the given rectangle intersects this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexIntersects(Rectangle rect)
  {
    return Math2D.Intersects(ref rect, this);
  }

  /// <summary>Determines whether the given convex polygon intersects this convex polygon.</summary>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public bool ConvexIntersects(Polygon convexPoly)
  {
    return Math2D.Intersects(this, convexPoly);
  }

  /// <summary>Calculates the intersection of the given line (not segment) with this convex polygon.</summary>
  /// <returns>Returns the line clipped to the polygon, or <see cref="Line2.Invalid"/> if there was no intersection.</returns>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public Line2 ConvexIntersection(Line2 line)
  {
    return Math2D.Intersection(ref line, this);
  }

  /// <summary>Calculates the intersection of the given line segment with this convex polygon.</summary>
  /// <returns>Returns the line segment clipped to the polygon, or <see cref="Line2.Invalid"/> if there was no
  /// intersection.
  /// </returns>
  /// <remarks>The result of calling this method on a nonconvex polygon is undefined.</remarks>
  public Line2 ConvexSegmentIntersection(Line2 segment)
  {
    return Math2D.SegmentIntersection(ref segment, this);
  }

  /// <summary>Calculates and returns the area of the polygon.</summary>
  /// <returns>The area of the polygon.</returns>
  public double GetArea()
  {
    AssertValid();
    double area=0;
    int i;
    for(i=0; i<pointCount-1; i++) area += points[i].X*points[i+1].Y - points[i+1].X*points[i].Y;
    area += points[i].X*points[0].Y - points[0].X*points[i].Y;
    return Math.Abs(area)/2;
  }

  /// <summary>Calculates and returns this polygon's bounding box.</summary>
  /// <returns>The smallest rectangle that contains this polygon.</returns>
  public Rectangle GetBounds()
  {
    AssertValid();
    Rectangle ret = new Rectangle(double.MaxValue, double.MaxValue, 0, 0);
    double x2=double.MinValue, y2=double.MinValue;
    for(int i=0; i<pointCount; i++)
    {
      if(points[i].X<ret.X) ret.X = points[i].X;
      if(points[i].X>x2) x2 = points[i].X;
      if(points[i].Y<ret.Y) ret.Y = points[i].Y;
      if(points[i].Y>y2) y2 = points[i].Y;
    }
    ret.Width  = x2-ret.X;
    ret.Height = y2-ret.Y;
    return ret;
  }

  /// <summary>Calculates and returns the polygon's centroid.</summary>
  /// <returns>The centroid of the polygon.</returns>
  /// <remarks>The centroid of a polygon is its center of mass (assuming it is uniformly dense).</remarks>
  public Point2 GetCentroid()
  {
    AssertValid();
    double area=0, x=0, y=0, d;
    for(int i=0, j; i<pointCount; i++)
    {
      j = i+1==pointCount ? 0 : i+1;
      d = points[i].X*points[j].Y - points[j].X*points[i].Y;
      x += (points[i].X+points[j].X)*d;
      y += (points[i].Y+points[j].Y)*d;
      area += d;
    }
    if(area<0) { area=-area; x=-x; y=-y; }
    area *= 3;
    return new Point2(x/area, y/area);
  }

  /// <summary>Gets the specified corner of the polygon.</summary>
  /// <param name="index">The index of the corner to retrieve, from 0 to <see cref="PointCount"/>-1.</param>
  /// <returns>A <see cref="Corner"/> representing the requested corner.</returns>
  public Corner GetCorner(int index)
  {
    AssertValid();
    Corner c = new Corner();
    c.Point = this[index];
    c.Vector0 = GetPoint(index-1) - c.Point;
    c.Vector1 = GetPoint(index+1) - c.Point;
    return c;
  }

  /// <summary>Gets the specified edge of the polygon.</summary>
  /// <param name="index">The index of the edge to retrieve, from 0 to <see cref="PointCount"/>-1.</param>
  /// <returns>A <see cref="Line2"/> segment representing the requested edge, built from the vertex at the given index
  /// and the next vertex (wrapping around to zero if <paramref name="index"/> is the last vertex).
  /// </returns>
  public Line2 GetEdge(int index)
  {
    if(pointCount<2) throw new InvalidOperationException("Polygon has no edges [not enough points]!");
    return new Line2(this[index], GetPoint(index+1));
  }

  /// <summary>Gets the specified point of the polygon.</summary>
  /// <param name="index">The index of the point to retrieve, from -<see cref="PointCount"/> to <see cref="PointCount"/>*2-1.</param>
  /// <returns>The requested <see cref="Point2"/>.</returns>
  /// <remarks>This method treats the list of points as circular, and allows negative indexes and indexes greater
  /// than or equal to <see cref="PointCount"/>, as long as the index is from -<see cref="PointCount"/> to
  /// <see cref="PointCount"/>*2-1. So if <see cref="PointCount"/> is 4, indexes of -4 and 7 are okay (they'll return points 0
  /// and 3 respectively), but -5 and 8 are not.
  /// </remarks>
  public Point2 GetPoint(int index)
  {
    return index<0 ? this[pointCount+index] : index>=pointCount ? this[index-pointCount] : this[index];
  }

  /// <summary>Inserts a point into the polygon.</summary>
  /// <param name="point">The <see cref="Point2"/> to insert.</param>
  /// <param name="index">The index at which the point should be inserted.</param>
  public void InsertPoint(Point2 point, int index)
  {
    if(pointCount == points.Length) EnlargeArray(1);
    if(index<pointCount) for(int i=pointCount; i>index; i--) points[i] = points[i-1];
    pointCount++;
    this[index] = point;
  }

  /// <summary>Determines whether the polygon was defined in a clockwise or counter-clockwise manner.</summary>
  /// <returns>True if the polygon points are defined in a clockwise manner and false otherwise.</returns>
  /// <remarks>This method only makes sense for convex polygons. The result of calling this method on a nonconvex
  /// polygon is undefined.
  /// </remarks>
  public bool IsClockwise()
  {
    for(int i=0; i<pointCount; i++)
    {
      int sign = Math.Sign(GetCorner(i).CrossZ);
      if(sign==1) return true;
      else if(sign==-1) return false;
    }
    return true;
  }

  /// <summary>Determines whether the polygon is convex.</summary>
  /// <returns>True if the polygon is convex and false otherwise.</returns>
  public bool IsConvex()
  {
    bool neg=false, pos=false;
    for(int i=0; i<pointCount; i++)
    {
      double z = GetCorner(i).CrossZ;
      if(z<0)
      {
        if(pos) return false;
        neg=true;
      }
      else if(z>0)
      {
        if(neg) return false;
        pos=true;
      }
    }
    return true;
  }

  /// <summary>Offsets the polygon by the given amount by offsetting all the points.</summary>
  /// <param name="offset">A <see cref="Vector2"/> containing the offset.</param>
  public void Offset(Vector2 offset) { Offset(offset.X, offset.Y); }

  /// <summary>Offsets the polygon by the given amount by offsetting all the points.</summary>
  /// <param name="xd">The distance to offset along the X axis.</param>
  /// <param name="yd">The distance to offset along the Y axis.</param>
  public void Offset(double xd, double yd) { for(int i=0; i<pointCount; i++) points[i].Offset(xd, yd); }

  /// <summary>Removes a point from the polygon.</summary>
  /// <param name="index">The index of the point to remove.</param>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than zero or greater
  /// than or equal to <see cref="PointCount"/>.
  /// </exception>
  public void RemovePoint(int index)
  {
    if((uint)index >= (uint)pointCount) throw new ArgumentOutOfRangeException();
    if(index != --pointCount) for(int i=index; i<pointCount; i++) points[i]=points[i+1];
  }

  /// <summary>Removes a range of points from the polygon.</summary>
  /// <param name="start">The index of the first point to remove.</param>
  /// <param name="length">The number of points to remove.</param>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if the range given falls outside the value range of points.
  /// </exception>
  public void RemoveRange(int start, int length)
  {
    if(length==0) return;
    int end = start+length;
    if((uint)start >= (uint)pointCount || (uint)end > (uint)pointCount) throw new ArgumentOutOfRangeException();
    for(; end<pointCount; end++) points[end-length]=points[end];
    this.pointCount -= length;
  }

  /// <summary>Reverses the order of this polygon's points.</summary>
  /// <remarks>This can be used to convert a convex polygon to and from clockwise ordering.</remarks>
  public void Reverse()
  {
    Point2 pt;
    for(int i=0, j=pointCount-1, len=pointCount/2; i<len; j--, i++) { pt = points[i]; points[i] = points[j]; points[j] = pt; }
  }

  /// <summary>Returns a copy of this polygon, with the points in reversed order.</summary>
  /// <returns>A copy of this polygon, with the points reversed.</returns>
  /// <remarks>This can be used to convert a convex polygon to and from clockwise ordering.</remarks>
  public Polygon Reversed()
  {
    Polygon newPoly = new Polygon(pointCount);
    for(int i=pointCount-1; i>=0; i--) newPoly.AddPoint(points[i]);
    return newPoly;
  }

  /// <summary>Rotates the points in this polygon around the origin (0,0).</summary>
  /// <param name="angle">The angle by which to rotate, in degrees.</param>
  public void Rotate(double angle) { Math2D.Rotate(points, 0, pointCount, angle); }

  /// <summary>Returns a copy of this polygon, with the points rotated around the origin (0,0).</summary>
  /// <param name="angle">The angle by which to rotate, in degrees.</param>
  /// <returns>A copy of this polygon, with the points rotated.</returns>
  public Polygon Rotated(double angle)
  {
    Polygon poly = Clone();
    poly.Rotate(angle);
    return poly;
  }

  /// <summary>Scales the points in this polygon by the given factors.</summary>
  /// <param name="xScale">The factor by which to multiply the X coordinates.</param>
  /// <param name="yScale">The factor by which to multiply the Y coordinates.</param>
  public void Scale(double xScale, double yScale)
  {
    for(int i=0; i<pointCount; i++)
    {
      points[i].X *= xScale;
      points[i].Y *= yScale;
    }
  }

  /// <summary>Returns a copy of this polygon, with the points scaled by the given factors.</summary>
  /// <param name="xScale">The factor by which to multiply the X coordinates.</param>
  /// <param name="yScale">The factor by which to multiply the Y coordinates.</param>
  /// <returns>A copy of this polygon, with the points scaled.</returns>
  public Polygon Scaled(double xScale, double yScale)
  {
    Polygon poly = Clone();
    poly.Scale(xScale, yScale);
    return poly;
  }

  /// <summary>Splits a non-convex polygon into convex polygons.</summary>
  /// <returns>An array of convex polygons that, together, make up the original polygon.</returns>
  /// <remarks>This method is only valid if the edges of the polygon do not overlap.</remarks>
  public Polygon[] Split()
  {
    const double epsilon = 1e-20;

    Polygon[] test = new Polygon[4], done = new Polygon[4];
    int tlen=1, dlen=0;

    test[0] = Clone();
    do
    {
      for(int pi=0, len=tlen; pi<len; pi++)
      {
        Polygon poly = test[pi];
        if(--tlen>0) // remove the current polygon
        {
          test[pi] = test[tlen];
          if(tlen<len) { pi--; len--; }
        }

        if(poly.pointCount<3) continue;
        // remove corners with coincident/parallel edges.
        for(int ci=poly.pointCount-2; ci>=1; ci--) if(poly.GetCorner(ci).CrossZ==0) poly.RemovePoint(ci);
        if(poly.pointCount<3) continue;

        int sign = Math.Sign(poly.GetCorner(0).CrossZ);
        for(int ci=1; ci<poly.pointCount; ci++)
        {
          Corner c = poly.GetCorner(ci);
          // if the sign is different, then the polygon is not convex, and splitting at this corner will result in
          // a simplification
          if(Math.Sign(c.CrossZ) != sign)
          {
            double dist = double.MaxValue, d, d2;
            Point2 splitPoint = new Point2();
            int splitEdge=-1, extPoint=-1, ept;
            for(int ei=0; ei<2; ei++) // try to extend each of the edges that make up this corner
            {
              Line2 toExtend = c.GetEdge(ei);
              int edge = ci-1+ei;
              for(int sei=0; sei<poly.pointCount; sei++) // test the edge with the intersection of every other edge
              {
                if(edge==sei || edge==sei-1 || (sei==0 && edge==poly.PointCount-1)) continue; // don't try to intersect adjacent edges
                else if(edge == 0 && sei == poly.PointCount-1) break;
                LineIntersection lint = toExtend.GetIntersectionInfo(poly.GetEdge(sei));
                // we don't want any points that are on the edge being extended (because it wouldn't be an extension)
                // and we want to make sure the other point is actually on the line segment
                if(lint.OnFirst || !lint.OnSecond || !lint.Point.IsValid) continue;
                ept = 0;
                d  = lint.Point.DistanceSquaredTo(toExtend.GetPoint(0)); // find the shortest cut
                d2 = lint.Point.DistanceSquaredTo(toExtend.GetPoint(1));
                // if substantially no extension has occurred, don't consider it. we can't compare against zero because
                // sometimes a very, very tiny extension occurs, and due to floating point inaccuracy, the algorithm
                // enters an infinite loop. using an epsilon prevents the infinite loop but increases the false failure
                // rate (when it throws an exception due to not being able to find any splits)
                if(d  < epsilon) d  = double.MaxValue;
                if(d2 < epsilon) d2 = double.MaxValue;
                if(d2<d) { d=d2; ept=1; } // 'ept' references which point gets moved to do the extension
                if(d<dist) { dist=d; splitEdge=sei; extPoint=ept; splitPoint=lint.Point; }
              }

              if(splitEdge!=-1) // if we can split it with this edge, do it. don't bother trying the other edges
              {
                poly.InsertPoint(splitPoint, ++splitEdge); // insert the split point
                Polygon new1 = new Polygon(), new2 = new Polygon();
                int extended = poly.Clip(edge+extPoint), other=poly.Clip(extended+(extPoint==0 ? 1 : -1));
                int npi = splitEdge;
                if(extended>=splitEdge) { extended++; other++; }
                // 'extended' is the point that was extended. 'other' is the other side of the edge being extended
                do // circle around the polygon, starting at the new point, adding points until we hit 'extended'
                {
                  new1.AddPoint(poly.points[npi]);
                  // if this polygon contains the edge being extended, then it must not contain the point being extended
                  if(npi==other) other=-1;
                  if(++npi>=poly.pointCount) npi-=poly.pointCount;
                } while(npi != extended);
                if(other!=-1) new1.AddPoint(poly.points[npi++]); // add the extended point to the appropriate polygon
                do // continue circling, adding points to the other polygon, and end by adding the split point again
                {
                  if(npi>=poly.pointCount) npi-=poly.pointCount;
                  new2.AddPoint(poly.points[npi]);
                } while(npi++ != splitEdge);
                test = AddPoly(new1, test, tlen++); // add the two polygons
                test = AddPoly(new2, test, tlen++);
                goto outer; // and continue the main loop
              }
            }
            if(splitEdge==-1) // if no split points could be found, give up
              throw new NotSupportedException("Unable to split polygon. This might not be a simple polygon.");
          }
        }
        done = AddPoly(poly, done, dlen++); // all the signs are the same, it's convex, so add it to the 'done' list
        outer: ;
      }
    } while(tlen>0);
    if(dlen==done.Length) return done; // return an array of the proper size
    else
    {
      Polygon[] narr = new Polygon[dlen];
      Array.Copy(done, narr, dlen);
      return narr;
    }
  }

  /// <summary>Sets the <see cref="Capacity"/> of the polygon to the actual number of points.</summary>
  void TrimExcess() { Capacity = pointCount; }

  int Clip(int index)
  {
    if(index<0) index += pointCount;
    else if(index>=pointCount) index -= pointCount;
    return index;
  }

  void EnlargeArray(int newElements)
  {
    if(points.Length < pointCount+newElements) points = Utility.EnlargeArray(points, pointCount, newElements);
  }

  static Polygon[] AddPoly(Polygon poly, Polygon[] array, int index)
  {
    if(index>=array.Length)
    {
      Polygon[] narr = new Polygon[array.Length*2];
      Array.Copy(array, narr, array.Length);
      array=narr;
    }
    array[index] = poly;
    return array;
  }

  Point2[] points;
  int pointCount;
}
#endregion

#region Rectangle
/// <summary>This structure represents a rectangle.</summary>
[Serializable]
public struct Rectangle
{
  /// <summary>Initializes this rectangle from a <see cref="System.Drawing.Rectangle"/>.</summary>
  /// <param name="rect">The <see cref="System.Drawing.Rectangle"/> from which this rectangle will be initialized.</param>
  public Rectangle(SysRectangle rect)
  {
    X=rect.X; Y=rect.Y; Width=rect.Width; Height=rect.Height;
  }
  /// <summary>Initializes this rectangle from a <see cref="System.Drawing.RectangleF"/>.</summary>
  /// <param name="rect">The <see cref="System.Drawing.RectangleF"/> from which this rectangle will be initialized.</param>
  public Rectangle(SysRectangleF rect) { X=rect.X; Y=rect.Y; Width=rect.Width; Height=rect.Height; }
  /// <summary>Initializes this rectangle from a position and a size.</summary>
  /// <param name="x">The X coordinate of the rectangle's top-left corner.</param>
  /// <param name="y">The Y coordinate of the rectangle's top-left corner.</param>
  /// <param name="width">The rectangle's width. This should not be negative.</param>
  /// <param name="height">The rectangle's height. This should not be negative.</param>
  public Rectangle(double x, double y, double width, double height) { X=x; Y=y; Width=width; Height=height; }
  /// <summary>Initializes this rectangle from a position and a size.</summary>
  /// <param name="location">The rectangle's top-left corner.</param>
  /// <param name="size">The vector from the <paramref name="location"/> to the rectangle's bottom-right conrner.
  /// In other words, a vector holding the width and height of the rectangle.
  /// </param>
  public Rectangle(Point2 location, Vector2 size) { X=location.X; Y=location.Y; Width=size.X; Height=size.Y; }
  /// <summary>Initializes this rectangle from two points.</summary>
  /// <param name="corner1">One corner of the rectangle.</param>
  /// <param name="corner2">The opposite corner of the rectangle.</param>
  /// <remarks>Since one corner will need to be converted into a vector, some miniscule accuracy may be lost.</remarks>
  public Rectangle(Point2 corner1, Point2 corner2)
  {
    double x2, y2;
    if(corner1.X<=corner2.X) { X=corner1.X; x2=corner2.X; }
    else { X=corner2.X; x2=corner1.X; }
    if(corner1.Y<=corner2.Y) { Y=corner1.Y; y2=corner2.Y; }
    else { Y=corner2.Y; y2=corner1.Y; }
    Width=x2-X; Height=y2-Y;
  }

  /// <summary>Gets the bottom of the rectangle.</summary>
  /// <remarks>This is equivalent to <see cref="Y"/> + <see cref="Height"/>.</remarks>
  public double Bottom { get { return Y+Height; } }
  /// <summary>Gets the bottom-right corner of the rectangle.</summary>
  /// <remarks>This is equivalent to <see cref="TopLeft"/> + <see cref="Size"/>.</remarks>
  public Point2 BottomRight { get { return new Point2(X+Width, Y+Height); } }
  /// <summary>Gets or sets the top-left corner of the rectangle.</summary>
  public Point2 Location
  {
    get { return new Point2(X, Y); }
    set { X=value.X; Y=value.Y; }
  }
  /// <summary>Gets the right side of the rectangle.</summary>
  /// <remarks>This is equivalent to <see cref="X"/> + <see cref="Width"/>.</remarks>
  public double Right { get { return X+Width; } }
  /// <summary>Gets or sets the size of the rectangle.</summary>
  public Vector2 Size
  {
    get { return new Vector2(Width, Height); }
    set { Width=value.X; Height=value.Y; }
  }
  /// <summary>Gets or sets the top-left corner of the rectangle.</summary>
  public Point2 TopLeft
  {
    get { return new Point2(X, Y); }
    set { X=value.X; Y=value.Y; }
  }

  /// <summary>Determines whether the specified circle is fully contained within this rectangle.</summary>
  public bool Contains(Circle circle)
  {
    return Math2D.Contains(ref this, ref circle);
  }

  /// <summary>Determines whether the specified line segment is fully contained within this rectangle.</summary>
  public bool Contains(Line2 segment)
  {
    return Math2D.Contains(ref this, ref segment);
  }

  /// <summary>Determines whether the specified point lies within the rectangle.</summary>
  public bool Contains(Point2 point)
  {
    return Math2D.Contains(ref this, ref point);
  }

  /// <summary>Determines whether this rectangle completely contains the specified rectangle.</summary>
  public bool Contains(Rectangle rect)
  {
    return Math2D.Contains(ref this, ref rect);
  }

  /// <summary>Determines whether the given possibly-concave polygon is completely contained within this rectangle.</summary>
  public bool Contains(Polygon poly)
  {
    return Math2D.Contains(ref this, poly);
  }

  /// <summary>Determines whether the given circle intersects this rectangle.</summary>
  public bool Intersects(Circle circle)
  {
    return Math2D.Intersects(ref circle, ref this);
  }

  /// <summary>Determines whether the given line (not segment) intersects this rectangle</summary>
  public bool Intersects(Line2 line)
  {
    return Math2D.Intersects(ref line, ref this);
  }

  /// <summary>Determines whether the given line segment intersects this rectangle</summary>
  public bool SegmentIntersects(Line2 segment)
  {
    return Math2D.SegmentIntersects(ref segment, ref this);
  }

  /// <summary>Determines whether the given rectangle intersects this one.</summary>
  public bool Intersects(Rectangle rect)
  {
    return Math2D.Intersects(ref this, ref rect);
  }

  /// <summary>Determines whether this rectangle intersects the given convex polygon.</summary>
  public bool Intersects(Polygon convexPoly)
  {
    return Math2D.Intersects(ref this, convexPoly);
  }

  /// <summary>Calculates the intersection of the given line (not segment) with this rectangle.</summary>
  /// <returns>Returns the line clipped to this rectangle, or <see cref="Line2.Invalid"/> if there was no intersection.</returns>
  public Line2 Intersection(Line2 line)
  {
    return Math2D.Intersection(ref line, ref this);
  }

  /// <summary>Calculates the intersection of the given line segment with this rectangle.</summary>
  /// <returns>Returns the line segment clipped to this rectangle, or <see cref="Line2.Invalid"/> if there was no intersection.</returns>
  public Line2 SegmentIntersection(Line2 segment)
  {
    return Math2D.SegmentIntersection(ref segment, ref this);
  }

  /// <summary>Determines whether the given is a <see cref="Rectangle"/> exactly equal to this one.</summary>
  public override bool Equals(object obj) { return obj is Rectangle && this==(Rectangle)obj; }
  /// <summary>Gets an edge of the rectangle.</summary>
  /// <param name="i">The index of the edge to retrieve (from 0 to 3).</param>
  /// <returns>The top, left, right, and bottom edges for respective values of <paramref name="i"/> from 0 to 3.</returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="i"/> is less than 0 or greater than 3.</exception>
  public Line2 GetEdge(int i)
  {
    if((uint)i > (uint)3) throw new ArgumentOutOfRangeException("i", i, "must be from 0 to 3");
    switch(i)
    {
      case 0: return new Line2(X, Y, 0, Height);       // left
      case 1: return new Line2(X, Y, Width, 0);        // top
      case 2: return new Line2(X+Width, Y, 0, Height); // right
      case 3: return new Line2(X, Y+Height, Width, 0); // bottom
      default: return Line2.Invalid; // can't get here
    }
  }
  /// <summary>Gets a corner of the rectangle.</summary>
  /// <param name="i">The index of the point to retrieve (from 0 to 3).</param>
  /// <returns>The top-left, top-right, bottom-right, and bottom-left corners for respective values of
  /// <paramref name="i"/> from 0 to 3.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="i"/> is less than 0 or greater than 3.</exception>
  public Point2 GetPoint(int i)
  {
    if((uint)i > (uint)3) throw new ArgumentOutOfRangeException("i", i, "must be from 0 to 3");
    switch(i)
    {
      case 0: return new Point2(X, Y);
      case 1: return new Point2(X+Width, Y);
      case 2: return new Point2(X+Width, Y+Height);
      case 3: return new Point2(X, Y+Height);
      default: return Point2.Invalid; // can't get here
    }
  }
  /// <summary>Inflates this rectangle by the given amount.</summary>
  /// <param name="x">The amount to inflate by on the X axis.</param>
  /// <param name="y">The amount to inflate by on the Y axis.</param>
  /// <remarks>All edges will be offset by the given values, so the actual difference in width and height will be
  /// twice the value of x and y.
  /// </remarks>
  public void Inflate(double x, double y) { X-=x; Width+=x*2; Y-=y; Height+=y*2; }
  /// <summary>Returns a copy of this rectangle, inflated by the given amount.</summary>
  /// <param name="x">The amount to inflate by on the X axis.</param>
  /// <param name="y">The amount to inflate by on the Y axis.</param>
  /// <remarks>All edges will be offset by the given values, so the actual difference in width and height will be
  /// twice the value of x and y.
  /// </remarks>
  public Rectangle Inflated(double x, double y) { return new Rectangle(X-x, Y-y, Width+x*2, Height+y*2); }
  /// <summary>Sets this rectangle to the intersection of this rectangle with the specified rectangle.</summary>
  /// <param name="rect">The rectangle to use for intersection.</param>
  /// <remarks>If the rectangles do not intersect, this rectangle will be set to an empty rectangle (with a width
  /// and height of zero).
  /// </remarks>
  public void Intersect(Rectangle rect)
  {
    this = Math2D.Intersection(ref this, ref rect);
  }
  /// <summary>Returns the intersection of this rectangle with the specified rectangle.</summary>
  /// <param name="rect">The rectangle to use for intersection.</param>
  /// <returns>The intersection of this rectangle and <paramref name="rect"/>, or an empty rectangle (a rectangle
  /// with a width and height of zero) if there is no intersection.
  /// </returns>
  public Rectangle Intersection(Rectangle rect)
  {
    return Math2D.Intersection(ref this, ref rect);
  }
  /// <summary>Returns the union of this rectangle with the given rectangle.</summary>
  /// <param name="rect">The rectangle with which this rectangle will be combined.</param>
  /// <returns>The smallest rectangle that contains both this and <paramref name="rect"/>.</returns>
  public Rectangle Union(Rectangle rect)
  {
    Rectangle ret = new Rectangle(X, Y, Width, Height);
    ret.Unite(rect);
    return ret;
  }
  /// <summary>Sets this rectangle to the union of this rectangle with the given rectangle.</summary>
  /// <param name="rect">The rectangle with which this rectangle will be combined.</param>
  /// <remarks>Sets this rectangle to the smallest rectangle that contains both this and <paramref name="rect"/>. If
  /// <paramref name="rect"/> is an empty rectangle, there will be no effect.
  /// </remarks>
  public void Unite(Rectangle rect)
  {
    if(rect.Width != 0 || rect.Height != 0) // uniting with an empty rectangle has no effect
    {
      if(X>rect.X) { Width += X-rect.X; X=rect.X; }
      if(Y>rect.Y) { Height += Y-rect.Y; Y=rect.Y; }
      if(Right<rect.Right) Width  += rect.Right-Right;
      if(Bottom<rect.Bottom) Height += rect.Bottom-Bottom;
    }
  }
  /// <summary>Offsets this rectangle by the given amount.</summary>
  /// <param name="x">The amount to offset along the X axis.</param>
  /// <param name="y">The amount to offset along the Y axis.</param>
  /// <remarks>This has the effect of offsetting <see cref="X"/> and <see cref="Y"/>.</remarks>
  public void Offset(double x, double y) { X+=x; Y+=y; }
  /// <summary>Offsets this rectangle by the given amount.</summary>
  /// <param name="vect">A <see cref="Vector2"/> specifying the offset.</param>
  /// <remarks>This has the effect of offsetting <see cref="X"/> and <see cref="Y"/>.</remarks>
  public void Offset(Vector2 vect) { X+=vect.X; Y+=vect.Y; }

  /// <summary>Converts this rectangle into the smallest <see cref="SysRectangle"/> that fully contains this rectangle.</summary>
  public SysRectangle ToRectangle()
  {
    int x = (int)X, y = (int)Y;
    return new SysRectangle(x, y, (int)Math.Ceiling(Right) - x, (int)Math.Ceiling(Bottom) - y);
  }

  /// <summary>Converts this rectangle into a <see cref="SysRectangleF"/>.</summary>
  public SysRectangleF ToRectangleF()
  {
    return new SysRectangleF((float)X, (float)Y, (float)Width, (float)Height);
  }

  /// <summary>Converts this rectangle into human-readable string.</summary>
  /// <returns>A human-readable string representation of this rectangle.</returns>
  public override string ToString()
  {
    return string.Format("X={0:F2} Y={1:F2} Width={2:F2} Height={3:F2}", X, Y, Width, Height);
  }
  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public override int GetHashCode()
  {
    return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
  }

  /// <summary>Creates a rectangle from a center point and a size.</summary>
  public static Rectangle FromCenter(Point2 center, double width, double height)
  {
    return new Rectangle(center.X-width*0.5, center.Y-height*0.5, width, height);
  }

  /// <summary>Creates a rectangle from a center point and a size.</summary>
  public static Rectangle FromCenter(double centerX, double centerY, double width, double height)
  {
    return new Rectangle(centerX-width*0.5, centerY-height*0.5, width, height);
  }

  /// <summary>Creates a rectangle from two points and returns it.</summary>
  /// <param name="x1">The X coordinate of one corner of the rectangle.</param>
  /// <param name="y1">The Y coordinate of one corner of the rectangle.</param>
  /// <param name="x2">The X coordinate of the opposite corner of the rectangle.</param>
  /// <param name="y2">The Y coordinate of the opposite corner of the rectangle.</param>
  /// <returns></returns>
  public static Rectangle FromPoints(double x1, double y1, double x2, double y2)
  {
    return new Rectangle(new Point2(x1, y1), new Point2(x2, y2));
  }

  /// <summary>Determines whether two <see cref="Rectangle"/> objects are exactly equal.</summary>
  public static bool operator==(Rectangle a, Rectangle b)
  {
    return a.X==b.X && a.Y==b.Y && a.Width==b.Width && a.Height==b.Height;
  }

  /// <summary>Determines whether two <see cref="Rectangle"/> objects are not exactly equal.</summary>
  public static bool operator!=(Rectangle a, Rectangle b)
  {
    return a.X!=b.X || a.Y!=b.Y || a.Width!=b.Width || a.Height!=b.Height;
  }

  /// <summary>The X coordinate of the top-left corner of the rectangle.</summary>
  public double X;
  /// <summary>The Y coordinate of the top-left corner of the rectangle.</summary>
  public double Y;
  /// <summary>The width of the rectangle. This value should not be negative.</summary>
  public double Width;
  /// <summary>The height of the rectangle. This value should not be negative.</summary>
  public double Height;
}
#endregion

#region Vector3
/// <summary>This structure represents a mathematical vector in three-dimensional space.</summary>
[Serializable]
public struct Vector3
{
  /// <summary>Initializes this vector from magnitudes along the X, Y, and Z axes.</summary>
  /// <param name="x">The magnitude along the X axis.</param>
  /// <param name="y">The magnitude along the Y axis.</param>
  /// <param name="z">The magnitude along the Z axis.</param>
  public Vector3(double x, double y, double z) { X=x; Y=y; Z=z; }
  /// <summary>Initializes this vector from a 2D vector. The Z coordinate will be zero.</summary>
  public Vector3(Vector2 v) { X=v.X; Y=v.Y; Z=0; }
  /// <summary>Initializes this vector from a <see cref="Point3"/>.</summary>
  /// <param name="pt">A <see cref="Point3"/>. The point's X, Y, and Z coordinates will become the corresponding
  /// X, Y, and Z magnitudes of the vector.
  /// </param>
  public Vector3(Point3 pt) { X=pt.X; Y=pt.Y; Z=pt.Z; }

  /// <summary>Determines whether the vector is not <see cref="Invalid"/>.</summary>
  public bool IsValid { get { return !double.IsNaN(X); } }

  /// <include file="documentation.xml" path="//Geometry/Vector/Length/node()"/>
  public double Length
  {
    get { return Math.Sqrt(X*X+Y*Y+Z*Z); }
    set { this = GetNormal(value); }
  }
  /// <summary>Returns the length of this vector, squared.</summary>
  public double LengthSqr { get { return X*X+Y*Y+Z*Z; } }
  /// <include file="documentation.xml" path="//Geometry/Vector/Normal/node()"/>
  public Vector3 Normal { get { return this / Length; } }
  /// <summary>Returns the cross product of this vector with another vector.</summary>
  /// <param name="v">The other operand.</param>
  /// <returns>A <see cref="Vector3"/> perpendicular to both this vector and <paramref name="v"/>.</returns>
  public Vector3 CrossProduct(Vector3 v) { return new Vector3(Y*v.Z-Z*v.Y, Z*v.X-X*v.Z, X*v.Y-Y*v.X); }
  /// <include file="documentation.xml" path="//Geometry/Vector/DotProduct/node()"/>
  public double DotProduct(Vector3 v) { return X*v.X + Y*v.Y + Z*v.Z; }

  /// <summary>Returns the angle from this vector to the given vector, within the plane that contains both. The angle will always be from
  /// 0 to Pi. To obtain angles from 0 to 2Pi, use <see cref="GetAngle(Vector3,Vector3)"/> instead.
  /// </summary>
  public double GetAngle(Vector3 v)
  {
    return Math.Acos(Math2D.Clamp1(DotProduct(v)/(Length*v.Length)));
  }

  /// <summary>Returns the angle from this vector to the given vector, expressed as a clockwise rotation around the given up vector, which
  /// should be orthogonal to the two vectors. This is different from simply using the dot product because the dot product doesn't account
  /// for whether the angle is greater or less than 180 degrees. None of the vectors need to be normalized.
  /// </summary>
  public double GetAngle(Vector3 v, Vector3 up)
  {
    double angle = GetAngle(v);
    if(up.DotProduct(CrossProduct(v)) < 0) angle = Math.PI*2 - angle;
    return angle;
  }

  /// <include file="documentation.xml" path="//Geometry/Vector/Normalized2/node()"/>
  public Vector3 GetNormal(double length) { return this * (length/Length); }

  /// <summary>Returns a <see cref="Vector2"/> representing the portion of this vector that lies within the plane Z=0.</summary>
  public Vector2 Project2D()
  {
    return new Vector2(X, Y);
  }

  /// <summary>Returns the portion of this vector that lies within the plane specified from the given plane normal.</summary>
  public Vector3 PlaneProject(Vector3 planeNormal)
  {
    return this - VectorProject(planeNormal);
  }

  /// <summary>Returns the portion of this vector parallel to the given normal vector.</summary>
  public Vector3 VectorProject(Vector3 dirNormal)
  {
    return dirNormal * DotProduct(dirNormal);
  }

  /// <summary>Returns a copy of this vector, rotated around the X axis.</summary>
  /// <param name="angle">The angle to rotate by, in radians.</param>
  /// <returns>A copy of this vector, rotated around the X axis.</returns>
  public Vector3 RotateX(double angle)
  {
    double sin = Math.Sin(angle), cos = Math.Cos(angle);
    return new Vector3(X, Y*cos-Z*sin, Y*sin+Z*cos);
  }
  /// <summary>Returns a copy of this vector, rotated around the Y axis.</summary>
  /// <param name="angle">The angle to rotate by, in radians.</param>
  /// <returns>A copy of this vector, rotated around the Y axis.</returns>
  public Vector3 RotateY(double angle)
  {
    double sin = Math.Sin(angle), cos = Math.Cos(angle);
    return new Vector3(Z*sin+X*cos, Y, Z*cos-X*sin);
  }
  /// <summary>Returns a copy of this vector, rotated around the Z axis.</summary>
  /// <param name="angle">The angle to rotate by, in radians.</param>
  /// <returns>A copy of this vector, rotated around the Z axis.</returns>
  public Vector3 RotateZ(double angle)
  {
    double sin = Math.Sin(angle), cos = Math.Cos(angle);
    return new Vector3(X*cos-Y*sin, X*sin+Y*cos, Z);
  }
  /// <summary>Returns a copy of this vector, rotated around an arbitrary axis.</summary>
  /// <param name="vector">The axis to rotate around. This should be a normalized vector.</param>
  /// <param name="angle">The angle to rotate by, in radians.</param>
  /// <returns>A copy of this vector, rotated around the given axis.</returns>
  public Vector3 Rotate(Vector3 vector, double angle)
  {
    double cos = Math.Cos(angle), sin = Math.Sin(angle);
    return this*cos + vector.CrossProduct(this)*sin + vector*vector.DotProduct(this)*(1-cos); // Rodrigues' rotation formula
  }

  /// <include file="documentation.xml" path="//Geometry/Vector/Equals/node()"/>
  public override bool Equals(object obj) { return obj is Vector3 && Equals((Vector3)obj); }
  /// <include file="documentation.xml" path="//Geometry/Vector/Equals2/node()"/>
  public bool Equals(Vector3 other)
  {
    return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
  }
  /// <include file="documentation.xml" path="//Geometry/Vector/Equals3/node()"/>
  public bool Equals(Vector3 vect, double epsilon)
  {
    return Math.Abs(vect.X-X)<=epsilon && Math.Abs(vect.Y-Y)<=epsilon && Math.Abs(vect.Z-Z)<=epsilon;
  }

  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public unsafe override int GetHashCode()
  {
    return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
  }
  /// <summary>Converts this <see cref="Vector3"/> into an equivalent <see cref="Point3"/>.</summary>
  /// <returns>Returns a <see cref="Point3"/> with X, Y, and Z coordinates corresponding to the X, Y, and Z magnitudes
  /// of this vector.
  /// </returns>
  public Point3 ToPoint() { return new Point3(X, Y, Z); }
  /// <summary>Converts this vector into a human-readable string.</summary>
  /// <returns>A human-readable string representation of this vector.</returns>
  public override string ToString() { return string.Format("[{0:f2},{1:f2},{2:f2}]", X, Y, Z); }

  /// <summary>The magnitude of this vector along the X axis.</summary>
  public double X;
  /// <summary>The magnitude of this vector along the Y axis.</summary>
  public double Y;
  /// <summary>The magnitude of this vector along the Z axis.</summary>
  public double Z;

  /// <summary>Returns a new vector with the magnitudes negated.</summary>
  public static Vector3 operator-(Vector3 v) { return new Vector3(-v.X, -v.Y, -v.Z); }
  /// <summary>Returns a new vector with the magnitudes equal to the sums of the operand's magnitudes.</summary>
  public static Vector3 operator+(Vector3 a, Vector3 b) { return new Vector3(a.X+b.X, a.Y+b.Y, a.Z+b.Z); }
  /// <summary>Returns a new vector with the magnitudes equal to the differences between the operand's magnitudes.</summary>
  public static Vector3 operator-(Vector3 a, Vector3 b) { return new Vector3(a.X-b.X, a.Y-b.Y, a.Z-b.Z); }
  /// <summary>Returns a new vector with all magnitudes multiplied by a scalar value.</summary>
  public static Vector3 operator*(Vector3 v, double f) { return new Vector3(v.X*f, v.Y*f, v.Z*f); }
  /// <summary>Returns a new vector with all magnitudes multiplied by a scalar value.</summary>
  public static Vector3 operator*(double f, Vector3 v) { return new Vector3(v.X*f, v.Y*f, v.Z*f); }
  /// <summary>Returns a new vector with all magnitudes divided by a scalar value.</summary>
  public static Vector3 operator/(Vector3 v, double f) { return new Vector3(v.X/f, v.Y/f, v.Z/f); }
  /// <summary>Determines whether two vectors are exactly equal.</summary>
  public static bool operator==(Vector3 a, Vector3 b) { return a.X==b.X && a.Y==b.Y && a.Z==b.Z; }
  /// <summary>Determines whether two vectors are not exactly equal.</summary>
  public static bool operator!=(Vector3 a, Vector3 b) { return a.X!=b.X || a.Y!=b.Y || a.Z!=b.Z; }

  /// <summary>Returns a vector having <see cref="double.NaN"/> for all of the components.</summary>
  public static readonly Vector3 Invalid = new Vector3(double.NaN, double.NaN, double.NaN);

  /// <summary>Returns a vector with zero for all components.</summary>
  public static readonly Vector3 Zero = new Vector3();
  /// <summary>Returns a vector with an X value of 1.</summary>
  public static readonly Vector3 X1 = new Vector3(1, 0, 0);
  /// <summary>Returns a vector with an Y value of 1.</summary>
  public static readonly Vector3 Y1 = new Vector3(0, 1, 0);
  /// <summary>Returns a vector with an Z value of 1.</summary>
  public static readonly Vector3 Z1 = new Vector3(0, 0, 1);
  /// <summary>Returns a vector with an X value of -1.</summary>
  public static readonly Vector3 XM1 = new Vector3(-1, 0, 0);
  /// <summary>Returns a vector with an Y value of -1.</summary>
  public static readonly Vector3 YM1 = new Vector3(0, -1, 0);
  /// <summary>Returns a vector with an Z value of -1.</summary>
  public static readonly Vector3 ZM1 = new Vector3(0, 0, -1);
}
#endregion

#region Point3
/// <summary>This structure represents a point in three-dimensional space.</summary>
[Serializable]
public struct Point3
{
  /// <summary>Initializes this <see cref="Point3"/> from a set of coordinates.</summary>
  /// <param name="x">The point's X coordinate.</param>
  /// <param name="y">The point's Y coordinate.</param>
  /// <param name="z">The point's Z coordinate.</param>
  public Point3(double x, double y, double z) { X=x; Y=y; Z=z; }

  /// <include file="documentation.xml" path="//Geometry/Point/DistanceTo/node()"/>
  public double DistanceTo(Point3 point)
  {
    double xd=point.X-X, yd=point.Y-Y, zd=point.Z-Z;
    return Math.Sqrt(xd*xd+yd*yd+zd*zd);
  }
  /// <include file="documentation.xml" path="//Geometry/Point/DistanceSquaredTo/node()"/>
  public double DistanceSquaredTo(Point3 point)
  {
    double xd=point.X-X, yd=point.Y-Y, zd=point.Z-Z;
    return xd*xd+yd*yd+zd*zd;
  }
  /// <summary>Offsets this point by a given amount, translating it in space.</summary>
  /// <param name="xd">The value to add to the point's X coordinate.</param>
  /// <param name="yd">The value to add to the point's Y coordinate.</param>
  /// <param name="zd">The value to add to the point's Z coordinate.</param>
  public void Offset(double xd, double yd, double zd) { X+=xd; Y+=yd; Z+=zd; }

  /// <include file="documentation.xml" path="//Geometry/Point/Equals/node()"/>
  public override bool Equals(object obj) { return obj is Point3 && (Point3)obj==this; }
  /// <include file="documentation.xml" path="//Geometry/Point/Equals3/node()"/>
  public bool Equals(Point3 point, double epsilon)
  {
    return Math.Abs(point.X-X)<=epsilon && Math.Abs(point.Y-Y)<=epsilon && Math.Abs(point.Z-Z)<=epsilon;
  }
  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public unsafe override int GetHashCode()
  {
    return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
  }

  /// <summary>Returns a vector with the same components as the coordinates of the point.</summary>
  public Vector3 ToVector()
  {
    return new Vector3(X, Y, Z);
  }

  /// <summary>Converts this <see cref="Point3"/> into a human-readable string.</summary>
  /// <returns>A human-readable string representation of this <see cref="Point3"/>.</returns>
  public override string ToString() { return string.Format("({0:f2},{1:f2},{2:f2})", X, Y, Z); }

  /// <summary>Returns a <see cref="Vector3"/> representing the distance between the two points.</summary>
  public static Vector3 operator-(Point3 lhs, Point3 rhs) { return new Vector3(lhs.X-rhs.X, lhs.Y-rhs.Y, lhs.Z-rhs.Z); }
  /// <summary>Subtracts a <see cref="Vector3"/> from a point and returns the new point.</summary>
  public static Point3 operator-(Point3 lhs, Vector3 rhs) { return new Point3(lhs.X-rhs.X, lhs.Y-rhs.Y, lhs.Z-rhs.Z); }
  /// <summary>Adds a <see cref="Vector3"/> to a point and returns the new point.</summary>
  public static Point3 operator+(Point3 lhs, Vector3 rhs) { return new Point3(lhs.X+rhs.X, lhs.Y+rhs.Y, lhs.Z+rhs.Z); }
  /// <summary>Adds a <see cref="Vector3"/> to a point and returns the new point.</summary>
  public static Point3 operator+(Vector3 lhs, Point3 rhs) { return new Point3(lhs.X+rhs.X, lhs.Y+rhs.Y, lhs.Z+rhs.Z); }
  /// <summary>Determines whether two points are exactly equal.</summary>
  public static bool operator==(Point3 lhs, Point3 rhs) { return lhs.X==rhs.X && lhs.Y==rhs.Y && lhs.Z==rhs.Z; }
  /// <summary>Determines whether two points are not exactly equal.</summary>
  public static bool operator!=(Point3 lhs, Point3 rhs) { return lhs.X!=rhs.X || lhs.Y!=rhs.Y || lhs.Z!=rhs.Z; }

  /// <summary>This point's X coordinate.</summary>
  public double X;
  /// <summary>This point's Y coordinate.</summary>
  public double Y;
  /// <summary>This point's Z coordinate.</summary>
  public double Z;

  /// <summary>A point with all zero components.</summary>
  public static readonly Point3 Empty = new Point3();
}
#endregion

#region Line3
/// <summary>This structure represents a line or line segment in three dimensions.</summary>
[Serializable]
public struct Line3
{
  /// <summary>Initializes this line from a point's coordinates and a vector's axis magnitudes.</summary>
  /// <param name="x">The X coordinate of a point on the line (or the start of the line segment).</param>
  /// <param name="y">The Y coordinate of a point on the line (or the start of the line segment).</param>
  /// <param name="z">The Z coordinate of a point on the line (or the start of the line segment).</param>
  /// <param name="xd">The magnitude along the X axis of the line's direction. If you're defining a line segment,
  /// this should be the distance from <paramref name="x"/> to the X coordinate of the endpoint.
  /// </param>
  /// <param name="yd">The magnitude along the Y axis of the line's direction. If you're defining a line segment,
  /// this should be the distance from <paramref name="y"/> to the Y coordinate of the endpoint.
  /// </param>
  /// <param name="zd">The magnitude along the Z axis of the line's direction. If you're defining a line segment,
  /// this should be the distance from <paramref name="z"/> to the Z coordinate of the endpoint.
  /// </param>
  public Line3(double x, double y, double z, double xd, double yd, double zd)
  {
    Start=new Point3(x, y, z); Vector=new Vector3(xd, yd, zd);
  }
  /// <include file="documentation.xml" path="//Geometry/Line/Line/node()"/>
  public Line3(Point3 start, Vector3 vector) { Start=start; Vector=vector; }
  /// <include file="documentation.xml" path="//Geometry/Line/Line2/node()"/>
  public Line3(Point3 start, Point3 end) { Start=start; Vector=end-start; }
  /// <summary>Returns the endpoint of the line segment.</summary>
  /// <remarks>This is equivalent to <see cref="Start"/> + <see cref="Vector"/>.</remarks>
  public Point3 End { get { return Start+Vector; } }
  /// <summary>Calculates and returns the line segment's length.</summary>
  /// <remarks>This returns the length of <see cref="Vector"/>.</remarks>
  public double Length { get { return Vector.Length; } }
  /// <summary>Calculates and returns the square of the line segment's length.</summary>
  /// <remarks>This returns the square of the length of <see cref="Vector"/>.</remarks>
  public double LengthSqr { get { return Vector.LengthSqr; } }
  /// <include file="documentation.xml" path="//Geometry/Line/GetPoint/node()"/>
  public Point3 GetPoint(int point)
  {
    if(point<0 || point>1) throw new ArgumentOutOfRangeException("point", point, "must be 0 or 1");
    return point==0 ? Start : End;
  }
  /// <include file="documentation.xml" path="//Geometry/Line/Equals/node()"/>
  public override bool Equals(object obj) { return obj is Line3 && Equals((Line3)obj); }
  /// <include file="documentation.xml" path="//Geometry/Line/Equals2/node()"/>
  public bool Equals(Line3 other)
  {
    return Start.Equals(other.Start) && Vector.Equals(other.Vector);
  }
  /// <include file="documentation.xml" path="//Geometry/Line/Equals3/node()"/>
  public bool Equals(Line3 line, double epsilon)
  {
    return Start.Equals(line.Start, epsilon) && Vector.Equals(line.Vector, epsilon);
  }
  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public override int GetHashCode() { return Start.GetHashCode() ^ Vector.GetHashCode(); }
  /// <summary>Converts this <see cref="Line3"/> into a human-readable string.</summary>
  /// <returns>A human-readable string representing this line.</returns>
  public override string ToString() { return string.Format("{0}->{1}", Start, Vector); }
  /// <summary>Creates a <see cref="Line3"/> from two points.</summary>
  /// <param name="x1">The X coordinate of the first point (a point on the line, or the start of the line segment).</param>
  /// <param name="y1">The Y coordinate of the first point (a point on the line, or the start of the line segment).</param>
  /// <param name="z1">The Z coordinate of the first point (a point on the line, or the start of the line segment).</param>
  /// <param name="x2">The X coordinate of the second point (another point on the line, or the end of the line
  /// segment).
  /// </param>
  /// <param name="y2">The Y coordinate of the second point (another point on the line, or the end of the line
  /// segment).
  /// </param>
  /// <param name="z2">The Z coordinate of the second point (another point on the line, or the end of the line
  /// segment).
  /// </param>
  /// <returns>A <see cref="Line3"/> initialized with those values.</returns>
  /// <remarks>Since the end point will need to be converted into a vector, some miniscule accuracy may be lost.
  /// Most notably, the <see cref="End"/> property may not be exactly equal to the given endpoint.
  /// </remarks>
  public static Line3 FromPoints(double x1, double y1, double z1, double x2, double y2, double z2)
  {
    return new Line3(x1, y1, z1, x2-x1, y2-y1, z2-z1);
  }

  /// <summary>Determines whether two line segments are exactly equal.</summary>
  public static bool operator==(Line3 lhs, Line3 rhs) { return lhs.Start==rhs.Start && lhs.Vector==rhs.Vector; }
  /// <summary>Determines whether two line segments are not exactly equal.</summary>
  public static bool operator!=(Line3 lhs, Line3 rhs) { return lhs.Start!=rhs.Start || lhs.Vector!=rhs.Vector; }

  /// <summary>A point on the line, or the start point of the line segment.</summary>
  public Point3 Start;
  /// <summary>The line's direction, or the vector from the start point to the end point of the line segment.</summary>
  public Vector3 Vector;
}
#endregion

#region Sphere
/// <summary>This structure represents a sphere.</summary>
[Serializable]
public struct Sphere
{
  /// <summary>Initializes this sphere from a center point and a radius.</summary>
  /// <param name="centerX">The X coordinate of the sphere's center point.</param>
  /// <param name="centerY">The Y coordinate of the sphere's center point.</param>
  /// <param name="centerZ">The Z coordinate of the sphere's center point.</param>
  /// <param name="radius">The radius of the sphere.</param>
  public Sphere(double centerX, double centerY, double centerZ, double radius)
  {
    Center=new Point3(centerX, centerY, centerZ); Radius=radius;
  }
  /// <summary>Initializes this sphere from a center point and a radius.</summary>
  /// <param name="center">The sphere's center point.</param>
  /// <param name="radius">The radius of the sphere.</param>
  public Sphere(Point3 center, double radius) { Center=center; Radius=radius; }

  /// <summary>Calculates and returns the area of the sphere.</summary>
  public double Volume { get { return Radius*Radius*Radius*Math.PI*4/3; } }

  /// <summary>Determines whether the given point is contained within the sphere.</summary>
  /// <param name="point">The <see cref="Point3"/> to test for containment.</param>
  /// <returns>Returns true if <paramref name="point"/> is contained within this sphere.</returns>
  public bool Contains(Point3 point) { return (point-Center).LengthSqr < Radius*Radius; }

  /// <summary>Determines whether the object is a <see cref="Sphere"/> exactly equal to this one.</summary>
  public override bool Equals(object obj)
  {
    return obj is Sphere && Equals((Sphere)obj);
  }

  /// <summary>Determines whether the given <see cref="Sphere"/> exactly equal to this one.</summary>
  public bool Equals(Sphere other)
  {
    return Center.Equals(other.Center) && Radius.Equals(other.Radius);
  }

  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public override int GetHashCode()
  {
    return Center.GetHashCode() ^ Radius.GetHashCode();
  }

  /// <summary>The center point of this sphere.</summary>
  public Point3 Center;
  /// <summary>The radius of this sphere.</summary>
  public double Radius;

  /// <summary>Determines whether two spheres are exactly equal.</summary>
  public static bool operator==(Sphere a, Sphere b)
  {
    return a.Radius == b.Radius && a.Center == b.Center;
  }

  /// <summary>Determines whether two spheres are not exactly equal.</summary>
  public static bool operator!=(Sphere a, Sphere b)
  {
    return a.Radius != b.Radius || a.Center != b.Center;
  }
}
#endregion

#pragma warning disable 1591
#region Quaternion
[Serializable]
public struct Quaternion
{
  public Quaternion(Vector3 v) { W=0; V=v; }
  public Quaternion(double w, Vector3 v) { W=w; V=v; }
  public Quaternion(double w, double x, double y, double z) { W=w; V=new Vector3(x, y, z); }
  public Quaternion(Vector3 axis, double angle)
  {
    angle *= 0.5;
    W=Math.Cos(angle); V=axis*Math.Sin(angle);
  }

  public Quaternion Conjugate { get { return new Quaternion(W, -V); } }

  public double Length
  {
    get { return Math.Sqrt(LengthSquared); }
    set { Normalize(value); }
  }

  public double LengthSquared
  {
    get { return V.X*V.X + V.Y*V.Y + V.Z*V.Z + W*W; }
  }

  public Quaternion Normal { get { return this/Length; } }

  /// <summary>Determines whether the object is a <see cref="Quaternion"/> exactly equal to this one.</summary>
  public override bool Equals(object obj) { return obj is Quaternion && Equals((Quaternion)obj); }

  /// <summary>Determines whether the object is a <see cref="Quaternion"/> exactly equal to this one.</summary>
  public bool Equals(Quaternion other)
  {
    return V.Equals(other.V) && W.Equals(other.W);
  }

  /// <summary>Determines whether the object is a <see cref="Quaternion"/> approximately equal to this one.</summary>
  public bool Equals(Quaternion q, double epsilon) { return Math.Abs(W-q.W)<=epsilon && V.Equals(q.V, epsilon); }

  public void GetAxisAngle(out Vector3 axis, out double angle)
  {
    double scale = V.LengthSqr;
    if(scale==0) { axis=new Vector3(0, 0, 1); angle=0; }
    else
    {
      scale = Math.Sqrt(scale);
      axis  = new Vector3(V.X/scale, V.Y/scale, V.Z/scale);
      angle = Math.Acos(W)*2;
    }
  }

  /// <include file="documentation.xml" path="//Common/GetHashCode/node()"/>
  public unsafe override int GetHashCode()
  {
    return V.GetHashCode() ^ W.GetHashCode();
  }

  /// <summary>Normalizes the <see cref="Quaternion"/> to a length of one.</summary>
  public void Normalize() { this /= Length; }

  /// <summary>Normalizes the <see cref="Quaternion"/> to the given length.</summary>
  public void Normalize(double length) { this *= length/Length; }

  public Matrix3 ToMatrix3()
  {
    double xx=V.X*V.X, xy=V.X*V.Y, xz=V.X*V.Z, xw=V.X*W, yy=V.Y*V.Y, yz=V.Y*V.Z, yw=V.Y*W, zz=V.Z*V.Z, zw=V.Z*W;
    Matrix3 ret = new Matrix3();
    ret.M00 = 1 - (yy+zz)*2; ret.M01 =     (xy-zw)*2; ret.M02 =     (xz+yw)*2;
    ret.M10 =     (xy+zw)*2; ret.M11 = 1 - (xx+zz)*2; ret.M12 =     (yz-xw)*2;
    ret.M20 =     (xz-yw)*2; ret.M21 =     (yz+xw)*2; ret.M22 = 1 - (xx+yy)*2;
    return ret;
  }

  public Matrix4 ToMatrix4()
  {
    double xx=V.X*V.X, xy=V.X*V.Y, xz=V.X*V.Z, xw=V.X*W, yy=V.Y*V.Y, yz=V.Y*V.Z, yw=V.Y*W, zz=V.Z*V.Z, zw=V.Z*W;
    Matrix4 ret = new Matrix4();
    ret.M00 = 1 - (yy+zz)*2; ret.M01 =     (xy-zw)*2; ret.M02 =     (xz+yw)*2;
    ret.M10 =     (xy+zw)*2; ret.M11 = 1 - (xx+zz)*2; ret.M12 =     (yz-xw)*2;
    ret.M20 =     (xz-yw)*2; ret.M21 =     (yz+xw)*2; ret.M22 = 1 - (xx+yy)*2;
    ret.M33 = 1;
    return ret;
  }

  public double W;
  public Vector3 V;

  /// <summary>Determines whether two quaternions are exactly equal.</summary>
  public static bool operator==(Quaternion a, Quaternion b) { return a.W==b.W && a.V==b.V; }
  /// <summary>Determines whether two quaternions are not exactly equal.</summary>
  public static bool operator!=(Quaternion a, Quaternion b) { return a.W!=b.W || a.V!=b.V; }

  public static Quaternion operator*(Quaternion a, Quaternion b)
  {
    return new Quaternion(a.W*b.W   - a.V.X*b.V.X - a.V.Y*b.V.Y - a.V.Z*b.V.Z,
                          a.W*b.V.X + a.V.X*b.W   + a.V.Y*b.V.Z - a.V.Z*b.V.Y,
                          a.W*b.V.Y - a.V.X*b.V.Z + a.V.Y*b.W   + a.V.Z*b.V.X,
                          a.W*b.V.Z + a.V.X*b.V.Y - a.V.Y*b.V.X + a.V.Z*b.W);
  }

  public static Quaternion operator*(Quaternion a, double b) { return new Quaternion(a.W*b, a.V*b); }
  public static Quaternion operator/(Quaternion a, double b) { return new Quaternion(a.W/b, a.V/b); }
}
#endregion
#pragma warning restore 1591

} // namespace AdamMil.Mathematics.Geometry
