// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using OpenVDB.Core.IO; // For StreamMetadata, if IMap had Read/WriteData

namespace OpenVDB.Core.Math.Maps
{
    /// <summary>
    /// A wrapper around an IMap interface, providing a concrete class
    /// that can be used to pass around maps generically.
    /// This is similar in spirit to C++ openvdb::math::GenericMap,
    /// though C# generics and interfaces often reduce the need for such wrappers.
    /// It can be useful if a non-generic context needs to hold and operate on any map type.
    /// </summary>
    public class GenericMap : IMap // Implement IMap to be a map itself
    {
        private readonly IMap _map;

        public GenericMap(IMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public GenericMap(Transform transform)
        {
            _map = transform?.GetMapCopy() ?? throw new ArgumentNullException(nameof(transform));
        }

        // IMap implementation delegated to the wrapped _map
        public string TypeName => _map.TypeName;
        public bool IsLinear => _map.IsLinear;
        public bool HasUniformScale => _map.HasUniformScale;

        public Vec3<double> ApplyMap(Vec3<double> sourcePoint) => _map.ApplyMap(sourcePoint);
        public Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => _map.ApplyInverseMap(sourcePoint);
        
        public Vec3<double> ApplyJacobian(Vec3<double> sourceVector) => _map.ApplyJacobian(sourceVector);
        public Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector) => _map.ApplyInverseJacobian(sourceVector);
        public Vec3<double> ApplyJT(Vec3<double> sourceVector) => _map.ApplyJT(sourceVector);
        public Vec3<double> ApplyIJT(Vec3<double> sourceVector) => _map.ApplyIJT(sourceVector);

        public double Determinant => _map.Determinant;
        public Vec3<double> VoxelSize => _map.VoxelSize;

        public Vec3<double> GetVoxelSize(Vec3<double> domainPos) => _map.GetVoxelSize(domainPos);
        public double GetDeterminant(Vec3<double> domainPos) => _map.GetDeterminant(domainPos);

        public IMap Clone() => new GenericMap(_map.Clone()); // Clone the wrapped map
        public AffineMap ToAffineMap() => _map.ToAffineMap();
        public IMap InverseMap() => new GenericMap(_map.InverseMap()); // Wrap the inverse

        // Composition methods delegate and re-wrap
        public IMap PreRotate(double radians, Axis axis) => new GenericMap(_map.PreRotate(radians, axis));
        public IMap PreTranslate(Vec3<double> t) => new GenericMap(_map.PreTranslate(t));
        public IMap PreScale(Vec3<double> s) => new GenericMap(_map.PreScale(s));
        public IMap PreShear(double shear, Axis axis0, Axis axis1) => new GenericMap(_map.PreShear(shear, axis0, axis1));

        public IMap PostRotate(double radians, Axis axis) => new GenericMap(_map.PostRotate(radians, axis));
        public IMap PostTranslate(Vec3<double> t) => new GenericMap(_map.PostTranslate(t));
        public IMap PostScale(Vec3<double> s) => new GenericMap(_map.PostScale(s));
        public IMap PostShear(double shear, Axis axis0, Axis axis1) => new GenericMap(_map.PostShear(shear, axis0, axis1));
        
        // I/O methods
        public void WriteData(BinaryWriter writer, StreamMetadata streamMetadata)
        {
            // To serialize a GenericMap, we serialize the TypeName of the *wrapped* map,
            // then the wrapped map's data.
            IoUtils.WriteString(writer, _map.TypeName);
            _map.WriteData(writer, streamMetadata);
        }

        public void ReadData(BinaryReader reader, StreamMetadata streamMetadata)
        {
            // This ReadData is problematic because GenericMap itself doesn't know what concrete type
            // _map should be. The type name is read here, but then what?
            // This suggests GenericMap itself shouldn't be directly deserialized this way
            // unless it's always wrapping a known type or the factory is used here.
            // The Transform.Read method is a better place for polymorphic map deserialization.
            // If GenericMap is written, on read, one would read the TypeName, then instantiate
            // the concrete map, then call its ReadData. GenericMap itself is just a wrapper.
            throw new NotSupportedException("GenericMap.ReadData should not be called directly. Deserialize the concrete wrapped map type.");
        }
    }
}
