// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using OpenVDB.Core.Util; // For NodeMask
using OpenVDB.Math;    // For Coord, CoordBBox

namespace OpenVDB.Core.Tree
{
    /// <summary>
    /// Represents a leaf node in the VDB tree structure, storing actual voxel values.
    /// </summary>
    /// <typeparam name="TValue">The type of values stored in the voxels.</typeparam>
    public class LeafNode<TValue> where TValue : struct // Using struct constraint for TValue
    {
        private readonly Coord _origin;
        private readonly int _log2Dim;
        private readonly int _dim; // Cached dimension (1 << Log2Dim)
        private readonly int _numValues; // Cached total number of values (Dim^3)

        private readonly LeafBuffer<TValue> _buffer;
        private readonly NodeMask _valueMask; // Active state mask

        public Coord Origin => _origin;
        public int Log2Dim => _log2Dim;
        public int Dim => _dim;
        public int NumValues => _numValues;
        public NodeMask ValueMask => _valueMask; // Expose for advanced operations if needed, or keep internal
        public LeafBuffer<TValue> Buffer => _buffer; // Expose for advanced operations/testing

        /// <summary>
        /// Initializes a new LeafNode.
        /// </summary>
        /// <param name="origin">The world-space coordinate of the minimum corner of this node.</param>
        /// <param name="log2Dim">Log base 2 of the dimension of this node (e.g., 3 for an 8x8x8 node).</param>
        /// <param name="initialValue">The value to which all voxels in this node are initialized.</param>
        /// <param name="activeState">The initial active state for all voxels in this node.</param>
        public LeafNode(Coord origin, int log2Dim, TValue initialValue, bool activeState)
        {
            if (log2Dim < 0 || log2Dim > NodeMask.MaxLog2Dim) // Assuming NodeMask has a MaxLog2Dim or similar constraint
                throw new ArgumentOutOfRangeException(nameof(log2Dim), $"Log2Dim must be between 0 and {NodeMask.MaxLog2Dim}.");

            _origin = origin;
            _log2Dim = log2Dim;
            _dim = 1 << _log2Dim;
            _numValues = _dim * _dim * _dim;
            if (_numValues < 0) _numValues = 0; // Should not happen with log2Dim constraint

            // LeafBuffer starts uniform. If activeState is true, individual SetValueOn calls
            // will make it non-uniform as needed. If activeState is false, all voxels are inactive
            // and the buffer can remain uniform with initialValue (which is then the background for this leaf).
            _buffer = new LeafBuffer<TValue>(log2Dim, initialValue, true);
            _valueMask = new NodeMask(log2Dim, activeState);
        }
        
        // Static helper methods for coordinate/offset conversion
        // These are crucial for mapping 3D local coordinates to 1D array indices.
        // Assuming X varies fastest, then Y, then Z. (C++ OpenVDB default)
        // Offset = x + y*dim + z*dim*dim

        /// <summary>
        /// Converts a 3D local coordinate (relative to the node's origin) to a 1D linear offset.
        /// </summary>
        /// <param name="localXyz">The local coordinate (components 0 to Dim-1).</param>
        /// <param name="log2Dim">Log base 2 of the node's dimension.</param>
        /// <returns>The linear offset.</returns>
        public static int LocalCoordToOffset(Coord localXyz, int log2Dim)
        {
            int dim = 1 << log2Dim;
            // Check bounds if necessary, though internal methods should ensure valid localXyz
            // if (localXyz.X < 0 || localXyz.X >= dim || ...) throw new ArgumentOutOfRangeException(...);
            return localXyz.X + localXyz.Y * dim + localXyz.Z * dim * dim;
        }

        /// <summary>
        /// Converts a 1D linear offset to a 3D local coordinate.
        /// </summary>
        /// <param name="offset">The linear offset (0 to Dim^3 - 1).</param>
        /// <param name="log2Dim">Log base 2 of the node's dimension.</param>
        /// <returns>The local coordinate.</returns>
        public static Coord OffsetToLocalCoord(int offset, int log2Dim)
        {
            int dim = 1 << log2Dim;
            // Check bounds if necessary
            // if (offset < 0 || offset >= (dim*dim*dim)) throw new ArgumentOutOfRangeException(...);
            int x = offset % dim;
            int temp = offset / dim;
            int y = temp % dim;
            int z = temp / dim;
            return new Coord(x, y, z);
        }

