// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using OpenVDB.Core.Metadata;
using OpenVDB.Core.Tree;
using OpenVDB.Math;

namespace OpenVDB.Core
{
    /// <summary>
    /// Abstract base class for all typed grids.
    /// Corresponds to openvdb::GridBase.
    /// </summary>
    public abstract class GridBase : MetaMap
    {
        private static readonly Dictionary<string, Func<GridBase>> _gridRegistry = new Dictionary<string, Func<GridBase>>();
        private Transform _transform;

        // Static members for grid registry
        public static void RegisterGrid(string typeName, Func<GridBase> factory)
        {
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _gridRegistry[typeName] = factory;
        }

        public static bool IsRegistered(string typeName) => _gridRegistry.ContainsKey(typeName);

        public static GridBase CreateGrid(string typeName)
        {
            if (!_gridRegistry.TryGetValue(typeName, out Func<GridBase> factory))
                throw new KeyErrorException($"Grid type '{typeName}' not registered.");
            return factory();
        }
        public static void ClearRegistry() => _gridRegistry.Clear();


        // Constructors
        protected GridBase()
        {
            _transform = Transform.CreateLinearTransform(1.0); // Default identity-like transform
        }

        protected GridBase(MetaMap meta, Transform transform) : base(meta) // Copy metadata
        {
            _transform = transform?.Clone() ?? throw new ArgumentNullException(nameof(transform));
        }

        protected GridBase(GridBase other) : base(other) // Copy MetaMap part (deep copy of metadata)
        {
            _transform = other._transform.Clone(); // Deep copy transform
        }
        
        /// <summary>
        /// Initializes a new grid, sharing the transform of the other grid
        /// but deep-copying metadata. Used by Grid<TTree>(Grid, ShallowCopy)
        /// </summary>
        protected GridBase(GridBase other, bool shareTransform) : base(other)
        {
            _transform = shareTransform ? other._transform : other._transform.Clone();
        }


        // Abstract methods and properties to be implemented by Grid<TTree>
        public abstract GridBase CopyGrid(); // Shallow copy of tree, deep copy of metadata & transform
        public abstract GridBase CopyGridWithNewTree(); // New tree, deep copy of metadata & transform
        public abstract GridBase DeepCopyGrid(); // Deep copy of tree, metadata & transform

        public abstract string GridTypeName { get; } // Name of the specific grid type (e.g., "FloatGrid")
        public abstract string ValueTypeName { get; } // Name of the voxel value type (e.g., "float")
        public abstract Type ValueType { get; } // System.Type of the voxel value

        public abstract ITree BaseTree { get; } // Access to the non-generic tree
        public abstract void SetTree(ITree tree); // Sets the tree, type checking occurs in Grid<TTree>
        public abstract void NewTree(); // Creates a new tree with current background value

        public abstract bool IsEmpty { get; }
        public abstract void Clear(); // Clears tree to background value

        public abstract void PruneGrid(float tolerance = 0.0f);
        public abstract void Clip(CoordBBox bbox);

        public abstract long ActiveVoxelCount();
        public abstract CoordBBox EvalActiveVoxelBoundingBox();
        public abstract bool EvalActiveVoxelDim(out Coord dim);
        public abstract long MemUsage();
        
        // Abstract I/O methods (to be implemented in Grid<TTree>)
        public abstract void ReadTopology(BinaryReader reader, StreamMetadata streamMetadata);
        public abstract void WriteTopology(BinaryWriter writer, StreamMetadata streamMetadata);
        public abstract void ReadBuffers(BinaryReader reader, StreamMetadata streamMetadata);
        // public abstract void ReadBuffers(BinaryReader reader, StreamMetadata streamMetadata, CoordBBox bbox); // For partial reads
        public abstract void WriteBuffers(BinaryWriter writer, StreamMetadata streamMetadata);
        public abstract void ReadNonresidentBuffers();


        public abstract void Print(TextWriter writer, int verboseLevel = 1);


        // Non-abstract methods (properties and methods using metadata or transform)

        public string Name
        {
            get => GetValue<string>(GridBaseMetadataKeys.GridName, "");
            set => Insert(GridBaseMetadataKeys.GridName, value);
        }

        public string Creator
        {
            get => GetValue<string>(GridBaseMetadataKeys.GridCreator, "");
            set => Insert(GridBaseMetadataKeys.GridCreator, value);
        }

        public bool SaveFloatAsHalf
        {
            get => GetValue<bool>(GridBaseMetadataKeys.SaveHalfFloat, false);
            set => Insert(GridBaseMetadataKeys.SaveHalfFloat, value);
        }

        public GridClass GridClass
        {
            get => (GridClass)GetValue<int>(GridBaseMetadataKeys.GridClass, (int)OpenVDB.GridClass.Unknown);
            set => Insert(GridBaseMetadataKeys.GridClass, (int)value);
        }
        public void ClearGridClass() => Remove(GridBaseMetadataKeys.GridClass);


        public VecType VectorType
        {
            get => (VecType)GetValue<int>(GridBaseMetadataKeys.VectorType, (int)OpenVDB.VecType.Invariant);
            set => Insert(GridBaseMetadataKeys.VectorType, (int)value);
        }
        public void ClearVectorType() => Remove(GridBaseMetadataKeys.VectorType);


        public bool IsInWorldSpace
        {
            get => GetValue<bool>(GridBaseMetadataKeys.IsLocalSpace, false) == false; // Note: C++ stores IsLocalSpace
            set => Insert(GridBaseMetadataKeys.IsLocalSpace, !value);
        }

        public Transform Transform
        {
            get => _transform;
            set => _transform = value?.Clone() ?? throw new ArgumentNullException(nameof(value));
        }
        
