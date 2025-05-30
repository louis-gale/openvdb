// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tools;
using OpenVDB.Core.Tree;
using OpenVDB.Core.Math;
using OpenVDB.Core;
using System;

namespace OpenVDB.Core.Tests.Tools
{
    [TestFixture]
    public class ClipTests
    {
        private const float ActiveTestValue = 1.0f;
        private const float BackgroundTestValue = 0.0f;
        private const float Epsilon = 1e-6f;

        private FloatGrid CreateTestFloatGrid(CoordBBox activeRegion, float activeValue, float backgroundValue)
        {
            var grid = new FloatGrid(backgroundValue);
            grid.Transform = Transform.CreateLinearTransform(1.0); // Identity transform
            var accessor = grid.GetAccessor();

            // Fill the activeRegion
            for (int x = activeRegion.Min.X; x <= activeRegion.Max.X; ++x)
            {
                for (int y = activeRegion.Min.Y; y <= activeRegion.Max.Y; ++y)
                {
                    for (int z = activeRegion.Min.Z; z <= activeRegion.Max.Z; ++z)
                    {
                        accessor.SetValueOn(new Coord(x, y, z), activeValue);
                    }
                }
            }
            return grid;
        }

        private BoolGrid CreateMaskBoolGrid(CoordBBox activeMaskRegion, Transform transform, bool backgroundValue = false)
        {
            var grid = new BoolGrid(backgroundValue);
            grid.Transform = transform.Clone();
            var accessor = grid.GetAccessor();

            if (!activeMaskRegion.IsEmpty)
            {
                for (int x = activeMaskRegion.Min.X; x <= activeMaskRegion.Max.X; ++x)
                {
                    for (int y = activeMaskRegion.Min.Y; y <= activeMaskRegion.Max.Y; ++y)
                    {
                        for (int z = activeMaskRegion.Min.Z; z <= activeMaskRegion.Max.Z; ++z)
                        {
                            accessor.SetValueOn(new Coord(x, y, z), true);
                        }
                    }
                }
            }
            return grid;
        }

