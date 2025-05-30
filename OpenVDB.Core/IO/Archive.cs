// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenVDB.Core.Metadata;

namespace OpenVDB.Core.IO
{
    /// <summary>
    public class Archive
    {
        private uint _fileVersionRead; // Version of the file being read
        private ushort _libraryVersionMajorRead;
        private ushort _libraryVersionMinorRead;
        private string _uuidRead;
        private bool _inputHasGridOffsets; // True if the archive being read contains grid offsets
        
        // Settings for writing
        private bool _enableInstancing;
        private CompressionFlags _compressionFlags;
        private bool _enableGridStats;

        private List<GridDescriptor> _gridDescriptors = new List<GridDescriptor>();

        public uint FileVersionRead => _fileVersionRead;
        public ushort LibraryVersionMajorRead => _libraryVersionMajorRead;
        public ushort LibraryVersionMinorRead => _libraryVersionMinorRead;
        public string LibraryVersionStringRead => $"{_libraryVersionMajorRead}.{_libraryVersionMinorRead}";
        public string VersionStringRead => $"{LibraryVersionStringRead}/{_fileVersionRead}";
        public string UUIDRead => _uuidRead;

        public bool IsInstancingEnabled
        {
            get => _enableInstancing;
            set => _enableInstancing = value;
        }

        public CompressionFlags Compression
        {
            get => _compressionFlags;
            set => _compressionFlags = value;
        }

        public bool IsGridStatsMetadataEnabled
        {
            get => _enableGridStats;
            set => _enableGridStats = value;
        }
        
        public IReadOnlyList<GridDescriptor> GridDescriptors => _gridDescriptors.AsReadOnly();

        public Archive()
        {
            // Initialize read properties to sensible defaults or indicate they are not yet read
            _fileVersionRead = 0;
            _libraryVersionMajorRead = 0;
            _libraryVersionMinorRead = 0;
            _uuidRead = string.Empty;
            _inputHasGridOffsets = false; 
            
            // Default settings for writing
            _enableInstancing = true;
            _compressionFlags = CompressionUtil.DefaultCompressionFlags;
            _enableGridStats = true;
        }

        /// <summary>
        /// Copy constructor - copies WRITING settings and READ state.
        /// </summary>
        public Archive(Archive other)
        {
            _fileVersionRead = other._fileVersionRead;
            _libraryVersionMajorRead = other._libraryVersionMajorRead;
            _libraryVersionMinorRead = other._libraryVersionMinorRead;
            _uuidRead = other._uuidRead;
            _inputHasGridOffsets = other._inputHasGridOffsets;
            
            _enableInstancing = other._enableInstancing;
            _compressionFlags = other._compressionFlags;
            _enableGridStats = other._enableGridStats;
            
            // Deep copy descriptors if any
            _gridDescriptors = new List<GridDescriptor>(other._gridDescriptors.Select(d => new GridDescriptor(d)));
        }

        /// <summary>
        /// Creates a new Archive instance with the same settings as this one.
        /// Read state and descriptors are also copied.
        /// </summary>
        public Archive Clone() => new Archive(this);

        public string GetUniqueTag() => _uuidRead; // Or a separate UUID for writing operations? C++ uses one.
        public bool IsIdentical(string uuidStr) => _uuidRead.Equals(uuidStr, StringComparison.OrdinalIgnoreCase);
        
        public static bool HasBloscCompression() => false; 
        public static bool HasZLibCompression() => true;  

