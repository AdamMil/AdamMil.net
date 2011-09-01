using System;
using System.Text;
using AdamMil.Utilities;

namespace AdamMil.Mathematics
{
  /// <summary>Represents an N-dimensional vector.</summary>
  [Serializable]
  public class Vector : ICloneable, IEquatable<Vector>
  {
    /// <summary>Initializes a new zero <see cref="Vector"/> with the given number of components.</summary>
    public Vector(int length)
    {
      if(length < 0) throw new ArgumentOutOfRangeException();
      data = new double[length];
    }

    /// <summary>Initializes a new <see cref="Vector"/> with components from the given array.</summary>
    public Vector(params double[] data)
    {
      if(data == null) throw new ArgumentNullException();
      Initialize(data, 0, data.Length);
    }

    /// <summary>Initializes a new <see cref="Vector"/> with components from a subsection of the given array.</summary>
    public Vector(double[] data, int index, int length)
    {
      Initialize(data, index, length);
    }

    /// <summary>Initializes a new <see cref="Vector"/> with components from the given array.</summary>
    [CLSCompliant(false)]
    public unsafe Vector(double* data, int length)
    {
      if(length < 0) throw new ArgumentOutOfRangeException();
      this.data = new double[length];
      fixed(double* dest = this.data) Unsafe.Copy(data, dest, length*sizeof(double));
    }

    /// <summary>Initializes a new <see cref="Vector"/> with components from the given array.</summary>
    /// <param name="data">The array storing the component values.</param>
    /// <param name="copyArray">If true, a new array will be allocated and the values will be copied into it. If false, the vector will
    /// directly use the given array as its internal storage.
    /// </param>
    public Vector(double[] data, bool copyArray)
    {
      this.data = copyArray ? (double[])data.Clone() : data;
    }

    /// <summary>Gets or sets a component of the vector.</summary>
    public double this[int index]
    {
      get { return data[index]; }
      set { data[index] = value; }
    }

    /// <summary>Gets a reference to the internal array that stores the vector's components. The reference will remain valid as long as the
    /// vector is not resized.
    /// </summary>
    public double[] Array
    {
      get { return data; }
    }

    /// <summary>Gets the size of the vector, which is the number of components it contains.</summary>
    public int Size
    {
      get { return data.Length; }
    }

    /// <summary>Adds another vector to the vector.</summary>
    public void Add(Vector other)
    {
      ValidateSameSize(other);
      for(int i=0; i<data.Length; i++) data[i] += other.data[i];
    }

    /// <summary>Copies data from another vector into this one.</summary>
    public void Assign(Vector other)
    {
      ValidateSameSize(other);
      ArrayUtility.SmallCopy(other.data, data, data.Length);
    }

    /// <summary>Returns a copy of the vector.</summary>
    public Vector Clone()
    {
      return new Vector(data, true);
    }

    /// <summary>Copies the elements of the vector into an array at the given index.</summary>
    public void CopyTo(double[] array, int index)
    {
      Utility.ValidateRange(array, index, Size);
      ArrayUtility.SmallCopy(data, 0, array, index, data.Length);
    }

    /// <summary>Divides the vector by a constant factor.</summary>
    public void Divide(double factor)
    {
      Multiply(1 / factor);
    }

    /// <summary>Determines whether the given object is a vector that equals this one.</summary>
    public override bool Equals(object obj)
    {
      return Equals(this, obj as Vector);
    }

    /// <summary>Determines whether the given vector equals this one.</summary>
    public bool Equals(Vector other)
    {
      return Equals(this, other);
    }

    /// <summary>Determines whether the given vector equals this one, to within the given tolerance.</summary>
    /// <param name="other">The vector to compare with.</param>
    /// <param name="tolerance">The tolerance, which should be a non-negative quantity. The vectors will be considered to be equal if the
    /// difference between each pair of components does not exceed this value.
    /// </param>
    public bool Equals(Vector other, double tolerance)
    {
      return Equals(this, other, tolerance);
    }

    /// <summary>Gets a hash code of the vector.</summary>
    public unsafe override int GetHashCode()
    {
      int hash = 0;
      for(int i=0; i<data.Length; i++)
      {
        double d = data[i];
        if(d != 0) hash ^= *(int*)&d ^ ((int*)&d)[1] ^ i; // +0 and -0 compare equally, so they mustn't lead to different hash codes
      }
      return hash;
    }

