// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tree;
using OpenVDB.Core.IO;   // For StreamMetadata
using OpenVDB.Math;    // For Coord
using System;
using System.IO;
using System.Linq; // For Count()

namespace OpenVDB.Core.Tests.Tree
{
    [TestFixture]
    public class RootNodeIoTests
    {
        // Concrete type aliases for tests
        using TestLeafNode = LeafNode<float>;
        using TestInternalNode = InternalNode<LeafNode<float>, float>; // Child is LeafNode
        using TestRootNode = RootNode<InternalNode<LeafNode<float>, float>, float>; // Child is InternalNode

        private const float Epsilon = 1e-6f;

        // Helper to create a populated LeafNode for testing
        private TestLeafNode CreatePopulatedLeafNode(Coord origin, float startValue, bool allActive = true)
        {
            var leaf = new TestLeafNode(origin, TestLeafNode.StaticLog2Dim, 0.0f, false); // Start with background
            var accessor = new LeafNodeAccessor<float>(leaf); // Use a direct accessor for leaf setup

            if (allActive)
            {
                leaf.ValueMask.SetAll(true);
                leaf.Buffer.Allocate(); // Ensure buffer is allocated if it was uniform
            }

            for (int i = 0; i < leaf.NumValues; ++i)
            {
                if (allActive || (i % 2 == 0)) // Make some pattern if not all active
                {
                    Coord localCoord = LeafNode<float>.OffsetToLocalCoord(i, leaf.Log2Dim);
                    accessor.SetValue(localCoord, startValue + i);
                    if (!allActive) accessor.SetActiveState(localCoord, true);
                }
            }
            return leaf;
        }
        
        // Helper to create an InternalNode, potentially with one child LeafNode
        private TestInternalNode CreatePopulatedInternalNode(Coord origin, float tileValue, bool tileActive,
            Coord? childLeafLocalOrigin = null, TestLeafNode childLeaf = null)
        {
            var internalNode = new TestInternalNode(origin, TestInternalNode.StaticLog2Dim, tileValue, tileActive);
            if (childLeafLocalOrigin.HasValue && childLeaf != null)
            {
                internalNode.SetChildNode(childLeafLocalOrigin.Value, childLeaf);
            }
            return internalNode;
        }


        [Test]
        public void WriteAndReadTopology_EmptyRootNode_ShouldPreserveBackground()
        {
            float background = 123.45f;
            var originalRoot = new TestRootNode(background);
            var streamMeta = new StreamMetadata();

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalRoot.WriteTopology(writer, streamMeta);
                }

                ms.Seek(0, SeekOrigin.Begin);

                // For reading, RootNode constructor needs a dummy background, ReadTopology will set the actual one.
                var newRoot = new TestRootNode(default(float)); 
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newRoot.ReadTopology(reader, streamMeta);
                }

