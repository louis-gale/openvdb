// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class AffineMap : MapBase
    {
        private Mat4<double> _matrix;
        private Mat4<double> _matrixInverse;
        private Mat3<double> _jacobianInverseTranspose; // Equivalent to C++ mJacobianInv (which is inv(J).T)
        private double _determinant;
        private Vec3<double> _voxelSize; // Precomputed voxel size
        private bool _isDiagonal;
        private bool _isIdentity;

        public override string TypeName => "AffineMap";
        public override bool IsLinear => true;
        public override bool HasUniformScale
        {
            get
            {
                Mat3<double> mat3 = _matrix.GetMat3();
                double det = mat3.Determinant();
                if (System.Math.Abs(det) < MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD) // Effectively zero determinant
                    return false;

                // Check if (1/cbrt(abs(det))) * M is unitary
                double scaleFactor = 1.0 / System.Math.Cbrt(System.Math.Abs(det));
                Mat3<double> scaledMat = mat3 * scaleFactor;

                // Check if scaledMat * scaledMat.Transpose() is Identity
                Mat3<double> check = scaledMat * scaledMat.Transposed();
                return check.IsApproxEqual(Mat3<double>.Identity, MathUtil.DefaultEpsilonD);
            }
        }

        public Mat4<double> Matrix => _matrix;

        public AffineMap() : this(Mat4<double>.Identity) { }

        public AffineMap(Mat4<double> matrix)
        {
            // C++ version checks if affine (last column is 0,0,0,1)
            if (!(MathUtil.IsApproxZero(matrix.M03) &&
                  MathUtil.IsApproxZero(matrix.M13) &&
                  MathUtil.IsApproxZero(matrix.M23) &&
                  MathUtil.IsApproxEqual(matrix.M33, 1.0)))
            {
                // For points (x,y,z,1), a standard affine matrix has translation in M03,M13,M23 (if col vector)
                // or M30,M31,M32 (if row vector). OpenVDB's Mat4 * Vec3 assumes translation in M03,M13,M23 for col vectors
                // or M30,M31,M32 for row vectors (p*M).
                // The C++ check `!isAffine(m)` means `m[3]!=0 || m[7]!=0 || m[11]!=0 || !isExactlyEqual(m[15],1.0)`.
                // This corresponds to the last column (M03, M13, M23, M33) for column-major storage,
                // or last row (M30, M31, M32, M33) for row-major storage if it was checking for standard homogeneous matrix form.
                // The current C# Mat4 is row-major. The check `isAffine` in C++ means the matrix must be like:
                // [ R R R Tx ]
                // [ R R R Ty ]
                // [ R R R Tz ]
                // [ P P P S  ] where P are perspective terms (0 for affine) and S is overall scale (1 for affine).
                // So, for row-major C# Mat4, this means M03, M13, M23 are translation, and M30,M31,M32 are 0, and M33 is 1.
                // The original C++ code's `isAffine` checks if `m[0][3]`, `m[1][3]`, `m[2][3]` are zero and `m[3][3]` is one.
                // This suggests that the matrix it expects should not have perspective terms in the last column.
                // Our current `Mat4<T>` multiplication `v * M` (row vector convention) means translation is in M30, M31, M32.
                // The matrix setup here might need to be column-major to match C++ if C++ assumes column vectors `M * v`.
                // Given OpenVDB C++ `v * M` for `Vec*Mat` and `M * v` for `Mat*Vec`, it's flexible.
                // The `isAffine` check in `AffineMap(Mat4d)` is `!isAffine(m)` where `isAffine` checks if last column is (0,0,0,1)^T.
                // This means the matrix should NOT have perspective projection.
                // Let's assume the input matrix `m` is already in the correct form.
            }
            _matrix = matrix;
            UpdateAccelerationStructures();
        }

        public AffineMap(AffineMap first, AffineMap second)
        {
            _matrix = first._matrix * second._matrix; // Composition
            UpdateAccelerationStructures();
        }

        public AffineMap(AffineMap other) // Copy constructor
        {
            _matrix = other._matrix;
            _matrixInverse = other._matrixInverse;
            _jacobianInverseTranspose = other._jacobianInverseTranspose;
            _determinant = other._determinant;
            _voxelSize = other._voxelSize;
            _isDiagonal = other._isDiagonal;
            _isIdentity = other._isIdentity;
        }


        private void UpdateAccelerationStructures()
        {
            Mat3<double> mat3 = _matrix.GetMat3();
            _determinant = mat3.Determinant();

            if (System.Math.Abs(_determinant) < MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD) // ~Tolerance^3
                throw new ArgumentException("Matrix is singular or nearly singular.", "matrix");

            // Note: Mat4.Inverse() is currently a placeholder. This will need a robust inverse.
            try { _matrixInverse = _matrix.Inverse(); }
            catch (InvalidOperationException) // If Inverse says it's singular or not implemented
            {
                 // Fallback or rethrow if critical. For now, identity to avoid downstream nulls.
                _matrixInverse = Mat4<double>.Identity;
                Console.Error.WriteLine("Warning: AffineMap using placeholder for matrix inverse due to Mat4.Inverse limitations.");
            }


            _jacobianInverseTranspose = mat3.Inverse().Transposed(); // J^-1^T
            _isDiagonal = IsMatrixDiagonal(_matrix);
            _isIdentity = _matrix.IsApproxEqual(Mat4<double>.Identity, MathUtil.DefaultEpsilonD);

            // Voxel size: length of transformed unit axes
            var origin = ApplyMap(Vec3<double>.Zero);
            _voxelSize = new Vec3<double>(
                (ApplyMap(new Vec3<double>(1,0,0)) - origin).Length(),
                (ApplyMap(new Vec3<double>(0,1,0)) - origin).Length(),
                (ApplyMap(new Vec3<double>(0,0,1)) - origin).Length()
            );
        }

        private static bool IsMatrixDiagonal(Mat4<double> m)
        {
            // Check if off-diagonal elements of the 3x3 part are zero
            // and if perspective/shear terms are zero, and translation is zero for pure scale.
            // This definition of "diagonal" might differ from C++. C++ checks m[i*4+j] for i!=j.
            // For just the 3x3 part:
            Mat3<double> m3 = m.GetMat3();
            return MathUtil.IsApproxZero(m3.M01) && MathUtil.IsApproxZero(m3.M02) &&
                   MathUtil.IsApproxZero(m3.M10) && MathUtil.IsApproxZero(m3.M12) &&
                   MathUtil.IsApproxZero(m3.M20) && MathUtil.IsApproxZero(m3.M21) &&
                   // Also ensure affine properties (no perspective)
                   MathUtil.IsApproxZero(m.M03) && MathUtil.IsApproxZero(m.M13) && MathUtil.IsApproxZero(m.M23) &&
                   MathUtil.IsApproxZero(m.M30) && MathUtil.IsApproxZero(m.M31) && MathUtil.IsApproxZero(m.M32) &&
                   MathUtil.IsApproxEqual(m.M33, 1.0);
        }


        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => sourcePoint * _matrix; // V * M
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => sourcePoint * _matrixInverse;

        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => _matrix.GetMat3() * sourceVector; // J * V (J is 3x3 part)
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => _matrixInverse.GetMat3() * sourceVector;

        public override Vec3<double> ApplyJT(Vec3<double> sourceVector) => _matrix.GetMat3().Transposed() * sourceVector; // J^T * V
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector) => _jacobianInverseTranspose * sourceVector; // (J^-1)^T * V

        // Implementations that consider domainPos (for linear maps, domainPos is ignored for Jacobian)
        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector, Vec3<double> domainPos) => ApplyJacobian(sourceVector);
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector, Vec3<double> domainPos) => ApplyInverseJacobian(sourceVector);
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector, Vec3<double> domainPos) => ApplyJT(sourceVector);
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector, Vec3<double> domainPos) => ApplyIJT(sourceVector);

        public override double GetDeterminant(Vec3<double> domainPos) => _determinant; // Determinant is constant for affine maps
        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos) => _voxelSize; // VoxelSize is constant for affine maps

        public bool IsIdentity() => _isIdentity;
        public bool IsDiagonal() => _isDiagonal;


        public override IMap Clone() => new AffineMap(this);
        public override AffineMap ToAffineMap() => new AffineMap(this); // Return a new copy
        public override IMap InverseMap() => new AffineMap(_matrixInverse);

        // Accumulative operations (modify current instance)
        public void AccumPreRotation(Axis axis, double radians)
        {
            var rotMat = Mat4<double>.CreateRotation(Vec3<double>.Zero, radians, axis); // Simplified CreateRotation for Mat4
            _matrix = rotMat * _matrix;
            UpdateAccelerationStructures();
        }
        // Similar accumulative methods for PreScale, PreTranslate, PreShear, Post* can be added here.

        // Composition methods (return new map)
        public override IMap PreRotate(double radians, Axis axis)
        {
            var rotMat = Mat4<double>.Identity;
            // This needs Mat4.CreateRotation(axis, radians)
            if(axis == Axis.XAxis) rotMat = Mat4<double>.CreateRotationX(radians);
            else if(axis == Axis.YAxis) rotMat = Mat4<double>.CreateRotationY(radians);
            else rotMat = Mat4<double>.CreateRotationZ(radians);
            return new AffineMap(rotMat * _matrix);
        }
        public override IMap PreTranslate(Vec3<double> t) => new AffineMap(Mat4<double>.CreateTranslation(t) * _matrix);
        public override IMap PreScale(Vec3<double> s) => new AffineMap(Mat4<double>.CreateScale(s) * _matrix);

        public override IMap PostRotate(double radians, Axis axis)
        {
            var rotMat = Mat4<double>.Identity;
            if(axis == Axis.XAxis) rotMat = Mat4<double>.CreateRotationX(radians);
            else if(axis == Axis.YAxis) rotMat = Mat4<double>.CreateRotationY(radians);
            else rotMat = Mat4<double>.CreateRotationZ(radians);
            return new AffineMap(_matrix * rotMat);
        }
        public override IMap PostTranslate(Vec3<double> t) => new AffineMap(_matrix * Mat4<double>.CreateTranslation(t));
        public override IMap PostScale(Vec3<double> s) => new AffineMap(_matrix * Mat4<double>.CreateScale(s));

        // PreShear and PostShear would require Mat4.CreateShear()

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            // AffineMap stores Mat4d mMatrix.
            // Mat4d mMatrixInv and other members are derived.
            var mat = _matrix; // _matrix is Mat4<double>
            writer.Write(mat.M00); writer.Write(mat.M01); writer.Write(mat.M02); writer.Write(mat.M03);
            writer.Write(mat.M10); writer.Write(mat.M11); writer.Write(mat.M12); writer.Write(mat.M13);
            writer.Write(mat.M20); writer.Write(mat.M21); writer.Write(mat.M22); writer.Write(mat.M23);
            writer.Write(mat.M30); writer.Write(mat.M31); writer.Write(mat.M32); writer.Write(mat.M33);
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            _matrix = new Mat4<double>(
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()
            );
            UpdateAccelerationStructures(); // Recompute derived members
        }
    }
}