        // Value Access Methods
        public TValue GetValue(Coord localXyz)
        {
            int offset = LocalCoordToOffset(localXyz, _log2Dim);
            return _buffer.GetValue(offset);
        }

        /// <summary>
        /// Sets the value of a voxel at the given local coordinate and marks it as active.
        /// </summary>
        public void SetValue(Coord localXyz, TValue value)
        {
            int offset = LocalCoordToOffset(localXyz, _log2Dim);
            _buffer.SetValue(offset, value); // This makes buffer non-uniform if it was uniform
            _valueMask.SetOn(offset);
        }

        public bool IsValueOn(Coord localXyz)
        {
            int offset = LocalCoordToOffset(localXyz, _log2Dim);
            return _valueMask.IsOn(offset);
        }

        public void SetActiveState(Coord localXyz, bool on)
        {
            int offset = LocalCoordToOffset(localXyz, _log2Dim);
            _valueMask.Set(offset, on);
            if (on && _buffer.IsUniform) // If activating a voxel in a uniform buffer
            {
                // Ensure buffer becomes non-uniform so this active voxel with potentially
                // different-than-uniform-value (if it was previously set with SetValueOnly)
                // or same-as-uniform-value is explicitly stored.
                _buffer.Allocate(); 
            }
        }

        /// <summary>
        /// Sets the value of a voxel at the given local coordinate without changing its active state.
        /// </summary>
        public void SetValueOnly(Coord localXyz, TValue value)
        {
            int offset = LocalCoordToOffset(localXyz, _log2Dim);
            _buffer.SetValue(offset, value); // Makes buffer non-uniform if previously uniform
        }
        
        /// <summary>
        /// Sets the value of a voxel at the given local coordinate and marks it as inactive.
        /// </summary>
        public void SetValueOff(Coord localXyz, TValue value)
        {
            int offset = LocalCoordToOffset(localXyz, _log2Dim);
            SetValueOnly(localXyz, value); // Set the value first
            SetActiveState(localXyz, false); // Then mark inactive
        }

        // Bulk Operations
        public void FillComplete(TValue value, bool activeState)
        {
            _buffer.Fill(value);
            _valueMask.SetAll(activeState);
        }

        public void Fill(CoordBBox localBBox, TValue value, bool activeState)
        {
            // Ensure localBBox is within node bounds [0, Dim-1]
            Coord min = Coord.MaxComponent(localBBox.Min, new Coord(0,0,0));
            Coord max = Coord.MinComponent(localBBox.Max, new Coord(_dim-1,_dim-1,_dim-1));

            for (int z = min.Z; z <= max.Z; ++z)
            {
                for (int y = min.Y; y <= max.Y; ++y)
                {
                    for (int x = min.X; x <= max.X; ++x)
                    {
                        var localCoord = new Coord(x, y, z);
                        // These calls will make buffer non-uniform if needed.
                        SetValueOnly(localCoord, value); 
                        SetActiveState(localCoord, activeState);
                    }
                }
            }
        }

        // Properties
        public bool IsEmpty => _valueMask.IsAllOff();
        public bool IsDense => _valueMask.IsAllOn(); // All voxels are active
        public bool IsAllocated => _buffer.IsAllocated;
        public void Allocate() => _buffer.Allocate();

        // Statistics
        public int OnVoxelCount() => _valueMask.CountOn();
        public int OffVoxelCount() => _valueMask.CountOff();

        // I/O Methods
        public void WriteTopology(BinaryWriter writer)
        {
            _valueMask.Write(writer);
        }

        public void ReadTopology(BinaryReader reader)
        {
            // Assumes _valueMask is already initialized with correct Log2Dim in constructor
            _valueMask.Read(reader);
        }

        public void WriteBuffers(BinaryWriter writer, bool toHalf)
        {
            _buffer.Write(writer, _valueMask, toHalf);
        }