        [Test]
        public void ClipWithBBox_KeepInterior_ClipsCorrectly()
        {
            var originalActiveRegion = new CoordBBox(new Coord(0, 0, 0), new Coord(4, 4, 4)); // 5x5x5
            var grid = CreateTestFloatGrid(originalActiveRegion, ActiveTestValue, BackgroundTestValue);
            grid.GridClass = GridClass.LevelSet; // Test class change

            // World BBox that maps to index space 1,1,1 to 3,3,3
            var worldClipBBox = new BBox<Vec3<double>, double>(new Vec3<double>(1,1,1), new Vec3<double>(3,3,3));
            var expectedIndexClipRegion = new CoordBBox(new Coord(1,1,1), new Coord(3,3,3));

            var clippedGrid = ClipTools.ClipWithBoundingBox<FloatGrid, Tree.Tree<float>, float>(grid, worldClipBBox, keepInterior: true);
            var clippedAccessor = clippedGrid.GetAccessor();

            Assert.AreEqual(GridClass.Unknown, clippedGrid.GridClass, "GridClass should change to Unknown after clipping a LevelSet.");

            for (int x = 0; x <= 4; ++x)
            for (int y = 0; y <= 4; ++y)
            for (int z = 0; z <= 4; ++z)
            {
                var coord = new Coord(x, y, z);
                if (expectedIndexClipRegion.IsInside(coord))
                {
                    Assert.IsTrue(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be active (KeepInterior).");
                    Assert.AreEqual(ActiveTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
                else
                {
                    Assert.IsFalse(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be inactive (KeepInterior).");
                    Assert.AreEqual(BackgroundTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
            }
            // Check a point outside original active area
            Assert.IsFalse(clippedAccessor.IsValueOn(new Coord(10,10,10)));
        }

        [Test]
        public void ClipWithBBox_KeepExterior_ClipsCorrectly()
        {
            var originalActiveRegion = new CoordBBox(new Coord(0, 0, 0), new Coord(4, 4, 4)); // 5x5x5
            var grid = CreateTestFloatGrid(originalActiveRegion, ActiveTestValue, BackgroundTestValue);

            var worldClipBBox = new BBox<Vec3<double>, double>(new Vec3<double>(1,1,1), new Vec3<double>(3,3,3));
            var indexClipRegionToDiscard = new CoordBBox(new Coord(1,1,1), new Coord(3,3,3));

            var clippedGrid = ClipTools.ClipWithBoundingBox<FloatGrid, Tree.Tree<float>, float>(grid, worldClipBBox, keepInterior: false);
            var clippedAccessor = clippedGrid.GetAccessor();
            
            for (int x = 0; x <= 4; ++x)
            for (int y = 0; y <= 4; ++y)
            for (int z = 0; z <= 4; ++z)
            {
                var coord = new Coord(x, y, z);
                if (indexClipRegionToDiscard.IsInside(coord))
                {
                    Assert.IsFalse(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be inactive (KeepExterior).");
                    Assert.AreEqual(BackgroundTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
                else // Was in originalActiveRegion but outside discard region
                {
                    Assert.IsTrue(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be active (KeepExterior).");
                    Assert.AreEqual(ActiveTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
            }
        }

        [Test]
        public void ClipWithMask_KeepInterior_MatchingTransform_ClipsCorrectly()
        {
            var originalActiveRegion = new CoordBBox(new Coord(0,0,0), new Coord(4,4,4));
            var gridToClip = CreateTestFloatGrid(originalActiveRegion, ActiveTestValue, BackgroundTestValue);

            var maskActiveRegion = new CoordBBox(new Coord(1,1,1), new Coord(3,3,3));
            var maskingGrid = CreateMaskBoolGrid(maskActiveRegion, gridToClip.Transform.Clone());

            var clippedGrid = ClipTools.ClipWithMask<FloatGrid, Tree.Tree<float>, float, BoolGrid, Tree.Tree<bool>, bool>(
                gridToClip, maskingGrid, keepInterior: true);
            var clippedAccessor = clippedGrid.GetAccessor();

            for (int x = 0; x <= 4; ++x)
            for (int y = 0; y <= 4; ++y)
            for (int z = 0; z <= 4; ++z)
            {
                var coord = new Coord(x,y,z);
                if (maskActiveRegion.IsInside(coord)) // Kept if it was in original and mask is true
                {
                    Assert.IsTrue(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be active.");
                    Assert.AreEqual(ActiveTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
                else // Discarded if it was in original but mask is false/inactive
                {
                    Assert.IsFalse(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be inactive.");
                    Assert.AreEqual(BackgroundTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
            }
        }
        
        [Test]
        public void ClipWithMask_KeepExterior_MatchingTransform_ClipsCorrectly()
        {
            var originalActiveRegion = new CoordBBox(new Coord(0,0,0), new Coord(4,4,4));
            var gridToClip = CreateTestFloatGrid(originalActiveRegion, ActiveTestValue, BackgroundTestValue);

            var maskActiveRegionToDiscard = new CoordBBox(new Coord(1,1,1), new Coord(3,3,3));
            var maskingGrid = CreateMaskBoolGrid(maskActiveRegionToDiscard, gridToClip.Transform.Clone());

            var clippedGrid = ClipTools.ClipWithMask<FloatGrid, Tree.Tree<float>, float, BoolGrid, Tree.Tree<bool>, bool>(
                gridToClip, maskingGrid, keepInterior: false);
            var clippedAccessor = clippedGrid.GetAccessor();

            for (int x = 0; x <= 4; ++x)
            for (int y = 0; y <= 4; ++y)
            for (int z = 0; z <= 4; ++z)
            {
                var coord = new Coord(x,y,z);
                if (maskActiveRegionToDiscard.IsInside(coord)) // Discarded if it was in original and mask is true
                {
                    Assert.IsFalse(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be inactive.");
                    Assert.AreEqual(BackgroundTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
                else // Kept if it was in original and mask is false/inactive
                {
                    Assert.IsTrue(clippedAccessor.IsValueOn(coord), $"Voxel {coord} should be active.");
                    Assert.AreEqual(ActiveTestValue, clippedAccessor.GetValue(coord), Epsilon);
                }
            }
        }


        [Test]
        public void ClipWithMask_DifferingTransform_ShouldThrowNotImplemented()
        {
            var gridToClip = CreateTestFloatGrid(new CoordBBox(new Coord(0,0,0), new Coord(2,2,2)), 1.0f, 0.0f);
            var differentTransform = Transform.CreateTranslationTransform(new Vec3<double>(10,0,0));
            var maskingGrid = CreateMaskBoolGrid(new CoordBBox(new Coord(0,0,0), new Coord(1,1,1)), differentTransform);

            Assert.Throws<NotImplementedException>(() => 
                ClipTools.ClipWithMask<FloatGrid, Tree.Tree<float>, float, BoolGrid, Tree.Tree<bool>, bool>(
                    gridToClip, maskingGrid, true)
            );
        }

        [Test]
        public void ConvertToMaskGrid_ShouldCreateCorrectMask()
        {
            var originalActiveRegion = new CoordBBox(new Coord(1,1,1), new Coord(2,2,2));
            var floatGrid = CreateTestFloatGrid(originalActiveRegion, ActiveTestValue, BackgroundTestValue);
            // Add an inactive voxel within the original BBox to ensure only active are masked
            floatGrid.GetAccessor().SetValueOff(new Coord(1,1,1)); 

            var maskGrid = ClipInternal.ConvertToMaskGrid<FloatGrid, Tree.Tree<float>, float>(floatGrid);
            var maskAccessor = maskGrid.GetAccessor();

            Assert.AreEqual(floatGrid.Transform.GetMap().ToAffineMap().Matrix, maskGrid.Transform.GetMap().ToAffineMap().Matrix);
            Assert.AreEqual(floatGrid.Name + "_mask", maskGrid.Name);
            Assert.IsFalse(maskGrid.Background); // Default background for created mask is false

            for (int x = 0; x <= 3; x++) // Check a slightly larger area
            for (int y = 0; y <= 3; y++)
            for (int z = 0; z <= 3; z++)
            {
                var coord = new Coord(x,y,z);
                bool originalIsActive = coord.Equals(new Coord(1,1,1)) ? false : originalActiveRegion.IsInside(coord);
                
                if (originalIsActive)
                {
                    Assert.IsTrue(maskAccessor.IsValueOn(coord) && maskAccessor.GetValue(coord), $"Mask for active coord {coord} should be true.");
                }
                else
                {
                    Assert.IsFalse(maskAccessor.IsValueOn(coord), $"Mask for inactive coord {coord} should be false/inactive.");
                }
            }
        }
    }
}
