// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    public class IdentityMap : MapBase
    {
        public static string StaticTypeName => "IdentityMap";
        public override string TypeName => StaticTypeName;
        public override bool IsLinear => true;
        public override bool HasUniformScale => true;

        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => sourcePoint;
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => sourcePoint;

        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => sourceVector;
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => sourceVector;
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector) => sourceVector;
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector) => sourceVector;

        public override double GetDeterminant(Vec3<double> domainPos) => 1.0;
        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos) => new Vec3<double>(1.0, 1.0, 1.0);

        public override IMap Clone() => new IdentityMap();

        public override AffineMap ToAffineMap() => new AffineMap(Mat4<double>.Identity);
        public override IMap InverseMap() => new IdentityMap(); // Inverse of identity is identity

        // Composition methods can be optimized
        public override IMap PreRotate(double radians, Axis axis) => new UnitaryMap(axis, radians);
        public override IMap PreTranslate(Vec3<double> t) => new TranslationMap(t);
        public override IMap PreScale(Vec3<double> s) => new ScaleMap(s);
        public override IMap PreShear(double shear, Axis axis0, Axis axis1)
        {
            var affine = ToAffineMap(); // Convert to AffineMap then apply shear
            return affine.PreShear(shear, axis0, axis1);
        }

        public override IMap PostRotate(double radians, Axis axis) => PreRotate(radians, axis); // Same as Pre for Identity
        public override IMap PostTranslate(Vec3<double> t) => PreTranslate(t);
        public override IMap PostScale(Vec3<double> s) => PreScale(s);
        public override IMap PostShear(double shear, Axis axis0, Axis axis1) => PreShear(shear, axis0, axis1);

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            // No data to write for IdentityMap
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            // No data to read for IdentityMap
        }
    }
}