        public void ReadBuffers(BinaryReader reader, bool fromHalf, TValue backgroundIfAllMaskedOff)
        {
            // backgroundIfAllMaskedOff is the value to fill into inactive voxels if the mask
            // indicates they were not written (e.g. if mask is all off, buffer becomes uniform with this).
            _buffer.Read(reader, _valueMask, backgroundIfAllMaskedOff, fromHalf);
        }
        
        // Placeholder for MemUsage - requires size of TValue and NodeMask/LeafBuffer internal sizes
        public long MemUsage()
        {
            long size = 0;
            // size += System.Runtime.InteropServices.Marshal.SizeOf(typeof(LeafNode<TValue>)); // Approximate object overhead
            // size += _buffer.MemUsage(); // Needs LeafBuffer.MemUsage()
            // size += _valueMask.MemUsage(); // Needs NodeMask.MemUsage()
            return size; // Placeholder
        }
        
        // Placeholder for EvalActiveBoundingBox
        public CoordBBox EvalActiveBoundingBox(bool visitVoxels = true)
        {
            var bbox = CoordBBox.CreateEmpty(); // Min > Max initially
            if (!visitVoxels) // If not visiting voxels, and not all off, return full node bounds
            {
                if (!_valueMask.IsAllOff())
                {
                    bbox.Min = _origin;
                    bbox.Max = _origin + new Coord(_dim - 1, _dim - 1, _dim - 1);
                }
                return bbox;
            }

            for (int offset = 0; offset < _numValues; ++offset)
            {
                if (_valueMask.IsOn(offset))
                {
                    bbox.Expand(_origin + OffsetToLocalCoord(offset, _log2Dim));
                }
            }
            return bbox;
        }

        // Placeholder for IsConstant
        public bool IsConstant(out TValue representativeValue, out bool activeState, TValue tolerance = default)
        {
            if (!_valueMask.IsConstant(out activeState))
            {
                representativeValue = default;
                return false; // If mask is not constant, node cannot be constant
            }

            // Mask is constant. Now check buffer.
            if (_buffer.IsUniform)
            {
                representativeValue = _buffer.UniformValue;
                return true;
            }
            else
            {
                // Buffer is not uniform, but mask is constant.
                // If mask is all off, it's constant (value = buffer's uniform value before it became non-uniform or background)
                if (!activeState) 
                {
                    representativeValue = _buffer.GetValue(0); // Or some defined background for inactive constant
                    return true;
                }

                // Mask is all on, buffer is not uniform. Check if all values in buffer are within tolerance.
                representativeValue = _buffer.GetValue(0);
                for (int i = 1; i < _numValues; ++i)
                {
                    TValue currentValue = _buffer.GetValue(i);
                    if (tolerance is IFloatingPointIeee754<TValue> tolFloat &&
                        representativeValue is IFloatingPointIeee754<TValue> repFloat &&
                        currentValue is IFloatingPointIeee754<TValue> currFloat)
                    {
                        if (TValue.Abs(repFloat - currFloat) > tolFloat) return false;
                    }
                    else if (!EqualityComparer<TValue>.Default.Equals(currentValue, representativeValue))
                    {
                        return false; 
                    }
                }
                return true;
            }
        }
    }
}

// Add MaxLog2Dim to NodeMask if it's not there.
// public const int MaxLog2Dim = 10; // Example, adjust as needed.
// This was used in LeafNode constructor.

// Add Write/Read to Vec types if not fully there.
// For example, in Vec3<T>:
// public void Write(BinaryWriter writer) { writer.Write(X); writer.Write(Y); writer.Write(Z); }
// public static Vec3<T> Read(BinaryReader reader) { return new Vec3<T>(reader.Read<T>(), reader.Read<T>(), reader.Read<T>()); }
// This requires T to be blittable or BinaryReader/Writer to support generic T.
// The current LeafBuffer I/O helpers use type switching for basic types and assume Write/Read for Vec/Mat.
// The math types' Write/Read methods from turn 51 need to be reviewed/completed.
// For example, Vec3<float>.Write should write 3 floats.
// Vec3<float>.GetSizeInBytes should be sizeof(float)*3.
// These were partially addressed in turn 53.
