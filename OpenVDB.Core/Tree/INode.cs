// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Math;
using System.IO;
using OpenVDB.Core.IO; // For StreamMetadata

namespace OpenVDB.Core.Tree
{
    /// <summary>
    /// Base interface for nodes in the VDB tree structure (InternalNode and LeafNode).
    /// </summary>
    /// <typeparam name="TValue">The type of values stored in the leaf nodes of the subtree rooted at this node.</typeparam>
    public interface INode<TValue> where TValue : struct
    {
        Coord Origin { get; }
        int Log2Dim { get; }
        int Dim { get; } // 1 << Log2Dim
        int NumValues { get; } // Dim * Dim * Dim
        int Level { get; } // LeafNode = 0, InternalNode<LeafNode> = 1, etc.

        // Value access - these typically operate in the global coordinate system of the grid
        // For a specific node, they might involve probing down to children or accessing local tiles/voxels.
        TValue GetValue(Coord globalXyz);
        void SetValue(Coord globalXyz, TValue value); // Typically implies SetValueOn
        bool IsValueOn(Coord globalXyz);
        void SetActiveState(Coord globalXyz, bool on);
        void SetValueOnly(Coord globalXyz, TValue value); // Set value without changing active state
        void SetValueOff(Coord globalXyz, TValue value); // Set value and mark inactive

        // Bulk operations - often in local coordinate space for the node itself
        void Fill(CoordBBox bbox, TValue value, bool activeState); // bbox can be local or global depending on context

        // Properties
        bool IsEmpty(); // True if all voxels/tiles are inactive (or background)
        bool IsDense(); // True if all voxels/tiles are active (not background) - for LeafNode, all bits on in valueMask

        // Memory management and statistics
        void Allocate(); // Ensure buffer/table is allocated if applicable
        bool IsAllocated { get; } // Is buffer/table allocated?
        long MemUsage(); // Approximate memory usage in bytes
        long OnVoxelCount(); // Count of active voxels at the leaf level under this node

        // I/O - These operate on the specific node's data
        void WriteTopology(BinaryWriter writer, StreamMetadata streamMeta);
        void ReadTopology(BinaryReader reader, StreamMetadata streamMeta);
        void WriteBuffers(BinaryWriter writer, StreamMetadata streamMeta);
        void ReadBuffers(BinaryReader reader, StreamMetadata streamMeta, TValue backgroundIfAllMaskedOff);

        // Other utility methods
        CoordBBox EvalActiveBoundingBox(bool visitVoxels = true); // In global coordinates
        bool IsConstant(out TValue representativeValue, out bool activeState, TValue tolerance = default);

        // For type system / polymorphism if needed, though less common with C# generics
        // string GetNodeType();
    }
}
