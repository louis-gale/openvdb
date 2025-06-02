// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System.Numerics;

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)]
    public struct Vec4<T> : IEquatable<Vec4<T>>
        where T : struct, IEquatable<T>, INumber<T>
    {
        public T X, Y, Z, W;

        public static int Size => 4;

        public T this[int i]
        {
            get
            {
                if (i == 0) return X;
                if (i == 1) return Y;
                if (i == 2) return Z;
                if (i == 3) return W;
                throw new IndexOutOfRangeException("Vec4 index out of range.");
            }
            set
            {
                if (i == 0) X = value;
                else if (i == 1) Y = value;
                else if (i == 2) Z = value;
                else if (i == 3) W = value;
                else throw new IndexOutOfRangeException("Vec4 index out of range.");
            }
        }

        public Vec4(T val)
        {
            X = val;
            Y = val;
            Z = val;
            W = val;
        }

        public Vec4(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vec4(T[] a)
        {
            if (a == null || a.Length < 4)
                throw new ArgumentException("Array must have at least 4 elements.", nameof(a));
            X = a[0];
            Y = a[1];
            Z = a[2];
            W = a[3];
        }

        public Vec4(Vec4<T> other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
            W = other.W;
        }

        public T[] AsArray() => new[] { X, Y, Z, W };

        public Vec3<T> GetVec3() => new(X, Y, Z);

        public void Init(T x = default, T y = default, T z = default, T w = default)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void SetZero()
        {
            X = T.Zero;
            Y = T.Zero;
            Z = T.Zero;
            W = T.Zero;
        }

        public bool Equals(Vec4<T> other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        }

        public override bool Equals(object obj)
        {
            return obj is Vec4<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }

        public static bool operator ==(Vec4<T> v1, Vec4<T> v2)
        {
            return v1.X.Equals(v2.X) && v1.Y.Equals(v2.Y) && v1.Z.Equals(v2.Z) && v1.W.Equals(v2.W);
        }

        public static bool operator !=(Vec4<T> v1, Vec4<T> v2)
        {
            return !(v1 == v2);
        }

        public bool IsApproxEqual(Vec4<T> other, double epsilon)
        {
            return Double.CreateChecked(T.Abs(X - other.X)) <= epsilon &&
                   Double.CreateChecked(T.Abs(Y - other.Y)) <= epsilon &&
                   Double.CreateChecked(T.Abs(Z - other.Z)) <= epsilon &&
                   Double.CreateChecked(T.Abs(W - other.W)) <= epsilon;
        }

        // C++ version uses isApproxEqual for eq method
        public bool Eq(Vec4<T> other, double epsilon = 1.0e-8)
        {
            return IsApproxEqual(other, epsilon);
        }

        public static Vec4<T> operator -(Vec4<T> v)
        {
            return new Vec4<T>(-v.X, -v.Y, -v.Z, -v.W);
        }

        public static Vec4<T> operator +(Vec4<T> v1, Vec4<T> v2)
        {
            return new Vec4<T>(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
        }

        public static Vec4<T> operator +(Vec4<T> v, T scalar)
        {
            return new Vec4<T>(v.X + scalar, v.Y + scalar, v.Z + scalar, v.W + scalar);
        }

        public static Vec4<T> operator +(T scalar, Vec4<T> v)
        {
            return new Vec4<T>(scalar + v.X, scalar + v.Y, scalar + v.Z, scalar + v.W);
        }

        public static Vec4<T> operator -(Vec4<T> v1, Vec4<T> v2)
        {
            return new Vec4<T>(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);
        }

        public static Vec4<T> operator -(Vec4<T> v, T scalar)
        {
            return new Vec4<T>(v.X - scalar, v.Y - scalar, v.Z - scalar, v.W - scalar);
        }

        public static Vec4<T> operator *(Vec4<T> v, T scalar)
        {
            return new Vec4<T>(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);
        }

        public static Vec4<T> operator *(T scalar, Vec4<T> v)
        {
            return new Vec4<T>(scalar * v.X, scalar * v.Y, scalar * v.Z, scalar * v.W);
        }

        public static Vec4<T> operator *(Vec4<T> v1, Vec4<T> v2) // Component-wise
        {
            return new Vec4<T>(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z, v1.W * v2.W);
        }

        public static Vec4<T> operator /(Vec4<T> v, T scalar)
        {
            if (scalar.Equals(T.Zero)) throw new DivideByZeroException("Scalar cannot be zero.");
            return new Vec4<T>(v.X / scalar, v.Y / scalar, v.Z / scalar, v.W / scalar);
        }

        public static Vec4<T> operator /(T scalar, Vec4<T> v)
        {
            if (v.X.Equals(T.Zero) || v.Y.Equals(T.Zero) || v.Z.Equals(T.Zero) || v.W.Equals(T.Zero))
                throw new DivideByZeroException("Vector component cannot be zero for scalar division.");
            return new Vec4<T>(scalar / v.X, scalar / v.Y, scalar / v.Z, scalar / v.W);
        }

        public static Vec4<T> operator /(Vec4<T> v1, Vec4<T> v2) // Component-wise
        {
             if (v2.X.Equals(T.Zero) || v2.Y.Equals(T.Zero) || v2.Z.Equals(T.Zero) || v2.W.Equals(T.Zero))
                throw new DivideByZeroException("Divisor vector component cannot be zero.");
            return new Vec4<T>(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z, v1.W / v2.W);
        }

        public T Dot(Vec4<T> other)
        {
            return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
        }

        public T LengthSqr()
        {
            return X * X + Y * Y + Z * Z + W * W;
        }

        public T Length()
        {
            return T.CreateChecked(System.Math.Sqrt(Double.CreateChecked(LengthSqr())));
        }

        public bool Normalize(double epsilon = 1.0e-8)
        {
            T len = Length();
            if (Double.CreateChecked(len) <= epsilon)
            {
                return false;
            }
            X /= len;
            Y /= len;
            Z /= len;
            W /= len;
            return true;
        }

        public Vec4<T> Normalized(double epsilon = 1.0e-8)
        {
            T len = Length();
            if (Double.CreateChecked(len) <= epsilon)
            {
                 // C++ OpenVDB throws ArithmeticError here.
                 throw new InvalidOperationException("Cannot normalize a zero-length vector.");
            }
            return new Vec4<T>(X / len, Y / len, Z / len, W / len);
        }

        public Vec4<T> UnitSafe()
        {
            T l2 = LengthSqr();
            if (T.IsZero(l2) || T.IsSubnormal(l2))
                return new Vec4<T>(T.One, T.Zero, T.Zero, T.Zero); // (1,0,0,0) for Vec4
            return this / T.CreateChecked(System.Math.Sqrt(Double.CreateChecked(l2)));
        }

        public T Sum() => X + Y + Z + W;
        public T Product() => X * Y * Z * W;

        public Vec4<T> Exp() => new(T.CreateChecked(System.Math.Exp(Double.CreateChecked(X))), T.CreateChecked(System.Math.Exp(Double.CreateChecked(Y))), T.CreateChecked(System.Math.Exp(Double.CreateChecked(Z))), T.CreateChecked(System.Math.Exp(Double.CreateChecked(W))));
        public Vec4<T> Log() => new(T.CreateChecked(System.Math.Log(Double.CreateChecked(X))), T.CreateChecked(System.Math.Log(Double.CreateChecked(Y))), T.CreateChecked(System.Math.Log(Double.CreateChecked(Z))), T.CreateChecked(System.Math.Log(Double.CreateChecked(W))));
        public Vec4<T> Abs() => new(T.Abs(X), T.Abs(Y), T.Abs(Z), T.Abs(W));

        public static Vec4<T> Zero() => new(T.Zero, T.Zero, T.Zero, T.Zero);
        public static Vec4<T> Ones() => new(T.One, T.One, T.One, T.One);
        public static Vec4<T> Origin() => new(T.Zero, T.Zero, T.Zero, T.One); // Typically for points in homogeneous coords


        public override string ToString()
        {
            return $"[{X.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}, {Y.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}, {Z.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}, {W.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}]";
        }
    }
}
