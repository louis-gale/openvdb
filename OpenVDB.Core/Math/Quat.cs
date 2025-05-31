// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;
using System.Numerics; // For IFloatingPointIeee754

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)]
    public struct Quat<T> : IEquatable<Quat<T>>
        where T : struct, IEquatable<T>, IFormattable, ISignedNumber<T>, IFloatingPointIeee754<T>
    {
        // Stored as (x, y, z, w) where w is the scalar part
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
                throw new IndexOutOfRangeException("Quat index out of range.");
            }
            set
            {
                if (i == 0) X = value;
                else if (i == 1) Y = value;
                else if (i == 2) Z = value;
                else if (i == 3) W = value;
                else throw new IndexOutOfRangeException("Quat index out of range.");
            }
        }

        public Quat(T x, T y, T z, T w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Quat(T[] a)
        {
            if (a == null || a.Length < 4)
                throw new ArgumentException("Array must have at least 4 elements.", nameof(a));
            X = a[0];
            Y = a[1];
            Z = a[2];
            W = a[3];
        }

        public Quat(Vec3<T> axis, T angle) // Assumes axis is normalized
        {
            T halfAngle = angle * T.CreateChecked(0.5);
            T s = T.Sin(halfAngle);
            X = axis.X * s;
            Y = axis.Y * s;
            Z = axis.Z * s;
            W = T.Cos(halfAngle);
        }

        public Quat(Mat3<T> mat)
        {
            // Adapted from C++ version; requires robust IsUnitary and matrix determinant/trace.
            // This conversion is non-trivial and error-prone if matrix is not a pure rotation.
            T trace = mat.M00 + mat.M11 + mat.M22;
            if (trace > T.Zero)
            {
                T s = T.Sqrt(trace + T.One) * T.CreateChecked(2.0);
                W = T.CreateChecked(0.25) * s;
                X = (mat.M21 - mat.M12) / s;
                Y = (mat.M02 - mat.M20) / s;
                Z = (mat.M10 - mat.M01) / s;
            }
            else if ((mat.M00 > mat.M11) && (mat.M00 > mat.M22))
            {
                T s = T.Sqrt(T.One + mat.M00 - mat.M11 - mat.M22) * T.CreateChecked(2.0);
                W = (mat.M21 - mat.M12) / s;
                X = T.CreateChecked(0.25) * s;
                Y = (mat.M01 + mat.M10) / s;
                Z = (mat.M02 + mat.M20) / s;
            }
            else if (mat.M11 > mat.M22)
            {
                T s = T.Sqrt(T.One + mat.M11 - mat.M00 - mat.M22) * T.CreateChecked(2.0);
                W = (mat.M02 - mat.M20) / s;
                X = (mat.M01 + mat.M10) / s;
                Y = T.CreateChecked(0.25) * s;
                Z = (mat.M12 + mat.M21) / s;
            }
            else
            {
                T s = T.Sqrt(T.One + mat.M22 - mat.M00 - mat.M11) * T.CreateChecked(2.0);
                W = (mat.M10 - mat.M01) / s;
                X = (mat.M02 + mat.M20) / s;
                Y = (mat.M12 + mat.M21) / s;
                Z = T.CreateChecked(0.25) * s;
            }
        }

        public void Init(T x = default, T y = default, T z = default, T w = default)
        {
            X = x; Y = y; Z = z; W = w;
            if (x == default && y == default && z == default && w == default) W = T.One; // Identity for parameterless init
        }

        public void SetIdentity()
        {
            X = Y = Z = T.Zero;
            W = T.One;
        }

        public static Quat<T> Identity => new Quat<T>(T.Zero, T.Zero, T.Zero, T.One);
        public static Quat<T> Zero => new Quat<T>(T.Zero, T.Zero, T.Zero, T.Zero);


        public bool Equals(Quat<T> other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        public override bool Equals(object obj) => obj is Quat<T> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        public static bool operator ==(Quat<T> q1, Quat<T> q2) => q1.Equals(q2);
        public static bool operator !=(Quat<T> q1, Quat<T> q2) => !q1.Equals(q2);

        public bool IsApproxEqual(Quat<T> other, T epsilon) =>
            T.Abs(X - other.X) <= epsilon && T.Abs(Y - other.Y) <= epsilon &&
            T.Abs(Z - other.Z) <= epsilon && T.Abs(W - other.W) <= epsilon;

        public T Angle()
        {
            T sqrLength = X * X + Y * Y + Z * Z;
            if (sqrLength <= T.Epsilon) // Using T.Epsilon as a small threshold
                return T.Zero;
            return T.Acos(W) * T.CreateChecked(2.0); // W is cos(angle/2)
        }

        public Vec3<T> Axis()
        {
            T sqrLength = X * X + Y * Y + Z * Z;
            if (sqrLength <= T.Epsilon)
                return new Vec3<T>(T.One, T.Zero, T.Zero); // Default axis

            T invLength = T.One / T.Sqrt(sqrLength);
            return new Vec3<T>(X * invLength, Y * invLength, Z * invLength);
        }

        public static Quat<T> operator +(Quat<T> q1, Quat<T> q2) =>
            new Quat<T>(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);

        public static Quat<T> operator -(Quat<T> q1, Quat<T> q2) =>
            new Quat<T>(q1.X - q2.X, q1.Y - q2.Y, q1.Z - q2.Z, q1.W - q2.W);

        public static Quat<T> operator *(Quat<T> q1, Quat<T> q2) => new Quat<T>(
            q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
            q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X,
            q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W,
            q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z);

        public static Quat<T> operator *(Quat<T> q, T scalar) =>
            new Quat<T>(q.X * scalar, q.Y * scalar, q.Z * scalar, q.W * scalar);

        public static Quat<T> operator *(T scalar, Quat<T> q) => q * scalar;

        public static Quat<T> operator /(Quat<T> q, T scalar)
        {
            if (T.IsZero(scalar)) throw new DivideByZeroException("Scalar cannot be zero.");
            return new Quat<T>(q.X / scalar, q.Y / scalar, q.Z / scalar, q.W / scalar);
        }

        public static Quat<T> operator -(Quat<T> q) => new Quat<T>(-q.X, -q.Y, -q.Z, -q.W); // Note: conjugate is often (-x,-y,-z, w)

        public T Dot(Quat<T> other) => X * other.X + Y * other.Y + Z * other.Z + W * other.W;

        public T LengthSqr() => X * X + Y * Y + Z * Z + W * W;
        public T Length() => T.Sqrt(LengthSqr());

        public bool Normalize(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T len = Length();
            if (len <= epsilon) return false;
            this /= len;
            return true;
        }

        public Quat<T> Normalized(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T len = Length();
            if (len <= epsilon) throw new InvalidOperationException("Cannot normalize a zero-length quaternion.");
            return this / len;
        }

        public Quat<T> Conjugate() => new Quat<T>(-X, -Y, -Z, W);

        public Quat<T> Inverse(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T lenSqr = LengthSqr();
            if (lenSqr <= epsilon * epsilon) // Using epsilon^2 for squared length check
                throw new InvalidOperationException("Cannot invert a zero-length quaternion.");
            return Conjugate() / lenSqr;
        }

        public Vec3<T> RotateVector(Vec3<T> v)
        {
            // q * v * q^-1
            // v' = q * (0, v) * q_conj
            Quat<T> p = new Quat<T>(v.X, v.Y, v.Z, T.Zero);
            Quat<T> q_conj = Conjugate();
            Quat<T> result = this * p * q_conj;
            return new Vec3<T>(result.X, result.Y, result.Z);
        }

        public Mat3<T> ToMat3()
        {
            T xx = X * X, yy = Y * Y, zz = Z * Z;
            T xy = X * Y, xz = X * Z, yz = Y * Z;
            T wx = W * X, wy = W * Y, wz = W * Z;
            T one = T.One;
            T two = T.CreateChecked(2.0);

            return new Mat3<T>(
                one - two * (yy + zz),       two * (xy - wz),          two * (xz + wy),
                two * (xy + wz),             one - two * (xx + zz),    two * (yz - wx),
                two * (xz - wy),             two * (yz + wx),          one - two * (xx + yy)
            );
        }

        public static Quat<T> Slerp(Quat<T> q1, Quat<T> q2, T t, T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;

            T dot = q1.Dot(q2);

            // If the dot product is negative, the quaternions are more than 90 degrees apart,
            // so we should invert one to take the shorter path.
            if (dot < T.Zero)
            {
                q2 = -q2;
                dot = -dot;
            }

            if (dot > T.One - epsilon) // If quaternions are very close, use linear interpolation
            {
                Quat<T> result = q1 + t * (q2 - q1);
                result.Normalize();
                return result;
            }

            T theta_0 = T.Acos(dot);        // Angle between input quaternions
            T theta = theta_0 * t;          // Angle between q1 and result
            T sin_theta = T.Sin(theta);     // Compute this value only once
            T sin_theta_0 = T.Sin(theta_0); // Compute this value only once

            T s0 = T.Cos(theta) - dot * sin_theta / sin_theta_0;  // == sin(theta_0 - theta) / sin(theta_0)
            T s1 = sin_theta / sin_theta_0;

            return (s0 * q1) + (s1 * q2);
        }


        public override string ToString()
        {
            return $"[{X}, {Y}, {Z}, {W}]";
        }
    }

    // Common type aliases
    public using Quatf = Quat<float>; // In C++ OpenVDB, Quats is float
    public using Quatd = Quat<double>;
}
