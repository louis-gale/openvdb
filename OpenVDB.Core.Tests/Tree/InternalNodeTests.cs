// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tree;
using OpenVDB.Core.Util; // For NodeMask
using OpenVDB.Core.IO;   // For StreamMetadata
using OpenVDB.Math;    // For Coord
using System;
using System.IO;

namespace OpenVDB.Core.Tests.Tree
{
    [TestFixture]
    public class InternalNodeTests
    {
        // Define a concrete internal node type for testing.
        // InternalNode<ChildType, ChildValueType>
        // Here, ChildType is LeafNode<float>, and ChildValueType (which is TValue for InternalNode) is float.
        using TestInternalNode = InternalNode<LeafNode<float>, float>;

        private const int TestInternalNodeLog2Dim = 3; // 8x8x8 table in the internal node
        private const int TestLeafNodeLog2Dim = 3;   // 8x8x8 voxels in child leaves
        private const float DefaultTileValue = 0.0f;
        private const bool DefaultActiveTileState = false; // Tiles are initially inactive background
        private const float Epsilon = 1e-6f;

        private LeafNode<float> CreateTestLeafNode(Coord origin, float fillValue = 1.0f, bool activeState = true)
        {
            return new LeafNode<float>(origin, TestLeafNodeLog2Dim, fillValue, activeState);
        }

        private TestInternalNode CreateTestInternalNode(Coord origin, float initialTileValue = DefaultTileValue, bool activeTileState = DefaultActiveTileState)
        {
            return new TestInternalNode(origin, TestInternalNodeLog2Dim, initialTileValue, activeTileState);
        }

        [Test]
        public void Constructor_InitializesCorrectly()
        {
            var origin = new Coord(0, 0, 0);
            float tileVal = 5.0f;
            bool tileActive = true;
            var node = CreateTestInternalNode(origin, tileVal, tileActive);

            Assert.AreEqual(origin, node.Origin);
            Assert.AreEqual(TestInternalNodeLog2Dim, node.Log2Dim);
            Assert.AreEqual(1 << TestInternalNodeLog2Dim, node.Dim);
            Assert.AreEqual((1 << TestInternalNodeLog2Dim) * (1 << TestInternalNodeLog2Dim) * (1 << TestInternalNodeLog2Dim), node.NumValues);

            var defaultLeaf = new LeafNode<float>(Coord.Zero, TestLeafNodeLog2Dim, 0.0f, false);
            Assert.AreEqual(defaultLeaf.Level + 1, node.Level);

            // Check initial tile state
            for (int i = 0; i < node.NumValues; ++i)
            {
                Coord localCoord = TestInternalNode.OffsetToLocalCoord(i, node.Log2Dim);
                Assert.IsFalse(node.HasChild(localCoord), $"Offset {i} should not have a child initially.");
                Assert.AreEqual(tileActive, node.IsTileValueOn(localCoord), $"Tile active state mismatch at offset {i}.");
                Assert.AreEqual(tileVal, node.GetLocalTileValue(localCoord), Epsilon, $"Tile value mismatch at offset {i}.");
            }
            Assert.AreEqual(tileActive ? node.NumValues : 0, node.ActiveTileCount());
        }

        [Test]
        public void SetAndGetChildNode_WorkCorrectly()
        {
            var node = CreateTestInternalNode(Coord.Zero);
            var childOrigin = node.Origin + TestInternalNode.OffsetToLocalCoord(0, node.Log2Dim); // Child for offset 0
            var leaf = CreateTestLeafNode(childOrigin, 10.0f);
            var localCoord = new Coord(0,0,0); // Local coord for offset 0

            node.SetChildNode(localCoord, leaf);

            Assert.IsTrue(node.HasChild(localCoord));
            Assert.AreSame(leaf, node.GetChildNode(localCoord));
            Assert.IsFalse(node.IsTileValueOn(localCoord), "ValueMask should be off for a child entry.");
            Assert.AreEqual(1, node.ChildCount());
        }

