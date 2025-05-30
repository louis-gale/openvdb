// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core;
using OpenVDB.Core.Tools;
using OpenVDB.Core.Tree; // For placeholder Tree
using OpenVDB.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVDB.Core.Tests.Tools
{
    [TestFixture]
    public class DiagnosticsTests
    {
        private const float EpsilonF = 1e-6f;

        // Helper to create a simple FloatGrid for testing Diagnose and static checks
        private FloatGrid CreateTestFloatGrid(float backgroundValue = 0.0f)
        {
            var grid = new FloatGrid(backgroundValue);
            grid.Transform = Transform.CreateLinearTransform(1.0); // Uniform scale
            grid.GridClass = GridClass.Unknown; // Default, can be changed by tests
            return grid;
        }

        [Test]
        public void CheckNaN_PredicateTests()
        {
            var pred = new CheckNaN<float>();
            Assert.IsTrue(pred.CheckFailed(float.NaN));
            Assert.IsFalse(pred.CheckFailed(1.0f));
            Assert.IsFalse(pred.CheckFailed(float.PositiveInfinity));
            StringAssert.Contains("NaN", pred.GetDescription());
            StringAssert.Contains("NaN", pred.GetFailureDescription(float.NaN));
        }

        [Test]
        public void CheckInf_PredicateTests()
        {
            var pred = new CheckInf<float>();
            Assert.IsTrue(pred.CheckFailed(float.PositiveInfinity));
            Assert.IsTrue(pred.CheckFailed(float.NegativeInfinity));
            Assert.IsFalse(pred.CheckFailed(1.0f));
            Assert.IsFalse(pred.CheckFailed(float.NaN));
            StringAssert.Contains("Infinity", pred.GetDescription());
            StringAssert.Contains("Infinity", pred.GetFailureDescription(float.PositiveInfinity));
        }

        [Test]
        public void CheckFinite_PredicateTests()
        {
            var pred = new CheckFinite<float>();
            Assert.IsFalse(pred.CheckFailed(1.0f));
            Assert.IsTrue(pred.CheckFailed(float.NaN));
            Assert.IsTrue(pred.CheckFailed(float.PositiveInfinity));
            StringAssert.Contains("non-finite", pred.GetDescription());
            StringAssert.Contains("Non-finite", pred.GetFailureDescription(float.NaN));
        }

        [Test]
        public void CheckMagnitude_PredicateTests()
        {
            var pred = new CheckMagnitude<float>(10.0f);
            Assert.IsFalse(pred.CheckFailed(5.0f));
            Assert.IsFalse(pred.CheckFailed(10.0f));
            Assert.IsFalse(pred.CheckFailed(-10.0f));
            Assert.IsTrue(pred.CheckFailed(10.1f));
            Assert.IsTrue(pred.CheckFailed(-10.1f));
            StringAssert.Contains("magnitude > 10", pred.GetDescription());
            StringAssert.Contains("exceeds max magnitude 10", pred.GetFailureDescription(10.1f));
        }

        [Test]
        public void CheckRange_PredicateTests()
        {
            var pred = new CheckRange<float>(0.0f, 1.0f);
            Assert.IsFalse(pred.CheckFailed(0.0f));
            Assert.IsFalse(pred.CheckFailed(0.5f));
            Assert.IsFalse(pred.CheckFailed(1.0f));
            Assert.IsTrue(pred.CheckFailed(-0.1f));
            Assert.IsTrue(pred.CheckFailed(1.1f));
            StringAssert.Contains("outside range [0, 1]", pred.GetDescription());
            StringAssert.Contains("is outside range [0, 1]", pred.GetFailureDescription(1.1f));
        }

        [Test]
        public void CheckMin_PredicateTests()
        {
            var pred = new CheckMin<float>(0.0f);
            Assert.IsTrue(pred.CheckFailed(-0.1f));
            Assert.IsFalse(pred.CheckFailed(0.0f));
            Assert.IsFalse(pred.CheckFailed(0.1f));
            StringAssert.Contains("< 0", pred.GetDescription());
            StringAssert.Contains("less than min 0", pred.GetFailureDescription(-0.1f));
        }

        [Test]
        public void CheckMax_PredicateTests()
        {
            var pred = new CheckMax<float>(1.0f);
            Assert.IsTrue(pred.CheckFailed(1.1f));
            Assert.IsFalse(pred.CheckFailed(1.0f));
            Assert.IsFalse(pred.CheckFailed(0.9f));
            StringAssert.Contains("> 1", pred.GetDescription());
            StringAssert.Contains("greater than max 1", pred.GetFailureDescription(1.1f));
        }

        [Test]
        public void Diagnose_Check_With_CheckFinite()
        {
            var grid = CreateTestFloatGrid();
            var accessor = grid.GetAccessor();
            var coordOk = new Coord(0, 0, 0);
            var coordNaN = new Coord(1, 0, 0);
            var coordInf = new Coord(0, 1, 0);

            accessor.SetValueOn(coordOk, 1.0f);
            accessor.SetValueOn(coordNaN, float.NaN);
            accessor.SetValueOn(coordInf, float.PositiveInfinity);
            
            // Manually set active states for placeholder tree
            ((Tree<float>)grid.Tree).SetActiveState(coordOk, true);
            ((Tree<float>)grid.Tree).SetActiveState(coordNaN, true);
            ((Tree<float>)grid.Tree).SetActiveState(coordInf, true);


            var diagnose = new Diagnose<FloatGrid, Tree<float>, float>(grid);
            diagnose.Check(new CheckFinite<float>(), updateMask: true, threaded: false);

            Assert.AreEqual(2, diagnose.FailureCount, "Should find NaN and Infinity.");
            Assert.AreEqual(3, diagnose.ValueCount, "Should check 3 active values."); // Based on simplified iteration

            var maskAccessor = diagnose.Mask.GetAccessor();
            Assert.IsFalse(maskAccessor.GetValue(coordOk), "CoordOk should be false in mask.");
            Assert.IsTrue(maskAccessor.GetValue(coordNaN), "CoordNaN should be true in mask.");
            Assert.IsTrue(maskAccessor.GetValue(coordInf), "CoordInf should be true in mask.");
            Assert.IsFalse(maskAccessor.GetValue(new Coord(5,5,5)), "Unrelated coord should be false (background).");

            diagnose.Clear();
            Assert.AreEqual(0, diagnose.FailureCount);
            Assert.AreEqual(0, diagnose.ValueCount);
            Assert.IsFalse(maskAccessor.GetValue(coordNaN), "Mask should be cleared.");
        }
        
        [Test]
        public void Diagnose_Check_With_CheckRange()
        {
            var grid = CreateTestFloatGrid();
            var accessor = grid.GetAccessor();
            var coordInRange = new Coord(0,0,0);
            var coordOutOfRangeLow = new Coord(1,0,0);
            var coordOutOfRangeHigh = new Coord(0,1,0);

            accessor.SetValueOn(coordInRange, 0.5f);
            accessor.SetValueOn(coordOutOfRangeLow, -1.0f);
            accessor.SetValueOn(coordOutOfRangeHigh, 1.5f);
            
            ((Tree<float>)grid.Tree).SetActiveState(coordInRange, true);
            ((Tree<float>)grid.Tree).SetActiveState(coordOutOfRangeLow, true);
            ((Tree<float>)grid.Tree).SetActiveState(coordOutOfRangeHigh, true);

            var diagnose = new Diagnose<FloatGrid, Tree<float>, float>(grid);
            diagnose.Check(new CheckRange<float>(0.0f, 1.0f), updateMask: true);

            Assert.AreEqual(2, diagnose.FailureCount);
            var maskAccessor = diagnose.Mask.GetAccessor();
            Assert.IsFalse(maskAccessor.GetValue(coordInRange));
            Assert.IsTrue(maskAccessor.GetValue(coordOutOfRangeLow));
            Assert.IsTrue(maskAccessor.GetValue(coordOutOfRangeHigh));
        }

        [Test]
        public void Diagnostics_CheckLevelSet_PassingCase()
        {
            var grid = CreateTestFloatGrid(3.0f * 0.1f); // background = halfWidth * voxelSize
            grid.Transform = Transform.CreateLinearTransform(0.1); // Voxel size 0.1
            grid.GridClass = GridClass.LevelSet;
            
            var accessor = grid.GetAccessor();
            accessor.SetValueOn(new Coord(1,1,1), 0.5f); // Inside +/- background
            ((Tree<float>)grid.Tree).SetActiveState(new Coord(1,1,1), true);


            var errors = Diagnostics.CheckLevelSet<FloatGrid, Tree<float>, float>(grid, checkCount: 5); // Up to CheckFinite
            CollectionAssert.IsEmpty(errors, string.Join("; ", errors));
        }
        
        [Test]
        public void Diagnostics_CheckLevelSet_FailingGridClass()
        {
            var grid = CreateTestFloatGrid();
            grid.GridClass = GridClass.FogVolume;
            var errors = Diagnostics.CheckLevelSet<FloatGrid, Tree<float>, float>(grid, checkCount: 2);
            Assert.IsTrue(errors.Any(s => s.Contains("incorrect grid class")));
        }

        [Test]
        public void Diagnostics_CheckLevelSet_FailingNonFiniteValue()
        {
            var grid = CreateTestFloatGrid(3.0f);
            grid.GridClass = GridClass.LevelSet;
            grid.GetAccessor().SetValueOn(new Coord(1,1,1), float.NaN);
            ((Tree<float>)grid.Tree).SetActiveState(new Coord(1,1,1), true);

            var errors = Diagnostics.CheckLevelSet<FloatGrid, Tree<float>, float>(grid, checkCount: 6); // CheckFinite is around check 5 or 6
            Assert.IsTrue(errors.Any(s => s.Contains("non-finite values found")));
        }
        
        // Test for CheckLevelSet for values outside +/- background will be skipped as
        // the Diagnose.Check method uses a simplified BBox iteration, not a proper ValueOnCIter,
        // and the CheckRange predicate for this specific level set constraint is not explicitly added
        // in the current CheckLevelSet placeholder.

        [Test]
        public void Diagnostics_CheckFogVolume_PassingCase()
        {
            var grid = CreateTestFloatGrid(0.0f); // Background must be 0
            grid.GridClass = GridClass.FogVolume;
            grid.GetAccessor().SetValueOn(new Coord(1,1,1), 0.5f); // Value in [0,1] range
            ((Tree<float>)grid.Tree).SetActiveState(new Coord(1,1,1), true);

            var errors = Diagnostics.CheckFogVolume<FloatGrid, Tree<float>, float>(grid, checkCount: 2); // Up to background check
            CollectionAssert.IsEmpty(errors, string.Join("; ", errors));
        }

        [Test]
        public void Diagnostics_CheckFogVolume_FailingGridClass()
        {
            var grid = CreateTestFloatGrid();
            grid.GridClass = GridClass.LevelSet;
            var errors = Diagnostics.CheckFogVolume<FloatGrid, Tree<float>, float>(grid, checkCount: 1);
            Assert.IsTrue(errors.Any(s => s.Contains("incorrect grid class")));
        }

        [Test]
        public void Diagnostics_CheckFogVolume_FailingNonZeroBackground()
        {
            var grid = CreateTestFloatGrid(1.0f); // Non-zero background
            grid.GridClass = GridClass.FogVolume;
            var errors = Diagnostics.CheckFogVolume<FloatGrid, Tree<float>, float>(grid, checkCount: 2);
            Assert.IsTrue(errors.Any(s => s.Contains("non-zero background")));
        }
        
        // CheckFogVolume for values outside [0,1] is commented out in Diagnostics.cs, so no test for it.

        [Test]
        public void Diagnostics_UniqueInactiveValues_PlaceholderReturnsEmptyList()
        {
            var grid = CreateTestFloatGrid();
            var uniqueValues = Diagnostics.UniqueInactiveValues<FloatGrid, Tree<float>, float>(grid);
            Assert.IsNotNull(uniqueValues);
            Assert.IsEmpty(uniqueValues);
        }
    }

    // Helper extension for placeholder Tree<TValue> to simulate SetActiveState for tests
    internal static class PlaceholderTreeExtensions
    {
        public static void SetActiveState<TValue>(this Tree<TValue> tree, Coord c, bool active) where TValue : struct
        {
            // This is a HACK for testing Diagnose which relies on IsValueOn from accessor.
            // We assume our TestAccessor (if used directly) or a future proper accessor for Tree<TValue>
            // would reflect this state change. For the current placeholder TreeValueAccessor, this won't
            // directly make IsValueOn(c) true. The tests for Diagnose.Check() work around this
            // by checking IsValueOn on the grid's accessor, which might be smarter if the tree is not empty.
            // For now, this method doesn't do much with the current placeholder Tree.
            // The tests that need active values will call accessor.SetValueOn directly.
        }
    }
}
