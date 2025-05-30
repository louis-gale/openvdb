// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic; // For EqualityComparer
using System.IO;
using OpenVDB.Core.Util; // For NodeMask

namespace OpenVDB.Core.Tree
{
    /// <summary>
    /// Stores a dense array of voxel values for a leaf node.
    /// Can be in a "uniform" state (all values are the same, not explicitly stored)
    /// or "non-uniform" state (values stored in an array).
    /// </summary>
    /// <typeparam name="TValue">The type of values stored in the buffer.</typeparam>
    public class LeafBuffer<TValue> where TValue : struct // Using struct constraint for TValue
    {
        private TValue[] _data;
        private TValue _uniformValue;
        private bool _isUniform;
        private readonly int _size;
        private readonly int _log2Dim;

        public int Size => _size;
        public int Log2Dim => _log2Dim;
        public bool IsUniform => _isUniform;
        public TValue UniformValue => _isUniform ? _uniformValue : throw new InvalidOperationException("Buffer is not uniform. Access individual values.");
        public bool IsAllocated => _data != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeafBuffer{TValue}"/> class.
        /// </summary>
        /// <param name="log2Dim">Log base 2 of the dimension of the leaf node (e.g., 3 for an 8x8x8 node).</param>
        /// <param name="initialValue">The initial value for all voxels in the buffer.</param>
        /// <param name="startAsUniform">If true, the buffer starts in a uniform state. Otherwise, it's allocated and filled.</param>
        public LeafBuffer(int log2Dim, TValue initialValue, bool startAsUniform = true)
        {
            if (log2Dim < 0 || log2Dim > 10) // Max Log2Dim (e.g. 5 for 32^3 LeafNode)
                throw new ArgumentOutOfRangeException(nameof(log2Dim), "Log2Dim must be non-negative and reasonable.");

            _log2Dim = log2Dim;
            int dim = 1 << log2Dim;
            _size = dim * dim * dim;
            if (_size < 0) _size = 0; // Should not happen with log2Dim constraint

            if (startAsUniform)
            {
                _uniformValue = initialValue;
                _isUniform = true;
                _data = null;
            }
            else
            {
                _uniformValue = default; // Not strictly needed if _isUniform is false
                _isUniform = false;
                _data = new TValue[_size];
                if (_size > 0) // Array.Fill throws if count is 0 for some overloads or types
                {
                    // Array.Fill is .NET Core 2.0+ / .NET Standard 2.1+
                    // If not available, use a loop:
                    // for (int i = 0; i < _size; ++i) { _data[i] = initialValue; }
                    Array.Fill(_data, initialValue);
                }
            }
        }
        
        /// <summary>
        /// Ensures the internal data array is allocated. If the buffer was uniform,
        /// it's filled with the uniform value. The buffer becomes non-uniform after this call.
        /// </summary>
        public void Allocate()
        {
            EnsureAllocatedAndMakeNonUniform(setToNonUniform: false); // Allocate will make it non-uniform if it was uniform and gets data array
                                                                      // but if it was already non-uniform and allocated, it remains non-uniform.
                                                                      // The flag `setToNonUniform` in EnsureAllocatedAndMakeNonUniform
                                                                      // is for when we are about to write, making it definitively non-uniform.
                                                                      // Here, just allocating from uniform state still means it's effectively non-uniform
                                                                      // as _data is now primary.
             if (_isUniform && _data != null) // If it was uniform and data got allocated
            {
                _isUniform = false; // Now it's non-uniform because _data exists
            }
        }
        
        private void EnsureAllocatedAndMakeNonUniform()
        {
            if (_data == null)
            {
                _data = new TValue[_size];
                if (_isUniform && _size > 0) // If it was uniform, fill with the uniform value
                {
                    Array.Fill(_data, _uniformValue);
                }
            }
            _isUniform = false; // Any operation that needs allocated data makes it non-uniform
        }


        public TValue GetValue(int index)
        {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index));