        /// <summary>
        /// Reads the VDB file header from the stream.
        /// Populates FileVersionRead, LibraryVersionRead, UUIDRead, and _inputHasGridOffsets.
        /// If grid offsets are present, it also reads the grid count and calls ReadGridDescriptors.
        /// </summary>
        public void ReadHeader(Stream stream)
        {
            if (!stream.CanRead) throw new ArgumentException("Stream is not readable.", nameof(stream));
            
            _gridDescriptors.Clear(); // Clear any previous descriptors

            using (var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true))
            {
                byte m0 = reader.ReadByte();
                byte m1 = reader.ReadByte();
                byte m2 = reader.ReadByte();
                byte m3 = reader.ReadByte();

                if (m0 != IoConstants.Magic0 || m1 != IoConstants.Magic1 || 
                    m2 != IoConstants.Magic2 || m3 != IoConstants.Magic3)
                {
                    throw new IOException("Invalid VDB magic number. Not a VDB file or corrupt.");
                }

                _fileVersionRead = reader.ReadUInt32();
                _libraryVersionMajorRead = reader.ReadUInt16();
                _libraryVersionMinorRead = reader.ReadUInt16();
                
                _inputHasGridOffsets = reader.ReadBoolean(); 

                char[] uuidChars = reader.ReadChars(36);
                _uuidRead = new string(uuidChars);

                if (_inputHasGridOffsets)
                {
                    // In VDB files, if hasGridOffsets is true, the number of grids (uint32_t)
                    // and then the offset to each GridDescriptor (uint64_t) follows.
                    // For now, we'll just read the grid count. The actual offsets would be used
                    // by a higher-level "File" class to seek and read descriptors.
                    // Here, we'll assume ReadGridDescriptors is called immediately after if needed.
                    uint gridCount = reader.ReadUInt32();
                    // Skip reading offsets for now, as ReadGridDescriptors will read them sequentially.
                    // If we were to use the offsets:
                    // List<long> offsets = new List<long>();
                    // for (int i = 0; i < gridCount; ++i) offsets.Add(reader.ReadInt64());
                    ReadGridDescriptors(stream, (int)gridCount);
                }
            }
        }

