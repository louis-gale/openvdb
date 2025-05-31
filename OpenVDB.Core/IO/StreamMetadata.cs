// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using OpenVDB.Core.Metadata; // For MetaMap

namespace OpenVDB.Core.IO
{
    /// <summary>
    /// Container for metadata describing how to unserialize grids from and/or
    /// serialize grids to a stream (which file format, compression scheme, etc. to use).
    /// This class is primarily used internally by Archive and I/O operations.
    /// </summary>
    public class StreamMetadata
    {
        // From C++ Impl struct
        public uint FileVersion { get; set; } = IoConstants.CurrentFileVersion; // Default to current
        public ushort LibraryVersionMajor { get; set; } = Version.MajorVersion;
        public ushort LibraryVersionMinor { get; set; } = Version.MinorVersion;

        public CompressionFlags Compression { get; set; } = CompressionFlags.Default;

        public GridClass GridClass { get; set; } = GridClass.Unknown;

        // Background value handling: store as object and its type
        public object BackgroundValue { get; private set; }
        public Type BackgroundType { get; private set; }

        public bool HalfFloat { get; set; } = false; // Whether to save floats as 16-bit half
        public bool WriteGridStats { get; set; } = true; // Whether to write grid stats metadata
        public bool IsSeekable { get; set; } = false; // Whether the underlying stream is seekable

        // For multi-pass I/O (placeholders for now, as full multi-pass is complex)
        public bool IsCountingPasses { get; set; } = false;
        public uint CurrentPass { get; set; } = 0; // Format: (numPasses << 16) | passIdx

        // For delayed loading (placeholders)
        public ulong CurrentLeafNode { get; set; } = 0;
        public bool HasDelayedLoadMetadata => GridMeta?.HasMetadata(GridBaseMetadataKeys.FileDelayedLoad) ?? false;


        /// <summary>
        /// Metadata of the grid currently being read or written.
        /// </summary>
        public MetaMap GridMeta { get; set; }

        /// <summary>
        /// Auxiliary data map for custom user data associated with the stream.
        /// </summary>
        public Dictionary<string, object> AuxData { get; set; }

        // Test member from C++ Impl (purpose unclear, included for completeness if needed by tests/logic)
        private uint _testData = 0;


        public StreamMetadata()
        {
            GridMeta = new MetaMap();
            AuxData = new Dictionary<string, object>();
            SetBackgroundValue<object>(null); // Initialize with null background
        }

        public StreamMetadata(StreamMetadata other) // Copy constructor
        {
            FileVersion = other.FileVersion;
            LibraryVersionMajor = other.LibraryVersionMajor;
            LibraryVersionMinor = other.LibraryVersionMinor;
            Compression = other.Compression;
            GridClass = other.GridClass;
            BackgroundValue = other.BackgroundValue; // Shallow copy for background, might need deep for some types
            BackgroundType = other.BackgroundType;
            HalfFloat = other.HalfFloat;
            WriteGridStats = other.WriteGridStats;
            IsSeekable = other.IsSeekable;
            IsCountingPasses = other.IsCountingPasses;
            CurrentPass = other.CurrentPass;
            CurrentLeafNode = other.CurrentLeafNode;
            _testData = other._testData;

            GridMeta = new MetaMap(other.GridMeta); // Deep copy MetaMap
            AuxData = new Dictionary<string, object>(other.AuxData); // Shallow copy aux data dictionary
        }

        public void SetBackgroundValue<T>(T value)
        {
            BackgroundValue = value;
            BackgroundType = typeof(T);
        }

        public T GetBackgroundValue<T>(T defaultValue = default)
        {
            if (BackgroundValue == null && BackgroundType == null) return defaultValue; // Not set
            if (BackgroundValue is T typedValue) return typedValue;
            if (BackgroundType == typeof(T)) // Value might be null for reference types
            {
                return (T)BackgroundValue; // This will be null if BackgroundValue is null and T is reference type
            }

            // Attempt conversion if types are compatible (e.g. int to long)
            try { return (T)Convert.ChangeType(BackgroundValue, typeof(T)); }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
            {
                throw new InvalidCastException($"Cannot convert background value of type {BackgroundType?.Name ?? "null"} to {typeof(T).Name}.", ex);
            }
        }

        public string GetVersionString() => $"{LibraryVersionMajor}.{LibraryVersionMinor}/{FileVersion}";

        // Test methods from C++ (if their purpose becomes clear or needed for direct porting of tests)
        public uint GetTestData() => _testData;
        public void SetTestData(uint data) => _testData = data;

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"File Version: {FileVersion}");
            sb.AppendLine($"Library Version: {LibraryVersionMajor}.{LibraryVersionMinor}");
            sb.AppendLine($"Compression: {CompressionUtil.ToString(Compression)}");
            sb.AppendLine($"Grid Class: {GridBase.GridClassToString(GridClass)}");
            sb.AppendLine($"Background Value: {BackgroundValue} (Type: {BackgroundType?.Name ?? "null"})");
            sb.AppendLine($"Half Float: {HalfFloat}");
            sb.AppendLine($"Write Grid Stats: {WriteGridStats}");
            sb.AppendLine($"Seekable Stream: {IsSeekable}");
            if (GridMeta.Count > 0)
            {
                sb.AppendLine("Grid Metadata:");
                foreach(var pair in GridMeta) sb.AppendLine($"  {pair.Key}: {pair.Value.ValueAsString()}");
            }
            if (AuxData.Count > 0)
            {
                sb.AppendLine("Auxiliary Data:");
                 foreach(var pair in AuxData) sb.AppendLine($"  {pair.Key}: {pair.Value}");
            }
            return sb.ToString();
        }
    }
}