    // TODO: this method may be vulnerable to overflow and loss of precision, even when the magnitude is easily representable in a double.
    // see the methods described in Numerical Recipes for computing the magnitude of complex numbers and see if they can be extended to
    // arbitrary vectors
    /// <summary>Returns the length, or magnitude, of the vector.</summary>
    public double GetMagnitude()
    {
      double sum = 0;
      for(int i=0; i<data.Length; i++) sum += data[i]*data[i];
      return Math.Sqrt(sum);
    }

    /// <summary>Multiplies the vector by a constant factor.</summary>
    public void Multiply(double factor)
    {
      for(int i=0; i<data.Length; i++) data[i] *= factor;
    }

    /// <summary>Negates the vector by negating each component.</summary>
    public void Negate()
    {
      for(int i=0; i<data.Length; i++) data[i] = -data[i];
    }

    /// <summary>Normalizes the vector to unit length.</summary>
    public void Normalize()
    {
      Normalize(1);
    }

    /// <summary>Normalizes the vector to the given length.</summary>
    public void Normalize(double newLength)
    {
      Multiply(newLength / GetMagnitude());
    }

    /// <summary>Resizes the vector to be of the given length. The vector's data is not preserved.</summary>
    public void Resize(int length)
    {
      if(data.Length != length)
      {
        if(length < 0) throw new ArgumentOutOfRangeException();
        data = new double[length];
      }
    }

    /// <summary>Subtracts another vector from the vector.</summary>
    public void Subtract(Vector other)
    {
      ValidateSameSize(other);
      for(int i=0; i<data.Length; i++) data[i] -= other.data[i];
    }

    /// <summary>Returns an array containing a copy of the components from the vector.</summary>
    public double[] ToArray()
    {
      double[] array = new double[Size];
      ArrayUtility.SmallCopy(data, array, array.Length);
      return array;
    }

    /// <summary>Converts the vector to a <see cref="Matrix"/> with single column containing the vector, and a width of one.</summary>
    public Matrix ToColumnMatrix()
    {
      return new Matrix(data, 1);
    }

    /// <summary>Converts the vector to a square <see cref="Matrix"/> containing the vector components along the diagonal.</summary>
    public Matrix ToDiagonalMatrix()
    {
      Matrix matrix = new Matrix(Size, Size);
      for(int i=0; i<data.Length; i++) matrix[i, i] = data[i];
      return matrix;
    }

    /// <summary>Converts the vector to a <see cref="Matrix"/> with single row containing the vector, and a height of one.</summary>
    public Matrix ToRowMatrix()
    {
      return new Matrix(data, data.Length);
    }

    /// <summary>Converts the vector to a string.</summary>
    public override string ToString()
    {
      return ToString(null);
    }

    /// <summary>Converts the vector to a string using the given format for the vector's components.</summary>
    public string ToString(string format)
    {
      StringBuilder sb = new StringBuilder();
      for(int i=0; i<data.Length; i++)
      {
        if(i != 0) sb.Append(", ");
        sb.Append(data[i].ToString(format));
      }
      return sb.ToString();
    }

    /// <summary>Adds two vectors and returns the result.</summary>
    public static Vector Add(Vector a, Vector b)
    {
      ValidateSameSize(a, b);
      double[] data = new double[a.Size];
      for(int i=0; i<data.Length; i++) data[i] = a.data[i] + b.data[i];
      return new Vector(data, false);
    }

    /// <summary>Assigns a copy of <paramref name="source"/> to <paramref name="dest"/>, overwriting the existing vector if possible, or
    /// allocating a new vector if not.
    /// </summary>
    public static void Assign(ref Vector dest, Vector source)
    {
      if(source == null) throw new ArgumentNullException();
      if(dest == null) dest = source.Clone();
      else dest.Assign(source);
    }

    /// <summary>Divides a vector by a constant factor and returns the result.</summary>
    public static Vector Divide(Vector vector, double factor)
    {
      return Multiply(vector, 1 / factor);
    }

    /// <summary>Returns the dot product of two vectors.</summary>
    public static double DotProduct(Vector a, Vector b)
    {
      ValidateSameSize(a, b);
      double sum = 0;
      for(int i=0; i<a.Size; i++) sum += a.data[i] * b.data[i];
      return sum;
    }