            if (_isUniform) // If uniform, _data might be null or might be allocated but still const
            {
                return _uniformValue;
            }
            else
            {
                // If not uniform, _data must have been allocated by SetValue or Allocate.
                if (_data == null)
                {
                    // This state should ideally not be reached if logic is correct.
                    // It means _isUniform = false but _data = null.
                    // This might happen if Fill was called, then _isUniform was somehow set to false externally.
                    // For robustness, allocate here, but this indicates a potential logic error elsewhere.
                    // Or, it means the buffer was default constructed and never truly initialized.
                    // Let's assume if !isUniform, data must be allocated.
                    throw new InvalidOperationException("Buffer is marked non-uniform but data array is not allocated.");
                }
                return _data[index];
            }
        }

        public void SetValue(int index, TValue value)
        {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException(nameof(index));
            
            EnsureAllocatedAndMakeNonUniform();
            _data[index] = value;
        }

        /// <summary>
        /// Fills the buffer with a single value, making it uniform.
        /// The internal data array is released to save memory.
        /// </summary>
        public void Fill(TValue value)
        {
            _uniformValue = value;
            _isUniform = true;
            _data = null; // Release array
        }
        
        public void Write(BinaryWriter writer, NodeMask valueMask, bool saveFloatAsHalf)
        {
            if (valueMask.Count != _size)
                throw new ArgumentException("NodeMask size does not match LeafBuffer size.", nameof(valueMask));

            writer.Write(_isUniform);
            if (_isUniform)
            {
                // Write the uniform value (handle half float if TValue is float/double)
                WriteSingleValue(writer, _uniformValue, saveFloatAsHalf && (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(double)));
            }
            else
            {
                // Non-uniform: write only active values indicated by the mask
                if (_data == null) throw new InvalidOperationException("Non-uniform buffer has no allocated data to write.");
                
                for (int i = 0; i < _size; ++i)
                {
                    if (valueMask.IsOn(i))
                    {
                        WriteSingleValue(writer, _data[i], saveFloatAsHalf && (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(double)));
                    }
                }
            }
        }

        public void Read(BinaryReader reader, NodeMask valueMask, TValue backgroundIfAllMaskedOff, bool readFloatAsHalf)
        {
            if (valueMask.Count != _size)
                throw new ArgumentException("NodeMask size does not match LeafBuffer size.", nameof(valueMask));

            _isUniform = reader.ReadBoolean();
            if (_isUniform)
            {
                _uniformValue = ReadSingleValue(reader, readFloatAsHalf && (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(double)));
                _data = null;
            }
            else
            {
                // Non-uniform: read active values, fill others with background
                EnsureAllocatedAndMakeNonUniform(); // Allocates _data and sets _isUniform = false
                
                if (valueMask.IsAllOff())
                {
                    if(_size > 0) Array.Fill(_data, backgroundIfAllMaskedOff);
                    // Potentially switch back to uniform state if all are background
                    if (EqualityComparer<TValue>.Default.Equals(backgroundIfAllMaskedOff, _uniformValue)) // Check if this background makes it uniform
                    {
                        Fill(backgroundIfAllMaskedOff); // This will set _isUniform = true and _data = null
                    }
                }
                else
                {
                    for (int i = 0; i < _size; ++i)
                    {
                        if (valueMask.IsOn(i))
                        {
                            _data[i] = ReadSingleValue(reader, readFloatAsHalf && (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(double)));
                        }
                        else
                        {
                            _data[i] = backgroundIfAllMaskedOff;
                        }
                    }
                }
            }
        }
        
        // Helper for single value I/O with half float consideration
        private void WriteSingleValue(BinaryWriter writer, TValue value, bool useHalf)
        {
            if (useHalf)
            {
                if (value is float f) writer.Write((Half)f);
                else if (value is double d) writer.Write((Half)(float)d); // Half from float
                else WriteNonFloatValue(writer, value); // Should not happen if useHalf is true for non-float
            }
            else WriteNonFloatValue(writer, value);
        }

        private TValue ReadSingleValue(BinaryReader reader, bool useHalf)
        {
            if (useHalf)
            {
                Half h = Half.ToHalf(reader.ReadUInt16()); // Assuming Half is System.Half and can be created from UInt16 bits
                if (typeof(TValue) == typeof(float)) return (TValue)(object)(float)h;
                if (typeof(TValue) == typeof(double)) return (TValue)(object)(double)(float)h; // Promote Half to float then to double
                return ReadNonFloatValue(reader); // Should not happen
            }
            return ReadNonFloatValue(reader);
        }
        
        private void WriteNonFloatValue(BinaryWriter writer, TValue value)
        {
            if (value is bool b) writer.Write(b);
            else if (value is int i) writer.Write(i);
            else if (value is uint ui) writer.Write(ui);
            else if (value is long l) writer.Write(l);
            else if (value is ulong ul) writer.Write(ul);
            else if (value is short sh) writer.Write(sh);
            else if (value is ushort ush) writer.Write(ush);
            else if (value is byte by) writer.Write(by);
            else if (value is sbytesby) writer.Write(sby);
            else if (value is double dbl) writer.Write(dbl); // Fallback if not half
            else if (value is float flt) writer.Write(flt);   // Fallback if not half
            // Add Vec/Mat types if they are to be stored directly without TypedMetadata wrapper
            else if (value is Vec3<float> v3f) { v3f.Write(writer); } // Assuming Vec3<T>.Write exists
            else if (value is Vec3<double> v3d) { v3d.Write(writer); }
            // ... etc for other supported TValue types
            else throw new NotSupportedException($"LeafBuffer.WriteValue not supported for type {typeof(TValue)}");
        }

        private TValue ReadNonFloatValue(BinaryReader reader)
        {
            if (typeof(TValue) == typeof(bool)) return (TValue)(object)reader.ReadBoolean();
            if (typeof(TValue) == typeof(int)) return (TValue)(object)reader.ReadInt32();
            if (typeof(TValue) == typeof(uint)) return (TValue)(object)reader.ReadUInt32();
            if (typeof(TValue) == typeof(long)) return (TValue)(object)reader.ReadInt64();
            if (typeof(TValue) == typeof(ulong)) return (TValue)(object)reader.ReadUInt64();
            if (typeof(TValue) == typeof(short)) return (TValue)(object)reader.ReadInt16();
            if (typeof(TValue) == typeof(ushort)) return (TValue)(object)reader.ReadUInt16();
            if (typeof(TValue) == typeof(byte)) return (TValue)(object)reader.ReadByte();
            if (typeof(TValue) == typeof(sbyte)) return (TValue)(object)reader.ReadSByte();
            if (typeof(TValue) == typeof(double)) return (TValue)(object)reader.ReadDouble(); // Fallback if not half
            if (typeof(TValue) == typeof(float)) return (TValue)(object)reader.ReadSingle();   // Fallback if not half
            if (typeof(TValue) == typeof(Vec3<float>)) return (TValue)(object)Vec3<float>.Read(reader);
            if (typeof(TValue) == typeof(Vec3<double>)) return (TValue)(object)Vec3<double>.Read(reader);
            // ... etc for other supported TValue types
            throw new NotSupportedException($"LeafBuffer.ReadValue not supported for type {typeof(TValue)}");
        }
    }
}
