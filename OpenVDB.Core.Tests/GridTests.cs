// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core;
using OpenVDB.Core.Tree; // For placeholder Tree
using OpenVDB.Core.Metadata;
using OpenVDB.Math;

namespace OpenVDB.Core.Tests
{
    [TestFixture]
    public class GridTests
    {
        // Use FloatGrid (which uses Tree<float>) for most tests as a concrete implementation
        private FloatGrid _grid;

        [SetUp]
        public void SetUp()
        {
            // Register the FloatGrid type for factory creation tests
            // This assumes FloatGrid (and its underlying Tree<float>) has a parameterless constructor
            // and uses a static registration method as defined in the ported Grid.cs
            if (!GridBase.IsRegistered(new Tree.Tree<float>().TreeType)) // Using placeholder tree's type name
            {
                 FloatGrid.Register();
            }
            _grid = new FloatGrid();
        }

        [TearDown]
        public void TearDown()
        {
            _grid = null;
            GridBase.ClearRegistry(); // Clean up registry after tests
        }

        [Test]
        public void Grid_DefaultConstructor_ShouldInitialize()
        {
            Assert.IsNotNull(_grid);
            Assert.IsNotNull(_grid.Transform);
            Assert.IsNotNull(_grid.Tree);
            Assert.AreEqual(0.0f, _grid.Background); // Default background for Tree<float> placeholder
        }

        [Test]
        public void Grid_ConstructorWithBackground_ShouldSetBackground()
        {
            var gridWithBg = new FloatGrid(123.45f);
            Assert.AreEqual(123.45f, gridWithBg.Background);
        }

        [Test]
        public void Grid_ConstructorWithTree_ShouldUseGivenTree()
        {
            var tree = new Tree.Tree<float>(77.0f);
            var gridWithTree = new FloatGrid(tree);
            Assert.AreSame(tree, gridWithTree.Tree);
            Assert.AreEqual(77.0f, gridWithTree.Background);
        }

        [Test]
        public void Grid_StaticCreate_Methods_ShouldWork()
        {
            var g1 = FloatGrid.Create();
            Assert.IsNotNull(g1);
            Assert.AreEqual(0.0f, g1.Background);

            var g2 = FloatGrid.Create(10.0f);
            Assert.IsNotNull(g2);
            Assert.AreEqual(10.0f, g2.Background);

            var tree = new Tree.Tree<float>(20.0f);
            var g3 = FloatGrid.Create(tree);
            Assert.IsNotNull(g3);
            Assert.AreSame(tree, g3.Tree);
        }


        [Test]
        public void GridBase_MetadataProperties_ShouldSetAndGet()
        {
            _grid.Name = "TestFloatGrid";
            Assert.AreEqual("TestFloatGrid", _grid.Name);

            _grid.Creator = "NUnit";
            Assert.AreEqual("NUnit", _grid.Creator);

            _grid.GridClass = GridClass.FogVolume;
            Assert.AreEqual(GridClass.FogVolume, _grid.GridClass);

            _grid.SaveFloatAsHalf = true;
            Assert.IsTrue(_grid.SaveFloatAsHalf);

            _grid.VectorType = VecType.Covariant;
            Assert.AreEqual(VecType.Covariant, _grid.VectorType);

            _grid.IsInWorldSpace = false; // Set to local space
            Assert.IsFalse(_grid.IsInWorldSpace);
        }

        [Test]
        public void GridBase_TransformProperty_ShouldSetAndGet()
        {
            var newTransform = Transform.CreateScaleTransform(new Vec3<double>(2.0, 2.0, 2.0));
            _grid.Transform = newTransform;

            Assert.IsNotNull(_grid.Transform);
            Assert.AreNotSame(newTransform, _grid.Transform, "Transform should be cloned on set.");
            Assert.IsTrue(_grid.Transform.GetMap().ToAffineMap().Matrix.IsApproxEqual(newTransform.GetMap().ToAffineMap().Matrix, 1e-9));
            Assert.AreEqual(new Vec3<double>(2,2,2), _grid.VoxelSize());
        }

        [Test]
        public void GridBase_IndexWorldConversions_ShouldUseTransform()
        {
            _grid.Transform = Transform.CreateScaleTranslateTransform(
                new Vec3<double>(2.0, 2.0, 2.0),
                new Vec3<double>(10, 20, 30));

            var indexPt = new Vec3<double>(1, 1, 1);
            var expectedWorldPt = new Vec3<double>(1*2+10, 1*2+20, 1*2+30); // (12, 22, 32)

            Assert.IsTrue(expectedWorldPt.IsApproxEqual(_grid.IndexToWorld(indexPt), 1e-9));
            Assert.IsTrue(indexPt.IsApproxEqual(_grid.WorldToIndex(expectedWorldPt), 1e-9));
        }

