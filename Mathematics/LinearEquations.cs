/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

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

// these methods have been largely adapted from Numerical Recipes, 3rd edition

namespace AdamMil.Mathematics.LinearEquations
{
  #region GaussJordan
  /// <summary>Implements Gauss-Jordan elimination to solve systems of linear equations. This method also generates an inverse matrix as
  /// a side effect. If you don't need the inverse matrix, or if you want to find additional solutions on demand, or if you want the
  /// determinant or the upper or lower decomposition, you should use <see cref="LUDecomposition"/> instead. In general,
  /// <see cref="LUDecomposition"/> is superior. However, this class may be slightly more numerically stable.
  /// </summary>
  public static class GaussJordan
  {
    /// <summary>Inverts a matrix.</summary>
    public static Matrix Invert(Matrix matrix)
    {
      if((object)matrix == null) throw new ArgumentNullException();
      Matrix inverse;
      Solve(matrix, new Matrix(0, matrix.Height), out inverse, true);
      return inverse;
    }

    /// <summary>Solves a system of linear equations.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/*[@name != 'inverse']"/>
    public static Matrix Solve(Matrix coefficients, Matrix values)
    {
      Matrix inverse;
      return Solve(coefficients, values, out inverse, false);
    }

    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/*"/>
    public static Matrix Solve(Matrix coefficients, Matrix values, out Matrix inverse)
    {
      return Solve(coefficients, values, out inverse, true);
    }

    static Matrix Solve(Matrix coefficients, Matrix values, out Matrix inverse, bool wantInverse)
    {
      if((object)coefficients == null || (object)values == null) throw new ArgumentNullException();
      if(!coefficients.IsSquare) throw new ArgumentException("The coefficient matrix must be square.");
      if(values.Height != coefficients.Height)
      {
        throw new ArgumentException("The value matrix must have the same height as the coefficients matrix.");
      }

      // this is the most basic form of Gauss-Jordan elimination: say we have the following equations:
      // 2x + 3y + 4z = 20
      // 3x + 4y + 5z = 26
      //  x - 2y + 2z = 3
      //
      // then we have coefficient and value matrices, called "a" and "b" here, of:
      // | 2  3  4 |         | 20 |
      // | 3  4  5 |   and   | 26 |
      // | 1 -2  2 |         |  3 |
      //
      // now we iterate over the columns. for column i, we:
      //   1. divide the entire row i in both matrices by a_ii
      //   2. for each other row k, then subtract row i times a_ki from row k. this results in a_ki becoming zero
      //   as a result, the column i has a 1 in row i and zeros in all other rows. this matches the form of an identity matrix for that
      //   column
      // after iterating over all columns, the coefficients matrix is changed into an identity matrix and the values matrix contains the
      // solutions. applied to the matrices above, we would do the following in the first iteration:
      //   * divide the first row by a_00 (which is 2), producing:  | 1 3/2 2 |  and  | 10 |
      //   * subtract 3 * row 0 from row 1 and 1 * row 0 from row 2
      //     | 1  3/2  2 |         | 10 |
      //     | 0 -1/2 -1 |   and   | -4 |
      //     | 0 -7/2  0 |         | -7 |
      // the other two iterations produce:
      //   | 1  0  -1 |         | -2 |                | 1  0  0 |   and   | 1 |
      //   | 0  1   2 |   and   |  8 |    and then    | 0  1  0 |   and   | 2 |
      //   | 0  0   7 |         | 21 |                | 0  0  1 |   and   | 3 |
      // so x=1, y=2, and z=3.
      //
      // it's useful to generate the inverse of the coefficient matrix at the same time. this can be done by adding a third matrix of the
      // same size as the coefficients matrix, initially containing an identity matrix, which is treated exactly like the values matrix. as
      // the algorithm progresses, the coefficients matrix will transform into the identity matrix and the identity matrix will transform
      // into the inverse matrix. in fact, since for each corresponding element of the two matrices, one of them will predictably be either
      // a 0 or 1, we don't need a separate matrix but can mutate the coefficient matrix into its inverse rather than mutating it into the
      // identity matrix
      // 
      // now it happens to be the case that this basic form of the algorithm is numerically unstable due to rounding error. therefore the
      // concept of "pivoting" is added to stabilize it. pivoting is essentially finding the best value to divide by in each iteration and
      // swapping rows and colums of the matrix to place that value at a_ii. to avoid messing up the part of the matrix that we've already
      // processed (and converted into identity form), we can choose from any value to the right of and below a_ii (or a_ii itself).
      // swapping rows has no effect on the solution, but swapping columns does, so column swaps have to be reversed at the end. the
      // criteria for choosing the best value to divide by remain undiscovered, but it works well in practice to divide by the coefficient
      // having the largest magnitude. (this makes the behavior dependent on the original scaling of the equations, which may be
      // undesirable in some rare cases, but that is not addressed in this implementation.)

      // clone the matrices since we'll be modifying them
      coefficients = coefficients.Clone();
      values = values.Clone();

      bool[] processed = new bool[coefficients.Height]; // stores whether a particular row or column has been processed yet
      int[] pivotColumns = new int[coefficients.Height], pivotRows = new int[coefficients.Height]; // store the pivot point for each column
      for(int x=0; x<coefficients.Height; x++) // for each column in the coefficients matrix...
      {
        // find the pivot by searching the area to the right and below for the element with the largest magnitude. rather than actually
        // swapping columns, though, we merely change the order of access
        double maxCoefficient = 0, value;
        int pivotX = 0, pivotY = 0; // these will be set to the position of the pivot element
        for(int py=0; py<coefficients.Width; py++) // for each column...
        {
          if(!processed[py]) // if this row hasn't been processed already...
          {
            for(int px=0; px<coefficients.Height; px++)
            {
              if(!processed[px]) // if this column hasn't been processed already...
              {
                value = Math.Abs(coefficients[py, px]);
                if(value >= maxCoefficient) // find the coefficient with the greatest magnitude
                {
                  maxCoefficient = value;
                  pivotX = px;
                  pivotY = py;
                }
              }
            }
          }
        }

        // mark this column as having been processed. the row will be implicitly marked by the fact that we'll swap the row if
        // pivotX != pivotY
        processed[pivotX] = true;

        if(pivotX != pivotY) // if the pivot element is not in the correct position vertically, then swap the current row and the pivot row
        {
          for(int i=0; i<coefficients.Width; i++) coefficients.Swap(pivotY, i, pivotX, i);
          for(int i=0; i<values.Width; i++) values.Swap(pivotY, i, pivotX, i);
        }

        // store the location of the pivot element for this column so we can undo the swaps later
        pivotColumns[x] = pivotX;
        pivotRows[x]    = pivotY;

        // because of the swaps (done above if pivotX != pivotY), the pivot element is on the diagonal (at pivotX, pivotX) now.
        // we can't reuse maxCoefficient because it contains only the magnitude, but we need the sign as well
        value = coefficients[pivotX, pivotX];
        if(value == 0) throw new ArgumentException("The coefficient matrix is singular.");
        double inversePivot = 1 / value; // we'll multiply by the reciprocal rather than dividing

        // divide the row by the pivot, except the pivot element itself, which we'll set to 1 first. this is part of generating the inverse
        // matrix, since the identity matrix that we'd be transforming into the inverse matrix in the would have had a 1 there
        coefficients[pivotX, pivotX] = 1;
        for(int i=0; i<coefficients.Width; i++) coefficients[pivotX, i] *= inversePivot;
        for(int i=0; i<values.Width; i++) values[pivotX, i] *= inversePivot;

        // subtract linear combinations of the pivot row from all other rows
        for(int i=0; i<coefficients.Height; i++)
        {
          if(i != pivotX) // if this isn't the pivot row...
          {
            // get the factor that we need to multiply the pivot row before subtracting it in order to cancel out the value in the pivot
            // column of this row. then we'll set that column to zero before the subtraction or order to help generate the inverse matrix,
            // since the identity matrix would have had a zero there
            value = coefficients[i, pivotX];
            coefficients[i, pivotX] = 0;
            for(int j=0; j<coefficients.Width; j++) coefficients[i, j] -= coefficients[pivotX, j] * value;
            for(int j=0; j<values.Width; j++) values[i, j] -= values[pivotX, j] * value;
          }
        }
      }

      // at this point, the solutions and inverse matrices have been generated, but the columns of the inverse matrix may be out of order
      if(wantInverse) // if we actually want the inverse matrix...
      {
        for(int i=coefficients.Width-1; i >= 0; i--) // go through the columns in reverse order
        {
          int pivotX = pivotColumns[i], pivotY = pivotRows[i];
          if(pivotX != pivotY) // if this column has an implied column swap, we have to perform it now
          {
            for(int j=0; j<coefficients.Height; j++) coefficients.Swap(j, pivotY, j, pivotX);
          }
        }
      }

      inverse = coefficients;
      return values;
    }
  }
  #endregion

  #region LUDecomposition
  /// <summary>Implements LU decomposition to solve systems of linear equations. This method is generally faster than
  /// <see cref="GaussJordan">Gauss-Jordan elimination</see> and has the benefit that once the decomposition is created, you can solve
  /// additional sets of values on demand. This class can also compute the determinant and inverse of the matrix, although if you only want
  /// the inverse matrix, it may be slightly better to use <see cref="GaussJordan">Gauss-Jordan elimination</see>.
  /// </summary>
  /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
  public sealed class LUDecomposition
  {
    /// <summary>Initializes a new <see cref="LUDecomposition"/> with a square, invertible matrix. If used to solve linear equations, the
    /// matrix represents the left side of the equations, where the rows represent the individual equations and the columns represent the
    /// coefficients in the equations.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
    public LUDecomposition(Matrix coefficients)
    {
      if((object)coefficients == null) throw new ArgumentNullException();
      if(!coefficients.IsSquare) throw new ArgumentException("The coefficient matrix must be square.");
      this.matrix = coefficients.Clone();
    }

    /// <summary>Gets the decomposition of the coefficient matrix.</summary>
    /// <remarks>
    /// LU decomposition normally decomposes a matrix A
    /// <code>
    /// | a b c |
    /// | d e f |
    /// | g h i |
    /// </code>
    /// into two matrices L and U, which are lower and upper triangular matrices where A = L*U:
    /// <code>
    /// | n 0 0 |       | u v w |
    /// | p q 0 |  and  | 0 x y |
    /// | r s t |       | 0 0 z |
    /// </code>
    /// As an optimization, this class packs both triangular matrices into a single matrix:
    /// <code>
    /// | u v w |
    /// | p x y |
    /// | r s z |
    /// </code>
    /// Where the elements along the diagonal of L (n, p, and t in the example) are not stored, and are implicitly equal to be 1.
    /// </remarks>
    public Matrix GetDecomposition()
    {
      EnsureDecomposition();
      return matrix.Clone();
    }

    /// <summary>Gets the determinant of the coefficient matrix. For sizeable matrices, the determinant may be larger than the dynamic
    /// range of the <see cref="double"/> type. In that case, you may want to use <see cref="GetLogDeterminant"/> to get the logarithm of
    /// the determinant.
    /// </summary>
    public double GetDeterminant()
    {
      // the determinant of an LU decomposition is the product of the diagonal elements of L and U. since the diagonal elements of L are
      // implicitly equal to 1, we can they contribute nothing. this leaves the diagonal of U, which is the diagonal of our decomposition.
      // (also, according to wikipedia, the determinant of any triangular matrix is the product of the elements on the diagonal.)
      EnsureDecomposition();
      double product = oddSwapCount ? -1 : 1;
      for(int i=0; i<matrix.Height; i++) product *= matrix[i, i];
      return product;
    }

    /// <summary>Gets the inverse of the coefficient matrix.</summary>
    public Matrix GetInverse()
    {
      if(inverse == null) inverse = Solve(Matrix.CreateIdentity(matrix.Height), true);
      return inverse.Clone();
    }

    /// <summary>Gets the natural logarithm of the absolute value of the determinant of the coefficient matrix. This is useful because for
    /// sizeable matrices, the determinant itself would be too large to fit within the dynamic range of the <see cref="double"/> type.
    /// </summary>
    /// <param name="negative">A variable that will be set to true if the determinant is negative and false if it is positive.</param>
    public double GetLogDeterminant(out bool negative)
    {
      EnsureDecomposition();

      double sum = 0;
      negative = oddSwapCount;
      for(int i=0; i<matrix.Height; i++)
      {
        double value = matrix[i, i];
        if(value < 0)
        {
          negative = !negative;
          value = -value;
        }
        sum += Math.Log(value);
      }
      return sum;
    }

    /// <summary>Solves the set of linear equations passed to the constructor with the given right-hand sides.</summary>
    /// <param name="values">A matrix of the same height as the coefficient matrix passed to the constructor, where each column contains
    /// the set of sums of the equation terms (i.e. what the equation equals).
    /// </param>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
    public Matrix Solve(Matrix values)
    {
      if((object)values == null) throw new ArgumentNullException();
      if(values.Height != matrix.Height)
      {
        throw new ArgumentException("The value matrix must have the same height as the coefficient matrix.");
      }

      return Solve(values, false);
    }

    /// <summary>Performs the LU decomposition if it hasn't been done yet.</summary>
    void EnsureDecomposition()
    {
      // LU decomposition works by decomposing a matrix A into two matrices L and U, which are lower and upper triangular matrices where
      // A = L*U:
      //
      //     | a b c |       | n 0 0 |       | u v w |
      // A = | d e f |   L = | p q 0 |   U = | 0 x y |
      //     | g h i |       | r s t |       | 0 0 z |
      // 
      // As an optimization, this class packs both triangular matrices into a single matrix:
      // | u v w |
      // | p x y |
      // | r s z |
      // where the diagonal of L is not stored, and is assumed to be all ones.
      //
      // To compute the decomposition, we'll use Crout's algorithm. This is an efficient way to compute the decomposition in place. If you
      // look at L and U above, multiply them, and examine which non-zero elements of L and U are accessed when computing the
      // multiplication, you'll see a pattern emerge. (Basically, elements closer to the top-left corner require fewer accesses, while
      // those closer to the bottom-right corner require more.) Crout's algorithm works by ordering the accesses such that the elements of
      // L and U being written can be computed based on a single value from A and the previous values written to L and U.
      //
      // To be numerically stable, pivoting must be added, as it was to Gauss-Jordan, above. (See that class for a description of
      // pivoting.) It is difficult to implement full pivoting efficiently, but we'll implement partial pivoting (where rows can be swapped
      // but not columns) and implicit pivoting (where when searching for the pivot, we treat the equations as though they were all scaled
      // so that their largest coefficient would have a magnitude of 1, in order to eliminate bias caused by the original scaling of the
      // equations and put them on more equal footing -- useful since the partial pivot search is much more limited in scope than a full
      // pivot search).
      if(!decomposed)
      {
        this.rowPermutation = new int[matrix.Height]; // keep track of rows swaps so we access them in the right order inside Solve()

        // this step relates to implicit pivoting. we'll compute a scale factor to be applied to each equation that would scale it so that
        // its largest coefficient would equal 1.
        double[] scale = new double[matrix.Height];
        for(int y=0; y<matrix.Height; y++) // for each equation...
        {
          double maxCoefficient = 0;
          for(int x=0; x<matrix.Width; x++) // find the coefficient with the largest magnitude
          {
            double value = Math.Abs(matrix[y, x]);
            if(value > maxCoefficient) maxCoefficient = value;
          }
          // if all coefficients were zero, that represents a row degeneracy (i think). in any case, it's singular and we can't solve it
          if(maxCoefficient == 0) throw new InvalidOperationException("The coefficient matrix is singular.");
          scale[y] = 1 / maxCoefficient; // store the scale factor for each row
        }

        // now we go through the matrix and compute the decomposition
        for(int k=0; k<matrix.Height; k++)
        {
          // first, we need to find the pivot value. since we're doing partial pivoting, we can only look below in the same column (k)
          double maxCoefficient = 0;
          int pivotRow = k;
          for(int y=k; y<matrix.Height; y++) // find the scaled coefficient with the largest magnitude and use that as the pivot element
          {
            double value = scale[y] * Math.Abs(matrix[y, k]);
            if(value > maxCoefficient)
            {
              maxCoefficient = value;
              pivotRow = y;
            }
          }

          // if the pivot element was in a row further down, then we'll swap the rows
          if(k != pivotRow)
          {
            for(int x=0; x<matrix.Width; x++) matrix.Swap(k, x, pivotRow, x);
            // keep track of whether the number of swaps was even or odd. swapping two rows or columns of a matrix negates its determinant.
            // so we use this in GetDeterminant() to determine whether we need to negate it back to get the correct value
            oddSwapCount = !oddSwapCount;
            // since the current row k was just swapped with the pivot row further down, we'll encounter it again later. when we do, we'll
            // need to have the correct scale value, so copy that too. we don't need to actually swap them, since we won't use the scale
            // for the pivot row after we move past it
            scale[pivotRow] = scale[k];
          }

          rowPermutation[k] = pivotRow; // keep track of the permutation so we can undo it later

          // now get the pivot element value
          double pivot = matrix[k, k];
          if(pivot == 0)
          {
            // i'm not quite sure what it means if the pivot value is zero. according to the algorithm, it would normally indicate a
            // singular matrix. however, it might just mean that floating point inaccuracy has caused an invertible matrix to become
            // singular. i'm not sure which is the case, but Numerical Recipes handles this case by replacing it with a very small value,
            // so we'll do the same. however, if it turns out that it indicates that the matrix really is singular, then i think it would
            // be better to throw an exception
            matrix[k, k] = 1e-40; // set the pivot to a very small value
            pivot = 1e+40; // then the inverse of the pivot becomes a very large value
          }
          else // otherwise, we have our pivot value. we'll invert it so we can replace later divisions with multiplication
          {
            pivot = 1 / pivot;
          }

          // finally, it's time to perform the decomposition. these are the two innermost loops of Crout's algorithm, as implemented here
          for(int i=k+1; i<matrix.Height; i++)
          {
            double factor = matrix[i, k] * pivot; // divide by the pivot element
            matrix[i, k] = factor;
            for(int j=k+1; j<matrix.Width; j++) matrix[i, j] -= factor * matrix[k, j];
          }
        }

        decomposed = true;
      }
    }

    Matrix Solve(Matrix values, bool inPlace)
    {
      // Solving a matrix based on an LU decomposition uses the idea of backsubstitution and forward substitution. these work as follows.
      // look above at the discussion of Gauss-Jordan elimination. imagine if instead of transforming the coefficient matrix into the
      // identity matrix, we only subtracted from the portion of the matrix below the diagonal. we would wind up with a partially
      // transformed upper triangular matrix and a partially solved value matrix:
      //
      //      | a b c |         | p |               | x |
      // A' = | 0 d e |    B' = | q |   (where A' * | y | = B')
      //      | 0 0 f |         | r |               | z |
      //
      // Given this, it's actually quite simple to complete the solution. The x, y, z vector above contains the unknowns. Because only one
      // element in the bottom row of A' is non-zero, we can solve for z immediately: z = r/f. (Because in the matrix multiplication,
      // r = 0*x + 0*y + f*z = f*z.) With z solved, we move onto y. Because q = 0*x + d*y + e*z = d*y + e*z, we can substitute r/f for z
      // and get q = d*y + e*r/f and solve for y: y = (q - e*r/f) / d. We can solve x similarly. This is called backsubstitution.
      // Forward substitution is exactly analogous, and works on a lower triangular matrix.
      //
      // The LU decomposition gives us upper and lower triangular matrices that are amenable to back and forward substitution. We need only
      // arrange the problem to combine the results appropriately. The standard linear equation formula is A·x = b, where A is the
      // coefficient matrix, b is the value vector, and x is a vector containing the unknowns. The goal is to solve for x. With LU
      // decomposition, we have A·x = (L·U)·x = b. Note that (L·U)·x = L·(U·x). We can then let y = U·x and get two equations:
      // U·x = y and L·y = b. These both correspond to the form used above for backsubstitution. So we first solve for y using forward
      // substitution and then we solve for x using backsubstitution.
      EnsureDecomposition();

      if(!inPlace) values = values.Clone(); // make a copy, since we'll be modifying it
      for(int x=0; x<values.Width; x++) // for each set of values (i.e. each column vector)...
      {
        // do the forward substitution. this transforms the column vector into the y vector from top to bottom. as an optimization, we'll
        // skip over leading zeroes in the column vector. this especially helps when GetInverse() is called, since it works by solving for
        // an identity matrix (which of course has lots of zeroes)
        int firstNonzeroIndex = -1; // the index of the first non-zero value in the column
        for(int y=0; y<values.Height; y++)
        {
          // we have to account for the row permutation caused by pivoting
          int rowIndex = rowPermutation[y];
          double sum = values[rowIndex, x];
          values[rowIndex, x] = values[y, x];

          if(firstNonzeroIndex != -1) // if we've found a non-zero element, solve it by forward substitution
          {
            for(int i=firstNonzeroIndex; i<y; i++) sum -= matrix[y, i] * values[i, x];
            values[y, x] = sum;
          }
          else if(sum != 0) // otherwise, if this is the first non-zero element...
          {
            values[y, x] = sum;
            firstNonzeroIndex = y;
          }
        }

        // now do the backsubstitution. this transforms the y vector into the x vector from bottom to top
        for(int y=values.Height-1; y >= 0; y--)
        {
          double sum = values[y, x];
          for(int i=y+1; i<values.Height; i++) sum -= matrix[y, i] * values[i, x];
          values[y, x] = sum / matrix[y, y];
        }
      }

      return values;
    }

    /// <summary>Before decomposition has been performed, this holds the coefficient matrix. Aftewards, it holds the LU decomposition.</summary>
    readonly Matrix matrix;
    /// <summary>Holds a copy of the matrix inverse, if the inverse has been generated.</summary>
    Matrix inverse;
    /// <summary>Represents the permutation that we performed during the pivot operation. rowPermutation[i] is the index of the row from
    /// which the pivot value was taken (and thus the row that row i was swapped with).
    /// </summary>
    int[] rowPermutation;
    bool decomposed, oddSwapCount;
  }
  #endregion
}