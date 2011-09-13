using System;

namespace AdamMil.Mathematics
{
  static class MathHelpers
  {
    public const double GoldenRatio = 1.61803398874989485;

    public static void AddScaledColumn(Matrix matrix1, int column1, Matrix matrix2, int column2, double scale, int startI=0)
    {
      for(; startI < matrix1.Width; startI++) matrix1[startI, column1] += scale * matrix2[startI, column2];
    }

    public static void Backsubstitute(Matrix utMatrix, Matrix values)
    {
      for(int i=0; i<values.Width; i++) Backsubstitute(utMatrix, values, i);
    }

    public static void Backsubstitute(Matrix utMatrix, Matrix values, int column)
    {
      for(int i=values.Height-1; i >= 0; i--)
      {
        double sum = values[i, column];
        for(int k=i+1; k<values.Height; k++) sum -= utMatrix[i, k] * values[k, column];
        values[i, column] = sum / utMatrix[i, i];
      }
    }

    public static void Backsubstitute(Matrix utMatrix, double[] values)
    {
      for(int i=values.Length-1; i >= 0; i--)
      {
        double sum = values[i];
        for(int k=i+1; k<values.Length; k++) sum -= utMatrix[i, k] * values[k];
        values[i] = sum / utMatrix[i, i];
      }
    }

    public static void CopyColumn(Matrix src, int srcColumn, Matrix dest, int destColumn)
    {
      for(int i=0; i<dest.Height; i++) dest[i, destColumn] = src[i, srcColumn];
    }

    public static void DivideColumn(Matrix matrix, int column, double dividend, int startI=0)
    {
      for(; startI < matrix.Height; startI++) matrix[startI, column] /= dividend;
    }

    public static void DivideVector(double[] vector, double dividend)
    {
      for(int i=0; i<vector.Length; i++) vector[i] /= dividend;
    }

    public static double DotProduct(double[] a, double[] b)
    {
      double sum = 0;
      for(int i=0; i<a.Length; i++) sum += a[i]*b[i];
      return sum;
    }

    public static double GetMaxMagnitudeInColumn(Matrix matrix, int column, int startI=0)
    {
      double max = 0;
      for(; startI < matrix.Height; startI++)
      {
        double value = Math.Abs(matrix[startI, column]);
        if(value > max) max = value;
      }
      return max;
    }

    public static double GetMaxMagnitudeInRow(Matrix matrix, int row)
    {
      double max = 0;
      for(int j=0; j<matrix.Width; j++)
      {
        double value = Math.Abs(matrix[row, j]);
        if(value > max) max = value;
      }
      return max;
    }

    public static void Multiply(Matrix lhs, double[] rhsColumn, double[] destColumn)
    {
      for(int i=0; i<destColumn.Length; i++) destColumn[i] = SumColumnTimesVector(lhs, i, rhsColumn);
    }

    public static void NegateColumn(Matrix matrix, int column)
    {
      for(int i=0; i<matrix.Height; i++) matrix[i, column] = -matrix[i, column];
    }

    public static void NegateVector(double[] vector)
    {
      for(int i=0; i<vector.Length; i++) vector[i] = -vector[i];
    }

    public static void PostJacobiRotation(Matrix matrix, int column1, int column2, double c, double s, int startI=0)
    {
      for(; startI < matrix.Height; startI++)
      {
        double a = matrix[startI, column1], b = matrix[startI, column2];
        matrix[startI, column1] = c*a - s*b;
        matrix[startI, column2] = c*b + s*a;
      }
    }

    public static void PreJacobiRotation(Matrix matrix, int i, double cos, double sin, int startJ=0)
    {
      for(; startJ < matrix.Width; startJ++)
      {
        double a = matrix[i, startJ], b = matrix[i+1, startJ];
        matrix[i, startJ]   = cos*a - sin*b;
        matrix[i+1, startJ] = sin*a + cos*b;
      }
    }

    public static void ScaleColumn(Matrix matrix, int column, double scale, int startI=0)
    {
      for(; startI < matrix.Height; startI++) matrix[startI, column] *= scale;
    }

    public static void ScaleRow(Matrix matrix, int row, double scale, int startJ)
    {
      for(; startJ < matrix.Width; startJ++) matrix[row, startJ] *= scale;
    }

    public static void ScaleVector(double[] vector, double scale)
    {
      for(int i=0; i<vector.Length; i++) vector[i] *= scale;
    }

    public static void SubtractScaledColumn(Matrix matrix1, int column1, Matrix matrix2, int column2, double scale, int startI=0)
    {
      for(; startI < matrix1.Width; startI++) matrix1[startI, column1] -= scale * matrix2[startI, column2];
    }

    public static void SubtractScaledRow(Matrix matrix1, int row1, Matrix matrix2, int row2, double scale, int startJ=0)
    {
      for(; startJ < matrix1.Width; startJ++) matrix1[row1, startJ] -= scale * matrix2[row2, startJ];
    }

    public static double SumColumnTimesColumn(Matrix matrix1, int column1, Matrix matrix2, int column2, int startI=0)
    {
      double sum = 0;
      for(; startI < matrix1.Height; startI++) sum += matrix1[startI, column1] * matrix2[startI, column2];
      return sum;
    }

    public static double SumColumnTimesVector(Matrix matrix, int column, double[] vector)
    {
      double sum = 0;
      for(int i=0; i<vector.Length; i++) sum += matrix[i, column] * vector[i];
      return sum;
    }

    public static double SumRowTimesColumn(Matrix rowMatrix, int row, Matrix columnMatrix, int column, int start=0)
    {
      double sum = 0;
      for(; start < columnMatrix.Height; start++) sum += rowMatrix[row, start] * columnMatrix[start, column];
      return sum;
    }

    public static double SumRowTimesRow(Matrix matrix1, int row1, Matrix matrix2, int row2, int startJ=0)
    {
      double sum = 0;
      for(; startJ < matrix1.Width; startJ++) sum += matrix1[row1, startJ] * matrix2[row2, startJ];
      return sum;
    }

    public static double SumRowTimesVector(Matrix matrix, int row, double[] vector, int startJ=0)
    {
      double sum = 0;
      for(; startJ < vector.Length; startJ++) sum += matrix[row, startJ]*vector[startJ];
      return sum;
    }

    public static double SumSquaredColumn(Matrix matrix, int column, int startI=0)
    {
      double sum = 0;
      for(; startI < matrix.Height; startI++)
      {
        double value = matrix[startI, column];
        sum += value*value;
      }
      return sum;
    }

    public static double SumSquaredVector(double[] vector)
    {
      double sum = 0;
      for(int i=0; i<vector.Length; i++)
      {
        double value = vector[i];
        sum += value*value;
      }
      return sum;
    }

    /// <summary>Returns a value having the magnitude of the first argument and the sign of the second argument. If the second argument is
    /// zero, it will be treated as though it was positive.
    /// </summary>
    public static double WithSign(double value, double sign)
    {
      return (sign < 0) ^ (value < 0) ? -value : value;
    }

    public static void ZeroColumn(Matrix matrix, int column, int startI=0)
    {
      for(; startI < matrix.Height; startI++) matrix[startI, column] = 0;
    }

    public static void ZeroRow(Matrix matrix, int row, int startJ=0)
    {
      for(; startJ < matrix.Width; startJ++) matrix[row, startJ] = 0;
    }
  }
}