        [Test]
        public void GridBase_AddStatsMetadata_ShouldAddRelevantMetadata()
        {
            // These rely on placeholder tree stats, so values might be default/zero
            _grid.AddStatsMetadata();
            Assert.IsTrue(_grid.HasMetadata(GridBaseMetadataKeys.FileVoxelCount));
            Assert.IsTrue(_grid.HasMetadata(GridBaseMetadataKeys.FileBBoxMin));
            Assert.IsTrue(_grid.HasMetadata(GridBaseMetadataKeys.FileBBoxMax));
            Assert.IsTrue(_grid.HasMetadata(GridBaseMetadataKeys.FileMemBytes));

            Assert.AreEqual(0L, _grid.GetValue<long>(GridBaseMetadataKeys.FileVoxelCount, -1L));
        }

        [Test]
        public void Grid_CopyMethods_ShouldBehaveAsExpected()
        {
            _grid.Name = "OriginalGrid";
            _grid.Transform = Transform.CreateTranslationTransform(new Vec3<double>(5,0,0));

            // CopyGrid (shares tree, copies metadata & transform)
            var shallowTreeCopy = (FloatGrid)_grid.CopyGrid();
            Assert.AreEqual("OriginalGrid", shallowTreeCopy.Name);
            Assert.AreSame(_grid.Tree, shallowTreeCopy.Tree, "CopyGrid should share the tree.");
            Assert.AreNotSame(_grid.Transform, shallowTreeCopy.Transform, "CopyGrid should copy the transform.");
            Assert.IsTrue(_grid.Transform.GetMap().ToAffineMap().Matrix.IsApproxEqual(shallowTreeCopy.Transform.GetMap().ToAffineMap().Matrix, 1e-9));

            // DeepCopyGrid (copies tree, metadata, transform)
            var deepCopiedGrid = (FloatGrid)_grid.DeepCopyGrid();
            Assert.AreEqual("OriginalGrid", deepCopiedGrid.Name);
            Assert.AreNotSame(_grid.Tree, deepCopiedGrid.Tree, "DeepCopyGrid should copy the tree.");
            Assert.AreNotSame(_grid.Transform, deepCopiedGrid.Transform, "DeepCopyGrid should copy the transform.");

            // CopyGridWithNewTree (new tree, copies metadata & transform)
            var newTreeCopy = (FloatGrid)_grid.CopyGridWithNewTree();
            Assert.AreEqual("OriginalGrid", newTreeCopy.Name);
            Assert.AreNotSame(_grid.Tree, newTreeCopy.Tree, "CopyGridWithNewTree should have a new tree.");
            Assert.AreEqual(_grid.Background, newTreeCopy.Background, "New tree should have same background.");
            Assert.AreNotSame(_grid.Transform, newTreeCopy.Transform, "CopyGridWithNewTree should copy the transform.");
        }

        [Test]
        public void Grid_GetAccessor_ShouldReturnAccessor()
        {
            var accessor = _grid.GetAccessor();
            Assert.IsNotNull(accessor);
            // Further tests depend on accessor and tree implementation
        }

        [Test]
        public void Grid_PlaceholderTreeOperations_ShouldNotThrow()
        {
            // These operations use placeholder tree methods.
            // Test that they can be called without crashing.
            // Actual effects cannot be verified without a real tree.
            var bbox = new CoordBBox(new Coord(0,0,0), new Coord(5,5,5));
            Assert.DoesNotThrow(() => _grid.Fill(bbox, 10.0f, true));
            Assert.DoesNotThrow(() => _grid.DenseFill(bbox, 20.0f, true));
            Assert.DoesNotThrow(() => _grid.PruneGrid(0.1f));
            Assert.DoesNotThrow(() => _grid.Clip(bbox));
            Assert.DoesNotThrow(() => _grid.Clear());
            Assert.AreEqual(0, _grid.ActiveVoxelCount()); // Placeholder returns 0
        }

        [Test]
        public void Grid_StaticRegistry_ShouldWork()
        {
            // FloatGrid is registered in SetUp
            Assert.IsTrue(GridBase.IsRegistered(new Tree.Tree<float>().TreeType));
            var createdGrid = GridBase.CreateGrid(new Tree.Tree<float>().TreeType) as FloatGrid;
            Assert.IsNotNull(createdGrid);
            Assert.IsInstanceOf<FloatGrid>(createdGrid);
        }
    }
}