        public void SetTransform(Transform transform) => Transform = transform; // Alias for property setter


        public Vec3<double> VoxelSize() => _transform.VoxelSize();
        public Vec3<double> VoxelSize(Vec3<double> indexSpacePos) => _transform.VoxelSize(indexSpacePos);
        public bool HasUniformVoxels() => _transform.HasUniformScale;

        public Vec3<double> IndexToWorld(Vec3<double> indexPoint) => _transform.IndexToWorld(indexPoint);
        public Vec3<double> IndexToWorld(Coord indexCoord) => _transform.IndexToWorld(indexCoord);
        public Vec3<double> WorldToIndex(Vec3<double> worldPoint) => _transform.WorldToIndex(worldPoint);
        public Coord WorldToIndexCellCentered(Vec3<double> worldPoint) => _transform.WorldToIndexCellCentered(worldPoint);
        public Coord WorldToIndexNodeCentered(Vec3<double> worldPoint) => _transform.WorldToIndexNodeCentered(worldPoint);
        
        public BBox<Vec3<double>, double> IndexToWorld(BBox<Coord, int> indexBBox) => _transform.IndexToWorld(indexBBox);
        public BBox<Vec3<double>, double> IndexToWorld(BBox<Vec3<double>, double> indexBBox) => _transform.IndexToWorld(indexBBox);
        public BBox<Vec3<double>, double> WorldToIndex(BBox<Vec3<double>, double> worldBBox) => _transform.WorldToIndex(worldBBox);
        public CoordBBox WorldToIndexCellCentered(BBox<Vec3<double>, double> worldBBox) => _transform.WorldToIndexCellCentered(worldBBox);
        public CoordBBox WorldToIndexNodeCentered(BBox<Vec3<double>, double> worldBBox) => _transform.WorldToIndexNodeCentered(worldBBox);

        public void AddStatsMetadata()
        {
            Insert(GridBaseMetadataKeys.FileVoxelCount, new Int64Metadata(ActiveVoxelCount()));
            CoordBBox bbox = EvalActiveVoxelBoundingBox();
            Insert(GridBaseMetadataKeys.FileBBoxMin, new Vec3IMetadata(bbox.Min.ToVec3i()));
            Insert(GridBaseMetadataKeys.FileBBoxMax, new Vec3IMetadata(bbox.Max.ToVec3i()));
            Insert(GridBaseMetadataKeys.FileMemBytes, new Int64Metadata(MemUsage()));
        }

        public MetaMap GetStatsMetadata()
        {
            var stats = new MetaMap();
            string[] statKeys = {
                GridBaseMetadataKeys.FileVoxelCount, GridBaseMetadataKeys.FileBBoxMin,
                GridBaseMetadataKeys.FileBBoxMax, GridBaseMetadataKeys.FileMemBytes
            };
            foreach (string key in statKeys)
            {
                var meta = GetMetadata(key);
                if (meta != null)
                {
                    stats.Insert(key, meta);
                }
            }
            return stats;
        }
        
        public void ClipGrid(BBox<Vec3<double>, double> worldSpaceBBox)
        {
            // Convert world-space BBox to index-space CoordBBox
            // This needs to handle non-linear transforms appropriately.
            // For linear, worldToIndex(bbox) then convert to CoordBBox.
            // For non-linear, this is more complex (sample points, find min/max index coords).
            // The C++ version applies clip to the tree using an index-space CoordBBox.
            // We assume worldToIndex for BBox handles non-linearity by sampling.
            BBox<Vec3<double>, double> indexSpaceFloatBBox = WorldToIndex(worldSpaceBBox);
            
            // Convert floating point index-space BBox to integer CoordBBox.
            // The conversion rule depends on how voxels are centered/aligned.
            // Usually, for cell-centered, you'd round. For node-centered, floor/ceil.
            // Let's assume cell-centered for a general clip.
            CoordBBox indexSpaceCoordBBox = new CoordBBox(
                Coord.Floor(indexSpaceFloatBBox.Min), // Conservative: floor min
                Coord.Ceil(indexSpaceFloatBBox.Max)   // Conservative: ceil max
            );
            
            Clip(indexSpaceCoordBBox);
        }


        // Static helper methods from C++ Grid.h
        public static string GridClassToString(GridClass gc)
        {
            switch (gc)
            {
                case OpenVDB.GridClass.LevelSet: return "LevelSetGrid";
                case OpenVDB.GridClass.FogVolume: return "FogGrid";
                case OpenVDB.GridClass.Staggered: return "StaggeredGrid";
                default: return "UnknownGrid";
            }
        }
        
        // Other static helpers like stringToGridClass, vecTypeToString etc. can be added here.
    }

    /// <summary>
    /// Standard metadata field names.
    /// </summary>
    public static class GridBaseMetadataKeys
    {
        public const string GridName = "grid_name";
        public const string GridCreator = "grid_creator";
        public const string GridClass = "grid_class"; // Stores GridClass enum as string or int
        public const string SaveHalfFloat = "save_half_float";
        public const string IsLocalSpace = "is_local_space"; // If true, grid is in local space
        public const string VectorType = "vector_type"; // Stores VecType enum as string or int

        // Statistics metadata (usually added by AddStatsMetadata)
        public const string FileBBoxMin = "file_bbox_min"; // Vec3i
        public const string FileBBoxMax = "file_bbox_max"; // Vec3i
        public const string FileCompression = "file_compression"; // String (e.g., "Zip", "Blosc")
        public const string FileMemBytes = "file_mem_bytes"; // Int64
        public const string FileVoxelCount = "file_voxel_count"; // Int64
        public const string FileDelayedLoad = "file_delayed_load"; // Bool
    }
}
