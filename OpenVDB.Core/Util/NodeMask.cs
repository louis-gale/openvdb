// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections; // For BitArray
using System.IO;

namespace OpenVDB.Core.Util
{
    /// <summary>
    /// A bitmask used by tree nodes (typically LeafNode and InternalNode)
    /// to store the active state of their child voxels or tiles.
    /// </summary>
    public class NodeMask
    {
        private readonly BitArray _bits;
        private readonly int _log2Dim;
        private readonly int _size; // Total number of bits = Dim^3 = (1 << Log2Dim)^3

        public int Log2Dim => _log2Dim;
        public int Count => _size; // Total number of bits this mask can hold

        /// <summary>
        /// Initializes a new NodeMask.
        /// </summary>
        /// <param name="log2Dim">Log base 2 of the node dimension.
        /// For example, log2Dim = 3 corresponds to a 2^3 = 8 dimension node (8x8x8 voxels).</param>
        /// <param name="initialValueForAll">Initial state for all bits in the mask.</param>
        public NodeMask(int log2Dim, bool initialValueForAll = false)
        {
            if (log2Dim < 0 || log2Dim > 10) // Max Log2Dim in C++ is usually around 5-7 for LeafNodes.
                                             // Max total bits for BitArray is int.MaxValue. 3*10 = 30. 2^30 is large but feasible.
                throw new ArgumentOutOfRangeException(nameof(log2Dim), "Log2Dim must be non-negative and reasonable.");

            _log2Dim = log2Dim;
            int dim = 1 << log2Dim; // Side dimension of the node
            _size = dim * dim * dim; // Total number of values/bits
            _bits = new BitArray(_size, initialValueForAll);
        }

        private NodeMask(int log2Dim, BitArray bits) // Internal constructor for cloning/reading
        {
            _log2Dim = log2Dim;
            _size = bits.Length;
            _bits = (BitArray)bits.Clone();
        }

        public NodeMask Copy() => new NodeMask(_log2Dim, _bits);


        /// <summary>
        /// Checks if the bit at the specified linear offset is on (true).
        /// </summary>
        public bool IsOn(int index)
        {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
            return _bits[index];
        }

        /// <summary>
        /// Checks if the bit at the specified linear offset is off (false).
        /// </summary>
        public bool IsOff(int index) => !IsOn(index);

        /// <summary>
        /// Sets the bit at the specified linear offset.
        /// </summary>
        public void Set(int index, bool on)
        {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
            _bits[index] = on;
        }

        /// <summary>
        /// Sets the bit at the specified linear offset to on (true).
        /// </summary>
        public void SetOn(int index) => Set(index, true);

        /// <summary>
        /// Sets the bit at the specified linear offset to off (false).
        /// </summary>
        public void SetOff(int index) => Set(index, false);

        /// <summary>
        /// Sets all bits in the mask to the specified state.
        /// </summary>
        public void SetAll(bool on) => _bits.SetAll(on);

        /// <summary>
        /// Checks if all bits in the mask are on (true).
        /// </summary>
        public bool IsAllOn()
        {
            for (int i = 0; i < _size; ++i)
            {
                if (!_bits[i]) return false;
            }
            return _size > 0; // Empty mask is not "all on"
        }

        /// <summary>
        /// Checks if all bits in the mask are off (false).
        /// </summary>
        public bool IsAllOff()
        {
            for (int i = 0; i < _size; ++i)
            {
                if (_bits[i]) return false;
            }
            return true; // Empty mask is "all off"
        }

        /// <summary>
        /// Checks if all bits in the mask have the same state.
        /// </summary>
        /// <param name="state">The common state if the mask is constant.</param>
        /// <returns>True if all bits are the same, false otherwise.</returns>
        public bool IsConstant(out bool state)
        {
            if (_size == 0)
            {
                state = false; // Or true with background state? C++ returns true, background state.
                return true;   // Let's say empty is constant (and off by default)
            }
            state = _bits[0];
            for (int i = 1; i < _size; ++i)
            {
                if (_bits[i] != state) return false;
            }
            return true;
        }

        /// <summary>
        /// Counts the number of bits that are on (true).
        /// </summary>
        public int CountOn()
        {
            int count = 0;
            for (int i = 0; i < _size; ++i)
            {
                if (_bits[i]) count++;
            }
            return count;
        }

        /// <summary>
        /// Counts the number of bits that are off (false).
        /// </summary>
        public int CountOff() => _size - CountOn();

