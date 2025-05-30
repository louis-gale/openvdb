// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

namespace OpenVDB.Core.IO
{
    /// <summary>
    /// Defines various OpenVDB file format version numbers.
    /// These correspond to OPENVDB_FILE_VERSION_* constants in C++ version.h.
    /// </summary>
    public static class VersionNumbers
    {
        // From openvdb/version.h (selected versions, not exhaustive)
        public const uint FileVersionGridDescNameIsLast = 219;
        public const uint FileVersionGridInstancing = 220; // Grid instancing (grid name is instance parent)
        public const uint FileVersionGridSharedTransform = 221; // Instanced grid transform is unique name
        public const uint FileVersionGridClass = 222; // GridClass metadata (level set, fog, etc.)
        public const uint FileVersionSaveHalfFloat = 223; // Quantize float data to 16-bit half on save
        public const uint FileVersionWorldSpaceBit = 223; // is_local_space metadata bit
        public const uint FileVersionTopologyStats = 228; // Leaf/node/topology byte counts in descriptor
        public const uint FileVersionFileBoundingBox = 230; // file_bbox_{min,max} metadata
        public const uint FileVersionNodeMaskCompression = 231; // COMPRESS_ACTIVE_MASK includes node-level metadata
        public const uint FileVersionBlosc = 232; // Blosc compression
        public const uint FileVersionPointPartitioner = 233; // PointPartitioner
        // ... Add other versions as needed by the porting process
        public const uint FileVersionMultiPassIo = 235; // Multi-pass I/O for certain leaf types
        public const uint FileVersionFloatFrustumBBox = 236; // NonlinearFrustumMap bbox is BBox<Vec3d>
    }
}
