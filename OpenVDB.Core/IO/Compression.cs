// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Core.IO
{
    /// <summary>
    /// OR-able bit flags for compression options on input and output streams.
    /// </summary>
    [Flags]
    public enum CompressionFlags : uint
    {
        /// <summary>
        /// No compression.
        /// On write, don't compress data.
        /// On read, the input stream contains uncompressed data.
        /// </summary>
        None = 0,

        /// <summary>
        /// ZLIB compression.
        /// When writing grids other than level sets or fog volumes, apply
        /// ZLIB compression to internal and leaf node value buffers.
        /// When reading, indicates that value buffers are ZLIB-compressed.
        /// ZLIB compresses well but can be slow.
        /// </summary>
        Zip = 0x1,

        /// <summary>
        /// Active mask compression.
        /// When writing a grid, don't output a node's inactive values
        /// if it has two or fewer distinct values. Instead, output minimal information
        /// to permit lossless reconstruction of inactive values.
        /// On read, nodes might have been stored without inactive values.
        /// </summary>
        ActiveMask = 0x2,

        /// <summary>
        /// Blosc compression.
        /// When writing grids other than level sets or fog volumes, apply
        /// Blosc compression to internal and leaf node value buffers.
        /// When reading, indicates that value buffers are Blosc-compressed.
        /// Blosc is much faster than ZLIB and can produce comparable file sizes.
        /// </summary>
        Blosc = 0x4,

        /// <summary>
        /// Default compression setting. Typically includes ActiveMask and a fast compressor if available.
        /// OpenVDB C++ defaults to ActiveMask | Blosc if Blosc is available, else ActiveMask | Zip.
        /// For C# port, actual Blosc/Zip support will be implemented later.
        /// </summary>
        Default = ActiveMask // Blosc or Zip would be added here once supported.
                            // For now, just ActiveMask to match the C++ Archive default constructor if no compressor is chosen.
    }

    /// <summary>
    /// Internal per-node indicator byte that specifies what additional metadata
    /// is stored to permit reconstruction of inactive values when ActiveMask compression is used.
    /// </summary>
    public enum MaskMetadataFlags : byte
    {
        /// <summary>No inactive vals, or all inactive vals are +background.</summary>
        NoMaskOrInactiveVals = 0,
        /// <summary>All inactive vals are -background.</summary>
        NoMaskAndMinusBg = 1,
        /// <summary>All inactive vals have the same non-background val.</summary>
        NoMaskAndOneInactiveVal = 2,
        /// <summary>Mask selects between -background and +background.</summary>
        MaskAndNoInactiveVals = 3,
        /// <summary>Mask selects between background and one other inactive val.</summary>
        MaskAndOneInactiveVal = 4,
        /// <summary>Mask selects between two non-background inactive vals.</summary>
        MaskAndTwoInactiveVals = 5,
        /// <summary>> 2 inactive vals, so no mask compression at all.</summary>
        NoMaskAndAllVals = 6
    }

    public static class CompressionUtil
    {
        /// <summary>
        /// Default compression flags used by OpenVDB archives if not otherwise specified.
        /// C++ OpenVDB often defaults to COMPRESS_ACTIVE_MASK, and adds COMPRESS_BLOSC or COMPRESS_ZIP
        /// if available and appropriate for the grid type.
        /// </summary>
        public const CompressionFlags DefaultCompressionFlags = CompressionFlags.ActiveMask; // Base default

        public static string ToString(CompressionFlags flags)
        {
            if (flags == CompressionFlags.None) return "None";
            
            var parts = new System.Collections.Generic.List<string>();
            if ((flags & CompressionFlags.Zip) != 0) parts.Add("Zip");
            if ((flags & CompressionFlags.ActiveMask) != 0) parts.Add("ActiveMask");
            if ((flags & CompressionFlags.Blosc) != 0) parts.Add("Blosc");
            
            return string.Join(" | ", parts);
        }
    }
}
