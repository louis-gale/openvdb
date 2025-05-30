// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OpenVDB.Core.Metadata
{
    /// <summary>
    /// Container that maps names (strings) to values of arbitrary types (Metadata).
    /// </summary>
    public class MetaMap : IEnumerable<KeyValuePair<string, Metadata>>
    {
        private readonly Dictionary<string, Metadata> _metadataMap;

        public MetaMap()
        {
            _metadataMap = new Dictionary<string, Metadata>();
        }

        /// <summary>
        /// Copy constructor (performs a deep copy of metadata values).
        /// </summary>
        public MetaMap(MetaMap other)
        {
            _metadataMap = new Dictionary<string, Metadata>(other._metadataMap.Count);
            foreach (var pair in other._metadataMap)
            {
                _metadataMap[pair.Key] = pair.Value.Copy(); // Deep copy each metadata item
            }
        }

        /// <summary>
        /// Returns a deep copy of this map.
        /// </summary>
        public MetaMap DeepCopy() => new MetaMap(this);

        /// <summary>
        /// Inserts a new metadata field or overwrites the value of an existing field.
        /// </summary>
        /// <param name="name">The name of the metadata field.</param>
        /// <param name="metadata">The metadata object to insert.</param>
        /// <exception cref="ArgumentException">If the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">If metadata is null.</exception>
        /// <exception cref="TypeErrorException">If a field with the given name already exists but has a different type.</exception>
        public void Insert(string name, Metadata metadata)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Metadata name cannot be null or empty.", nameof(name));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (_metadataMap.TryGetValue(name, out Metadata existingMeta))
            {
                if (existingMeta.TypeName != metadata.TypeName)
                {
                    throw new TypeErrorException(
                        $"Metadata '{name}' already exists with type '{existingMeta.TypeName}', cannot overwrite with type '{metadata.TypeName}'.");
                }
                // Overwrite with a copy to maintain consistency (e.g. if user inserts same instance twice)
                _metadataMap[name] = metadata.Copy(); 
            }
            else
            {
                _metadataMap[name] = metadata.Copy();
            }
        }

        /// <summary>
        /// Inserts a strongly-typed metadata value.
        /// </summary>
        public void Insert<T>(string name, T value)
        {
            Insert(name, new TypedMetadata<T>(value));
        }

        /// <summary>
        /// Deep copies all metadata fields from the given map into this map.
        /// </summary>
        /// <param name="otherMap">The map to copy metadata from.</param>
        /// <exception cref="TypeErrorException">If any field in the given map has the same name but a different type.</exception>
        public void Insert(MetaMap otherMap)
        {
            if (otherMap == null) return;
            foreach (var pair in otherMap._metadataMap)
            {
                Insert(pair.Key, pair.Value); // Insert will handle copying and type checking
            }
        }
        
        /// <summary>
        /// Alias for Insert(MetaMap otherMap).
        /// </summary>
        public void UnionWith(MetaMap otherMap) => Insert(otherMap);


        /// <summary>
        /// Removes the metadata field with the given name.
        /// </summary>
        /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
        public bool Remove(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return _metadataMap.Remove(name);
        }

        /// <summary>
        /// Gets the metadata object associated with the specified name.
        /// </summary>
        /// <returns>The metadata object, or null if the key is not found.</returns>
        public Metadata GetMetadata(string name)
        {
            _metadataMap.TryGetValue(name, out Metadata meta);
            return meta; // Returns null if not found, which is similar to C++ Ptr behavior
        }
        
        /// <summary>
        /// Gets the strongly-typed metadata object associated with the specified name.
        /// </summary>
        /// <typeparam name="TMetadataType">The specific TypedMetadata type (e.g., FloatMetadata, StringMetadata).</typeparam>
        /// <returns>The typed metadata object, or null if not found or if type mismatches.</returns>
        public TMetadataType GetMetadata<TMetadataType>(string name) where TMetadataType : Metadata
        {
            if (_metadataMap.TryGetValue(name, out Metadata meta))
            {
                return meta as TMetadataType;
            }
            return null;
        }

        /// <summary>
        /// Gets the value of a strongly-typed metadata field.
        /// </summary>
        /// <typeparam name="TValue">The expected type of the value.</typeparam>
        /// <param name="name">The name of the metadata field.</param>
        /// <returns>The value of the metadata field.</returns>
        /// <exception cref="KeyErrorException">If no field with the given name exists.</exception>
        /// <exception cref="TypeErrorException">If the field is not of type TValue.</exception>
        public TValue GetValue<TValue>(string name)
        {
            if (!_metadataMap.TryGetValue(name, out Metadata meta))
                throw new KeyErrorException($"Metadata field '{name}' not found.");

            if (meta is TypedMetadata<TValue> typedMeta)
                return typedMeta.Value;
            
            // Attempt conversion for some common compatible types if direct cast fails
            if (meta is TypedMetadata<double> doubleMeta && typeof(TValue) == typeof(float))
                return (TValue)(object)Convert.ToSingle(doubleMeta.Value);
            if (meta is TypedMetadata<float> floatMeta && typeof(TValue) == typeof(double))
                return (TValue)(object)Convert.ToDouble(floatMeta.Value);
            if (meta is TypedMetadata<long> longMeta && typeof(TValue) == typeof(int))
                return (TValue)(object)Convert.ToInt32(longMeta.Value);
             if (meta is TypedMetadata<int> intMeta && typeof(TValue) == typeof(long))
                return (TValue)(object)Convert.ToInt64(intMeta.Value);

            throw new TypeErrorException(
                $"Metadata field '{name}' is of type '{meta.TypeName}', not assignable to '{OpenVDB.TypeName.GetName<TValue>()}'.");
        }

        /// <summary>
        /// Gets the value of a strongly-typed metadata field, or a default value if not found or type mismatches.
        /// </summary>
        public TValue GetValue<TValue>(string name, TValue defaultValue)
        {
            if (_metadataMap.TryGetValue(name, out Metadata meta) && meta is TypedMetadata<TValue> typedMeta)
                return typedMeta.Value;
            
            // Attempt conversion for some common compatible types if direct cast fails
            if (meta is TypedMetadata<double> doubleMeta && typeof(TValue) == typeof(float))
                return (TValue)(object)Convert.ToSingle(doubleMeta.Value);
            if (meta is TypedMetadata<float> floatMeta && typeof(TValue) == typeof(double))
                return (TValue)(object)Convert.ToDouble(floatMeta.Value);

            return defaultValue;
        }

        public bool HasMetadata(string name) => _metadataMap.ContainsKey(name);

        public int Count => _metadataMap.Count;

        public void Clear() => _metadataMap.Clear();

        public IEnumerator<KeyValuePair<string, Metadata>> GetEnumerator() => _metadataMap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public override bool Equals(object obj)
        {
            if (obj is MetaMap other)
            {
                if (Count != other.Count) return false;
                foreach (var pair in _metadataMap)
                {
                    if (!other._metadataMap.TryGetValue(pair.Key, out Metadata otherMeta)) return false;
                    if (!pair.Value.Equals(otherMeta)) return false;
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Simple hash based on count. More robust would iterate, but can be slow.
            return _metadataMap.Count; 
        }

        public void Write(BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMeta)
        {
            writer.Write((uint)_metadataMap.Count);
            foreach (var pair in _metadataMap)
            {
                OpenVDB.Core.IO.IoUtils.WriteString(writer, pair.Key);
                OpenVDB.Core.IO.IoUtils.WriteString(writer, pair.Value.TypeName);

                // Value serialization - this is simplified.
                // A full implementation needs type dispatch and robust serialization for each type.
                // C++ Metadata has virtual WriteValue.
                if (pair.Value is TypedMetadata<string> strMeta)
                    OpenVDB.Core.IO.IoUtils.WriteString(writer, strMeta.Value);
                else if (pair.Value is TypedMetadata<int> intMeta)
                    writer.Write(intMeta.Value);
                else if (pair.Value is TypedMetadata<long> longMeta)
                    writer.Write(longMeta.Value);
                else if (pair.Value is TypedMetadata<float> floatMeta)
                    writer.Write(floatMeta.Value);
                else if (pair.Value is TypedMetadata<double> doubleMeta)
                    writer.Write(doubleMeta.Value);
                else if (pair.Value is TypedMetadata<bool> boolMeta)
                    writer.Write(boolMeta.Value);
                // Add more types as needed: Vec3d, Mat4d etc.
                // For Vec/Mat types, each component would be written.
                // For UnknownMetadata, its byte array would be written.
                else if (pair.Value is UnknownMetadata unknownMeta)
                {
                    writer.Write(unknownMeta.Value.Length);
                    writer.Write(unknownMeta.Value);
                }
                else
                {
                    // Placeholder for other types or throw exception
                    Console.Error.WriteLine($"Warning: MetaMap.Write skipping unsupported metadata type: {pair.Value.TypeName} for key '{pair.Key}'");
                    // To avoid corrupting the stream, we should write something or have a clear "unsupported" marker.
                    // For now, just skipping value part for unsupported. A robust solution needs type-specific handlers.
                    // Or, the Metadata object itself should have a WriteValue(writer) method.
                }
            }
        }

        public void Read(BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMeta)
        {
            Clear();
            uint numMetaItems = reader.ReadUInt32();
            for (uint i = 0; i < numMetaItems; ++i)
            {
                string name = OpenVDB.Core.IO.IoUtils.ReadString(reader);
                string typeName = OpenVDB.Core.IO.IoUtils.ReadString(reader);

                // Value deserialization - simplified.
                // Needs a way to create Metadata instance from typeName and then read value.
                // This typically involves a factory or reflection.
                // For now, supporting only a few basic types.
                Metadata metaValue = null;
                if (typeName == OpenVDB.TypeName.GetName<string>())
                    metaValue = new StringMetadata(OpenVDB.Core.IO.IoUtils.ReadString(reader));
                else if (typeName == OpenVDB.TypeName.GetName<int>())
                    metaValue = new Int32Metadata(reader.ReadInt32());
                else if (typeName == OpenVDB.TypeName.GetName<long>())
                    metaValue = new Int64Metadata(reader.ReadInt64());
                else if (typeName == OpenVDB.TypeName.GetName<float>())
                    metaValue = new FloatMetadata(reader.ReadSingle());
                else if (typeName == OpenVDB.TypeName.GetName<double>())
                    metaValue = new DoubleMetadata(reader.ReadDouble());
                else if (typeName == OpenVDB.TypeName.GetName<bool>())
                    metaValue = new BoolMetadata(reader.ReadBoolean());
                // Add more types as needed
                else
                {
                     Console.Error.WriteLine($"Warning: MetaMap.Read skipping unsupported metadata type: {typeName} for key '{name}'");
                    // To robustly skip, we'd need to know the byte size of the value.
                    // This simplified version will likely fail if an unsupported type is encountered.
                    // A better approach: use Metadata.Create(typeName) then meta.ReadValue(reader).
                    // For now, if type is not supported, we insert a placeholder or skip.
                    // This will lead to data loss for unsupported types.
                    // For UnknownMetadata, it should read its byte array.
                    // metaValue = new UnknownMetadata(typeName, /* read byte array here */);
                }
                
                if (metaValue != null)
                {
                    _metadataMap[name] = metaValue; // No need to copy, it's newly created.
                }
            }
        }
    }
}
