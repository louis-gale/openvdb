// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System.Numerics; // For IFloatingPointIeee754

namespace OpenVDB.Math
{
    [Serializable]
    // [StructLayout(LayoutKind.Sequential)] // Row-major order by default for C#
    public struct Mat3<T> : IEquatable<Mat3<T>>
        where T : struct, IEquatable<T>, IFormattable, IFloatingPointIeee754<T>
    {
        // Values are stored in row-major order:
        // M00, M01, M02
        // M10, M11, M12
        // M20, M21, M22
        public T M00, M01, M02;
        public T M10, M11, M12;
        public T M20, M21, M22;

        public static int Rows => 3;
        public static int Cols => 3;
        public static int NumElements => 9;

        public Mat3(T val)
        {
            M00 = M01 = M02 = val;
            M10 = M11 = M12 = val;
            M20 = M21 = M22 = val;
        }

        public Mat3(
            T m00, T m01, T m02,
            T m10, T m11, T m12,
            T m20, T m21, T m22)
        {
            M00 = m00; M01 = m01; M02 = m02;
            M10 = m10; M11 = m11; M12 = m12;
            M20 = m20; M21 = m21; M22 = m22;
        }

        public Mat3(T[] a, bool rowMajor = true)
        {
            if (a == null || a.Length < 9)
                throw new ArgumentException("Array must have at least 9 elements.", nameof(a));

            if (rowMajor)
            {
                M00 = a[0]; M01 = a[1]; M02 = a[2];
                M10 = a[3]; M11 = a[4]; M12 = a[5];
                M20 = a[6]; M21 = a[7]; M22 = a[8];
            }
            else // Column-major
            {
                M00 = a[0]; M01 = a[3]; M02 = a[6];
                M10 = a[1]; M11 = a[4]; M12 = a[7];
                M20 = a[2]; M21 = a[5]; M22 = a[8];
            }
        }

        public Mat3(Vec3<T> row0, Vec3<T> row1, Vec3<T> row2)
        {
            M00 = row0.X; M01 = row0.Y; M02 = row0.Z;
            M10 = row1.X; M11 = row1.Y; M12 = row1.Z;
            M20 = row2.X; M21 = row2.Y; M22 = row2.Z;
        }

        public Mat3(Quat<T> q)
        {
            this = q.ToMat3();
        }

        public Mat3(Mat4<T> m4) // From Mat4, takes upper-left 3x3
        {
            M00 = m4.M00; M01 = m4.M01; M02 = m4.M02;
            M10 = m4.M10; M11 = m4.M11; M12 = m4.M12;
            M20 = m4.M20; M21 = m4.M21; M22 = m4.M22;
        }


        public T this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= 3 || col < 0 || col >= 3)
                    throw new IndexOutOfRangeException("Mat3 index out of range.");
                if (row == 0) { if (col == 0) return M00; if (col == 1) return M01; return M02; }
                if (row == 1) { if (col == 0) return M10; if (col == 1) return M11; return M12; }
                /*row == 2*/ { if (col == 0) return M20; if (col == 1) return M21; return M22; }
            }
            set
            {
                if (row < 0 || row >= 3 || col < 0 || col >= 3)
                    throw new IndexOutOfRangeException("Mat3 index out of range.");
                if (row == 0) { if (col == 0) M00 = value; else if (col == 1) M01 = value; else M02 = value; }
                else if (row == 1) { if (col == 0) M10 = value; else if (col == 1) M11 = value; else M12 = value; }
                else /*row == 2*/ { if (col == 0) M20 = value; else if (col == 1) M21 = value; else M22 = value; }
            }
        }

        public T[] AsArray(bool rowMajor = true)
        {
            if(rowMajor)
                return new[] { M00, M01, M02, M10, M11, M12, M20, M21, M22 };
            else
                return new[] { M00, M10, M20, M01, M11, M21, M02, M12, M22 };
        }

        public void SetRow(int r, Vec3<T> v)
        {
            if (r < 0 || r >= 3) throw new IndexOutOfRangeException();
            this[r, 0] = v.X; this[r, 1] = v.Y; this[r, 2] = v.Z;
        }

        public Vec3<T> GetRow(int r)
        {
            if (r < 0 || r >= 3) throw new IndexOutOfRangeException();
            return new Vec3<T>(this[r, 0], this[r, 1], this[r, 2]);
        }

        public void SetCol(int c, Vec3<T> v)
        {
            if (c < 0 || c >= 3) throw new IndexOutOfRangeException();
            this[0, c] = v.X; this[1, c] = v.Y; this[2, c] = v.Z;
        }

        public Vec3<T> GetCol(int c)
        {
            if (c < 0 || c >= 3) throw new IndexOutOfRangeException();
            return new Vec3<T>(this[0, c], this[1, c], this[2, c]);
        }

        public void SetRows(Vec3<T> v0, Vec3<T> v1, Vec3<T> v2)
        {
            M00 = v0.X; M01 = v0.Y; M02 = v0.Z;
            M10 = v1.X; M11 = v1.Y; M12 = v1.Z;
            M20 = v2.X; M21 = v2.Y; M22 = v2.Z;
        }

        public void SetColumns(Vec3<T> v0, Vec3<T> v1, Vec3<T> v2)
        {
            M00 = v0.X; M01 = v1.X; M02 = v2.X;
            M10 = v0.Y; M11 = v1.Y; M12 = v2.Y;
            M20 = v0.Z; M21 = v1.Z; M22 = v2.Z;
        }

        public void SetZero()
        {
            M00 = M01 = M02 = T.Zero;
            M10 = M11 = M12 = T.Zero;
            M20 = M21 = M22 = T.Zero;
        }

        public void SetIdentity()
        {
            M00 = T.One; M01 = T.Zero; M02 = T.Zero;
            M10 = T.Zero; M11 = T.One; M12 = T.Zero;
            M20 = T.Zero; M21 = T.Zero; M22 = T.One;
        }

        public static Mat3<T> Identity => new Mat3<T>(
            T.One, T.Zero, T.Zero,
            T.Zero, T.One, T.Zero,
            T.Zero, T.Zero, T.One);

        public static Mat3<T> ZeroMatrix => new Mat3<T>(
            T.Zero, T.Zero, T.Zero,
            T.Zero, T.Zero, T.Zero,
            T.Zero, T.Zero, T.Zero);

        public bool Equals(Mat3<T> other)
        {
            return M00.Equals(other.M00) && M01.Equals(other.M01) && M02.Equals(other.M02) &&
                   M10.Equals(other.M10) && M11.Equals(other.M11) && M12.Equals(other.M12) &&
                   M20.Equals(other.M20) && M21.Equals(other.M21) && M22.Equals(other.M22);
        }

        public override bool Equals(object obj)
        {
            return obj is Mat3<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash1 = HashCode.Combine(M00, M01, M02, M10, M11);
            var hash2 = HashCode.Combine(M12, M20, M21, M22);
            
            return HashCode.Combine(hash1, hash2);
        }

        public static bool operator ==(Mat3<T> m1, Mat3<T> m2) => m1.Equals(m2);
        public static bool operator !=(Mat3<T> m1, Mat3<T> m2) => !m1.Equals(m2);

        public bool IsApproxEqual(Mat3<T> other, T epsilon)
        {
            return T.Abs(M00 - other.M00) <= epsilon && T.Abs(M01 - other.M01) <= epsilon && T.Abs(M02 - other.M02) <= epsilon &&
                   T.Abs(M10 - other.M10) <= epsilon && T.Abs(M11 - other.M11) <= epsilon && T.Abs(M12 - other.M12) <= epsilon &&
                   T.Abs(M20 - other.M20) <= epsilon && T.Abs(M21 - other.M21) <= epsilon && T.Abs(M22 - other.M22) <= epsilon;
        }

        public static Mat3<T> operator -(Mat3<T> m) => new Mat3<T>(
            -m.M00, -m.M01, -m.M02,
            -m.M10, -m.M11, -m.M12,
            -m.M20, -m.M21, -m.M22);

        public static Mat3<T> operator +(Mat3<T> m1, Mat3<T> m2) => new Mat3<T>(
            m1.M00 + m2.M00, m1.M01 + m2.M01, m1.M02 + m2.M02,
            m1.M10 + m2.M10, m1.M11 + m2.M11, m1.M12 + m2.M12,
            m1.M20 + m2.M20, m1.M21 + m2.M21, m1.M22 + m2.M22);

        public static Mat3<T> operator -(Mat3<T> m1, Mat3<T> m2) => new Mat3<T>(
            m1.M00 - m2.M00, m1.M01 - m2.M01, m1.M02 - m2.M02,
            m1.M10 - m2.M10, m1.M11 - m2.M11, m1.M12 - m2.M12,
            m1.M20 - m2.M20, m1.M21 - m2.M21, m1.M22 - m2.M22);

        public static Mat3<T> operator *(Mat3<T> m, T scalar) => new Mat3<T>(
            m.M00 * scalar, m.M01 * scalar, m.M02 * scalar,
            m.M10 * scalar, m.M11 * scalar, m.M12 * scalar,
            m.M20 * scalar, m.M21 * scalar, m.M22 * scalar);

        public static Mat3<T> operator *(T scalar, Mat3<T> m) => m * scalar;

        // Matrix-vector multiplication (m * v)
        public static Vec3<T> operator *(Mat3<T> m, Vec3<T> v) => new Vec3<T>(
            m.M00 * v.X + m.M01 * v.Y + m.M02 * v.Z,
            m.M10 * v.X + m.M11 * v.Y + m.M12 * v.Z,
            m.M20 * v.X + m.M21 * v.Y + m.M22 * v.Z);

        // Vector-matrix multiplication (v * m)
        public static Vec3<T> operator *(Vec3<T> v, Mat3<T> m) => new Vec3<T>(
            v.X * m.M00 + v.Y * m.M10 + v.Z * m.M20,
            v.X * m.M01 + v.Y * m.M11 + v.Z * m.M21,
            v.X * m.M02 + v.Y * m.M12 + v.Z * m.M22);

        // Matrix-matrix multiplication
        public static Mat3<T> operator *(Mat3<T> m1, Mat3<T> m2) => new Mat3<T>(
            m1.M00 * m2.M00 + m1.M01 * m2.M10 + m1.M02 * m2.M20,
            m1.M00 * m2.M01 + m1.M01 * m2.M11 + m1.M02 * m2.M21,
            m1.M00 * m2.M02 + m1.M01 * m2.M12 + m1.M02 * m2.M22,

            m1.M10 * m2.M00 + m1.M11 * m2.M10 + m1.M12 * m2.M20,
            m1.M10 * m2.M01 + m1.M11 * m2.M11 + m1.M12 * m2.M21,
            m1.M10 * m2.M02 + m1.M11 * m2.M12 + m1.M12 * m2.M22,

            m1.M20 * m2.M00 + m1.M21 * m2.M10 + m1.M22 * m2.M20,
            m1.M20 * m2.M01 + m1.M21 * m2.M11 + m1.M22 * m2.M21,
            m1.M20 * m2.M02 + m1.M21 * m2.M12 + m1.M22 * m2.M22);

        public Mat3<T> Transposed() => new Mat3<T>(
            M00, M10, M20,
            M01, M11, M21,
            M02, M12, M22);

        public void Transpose() => this = Transposed();

        public T Determinant()
        {
            return M00 * (M11 * M22 - M12 * M21) -
                   M01 * (M10 * M22 - M12 * M20) +
                   M02 * (M10 * M21 - M11 * M20);
        }

        public T Trace() => M00 + M11 + M22;

        public Mat3<T> Adjoint() => new Mat3<T>(
            M11 * M22 - M12 * M21, M02 * M21 - M01 * M22, M01 * M12 - M02 * M11,
            M12 * M20 - M10 * M22, M00 * M22 - M02 * M20, M02 * M10 - M00 * M12,
            M10 * M21 - M11 * M20, M01 * M20 - M00 * M21, M00 * M11 - M01 * M10);

        public Mat3<T> Inverse(T epsilon = default)
        {
            if (epsilon == default) epsilon = T.Epsilon;
            T det = Determinant();
            if (T.Abs(det) <= epsilon)
            {
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
            }
            return Adjoint() * (T.One / det);
        }

        public static Mat3<T> CreateRotation(Vec3<T> axis, T angle)
        {
            T cosAngle = T.Cos(angle);
            T sinAngle = T.Sin(angle);
            T oneMinusCos = T.One - cosAngle;

            axis.Normalize(); // Ensure axis is unit length

            return new Mat3<T>(
                cosAngle + axis.X * axis.X * oneMinusCos,
                axis.X * axis.Y * oneMinusCos - axis.Z * sinAngle,
                axis.X * axis.Z * oneMinusCos + axis.Y * sinAngle,

                axis.Y * axis.X * oneMinusCos + axis.Z * sinAngle,
                cosAngle + axis.Y * axis.Y * oneMinusCos,
                axis.Y * axis.Z * oneMinusCos - axis.X * sinAngle,

                axis.Z * axis.X * oneMinusCos - axis.Y * sinAngle,
                axis.Z * axis.Y * oneMinusCos + axis.X * sinAngle,
                cosAngle + axis.Z * axis.Z * oneMinusCos
            );
        }

        public static Mat3<T> CreateSkewSymmetric(Vec3<T> v) => new Mat3<T>(
            T.Zero, -v.Z,  v.Y,
             v.Z, T.Zero, -v.X,
            -v.Y,  v.X, T.Zero);


        public override string ToString()
        {
            return $"[{M00}, {M01}, {M02}\n {M10}, {M11}, {M12}\n {M20}, {M21}, {M22}]";
        }
    }
}
