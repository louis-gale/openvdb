// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using OpenVDB.Core.Metadata; // For MetaMap
using OpenVDB.Math; // For Transform

namespace OpenVDB.Core.IO
{
    public static class IoConstants
    {
        /// <summary>
        /// Magic number for VDB files (0x56444200, 'VDB\0' in little-endian).
        /// The C++ code writes this as four individual bytes.
        /// </summary>
        public const uint Magic = 0x56444200; // VDB\0
        public const byte Magic0 = (byte)'V'; // 0x56
        public const byte Magic1 = (byte)'D'; // 0x44
        public const byte Magic2 = (byte)'B'; // 0x42
        public const byte Magic3 = (byte)'\0';// 0x00

        /// <summary>
        /// Current OpenVDB file format version.
        /// This should be updated as the format evolves.
        /// From C++ openvdb/version.h -> OPENVDB_FILE_VERSION_NUMBER
        /// For example, version 233 corresponds to OpenVDB 3.0.0.
        /// Let's use a common recent version as a placeholder.
        /// Version 223: Houdini 12.5, OpenVDB 0.99.2
        /// Version 224: OpenVDB 1.0.0
        /// Version 233: OpenVDB 3.0.0
        /// Version 240: OpenVDB 5.0.0
        /// Version 247: OpenVDB 7.0.0
        /// Version 250: OpenVDB 8.0.0
        /// Version 252: OpenVDB 10.0.0
        /// </summary>
        public const uint CurrentFileVersion = 252; // Example: OpenVDB 10.0.0

        /// <summary>
        /// Size of the file header before grid descriptors, including magic, versions, and UUID.
        /// Magic (4) + FileVersion (4) + LibMajor (2) + LibMinor (2) + GridOffsets (1) + UUID (36) = 49 bytes
        /// </summary>
        public const int BaseHeaderSize = 4 + 4 + 2 + 2 + 1 + 36;
    }

    public static class IoUtils
    {
        /// <summary>
        /// Reads a NUL-terminated or length-prefixed string from the reader.
        /// OpenVDB strings in files are typically length-prefixed.
        /// </summary>
        public static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32(); // VDB strings are prefixed with int32 length
            if (length < 0) throw new IOException("Invalid string length encountered in stream.");
            if (length == 0) return string.Empty;

            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Writes a string to the writer, prefixed with its length.
        /// </summary>
        public static void WriteString(BinaryWriter writer, string s)
        {
            if (s == null) s = string.Empty;
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            writer.Write(bytes.Length); // int32 length prefix
            writer.Write(bytes);
        }

        // Placeholders for MetaMap and Transform serialization - to be implemented later
        public static MetaMap ReadMetaMap(BinaryReader reader, StreamMetadata streamMeta)
        {
            // Implementation will depend on MetaMap's own ReadMeta method
            // and how Metadata objects are serialized.
            var metaMap = new MetaMap();
            // Example structure:
            // uint32_t numMetaItems = reader.ReadUInt32();
            // for (uint i = 0; i < numMetaItems; ++i) {
            //     string name = ReadString(reader);
            //     string typeName = ReadString(reader);
            //     Metadata meta = MetadataRegistry.Create(typeName); // Needs registry
            //     meta.ReadValue(reader, streamMeta); // Metadata needs ReadValue method
            //     metaMap.Insert(name, meta);
            // }
            throw new NotImplementedException("MetaMap serialization not yet implemented.");
            // return metaMap;
        }

        public static void WriteMetaMap(BinaryWriter writer, MetaMap metaMap, StreamMetadata streamMeta)
        {
            // writer.Write((uint)metaMap.Count);
            // foreach(var pair in metaMap) {
            //     WriteString(writer, pair.Key);
            //     WriteString(writer, pair.Value.TypeName);
            //     pair.Value.WriteValue(writer, streamMeta); // Metadata needs WriteValue method
            // }
            throw new NotImplementedException("MetaMap serialization not yet implemented.");
        }

        public static Transform ReadTransform(BinaryReader reader, StreamMetadata streamMeta)
        {
            // Transforms are serialized based on their map type.
            // string mapType = ReadString(reader);
            // IMap map = MapRegistry.Create(mapType); // Needs registry
            // map.Read(reader, streamMeta); // IMap needs Read method
            // return new Transform(map);
            throw new NotImplementedException("Transform serialization not yet implemented.");
            // return Transform.CreateLinearTransform(); // Placeholder
        }

        public static void WriteTransform(BinaryWriter writer, Transform transform, StreamMetadata streamMeta)
        {
            // WriteString(writer, transform.MapTypeName);
            // transform.GetMap().Write(writer, streamMeta); // IMap needs Write method
            throw new NotImplementedException("Transform serialization not yet implemented.");
        }

        /// <summary>
        /// Writes only the bytes of the string, assuming length is handled externally.
        /// Useful for StringMetadata where MetaMap writes the size first.
        /// </summary>
        public static void WriteStringBytes(BinaryWriter writer, string s)
        {
            if (s == null) return; // Or write nothing / handle as error
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            writer.Write(bytes);
        }

        /// <summary>
        /// Reads a string from bytes, assuming length is known externally.
        /// </summary>
        public static string ReadStringBytes(BinaryReader reader, int length)
        {
            if (length < 0) throw new IOException("Invalid string length for byte read.");
            if (length == 0) return string.Empty;
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