        /// <summary>
        /// Writes the VDB file header to the stream.
        /// The UUID used is the one from the last ReadHeader call, or a new one if never read.
        /// </summary>
        public void WriteHeader(Stream stream, int gridCount, bool hasGridOffsets)
        {
            if (!stream.CanWrite) throw new ArgumentException("Stream is not writable.", nameof(stream));

            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(IoConstants.Magic0);
                writer.Write(IoConstants.Magic1);
                writer.Write(IoConstants.Magic2);
                writer.Write(IoConstants.Magic3);

                writer.Write(IoConstants.CurrentFileVersion); 
                writer.Write(Version.MajorVersion);
                writer.Write(Version.MinorVersion);
                
                writer.Write(hasGridOffsets); 

                string uuidToWrite = string.IsNullOrEmpty(_uuidRead) ? System.Guid.NewGuid().ToString() : _uuidRead;
                if (uuidToWrite.Length != 36) uuidToWrite = System.Guid.NewGuid().ToString();
                writer.Write(uuidToWrite.ToCharArray());

                if (hasGridOffsets)
                {
                    writer.Write((uint)gridCount);
                    // Placeholder for actual grid descriptor offsets.
                    // These offsets are typically known only after all grid data is laid out.
                    // For now, writing zeros or placeholder values.
                    for (int i = 0; i < gridCount; ++i)
                    {
                        writer.Write((long)0); // Placeholder offset
                    }
                }
            }
        }

        /// <summary>
        /// Writes grids and global metadata to a stream.
        /// </summary>
        public virtual void Write(Stream stream, IEnumerable<GridBase> grids, MetaMap globalMetadata)
        {
            if (grids == null) throw new ArgumentNullException(nameof(grids));
            var gridList = grids.ToList();

            // Prepare StreamMetadata for this write operation
            var streamMetadata = new StreamMetadata
            {
                FileVersion = IoConstants.CurrentFileVersion, // Writing with current version
                LibraryVersionMajor = Version.MajorVersion, // Writing with current library version
                LibraryVersionMinor = Version.MinorVersion,
                Compression = this.Compression,
                WriteGridStats = this.IsGridStatsMetadataEnabled,
                IsSeekable = stream.CanSeek
            };
            
            // This is a simplified Write sequence. A full implementation is more complex.
            // 1. Write Header (potentially with placeholder offsets if not seekable, or come back later)
            bool hasGridOffsets = _enableInstancing && gridList.Count > 1;
            WriteHeader(stream, gridList.Count, hasGridOffsets);
            long headerEndPosition = stream.Position;

            // 2. Write Global Metadata
            if (globalMetadata != null && globalMetadata.Count > 0)
            {
                IoUtils.WriteMetaMap(new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true), globalMetadata, streamMetadata);
            }
            long globalMetadataEndPosition = stream.Position;

            // 3. Prepare Grid Descriptors (but don't write them yet if offsets are unknown)
            var descriptors = new List<GridDescriptor>();
            foreach (var grid in gridList)
            {
                var desc = new GridDescriptor(grid.Name, grid.GridTypeName, grid.SaveFloatAsHalf);
                // Populate other descriptor fields from grid (GridClass, IsInWorldSpace, etc.)
                desc.GridClass = grid.GridClass;
                desc.IsInWorldSpace = grid.IsInWorldSpace;
                // Offsets will be filled in as data is written
                descriptors.Add(desc);
            }

            // 4. Write Grid Data (Metadata, Transform, Topology, Blocks) for each grid
            //    and record their offsets in the descriptors.
            //    This part is highly complex and involves seeking if stream is seekable,
            //    or careful sequential writing.
            //    For now, this is a major placeholder.
            Console.WriteLine($"Placeholder: Writing {gridList.Count} grids.");
            // foreach (var grid in gridList) { /* ... write grid data ... */ }


            // 5. If hasGridOffsets and stream is seekable, go back and write the actual descriptor offsets.
            //    If not seekable, offsets must have been pre-calculated or written sequentially.
            //    For now, we assume sequential writing for descriptors if written here.
            if (hasGridOffsets && stream.CanSeek)
            {
                // This part is complex: would need to store current stream pos,
                // write all grid data, then come back to write the descriptor offsets table
                // that was placeholder-written in WriteHeader.
                // Or, write descriptors after all grid data and store a pointer to the descriptor block.
                Console.WriteLine("Placeholder: Grid descriptor offset table would be updated if seekable.");
            }
            
            // 6. Write Grid Descriptors now (if not written via an offset table earlier)
            //    This is simplified; VDB format might put descriptors before grid data,
            //    requiring either seeking or knowing all sizes beforehand.
            WriteGridDescriptors(stream, descriptors, streamMetadata);
        }

        /// <summary>
        /// Reads grid descriptors from the stream.
        /// </summary>
        public void ReadGridDescriptors(Stream stream, int gridCount)
        {
            if (!stream.CanRead) throw new ArgumentException("Stream is not readable.", nameof(stream));
            _gridDescriptors.Clear();
            
            var streamMetadata = new StreamMetadata // Create a relevant StreamMetadata for reading descriptors
            {
                FileVersion = _fileVersionRead,
                LibraryVersionMajor = _libraryVersionMajorRead,
                LibraryVersionMinor = _libraryVersionMinorRead,
                IsSeekable = stream.CanSeek
                // Other settings might be relevant depending on descriptor content
            };

            using (var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true))
            {
                for (int i = 0; i < gridCount; ++i)
                {
                    var descriptor = new GridDescriptor();
                    descriptor.Read(reader, streamMetadata);
                    _gridDescriptors.Add(descriptor);
                }
            }
        }
        
        /// <summary>
        /// Writes the provided list of grid descriptors to the stream.
        /// </summary>
        public void WriteGridDescriptors(Stream stream, List<GridDescriptor> descriptors, StreamMetadata streamMeta)
        {
            if (!stream.CanWrite) throw new ArgumentException("Stream is not writable.", nameof(stream));
            if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));

            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
            {
                foreach (var descriptor in descriptors)
                {
                    descriptor.Write(writer, streamMeta);
                }
            }
        }


        /// <summary>
        /// Placeholder for reading a single grid based on its descriptor.
        /// </summary>
        public virtual GridBase ReadGrid(Stream stream, GridDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            
            var streamMetadata = new StreamMetadata // Create StreamMetadata for this grid
            {
                FileVersion = _fileVersionRead,
                LibraryVersionMajor = _libraryVersionMajorRead,
                LibraryVersionMinor = _libraryVersionMinorRead,
                Compression = this.Compression, // Use archive's current compression for reading this grid
                GridClass = descriptor.GridClass,
                HalfFloat = descriptor.SaveFloatAsHalf,
                IsSeekable = stream.CanSeek
                // TODO: SetBackgroundValue from grid's metadata if stored, or from descriptor
            };

            // Seek to descriptor.GridDescriptorOffset + offset to actual grid data (e.g. metadata / transform / tree)
            // Create grid instance using GridBase.CreateGrid(descriptor.GridType).
            // Read grid metadata.
            // Read transform.
            // Read tree topology and buffers.
            Console.WriteLine($"Placeholder: Reading grid '{descriptor.Name}' of type '{descriptor.GridType}'.");
            // Actual implementation is complex and involves creating the right grid type,
            // seeking to various offsets specified in the descriptor, and deserializing components.
            return null; 
        }

        /// <summary>
        /// Writes a single grid's data (metadata, transform, topology, buffers) to the stream.
        /// Updates the provided descriptor with actual file offsets and byte sizes.
        /// </summary>
        private void WriteGrid(GridBase grid, GridDescriptor descriptor, BinaryWriter writer, StreamMetadata streamMetadata, bool isInstanceRoot)
        {
            // Ensure grid name in descriptor matches grid, or update descriptor.
            // This might have been set when descriptors were first prepared.
            if (string.IsNullOrEmpty(descriptor.Name)) descriptor.Name = grid.Name;
            descriptor.GridType = grid.GridTypeName; // Ensure type is correct

            // 1. Write Grid Metadata
            descriptor.MetaDataOffset = writer.BaseStream.Position;
            grid.Write(writer, streamMetadata); // Assumes MetaMap (base of GridBase) has Write method
            
            // 2. Write Grid Transform
            descriptor.TransformOffset = writer.BaseStream.Position;
            grid.Transform.Write(writer, streamMetadata);

            // 3. Write Tree Topology
            if (!descriptor.IsTopologyInstance() || isInstanceRoot)
            {
                descriptor.TopologyOffset = writer.BaseStream.Position;
                grid.WriteTopology(writer, streamMetadata); // GridBase -> Grid -> Tree
                descriptor.TopologyByteSize = writer.BaseStream.Position - descriptor.TopologyOffset;
            }
            else // Is a topology instance and not the root of instance chain
            {
                descriptor.TopologyOffset = 0; // Or point to parent's topology offset if known
                descriptor.TopologyByteSize = 0;
            }

            // 4. Write Tree Buffers (Voxel Data)
            // C++ has IsDataInstance. For now, assume data is written if topology is written.
            // A more complete IsDataInstance would check if BlocksOffset should be from parent.
            bool writeData = (!descriptor.IsTopologyInstance() || isInstanceRoot); 
            if (writeData)
            {
                descriptor.BlocksOffset = writer.BaseStream.Position;
                grid.WriteBuffers(writer, streamMetadata); // GridBase -> Grid -> Tree
                descriptor.BlocksByteSize = writer.BaseStream.Position - descriptor.BlocksOffset;
            }
            else
            {
                descriptor.BlocksOffset = 0; // Or point to parent's blocks offset
                descriptor.BlocksByteSize = 0;
            }
            
            // Descriptor itself doesn't store its end position; it's implicit or handled by Archive writing sequence.
        }
        
        public override void Write(Stream stream, IEnumerable<GridBase> grids, MetaMap globalMetadata)
        {
            if (grids == null) throw new ArgumentNullException(nameof(grids));
            var gridList = grids.ToList();
            if (!gridList.Any()) // No grids to write
            {
                WriteHeader(stream, 0, false); // Write header for empty file
                if (globalMetadata != null && globalMetadata.Count > 0)
                {
                     // Still need streamMetadata for WriteMetaMap
                    var streamMetaForGlobal = new StreamMetadata { FileVersion = IoConstants.CurrentFileVersion };
                    IoUtils.WriteMetaMap(new BinaryWriter(stream, Encoding.ASCII, true), globalMetadata, streamMetaForGlobal);
                }
                return;
            }

            // Prepare initial descriptors (offsets will be updated)
            var descriptors = new List<GridDescriptor>();
            var uniqueGridNames = new Dictionary<string, int>();
            foreach (var grid in gridList)
            {
                string gridName = grid.Name;
                if (string.IsNullOrEmpty(gridName)) gridName = "grid"; // Default name if empty

                int suffix = 0;
                string uniqueName = gridName;
                if (uniqueGridNames.TryGetValue(gridName, out int count))
                {
                    suffix = count + 1;
                    uniqueName = GridDescriptor.AddSuffix(gridName, suffix);
                }
                uniqueGridNames[gridName] = suffix;

                var desc = new GridDescriptor(gridName, grid.GridTypeName, grid.SaveFloatAsHalf);
                desc.SetUniqueName(uniqueName); // Set unique name based on suffix
                desc.GridClass = grid.GridClass;
                desc.IsInWorldSpace = grid.IsInWorldSpace;
                // TODO: Handle instancing detection here to set desc.InstanceParentName and desc.InstanceTransform
                descriptors.Add(desc);
            }
            
            // Determine if grid offsets table is needed
            bool hasGridOffsets = _enableInstancing && gridList.Count > 0; // Simplified: always true if instancing and grids exist
                                                                          // C++ logic is more complex, depends on actual instances

            // Write Header (potentially with placeholder offsets for descriptors)
            WriteHeader(stream, descriptors.Count, hasGridOffsets);
            long gridDescriptorTableOffset = stream.Position; // Position after header (and after grid count/offsets if written)
            
            // If hasGridOffsets, the offsets written by WriteHeader are placeholders.
            // We will need to come back and update them if the stream is seekable.

            var streamMetadata = new StreamMetadata
            {
                FileVersion = IoConstants.CurrentFileVersion,
                LibraryVersionMajor = Version.MajorVersion,
                LibraryVersionMinor = Version.MinorVersion,
                Compression = this.Compression,
                WriteGridStats = this.IsGridStatsMetadataEnabled,
                IsSeekable = stream.CanSeek
            };

            // Write Global Metadata
            long globalMetadataStartOffset = stream.Position;
            if (globalMetadata != null && globalMetadata.Count > 0)
            {
                IoUtils.WriteMetaMap(new BinaryWriter(stream, Encoding.ASCII, true), globalMetadata, streamMetadata);
            }
            // else: write 0 for metadata count if required by format for "no metadata"

            // Write Grid Data and update descriptor offsets
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                for (int i = 0; i < gridList.Count; ++i)
                {
                    var grid = gridList[i];
                    var descriptor = descriptors[i];
                    
                    // Update descriptor with its own starting position IF descriptors are written sequentially after global metadata
                    // This depends on the final VDB structure being targeted.
                    // For now, assume descriptors are written later in a block.
                    // The WriteGrid method will populate offsets relative to start of grid data.
                    
                    // TODO: Determine isInstanceRoot properly. For now, treat all as roots if not instancing.
                    bool isInstanceRoot = true; // Simplified for now
                    if (descriptor.IsInstance())
                    {
                        // Logic to check if this is the first occurrence of this instance's source.
                        // For now, this is simplified.
                    }

                    // Set per-grid stream metadata before writing grid components
                    streamMetadata.GridClass = grid.GridClass; // From GridBase property
                    streamMetadata.HalfFloat = grid.SaveFloatAsHalf; // From GridBase property
                    streamMetadata.SetBackgroundValue(grid.BaseTree.BackgroundValue); // Get object background

                    WriteGrid(grid, descriptor, writer, streamMetadata, isInstanceRoot);
                }
            }

            // Write actual Grid Descriptors
            long finalDescriptorsOffset = stream.Position;
            WriteGridDescriptors(stream, descriptors, streamMetadata);

            // If seekable and hasGridOffsets, go back and update the offset table in the header.
            // The header has: magic, versions, uuid, hasGridOffsets(bool), gridCount(uint32), offsets_table(gridCount * uint64)
            // Offset to gridCount is IoConstants.BaseHeaderSize - 1 (for bool) - 36 (uuid) - 2(minor) - 2(major) -4(fileVer) -4(magic)
            // = IoConstants.BaseHeaderSize - 49 = after hasGridOffsets flag.
            // This part is complex and requires careful offset calculation.
            if (hasGridOffsets && stream.CanSeek)
            {
                // This is a placeholder for the logic to update the offset table.
                // It would involve:
                // 1. Storing the start position of each GridDescriptor block when it was written (if not sequential after global meta).
                // 2. Seeking back to the header's offset table.
                // 3. Writing the actual offsets.
                // For now, the WriteHeader wrote placeholder 0s.
                // If descriptors are written last (as in current code), then their offsets are known.
                // The "offset table" in VDB header usually points to the *start* of each descriptor in the descriptor block.
                // If descriptors are written sequentially in a block starting at `finalDescriptorsOffset`:
                long currentDescriptorOffsetInTable = finalDescriptorsOffset;
                long positionToUpdateOffsets = IoConstants.BaseHeaderSize; // Position after hasGridOffsets flag and gridCount

                stream.Seek(positionToUpdateOffsets, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    foreach(var desc in descriptors)
                    {
                        // The offset stored in the header table should be the absolute offset to the descriptor.
                        // This requires knowing where each descriptor WILL BE written.
                        // If `WriteGridDescriptors` writes them sequentially starting at `finalDescriptorsOffset`:
                        writer.Write(currentDescriptorOffsetInTable); 
                        // This isn't quite right: `desc.GridDescriptorOffset` should be this value.
                        // The C++ `Archive::write` calculates these offsets *before* writing grid data,
                        // then writes descriptors, then grid data. This requires knowing sizes or multiple passes.
                        // For simplicity here, we write descriptors last, so their offsets are known.
                        // The header table pointing to these would need `finalDescriptorsOffset` + cumulative size of previous descriptors.
                        // This simplified version just writes placeholder 0s in WriteHeader for the table.
                        // A full implementation needs to manage this offset table correctly.
                    }
                }
                stream.Seek(0, SeekOrigin.End); // Go back to the end of the stream
            }
        }
    }
}
