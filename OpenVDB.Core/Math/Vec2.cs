// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices; // For StructLayout if needed for interop
using System.Numerics; // For ISignedNumber, IFloatingPointIeee754

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)] // Useful for interop
    public struct Vec2<T> : IEquatable<Vec2<T>>
        where T : struct, IEquatable<T>, IFormattable, ISignedNumber<T>, IFloatingPointIeee754<T> // Common numeric constraints
    {
        public T X, Y;

        public static int Size => 2;

        public T this[int i]
        {
            get
            {
                if (i == 0) return X;
                if (i == 1) return Y;
                throw new IndexOutOfRangeException("Vec2 index out of range.");
            }
            set
            {
                if (i == 0) X = value;
                else if (i == 1) Y = value;
                else throw new IndexOutOfRangeException("Vec2 index out of range.");
            }
        }

        public Vec2(T val)
        {
            X = val;
            Y = val;
        }

        public Vec2(T x, T y)
        {
            X = x;
            Y = y;
        }

        public Vec2(T[] a)
        {
            if (a == null || a.Length < 2)
                throw new ArgumentException("Array must have at least 2 elements.", nameof(a));
            X = a[0];
            Y = a[1];
        }

        public Vec2(Vec2<T> other)
        {
            X = other.X;
            Y = other.Y;
        }
        
        public T[] AsArray() => new[] { X, Y };

        public void Init(T x = default, T y = default)
        {
            X = x;
            Y = y;
        }

        public void SetZero()
        {
            X = T.Zero;
            Y = T.Zero;
        }

        public bool Equals(Vec2<T> other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Vec2<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Vec2<T> v1, Vec2<T> v2)
        {
            return v1.X.Equals(v2.X) && v1.Y.Equals(v2.Y);
        }

        public static bool operator !=(Vec2<T> v1, Vec2<T> v2)
        {
            return !(v1 == v2);
        }

        public bool IsApproxEqual(Vec2<T> other, T epsilon)
        {
            return T.Abs(X - other.X) <= epsilon &&
                   T.Abs(Y - other.Y) <= epsilon;
        }
        
        public static Vec2<T> operator -(Vec2<T> v)
        {
            return new Vec2<T>(-v.X, -v.Y);
        }

        public static Vec2<T> operator +(Vec2<T> v1, Vec2<T> v2)
        {
            return new Vec2<T>(v1.X + v2.X, v1.Y + v2.Y);
        }
        
        public static Vec2<T> operator +(Vec2<T> v, T scalar)
        {
            return new Vec2<T>(v.X + scalar, v.Y + scalar);
        }

        public static Vec2<T> operator +(T scalar, Vec2<T> v)
        {
            return new Vec2<T>(scalar + v.X, scalar + v.Y);
        }

        public static Vec2<T> operator -(Vec2<T> v1, Vec2<T> v2)
        {
            return new Vec2<T>(v1.X - v2.X, v1.Y - v2.Y);
        }
        
        public static Vec2<T> operator -(Vec2<T> v, T scalar)
        {
            return new Vec2<T>(v.X - scalar, v.Y - scalar);
        }

        public static Vec2<T> operator *(Vec2<T> v, T scalar)
        {
            return new Vec2<T>(v.X * scalar, v.Y * scalar);
        }

        public static Vec2<T> operator *(T scalar, Vec2<T> v)
        {
            return new Vec2<T>(scalar * v.X, scalar * v.Y);
        }

        public static Vec2<T> operator *(Vec2<T> v1, Vec2<T> v2) // Component-wise
        {
            return new Vec2<T>(v1.X * v2.X, v1.Y * v2.Y);
        }

        public static Vec2<T> operator /(Vec2<T> v, T scalar)
        {
            if (scalar.Equals(T.Zero)) throw new DivideByZeroException("Scalar cannot be zero.");
            return new Vec2<T>(v.X / scalar, v.Y / scalar);
        }
        
        public static Vec2<T> operator /(T scalar, Vec2<T> v)
        {
            if (v.X.Equals(T.Zero) || v.Y.Equals(T.Zero)) throw new DivideByZeroException("Vector component cannot be zero for scalar division.");
            return new Vec2<T>(scalar / v.X, scalar / v.Y);
        }

        public static Vec2<T> operator /(Vec2<T> v1, Vec2<T> v2) // Component-wise
        {
            if (v2.X.Equals(T.Zero) || v2.Y.Equals(T.Zero)) throw new DivideByZeroException("Divisor vector component cannot be zero.");
            return new Vec2<T>(v1.X / v2.X, v1.Y / v2.Y);
        }
        
        public T Dot(Vec2<T> other)
        {
            return X * other.X + Y * other.Y;
        }

        public T LengthSqr()
        {
            return X * X + Y * Y;
        }

        public T Length()
        {
            return T.Sqrt(LengthSqr());
        }

        public void Normalize(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon; // A very small number
            T len = Length();
            if (len <= epsilon) // Or use IsApproxZero
            {
                // Optionally throw, or set to a default vector like (1,0)
                // For now, matches C++ bool return by not modifying if too small
                return; 
            }
            X /= len;
            Y /= len;
        }

        public Vec2<T> Normalized(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T len = Length();
            if (len <= epsilon)
            {
                // Consider throwing an exception like in C++ OpenVDB
                // throw new InvalidOperationException("Cannot normalize a zero-length vector.");
                // Or return a safe vector
                 return new Vec2<T>(T.One, T.Zero); // Or Vec2<T>.Zero()
            }
            return new Vec2<T>(X / len, Y / len);
        }
        
        public Vec2<T> UnitSafe()
        {
            T l2 = LengthSqr();
            if (T.IsZero(l2) || T.IsSubnormal(l2)) // isApproxZero(l2) equivalent
                return new Vec2<T>(T.One, T.Zero);
            return this / T.Sqrt(l2);
        }

        public T Component(Vec2<T> onto, T eps = default)
        {
            if (eps == default) eps = T.Epsilon;
            T l = onto.Length();
            if (l <= eps) return T.Zero;
            return Dot(onto) / l;
        }

        public Vec2<T> Projection(Vec2<T> onto, T eps = default)
        {
            if (eps == default) eps = T.Epsilon;
            T lSqr = onto.LengthSqr();
            if (lSqr <= eps * eps) return Zero(); // Or use more robust check for small lSqr
            return onto * (Dot(onto) / lSqr);
        }

        public Vec2<T> GetArbPerpendicular()
        {
            return new Vec2<T>(-Y, X);
        }
        
        public T Sum() => X + Y;
        public T Product() => X * Y;

        public Vec2<T> Exp() => new Vec2<T>(T.Exp(X), T.Exp(Y));
        public Vec2<T> Log() => new Vec2<T>(T.Log(X), T.Log(Y));
        public Vec2<T> Abs() => new Vec2<T>(T.Abs(X), T.Abs(Y));


        public static Vec2<T> Zero() => new Vec2<T>(T.Zero, T.Zero);
        public static Vec2<T> Ones() => new Vec2<T>(T.One, T.One);

        public override string ToString()
        {
            return $"[{X.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}, {Y.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}]";
        }
        
        public void Write(System.IO.BinaryWriter writer)
        {
            if (typeof(T) == typeof(double)) { writer.Write((double)(object)X); writer.Write((double)(object)Y); }
            else if (typeof(T) == typeof(float)) { writer.Write((float)(object)X); writer.Write((float)(object)Y); }
            else if (typeof(T) == typeof(int)) { writer.Write((int)(object)X); writer.Write((int)(object)Y); }
            // Add other supported types as necessary
            else throw new NotSupportedException($"Vec2.Write for type {typeof(T)} not supported for metadata serialization.");
        }

        public static Vec2<T> Read(System.IO.BinaryReader reader)
        {
            if (typeof(T) == typeof(double)) return new Vec2<T>((T)(object)reader.ReadDouble(), (T)(object)reader.ReadDouble());
            if (typeof(T) == typeof(float)) return new Vec2<T>((T)(object)reader.ReadSingle(), (T)(object)reader.ReadSingle());
            if (typeof(T) == typeof(int)) return new Vec2<T>((T)(object)reader.ReadInt32(), (T)(object)reader.ReadInt32());
            // Add other supported types as necessary
            throw new NotSupportedException($"Vec2.Read for type {typeof(T)} not supported for metadata deserialization.");
        }
        
        public static uint GetSizeInBytes()
        {
            if (typeof(T) == typeof(double)) return (uint)System.Runtime.InteropServices.Marshal.SizeOf<double>() * 2;
            if (typeof(T) == typeof(float)) return (uint)System.Runtime.InteropServices.Marshal.SizeOf<float>() * 2;
            if (typeof(T) == typeof(int)) return (uint)System.Runtime.InteropServices.Marshal.SizeOf<int>() * 2;
            // Add other supported types as necessary
            throw new NotSupportedException($"Vec2.GetSizeInBytes for type {typeof(T)} not supported for metadata serialization.");
        }
    }

    // Common type aliases
    public using Vec2i = Vec2<int>;
    public using Vec2f = Vec2<float>;
    public using Vec2d = Vec2<double>;
    // Note: Vec2s in OpenVDB C++ is often Vec2<float>.
    // If System.Half is available and desired:
    // public using Vec2h = Vec2<System.Half>; 
}