        [Test]
        public void SetAndGetTileValue_WorkCorrectly()
        {
            var node = CreateTestInternalNode(Coord.Zero);
            var localCoord = new Coord(1,1,1);
            float testValue = 25.0f;

            node.SetTileValue(localCoord, testValue, true);

            Assert.IsFalse(node.HasChild(localCoord));
            Assert.IsTrue(node.IsTileValueOn(localCoord));
            Assert.AreEqual(testValue, node.GetLocalTileValue(localCoord), Epsilon);
            Assert.AreEqual(1, node.ActiveTileCount());
        }

        [Test]
        public void SetTileValue_OverwritesExistingChild()
        {
            var node = CreateTestInternalNode(Coord.Zero);
            var localCoord = new Coord(0,0,0);
            var leaf = CreateTestLeafNode(node.Origin + localCoord);
            node.SetChildNode(localCoord, leaf);
            Assert.IsTrue(node.HasChild(localCoord));
            Assert.AreEqual(1, node.ChildCount());

            float newTileValue = 35.0f;
            node.SetTileValue(localCoord, newTileValue, true);

            Assert.IsFalse(node.HasChild(localCoord));
            Assert.IsTrue(node.IsTileValueOn(localCoord));
            Assert.AreEqual(newTileValue, node.GetLocalTileValue(localCoord), Epsilon);
            Assert.AreEqual(0, node.ChildCount());
            Assert.AreEqual(1, node.ActiveTileCount());
        }

        [Test]
        public void RemoveChild_ReplacesChildWithTile()
        {
            var node = CreateTestInternalNode(Coord.Zero);
            var localCoord = new Coord(0,0,0);
            var leaf = CreateTestLeafNode(node.Origin + localCoord);
            node.SetChildNode(localCoord, leaf);
            Assert.IsTrue(node.HasChild(localCoord));

            float replacementTileValue = 45.0f;
            node.RemoveChild(localCoord, replacementTileValue, false); // Replace with inactive tile

            Assert.IsFalse(node.HasChild(localCoord));
            Assert.IsFalse(node.IsTileValueOn(localCoord));
            Assert.AreEqual(replacementTileValue, node.GetLocalTileValue(localCoord), Epsilon);
        }

        [Test]
        public void GetValue_ReturnsCorrectTileValue_FromLocalTile()
        {
            var nodeOrigin = new Coord(0,0,0);
            var node = CreateTestInternalNode(nodeOrigin, 7.0f, true); // All tiles active with 7.0f
            var localCoord = new Coord(1,2,3);
            var globalCoord = nodeOrigin + localCoord;

            Assert.AreEqual(7.0f, node.GetValue(globalCoord), Epsilon);
            Assert.IsTrue(node.IsValueOn(globalCoord));
        }

        [Test]
        public void GetValue_ReturnsCorrectVoxelValue_FromChildLeaf()
        {
            var nodeOrigin = new Coord(0,0,0);
            var node = CreateTestInternalNode(nodeOrigin); // Default tiles (0, inactive)

            var childLocalCoordInInternal = new Coord(1,1,1); // Child is at (1,1,1) in internal node's space
            var childGlobalOrigin = nodeOrigin + childLocalCoordInInternal; // Child's global origin

            var childLeaf = CreateTestLeafNode(childGlobalOrigin, 0.0f, false); // Leaf, all inactive background
            var voxelLocalCoordInLeaf = new Coord(2,2,2);
            float voxelValue = 88.0f;
            childLeaf.SetValue(voxelLocalCoordInLeaf, voxelValue); // Sets active and value

            node.SetChildNode(childLocalCoordInInternal, childLeaf);

            var globalTargetCoord = childGlobalOrigin + voxelLocalCoordInLeaf;

            Assert.AreEqual(voxelValue, node.GetValue(globalTargetCoord), Epsilon);
            Assert.IsTrue(node.IsValueOn(globalTargetCoord));
        }

