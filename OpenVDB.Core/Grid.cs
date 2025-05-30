// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using OpenVDB.Core.Metadata;
using OpenVDB.Core.Tree;
using OpenVDB.Math;

namespace OpenVDB.Core
{
    /// <summary>
    /// Generic grid class that associates a tree with a transform and metadata.
    /// Corresponds to openvdb::Grid<TreeType>.
    /// </summary>
    /// <typeparam name="TTree">The type of the tree structure, must implement ITree<TValue>.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the grid's voxels.</typeparam>
    public class Grid<TTree, TValue> : GridBase where TTree : class, ITree<TValue>, new() // Assuming new() for tree creation
    {
        private TTree _tree;

        // Type aliases (using properties for type information)
        public TTree Tree => _tree;
        public override ITree BaseTree => _tree;
        public TValue Background => _tree.BackgroundValue;
        
        public override string GridTypeName => _tree.TreeType; // Or a more specific grid name like "FloatGrid"
        public override string ValueTypeName => _tree.ValueTypeName;
        public override Type ValueType => typeof(TValue);

        // Constructors
        public Grid() : this(new TTree()) // Default tree with its default background
        {
        }

        public Grid(TValue background) : this(CreateTreeWithBackground(background))
        {
        }
        
        private static TTree CreateTreeWithBackground(TValue background)
        {
            // This is a bit of a hack due to lack of direct constructor with background in new TTree() constraint.
            // A real tree implementation would handle this better.
            var tree = new TTree();
            if (tree is Tree.Tree<TValue> placeholderTree) // Specific to placeholder
            {
                placeholderTree = new Tree.Tree<TValue>(background); // Re-create with background
                return (TTree)(object)placeholderTree; // This cast is risky and implies TTree is exactly Tree<TValue>
            }
            // If not placeholder, and TTree doesn't take background in `new()`, this is an issue.
            // For now, we assume the tree's default constructor is sufficient or background is set post-construction.
            // A factory pattern or more specific constraints on TTree would be better.
            // Or, Tree<TValue> should have a SetBackground method.
             if (!object.Equals(tree.BackgroundValue, background))
             {
                 Console.Error.WriteLine($"Warning: Tree for Grid created with default background, not specified '{background}'. Tree background: '{tree.BackgroundValue}'");
                 // Ideally, throw or have a tree.SetBackground(background)
             }
            return tree;
        }


        public Grid(TTree tree) : base()
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }
        
        // Internal constructor for copying
        private Grid(TTree tree, MetaMap meta, Transform transform) : base(meta, transform)
        {
             _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }
        
        // Copy constructor (deep copy of tree, metadata, transform)
        public Grid(Grid<TTree, TValue> other) : base(other) // Deep copies metadata & transform
        {
            _tree = (TTree)other._tree.Copy(); // Deep copy tree
        }

        // Constructor from GridBase (copies transform/metadata, new tree with default background)
        public Grid(GridBase otherGridBase) : base(otherGridBase)
        {
            _tree = new TTree(); // New tree with its default background.
                                 // If otherGridBase's tree background is desired, it needs to be fetched and used.
                                 // This requires BaseTree to expose BackgroundValue as object, then cast.
            if (otherGridBase.BaseTree.ValueType == typeof(TValue))
            {
                _tree = CreateTreeWithBackground((TValue)otherGridBase.BaseTree.BackgroundValue);
            }
            else
            {
                 Console.Error.WriteLine($"Warning: Grid created from GridBase of different ValueType. Using default background for new tree.");
            }
        }
        
        /// <summary>
        /// Shallow copy of tree, deep copy of metadata & transform.
        /// </summary>
        private Grid(Grid<TTree, TValue> other, bool shareTree)
            : base(other, shareTransform: true) // Share transform, deep copy metadata
        {
            if (shareTree)
            {
                _tree = other._tree;
            }
            else
            {
                _tree = (TTree)other._tree.Copy(); // Deep copy tree
            }
        }


        // Factory methods
        public static Grid<TTree, TValue> Create() => new Grid<TTree, TValue>();
        public static Grid<TTree, TValue> Create(TValue background) => new Grid<TTree, TValue>(background);
        public static Grid<TTree, TValue> Create(TTree tree) => new Grid<TTree, TValue>(tree);


