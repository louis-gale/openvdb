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
    public class NodeTableTests
    {
        private const int TableSize = 8; // Corresponds to Log2Dim = 1 for a 2x2x2 node
        private const float DefaultTileValue = 0.0f;
        private const float Epsilon = 1e-6f;

        // Using LeafNode<float> as a concrete INode<float> for TChildNodeType
        private LeafNode<float> CreateTestLeafNode(Coord origin, float fillValue = 1.0f)
        {
            return new LeafNode<float>(origin, 3, fillValue, true); // Assuming LeafNode Log2Dim = 3
        }

        [Test]
        public void Constructor_InitializesWithTileValues()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            for (int i = 0; i < TableSize; ++i)
            {
                Assert.IsFalse(table.IsChildNode(i), $"Index {i} should initially be a tile.");
                Assert.AreEqual(DefaultTileValue, table.GetTileValue(i), Epsilon, $"Tile value at {i} mismatch.");
            }
        }

        [Test]
        public void SetAndGetChildNode_WorksCorrectly()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            var leafNode = CreateTestLeafNode(new Coord(0,0,0));

            table.SetChildNode(0, leafNode);
            Assert.IsTrue(table.IsChildNode(0));
            Assert.AreSame(leafNode, table.GetChildNode(0));
            Assert.Throws<InvalidOperationException>(() => table.GetTileValue(0), "Accessing tile value of a child entry should throw.");

            // Check other entries remain tiles
            Assert.IsFalse(table.IsChildNode(1));
            Assert.AreEqual(DefaultTileValue, table.GetTileValue(1), Epsilon);
        }

        [Test]
        public void SetAndGetTileValue_WorksCorrectly()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            float newTileValue = 5.0f;

            table.SetTileValue(0, newTileValue);
            Assert.IsFalse(table.IsChildNode(0));
            Assert.AreEqual(newTileValue, table.GetTileValue(0), Epsilon);
        }

        [Test]
        public void SetTileValue_OverwritesChildNode()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            var leafNode = CreateTestLeafNode(new Coord(0,0,0));
            table.SetChildNode(0, leafNode);
            Assert.IsTrue(table.IsChildNode(0));

            float newTileValue = 10.0f;
            table.SetTileValue(0, newTileValue);
            Assert.IsFalse(table.IsChildNode(0), "Should no longer be a child node after SetTileValue.");
            Assert.AreEqual(newTileValue, table.GetTileValue(0), Epsilon);
            Assert.IsNull(table.GetChildNode(0), "GetChildNode should return null after being set to tile.");
        }

        [Test]
        public void RemoveChild_ReplacesChildWithTile()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            var leafNode = CreateTestLeafNode(new Coord(0,0,0));
            table.SetChildNode(0, leafNode);
            Assert.IsTrue(table.IsChildNode(0));

            float replacementTileValue = 20.0f;
            table.RemoveChild(0, replacementTileValue);
            Assert.IsFalse(table.IsChildNode(0));
            Assert.AreEqual(replacementTileValue, table.GetTileValue(0), Epsilon);
        }

        [Test]
        public void IsUniform_AllSameTiles_ReturnsTrue()
        {
            float uniformValue = 7.0f;
            var table = new NodeTable<LeafNode<float>, float>(TableSize, uniformValue);
            var childMask = new NodeMask(1, false); // No children active

            Assert.IsTrue(table.IsUniform(childMask, out float tileVal));
            Assert.AreEqual(uniformValue, tileVal, Epsilon);
        }

        [Test]
        public void IsUniform_MixedTiles_ReturnsFalse()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            table.SetTileValue(0, 1.0f);
            table.SetTileValue(1, 2.0f);
            var childMask = new NodeMask(1, false);

            Assert.IsFalse(table.IsUniform(childMask, out _));
        }

        [Test]
        public void IsUniform_WithChildren_ReturnsFalse()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, DefaultTileValue);
            table.SetChildNode(0, CreateTestLeafNode(Coord.Zero));
            var childMask = new NodeMask(1, false);
            childMask.SetOn(0); // Mark index 0 as having a child

            Assert.IsFalse(table.IsUniform(childMask, out _));
        }

        [Test]
        public void IsUniform_SomeChildrenAndUniformTiles_ReturnsFalseIfAnyChildPresent()
        {
            float uniformTileVal = 5.0f;
            var table = new NodeTable<LeafNode<float>, float>(TableSize, uniformTileVal); // All tiles are 5.0f
            table.SetChildNode(0, CreateTestLeafNode(Coord.Zero)); // Add one child

            var childMask = new NodeMask(1, false);
            childMask.SetOn(0); // Child at index 0

            // Even if all other tiles are uniform, the presence of a child makes the table non-uniform in this context.
            Assert.IsFalse(table.IsUniform(childMask, out _));
        }

        [Test]
        public void IsUniform_EmptyTable_ReturnsTrue() // Log2Dim=0 -> Size=1
        {
            var table = new NodeTable<LeafNode<float>, float>(1, 5.0f);
            var childMask = new NodeMask(0, false);
            Assert.IsTrue(table.IsUniform(childMask, out float tileVal));
            Assert.AreEqual(5.0f, tileVal, Epsilon);
        }


        [Test]
        public void WriteAndReadTiles_ShouldPreserveTileData()
        {
            var table = new NodeTable<LeafNode<float>, float>(TableSize, 0.0f);
            table.SetTileValue(0, 1.0f);
            // Index 1 is child
            table.SetTileValue(2, 3.0f);
            // Index 3 is child
            table.SetTileValue(4, 5.0f);
            // Index 5,6,7 are default 0.0f tiles

            var valueMask = new NodeMask(1, false); // Active tiles
            valueMask.SetOn(0); // Tile 1.0f
            valueMask.SetOn(2); // Tile 3.0f
            valueMask.SetOn(4); // Tile 5.0f
            // valueMask.SetOn(5); // Tile 0.0f - if we want to write active background tiles

            var childMask = new NodeMask(1, false); // Children
            childMask.SetOn(1);
            childMask.SetOn(3);

            var streamMeta = new StreamMetadata(); // Default

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    table.WriteTiles(writer, valueMask, childMask, streamMeta);
                }

                ms.Seek(0, SeekOrigin.Begin);

                var newTable = new NodeTable<LeafNode<float>, float>(TableSize, -1.0f); // Different background
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    // ReadTiles needs to know which entries are tiles (not children) and active
                    // It will only read data for entries where valueMask.IsOn(i) AND childMask.IsOff(i)
                    newTable.ReadTiles(reader, valueMask, childMask, streamMeta, -1.0f); // Background for inactive tiles
                }

                // Verify only active tiles that are not children were read and match
                Assert.AreEqual(1.0f, newTable.GetTileValue(0), Epsilon); // Was active tile
                Assert.IsFalse(newTable.IsChildNode(0));

                Assert.AreEqual(-1.0f, newTable.GetTileValue(1), Epsilon); // Was child, should be background in newTable after ReadTiles
                Assert.IsFalse(newTable.IsChildNode(1));

                Assert.AreEqual(3.0f, newTable.GetTileValue(2), Epsilon); // Was active tile
                Assert.IsFalse(newTable.IsChildNode(2));

                Assert.AreEqual(-1.0f, newTable.GetTileValue(3), Epsilon); // Was child
                Assert.IsFalse(newTable.IsChildNode(3));

                Assert.AreEqual(5.0f, newTable.GetTileValue(4), Epsilon); // Was active tile
                Assert.IsFalse(newTable.IsChildNode(4));

                // Indices 5,6,7 were not active in valueMask, so they should have backgroundForRead
                Assert.AreEqual(-1.0f, newTable.GetTileValue(5), Epsilon);
                Assert.IsFalse(newTable.IsChildNode(5));
            }
        }
    }
}
