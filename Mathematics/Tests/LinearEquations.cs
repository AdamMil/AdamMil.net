using AdamMil.Mathematics.LinearEquations;
using NUnit.Framework;
using System;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class LinearEquations
  {
    [Test]
    public void TestLinearEquations()
    {
      const double Accuracy = 2.2205e-15, LowerAccuracy = 1.2e-13; // the lower accuracy is used for determinant comparisons

      // given w=-1, x=2, y=-3, and z=4...
      // w - 2x + 3y - 5z = -1 - 4 - 9 - 20 = -34
      // w + 2x + 3y + 5z = -1 + 4 - 9 + 20 = 14
      // 5w - 3x + 2y - z = -5 - 6 - 6 - 4  = -21
      // 5w + 3x + 2y + z = -5 + 6 - 6 + 4  = -1
      double[] coefficientArray = new double[16]
      {
        1, -2, 3, -5,
        1,  2, 3,  5,
        5, -3, 2, -1,
        5,  3, 2,  1,
      };
      double[] valueArray = new double[4] { -34, 14, -21, -1 };

      // first solve using Gauss-Jordan elimination
      Matrix4 coefficients = new Matrix4(coefficientArray);
      Matrix gjInverse, values = new Matrix(valueArray, 1);
      Matrix solution = GaussJordan.Solve(coefficients.ToMatrix(), values, out gjInverse);
      CheckSolution(solution, Accuracy);
      CheckSolution(gjInverse * values, Accuracy); // make sure multiplying by the matrix inverse also gives the right answer

      // then solve using LU decomposition
      LUDecomposition lud = new LUDecomposition(coefficients.ToMatrix());
      solution = lud.Solve(values);
      CheckSolution(solution, Accuracy);
      CheckSolution(lud.GetInverse() * values, Accuracy); // check that the inverse can be multiplied to produce a good solution

      solution.Multiply(1.1); // mess up the solution
      lud.RefineSolution(values, solution); // test that refinement can fix it
      CheckSolution(solution, Accuracy);

      // check that the computed determinants are what we expect
      bool negative;
      Assert.AreEqual(coefficients.GetDeterminant(), lud.GetDeterminant(), LowerAccuracy);
      Assert.AreEqual(Math.Log(Math.Abs(coefficients.GetDeterminant())), lud.GetLogDeterminant(out negative), Accuracy);
      Assert.AreEqual(coefficients.GetDeterminant() < 0, negative);

      // check that the inverses are about the same from both methods
      Assert.IsTrue(gjInverse.Equals(lud.GetInverse(), Accuracy));
    }

    static void CheckSolution(Matrix solution, double accuracy)
    {
      Assert.AreEqual(1, solution.Width);
      Assert.AreEqual(4, solution.Height);
      Assert.AreEqual(-1, solution[0, 0], accuracy);
      Assert.AreEqual(2, solution[1, 0], accuracy);
      Assert.AreEqual(-3, solution[2, 0], accuracy);
      Assert.AreEqual(4, solution[3, 0], accuracy);
    }
  }
}