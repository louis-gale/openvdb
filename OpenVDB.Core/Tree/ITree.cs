// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Core.Metadata; // For MetaMap if needed by TreeBase
using OpenVDB.Math; // For CoordBBox etc.

namespace OpenVDB.Core.Tree
{
    // Placeholder for voxel accessor
    public interface ITreeValueAccessor<TValue>
    {
        TValue GetValue(Coord xyz);
        void SetValue(Coord xyz, TValue value);
        bool IsValueOn(Coord xyz);
        void SetValueOn(Coord xyz, TValue value);
        void SetValueOff(Coord xyz);
        // ... other accessor methods
    }

    // Base interface for all tree types
    public interface ITree
    {
        string TreeType { get; } // Name of the specific tree configuration (e.g., "TreeFloatR5")
        string ValueTypeName { get; } // Name of the value type (e.g., "float")

        Type ValueType { get; } // Actual System.Type of the value

        object BackgroundValue { get; } // Non-generic background access

        bool IsEmpty { get; }
        void Clear(); // Resets to background

        long ActiveVoxelCount();
        CoordBBox EvalActiveVoxelBoundingBox();
        bool EvalActiveVoxelDim(out Coord dim); // C++ returns bool, dim is out param
        long MemUsage();

        void Prune(object toleranceValue); // Non-generic tolerance
        void Clip(CoordBBox bbox);

        ITree Copy(); // Deep copy of the tree structure

        // I/O methods
        void ReadTopology(System.IO.BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        void WriteTopology(System.IO.BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        void ReadBuffers(System.IO.BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        // void ReadBuffers(System.IO.BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata, CoordBBox bbox); // Overload for partial read
        void WriteBuffers(System.IO.BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata);
        void ReadNonresidentBuffers(); // If delayed loading is supported

        void Print(System.IO.TextWriter writer, int verboseLevel);

        // For Grid<TTree> ValueType and Accessor type members
        // These might need to be actual generic type parameters on ITree if possible,
        // or handled via reflection/casting carefully.
        // For now, Grid<TTree> will assume TTree itself can provide these.
    }

    // Generic tree interface
    public interface ITree<TValue> : ITree
    {
        new TValue BackgroundValue { get; }
        ITreeValueAccessor<TValue> GetAccessor();
        // ... other generic methods like Fill, DenseFill, Merge, Topology ops
        void Fill(CoordBBox bbox, TValue value, bool activeState);
        void DenseFill(CoordBBox bbox, TValue value, bool activeState);
        void Merge(ITree<TValue> otherTree, MergePolicy policy);

        void TopologyUnion<TOtherValue>(ITree<TOtherValue> otherTree);
        void TopologyIntersection<TOtherValue>(ITree<TOtherValue> otherTree);
        void TopologyDifference<TOtherValue>(ITree<TOtherValue> otherTree);
    }
}
