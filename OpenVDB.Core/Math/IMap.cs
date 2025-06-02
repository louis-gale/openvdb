// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Math;
using OpenVDB.Math.Maps;

namespace OpenVDB.Math
{
    public interface IMap
    {
        string TypeName { get; }
        bool IsLinear { get; }
        bool HasUniformScale { get; }

        Vec3<double> ApplyMap(Vec3<double> sourcePoint);
        Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint);

        // Simplified Jacobian methods (not taking domainPos for now)
        Vec3<double> ApplyJacobian(Vec3<double> sourceVector);
        Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector);

        // Simplified Jacobian Transpose methods
        Vec3<double> ApplyJT(Vec3<double> sourceVector, Vec3<double> domainPos); // Jacobian Transpose
        Vec3<double> ApplyIJT(Vec3<double> sourceVector, Vec3<double> domainPos); // Inverse Jacobian Transpose

        // Simplified Determinant and VoxelSize (not taking domainPos)
        double Determinant { get; }
        Vec3<double> VoxelSize { get; }

        // VoxelSize at a specific point (for non-linear maps)
        Vec3<double> GetVoxelSize(Vec3<double> domainPos);

        // Determinant at a specific point (for non-linear maps)
        double GetDeterminant(Vec3<double> domainPos);


        IMap Clone(); // For deep copy semantics if needed by Transform
        AffineMap ToAffineMap(); // All maps should be convertible to a general AffineMap
        IMap InverseMap(); // Return a new map that is the inverse of this map

        // Composition methods - these return new map instances
        IMap PreRotate(double radians, Axis axis);
        IMap PreTranslate(Vec3<double> t);
        IMap PreScale(Vec3<double> s);
        IMap PreShear(double shear, Axis axis0, Axis axis1);

        IMap PostRotate(double radians, Axis axis);
        IMap PostTranslate(Vec3<double> t);
        IMap PostScale(Vec3<double> s);
        IMap PostShear(double shear, Axis axis0, Axis axis1);

        // I/O - specific to each map type
        void WriteData(System.IO.BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        void ReadData(System.IO.BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata);
    }

    // Base class for convenience, providing default implementations or shared logic
    public abstract class MapBase : IMap
    {
        public abstract string TypeName { get; }
        public abstract bool IsLinear { get; }
        public abstract bool HasUniformScale { get; }

        public abstract Vec3<double> ApplyMap(Vec3<double> sourcePoint);
        public abstract Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint);

        // Default for linear maps: apply map excluding translation
        // For non-linear, these needs to be overridden to accept domainPos.
        public abstract Vec3<double> ApplyJacobian(Vec3<double> sourceVector, Vec3<double> domainPos);
        public abstract Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector, Vec3<double> domainPos);
        public abstract Vec3<double> ApplyJT(Vec3<double> sourceVector, Vec3<double> domainPos);
        public abstract Vec3<double> ApplyIJT(Vec3<double> sourceVector, Vec3<double> domainPos);

        // Simplified versions for convenience, assuming domainPos = Zero if not provided or map is linear
        public virtual Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => ApplyJacobian(sourceVector, Vec3<double>.Zero);
        public virtual Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => ApplyInverseJacobian(sourceVector, Vec3<double>.Zero);
        public virtual Vec3<double> ApplyJT(Vec3<double> sourceVector) => ApplyJT(sourceVector, Vec3<double>.Zero);
        public virtual Vec3<double> ApplyIJT(Vec3<double> sourceVector) => ApplyIJT(sourceVector, Vec3<double>.Zero);


        public virtual double Determinant => GetDeterminant(Vec3<double>.Zero); // Default to origin for linear maps
        public virtual Vec3<double> VoxelSize => GetVoxelSize(Vec3<double>.Zero); // Default to origin

        public abstract Vec3<double> GetVoxelSize(Vec3<double> domainPos);
        public abstract double GetDeterminant(Vec3<double> domainPos);

        public abstract IMap Clone();
        public abstract AffineMap ToAffineMap();
        public abstract IMap InverseMap();

        // Default composition logic: convert current map to Affine, compose, then return new AffineMap.
        // Specific map types can override for more optimized compositions (e.g., ScaleMap.PreTranslate -> ScaleTranslateMap).
        public virtual IMap PreRotate(double radians, Axis axis) => ToAffineMap().PreRotate(radians, axis);
        public virtual IMap PreTranslate(Vec3<double> t) => ToAffineMap().PreTranslate(t);
        public virtual IMap PreScale(Vec3<double> s) => ToAffineMap().PreScale(s);
        public virtual IMap PreShear(double shear, Axis axis0, Axis axis1) => ToAffineMap().PreShear(shear, axis0, axis1);

        public virtual IMap PostRotate(double radians, Axis axis) => ToAffineMap().PostRotate(radians, axis);
        public virtual IMap PostTranslate(Vec3<double> t) => ToAffineMap().PostTranslate(t);
        public virtual IMap PostScale(Vec3<double> s) => ToAffineMap().PostScale(s);
        public virtual IMap PostShear(double shear, Axis axis0, Axis axis1) => ToAffineMap().PostShear(shear, axis0, axis1);

        public abstract void WriteData(System.IO.BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        public abstract void ReadData(System.IO.BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata);
    }
}