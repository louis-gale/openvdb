// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using OpenVDB.Math; // For Coord

namespace OpenVDB.Core.IO
{
    /// <summary>
    /// Stores descriptive information about a grid, particularly its location and
    /// properties within a VDB file.
    /// </summary>
    public class GridDescriptor
    {
        // Names
        public string Name { get; set; } // Name of the grid
        public string UniqueName { get; private set; } // Unique name for this descriptor (name + suffix if needed)
        public string GridType { get; set; } // Type name of the grid (e.g., "TreeFloatR5", "FloatGrid")

        // Instancing
        public string InstanceParentName { get; set; } // If non-empty, name of another grid that shares this grid's tree
        public string InstanceTransform { get; set; } // "share" or "copy" (or unique name of transform grid)

        // Grid properties
        public GridClass GridClass { get; set; } = GridClass.Unknown;
        public bool SaveFloatAsHalf { get; set; } = false;
        public bool IsInWorldSpace { get; set; } = true; // Default to true as per C++ GridBase constructor

        // Bounding box in file (typically index space)
        public Coord FileBBoxMin { get; set; } = Coord.Zero;
        public Coord FileBBoxMax { get; set; } = Coord.Zero;

        // Stream offsets (positions in the file)
        public long GridDescriptorOffset { get; set; } = 0; // Offset to this descriptor itself
        public long NameOffset { get; set; } = 0;
        public long MetaDataOffset { get; set; } = 0;
        public long TransformOffset { get; set; } = 0;
        public long TopologyOffset { get; set; } = 0; // Offset to the tree topology data
        public long BlocksOffset { get; set; } = 0;   // Offset to the data blocks (leaf node values)

        // Byte sizes of components
        public long TopologyByteSize { get; set; } = 0;
        public long BlocksByteSize { get; set; } = 0;

        // Legacy/compatibility fields (not explicitly in C++ GridDescriptor but implied by Read/Write logic)
        // C++ GridDescriptor has mGridPos, mBlockPos, mEndPos
        // mGridPos seems to be start of all grid data (metadata, transform, topology, blocks)
        // mBlockPos appears to be our BlocksOffset
        // mEndPos is start of next descriptor (or end of file marker)
        // For C# version, more granular offsets are defined above.
        // We can map mGridPos to the earliest of these, and mEndPos can be calculated.

        public GridDescriptor() : this("", "") { }

        public GridDescriptor(string name, string gridType, bool saveFloatAsHalf = false)
        {
            Name = name ?? "";
            GridType = gridType ?? "";
            UniqueName = Name; // Initially, unique name is the same as grid name
            SaveFloatAsHalf = saveFloatAsHalf;
            InstanceParentName = "";
            InstanceTransform = "copy"; // Default for non-instances or instances with own transform
        }

        public bool IsInstance() => !string.IsNullOrEmpty(InstanceParentName);

        /// <summary>
        /// True if this is an instance and its topology is shared with the parent.
        /// In VDB files, this often means the topologyOffset is 0 or points to the parent's topology.
        /// For simplicity here, we'll use the C++ heuristic of topologyOffset == 0.
        /// </summary>
        public bool IsTopologyInstance() => IsInstance() && TopologyOffset == 0;

        /// <summary>
        /// True if this is an instance and its transform is shared with the parent.
        /// </summary>
        public bool IsTransformInstance() => IsInstance() && InstanceTransform == "share";

        public void SetIsTransformInstance(bool on)
        {
            InstanceTransform = on ? "share" : "copy";
        }

        /// <summary>
        /// Appends a suffix to a name, used for creating unique names for instanced grids.
        /// Suffix is separated by an ASCII "record separator" (char 30).
        /// </summary>
        public static string AddSuffix(string name, int n) => $"{name}{(char)30}{n}";

        /// <summary>
        /// Strips a suffix (if any) from a unique name.
        /// </summary>
        public static string StripSuffix(string uniqueName)
        {
            int pos = uniqueName.LastIndexOf((char)30);
            return pos == -1 ? uniqueName : uniqueName.Substring(0, pos);
        }

        public void SetUniqueName(string name, int suffix)
        {
            Name = name;
            UniqueName = AddSuffix(name, suffix);
        }

        public void SetUniqueName(string uniqueName)
        {
            UniqueName = uniqueName;
            Name = StripSuffix(uniqueName);
        }


        /// <summary>
        /// Reads the grid descriptor from the stream.
        /// Handles different file versions.
        /// </summary>
        public void Read(BinaryReader reader, StreamMetadata streamMeta)
        {
            GridDescriptorOffset = reader.BaseStream.Position;

            if (streamMeta.FileVersion < VersionNumbers.FileVersionGridDescNameIsLast) // Legacy before name was last
            {
                Name = IoUtils.ReadString(reader);
                UniqueName = Name; // Older files didn't have separate unique name field
            }

            GridType = IoUtils.ReadString(reader);
            InstanceParentName = IoUtils.ReadString(reader);

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionGridInstancing)
            {
                if (streamMeta.FileVersion >= VersionNumbers.FileVersionGridSharedTransform)
                {
                    InstanceTransform = IoUtils.ReadString(reader);
                }
                else if (IsInstance()) // Older instancing implies shared transform
                {
                    InstanceTransform = "share";
                }
                else
                {
                    InstanceTransform = "copy";
                }
            }

