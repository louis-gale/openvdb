// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class ScaleTranslateMap : MapBase
    {
        protected Vec3<double> _translation;
        protected Vec3<double> _scaleValues;
        protected Vec3<double> _voxelSize; // abs(_scaleValues)
        protected Vec3<double> _scaleValuesInverse;

        public override string TypeName => "ScaleTranslateMap";
        public override bool IsLinear => true;
        public override bool HasUniformScale
        {
            get
            {
                bool val = System.Math.Abs(_scaleValues.X - _scaleValues.Y) < MathUtil.DefaultEpsilonD;
                val = val && System.Math.Abs(_scaleValues.X - _scaleValues.Z) < MathUtil.DefaultEpsilonD;
                return val;
            }
        }

        public Vec3<double> Translation => _translation;
        public Vec3<double> Scale => _scaleValues;


        public ScaleTranslateMap() : this(new Vec3<double>(1.0, 1.0, 1.0), Vec3<double>.Zero) { }

        public ScaleTranslateMap(Vec3<double> scale, Vec3<double> translation)
        {
            if (System.Math.Abs(scale.X * scale.Y * scale.Z) < MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD * MathUtil.DefaultEpsilonD)
                throw new ArgumentException("Scale values cannot result in zero determinant.", nameof(scale));

            _scaleValues = scale;
            _translation = translation;
            _voxelSize = new Vec3<double>(System.Math.Abs(scale.X), System.Math.Abs(scale.Y), System.Math.Abs(scale.Z));
            _scaleValuesInverse = new Vec3<double>(1.0 / scale.X, 1.0 / scale.Y, 1.0 / scale.Z);
        }

        public ScaleTranslateMap(ScaleMap scaleMap, TranslationMap translationMap)
            : this(scaleMap.Scale, translationMap.Translation)
        {
        }

        public ScaleTranslateMap(ScaleTranslateMap other)
        {
            _scaleValues = other._scaleValues;
            _translation = other._translation;
            _voxelSize = other._voxelSize;
            _scaleValuesInverse = other._scaleValuesInverse;
        }

        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => (sourcePoint * _scaleValues) + _translation;
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => (sourcePoint - _translation) * _scaleValuesInverse;

        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => sourceVector * _scaleValues;
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => sourceVector * _scaleValuesInverse;
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector) => sourceVector * _scaleValues;
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector) => sourceVector * _scaleValuesInverse;

        public override double GetDeterminant(Vec3<double> domainPos) => _scaleValues.X * _scaleValues.Y * _scaleValues.Z;
        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos) => _voxelSize;

        public override IMap Clone() => new ScaleTranslateMap(this);

        public override AffineMap ToAffineMap()
        {
            var mat = Mat4<double>.CreateScale(_scaleValues);
            mat.M30 = _translation.X; // Row-vector translation for M * T
            mat.M31 = _translation.Y;
            mat.M32 = _translation.Z;
            // If column vector convention (T * M):
            // mat.M03 = _translation.X; mat.M13 = _translation.Y; mat.M23 = _translation.Z;
            // OpenVDB C++ uses V * M (row vector convention for points)
            // So, translation part is in the last row of Mat4.
            // A point (x,y,z,1) * Mat4 gives the transformed point.
            // (S*p) + T is: p * S_mat then add T.
            // If S_mat is Mat4.CreateScale, and T is added, this is actually:
            // (p_x*s_x + t_x, p_y*s_y + t_y, p_z*s_z + t_z)
            // This means the matrix should be:
            // sx 0  0  0
            // 0  sy 0  0
            // 0  0  sz 0
            // tx ty tz 1
            // This is Mat4.CreateScale followed by setting translation row.
            // Or, it's T_mat * S_mat if T_mat is set up correctly.
            // Let's use a matrix that represents p' = p*S + T_vec_component_wise
            // This implies composition: scale first, then translate.
            // So M_final = M_scale * M_translate
            var scaleMat = Mat4<double>.CreateScale(_scaleValues);
            var transMat = Mat4<double>.CreateTranslation(_translation);
            return new AffineMap(scaleMat * transMat);
        }

        public override IMap InverseMap() => new ScaleTranslateMap(_scaleValuesInverse, -_translation * _scaleValuesInverse);

        public override IMap PreTranslate(Vec3<double> t)
        {
            // S_this * T_this applied to point p: p*S_this + T_this
            // Pre-translate by t_new: (p + t_new)*S_this + T_this = p*S_this + t_new*S_this + T_this
            // New scale: S_this, New translation: t_new*S_this + T_this
            return new ScaleTranslateMap(_scaleValues, (_scaleValues * t) + _translation);
        }

        public override IMap PreScale(Vec3<double> s)
        {
            // (p*s_new)*S_this + T_this = p*(s_new*S_this) + T_this
            // New scale: s_new*S_this, New translation: T_this
            Vec3<double> newScale = _scaleValues * s;
            if (System.Math.Abs(newScale.X - newScale.Y) < MathUtil.DefaultEpsilonD && System.Math.Abs(newScale.X - newScale.Z) < MathUtil.DefaultEpsilonD)
                return new UniformScaleTranslateMap(newScale.X, _translation);
            return new ScaleTranslateMap(newScale, _translation);
        }

        public override IMap PostTranslate(Vec3<double> t)
        {
            // (p*S_this + T_this) + t_new
            // New scale: S_this, New translation: T_this + t_new
            return new ScaleTranslateMap(_scaleValues, _translation + t);
        }

        public override IMap PostScale(Vec3<double> s)
        {
            // (p*S_this + T_this) * s_new (component-wise for vector part, translation part also scaled)
            // ( (p.x*sx_old + tx_old)*sx_new, ... )
            // p.x*(sx_old*sx_new) + (tx_old*sx_new)
            // New scale: S_this * s_new, New translation: T_this * s_new (component-wise)
            Vec3<double> newScale = _scaleValues * s;
            Vec3<double> newTranslation = _translation * s; // Component-wise multiplication
            if (System.Math.Abs(newScale.X - newScale.Y) < MathUtil.DefaultEpsilonD && System.Math.Abs(newScale.X - newScale.Z) < MathUtil.DefaultEpsilonD)
                return new UniformScaleTranslateMap(newScale.X, newTranslation);
            return new ScaleTranslateMap(newScale, newTranslation);
        }

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            writer.Write(_translation.X);
            writer.Write(_translation.Y);
            writer.Write(_translation.Z);
            writer.Write(_scaleValues.X);
            writer.Write(_scaleValues.Y);
            writer.Write(_scaleValues.Z);
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            var tX = reader.ReadDouble();
            var tY = reader.ReadDouble();
            var tZ = reader.ReadDouble();
            _translation = new Vec3<double>(tX, tY, tZ);

            var sX = reader.ReadDouble();
            var sY = reader.ReadDouble();
            var sZ = reader.ReadDouble();
            _scaleValues = new Vec3<double>(sX, sY, sZ);

            // Re-initialize derived members
            _voxelSize = new Vec3<double>(System.Math.Abs(sX), System.Math.Abs(sY), System.Math.Abs(sZ));
            _scaleValuesInverse = new Vec3<double>(1.0 / sX, 1.0 / sY, 1.0 / sZ);
        }
    }
}