        /// <summary>
        /// Writes the mask to a binary stream.
        /// Format: number of ulongs, then the ulong data.
        /// </summary>
        public void Write(BinaryWriter writer)
        {
            // BitArray.CopyTo can copy to byte[], int[], bool[].
            // To match C++ ulong[], we need to pack bits manually or find a more direct way.
            // For simplicity, let's serialize as a sequence of bools or bytes.
            // C++ NodeMask uses an array of WordType (ulong).
            // BitArray stores bits packed.

            // Simplified: write as a block of bytes.
            // Number of bytes needed for BitArray.
            // Each byte in the array represents 8 bits.
            int numBytes = (_size + 7) / 8;
            byte[] bytes = new byte[numBytes];
            _bits.CopyTo(bytes, 0);

            writer.Write(numBytes); // Write the number of bytes
            writer.Write(bytes);    // Write the byte array
        }

        /// <summary>
        /// Reads the mask from a binary stream.
        /// Assumes Log2Dim and Size are already set correctly (e.g. by LeafNode constructor before calling this).
        /// </summary>
        public void Read(BinaryReader reader)
        {
             int numBytesFromFile = reader.ReadInt32();
             if (numBytesFromFile < 0) throw new IOException("Invalid byte count for NodeMask.");

             // Expected number of bytes based on current mask size
             int expectedNumBytes = (_size + 7) / 8;
             if (numBytesFromFile != expectedNumBytes && _size > 0) // Allow 0 size if mask is empty
             {
                 // This could happen if Log2Dim of the persisted mask was different.
                 // A robust solution would store Log2Dim with the mask.
                 // For now, we assume the current Log2Dim is correct for the data being read.
                 Console.Error.WriteLine($"Warning: NodeMask.Read expected {expectedNumBytes} bytes based on Log2Dim={_log2Dim}, but file indicates {numBytesFromFile} bytes. Data might be misaligned if Log2Dim changed.");
                 // Adjust numBytesToRead to the smaller of the two to avoid over/under reading if sizes mismatch.
                 // This is just a recovery attempt; ideally, persisted Log2Dim should be used to reconstruct.
                 // For this placeholder, if _size is already set (e.g. from LeafNode constructor), we use it.
             }
             if (_size == 0 && numBytesFromFile > 0) throw new IOException("NodeMask size is 0 but file contains mask data.");
             if (_size > 0)
             {
                byte[] bytes = reader.ReadBytes(expectedNumBytes); // Read based on current mask's expected size
                if (bytes.Length < expectedNumBytes && numBytesFromFile > bytes.Length)
                {
                    // If ReadBytes returned less than expected but file claimed more, it's an EOF issue.
                    throw new EndOfStreamException("Failed to read complete NodeMask data.");
                }

                // Ensure BitArray is reconstructed with correct total bit count (_size),
                // not just based on byte array length.
                var tempBits = new BitArray(bytes);
                for(int i=0; i < _size && i < tempBits.Length; ++i)
                {
                    _bits[i] = tempBits[i];
                }
                // If tempBits.Length < _size, remaining bits in _bits retain their initialized value (usually false)
             }
        }

        // Bitwise operators (returning new NodeMask instances)
        // These require both masks to have the same dimensions.
        public NodeMask And(NodeMask other)
        {
            if (_log2Dim != other._log2Dim) throw new ArgumentException("NodeMask dimensions must match for bitwise operations.");
            return new NodeMask(_log2Dim, new BitArray(_bits).And(other._bits));
        }
        public NodeMask Or(NodeMask other)
        {
            if (_log2Dim != other._log2Dim) throw new ArgumentException("NodeMask dimensions must match for bitwise operations.");
            return new NodeMask(_log2Dim, new BitArray(_bits).Or(other._bits));
        }
        public NodeMask Xor(NodeMask other)
        {
            if (_log2Dim != other._log2Dim) throw new ArgumentException("NodeMask dimensions must match for bitwise operations.");
            return new NodeMask(_log2Dim, new BitArray(_bits).Xor(other._bits));
        }
        public NodeMask Not()
        {
            return new NodeMask(_log2Dim, new BitArray(_bits).Not());
        }

        public static NodeMask operator &(NodeMask a, NodeMask b) => a.And(b);
        public static NodeMask operator |(NodeMask a, NodeMask b) => a.Or(b);
        public static NodeMask operator ^(NodeMask a, NodeMask b) => a.Xor(b);
        public static NodeMask operator ~(NodeMask a) => a.Not();
    }
}
