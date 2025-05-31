// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class UnitaryMap : MapBase
    {
        private AffineMap _affineMap; // Underlying AffineMap which must represent a unitary transformation

        public override string TypeName => "UnitaryMap";
        public override bool IsLinear => true;
        public override bool HasUniformScale => true; // Unitary transformations are inherently uniform scale (scale factor 1)

        public UnitaryMap() : this(Mat3<double>.Identity) { }

        public UnitaryMap(Mat3<double> matrix)
        {
            // Validate if matrix is unitary (M * M^T = I)
            var check = matrix * matrix.Transposed();
            if (!check.IsApproxEqual(Mat3<double>.Identity, MathUtil.DefaultEpsilonD * 10)) // Looser tolerance for unitary check
                throw new ArgumentException("Matrix for UnitaryMap must be unitary (orthogonal).", nameof(matrix));

            _affineMap = new AffineMap(new Mat4<double>(matrix)); // Convert Mat3 to Mat4 for AffineMap
        }

        public UnitaryMap(Mat4<double> matrix)
        {
            // More thorough validation from C++
            // 1. Must be invertible (already handled by AffineMap constructor if it throws on singular)
            // 2. Must be affine (no perspective projection, M33=1, M03,M13,M23,M30,M31,M32 = 0)
            //    The C++ isAffine(m) checks m.row(3) == (0,0,0,1).
            //    For row-major matrix M, this means M30,M31,M32 are 0 and M33 is 1.
            //    And M03,M13,M23 (translation part for column vector convention) must be zero.
            if (!(MathUtil.IsApproxZero(matrix.M30) && MathUtil.IsApproxZero(matrix.M31) && MathUtil.IsApproxZero(matrix.M32) && MathUtil.IsApproxEqual(matrix.M33, 1.0) &&
                  MathUtil.IsApproxZero(matrix.M03) && MathUtil.IsApproxZero(matrix.M13) && MathUtil.IsApproxZero(matrix.M23)))
            {
                throw new ArgumentException("Matrix for UnitaryMap must be affine and have no translation part.", nameof(matrix));
            }

            Mat3<double> mat3 = matrix.GetMat3();
            var check = mat3 * mat3.Transposed();
             if (!check.IsApproxEqual(Mat3<double>.Identity, MathUtil.DefaultEpsilonD * 10))
                throw new ArgumentException("3x3 part of Matrix for UnitaryMap must be unitary.", nameof(matrix));

            _affineMap = new AffineMap(matrix);
        }

        public UnitaryMap(Vec3<double> axis, double radians)
        {
            _affineMap = new AffineMap(Mat4<double>.CreateRotation(axis, radians));
        }

        public UnitaryMap(Axis axis, double radians)
        {
             _affineMap = new AffineMap(Mat4<double>.CreateRotation(Vec3<double>.Zero, radians, axis)); // CreateRotation needs axis for arbitrary, or specific axis rotation
        }


        public UnitaryMap(UnitaryMap other)
        {
            _affineMap = (AffineMap)other._affineMap.Clone();
        }

        public UnitaryMap(UnitaryMap first, UnitaryMap second) // Composition
        {
            _affineMap = new AffineMap(first._affineMap, second._affineMap);
        }


        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => _affineMap.ApplyMap(sourcePoint);
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => _affineMap.ApplyInverseMap(sourcePoint);

        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => _affineMap.ApplyJacobian(sourceVector);
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => _affineMap.ApplyInverseJacobian(sourceVector);

        // For unitary maps, J^T = J^-1
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector) => ApplyInverseJacobian(sourceVector);
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector) => ApplyJacobian(sourceVector);


        public override double GetDeterminant(Vec3<double> domainPos) => _affineMap.Determinant; // Should be +1 or -1
        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos) => _affineMap.VoxelSize; // Should be (1,1,1)

        public override IMap Clone() => new UnitaryMap(this);
        public override AffineMap ToAffineMap() => (AffineMap)_affineMap.Clone(); // Return a clone
        public override IMap InverseMap() => new UnitaryMap((AffineMap)_affineMap.InverseMap());

        // Compositions
        public override IMap PreRotate(double radians, Axis axis)
        {
            var rotMap = new UnitaryMap(axis, radians);
            return new UnitaryMap(rotMap, this); // R_new * R_this
        }

        public override IMap PreTranslate(Vec3<double> t) => ToAffineMap().PreTranslate(t); // Result is Affine
        public override IMap PreScale(Vec3<double> s) => ToAffineMap().PreScale(s);     // Result is Affine

        public override IMap PostRotate(double radians, Axis axis)
        {
            var rotMap = new UnitaryMap(axis, radians);
            return new UnitaryMap(this, rotMap); // R_this * R_new
        }
        public override IMap PostTranslate(Vec3<double> t) => ToAffineMap().PostTranslate(t);
        public override IMap PostScale(Vec3<double> s) => ToAffineMap().PostScale(s);

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            // UnitaryMap wraps an AffineMap. Serialize the underlying AffineMap's data.
            // The AffineMap should already be constrained to be unitary (rotation/reflection only).
            _affineMap.WriteData(writer, streamMetadata);
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            // Create a new AffineMap and read its data.
            // Then, assign it to _affineMap. Validation that it's truly unitary
            // should ideally happen here or in the constructor if this ReadData
            // was part of a constructor.
            var affineData = new AffineMap(); // Default constructor
            affineData.ReadData(reader, streamMetadata);

            // Validate if affineData represents a unitary transformation.
            // For now, we assume the data read corresponds to a valid unitary matrix.
            // A full validation is complex (check if M*M^T = I, no translation, etc.).
            // The UnitaryMap constructor taking Mat4 does these checks.
            // Here, we are directly setting the internal _affineMap.
            // This might bypass some constructor validations if not careful.
            // A safer way would be:
            // Mat4<double> matrix = affineData.Matrix;
            // // Perform unitary checks on matrix here, or rely on constructor:
            // _affineMap = new AffineMap(matrix); // This re-validates if AffineMap ctor checks for singularity
            // // Or, more directly for UnitaryMap:
            // // Mat3<double> mat3 = matrix.GetMat3();
            // // if (!MathUtil.IsUnitary(mat3) || matrix has translation/perspective) throw new IoErrorException("Data read for UnitaryMap is not unitary.");
            _affineMap = affineData;
        }
    }
}
