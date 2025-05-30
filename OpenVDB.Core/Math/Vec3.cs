// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)]
    public struct Vec3<T> : IEquatable<Vec3<T>>, IReadOnlyVec<T>
        where T : struct, IEquatable<T>, IFormattable, ISignedNumber<T>, IFloatingPointIeee754<T>
    {
        public T X, Y, Z;

        public static int StaticSize => 3; // Keep static size if used elsewhere
        public int Size => 3; // Instance property for interface

        public T this[int i]
        {
            get
            {
                if (i == 0) return X;
                if (i == 1) return Y;
                if (i == 2) return Z;
                throw new IndexOutOfRangeException("Vec3 index out of range.");
            }
            set
            {
                if (i == 0) X = value;
                else if (i == 1) Y = value;
                else if (i == 2) Z = value;
                else throw new IndexOutOfRangeException("Vec3 index out of range.");
            }
        }

        public Vec3(T val)
        {
            X = val;
            Y = val;
            Z = val;
        }

        public Vec3(T x, T y, T z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3(T[] a)
        {
            if (a == null || a.Length < 3)
                throw new ArgumentException("Array must have at least 3 elements.", nameof(a));
            X = a[0];
            Y = a[1];
            Z = a[2];
        }
        
        public Vec3(Vec3<T> other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }
        
        public T[] AsArray() => new[] { X, Y, Z };

        public void Init(T x = default, T y = default, T z = default)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void SetZero()
        {
            X = T.Zero;
            Y = T.Zero;
            Z = T.Zero;
        }

        public bool Equals(Vec3<T> other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is Vec3<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vec3<T> v1, Vec3<T> v2)
        {
            return v1.X.Equals(v2.X) && v1.Y.Equals(v2.Y) && v1.Z.Equals(v2.Z);
        }

        public static bool operator !=(Vec3<T> v1, Vec3<T> v2)
        {
            return !(v1 == v2);
        }

        public bool IsApproxEqual(Vec3<T> other, T epsilon)
        {
            return T.Abs(X - other.X) <= epsilon &&
                   T.Abs(Y - other.Y) <= epsilon &&
                   T.Abs(Z - other.Z) <= epsilon;
        }
        
        // C++ version uses isRelOrApproxEqual for eq method
        public bool Eq(Vec3<T> other, T epsilon = default, T relTol = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            if (relTol == default) relTol = T.Epsilon;

            return MathUtil.IsRelOrApproxEqual(X, other.X, epsilon, relTol) &&
                   MathUtil.IsRelOrApproxEqual(Y, other.Y, epsilon, relTol) &&
                   MathUtil.IsRelOrApproxEqual(Z, other.Z, epsilon, relTol);
        }


        public static Vec3<T> operator -(Vec3<T> v)
        {
            return new Vec3<T>(-v.X, -v.Y, -v.Z);
        }

        public static Vec3<T> operator +(Vec3<T> v1, Vec3<T> v2)
        {
            return new Vec3<T>(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }
        
        public static Vec3<T> operator +(Vec3<T> v, T scalar)
        {
            return new Vec3<T>(v.X + scalar, v.Y + scalar, v.Z + scalar);
        }

        public static Vec3<T> operator +(T scalar, Vec3<T> v)
        {
            return new Vec3<T>(scalar + v.X, scalar + v.Y, scalar + v.Z);
        }

        public static Vec3<T> operator -(Vec3<T> v1, Vec3<T> v2)
        {
            return new Vec3<T>(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }
        
        public static Vec3<T> operator -(Vec3<T> v, T scalar)
        {
            return new Vec3<T>(v.X - scalar, v.Y - scalar, v.Z - scalar);
        }

        public static Vec3<T> operator *(Vec3<T> v, T scalar)
        {
            return new Vec3<T>(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vec3<T> operator *(T scalar, Vec3<T> v)
        {
            return new Vec3<T>(scalar * v.X, scalar * v.Y, scalar * v.Z);
        }

        public static Vec3<T> operator *(Vec3<T> v1, Vec3<T> v2) // Component-wise
        {
            return new Vec3<T>(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public static Vec3<T> operator /(Vec3<T> v, T scalar)
        {
            if (scalar.Equals(T.Zero)) throw new DivideByZeroException("Scalar cannot be zero.");
            return new Vec3<T>(v.X / scalar, v.Y / scalar, v.Z / scalar);
        }
        
        public static Vec3<T> operator /(T scalar, Vec3<T> v)
        {
            if (v.X.Equals(T.Zero) || v.Y.Equals(T.Zero) || v.Z.Equals(T.Zero)) 
                throw new DivideByZeroException("Vector component cannot be zero for scalar division.");
            return new Vec3<T>(scalar / v.X, scalar / v.Y, scalar / v.Z);
        }

        public static Vec3<T> operator /(Vec3<T> v1, Vec3<T> v2) // Component-wise
        {
            if (v2.X.Equals(T.Zero) || v2.Y.Equals(T.Zero) || v2.Z.Equals(T.Zero)) 
                throw new DivideByZeroException("Divisor vector component cannot be zero.");
            return new Vec3<T>(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
        }

        public T Dot(Vec3<T> other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        public Vec3<T> Cross(Vec3<T> other)
        {
            return new Vec3<T>(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X);
        }

        public T LengthSqr()
        {
            return X * X + Y * Y + Z * Z;
        }

        public T Length()
        {
            return T.Sqrt(LengthSqr());
        }

        public bool Normalize(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T len = Length();
            if (len <= epsilon) // Or use IsApproxZero
            {
                return false; 
            }
            X /= len;
            Y /= len;
            Z /= len;
            return true;
        }

        public Vec3<T> Normalized(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T len = Length();
            if (len <= epsilon)
            {
                 // C++ OpenVDB throws ArithmeticError here.
                 throw new InvalidOperationException("Cannot normalize a zero-length vector.");
            }
            return new Vec3<T>(X / len, Y / len, Z / len);
        }
        
        public Vec3<T> UnitSafe()
        {
            T l2 = LengthSqr();
            if (T.IsZero(l2) || T.IsSubnormal(l2)) // isApproxZero(l2) equivalent
                return new Vec3<T>(T.One, T.Zero, T.Zero); // (1,0,0) for Vec3
            return this / T.Sqrt(l2);
        }
        
        public T Component(Vec3<T> onto, T eps = default)
        {
            if (eps == default) eps = T.Epsilon;
            T l = onto.Length();
            if (l <= eps) return T.Zero;
            return Dot(onto) / l;
        }

        public Vec3<T> Projection(Vec3<T> onto, T eps = default)
        {
            if (eps == default) eps = T.Epsilon;
            T lSqr = onto.LengthSqr();
            if (lSqr <= eps * eps) return Zero();
            return onto * (Dot(onto) / lSqr);
        }

        public Vec3<T> GetArbPerpendicular()
        {
            T l;
            if (T.Abs(X) >= T.Abs(Y))
            {
                l = X * X + Z * Z;
                if (T.IsZero(l) || T.IsSubnormal(l)) return new Vec3<T>(T.Zero, T.One, T.Zero); // Or handle error
                l = T.One / T.Sqrt(l);
                return new Vec3<T>(-Z * l, T.Zero, X * l);
            }
            else
            {
                l = Y * Y + Z * Z;
                if (T.IsZero(l) || T.IsSubnormal(l)) return new Vec3<T>(T.One, T.Zero, T.Zero); // Or handle error
                l = T.One / T.Sqrt(l);
                return new Vec3<T>(T.Zero, Z * l, -Y * l);
            }
        }
        
        public Vec3<T> Sorted()
        {
            T val0 = X, val1 = Y, val2 = Z;
            if (val0 > val1) (val0, val1) = (val1, val0); // Swap
            if (val1 > val2) (val1, val2) = (val2, val1); // Swap
            if (val0 > val1) (val0, val1) = (val1, val0); // Swap
            return new Vec3<T>(val0, val1, val2);
        }

        public Vec3<T> Reversed()
        {
            return new Vec3<T>(Z, Y, X);
        }

        public T Sum() => X + Y + Z;
        public T Product() => X * Y * Z;

        public Vec3<T> Exp() => new Vec3<T>(T.Exp(X), T.Exp(Y), T.Exp(Z));
        public Vec3<T> Log() => new Vec3<T>(T.Log(X), T.Log(Y), T.Log(Z));
        public Vec3<T> Abs() => new Vec3<T>(T.Abs(X), T.Abs(Y), T.Abs(Z));

        public static Vec3<T> Zero() => new Vec3<T>(T.Zero, T.Zero, T.Zero);
        public static Vec3<T> Ones() => new Vec3<T>(T.One, T.One, T.One);
        
        // Standard axes
        public static Vec3<T> XAxis() => new Vec3<T>(T.One, T.Zero, T.Zero);
        public static Vec3<T> YAxis() => new Vec3<T>(T.Zero, T.One, T.Zero);
        public static Vec3<T> ZAxis() => new Vec3<T>(T.Zero, T.Zero, T.One);


        public override string ToString()
        {
            return $"[{X.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}, {Y.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}, {Z.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}]";
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            if (typeof(T) == typeof(double)) { writer.Write((double)(object)X); writer.Write((double)(object)Y); writer.Write((double)(object)Z); }
            else if (typeof(T) == typeof(float)) { writer.Write((float)(object)X); writer.Write((float)(object)Y); writer.Write((float)(object)Z); }
            else if (typeof(T) == typeof(int)) { writer.Write((int)(object)X); writer.Write((int)(object)Y); writer.Write((int)(object)Z); }
            // Add other supported types as necessary
            else throw new NotSupportedException($"Vec3.Write for type {typeof(T)} not supported for metadata serialization.");
        }

        public static Vec3<T> Read(System.IO.BinaryReader reader)
        {
            if (typeof(T) == typeof(double)) return new Vec3<T>((T)(object)reader.ReadDouble(), (T)(object)reader.ReadDouble(), (T)(object)reader.ReadDouble());
            if (typeof(T) == typeof(float)) return new Vec3<T>((T)(object)reader.ReadSingle(), (T)(object)reader.ReadSingle(), (T)(object)reader.ReadSingle());
            if (typeof(T) == typeof(int)) return new Vec3<T>((T)(object)reader.ReadInt32(), (T)(object)reader.ReadInt32(), (T)(object)reader.ReadInt32());
            // Add other supported types as necessary
            throw new NotSupportedException($"Vec3.Read for type {typeof(T)} not supported for metadata deserialization.");
        }
        
        public static uint GetSizeInBytes()
        {
            if (typeof(T) == typeof(double)) return (uint)System.Runtime.InteropServices.Marshal.SizeOf<double>() * 3;
            if (typeof(T) == typeof(float)) return (uint)System.Runtime.InteropServices.Marshal.SizeOf<float>() * 3;
            if (typeof(T) == typeof(int)) return (uint)System.Runtime.InteropServices.Marshal.SizeOf<int>() * 3;
            // Add other supported types as necessary
            throw new NotSupportedException($"Vec3.GetSizeInBytes for type {typeof(T)} not supported for metadata serialization.");
        }
    }

    // Common type aliases
    public using Vec3i = Vec3<int>;
    public using Vec3f = Vec3<float>; // In C++ OpenVDB, Vec3s is float
    public using Vec3d = Vec3<double>;
    public using Vec3U8 = Vec3<byte>;
    public using Vec3U16 = Vec3<ushort>;
    // If System.Half is available and desired:
    // public using Vec3h = Vec3<System.Half>;
}
