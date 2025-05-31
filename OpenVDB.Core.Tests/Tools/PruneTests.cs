// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tools;
using OpenVDB.Core.Tree; // For ITreeValueAccessor
using OpenVDB.Core;     // For FloatGrid, BoolGrid
using OpenVDB.Math;
using System;

namespace OpenVDB.Core.Tests.Tools
{
    [TestFixture]
    public class PruneTests
    {
        private const float EpsilonF = 1e-6f;

        // Helper to create a FloatGrid and set specific values/states
        private FloatGrid CreateFloatGridForPrune(float background = 0.0f)
        {
            var grid = new FloatGrid(background);
            grid.Transform = Transform.CreateLinearTransform(1.0);
            return grid;
        }

        private BoolGrid CreateBoolGridForPrune(bool background = false)
        {
            var grid = new BoolGrid(background);
            grid.Transform = Transform.CreateLinearTransform(1.0);
            return grid;
        }

        // Helper to set up a small region for IsConstant checks
        // The current SimplifiedNodeInfo.IsConstant checks a 2x2x2 region from Origin.
        private void SetupRegionForConstCheck(ITreeValueAccessor<float> accessor, Coord regionOrigin, float value, bool active)
        {
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            for (int k = 0; k < 2; k++)
            {
                var coord = regionOrigin.OffsetBy(i,j,k);
                if (active) accessor.SetValueOn(coord, value);
                else accessor.SetValueOff(coord, value);
            }
        }

        private void SetupRegionForConstToleranceCheck(ITreeValueAccessor<float> accessor, Coord regionOrigin, float baseValue, float delta, bool active)
        {
            float val = baseValue;
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            for (int k = 0; k < 2; k++)
            {
                var coord = regionOrigin.OffsetBy(i,j,k);
                if (active) accessor.SetValueOn(coord, val);
                else accessor.SetValueOff(coord, val);
                val += delta / 7f; // Slightly vary values within the 8 points
            }
        }


        [Test]
        public void PruneInactive_ShouldSetInactiveNodeOriginToBackground()
        {
            var grid = CreateFloatGridForPrune(background: 100.0f);
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0, 0, 0); // This will be a conceptual "internal node" origin

            // Setup: Make the 2x2x2 region around nodeOrigin inactive (all background)
            // The simplified IsInactive check in SimplifiedNodeInfo will look at this region.
            for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) for (int k = 0; k < 2; k++)
            {
                accessor.SetValueOff(nodeOrigin.OffsetBy(i,j,k), 100.0f);
            }
            // Ensure at least one active voxel exists elsewhere to define an active bounding box for traversal
            accessor.SetValueOn(new Coord(10,10,10), 5.0f);


            PruneTools.PruneInactive(grid, threaded: false);

