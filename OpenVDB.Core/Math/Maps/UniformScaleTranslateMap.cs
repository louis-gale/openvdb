// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class UniformScaleTranslateMap : ScaleTranslateMap // Inherits from ScaleTranslateMap
    {
        public override string TypeName => "UniformScaleTranslateMap";
        // IsLinear is inherited and true
        public override bool HasUniformScale => true;

        public double UniformScaleValue => _scaleValues.X; // All components of _scaleValues are the same

        public UniformScaleTranslateMap() : this(1.0, Vec3<double>.Zero) { }

        public UniformScaleTranslateMap(double scale, Vec3<double> translation)
            : base(new Vec3<double>(scale, scale, scale), translation) // Call base constructor
        {
        }

        public UniformScaleTranslateMap(UniformScaleMap scaleMap, TranslationMap translationMap)
            : base(scaleMap.Scale, translationMap.Translation)
        {
        }
        
        public UniformScaleTranslateMap(UniformScaleTranslateMap other) : base(other)
        {
        }

        public override IMap Clone() => new UniformScaleTranslateMap(this);

        // ToAffineMap is inherited from ScaleTranslateMap

        public override IMap InverseMap()
        {
            // Inverse of p' = p*S + T  is p = (p' - T) * (1/S)
            // New scale is 1/S, new translation is (-T)*(1/S)
            double invScale = 1.0 / UniformScaleValue;
            Vec3<double> newTranslation = -_translation * invScale;
            return new UniformScaleTranslateMap(invScale, newTranslation);
        }

        // Further optimized compositions
        public override IMap PreTranslate(Vec3<double> t)
        {
            // (p + t_new)*S_this + T_this = p*S_this + t_new*S_this + T_this
            // New scale: S_this (uniform), New translation: t_new*S_this + T_this
            return new UniformScaleTranslateMap(UniformScaleValue, (_scaleValues * t) + _translation);
        }

        public override IMap PreScale(Vec3<double> s)
        {
            // (p*s_new)*S_this + T_this = p*(s_new*S_this) + T_this
            Vec3<double> newScale = _scaleValues * s;
            if (System.Math.Abs(newScale.X - newScale.Y) < MathUtil.DefaultEpsilonD && System.Math.Abs(newScale.X - newScale.Z) < MathUtil.DefaultEpsilonD)
                return new UniformScaleTranslateMap(newScale.X, _translation);
            return new ScaleTranslateMap(newScale, _translation); // Promotes to ScaleTranslateMap
        }

        public override IMap PostTranslate(Vec3<double> t)
        {
            // (p*S_this + T_this) + t_new
            return new UniformScaleTranslateMap(UniformScaleValue, _translation + t);
        }

        public override IMap PostScale(Vec3<double> s)
        {
            // (p*S_this + T_this) * s_new (component-wise for vector part, translation part also scaled)
            Vec3<double> newScale = _scaleValues * s;
            Vec3<double> newTranslation = _translation * s; // Component-wise
            if (System.Math.Abs(newScale.X - newScale.Y) < MathUtil.DefaultEpsilonD && System.Math.Abs(newScale.X - newScale.Z) < MathUtil.DefaultEpsilonD)
                return new UniformScaleTranslateMap(newScale.X, newTranslation);
            return new ScaleTranslateMap(newScale, newTranslation); // Promotes
        }

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            // UniformScaleTranslateMap inherits from ScaleTranslateMap,
            // which stores _translation and _scaleValues (Vec3d).
            // For UniformScale, all components of _scaleValues are the same.
            writer.Write(_translation.X);
            writer.Write(_translation.Y);
            writer.Write(_translation.Z);
            writer.Write(_scaleValues.X); // All scale components are the same, write one or all three.
                                         // C++ ScaleTranslateMap writes all three components of mScaleValues.
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
            var sY = reader.ReadDouble(); // Read all components
            var sZ = reader.ReadDouble();
            
            // For UniformScale, sX, sY, sZ should be the same.
            // We can validate this or just use sX and enforce uniformity.
            if (!(System.Math.Abs(sX - sY) < MathUtil.DefaultEpsilonD && System.Math.Abs(sX - sZ) < MathUtil.DefaultEpsilonD))
            {
                 Console.Error.WriteLine($"Warning: Reading non-uniform scale ({sX},{sY},{sZ}) into UniformScaleTranslateMap. Using X component ({sX}).");
            }
            _scaleValues = new Vec3<double>(sX, sX, sX); // Enforce uniform scale

            // Re-initialize derived members from base ScaleTranslateMap (if any were specific to it)
            // Or, ensure base class's protected fields are correctly set for derived calculations.
            // For ScaleTranslateMap, these are _voxelSize and _scaleValuesInverse
            _voxelSize = new Vec3<double>(System.Math.Abs(sX), System.Math.Abs(sX), System.Math.Abs(sX));
            _scaleValuesInverse = new Vec3<double>(1.0 / sX, 1.0 / sX, 1.0 / sX);
        }
    }
}
