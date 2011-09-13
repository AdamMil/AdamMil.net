using System;
using AdamMil.Mathematics.LinearAlgebra;
using NUnit.Framework;

namespace AdamMil.Mathematics.Tests
{
  [TestFixture]
  public class LinearAlgebra
  {
    [Test]
    public void T01_BasicLinearEquations()
    {
      const double Accuracy = 2.2205e-15, SvdAccuracy = 4.46e-15, DeterminantAccuracy = 1.2e-13;

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
      Assert.AreEqual(coefficients.GetDeterminant(), lud.GetDeterminant(), DeterminantAccuracy);
      Assert.AreEqual(Math.Log(Math.Abs(coefficients.GetDeterminant())), lud.GetLogDeterminant(out negative), Accuracy);
      Assert.AreEqual(coefficients.GetDeterminant() < 0, negative);

      // check that the inverses are about the same from both methods
      Assert.IsTrue(gjInverse.Equals(lud.GetInverse(), Accuracy));

      // then solve using QR decomposition
      QRDecomposition qrd = new QRDecomposition(coefficients.ToMatrix());
      solution = qrd.Solve(values);
      CheckSolution(solution, Accuracy);
      CheckSolution(qrd.GetInverse() * values, Accuracy); // check that the inverse can be multiplied to produce a good solution
      // TODO: test qrd.Update()

      // finally, try solving using singular value decomposition
      SVDecomposition svd = new SVDecomposition(coefficients.ToMatrix());
      solution = svd.Solve(values);
      CheckSolution(solution, SvdAccuracy);
      CheckSolution(svd.GetInverse() * values, SvdAccuracy);
      Assert.IsTrue(gjInverse.Equals(svd.GetInverse(), Accuracy));
      Assert.AreEqual(4, svd.GetRank());
      Assert.AreEqual(0, svd.GetNullity());
    }

    [Test]
    public void T02_SingularValueDecomposition()
    {
      const double Accuracy = 1.8e-15;
      // create a system of linear equations that doesn't appear degenerate at first glance, but actually is:
      // x + y + z = 6
      // x + 2y + 3z = 14  (x = 1, y = 2, z = 3)
      // 3x + 2y + z = 10
      // the problem is not row degeneracy, but column degeneracy, where the y column (1 2 2) can be represented as (x+z)/2:
      // ((1 1 3) + (1 3 1)) / 2 = (2 4 4) / 2 = (1 2 2)

      Matrix m = new Matrix(new double[]
      {
        1, 1, 1,
        1, 2, 3,
        3, 2, 1,
      }, 3);

      SVDecomposition svd = new SVDecomposition(m);
      Assert.AreEqual(1, svd.GetNullity()); // make sure the degeneracy is detected
      Assert.AreEqual(2, svd.GetRank());
      Assert.AreEqual(0, svd.GetSingularValues()[2], Accuracy); // see that the smallest singular value is practically zero
      Assert.AreEqual(0, svd.GetInverseCondition(), Accuracy); // and that the inverse condition value is too

      // ensure that we can get a valid solution anyway
      Matrix values = new Vector(6, 14, 10).ToColumnMatrix();
      Vector v = svd.Solve(values).GetColumn(0);
      Assert.AreEqual(1, v[0], Accuracy); // it should find the smallest solution vector, (1 2 3)
      Assert.AreEqual(2, v[1], Accuracy);
      Assert.AreEqual(3, v[2], Accuracy);

      // try using the pseudoinverse to generate the same solution
      // TODO: test pseudoinverse with non-square matrices
      Matrix inverse = svd.GetInverse();
      Matrix solution = inverse * values;
      Assert.AreEqual(1, solution.Width);
      Assert.AreEqual(3, solution.Height);
      Assert.AreEqual(1, solution[0, 0], Accuracy*2);
      Assert.AreEqual(2, solution[1, 0], Accuracy*2);
      Assert.AreEqual(3, solution[2, 0], Accuracy*2);

      // check that the null space lets us generate more solutions
      Matrix ns = svd.GetNullSpace();
      Assert.AreEqual(1, ns.Width);
      Assert.AreEqual(3, ns.Height);
      Vector nsv = ns.GetColumn(0);
      v += 7*nsv;
      Assert.AreEqual(6, v[0]+v[1]+v[2], Accuracy*2); // we lose some precision after the multiplications, so tweak the required accuracy
      Assert.AreEqual(14, v[0]+2*v[1]+3*v[2], Accuracy*2);
      Assert.AreEqual(10, 3*v[0]+2*v[1]+v[2], Accuracy*2);

      // check the range
      Matrix range = svd.GetRange();
      Assert.AreEqual(2, range.Width);
      Assert.AreEqual(3, range.Height);
      // the first column vector should be some multiple of (1 2 2)
      v = range.GetColumn(0);
      v /= v[0]; // scale so the first component (x) is 1
      Assert.AreEqual(1, v[0], Accuracy);
      Assert.AreEqual(2, v[1], Accuracy);
      Assert.AreEqual(2, v[2], Accuracy);
      // the second column vector should be some multiple of (0 1 -1)
      v = range.GetColumn(1);
      v /= v[1]; // scale so the second component (y) is 1
      Assert.AreEqual(0, v[0], Accuracy);
      Assert.AreEqual(1, v[1], Accuracy);
      Assert.AreEqual(-1, v[2], Accuracy);
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