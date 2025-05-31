// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class TranslationMap : MapBase
    {
        private Vec3<double> _translation;

        public Vec3<double> Translation => _translation;

        public override string TypeName => "TranslationMap";
        public override bool IsLinear => true;
        public override bool HasUniformScale => true; // Translation doesn't affect scale

        public TranslationMap() : this(Vec3<double>.Zero) { }

        public TranslationMap(Vec3<double> translation)
        {
            _translation = translation;
        }

        public TranslationMap(TranslationMap other)
        {
            _translation = other._translation;
        }

        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => sourcePoint + _translation;
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => sourcePoint - _translation;

        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => sourceVector; // Jacobian of translation is identity
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => sourceVector;
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector) => sourceVector;
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector) => sourceVector;

        public override double GetDeterminant(Vec3<double> domainPos) => 1.0;
        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos) => new Vec3<double>(1.0, 1.0, 1.0);

        public override IMap Clone() => new TranslationMap(this);

        public override AffineMap ToAffineMap()
        {
            var mat = Mat4<double>.CreateTranslation(_translation);
            return new AffineMap(mat);
        }

        public override IMap InverseMap() => new TranslationMap(-_translation);

        // Composition
        public override IMap PreRotate(double radians, Axis axis)
        {
            // R * T(v) = T(R*v) * R
            // To make TranslationMap the "outer" operation, we'd need a more complex map type.
            // Fallback to AffineMap.
            return ToAffineMap().PreRotate(radians, axis);
        }

        public override IMap PreTranslate(Vec3<double> t) => new TranslationMap(_translation + t);

        public override IMap PreScale(Vec3<double> s)
        {
            // S * T(v) = T(S*v) * S
            // This creates T(S*v) and then the S part needs to be outside.
            // For PreScale, the scale applies first, then the original translation.
            // (x*s_x + t_x, y*s_y + t_y, z*s_z + t_z) which is ScaleTranslate
            return new ScaleTranslateMap(s, _translation);
        }

        public override IMap PostRotate(double radians, Axis axis)
        {
            // T(v) * R = R * T(R_inv * v)
            // More complex, fallback to AffineMap
            return ToAffineMap().PostRotate(radians, axis);
        }

        public override IMap PostTranslate(Vec3<double> t) => new TranslationMap(_translation + t);

        public override IMap PostScale(Vec3<double> s)
        {
            // T(v) * S = S * T(v*invS) -> ( (x+v_x)*s_x, ... )
            // (x+t_x)*s_x, (y+t_y)*s_y, (z+t_z)*s_z
            // sx*x + t_x*s_x ...
            // This is a ScaleTranslateMap where scale is s, and translation is t_x*s_x ...
            return new ScaleTranslateMap(s, new Vec3<double>(
                _translation.X * s.X,
                _translation.Y * s.Y,
                _translation.Z * s.Z));
        }

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            writer.Write(_translation.X);
            writer.Write(_translation.Y);
            writer.Write(_translation.Z);
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            _translation = new Vec3<double>(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
        }
    }
}