            MetaDataOffset = reader.ReadInt64();
            TransformOffset = reader.ReadInt64();
            TopologyOffset = reader.ReadInt64();
            BlocksOffset = reader.ReadInt64();

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionGridDescNameIsLast)
            {
                NameOffset = reader.ReadInt64(); // Offset to where the name string is stored
                long currentPos = reader.BaseStream.Position;
                if (NameOffset > 0) // If name is stored elsewhere (e.g. shared name pool)
                {
                    reader.BaseStream.Seek(NameOffset, SeekOrigin.Begin);
                    Name = IoUtils.ReadString(reader);
                }
                else // Name is implicitly current string (should not happen with NameOffset > 0)
                {
                    Name = IoUtils.ReadString(reader);
                }
                UniqueName = Name; // Placeholder, C++ reads unique name if different from name.
                                   // For simplicity, assume Name is the UniqueName read here.
                                   // A full implementation would read mUniqueName separately if file version supports it.
                reader.BaseStream.Seek(currentPos, SeekOrigin.Begin); // Reset stream position
            }

            // Read grid properties that were added over time
            if (streamMeta.FileVersion >= VersionNumbers.FileVersionGridClass)
                GridClass = (GridClass)reader.ReadInt32();
            else
                GridClass = GridType.Contains("LevelSet") ? OpenVDB.GridClass.LevelSet : OpenVDB.GridClass.Unknown;


            if (streamMeta.FileVersion >= VersionNumbers.FileVersionSaveHalfFloat)
                SaveFloatAsHalf = reader.ReadBoolean();
            else
                SaveFloatAsHalf = false; // Default for older files

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionWorldSpaceBit)
                IsInWorldSpace = reader.ReadBoolean();
            else
                IsInWorldSpace = true; // Default for older files

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionTopologyStats)
            {
                TopologyByteSize = reader.ReadInt64();
                BlocksByteSize = reader.ReadInt64();
            }

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionFileBoundingBox)
            {
                FileBBoxMin = new Coord(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                FileBBoxMax = new Coord(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            }
        }

        /// <summary>
        /// Writes the grid descriptor to the stream.
        /// </summary>
        public void Write(BinaryWriter writer, StreamMetadata streamMeta)
        {
            // In modern VDB, Name is written last using NameOffset.
            // For simplicity in this placeholder, we write it first like older versions,
            // or assume NameOffset points to immediately following data.
            // A full implementation needs to handle the string table for names.

            // Placeholder for NameOffset: assume name is written inline or managed externally
            // For now, write name first for simplicity (matches legacy or non-offsetted name)
            IoUtils.WriteString(writer, Name);

            IoUtils.WriteString(writer, GridType);
            IoUtils.WriteString(writer, InstanceParentName ?? ""); // Ensure empty string if null

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionGridSharedTransform)
            {
                IoUtils.WriteString(writer, InstanceTransform ?? "copy");
            }

            writer.Write(MetaDataOffset);
            writer.Write(TransformOffset);
            writer.Write(TopologyOffset);
            writer.Write(BlocksOffset);

            // NameOffset would be written here if we were using a separate string table for names.
            // For this simplified version, we assume names are inlined or handled by the caller
            // setting the NameOffset to the position where WriteString(Name) occurred.
            // If Name is always written first (like legacy or simplified current), NameOffset might be 0 or point to start.
            writer.Write(NameOffset); // Placeholder value, actual calculation is complex.

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionGridClass)
                writer.Write((int)GridClass);

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionSaveHalfFloat)
                writer.Write(SaveFloatAsHalf);

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionWorldSpaceBit)
                writer.Write(IsInWorldSpace);

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionTopologyStats)
            {
                writer.Write(TopologyByteSize);
                writer.Write(BlocksByteSize);
            }

            if (streamMeta.FileVersion >= VersionNumbers.FileVersionFileBoundingBox)
            {
                writer.Write(FileBBoxMin.X); writer.Write(FileBBoxMin.Y); writer.Write(FileBBoxMin.Z);
                writer.Write(FileBBoxMax.X); writer.Write(FileBBoxMax.Y); writer.Write(FileBBoxMax.Z);
            }
        }

        public void Print(TextWriter writer, string indent = "")
        {
            writer.WriteLine($"{indent}GridDescriptor:");
            writer.WriteLine($"{indent}  Name: {Name} (Unique: {UniqueName})");
            writer.WriteLine($"{indent}  GridType: {GridType}");
            writer.WriteLine($"{indent}  IsInstance: {IsInstance()} {(IsInstance() ? $" (Parent: {InstanceParentName}, Transform: {InstanceTransform})" : "")}");
            writer.WriteLine($"{indent}  GridClass: {GridClass}");
            writer.WriteLine($"{indent}  SaveFloatAsHalf: {SaveFloatAsHalf}");
            writer.WriteLine($"{indent}  IsInWorldSpace: {IsInWorldSpace}");
            writer.WriteLine($"{indent}  FileBBox: [{FileBBoxMin}] -> [{FileBBoxMax}]");
            writer.WriteLine($"{indent}  Offsets (Desc:{GridDescriptorOffset}, Name:{NameOffset}, Meta:{MetaDataOffset}, Xform:{TransformOffset}, Topo:{TopologyOffset}, Blocks:{BlocksOffset})");
            writer.WriteLine($"{indent}  ByteSizes (Topo:{TopologyByteSize}, Blocks:{BlocksByteSize})");
        }
    }
}
