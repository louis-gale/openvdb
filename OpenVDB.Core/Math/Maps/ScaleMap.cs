// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class ScaleMap : MapBase
    {
        protected Vec3<double> _scaleValues; // Renaming from C++ mScaleValues for clarity
        protected Vec3<double> _voxelSize;   // Absolute scale values
        protected Vec3<double> _scaleValuesInverse;
        // Acceleration structures from C++ (mInvScaleSqr, mInvTwiceScale) are not directly ported
        // unless specific performance bottlenecks show their necessity. Can be computed on-the-fly.

        public override string TypeName => "ScaleMap";
        public override bool IsLinear => true;
        public override bool HasUniformScale
        {
            get
            {
                bool val = System.Math.Abs(_scaleValues.X - _scaleValues.Y) < MathUtil.DefaultEpsilonD; // Using a small epsilon
                val = val && System.Math.Abs(_scaleValues.X - _scaleValues.Z) < MathUtil.DefaultEpsilonD;
                return val;
            }
        }

        public Vec3<double> Scale => _scaleValues;

        public ScaleMap() : this(new Vec3<double>(1.0, 1.0, 1.0)) { }

        public ScaleMap(Vec3<double> scale)
        {
            if (System.Math.Abs(scale.X * scale.Y * scale.Z) < MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD) // Check against a small volume
                throw new ArgumentException("Scale values cannot result in zero determinant (non-invertible).", nameof(scale));

            _scaleValues = scale;
            _voxelSize = new Vec3<double>(System.Math.Abs(scale.X), System.Math.Abs(scale.Y), System.Math.Abs(scale.Z));
            _scaleValuesInverse = new Vec3<double>(1.0 / scale.X, 1.0 / scale.Y, 1.0 / scale.Z);
        }

        public ScaleMap(ScaleMap other)
        {
            _scaleValues = other._scaleValues;
            _voxelSize = other._voxelSize;
            _scaleValuesInverse = other._scaleValuesInverse;
        }


        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => sourcePoint * _scaleValues; // Component-wise
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => sourcePoint * _scaleValuesInverse;

        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => sourceVector * _scaleValues;
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => sourceVector * _scaleValuesInverse;
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector) => sourceVector * _scaleValues; // For diagonal matrix, JT is same as Jacobian
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector) => sourceVector * _scaleValuesInverse;

        public override double GetDeterminant(Vec3<double> domainPos) => _scaleValues.X * _scaleValues.Y * _scaleValues.Z;
        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos) => _voxelSize;

        public override IMap Clone() => new ScaleMap(this);

        public override AffineMap ToAffineMap() => new AffineMap(Mat4<double>.CreateScale(_scaleValues));

        public override IMap InverseMap() => new ScaleMap(_scaleValuesInverse);

        // Optimized compositions
        public override IMap PreTranslate(Vec3<double> t)
        {
            // S * T(v) = T(S*v) * S. If this map is S, and we pre-translate by T(t),
            // the operation is T(t) * S.
            // The point transformation is S * (p + t) = S*p + S*t.
            // This is a ScaleTranslateMap with scale S and translation S*t.
            return new ScaleTranslateMap(_scaleValues, _scaleValues * t);
        }

        public override IMap PreScale(Vec3<double> s)
        {
            Vec3<double> newScale = _scaleValues * s;
            if (System.Math.Abs(newScale.X - newScale.Y) < MathUtil.DefaultEpsilonD && System.Math.Abs(newScale.X - newScale.Z) < MathUtil.DefaultEpsilonD)
                return new UniformScaleMap(newScale.X);
            return new ScaleMap(newScale);
        }

        public override IMap PostTranslate(Vec3<double> t)
        {
            // T(v) * S. If this map is S, and we post-translate by T(t),
            // the operation is S * T(t).
            // The point transformation is (S*p) + t.
            // This is a ScaleTranslateMap with scale S and translation t.
            return new ScaleTranslateMap(_scaleValues, t);
        }

        public override IMap PostScale(Vec3<double> s) => PreScale(s); // For diagonal matrices, pre/post scale is commutative

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            writer.Write(_scaleValues.X);
            writer.Write(_scaleValues.Y);
            writer.Write(_scaleValues.Z);
            // _voxelSize, _scaleValuesInverse are derived, no need to write.
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            _scaleValues = new Vec3<double>(x, y, z);
            _voxelSize = new Vec3<double>(System.Math.Abs(x), System.Math.Abs(y), System.Math.Abs(z));
            _scaleValuesInverse = new Vec3<double>(1.0 / x, 1.0 / y, 1.0 / z);
        }
    }
}
