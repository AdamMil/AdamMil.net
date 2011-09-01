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
using System.Runtime.Serialization;

namespace AdamMil.Mathematics.LinearEquations
{
  #region SingularMatrixException
  /// <summary>An exception thrown when a matrix is singular and cannot be solved.</summary>
  [Serializable]
  public class SingularMatrixException : ArgumentException
  {
    /// <inheritdoc/>
    public SingularMatrixException(string message) : base(message) { }
    /// <inheritdoc/>
    public SingularMatrixException(string message, Exception innerException) : base(message, innerException) { }
    /// <inheritdoc/>
    public SingularMatrixException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
  #endregion

  #region ILinearEquationSolver
  /// <summary>Represents a class that can solve systems of linear equations.</summary>
  public interface ILinearEquationSolver
  {
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/GetInverse/*"/>
    Matrix GetInverse();
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Initialize/*"/>
    void Initialize(Matrix coefficients);
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve/*"/>
    Matrix Solve(Matrix values, bool tryInPlace);
  }
  #endregion

  #region GaussJordan
  /// <summary>Implements Gauss-Jordan elimination to solve systems of linear equations. This method also generates an inverse matrix as
  /// a side effect. If you don't need the inverse matrix, or if you want to find additional solutions on demand, or if you want the
  /// determinant or the upper or lower decomposition, you should use <see cref="LUDecomposition"/> instead. In general,
  /// <see cref="LUDecomposition"/> is superior. However, this class may be slightly more numerically stable and therefore slightly more
  /// accurate, although that is largely counteracted by the existence of the <see cref="LUDecomposition.RefineSolution"/> method.
  /// </summary>
  public sealed class GaussJordan : ILinearEquationSolver
  {
    /// <summary>Initializes a new <see cref="GaussJordan"/> solver. It is generally not necessary to create a <see cref="GaussJordan"/>
    /// object, as there are static methods to perform all of the same operations with less overhead.
    /// </summary>
    public GaussJordan() { }

    /// <summary>Initializes a new <see cref="GaussJordan"/> solver with the given matrix of coefficients. It is generally not necessary
    /// to create a <see cref="GaussJordan"/> object, as there are static methods to perform all of the same operations with less overhead.
    /// </summary>
    public GaussJordan(Matrix coefficients)
    {
      Initialize(coefficients);
    }

    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/GetInverse/*"/>
    public Matrix GetInverse()
    {
      AssertInitialized();
      return inverse == null ? Invert(coefficients) : inverse.Clone();
    }

    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Initialize/*"/>
    public void Initialize(Matrix coefficients)
    {
      Matrix.Assign(ref this.coefficients, coefficients);
      inverse = null;
    }

    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve1/*"/>
    public Matrix Solve(Matrix values)
    {
      return Solve(values, false);
    }

    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve/*"/>
    /// <remarks>Gauss-Jordan is always capable of solving in place, if requested.</remarks>
    public Matrix Solve(Matrix values, bool tryInPlace)
    {
      AssertInitialized();
      Matrix inverse, solution = Solve(coefficients, values, out inverse, this.inverse == null, tryInPlace);
      if(this.inverse == null) this.inverse = inverse;
      return solution;
    }

    /// <summary>Inverts a matrix.</summary>
    public static Matrix Invert(Matrix matrix)
    {
      if(matrix == null) throw new ArgumentNullException();
      Matrix inverse;
      Solve(matrix, new Matrix(matrix.Height, 0), out inverse, true, true);
      return inverse;
    }

    /// <summary>Solves a system of linear equations.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/*[@name != 'inverse' and @name != 'tryInPlace']"/>
    public static Matrix Solve(Matrix coefficients, Matrix values)
    {
      return Solve(coefficients, values, false);
    }

    /// <summary>Solves a system of linear equations.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/*[@name != 'inverse']"/>
    public static Matrix Solve(Matrix coefficients, Matrix values, bool tryInPlace)
    {
      Matrix inverse;
      return Solve(coefficients, values, out inverse, false, tryInPlace);
    }

    /// <summary>Solves a system of linear equations and returns the inverse matrix.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/*[@name != 'tryInPlace']"/>
    public static Matrix Solve(Matrix coefficients, Matrix values, out Matrix inverse)
    {
      return Solve(coefficients, values, out inverse, true, false);
    }

    /// <summary>Solves a system of linear equations and returns the inverse matrix.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/*"/>
    public static Matrix Solve(Matrix coefficients, Matrix values, out Matrix inverse, bool tryInPlace)
    {
      return Solve(coefficients, values, out inverse, true, tryInPlace);
    }

    void AssertInitialized()
    {
      if(coefficients == null) throw new InvalidOperationException("The solver has not been initialized.");
    }

    Matrix coefficients, inverse;

    static Matrix Solve(Matrix coefficients, Matrix values, out Matrix inverse, bool wantInverse, bool valuesInPlace)
    {
      if(coefficients == null || values == null) throw new ArgumentNullException();
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
      // concept of "pivoting" is added to stabilize it. (pivoting is also needed to avoid division by zero in the case of zero entries on
      // the diagonal.) pivoting is essentially finding the best value to divide by in each iteration and swapping rows and columns of the
      // matrix to place that value at a_ii. to avoid messing up the part of the matrix that we've already processed (and converted into
      // identity form), we can choose from any value to the right of and below a_ii (or a_ii itself). swapping rows has no effect on the
      // solution, but swapping columns does, so column swaps have to be reversed at the end. the criteria for choosing the best value to
      // divide by remain undiscovered, but it works well in practice to divide by the coefficient having the largest magnitude. (this
      // makes the behavior dependent on the original scaling of the equations, which may be undesirable in some rare cases, but that is
      // not addressed in this implementation.)

      // clone the matrices since we'll be modifying them
      coefficients = coefficients.Clone();
      if(!valuesInPlace) values = values.Clone();

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
          coefficients.SwapRows(pivotY, pivotX);
          values.SwapRows(pivotY, pivotX);
        }

        // store the location of the pivot element for this column so we can undo the swaps later
        pivotColumns[x] = pivotX;
        pivotRows[x]    = pivotY;

        // because of the swaps (done above if pivotX != pivotY), the pivot element is on the diagonal (at pivotX, pivotX) now.
        // we can't reuse maxCoefficient because it contains only the magnitude, but we need the sign as well
        value = coefficients[pivotX, pivotX];
        if(value == 0) throw new SingularMatrixException("The coefficient matrix is singular.");
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
  public sealed class LUDecomposition : ILinearEquationSolver
  {
    /// <summary>Initializes a new <see cref="LUDecomposition"/> with no matrix. <see cref="Initialize" /> can be called to provide a
    /// matrix to decompose.
    /// </summary>
    public LUDecomposition() { }

    /// <summary>Initializes a new <see cref="LUDecomposition"/> with a square, invertible matrix. If used to solve linear equations, the
    /// matrix represents the left side of the equations, where the rows represent the individual equations and the columns represent the
    /// coefficients in the equations.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
    public LUDecomposition(Matrix coefficients)
    {
      Initialize(coefficients);
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
      AssertDecomposition();
      return Solve(Matrix.CreateIdentity(matrix.Height), true);
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

    /// <summary>Decomposes a new matrix. The matrix must be square and invertible. If used to solve linear equations, the matrix
    /// represents the left side of the equations, where the rows represent the individual equations and the columns represent the
    /// coefficients in the equations.
    /// </summary>
    public void Initialize(Matrix coefficients)
    {
      if(coefficients == null) throw new ArgumentNullException();
      if(!coefficients.IsSquare) throw new ArgumentException("The coefficient matrix must be square.");
      Matrix.Assign(ref matrix, coefficients);
      this.coefficients = coefficients; // keep a reference to the coefficient matrix for use by RefineSolution()
      decomposed = false;
    }

    /// <summary>Improves a solution from <see cref="Solve"/>, assuming that the coefficient matrix given to the constructor (or to
    /// <see cref="Initialize"/>) has not changed since the solution was produced.
    /// </summary>
    /// <param name="values">The values matrix passed to <see cref="Solve"/> to produce the solution matrix.</param>
    /// <param name="solution">The solution matrix returned from <see cref="Solve"/>. This matrix will be changed to a refined solution,
    /// which is usually better than the original solution, and never worse.
    /// </param>
    /// <remarks>When solving large matrices, roundoff errors accumulate during the process and manifest as a loss of precision in the
    /// solution. This method can improve a solution to eliminate some of the accumulated roundoff error, and may be called
    /// multiple times to further refine the solution, although once is usually enough.
    /// The method requires the original coefficient matrix passed to the constructor or to <see cref="Initialize"/>. The
    /// <see cref="LUDecomposition"/> class saves a reference to that matrix, and does not make a copy of it. It is assumed, therefore,
    /// that the coefficient matrix has not been changed since the LU decomposition was constructed. If it has been changed, this method
    /// must not be used.
    /// </remarks>
    public void RefineSolution(Matrix values, Matrix solution)
    {
      if(values == null || solution == null) throw new ArgumentNullException();
      if(values.Height != solution.Height || values.Width != solution.Width)
      {
        throw new ArgumentException("The value matrix must have the same size as the solution matrix.");
      }

      EnsureDecomposition();
      if(values.Height != matrix.Height)
      {
        throw new ArgumentException("The value matrix must have the same height as the coefficient matrix.");
      }

      Matrix errors = new Matrix(solution.Height, solution.Width);
      for(int x=0; x<solution.Width; x++) // for each solution vector...
      {
        for(int y=0; y<solution.Height; y++)
        {
          double error = -values[y, x]; // TODO: if possible, use a higher precision data type to do the multiply and accumulate the error
          for(int i=0; i<solution.Height; i++) error += coefficients[y, i] * solution[i, x];
          errors[y, x] = error;
        }
      }

      Solve(errors, true);
      for(int y=0; y<solution.Height; y++)
      {
        for(int x=0; x<solution.Width; x++) solution[y, x] -= errors[y, x];
      }
    }

    /// <summary>Solves the set of linear equations passed to the constructor with the given right-hand sides.</summary>
    /// <param name="values">A matrix of the same height as the coefficient matrix passed to the constructor, where each column contains
    /// a set of right-hand side values of the equations.
    /// </param>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
    public Matrix Solve(Matrix values)
    {
      return Solve(values, false);
    }

    /// <summary>Solves the set of linear equations passed to the constructor with the given right-hand sides.</summary>
    /// <param name="values">A matrix of the same height as the coefficient matrix passed to the constructor, where each column contains
    /// a set of right-hand side values of the equations.
    /// </param>
    /// <param name="inPlace">If true, the matrix will be solved in place and returned directly. If false, the solution will be returned
    /// in a new matrix.
    /// </param>
    /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
    public Matrix Solve(Matrix values, bool inPlace)
    {
      if(values == null) throw new ArgumentNullException();

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
      if(values.Height != matrix.Height)
      {
        throw new ArgumentException("The value matrix must have the same height as the coefficient matrix.");
      }

      if(!inPlace) values = values.Clone(); // make a copy, since we'll be modifying it
      for(int x=0; x<values.Width; x++) // for each set of values (i.e. each column vector)...
      {
        // do the forward substitution. this transforms the column vector into the y vector from top to bottom. as an optimization, we'll
        // skip over leading zeroes in the column vector. this especially helps when GetInverse() is called, since it works by solving for
        // an identity matrix (which of course has lots of zeroes)
        int firstNonzeroIndex = -1; // the index of the first non-zero value in the column
        for(int y=0; y<values.Height; y++)
        {
          int rowIndex = rowPermutation[y]; // we have to account for the row permutation caused by pivoting
          double sum = values[rowIndex, x];
          values[rowIndex, x] = values[y, x];

          if(firstNonzeroIndex != -1) // if we've found a non-zero element, solve it by forward substitution
          {
            for(int i=firstNonzeroIndex; i<y; i++) sum -= matrix[y, i] * values[i, x];
          }
          else if(sum != 0) // otherwise, if this is the first non-zero element...
          {
            firstNonzeroIndex = y;
          }
          values[y, x] = sum;
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

    void AssertDecomposition()
    {
      if(matrix == null) throw new InvalidOperationException("No matrix has been decomposed yet.");
    }

    /// <summary>Performs the LU decomposition if it hasn't been done yet.</summary>
    void EnsureDecomposition()
    {
      // LU decomposition works by decomposing a matrix A into two matrices L and U, which are lower and upper triangular matrices where
      // A = L*U:
      //
      //     | a b c |       | n 0 0 |       | u v w |            | nu nv    nw       |
      // A = | d e f |   L = | p q 0 |   U = | 0 x y |   Thus A = | pu pv+qx pw+qy    |
      //     | g h i |       | r s t |       | 0 0 z |            | ru rv+sx rw+sy+tz |
      //
      // As an optimization, this class packs both triangular matrices into a single matrix, where the diagonal of L is not stored, and is
      // assumed to be all ones. Note that the matrices below do not take into account pivoting, discussed below.
      // | u v w |             | 1 0 0 |       | u v w |            | u  v     w       |
      // | p x y |  where  L = | p 1 0 |   U = | 0 x y |   Thus A = | pu pv+x  pw+y    |
      // | r s z |             | r s 1 |       | 0 0 z |            | ru rv+sx rw+sy+z |
      //
      // To compute the decomposition, we'll use Crout's algorithm. This is an efficient way to compute the decomposition in place. If you
      // look at L and U above, multiply them, and examine which non-zero elements of L and U are accessed when computing the
      // multiplication, you'll see a pattern emerge. (Basically, elements closer to the top-left corner require fewer accesses, while
      // those closer to the bottom-right corner require more.) Crout's algorithm works by ordering the accesses such that the elements of
      // L and U being written can be computed based on a single value from A and the previous values written to L and U.
      //
      // To be numerically stable, pivoting must be added, as it was to Gauss-Jordan, above. (See that class for a description of
      // pivoting.) It is difficult to implement full pivoting efficiently, but we'll implement partial pivoting (where rows can be swapped
      // but not columns) and scaled pivoting (where when searching for the pivot, we treat the equations as though they were all scaled
      // so that their largest coefficient would have a magnitude of 1, in order to eliminate bias caused by the original scaling of the
      // equations and put them on more equal footing -- useful since the partial pivot search is much more limited in scope than a full
      // pivot search). the result is not a rowwise permutation of the decomposition, but the decomposition of a rowwise permutation of A.
      //
      // If, for instance, the top and bottom rows of A are swapped:
      // | ru rv+sx rw+sy+z |                                         | r s 1 |       | u v w |                      | u v w |
      // | pu pv+x  pw+y    | then our decomposition is actually  L = | p 1 0 |   U = | 0 x y |  but still stored as | p x y |
      // | u  v     w       |                                         | 1 0 0 |       | 0 0 z |                      | r s z |
      // If, instead, the bottow two rows are swapped:
      // | u  v     w       |                                         | 1 0 0 |       | u v w |                      | u v w |
      // | ru rv+sx rw+sy+z | then our decomposition is actually  L = | r s 1 |   U = | 0 x y |  but still stored as | p x y |
      // | pu pv+x  pw+y    |                                         | p 1 0 |       | 0 0 z |                      | r s z |
      // So, it would seem, the pivoting only affects the lower matrix, and not the upper matrix. Because this renders the L matrix no
      // longer triangular, it doesn't really make sense for us to provide a method to retrieve the L and U matrices.
      if(!decomposed)
      {
        AssertDecomposition();
        this.rowPermutation = new int[matrix.Height]; // keep track of rows swaps so we access them in the right order inside Solve()
        this.oddSwapCount = false;

        // this step relates to scaled pivoting. we'll compute a scale factor to be applied to each equation that would scale it so that
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
          if(maxCoefficient == 0) throw new SingularMatrixException("The coefficient matrix is singular.");
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
            matrix.SwapRows(k, pivotRow);
            // keep track of whether the number of swaps was even or odd. swapping two rows or columns of a matrix negates its determinant.
            // so we use this in GetDeterminant() to determine whether we need to negate it back to get the correct value
            oddSwapCount = !oddSwapCount;
            // since the current row k was just swapped with the pivot row further down, we'll encounter it again later. when we do, we'll
            // need to have the correct scale value, so copy that too. we don't need to actually swap them, since we won't use the scale
            // for the pivot row anymore after we move past it
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

    /// <summary>Before decomposition has been performed, this holds the coefficient matrix. Aftewards, it holds the LU decomposition.</summary>
    Matrix matrix;
    Matrix coefficients;
    /// <summary>Represents the permutation that we performed during the pivot operation. rowPermutation[i] is the index of the row from
    /// which the pivot value was taken (and thus the row that row i was swapped with).
    /// </summary>
    int[] rowPermutation;
    bool decomposed, oddSwapCount;
  }
  #endregion

  #region SVDecomposition
  /// <summary>Implements singular value decomposition to solve systems of linear equations. This method is substantially slower than both
  /// <see cref="GaussJordan" /> and <see cref="LUDecomposition" />, as well as being slightly less accurate, but it has a number of
  /// powerful features. First, it is able to "solve" overdetermined, underdetermined, and singular (degenerate) systems. Given an
  /// underdetermined system, which will generally have an infinite number of solutions, it can give one of the solutions as well as
  /// provide a description of the solution space that allows you to generate the others.
  /// Given an overdetermined system, which will generally have no solution, it can provide
  /// a "solution" that is as close as possible to solving the equations in a least squares sense. Given a degenerate system, it can
  /// provide an approximate solution as well as allow you to diagnose exactly what makes the system degenerate. This flexibility allows
  /// <see cref="SVDecomposition"/> to succeed where other methods fail. Singular value decomposition can also be used to obtain a
  /// quantitative description of how near a system is to being singular (i.e. how ill-conditioned it is), to compute the Moore-Penrose
  /// pseudoinverse of a matrix, and to approximate (i.e. lossily compress) a matrix to a specified degree by removing the elements that
  /// contribute the least to it.
  /// </summary>
  /// <include file="documentation.xml" path="/Math/LinearEquations/Solve/remarks"/>
  public sealed class SVDecomposition : ILinearEquationSolver
  {
    /// <summary>Initializes a new <see cref="SVDecomposition"/> with no matrix. You can decompose a matrix by calling
    /// <see cref="Initialize"/>.
    /// </summary>
    public SVDecomposition() { }

    /// <summary>Initializes a new <see cref="SVDecomposition"/> with a matrix to decompose.</summary>
    public SVDecomposition(Matrix coefficients)
    {
      Initialize(coefficients);
    }

    /// <summary>Gets the default threshold for singular values. Singular values less than or equal to this value will be considered to be
    /// zero. See the remarks for more details. This property is only valid after a matrix has been decomposed.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <remarks>This property returns the default threshold that singular values must be above in order to be considered non-zero. By
    /// default, methods of the <see cref="SVDecomposition"/> class will use the default threshold, but you can use your own threshold
    /// (perhaps based on the default threshold).
    /// </remarks>
    public double DefaultThreshold
    {
      get
      {
        AssertDecomposition();
        return _defaultThreshold;
      }
    }

    /// <summary>Returns the inverse or Moore-Penrose pseudoinverse of the decomposed matrix, using the <see cref="DefaultThreshold"/>.</summary>
    /// <remarks>For decomposed matrices that are invertible, this method returns the inverse. For matrices that are not invertible, this
    /// method returns the Moore-Penrose pseudoinverse.
    /// </remarks>
    public Matrix GetInverse()
    {
      return GetInverse(_defaultThreshold);
    }

    /// <summary>Returns the inverse or Moore-Penrose pseudoinverse of the decomposed matrix, using the given threshold.</summary>
    /// <remarks>For decomposed matrices that are invertible, this method returns the inverse. For matrices that are not invertible, this
    /// method returns the Moore-Penrose pseudoinverse.
    /// </remarks>
    /// <seealso cref="DefaultThreshold"/>
    public Matrix GetInverse(double threshold)
    {
      AssertDecomposition();
      if(!u.IsSquare) throw new InvalidOperationException("The inverse can only be computed when the coefficient matrix is square.");
      return Solve(Matrix.CreateIdentity(u.Height), threshold, true);
    }

    /// <summary>Returns the inverse of the decomposed matrix's condition number.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <remarks>Rather than returning the condition number described above, this method returns its inverse. The reason is that with
    /// singular matrices, the condition number is infinite (since it involves a division by zero), and for very ill-conditoned matrices
    /// it may overflow. So with this method, an inverse condition number of zero indicates an exactly singular matrix, and a condition
    /// number much smaller than 1 represents a nearly singular matrix.
    /// </remarks>
    public double GetInverseCondition()
    {
      AssertDecomposition();
      return w[0] <= 0 || w[w.Size-1] <= 0 ? 0 : w[w.Size-1] / w[0];
    }

    /// <summary>Returns the nullity of the matrix, which is the dimension of its null space, using the <see cref="DefaultThreshold"/>.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/NullSpaceRemarks/*"/>
    public int GetNullity()
    {
      return GetNullity(_defaultThreshold);
    }

    /// <summary>Returns the nullity of the matrix, which is the dimension of its null space, using the given threshold.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/NullSpaceRemarks/*"/>
    /// <seealso cref="DefaultThreshold"/>
    public int GetNullity(double threshold)
    {
      AssertDecomposition();
      return w.Size - GetRank(threshold);
    }

    /// <summary>Returns a description of the null space of the decomposed matrix, as a matrix whose column vectors represent the
    /// orthogonal basis of the null space, using the <see cref="DefaultThreshold"/>.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/NullSpaceRemarks/*"/>
    public Matrix GetNullSpace()
    {
      return GetNullSpace(_defaultThreshold);
    }

    /// <summary>Returns a description of the null space of the decomposed matrix, as a matrix whose column vectors represent the
    /// orthogonal basis of the null space, using the given threshold.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/NullSpaceRemarks/*"/>
    /// <seealso cref="DefaultThreshold"/>
    public Matrix GetNullSpace(double threshold)
    {
      AssertDecomposition();
      Matrix nullSpace = new Matrix(v.Height, GetNullity(threshold));
      for(int nullity=0, x=0; x<v.Width; x++)
      {
        if(w[x] <= threshold) nullSpace.SetColumn(nullity++, v, x);
      }
      return nullSpace;
    }

    /// <summary>Returns a description of the range of the decomposed matrix, as a matrix whose column vectors represent the
    /// orthogonal basis of the range, using the <see cref="DefaultThreshold"/>.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/RangeRemarks/*"/>
    public Matrix GetRange()
    {
      return GetRange(_defaultThreshold);
    }

    /// <summary>Returns a description of the range of the decomposed matrix, as a matrix whose column vectors represent the
    /// orthogonal basis of the range, using the given threshold.
    /// </summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/RangeRemarks/*"/>
    /// <seealso cref="DefaultThreshold"/>
    public Matrix GetRange(double threshold)
    {
      AssertDecomposition();
      Matrix range = new Matrix(u.Height, GetRank(threshold));
      for(int rank=0,x=0; x<u.Width; x++)
      {
        if(w[x] > threshold) range.SetColumn(rank++, u, x);
      }
      return range;
    }

    /// <summary>Returns the rank of the matrix, which is the dimension of its range, using the <see cref="DefaultThreshold"/>.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/RangeRemarks/*"/>
    public int GetRank()
    {
      return GetRank(_defaultThreshold);
    }

    /// <summary>Returns the rank of the matrix, which is the dimension of its range, using the given threshold.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/RangeRemarks/*"/>
    public int GetRank(double threshold)
    {
      AssertDecomposition();
      int rank = 0;
      for(int i=0; i<w.Size; i++)
      {
        if(w[i] > threshold) rank++;
      }
      return rank;
    }

    /// <summary>Returns a vector containing the singular values of the matrix, sorted from largest to smallest.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/GeneralRemarks/*"/>
    public Vector GetSingularValues()
    {
      AssertDecomposition();
      return w.Clone();
    }

    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Initialize/*"/>
    public void Initialize(Matrix coefficients)
    {
      // this routine was mostly adapted from http://www.public.iastate.edu/~dicook/JSS/paper/code/svd.c, which was adapted from a routine
      // in XLISP-STAT 2.1, which was adapted from some version of Numerical Recipes. i don't claim to understand anything more than the
      // high level approach. i don't understand why people write such inscrutable code...

      if(coefficients == null) throw new ArgumentNullException();
      if(coefficients.Width == 0 || coefficients.Height == 0) throw new ArgumentException("The coefficient array is empty.");

      Matrix.Assign(ref u, coefficients);
      Matrix.Resize(ref v, u.Width, u.Width);
      Vector.Resize(ref w, u.Width);

      #region SV Decomposition gobbledygook
      // first, reduce the matrix to bidiagonal form using the Householder reduction
      double[] rv = new double[u.Width];
      double scale = 0, g = 0, anorm = 0;
      for(int i=0; i < rv.Length; i++)
      {
        int L = i+1;
        rv[i] = scale*g;

        // left-hand reduction
        g = scale = 0;
        double s = 0;
        if(i < u.Height)
        {
          for(int k=i; k<u.Height; k++) scale += Math.Abs(u[k, i]);
          if(scale != 0)
          {
            for(int k=i; k<u.Height; k++)
            {
              double value = u[k, i] / scale;
              u[k, i] = value;
              s += value*value;
            }

            double f = u[i, i];
            g = -WithSign(Math.Sqrt(s), f);
            double h = f*g - s;
            u[i, i] = f - g;
            for(int j=L; j<u.Width; j++)
            {
              s = 0;
              for(int k=i; k<u.Height; k++) s += u[k, i] * u[k, j];
              f = s / h;
              for(int k=i; k<u.Height; k++) u[k, j] += f * u[k, i];
            }
            for(int k=i; k<u.Height; k++) u[k, i] *= scale;
          }
        }

        w[i] = scale * g;

        // right-hand reduction
        g = s = scale = 0;
        if(i < u.Height && L != u.Width)
        {
          for(int k=L; k<u.Width; k++) scale += Math.Abs(u[i, k]);
          if(scale != 0)
          {
            for(int k=L; k<u.Width; k++)
            {
              double value = u[i, k] / scale;
              u[i, k] = value;
              s += value*value;
            }

            double f = u[i, L];
            g = -WithSign(Math.Sqrt(s), f);
            double h = f*g - s;
            u[i, L] = f - g;

            for(int k=L; k<u.Width; k++) rv[k] = u[i, k] / h;
            for(int j=L; j<u.Height; j++)
            {
              s = 0;
              for(int k=L; k<u.Width; k++) s += u[j, k] * u[i, k];
              for(int k=L; k<rv.Length; k++) u[j, k] += s * rv[k];
            }
            for(int k=L; k<u.Width; k++) u[i, k] *= scale;
          }
        }

        anorm = Math.Max(anorm, Math.Abs(w[i]) + Math.Abs(rv[i]));
      }
      anorm *= IEEE754.DoublePrecision;

      // accumulate right-hand transformation
      for(int i=u.Width-1; i >= 0; i--)
      {
        int L = i+1;
        if(L != u.Width) // if this isn't the first iteration...
        {
          if(g != 0)
          {
            for(int j=L; j<u.Width; j++) v[j, i] = u[i, j] / u[i, L] / g; // double division avoids possible underflow
            for(int j=L; j<u.Width; j++)
            {
              double s = 0;
              for(int k=L; k<u.Width; k++) s += u[i, k] * v[k, j];
              for(int k=L; k<u.Width; k++) v[k, j] += s * v[k, i];
            }
          }

          for(int j=L; j<u.Width; j++)
          {
            v[i, j] = 0;
            v[j, i] = 0;
          }
        }

        v[i, i] = 1;
        g = rv[i];
      }

      // accumulate left-hand transformation
      for(int i=Math.Min(u.Width, u.Height)-1; i >= 0; i--)
      {
        int L = i+1;
        for(int j=L; j<u.Width; j++) u[i, j] = 0;

        g = w[i];
        if(g == 0)
        {
          for(int j=i; j<u.Height; j++) u[j, i] = 0;
        }
        else
        {
          g = 1 / g;
          for(int j=L; j<u.Width; j++)
          {
            double s = 0;
            for(int k=L; k<u.Height; k++) s += u[k, i] * u[k, j];
            double f = s / u[i, i] * g;
            for(int k=i; k<u.Height; k++) u[k, j] += f * u[k, i];
          }
          for(int j=i; j<u.Height; j++) u[j, i] *= g;
        }
        u[i, i] = u[i, i] + 1;
      }

      // now we have a bidiagonal form, and we'll diagonalize it
      for(int k=u.Width-1; k >= 0; k--)
      {
        const int MaxIterations = 30;
        for(int iteration=0; iteration < MaxIterations; iteration++)
        {
          int nm = -1, L;
          bool flag = true;
          for(L=k; L >= 0; L--)
          {
            nm = L-1;
            if(L == 0 || Math.Abs(rv[L]) <= anorm)
            {
              flag = false;
              break;
            }
            if(Math.Abs(w[nm]) <= anorm) break;
          }

          if(flag)
          {
            double c = 0, s = 1;
            for(int i=L; i <= k; i++)
            {
              double f = s * rv[i];
              rv[i] *= c;
              if(Math.Abs(f) <= anorm) break;

              g = w[i];
              double h = Pythag(f, g);
              w[i] = h;
              h = 1 / h;
              c = g * h;
              s = -f * h;
              for(int j=0; j<u.Height; j++)
              {
                double a = u[j, nm], b = u[j, i];
                u[j, nm] = a*c + b*s;
                u[j, i]  = b*c - a*s;
              }
            }
          }

          {
            double z = w[k], f, x;
            if(L == k) // convergence
            {
              if(z < 0) // if the singular value is negative, make it non-negative
              {
                w[k] = -z;
                for(int j=0; j<u.Width; j++) v[j, k] = -v[j, k];
              }
              break;
            }
            else if(iteration == MaxIterations-1)
            {
              u = null; // clear 'u' so other methods will not think there is a valid decomposition
              throw new ArgumentException("Singular value decomposition failed to converge with this matrix.");
            }
            else // after that, we'll do some crazy stuff
            {
              x  = w[L];
              nm = k-1;
              double y = w[nm], h = rv[k];
              g  = rv[nm];
              f = ((y-z)*(y+z) + (g-h)*(g+h)) / (2*h*y);
              g = Pythag(f, 1);
              f = ((x-z)*(x+z) + h*(y/(f+WithSign(g, f)) - h)) / x;
              double c = 1, s = 1;
              for(int j=L; j <= nm; j++)
              {
                int i = j+1;
                g = rv[i];
                y = w[i];
                h = s * g;
                g = c * g;
                z = Pythag(f, h);
                rv[j] = z;
                c = f / z;
                s = h / z;
                f = x*c + g*s;
                g = g*c - x*s;
                h = y * s;
                y *= c;
                for(int m=0; m<u.Width; m++)
                {
                  x = v[m, j];
                  z = v[m, i];
                  v[m, j] = x*c + z*s;
                  v[m, i] = z*c - x*s;
                }
                z = Pythag(f, h);
                w[j] = z;
                if(z != 0)
                {
                  z = 1 / z;
                  c = f * z;
                  s = h * z;
                }
                f = c*g + s*y;
                x = c*y - s*g;
                for(int m=0; m<u.Height; m++)
                {
                  y = u[m, j];
                  z = u[m, i];
                  u[m, j] = y*c + z*s;
                  u[m, i] = z*c - y*s;
                }
              }
            }

            rv[L] = 0;
            rv[k] = f;
            w[k]  = x;
          }
        }
      }

      // now that the SV decomposition is done, we'll sort the matrix into a canonical order
      double[] su = new double[u.Height];
      int inc = 1;
      do inc = inc*3+1; while(inc <= u.Width);
      do
      {
        inc /= 3;
        for(int i=inc; i<u.Width; i++)
        {
          g = w[i];
          u.GetColumn(i, su);
          v.GetColumn(i, rv);
          int j = i;
          while(w[j-inc] < g)
          {
            w[j] = w[j-inc];
            for(int k=0; k<u.Height; k++) u[k, j] = u[k, j-inc];
            for(int k=0; k<u.Width; k++) v[k, j] = v[k, j-inc];
            j -= inc;
            if(j < inc) break;
          }
          w[j] = g;
          u.SetColumn(j, su);
          v.SetColumn(j, rv);
        }
      } while(inc > 1);

      // flip signs to make as many elements positive as possible
      for(int k=0; k<u.Width; k++)
      {
        int count = 0;
        for(int i=0; i<u.Height; i++)
        {
          if(u[i, k] < 0) count++;
        }
        for(int i=0; i<u.Width; i++)
        {
          if(v[i, k] < 0) count++;
        }
        if(count > (u.Width+u.Height)/2)
        {
          for(int i=0; i<u.Height; i++) u[i, k] = -u[i, k];
          for(int i=0; i<u.Width; i++) v[i, k] = -v[i, k];
        }
      }
      #endregion

      // set the default threshold based on the result
      _defaultThreshold = 0.5 * Math.Sqrt(u.Width + u.Height + 1) * w[0] * IEEE754.DoublePrecision;
    }

    /// <summary>Solves a system of linear equations using the <see cref="DefaultThreshold"/>.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve/*[not(self::summary) and @name != 'tryInPlace']"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/SolveRemarks/*"/>
    public Matrix Solve(Matrix values)
    {
      return Solve(values, _defaultThreshold, false);
    }

    /// <summary>Solves a system of linear equations using the given threshold.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve/*[not(self::summary) and @name != 'tryInPlace']"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/SolveRemarks/*"/>
    /// <seealso cref="DefaultThreshold"/>
    public Matrix Solve(Matrix values, double threshold)
    {
      return Solve(values, threshold, false);
    }

    /// <summary>Solves a system of linear equations using the <see cref="DefaultThreshold"/>.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve/*[not(self::summary)]"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/SolveRemarks/*"/>
    public Matrix Solve(Matrix values, bool tryInPlace)
    {
      return Solve(values, _defaultThreshold, tryInPlace);
    }

    /// <summary>Solves a system of linear equations using the given threshold.</summary>
    /// <include file="documentation.xml" path="/Math/LinearEquations/ILinearEquationSolver/Solve/*[not(self::summary)]"/>
    /// <include file="documentation.xml" path="/Math/LinearEquations/SVDecomposition/SolveRemarks/*"/>
    /// <seealso cref="DefaultThreshold"/>
    public Matrix Solve(Matrix values, double threshold, bool tryInPlace)
    {
      if(values == null) throw new ArgumentNullException();
      AssertDecomposition();
      if(values.Height != u.Height) throw new ArgumentException("The value matrix must have the same height as the coefficient matrix.");
      // we can only solve in place if the values matrix has the same height as the width of the coefficient matrix, or if the values
      // matrix has a width of 1 (so that we'll only make a single pass through the outer loop below)
      Matrix solutions = tryInPlace && (values.Height == u.Width || values.Width == 1) ? values : new Matrix(u.Width, values.Width);
      double[] temp = new double[u.Width];

      for(int x=0; x<values.Width; x++)
      {
        for(int j=0; j<temp.Length; j++)
        {
          double sum = 0;
          if(w[j] > threshold)
          {
            for(int i=0; i<u.Height; i++) sum += u[i, j] * values[i, x];
            sum /= w[j];
          }
          temp[j] = sum;
        }

        // if we want in-place solution, but the values matrix has the wrong height, then it must be the case that it has a width of 1
        // (given the above logic), so we can resize it now, before writing the answer into it
        if(tryInPlace && values.Height != u.Width) values.Resize(u.Width, 1);

        for(int j=0; j<u.Width; j++)
        {
          double sum = 0;
          for(int k=0; k<temp.Length; k++) sum += v[j, k] * temp[k];
          solutions[j, x] = sum;
        }
      }

      return solutions;
    }

    void AssertDecomposition()
    {
      if(u == null) throw new InvalidOperationException("No matrix has been decomposed yet.");
    }

    Matrix u, v;
    Vector w;
    double _defaultThreshold;

    static double Pythag(double a, double b)
    {
      // pythagorean theorem (sqrt(a^2 + b^2)), implemented to avoid overflow and underflow
      a = Math.Abs(a);
      b = Math.Abs(b);
      if(a > b)
      {
        double c = b/a;
        return a * Math.Sqrt(1 + c*c);
      }
      else if(b != 0)
      {
        double c = a/b;
        return b * Math.Sqrt(1 + c*c);
      }
      else
      {
        return 0;
      }
    }

    /// <summary>Returns a value having the magnitude of the first argument and the sign of the second argument. If the second argument is
    /// zero, it will be treated as though it was positive.
    /// </summary>
    static double WithSign(double value, double sign)
    {
      return (sign < 0) ^ (value < 0) ? -value : value;
    }
  }
  #endregion
}