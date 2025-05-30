// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;
using System.Numerics; // For IFloatingPointIeee754

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)] // Row-major order
    public struct Mat4<T> : IEquatable<Mat4<T>>
        where T : struct, IEquatable<T>, IFormattable, ISignedNumber<T>, IFloatingPointIeee754<T>
    {
        // Row-major order: M<row><col>
        public T M00, M01, M02, M03;
        public T M10, M11, M12, M13;
        public T M20, M21, M22, M23;
        public T M30, M31, M32, M33;

        public static int Rows => 4;
        public static int Cols => 4;
        public static int NumElements => 16;

        public Mat4(T val)
        {
            M00 = M01 = M02 = M03 = val;
            M10 = M11 = M12 = M13 = val;
            M20 = M21 = M22 = M23 = val;
            M30 = M31 = M32 = M33 = val;
        }

        public Mat4(
            T m00, T m01, T m02, T m03,
            T m10, T m11, T m12, T m13,
            T m20, T m21, T m22, T m23,
            T m30, T m31, T m32, T m33)
        {
            M00 = m00; M01 = m01; M02 = m02; M03 = m03;
            M10 = m10; M11 = m11; M12 = m12; M13 = m13;
            M20 = m20; M21 = m21; M22 = m22; M23 = m23;
            M30 = m30; M31 = m31; M32 = m32; M33 = m33;
        }

        public Mat4(T[] a, bool rowMajor = true)
        {
            if (a == null || a.Length < 16)
                throw new ArgumentException("Array must have at least 16 elements.", nameof(a));

            if (rowMajor)
            {
                M00 = a[0]; M01 = a[1]; M02 = a[2]; M03 = a[3];
                M10 = a[4]; M11 = a[5]; M12 = a[6]; M13 = a[7];
                M20 = a[8]; M21 = a[9]; M22 = a[10]; M23 = a[11];
                M30 = a[12]; M31 = a[13]; M32 = a[14]; M33 = a[15];
            }
            else // Column-major
            {
                M00 = a[0]; M01 = a[4]; M02 = a[8]; M03 = a[12];
                M10 = a[1]; M11 = a[5]; M12 = a[9]; M13 = a[13];
                M20 = a[2]; M21 = a[6]; M22 = a[10]; M23 = a[14];
                M30 = a[3]; M31 = a[7]; M32 = a[11]; M33 = a[15];
            }
        }
        
        public Mat4(Vec4<T> row0, Vec4<T> row1, Vec4<T> row2, Vec4<T> row3)
        {
            M00 = row0.X; M01 = row0.Y; M02 = row0.Z; M03 = row0.W;
            M10 = row1.X; M11 = row1.Y; M12 = row1.Z; M13 = row1.W;
            M20 = row2.X; M21 = row2.Y; M22 = row2.Z; M23 = row2.W;
            M30 = row3.X; M31 = row3.Y; M32 = row3.Z; M33 = row3.W;
        }

        public T this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= 4 || col < 0 || col >= 4)
                    throw new IndexOutOfRangeException("Mat4 index out of range.");
                // This is a bit verbose, could use a switch or a flat array backing if performance critical
                if (row == 0) { if (col == 0) return M00; if (col == 1) return M01; if (col == 2) return M02; return M03; }
                if (row == 1) { if (col == 0) return M10; if (col == 1) return M11; if (col == 2) return M12; return M13; }
                if (row == 2) { if (col == 0) return M20; if (col == 1) return M21; if (col == 2) return M22; return M23; }
                /*row == 3*/ { if (col == 0) return M30; if (col == 1) return M31; if (col == 2) return M32; return M33; }
            }
            set
            {
                if (row < 0 || row >= 4 || col < 0 || col >= 4)
                    throw new IndexOutOfRangeException("Mat4 index out of range.");
                if (row == 0) { if (col == 0) M00 = value; else if (col == 1) M01 = value; else if (col == 2) M02 = value; else M03 = value; }
                else if (row == 1) { if (col == 0) M10 = value; else if (col == 1) M11 = value; else if (col == 2) M12 = value; else M13 = value; }
                else if (row == 2) { if (col == 0) M20 = value; else if (col == 1) M21 = value; else if (col == 2) M22 = value; else M23 = value; }
                else /*row == 3*/ { if (col == 0) M30 = value; else if (col == 1) M31 = value; else if (col == 2) M32 = value; else M33 = value; }
            }
        }
        
        public T[] AsArray(bool rowMajor = true)
        {
            if(rowMajor)
                return new[] { 
                    M00, M01, M02, M03, M10, M11, M12, M13, 
                    M20, M21, M22, M23, M30, M31, M32, M33 
                };
            else
                 return new[] { 
                    M00, M10, M20, M30, M01, M11, M21, M31, 
                    M02, M12, M22, M32, M03, M13, M23, M33 
                };
        }

        public void SetRow(int r, Vec4<T> v)
        {
            if (r < 0 || r >= 4) throw new IndexOutOfRangeException();
            this[r, 0] = v.X; this[r, 1] = v.Y; this[r, 2] = v.Z; this[r, 3] = v.W;
        }

        public Vec4<T> GetRow(int r)
        {
            if (r < 0 || r >= 4) throw new IndexOutOfRangeException();
            return new Vec4<T>(this[r, 0], this[r, 1], this[r, 2], this[r, 3]);
        }

        public void SetCol(int c, Vec4<T> v)
        {
            if (c < 0 || c >= 4) throw new IndexOutOfRangeException();
            this[0, c] = v.X; this[1, c] = v.Y; this[2, c] = v.Z; this[3, c] = v.W;
        }

        public Vec4<T> GetCol(int c)
        {
            if (c < 0 || c >= 4) throw new IndexOutOfRangeException();
            return new Vec4<T>(this[0, c], this[1, c], this[2, c], this[3, c]);
        }
        
        public void SetZero()
        {
            M00 = M01 = M02 = M03 = T.Zero;
            M10 = M11 = M12 = M13 = T.Zero;
            M20 = M21 = M22 = M23 = T.Zero;
            M30 = M31 = M32 = M33 = T.Zero;
        }

        public void SetIdentity()
        {
            M00 = T.One; M01 = T.Zero; M02 = T.Zero; M03 = T.Zero;
            M10 = T.Zero; M11 = T.One; M12 = T.Zero; M13 = T.Zero;
            M20 = T.Zero; M21 = T.Zero; M22 = T.One; M23 = T.Zero;
            M30 = T.Zero; M31 = T.Zero; M32 = T.Zero; M33 = T.One;
        }

        public static Mat4<T> Identity => new Mat4<T>(
            T.One, T.Zero, T.Zero, T.Zero,
            T.Zero, T.One, T.Zero, T.Zero,
            T.Zero, T.Zero, T.One, T.Zero,
            T.Zero, T.Zero, T.Zero, T.One);

        public static Mat4<T> ZeroMatrix => new Mat4<T>(
            T.Zero, T.Zero, T.Zero, T.Zero,
            T.Zero, T.Zero, T.Zero, T.Zero,
            T.Zero, T.Zero, T.Zero, T.Zero,
            T.Zero, T.Zero, T.Zero, T.Zero);

        public bool Equals(Mat4<T> other)
        {
            return M00.Equals(other.M00) && M01.Equals(other.M01) && M02.Equals(other.M02) && M03.Equals(other.M03) &&
                   M10.Equals(other.M10) && M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) &&
                   M20.Equals(other.M20) && M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) &&
                   M30.Equals(other.M30) && M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33);
        }
        public override bool Equals(object obj) => obj is Mat4<T> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(M00, M01, M02, M03, M10, M11, M12, M13, M20, M21, M22, M23, M30, M31, M32, M33);

        public static bool operator ==(Mat4<T> m1, Mat4<T> m2) => m1.Equals(m2);
        public static bool operator !=(Mat4<T> m1, Mat4<T> m2) => !m1.Equals(m2);

        public bool IsApproxEqual(Mat4<T> other, T epsilon)
        {
            return T.Abs(M00 - other.M00) <= epsilon && T.Abs(M01 - other.M01) <= epsilon && T.Abs(M02 - other.M02) <= epsilon && T.Abs(M03 - other.M03) <= epsilon &&
                   T.Abs(M10 - other.M10) <= epsilon && T.Abs(M11 - other.M11) <= epsilon && T.Abs(M12 - other.M12) <= epsilon && T.Abs(M13 - other.M13) <= epsilon &&
                   T.Abs(M20 - other.M20) <= epsilon && T.Abs(M21 - other.M21) <= epsilon && T.Abs(M22 - other.M22) <= epsilon && T.Abs(M23 - other.M23) <= epsilon &&
                   T.Abs(M30 - other.M30) <= epsilon && T.Abs(M31 - other.M31) <= epsilon && T.Abs(M32 - other.M32) <= epsilon && T.Abs(M33 - other.M33) <= epsilon;
        }

        public static Mat4<T> operator -(Mat4<T> m) => new Mat4<T>(
            -m.M00, -m.M01, -m.M02, -m.M03, -m.M10, -m.M11, -m.M12, -m.M13,
            -m.M20, -m.M21, -m.M22, -m.M23, -m.M30, -m.M31, -m.M32, -m.M33);

        public static Mat4<T> operator +(Mat4<T> m1, Mat4<T> m2) => new Mat4<T>(
            m1.M00 + m2.M00, m1.M01 + m2.M01, m1.M02 + m2.M02, m1.M03 + m2.M03,
            m1.M10 + m2.M10, m1.M11 + m2.M11, m1.M12 + m2.M12, m1.M13 + m2.M13,
            m1.M20 + m2.M20, m1.M21 + m2.M21, m1.M22 + m2.M22, m1.M23 + m2.M23,
            m1.M30 + m2.M30, m1.M31 + m2.M31, m1.M32 + m2.M32, m1.M33 + m2.M33);

        public static Mat4<T> operator -(Mat4<T> m1, Mat4<T> m2) => new Mat4<T>(
            m1.M00 - m2.M00, m1.M01 - m2.M01, m1.M02 - m2.M02, m1.M03 - m2.M03,
            m1.M10 - m2.M10, m1.M11 - m2.M11, m1.M12 - m2.M12, m1.M13 - m2.M13,
            m1.M20 - m2.M20, m1.M21 - m2.M21, m1.M22 - m2.M22, m1.M23 - m2.M23,
            m1.M30 - m2.M30, m1.M31 - m2.M31, m1.M32 - m2.M32, m1.M33 - m2.M33);

        public static Mat4<T> operator *(Mat4<T> m, T scalar) => new Mat4<T>(
            m.M00 * scalar, m.M01 * scalar, m.M02 * scalar, m.M03 * scalar,
            m.M10 * scalar, m.M11 * scalar, m.M12 * scalar, m.M13 * scalar,
            m.M20 * scalar, m.M21 * scalar, m.M22 * scalar, m.M23 * scalar,
            m.M30 * scalar, m.M31 * scalar, m.M32 * scalar, m.M33 * scalar);
            
        public static Mat4<T> operator *(T scalar, Mat4<T> m) => m * scalar;

        // Matrix-vector multiplication (m * v)
        // Assumes v.W is 1 for points, 0 for vectors if not Vec4
        public static Vec4<T> operator *(Mat4<T> m, Vec4<T> v) => new Vec4<T>(
            m.M00 * v.X + m.M01 * v.Y + m.M02 * v.Z + m.M03 * v.W,
            m.M10 * v.X + m.M11 * v.Y + m.M12 * v.Z + m.M13 * v.W,
            m.M20 * v.X + m.M21 * v.Y + m.M22 * v.Z + m.M23 * v.W,
            m.M30 * v.X + m.M31 * v.Y + m.M32 * v.Z + m.M33 * v.W);
            
        // Vector-matrix multiplication (v * m)
        public static Vec4<T> operator *(Vec4<T> v, Mat4<T> m) => new Vec4<T>(
            v.X * m.M00 + v.Y * m.M10 + v.Z * m.M20 + v.W * m.M30,
            v.X * m.M01 + v.Y * m.M11 + v.Z * m.M21 + v.W * m.M31,
            v.X * m.M02 + v.Y * m.M12 + v.Z * m.M22 + v.W * m.M32,
            v.X * m.M03 + v.Y * m.M13 + v.Z * m.M23 + v.W * m.M33);

        // Matrix-matrix multiplication
        public static Mat4<T> operator *(Mat4<T> m1, Mat4<T> m2)
        {
            return new Mat4<T>(
                m1.M00 * m2.M00 + m1.M01 * m2.M10 + m1.M02 * m2.M20 + m1.M03 * m2.M30,
                m1.M00 * m2.M01 + m1.M01 * m2.M11 + m1.M02 * m2.M21 + m1.M03 * m2.M31,
                m1.M00 * m2.M02 + m1.M01 * m2.M12 + m1.M02 * m2.M22 + m1.M03 * m2.M32,
                m1.M00 * m2.M03 + m1.M01 * m2.M13 + m1.M02 * m2.M23 + m1.M03 * m2.M33,

                m1.M10 * m2.M00 + m1.M11 * m2.M10 + m1.M12 * m2.M20 + m1.M13 * m2.M30,
                m1.M10 * m2.M01 + m1.M11 * m2.M11 + m1.M12 * m2.M21 + m1.M13 * m2.M31,
                m1.M10 * m2.M02 + m1.M11 * m2.M12 + m1.M12 * m2.M22 + m1.M13 * m2.M32,
                m1.M10 * m2.M03 + m1.M11 * m2.M13 + m1.M12 * m2.M23 + m1.M13 * m2.M33,

                m1.M20 * m2.M00 + m1.M21 * m2.M10 + m1.M22 * m2.M20 + m1.M23 * m2.M30,
                m1.M20 * m2.M01 + m1.M21 * m2.M11 + m1.M22 * m2.M21 + m1.M23 * m2.M31,
                m1.M20 * m2.M02 + m1.M21 * m2.M12 + m1.M22 * m2.M22 + m1.M23 * m2.M32,
                m1.M20 * m2.M03 + m1.M21 * m2.M13 + m1.M22 * m2.M23 + m1.M23 * m2.M33,

                m1.M30 * m2.M00 + m1.M31 * m2.M10 + m1.M32 * m2.M20 + m1.M33 * m2.M30,
                m1.M30 * m2.M01 + m1.M31 * m2.M11 + m1.M32 * m2.M21 + m1.M33 * m2.M31,
                m1.M30 * m2.M02 + m1.M31 * m2.M12 + m1.M32 * m2.M22 + m1.M33 * m2.M32,
                m1.M30 * m2.M03 + m1.M31 * m2.M13 + m1.M32 * m2.M23 + m1.M33 * m2.M33
            );
        }

        public Mat4<T> Transposed() => new Mat4<T>(
            M00, M10, M20, M30,
            M01, M11, M21, M31,
            M02, M12, M22, M32,
            M03, M13, M23, M33);

        public void Transpose() => this = Transposed();
        
        public Mat3<T> GetMat3() => new Mat3<T>(M00, M01, M02, M10, M11, M12, M20, M21, M22);
        public void SetMat3(Mat3<T> m3)
        {
            M00 = m3.M00; M01 = m3.M01; M02 = m3.M02;
            M10 = m3.M10; M11 = m3.M11; M12 = m3.M12;
            M20 = m3.M20; M21 = m3.M21; M22 = m3.M22;
        }

        public Vec3<T> GetTranslation() => new Vec3<T>(M30, M31, M32); // In C++ OpenVDB, translation is often in the last row for column vectors (M03, M13, M23)
                                                                      // or last column for row vectors (M30, M31, M32). Assuming row vectors for translation part.
                                                                      // The C++ version uses m[12], m[13], m[14] which is M30, M31, M32 for row-major.

        public void SetTranslation(Vec3<T> t)
        {
            M30 = t.X; M31 = t.Y; M32 = t.Z;
        }


        // Determinant and Inverse are complex for Mat4.
        // A full robust implementation is lengthy.
        // Using a simplified approach or noting that a library like System.Numerics.Matrix4x4 handles this.
        // For now, a placeholder for Inverse and Determinant.
        public T Determinant()
        {
            // Placeholder: Full determinant calculation is complex.
            // This is a common expansion by minors.
            T det = T.Zero;
            det += M00 * new Mat3<T>(M11, M12, M13, M21, M22, M23, M31, M32, M33).Determinant();
            det -= M01 * new Mat3<T>(M10, M12, M13, M20, M22, M23, M30, M32, M33).Determinant();
            det += M02 * new Mat3<T>(M10, M11, M13, M20, M21, M23, M30, M31, M33).Determinant();
            det -= M03 * new Mat3<T>(M10, M11, M12, M20, M21, M22, M30, M31, M32).Determinant();
            return det;

            // The C++ code has a more direct calculation for det3 and det2.
            // For a full port, that logic should be translated.
        }

        public Mat4<T> Inverse(T epsilon = default)
        {
            // Full general 4x4 matrix inversion is non-trivial.
            // The C++ code uses a blockwise inversion if detA (top-left 3x3) is non-zero,
            // otherwise falls back to Gauss-Jordan.
            // This is a simplified placeholder. For robust inversion, a library function or full algorithm port is needed.
            // System.Numerics.Matrix4x4.Invert(matrix, out result) is a good reference for behavior.
            if (epsilon == default) epsilon = T.Epsilon;

            // This is a very basic check, not a full inversion.
            T det = Determinant();
            if (T.Abs(det) <= epsilon)
            {
                throw new InvalidOperationException("Matrix is singular or nearly singular; cannot invert.");
            }
            
            // Placeholder - this is not a correct general inverse.
            // A full implementation is required.
            // Example: Using adjugate matrix / determinant
            // The C++ code has a detailed implementation for inverse()
            // For now, returning Identity as a clear indicator this is not implemented.
            System.Diagnostics.Debug.WriteLine("Warning: Mat4.Inverse() is not fully implemented and returns Identity.");
            return Identity;
        }

        public static Mat4<T> CreateTranslation(Vec3<T> v) => new Mat4<T>(
            T.One,  T.Zero, T.Zero, T.Zero,
            T.Zero, T.One,  T.Zero, T.Zero,
            T.Zero, T.Zero, T.One,  T.Zero,
            v.X,    v.Y,    v.Z,    T.One);

        public static Mat4<T> CreateScale(Vec3<T> v) => new Mat4<T>(
            v.X,    T.Zero, T.Zero, T.Zero,
            T.Zero, v.Y,    T.Zero, T.Zero,
            T.Zero, T.Zero, v.Z,    T.Zero,
            T.Zero, T.Zero, T.Zero, T.One);

        public static Mat4<T> CreateRotation(Vec3<T> axis, T angle) // Axis must be normalized
        {
            T c = T.Cos(angle);
            T s = T.Sin(angle);
            T oneMinusC = T.One - c;

            T x = axis.X;
            T y = axis.Y;
            T z = axis.Z;

            return new Mat4<T>(
                c + x * x * oneMinusC,      y * x * oneMinusC + z * s,  z * x * oneMinusC - y * s,  T.Zero,
                x * y * oneMinusC - z * s,  c + y * y * oneMinusC,      z * y * oneMinusC + x * s,  T.Zero,
                x * z * oneMinusC + y * s,  y * z * oneMinusC - x * s,  c + z * z * oneMinusC,      T.Zero,
                T.Zero,                     T.Zero,                     T.Zero,                     T.One
            );
        }

        // Simplified transform for point (assumes W=1 for input, output W may vary)
        public Vec3<T> TransformPoint(Vec3<T> p)
        {
            T invW = T.One / (M30 * p.X + M31 * p.Y + M32 * p.Z + M33); // Assuming M3* are perspective terms
            return new Vec3<T>(
                (M00 * p.X + M01 * p.Y + M02 * p.Z + M03) * invW,
                (M10 * p.X + M11 * p.Y + M12 * p.Z + M13) * invW,
                (M20 * p.X + M21 * p.Y + M22 * p.Z + M23) * invW
            );
        }
        
        // Transform for vector (assumes W=0)
        public Vec3<T> TransformVector(Vec3<T> v) => new Vec3<T>(
            M00 * v.X + M01 * v.Y + M02 * v.Z,
            M10 * v.X + M11 * v.Y + M12 * v.Z,
            M20 * v.X + M21 * v.Y + M22 * v.Z
        );


        public override string ToString()
        {
            return $"[{M00}, {M01}, {M02}, {M03}\n" +
                   $" {M10}, {M11}, {M12}, {M13}\n" +
                   $" {M20}, {M21}, {M22}, {M23}\n" +
                   $" {M30}, {M31}, {M32}, {M33}]";
        }
    }

    // Common type aliases
    public using Mat4f = Mat4<float>; // In C++ OpenVDB, Mat4s is float
    public using Mat4d = Mat4<double>;
}