        [Test]
        public void GetValue_ReturnsBackground_ForNonExistentPath()
        {
            var nodeOrigin = new Coord(0,0,0);
            float initialTileVal = 1.0f;
            var node = CreateTestInternalNode(nodeOrigin, initialTileVal, false); // All tiles inactive with 1.0f

            var localCoordWithNoChild = new Coord(3,3,3);
            var globalCoord = nodeOrigin + localCoordWithNoChild;

            Assert.AreEqual(initialTileVal, node.GetValue(globalCoord), Epsilon);
            Assert.IsFalse(node.IsValueOn(globalCoord));
        }

        [Test]
        public void SetValue_UpdatesLocalTile()
        {
            var nodeOrigin = new Coord(0,0,0);
            var node = CreateTestInternalNode(nodeOrigin, 0.0f, false);
            var localCoord = new Coord(1,2,3);
            var globalCoord = nodeOrigin + localCoord;
            float newValue = 99.0f;

            node.SetValue(globalCoord, newValue);
            Assert.AreEqual(newValue, node.GetLocalTileValue(localCoord), Epsilon);
            Assert.IsTrue(node.IsTileValueOn(localCoord));
            Assert.IsTrue(node.IsValueOn(globalCoord));
        }

        [Test]
        public void SetValue_UpdatesVoxelInChildLeaf()
        {
            var nodeOrigin = new Coord(0,0,0);
            var node = CreateTestInternalNode(nodeOrigin);
            var childLocalCoord = new Coord(1,1,1);
            var childGlobalOrigin = nodeOrigin + childLocalCoord;
            var leaf = CreateTestLeafNode(childGlobalOrigin, 0.0f, false);
            node.SetChildNode(childLocalCoord, leaf);

            var voxelLocalCoordInLeaf = new Coord(2,3,4);
            var globalTargetCoord = childGlobalOrigin + voxelLocalCoordInLeaf;
            float newValue = 123.0f;

            node.SetValue(globalTargetCoord, newValue); // Should path to leaf and set value

            Assert.IsTrue(leaf.IsValueOn(voxelLocalCoordInLeaf));
            Assert.AreEqual(newValue, leaf.GetValue(voxelLocalCoordInLeaf), Epsilon);
            Assert.IsTrue(node.IsValueOn(globalTargetCoord));
            Assert.AreEqual(newValue, node.GetValue(globalTargetCoord), Epsilon);
        }

        [Test]
        public void SetValue_CreatesActiveTile_IfNoChildAtPathAndPathCreationIsPlaceholder()
        {
            var nodeOrigin = new Coord(0,0,0);
            var initialTileValue = 0.0f;
            var node = CreateTestInternalNode(nodeOrigin, initialTileValue, false); // All tiles are 0.0f, inactive

            var localCoordToBecomeTile = new Coord(2,2,2);
            var globalCoord = nodeOrigin + localCoordToBecomeTile;
            float newValue = 77.0f;

            // Current SetValue on InternalNode will try to find a child. If none, it sets a local tile.
            node.SetValue(globalCoord, newValue);

            Assert.IsFalse(node.HasChild(localCoordToBecomeTile));
            Assert.IsTrue(node.IsTileValueOn(localCoordToBecomeTile));
            Assert.AreEqual(newValue, node.GetLocalTileValue(localCoordToBecomeTile), Epsilon);
        }


        [Test]
        public void IoMethods_Placeholders_DoNotCrash()
        {
            var node = CreateTestInternalNode(Coord.Zero);
            var streamMeta = new StreamMetadata();
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    Assert.DoesNotThrow(() => node.WriteTopology(writer, streamMeta));
                    Assert.DoesNotThrow(() => node.WriteBuffers(writer, streamMeta));
                }
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    Assert.DoesNotThrow(() => node.ReadTopology(reader, streamMeta));
                    Assert.DoesNotThrow(() => node.ReadBuffers(reader, streamMeta, DefaultTileValue));
                }
            }
        }
    }
}
