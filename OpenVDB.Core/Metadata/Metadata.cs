// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using OpenVDB.Core.IO;
using OpenVDB.Math; // For Vec, Mat types

namespace OpenVDB.Core.Metadata
{
    // Common TypedMetadata aliases
    using BoolMetadata = TypedMetadata<bool>;
    using DoubleMetadata = TypedMetadata<double>;
    using FloatMetadata = TypedMetadata<float>;
    using Int32Metadata = TypedMetadata<int>;
    using Int64Metadata = TypedMetadata<long>;
    using StringMetadata = TypedMetadata<string>; // StringMetadata has special size/str in C++

    using Vec2DMetadata = TypedMetadata<Vec2<double>>;
    using Vec2IMetadata = TypedMetadata<Vec2<int>>;
    using Vec2SMetadata = TypedMetadata<Vec2<float>>; // 's' typically means float in OpenVDB

    using Vec3DMetadata = TypedMetadata<Vec3<double>>;
    using Vec3IMetadata = TypedMetadata<Vec3<int>>;
    using Vec3SMetadata = TypedMetadata<Vec3<float>>;

    // Assuming Vec4 types exist and are needed for metadata
    using Vec4DMetadata = TypedMetadata<Vec4<double>>;
    using Vec4IMetadata = TypedMetadata<Vec4<int>>;
    using Vec4SMetadata = TypedMetadata<Vec4<float>>;

    // Assuming Mat types exist and are needed for metadata
    using Mat3SMetadata = TypedMetadata<Mat3<float>>;
    using Mat3DMetadata = TypedMetadata<Mat3<double>>;
    using Mat4SMetadata = TypedMetadata<Mat4<float>>;
    using Mat4DMetadata = TypedMetadata<Mat4<double>>;

    /// <summary>
    /// Base class for storing metadata information.
    /// </summary>
    public abstract class Metadata
    {
        /// <summary>
        /// Gets the type name of the metadata (e.g., "float", "string", "Vec3d").
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// Creates a deep copy of this metadata object.
        /// </summary>
        public abstract Metadata Copy();

        /// <summary>
        /// Gets a string representation of the metadata's value.
        /// </summary>
        public abstract string ValueAsString();

        /// <summary>
        /// Gets the boolean representation of this metadata.
        /// (Empty strings and zero values typically evaluate to false).
        /// </summary>
        public abstract bool AsBool();

        // C++ size() returns bytes. This is less relevant for managed C#.
        // If needed for serialization, it would be handled differently.

        public override bool Equals(object obj)
        {
            if (obj is Metadata other)
            {
                if (TypeName != other.TypeName) return false;
                // This basic Equals should be overridden by derived classes for value comparison.
                // For now, it relies on ValueAsString for a generic comparison, which is not ideal.
                return ValueAsString() == other.ValueAsString();
            }
            return false;
        }

        public override int GetHashCode() => ValueAsString()?.GetHashCode() ?? 0;

        public static bool operator ==(Metadata left, Metadata right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(Metadata left, Metadata right) => !(left == right);

        // Abstract methods for I/O, to be implemented by derived classes
        public abstract void ReadValue(System.IO.BinaryReader reader, StreamMetadata streamMeta, uint sizeFromFile);
        public abstract void WriteValue(System.IO.BinaryWriter writer, StreamMetadata streamMeta);

        // C++ Metadata::size() is for on-disk size.
        // This is different from in-memory size and specific to serialization.
        public abstract uint GetSizeOnDisk();

        // Simplified factory method - a full registry pattern would be more robust.
        public static Metadata Create(string typeName)
        {
            // This is a very basic factory. A real implementation would use reflection
            // or a registered dictionary of type constructors.
            if (typeName == nameof(String)) return new StringMetadata();
            if (typeName == nameof(Int32)) return new Int32Metadata();
            if (typeName == nameof(Int64)) return new Int64Metadata();
            if (typeName == nameof(Single)) return new FloatMetadata();
            if (typeName == nameof(Double)) return new DoubleMetadata();
            if (typeName == nameof(Boolean)) return new BoolMetadata();
            if (typeName == nameof(Vec3<double>)) return new Vec3DMetadata();
            if (typeName == nameof(Vec3<float>)) return new Vec3SMetadata();
            if (typeName == nameof(Vec3<int>)) return new Vec3IMetadata();
            if (typeName == nameof(Mat4<double>)) return new Mat4DMetadata();
            // Add other common types...
            return new UnknownMetadata(typeName); // Fallback for unregistered types
        }
    }

    /// <summary>
    /// Stores metadata of an unknown or unregistered type as a byte array.
    /// </summary>
    public class UnknownMetadata : Metadata
    {
        private readonly string _typeName;
        private byte[] _data;

        public override string TypeName => _typeName;
        public byte[] Value => _data;

        public UnknownMetadata(string typeName, byte[] data = null)
        {
            _typeName = string.IsNullOrEmpty(typeName) ? "<unknown>" : typeName;
            _data = data ?? Array.Empty<byte>();
        }

        public override Metadata Copy() => new UnknownMetadata(_typeName, (byte[])_data.Clone());

        public override string ValueAsString() => _data.Length > 0 ? "<binary_data>" : "";

        public override bool AsBool() => _data.Length > 0;

        public void SetValue(byte[] data) => _data = data ?? Array.Empty<byte>();

        public override void ReadValue(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMeta, uint sizeFromFile)
        {
            _data = reader.ReadBytes((int)sizeFromFile);
        }

        public override void WriteValue(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMeta)
        {
            // Size is written by MetaMap before calling this.
            writer.Write(_data);
        }
        public override uint GetSizeOnDisk() => (uint)(_data?.Length ?? 0);

    }