                Assert.AreEqual(originalRoot.BackgroundValue, newRoot.BackgroundValue, Epsilon);
                Assert.AreEqual(0, newRoot.ChildTableCountTestHook); // Assuming ChildTable is internal Dictionary
            }
        }

        [Test]
        public void WriteAndRead_RootWithOneChildLeaf_ShouldPreserveHierarchyAndData()
        {
            float rootBg = 0.0f;
            var originalRoot = new TestRootNode(rootBg);
            
            var leafOrigin = new Coord(0, 0, 0); // This is the origin for the child node slot in root
                                                 // The leaf's own origin will be this.
            var originalLeaf = CreatePopulatedLeafNode(leafOrigin, 5.0f, allActive: true);
            originalLeaf.ValueMask.SetOff(0); // Make one voxel inactive
            originalLeaf.SetValueOnly(LeafNode<float>.OffsetToLocalCoord(0, originalLeaf.Log2Dim), 100.0f); // Set its value

            // RootNode's children are InternalNodes. So we need an InternalNode to hold the Leaf.
            // The leafOrigin here is the origin for the InternalNode child of the Root.
            // The LeafNode will be a child of this InternalNode, at a local offset within it.
            var internalChildOrigin = leafOrigin; // Let InternalNode have the same origin for simplicity here
            var internalChild = new TestInternalNode(internalChildOrigin, TestInternalNode.StaticLog2Dim, rootBg, false);
            
            Coord leafLocalCoordInInternal = new Coord(1,1,1); // Place leaf at (1,1,1) within InternalNode
            originalLeaf.Origin = internalChildOrigin + leafLocalCoordInInternal; // Update leaf's global origin
            
            internalChild.SetChildNode(leafLocalCoordInInternal, originalLeaf);
            originalRoot.SetChildNode(internalChildOrigin, internalChild);

            var streamMeta = new StreamMetadata();

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalRoot.WriteTopology(writer, streamMeta);
                    originalRoot.WriteBuffers(writer, streamMeta);
                }

                ms.Seek(0, SeekOrigin.Begin);
                
                var newRoot = new TestRootNode(default(float));
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newRoot.ReadTopology(reader, streamMeta);
                     // Must set background in streamMeta AFTER root reads its own, BEFORE children read.
                    streamMeta.SetBackgroundValue(newRoot.BackgroundValue);
                    newRoot.ReadBuffers(reader, streamMeta, newRoot.BackgroundValue);
                }

                Assert.AreEqual(rootBg, newRoot.BackgroundValue, Epsilon);
                Assert.AreEqual(1, newRoot.ChildTableCountTestHook);

                var deserializedInternalChild = newRoot.GetChildNode(internalChildOrigin);
                Assert.IsNotNull(deserializedInternalChild);
                Assert.IsInstanceOf<TestInternalNode>(deserializedInternalChild);
                
                var deserializedLeaf = deserializedInternalChild.GetChildNode(leafLocalCoordInInternal);
                Assert.IsNotNull(deserializedLeaf);
                Assert.IsInstanceOf<TestLeafNode>(deserializedLeaf);

                Assert.AreEqual(originalLeaf.Origin, deserializedLeaf.Origin);
                Assert.AreEqual(originalLeaf.Log2Dim, deserializedLeaf.Log2Dim);

                // Compare masks
                for (int i = 0; i < originalLeaf.NumValues; ++i)
                {
                    Assert.AreEqual(originalLeaf.ValueMask.IsOn(i), deserializedLeaf.ValueMask.IsOn(i), $"Mask mismatch at leaf index {i}");
                }
                // Compare buffer values for active voxels
                for (int i = 0; i < originalLeaf.NumValues; ++i)
                {
                    if (originalLeaf.ValueMask.IsOn(i)) // Only compare if it was supposed to be written
                    {
                         Assert.AreEqual(originalLeaf.Buffer.GetValue(i), deserializedLeaf.Buffer.GetValue(i), Epsilon, $"Buffer value mismatch at leaf index {i}");
                    }
                }
                 Assert.AreEqual(originalLeaf.GetValue(LeafNode<float>.OffsetToLocalCoord(0, originalLeaf.Log2Dim)), 
                                deserializedLeaf.GetValue(LeafNode<float>.OffsetToLocalCoord(0, originalLeaf.Log2Dim)), Epsilon);

            }
        }
        
        [Test]
        public void WriteAndRead_RootWithNestedStructure_ShouldPreserveHierarchy()
        {
            float rootBg = 0.0f;
            var originalRoot = new TestRootNode(rootBg);

            var internal1Origin = new Coord(0,0,0);
            var internal1 = CreatePopulatedInternalNode(internal1Origin, 100.0f, true); // All tiles active with 100.0f

            var leaf1Origin = internal1Origin + new Coord(1,1,1); // Leaf relative to InternalNode1's origin
            var leaf1 = CreatePopulatedLeafNode(leaf1Origin, 200.0f);
            leaf1.SetValueOnly(new Coord(0,0,0), 205.0f); // Change one voxel in leaf1 (local coord)
            internal1.SetChildNode(new Coord(1,1,1), leaf1); // Set leaf1 as child of internal1

            internal1.SetTileValue(new Coord(0,0,0), 300.0f, true); // An active tile in internal1

            originalRoot.SetChildNode(internal1Origin, internal1);

            var streamMeta = new StreamMetadata();
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalRoot.WriteTopology(writer, streamMeta);
                    originalRoot.WriteBuffers(writer, streamMeta);
                }
                ms.Seek(0, SeekOrigin.Begin);
                var newRoot = new TestRootNode(default(float));
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newRoot.ReadTopology(reader, streamMeta);
                    streamMeta.SetBackgroundValue(newRoot.BackgroundValue);
                    newRoot.ReadBuffers(reader, streamMeta, newRoot.BackgroundValue);
                }

                Assert.AreEqual(rootBg, newRoot.BackgroundValue, Epsilon);
                var newInternal1 = newRoot.GetChildNode(internal1Origin) as TestInternalNode;
                Assert.IsNotNull(newInternal1);
                Assert.AreEqual(internal1Origin, newInternal1.Origin);
                Assert.AreEqual(TestInternalNode.StaticLog2Dim, newInternal1.Log2Dim);

                // Check active tile in newInternal1
                Assert.IsTrue(newInternal1.IsTileValueOn(new Coord(0,0,0)));
                Assert.AreEqual(300.0f, newInternal1.GetLocalTileValue(new Coord(0,0,0)), Epsilon);
                
                // Check grandchild leaf
                var newLeaf1 = newInternal1.GetChildNode(new Coord(1,1,1)) as TestLeafNode;
                Assert.IsNotNull(newLeaf1);
                Assert.AreEqual(leaf1Origin, newLeaf1.Origin);
                Assert.AreEqual(TestLeafNode.StaticLog2Dim, newLeaf1.Log2Dim);
                Assert.AreEqual(205.0f, newLeaf1.GetValue(new Coord(0,0,0)), Epsilon); // Check the modified voxel
                Assert.AreEqual(200.0f + 1, newLeaf1.GetValue(new Coord(0,0,1)), Epsilon); // Check another populated voxel
            }
        }
        
        [Test]
        public void WriteAndRead_RootWithMultipleChildren_ShouldPreserveAll()
        {
            float rootBg = 10.0f;
            var originalRoot = new TestRootNode(rootBg);

            var internal1Origin = new Coord(0,0,0);
            var internal1 = CreatePopulatedInternalNode(internal1Origin, 1.0f, true); // All tiles active with 1.0f
            originalRoot.SetChildNode(internal1Origin, internal1);

            var internal2Origin = new Coord(1 << TestInternalNode.StaticLog2Dim, 0, 0); // Place next to internal1
            var internal2 = CreatePopulatedInternalNode(internal2Origin, 2.0f, true);
            originalRoot.SetChildNode(internal2Origin, internal2);
            
            var streamMeta = new StreamMetadata();
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalRoot.WriteTopology(writer, streamMeta);
                    // Buffers are not written for internal nodes with only tiles in this simplified test.
                    // If they had child leaves with buffers, originalRoot.WriteBuffers would be needed.
                }
                ms.Seek(0, SeekOrigin.Begin);
                var newRoot = new TestRootNode(default(float));
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newRoot.ReadTopology(reader, streamMeta);
                    // streamMeta.SetBackgroundValue(newRoot.BackgroundValue); // Already done in ReadTopology
                    // newRoot.ReadBuffers(reader, streamMeta, newRoot.BackgroundValue); // No buffers to read for this setup
                }

                Assert.AreEqual(rootBg, newRoot.BackgroundValue, Epsilon);
                Assert.AreEqual(2, newRoot.ChildTableCountTestHook);

                var newInternal1 = newRoot.GetChildNode(internal1Origin) as TestInternalNode;
                Assert.IsNotNull(newInternal1);
                Assert.AreEqual(1.0f, newInternal1.GetLocalTileValue(new Coord(0,0,0)), Epsilon); // Check a tile

                var newInternal2 = newRoot.GetChildNode(internal2Origin) as TestInternalNode;
                Assert.IsNotNull(newInternal2);
                Assert.AreEqual(2.0f, newInternal2.GetLocalTileValue(new Coord(0,0,0)), Epsilon);
            }
        }
    }

    // Helper extension to access internal RootNode._childTable.Count for testing
    internal static class RootNodeTestExtensions
    {
        public static int ChildTableCountTestHook<TChild, TValue>(this RootNode<TChild, TValue> rootNode)
            where TChild : class, INode<TValue>, new()
            where TValue : struct
        {
            var fieldInfo = typeof(RootNode<TChild, TValue>).GetField("_childTable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = fieldInfo?.GetValue(rootNode) as System.Collections.IDictionary;
            return dict?.Count ?? -1;
        }
    }
}
