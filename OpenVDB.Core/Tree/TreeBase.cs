// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Math; // For CoordBBox etc.
using System;
using System.IO;

namespace OpenVDB.Core.Tree
{
    /// <summary>
    /// Abstract base class for all tree types.
    /// Corresponds to openvdb::tree::TreeBase.
    /// </summary>
    public abstract class TreeBase : ITree
    {
        public abstract string TreeType { get; }
        public abstract string ValueTypeName { get; }
        public abstract Type ValueType { get; }

        public abstract object BackgroundValue { get; }

        public abstract bool IsEmpty { get; }
        public abstract void Clear();

        public abstract long ActiveVoxelCount();
        public abstract CoordBBox EvalActiveVoxelBoundingBox();
        public abstract bool EvalActiveVoxelDim(out Coord dim);
        public abstract long MemUsage();

        public abstract void Prune(object toleranceValue);
        public abstract void Clip(CoordBBox bbox);

        public abstract ITree Copy();

        // I/O methods
        public abstract void ReadTopology(BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        public abstract void WriteTopology(BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        public abstract void ReadBuffers(BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        // public abstract void ReadBuffers(BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata, CoordBBox bbox);
        public abstract void WriteBuffers(BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        public abstract void ReadNonresidentBuffers();

        public virtual void Print(TextWriter writer, int verboseLevel)
        {
            writer.WriteLine($"TreeType: {TreeType}, ValueType: {ValueTypeName}, Background: {BackgroundValue}");
            writer.WriteLine($"IsEmpty: {IsEmpty}, ActiveVoxels: {ActiveVoxelCount()}, MemUsage: {MemUsage()} bytes");
            CoordBBox bbox = EvalActiveVoxelBoundingBox();
            writer.WriteLine($"Active BBox: {bbox.Min} -> {bbox.Max}");
        }
    }
}