    /// <summary>Determines whether two vectors are equal. Null vectors are only equal to other null vectors.</summary>
    public static bool Equals(Vector a, Vector b)
    {
      if(a == null) return b == null;
      else if(b == null || a.Size != b.Size) return false;

      for(int i=0; i<a.data.Length; i++)
      {
        if(a.data[i] != b.data[i]) return false;
      }

      return true;
    }

    /// <summary>Determines whether two vectors are equal to within the given tolerance. Null vectors are only equal to other null vectors.</summary>
    /// <param name="a">A vector to compare.</param>
    /// <param name="b">The other vector to compare.</param>
    /// <param name="tolerance">The tolerance, which should be a non-negative quantity. The vectors will be considered to be equal if the
    /// difference between each pair of components does not exceed this value.
    /// </param>
    public static bool Equals(Vector a, Vector b, double tolerance)
    {
      if(a == null) return b == null;
      else if(b == null || a.Size != b.Size) return false;

      for(int i=0; i<a.data.Length; i++)
      {
        if(Math.Abs(a.data[i] - b.data[i]) > tolerance) return false;
      }

      return true;
    }

    /// <summary>Multiplies a vector by a constant factor and returns the result.</summary>
    public static Vector Multiply(Vector vector, double factor)
    {
      if(vector == null) throw new ArgumentNullException();
      double[] data = new double[vector.Size];
      for(int i=0; i<data.Length; i++) data[i] = vector.data[i] * factor;
      return new Vector(data, false);
    }

    /// <summary>Negates a vector by negating each component, and returns the result.</summary>
    public static Vector Negate(Vector vector)
    {
      if(vector == null) throw new ArgumentNullException();
      double[] data = new double[vector.Size];
      for(int i=0; i<data.Length; i++) data[i] = -vector.data[i];
      return new Vector(data, false);
    }

    /// <summary>Returns a copy of the vector, normalized to unit length.</summary>
    public static Vector Normalize(Vector vector)
    {
      return Normalize(vector, 1);
    }

    /// <summary>Returns a copy of the vector, normalized to the given length.</summary>
    public static Vector Normalize(Vector vector, double newLength)
    {
      if(vector == null) throw new ArgumentNullException();
      return Multiply(vector, newLength / vector.GetMagnitude());
    }

    /// <summary>Resizes the given vector if it exists, or allocates a new vector if it is null.</summary>
    public static void Resize(ref Vector dest, int size)
    {
      if(dest == null) dest = new Vector(size);
      else dest.Resize(size);
    }

    /// <summary>Subtracts one vector from another and returns the result.</summary>
    public static Vector Subtract(Vector a, Vector b)
    {
      ValidateSameSize(a, b);
      double[] data = new double[a.Size];
      for(int i=0; i<data.Length; i++) data[i] = a.data[i] - b.data[i];
      return new Vector(data, false);
    }

    /// <summary>Adds two vectors and returns the result.</summary>
    public static Vector operator+(Vector a, Vector b)
    {
      return Add(a, b);
    }

    /// <summary>Negates a vector by negating each component, and returns the result.</summary>
    public static Vector operator-(Vector vector)
    {
      return Negate(vector);
    }

    /// <summary>Subtracts one vector from another and returns the result.</summary>
    public static Vector operator-(Vector a, Vector b)
    {
      return Subtract(a, b);
    }

    /// <summary>Multiplies a vector by a constant factor and returns the result.</summary>
    public static Vector operator*(Vector a, double b)
    {
      return Multiply(a, b);
    }

    /// <summary>Multiplies a vector by a constant factor and returns the result.</summary>
    public static Vector operator*(double a, Vector b)
    {
      return Multiply(b, a);
    }

    /// <summary>Divides a vector by a constant factor and returns the result.</summary>
    public static Vector operator/(Vector a, double b)
    {
      return Divide(a, b);
    }

    void Initialize(double[] data, int index, int length)
    {
      Utility.ValidateRange(data, index, length);
      this.data = new double[length];
      ArrayUtility.SmallCopy(data, index, this.data, 0, length);
    }

    void ValidateSameSize(Vector other)
    {
      if(other == null) throw new ArgumentNullException();
      if(Size != other.Size) throw new ArgumentException("The vectors must be the same size.");
    }

    static void ValidateSameSize(Vector a, Vector b)
    {
      if(a == null || b == null) throw new ArgumentNullException();
      if(a.Size != b.Size) throw new ArgumentException("The vectors must be the same size.");
    }

    #region ICloneable Members
    object ICloneable.Clone()
    {
      return Clone();
    }
    #endregion

    double[] data;
  }
}