            // Test: The value at the nodeOrigin itself should now be background, and state should be off.
            // This is because InactivePruneOperation.ProcessNode (simplified) sets nodeOrigin to inactive tile.
            Assert.IsFalse(accessor.IsValueOn(nodeOrigin), "Node origin should be inactive after PruneInactive.");
            Assert.AreEqual(grid.Background, accessor.GetValue(nodeOrigin), EpsilonF, "Node origin value should be background.");
        }

        [Test]
        public void PruneInactiveWithValue_ShouldSetInactiveNodeOriginToCustomValue()
        {
            var grid = CreateFloatGridForPrune(background: 100.0f);
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0, 0, 0);
            float customValue = 55.0f;

            for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) for (int k = 0; k < 2; k++)
            {
                 accessor.SetValueOff(nodeOrigin.OffsetBy(i,j,k), 100.0f); // All background and inactive
            }
            accessor.SetValueOn(new Coord(10,10,10), 5.0f); // Ensure grid is not empty

            PruneTools.PruneInactiveWithValue(grid, customValue, threaded: false);

            Assert.IsFalse(accessor.IsValueOn(nodeOrigin), "Node origin should be inactive.");
            Assert.AreEqual(customValue, accessor.GetValue(nodeOrigin), EpsilonF, "Node origin value should be customValue.");
        }

        [Test]
        public void Prune_ToleranceExactMatch_BoolGrid_ShouldSetConstantNodeOriginToTile()
        {
            var grid = CreateBoolGridForPrune(background: false);
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0, 0, 0);

            // Setup: Make the 2x2x2 region around nodeOrigin constant (all true, active)
            SetupRegionForConstCheck(accessor as ITreeValueAccessor<bool>, nodeOrigin, true, true); // Cast needed
             accessor.SetValueOn(new Coord(10,10,10), false); // Ensure grid is not empty overall

            PruneTools.Prune(grid, false, threaded: false); // Tolerance = false for bool means exact match

            // Test: The value at nodeOrigin should be the representative value (true) and active.
            // TolerancePruneOperation.ProcessNode sets nodeOrigin to the tile state.
            Assert.IsTrue(accessor.IsValueOn(nodeOrigin), "Node origin should be active after Prune (exact).");
            Assert.AreEqual(true, accessor.GetValue(nodeOrigin), "Node origin value should be representative constant value.");
        }

        [Test]
        public void Prune_ToleranceMatch_FloatGrid_ShouldSetConstantNodeOriginToTile()
        {
            var grid = CreateFloatGridForPrune();
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0,0,0);
            float baseValue = 0.5f;
            float tolerance = 0.01f; // All values in [0.49, 0.51] approx

            // Setup: region with values ~0.5f, all active
            SetupRegionForConstToleranceCheck(accessor, nodeOrigin, baseValue, tolerance / 2.0f, true);
            accessor.SetValueOn(new Coord(10,10,10), 100.0f); // Ensure grid is not empty

            PruneTools.Prune(grid, tolerance, threaded: false);

            // The simplified IsConstant takes the value at Origin as representative.
            float expectedRepValue = baseValue; // Since baseValue was at Origin
            Assert.IsTrue(accessor.IsValueOn(nodeOrigin), "Node origin should be active after Prune (tolerance).");
            Assert.AreEqual(expectedRepValue, accessor.GetValue(nodeOrigin), EpsilonF,
                "Node origin value should be representative constant value.");
        }

        [Test]
        public void Prune_NonConstantRegion_ShouldNotChangeNodeOriginSignificantly()
        {
            var grid = CreateFloatGridForPrune();
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0,0,0);

            // Setup: Non-constant region
            accessor.SetValueOn(nodeOrigin.OffsetBy(0,0,0), 1.0f);
            accessor.SetValueOn(nodeOrigin.OffsetBy(1,0,0), 10.0f); // Clearly different
            accessor.SetValueOn(nodeOrigin.OffsetBy(0,1,0), 1.0f);
            accessor.SetValueOn(nodeOrigin.OffsetBy(1,1,0), 1.0f);
             accessor.SetValueOn(new Coord(10,10,10), 100.0f);


            float originalValueAtOrigin = accessor.GetValue(nodeOrigin);
            bool originalStateAtOrigin = accessor.IsValueOn(nodeOrigin);

            PruneTools.Prune(grid, 0.01f, threaded: false);

            // Test: The value at nodeOrigin should remain unchanged as the region is not constant.
            Assert.AreEqual(originalStateAtOrigin, accessor.IsValueOn(nodeOrigin));
            Assert.AreEqual(originalValueAtOrigin, accessor.GetValue(nodeOrigin), EpsilonF);
        }


        [Test]
        public void PruneLevelSet_OutsideCase_ShouldSetInactiveNodeToOutsideWidth()
        {
            var grid = CreateFloatGridForPrune(background: 10.0f); // Background (represents one of the widths)
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0,0,0);
            float outsideWidth = 5.0f;
            float insideWidth = -5.0f;

            // Setup: Node region is inactive, and its representative value is positive (outside)
            // The simplified IsInactive check looks at a 2x2x2 box from Origin.
            // The simplified GetTileValue in LevelSetPruneOperation checks Origin's value.
            accessor.SetValue(nodeOrigin, 2.0f); // Positive value at origin
            for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) for (int k = 0; k < 2; k++)
            {
                 accessor.SetValueOff(nodeOrigin.OffsetBy(i,j,k), grid.Background); // All inactive background
            }
            accessor.SetValue(nodeOrigin, 2.0f); // Ensure origin has the positive test value, but is inactive.
            accessor.SetValueOff(nodeOrigin);


            accessor.SetValueOn(new Coord(10,10,10), 100.0f); // Ensure grid not empty

            PruneTools.PruneLevelSet(grid, outsideWidth, insideWidth, threaded: false);

            Assert.IsFalse(accessor.IsValueOn(nodeOrigin), "Node origin should be inactive.");
            Assert.AreEqual(outsideWidth, accessor.GetValue(nodeOrigin), EpsilonF, "Node origin value should be outsideWidth.");
        }

        [Test]
        public void PruneLevelSet_InsideCase_ShouldSetInactiveNodeToInsideWidth()
        {
            var grid = CreateFloatGridForPrune(background: 10.0f);
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0,0,0);
            float outsideWidth = 5.0f;
            float insideWidth = -5.0f;

            accessor.SetValue(nodeOrigin, -2.0f); // Negative value at origin
             for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) for (int k = 0; k < 2; k++)
            {
                 accessor.SetValueOff(nodeOrigin.OffsetBy(i,j,k), grid.Background);
            }
            accessor.SetValue(nodeOrigin, -2.0f);
            accessor.SetValueOff(nodeOrigin);

            accessor.SetValueOn(new Coord(10,10,10), 100.0f);

            PruneTools.PruneLevelSet(grid, outsideWidth, insideWidth, threaded: false);

            Assert.IsFalse(accessor.IsValueOn(nodeOrigin), "Node origin should be inactive.");
            Assert.AreEqual(insideWidth, accessor.GetValue(nodeOrigin), EpsilonF, "Node origin value should be insideWidth.");
        }

        [Test]
        public void PruneLevelSet_DefaultWidths_UsesBackground()
        {
            float background = 3.0f;
            var grid = CreateFloatGridForPrune(background: background);
            var accessor = grid.GetAccessor();
            var nodeOrigin = new Coord(0,0,0);

            accessor.SetValue(nodeOrigin, 0.1f); // Positive value at origin, but node is inactive
            for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) for (int k = 0; k < 2; k++)
            {
                 accessor.SetValueOff(nodeOrigin.OffsetBy(i,j,k), background);
            }
            accessor.SetValue(nodeOrigin, 0.1f);
            accessor.SetValueOff(nodeOrigin);

            accessor.SetValueOn(new Coord(10,10,10), 100.0f);

            PruneTools.PruneLevelSet(grid, threaded: false); // Uses default widths (background, -background)

            Assert.IsFalse(accessor.IsValueOn(nodeOrigin));
            Assert.AreEqual(background, accessor.GetValue(nodeOrigin), EpsilonF, "Expected outsideWidth (background).");
        }
    }
}