    /// <summary>
    /// Generic class to hold strongly-typed metadata values.
    /// </summary>
    /// <typeparam name="T">The type of the metadata value.</typeparam>
    public class TypedMetadata<T> : Metadata
    {
        private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public override string TypeName => nameof(T); // Using global TypeName helper

        public TypedMetadata()
        {
            _value = default(T);
        }

        public TypedMetadata(T value)
        {
            _value = value;
        }

        public TypedMetadata(TypedMetadata<T> other) // Copy constructor
        {
            // For simple value types or immutable types, direct assignment is fine.
            // For complex reference types, a deep copy mechanism might be needed if T can be one.
            // Assuming T is mostly primitive, string, or simple struct (Vec, Mat).
            _value = other._value;
        }

        public override Metadata Copy() => new TypedMetadata<T>(this);

        public override string ValueAsString()
        {
            if (_value == null) return string.Empty;
            if (typeof(T) == typeof(bool)) return (bool)(object)_value ? "true" : "false";
            return _value.ToString();
        }

        public override bool AsBool()
        {
            if (_value == null) return false;
            if (typeof(T) == typeof(string)) return !string.IsNullOrEmpty((string)(object)_value);
            if (typeof(T) == typeof(bool)) return (bool)(object)_value;

            // Mimic C++ isZero behavior: non-zero is true.
            try { return Convert.ToDouble(_value) != 0.0; } // General case for numbers
            catch { /* Fall through for non-convertible types */ }

            return true; // Default for non-numeric, non-string, non-bool if not null
        }

        public override bool Equals(object obj)
        {
            if (obj is TypedMetadata<T> other)
            {
                if (_value == null) return other._value == null;
                return _value.Equals(other._value);
            }
            return false;
        }

        public override int GetHashCode() => _value?.GetHashCode() ?? 0;

        public override void WriteValue(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMeta)
        {
            // This is where type-specific serialization would happen.
            // Size is written by MetaMap before this call.
            if (typeof(T) == typeof(string))
                IoUtils.WriteStringBytes(writer, (string)(object)_value); // Write only bytes, length handled by MetaMap
            else if (typeof(T) == typeof(bool)) writer.Write((bool)(object)_value);
            else if (typeof(T) == typeof(int)) writer.Write((int)(object)_value);
            else if (typeof(T) == typeof(long)) writer.Write((long)(object)_value);
            else if (typeof(T) == typeof(float)) writer.Write((float)(object)_value);
            else if (typeof(T) == typeof(double)) writer.Write((double)(object)_value);
            else if (typeof(T) == typeof(Vec3<double>)) ((Vec3<double>)(object)_value).Write(writer);
            else if (typeof(T) == typeof(Vec3<float>)) ((Vec3<float>)(object)_value).Write(writer);
            else if (typeof(T) == typeof(Vec3<int>)) ((Vec3<int>)(object)_value).Write(writer);
            // ... other Vec/Mat types
            else if (typeof(T) == typeof(Mat4<double>)) ((Mat4<double>)(object)_value).Write(writer);
            // ...
            else throw new NotSupportedException($"Metadata serialization for type {TypeName} not implemented.");
        }

        public override void ReadValue(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMeta, uint sizeFromFile)
        {
            // Size is read by MetaMap before this call.
            if (typeof(T) == typeof(string))
                _value = (T)(object)IoUtils.ReadStringBytes(reader, (int)sizeFromFile);
            else if (typeof(T) == typeof(bool)) _value = (T)(object)reader.ReadBoolean();
            else if (typeof(T) == typeof(int)) _value = (T)(object)reader.ReadInt32();
            else if (typeof(T) == typeof(long)) _value = (T)(object)reader.ReadInt64();
            else if (typeof(T) == typeof(float)) _value = (T)(object)reader.ReadSingle();
            else if (typeof(T) == typeof(double)) _value = (T)(object)reader.ReadDouble();
            else if (typeof(T) == typeof(Vec3<double>)) _value = (T)(object)Vec3<double>.Read(reader);
            else if (typeof(T) == typeof(Vec3<float>)) _value = (T)(object)Vec3<float>.Read(reader);
            else if (typeof(T) == typeof(Vec3<int>)) _value = (T)(object)Vec3<int>.Read(reader);
            // ... other Vec/Mat types
            else if (typeof(T) == typeof(Mat4<double>)) _value = (T)(object)Mat4<double>.Read(reader);
            // ...
            else throw new NotSupportedException($"Metadata deserialization for type {TypeName} not implemented.");
        }

        public override uint GetSizeOnDisk()
        {
            if (typeof(T) == typeof(string))
                return (uint)Encoding.UTF8.GetByteCount((string)(object)_value ?? "");
            if (typeof(T) == typeof(bool)) return sizeof(bool);
            if (typeof(T) == typeof(int)) return sizeof(int);
            if (typeof(T) == typeof(long)) return sizeof(long);
            if (typeof(T) == typeof(float)) return sizeof(float);
            if (typeof(T) == typeof(double)) return sizeof(double);
            if (typeof(T) == typeof(Vec3<double>)) return (uint)Unsafe.SizeOf<Vec3<double>>();
            if (typeof(T) == typeof(Vec3<float>)) return (uint)Unsafe.SizeOf<Vec3<float>>();
            if (typeof(T) == typeof(Vec3<int>)) return (uint)Unsafe.SizeOf<Vec3<int>>();
            if (typeof(T) == typeof(Mat4<double>)) return (uint)Unsafe.SizeOf<Mat4<double>>();
            // ... other types
            throw new NotSupportedException($"GetSizeOnDisk for type {TypeName} not implemented.");
        }
    }
}
