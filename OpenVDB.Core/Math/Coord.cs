// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices; // For StructLayout if needed
using System.Numerics; // For IFloatingPointIeee754 for Vec3 conversions

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)] // Can be useful for interop
    public struct Coord : IEquatable<Coord>, IComparable<Coord>
    {
        public int X, Y, Z;

        public Coord(int xyz)
        {
            X = xyz;
            Y = xyz;
            Z = xyz;
        }

        public Coord(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Coord(Vec3<int> v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }
        
        public Coord(Vec3<uint> v) // from Vec3I in C++
        {
            X = (int)v.X;
            Y = (int)v.Y;
            Z = (int)v.Z;
        }

        public Coord(int[] v)
        {
            if (v == null || v.Length < 3)
                throw new ArgumentException("Array must have at least 3 elements.", nameof(v));
            X = v[0];
            Y = v[1];
            Z = v[2];
        }

        public static Coord MinValue => new Coord(int.MinValue);
        public static Coord MaxValue => new Coord(int.MaxValue);

        public static Coord Round<T>(Vec3<T> xyz) where T : struct, IFloatingPointIeee754<T>
        {
            return new Coord((int)T.Round(xyz.X), (int)T.Round(xyz.Y), (int)T.Round(xyz.Z));
        }

        public static Coord Floor<T>(Vec3<T> xyz) where T : struct, IFloatingPointIeee754<T>
        {
            return new Coord((int)T.Floor(xyz.X), (int)T.Floor(xyz.Y), (int)T.Floor(xyz.Z));
        }

        public static Coord Ceil<T>(Vec3<T> xyz) where T : struct, IFloatingPointIeee754<T>
        {
            return new Coord((int)T.Ceiling(xyz.X), (int)T.Ceiling(xyz.Y), (int)T.Ceiling(xyz.Z));
        }

        public void Reset(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Reset(int xyz)
        {
            X = Y = Z = xyz;
        }

        public void Offset(int dx, int dy, int dz)
        {
            X += dx;
            Y += dy;
            Z += dz;
        }

        public void Offset(int n)
        {
            X += n;
            Y += n;
            Z += n;
        }

        public Coord OffsetBy(int dx, int dy, int dz) const
        {
            return new Coord(X + dx, Y + dy, Z + dz);
        }

        public Coord OffsetBy(int n) const
        {
            return new Coord(X + n, Y + n, Z + n);
        }
        
        public int this[int i]
        {
            get
            {
                if (i == 0) return X;
                if (i == 1) return Y;
                if (i == 2) return Z;
                throw new IndexOutOfRangeException("Coord index out of range.");
            }
            set
            {
                if (i == 0) X = value;
                else if (i == 1) Y = value;
                else if (i == 2) Z = value;
                else throw new IndexOutOfRangeException("Coord index out of range.");
            }
        }
        
        public int[] ToArray() => new[] { X, Y, Z };
        public Vec3<double> ToVec3d() => new Vec3<double>(X, Y, Z);
        public Vec3<float> ToVec3f() => new Vec3<float>(X, Y, Z);
        public Vec3<int> ToVec3i() => new Vec3<int>(X, Y, Z);
        public Vec3<uint> ToVec3UI() => new Vec3<uint>((uint)X, (uint)Y, (uint)Z);


        public static Coord operator +(Coord a, Coord b) => new Coord(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Coord operator -(Coord a, Coord b) => new Coord(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Coord operator -(Coord a) => new Coord(-a.X, -a.Y, -a.Z);
        
        public static Coord operator *(Coord c, int scalar) => new Coord(c.X * scalar, c.Y * scalar, c.Z * scalar);
        public static Coord operator *(int scalar, Coord c) => new Coord(c.X * scalar, c.Y * scalar, c.Z * scalar);
        public static Coord operator /(Coord c, int scalar)
        {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide Coord by zero.");
            return new Coord(c.X / scalar, c.Y / scalar, c.Z / scalar);
        }

        public static Coord operator >>(Coord c, int shift) => new Coord(c.X >> shift, c.Y >> shift, c.Z >> shift);
        public static Coord operator <<(Coord c, int shift) => new Coord(c.X << shift, c.Y << shift, c.Z << shift);
        public static Coord operator &(Coord c, int val) => new Coord(c.X & val, c.Y & val, c.Z & val);
        public static Coord operator |(Coord c, int val) => new Coord(c.X | val, c.Y | val, c.Z | val);

        public bool Equals(Coord other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Coord other && Equals(other);
        public static bool operator ==(Coord a, Coord b) => a.Equals(b);
        public static bool operator !=(Coord a, Coord b) => !a.Equals(b);

        public int CompareTo(Coord other) // Lexicographical comparison
        {
            if (X < other.X) return -1;
            if (X > other.X) return 1;
            if (Y < other.Y) return -1;
            if (Y > other.Y) return 1;
            if (Z < other.Z) return -1;
            if (Z > other.Z) return 1;
            return 0;
        }

        public static bool operator <(Coord a, Coord b) => a.CompareTo(b) < 0;
        public static bool operator <=(Coord a, Coord b) => a.CompareTo(b) <= 0;
        public static bool operator >(Coord a, Coord b) => a.CompareTo(b) > 0;
        public static bool operator >=(Coord a, Coord b) => a.CompareTo(b) >= 0;
        
        public void MinComponent(Coord other)
        {
            X = System.Math.Min(X, other.X);
            Y = System.Math.Min(Y, other.Y);
            Z = System.Math.Min(Z, other.Z);
        }

        public void MaxComponent(Coord other)
        {
            X = System.Math.Max(X, other.X);
            Y = System.Math.Max(Y, other.Y);
            Z = System.Math.Max(Z, other.Z);
        }

        public static Coord MinComponent(Coord a, Coord b) =>
            new Coord(System.Math.Min(a.X, b.X), System.Math.Min(a.Y, b.Y), System.Math.Min(a.Z, b.Z));

        public static Coord MaxComponent(Coord a, Coord b) =>
            new Coord(System.Math.Max(a.X, b.X), System.Math.Max(a.Y, b.Y), System.Math.Max(a.Z, b.Z));
            
        public static bool LessThan(Coord a, Coord b) => (a.X < b.X || a.Y < b.Y || a.Z < b.Z);
        
        public int MinIndex()
        {
            if (X <= Y && X <= Z) return 0;
            if (Y <= Z) return 1;
            return 2;
        }

        public int MaxIndex()
        {
            if (X >= Y && X >= Z) return 0;
            if (Y >= Z) return 1;
            return 2;
        }
        
        public long LengthSqr() => (long)X * X + (long)Y * Y + (long)Z * Z; // Use long to avoid overflow before sqrt
        public double Length() => System.Math.Sqrt(LengthSqr());

        public Coord Abs() => new Coord(System.Math.Abs(X), System.Math.Abs(Y), System.Math.Abs(Z));

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        // C++ uses: ((1<<Log2N)-1) & (vec[0]*73856093 ^ vec[1]*19349669 ^ vec[2]*83492791);
        // This is a specific hashing algorithm. HashCode.Combine is generally good enough for C#.
        // If identical hash values to C++ are needed, this specific algorithm should be ported.

        public override string ToString() => $"[{X}, {Y}, {Z}]";
        
        public static Coord Zero = new Coord(0,0,0);
        public static Coord One = new Coord(1,1,1);
        public static Coord XAxis = new Coord(1,0,0);
        public static Coord YAxis = new Coord(0,1,0);
        public static Coord ZAxis = new Coord(0,0,1);

    }
}
