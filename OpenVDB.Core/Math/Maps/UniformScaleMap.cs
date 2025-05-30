// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class UniformScaleMap : ScaleMap // Inherits from ScaleMap
    {
        public override string TypeName => "UniformScaleMap";
        // IsLinear is inherited and is true
        // HasUniformScale is overridden to be always true
        public override bool HasUniformScale => true;

        public double UniformScaleValue => _scaleValues.X; // All components of _scaleValues are the same

        public UniformScaleMap() : this(1.0) { }

        public UniformScaleMap(double scale) 
            : base(new Vec3<double>(scale, scale, scale)) // Call base constructor
        {
        }
        
        public UniformScaleMap(UniformScaleMap other) : base(other) // Call base copy constructor
        {
        }

        public override IMap Clone() => new UniformScaleMap(this);
        
        // ToAffineMap is inherited from ScaleMap

        public override IMap InverseMap() => new UniformScaleMap(1.0 / UniformScaleValue);

        // Composition methods that can maintain UniformScaleMap or promote appropriately
        public override IMap PreTranslate(Vec3<double> t)
        {
            // S_uniform * T(v) = T(S_uniform*v) * S_uniform
            // (x*s + t_x, y*s + t_y, z*s + t_z) is not a UniformScaleTranslate if t is not scaled uniformly by S_uniform.
            // The result of T(t) * S_u is S_u * T(t * (1/S_u))
            // Point transform: (p.x + t.x)*S_u = p.x*S_u + t.x*S_u
            // This is a UniformScaleTranslateMap
            return new UniformScaleTranslateMap(UniformScaleValue, _scaleValues * t); // scale * translation
        }

        public override IMap PreScale(Vec3<double> s)
        {
            // S_u * S_v = S_{u*v}
            Vec3<double> newScale = _scaleValues * s; // _scaleValues.X is the uniform scale
            if (System.Math.Abs(newScale.X - newScale.Y) < MathUtil.DefaultEpsilonD && System.Math.Abs(newScale.X - newScale.Z) < MathUtil.DefaultEpsilonD)
                return new UniformScaleMap(newScale.X);
            return new ScaleMap(newScale);
        }

        public override IMap PostTranslate(Vec3<double> t)
        {
            // T(v) * S_u
            // (p.x*S_u) + t_x
            return new UniformScaleTranslateMap(UniformScaleValue, t);
        }

        public override IMap PostScale(Vec3<double> s) => PreScale(s); // Commutative with uniform scale

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            // UniformScaleMap stores scale as Vec3 in base ScaleMap. Write that out.
            writer.Write(_scaleValues.X); // All components are the same, just write one, or write all three
            writer.Write(_scaleValues.Y); // For consistency with ScaleMap.WriteData
            writer.Write(_scaleValues.Z);
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            // Read as Vec3, then ensure it's uniform.
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            if (!(System.Math.Abs(x - y) < MathUtil.DefaultEpsilonD && System.Math.Abs(x - z) < MathUtil.DefaultEpsilonD))
            {
                // This indicates an issue if we are strictly UniformScaleMap,
                // but ScaleMap.ReadData would handle this by setting _scaleValues.
                // For UniformScaleMap, we expect them to be same.
                Console.Error.WriteLine($"Warning: Reading non-uniform scale ({x},{y},{z}) into UniformScaleMap. Using X component ({x}).");
            }
            _scaleValues = new Vec3<double>(x, x, x); // Enforce uniformity from the first component read.
                                                     // Or, if read from base ScaleMap, it would set _scaleValues correctly.
                                                     // This direct ReadData assumes it's only for UniformScaleMap specific data,
                                                     // but since it inherits ScaleMap's fields, it should align with ScaleMap.WriteData.
                                                     // The current ScaleMap.WriteData writes all 3 components.
            _voxelSize = new Vec3<double>(System.Math.Abs(x), System.Math.Abs(x), System.Math.Abs(x));
            _scaleValuesInverse = new Vec3<double>(1.0 / x, 1.0 / x, 1.0 / x);
        }
    }
}