        // GridBase overrides
        public override GridBase CopyGrid() => new Grid<TTree, TValue>(this, shareTree: true); // Shallow tree copy

        public override GridBase CopyGridWithNewTree()
        {
            var newGrid = new Grid<TTree, TValue>(this.BackgroundValue); // New tree with same background
            newGrid.Transform = this.Transform.Clone(); // Deep copy transform
            newGrid.UnionWith(this); // Deep copy metadata
            return newGrid;
        }

        public override GridBase DeepCopyGrid() => new Grid<TTree, TValue>(this); // Deep copy (copy constructor)

        public override void SetTree(ITree tree)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));
            if (tree is TTree specificTree)
            {
                _tree = specificTree;
            }
            else
            {
                throw new TypeErrorException($"Cannot assign tree of type '{tree.GetType().Name}' to grid with tree type '{typeof(TTree).Name}'.");
            }
        }

        public override void NewTree()
        {
            _tree = CreateTreeWithBackground(this.Background);
        }


        public override bool IsEmpty => _tree.IsEmpty;
        public override void Clear() => _tree.Clear();

        public override void PruneGrid(float tolerance = 0.0f)
        {
            // Convert float tolerance to TValue. This is tricky for generic TValue.
            // Assuming TValue can be compared with a tolerance concept.
            // For simplicity, if TValue is float or double, use it. Otherwise, this might need adjustment.
            if (typeof(TValue) == typeof(float))
                _tree.Prune((TValue)(object)tolerance);
            else if (typeof(TValue) == typeof(double))
                _tree.Prune((TValue)(object)(double)tolerance);
            else if (Convert.ChangeType(tolerance, typeof(TValue)) is TValue typedTolerance) // General numeric types
                 _tree.Prune(typedTolerance);
            else // For non-numeric or complex types, tolerance might mean something else or not apply.
                _tree.Prune(default(TValue)); // Or throw, or require specific IPrunable<TValue>
        }

        public override void Clip(CoordBBox bbox) => _tree.Clip(bbox);

        public override long ActiveVoxelCount() => _tree.ActiveVoxelCount();
        public override CoordBBox EvalActiveVoxelBoundingBox() => _tree.EvalActiveVoxelBoundingBox();
        public override bool EvalActiveVoxelDim(out Coord dim) => _tree.EvalActiveVoxelDim(out dim);
        public override long MemUsage() => base.Count * 24 + 128 + _tree.MemUsage(); // Rough estimate for dictionary + transform + tree

        // Voxel Access
        public ITreeValueAccessor<TValue> GetAccessor() => _tree.GetAccessor();
        // Add GetConstAccessor if ITree had it and a read-only accessor existed.

        // Grid-specific operations
        public void Fill(CoordBBox bbox, TValue value, bool activeState = true) => _tree.Fill(bbox, value, activeState);
        public void DenseFill(CoordBBox bbox, TValue value, bool activeState = true) => _tree.DenseFill(bbox, value, activeState);
        public void Merge(Grid<TTree, TValue> otherGrid, MergePolicy policy = MergePolicy.MergeActiveStates)
        {
            if (otherGrid == null) throw new ArgumentNullException(nameof(otherGrid));
            _tree.Merge(otherGrid.Tree, policy);
        }
        
        public void TopologyUnion<TOtherValue>(Grid<ITree<TOtherValue>, TOtherValue> otherGrid)
        {
             if (otherGrid == null) throw new ArgumentNullException(nameof(otherGrid));
            _tree.TopologyUnion(otherGrid.BaseTree as ITree<TOtherValue>);
        }
        public void TopologyIntersection<TOtherValue>(Grid<ITree<TOtherValue>, TOtherValue> otherGrid)
        {
             if (otherGrid == null) throw new ArgumentNullException(nameof(otherGrid));
            _tree.TopologyIntersection(otherGrid.BaseTree as ITree<TOtherValue>);
        }
        public void TopologyDifference<TOtherValue>(Grid<ITree<TOtherValue>, TOtherValue> otherGrid)
        {
             if (otherGrid == null) throw new ArgumentNullException(nameof(otherGrid));
            _tree.TopologyDifference(otherGrid.BaseTree as ITree<TOtherValue>);
        }


        // I/O (delegating to tree, using StreamMetadata)
        public override void ReadTopology(BinaryReader reader, StreamMetadata streamMetadata)
        {
            streamMetadata.HalfFloat = SaveFloatAsHalf; // Ensure tree knows if source data is half-float
            _tree.ReadTopology(reader, streamMetadata);
        }

        public override void WriteTopology(BinaryWriter writer, StreamMetadata streamMetadata)
        {
            streamMetadata.HalfFloat = SaveFloatAsHalf;
            _tree.WriteTopology(writer, streamMetadata);
        }

        public override void ReadBuffers(BinaryReader reader, StreamMetadata streamMetadata)
        {
            streamMetadata.HalfFloat = SaveFloatAsHalf;
            _tree.ReadBuffers(reader, streamMetadata);
        }

        // public override void ReadBuffers(BinaryReader reader, StreamMetadata streamMetadata, CoordBBox bbox)
        // {
        //     streamMetadata.HalfFloat = SaveFloatAsHalf;
        //     _tree.ReadBuffers(reader, streamMetadata, bbox); // Assuming Tree has this overload
        // }

        public override void WriteBuffers(BinaryWriter writer, StreamMetadata streamMetadata)
        {
            streamMetadata.HalfFloat = SaveFloatAsHalf;
            _tree.WriteBuffers(writer, streamMetadata);
        }
        
        public override void ReadNonresidentBuffers() => _tree.ReadNonresidentBuffers();

        public override void Print(TextWriter writer, int verboseLevel = 1)
        {
            writer.WriteLine($"Grid Type: {GridTypeName}, Value Type: {ValueTypeName}");
            writer.WriteLine($"Name: {Name}, Creator: {Creator}, Class: {GridClass}");
            writer.WriteLine($"Is In World Space: {IsInWorldSpace}, Vector Type: {VectorType}");
            writer.WriteLine($"Save Float As Half: {SaveFloatAsHalf}");
            
            writer.WriteLine("Metadata:");
            foreach(var metaPair in this) // Iterates MetaMap from GridBase
            {
                writer.WriteLine($"  {metaPair.Key}: {metaPair.Value.ValueAsString()} (Type: {metaPair.Value.TypeName})");
            }

            writer.WriteLine("Transform:");
            // Transform.Print needs to be implemented or use ToString()
            // For now:
            writer.WriteLine($"  {Transform.MapTypeName}"); // Placeholder for full transform print

            writer.WriteLine("Tree:");
            _tree.Print(writer, verboseLevel);
        }
        
        // Static registration (specific to this Grid<TTree, TValue> type)
        public static string StaticGridType => new TTree().TreeType; // Or a more robust way to get a static type name

        public static void Register()
        {
            GridBase.RegisterGrid(StaticGridType, () => new Grid<TTree, TValue>());
        }
        public static void Unregister()
        {
            // GridBase.UnregisterGrid(StaticGridType); // Needs UnregisterGrid in GridBase
        }
        public static bool IsGridRegistered() => GridBase.IsRegistered(StaticGridType);

    }

    // Common Grid type aliases (assuming placeholder Tree<TValue> is the concrete tree)
    // These would typically be defined where FloatTree, DoubleTree etc. are fully defined.
    // For now, using the generic Tree<TValue> placeholder.
    public using BoolGrid = Grid<Tree.Tree<bool>, bool>;
    public using FloatGrid = Grid<Tree.Tree<float>, float>;
    public using DoubleGrid = Grid<Tree.Tree<double>, double>;
    public using Int32Grid = Grid<Tree.Tree<int>, int>;
    public using Int64Grid = Grid<Tree.Tree<long>, long>;
    public using Vec3IGrid = Grid<Tree.Tree<Vec3<int>>, Vec3<int>>;
    public using Vec3SGrid = Grid<Tree.Tree<Vec3<float>>, Vec3<float>>;
    public using Vec3DGrid = Grid<Tree.Tree<Vec3<double>>, Vec3<double>>;
    // ... and so on for other common types.
}
