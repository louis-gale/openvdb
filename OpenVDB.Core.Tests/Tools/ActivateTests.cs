// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tools;
using OpenVDB.Core.Tree; // For ITreeValueAccessor
using OpenVDB.Core;     // For FloatGrid
using OpenVDB.Math;
using System;

namespace OpenVDB.Core.Tests.Tools
{
    [TestFixture]
    public class ActivateTests
    {
        private FloatGrid _grid;
        private ITreeValueAccessor<float> _accessor;

        // Define test coordinates
        private readonly Coord coordA = new Coord(0, 0, 0); // Target for activation/deactivation (0.5f)
        private readonly Coord coordB = new Coord(1, 0, 0); // Target for activation/deactivation (0.5f)
        private readonly Coord coordC = new Coord(0, 1, 0); // Different value (1.0f)
        private readonly Coord coordD = new Coord(1, 1, 0); // Different value (1.0f)
        private readonly Coord coordE = new Coord(0, 0, 1); // For tolerance (0.52f)
        private readonly Coord coordF = new Coord(1, 0, 1); // For tolerance (0.48f)
        private readonly Coord coordG = new Coord(0, 1, 1); // Outside tolerance (0.40f)
        private readonly Coord coordH = new Coord(1, 1, 1); // Outside tolerance (0.60f)

        // Bounding box that includes all test coordinates
        private readonly CoordBBox _testBBox = new CoordBBox(new Coord(0,0,0), new Coord(1,1,1));


        [SetUp]
        public void SetUp()
        {
            _grid = new FloatGrid(0.0f); // Background 0 for simplicity
            _grid.Transform = Transform.CreateLinearTransform(1.0);
            _accessor = _grid.GetAccessor();

            // Clear active states by setting all to background and inactive (if tree supported this well)
            // For placeholder tree, ensure values are set and then explicitly manage active state for test setup.
            // The simplified iteration in Activate.cs iterates the ActiveVoxelBoundingBox.
            // We need to ensure this BBox is what we expect, or that values are set so it becomes what we expect.
            // For these tests, we will explicitly set values and their states.
            // The simplified BBox iteration in Activate.cs will iterate this box if any value within it is active.
            // If all start inactive, EvalActiveVoxelBoundingBox might be empty.
            // To ensure the test loop runs, we can activate one cell, or rely on the fact that
            // the test loop in Activate.cs iterates from bbox.Min to bbox.Max.
            // Let's ensure the BBox used by Activate.cs is defined by our test points.
            // The current Activate.cs uses grid.EvalActiveVoxelBoundingBox().
            // So, at least one point must be active for the loop to run over a defined region.

            // Initial state for tests:
            // Coord A: active, value 0.5f
            // Coord B: inactive, value 0.5f
            // Coord C: active, value 1.0f
            // Coord D: inactive, value 1.0f
            // Coord E: active, value 0.52f (for tolerance tests)
            // Coord F: inactive, value 0.48f (for tolerance tests)
            // Coord G: inactive, value 0.40f (outside tolerance)
            // Coord H: active, value 0.60f (outside tolerance)

            // Set values
            _accessor.SetValue(coordA, 0.5f);
            _accessor.SetValue(coordB, 0.5f);
            _accessor.SetValue(coordC, 1.0f);
            _accessor.SetValue(coordD, 1.0f);
            _accessor.SetValue(coordE, 0.52f);
            _accessor.SetValue(coordF, 0.48f);
            _accessor.SetValue(coordG, 0.40f);
            _accessor.SetValue(coordH, 0.60f);

            // Set active states
            _accessor.SetValueOn(coordA, 0.5f); // Active
            _accessor.SetValueOff(coordB);      // Inactive
            _accessor.SetValueOn(coordC, 1.0f); // Active
            _accessor.SetValueOff(coordD);      // Inactive
            _accessor.SetValueOn(coordE, 0.52f);// Active
            _accessor.SetValueOff(coordF);      // Inactive
            _accessor.SetValueOff(coordG);      // Inactive
            _accessor.SetValueOn(coordH, 0.60f);// Active

            // Ensure the active bounding box covers these points for the simplified iteration in Activate.cs
            // If no points were active, EvalActiveVoxelBoundingBox would be empty.
            // By activating A, C, E, H, the bounding box will be at least _testBBox.
        }

        [Test]
        public void DoActivate_ExactMatch_ActivatesCorrectVoxels()
        {
            Activate.DoActivate(_grid, 0.5f, 0.0f, threaded: false);

            Assert.IsTrue(_accessor.IsValueOn(coordA), "A (active, 0.5f) should remain active.");
            Assert.AreEqual(0.5f, _accessor.GetValue(coordA));

            Assert.IsTrue(_accessor.IsValueOn(coordB), "B (inactive, 0.5f) should become active.");
            Assert.AreEqual(0.5f, _accessor.GetValue(coordB));

            Assert.IsTrue(_accessor.IsValueOn(coordC), "C (active, 1.0f) should remain active.");
            Assert.IsFalse(_accessor.IsValueOn(coordD), "D (inactive, 1.0f) should remain inactive.");
            Assert.IsTrue(_accessor.IsValueOn(coordE), "E (active, 0.52f) should remain active (no exact match).");
            Assert.IsFalse(_accessor.IsValueOn(coordF), "F (inactive, 0.48f) should remain inactive (no exact match).");
        }

        [Test]
        public void DoActivate_ToleranceMatch_ActivatesCorrectVoxels()
        {
            Activate.DoActivate(_grid, 0.5f, 0.05f, threaded: false); // Tolerance 0.05f -> range [0.45f, 0.55f]

            Assert.IsTrue(_accessor.IsValueOn(coordA), "A (active, 0.5f) should remain active.");
            Assert.IsTrue(_accessor.IsValueOn(coordB), "B (inactive, 0.5f) should become active.");
            Assert.IsTrue(_accessor.IsValueOn(coordE), "E (active, 0.52f) should remain active.");
            Assert.IsTrue(_accessor.IsValueOn(coordF), "F (inactive, 0.48f) should become active due to tolerance.");

            Assert.IsFalse(_accessor.IsValueOn(coordG), "G (inactive, 0.40f) should remain inactive (outside tolerance).");
            Assert.IsTrue(_accessor.IsValueOn(coordH), "H (active, 0.60f) should remain active (outside tolerance).");
            Assert.IsFalse(_accessor.IsValueOn(coordD), "D (inactive, 1.0f) should remain inactive.");
        }

        [Test]
        public void DoActivate_NoMatch_ShouldNotChangeStates()
        {
            // Capture initial states
            bool initialA = _accessor.IsValueOn(coordA);
            bool initialB = _accessor.IsValueOn(coordB);
            bool initialC = _accessor.IsValueOn(coordC);
            bool initialD = _accessor.IsValueOn(coordD);

            Activate.DoActivate(_grid, 9.9f, 0.0f, threaded: false);

            Assert.AreEqual(initialA, _accessor.IsValueOn(coordA));
            Assert.AreEqual(initialB, _accessor.IsValueOn(coordB));
            Assert.AreEqual(initialC, _accessor.IsValueOn(coordC));
            Assert.AreEqual(initialD, _accessor.IsValueOn(coordD));
        }


        [Test]
        public void DoDeactivate_ExactMatch_DeactivatesCorrectVoxels()
        {
            Activate.DoDeactivate(_grid, 0.5f, 0.0f, threaded: false);

            Assert.IsFalse(_accessor.IsValueOn(coordA), "A (active, 0.5f) should become inactive.");
            Assert.IsFalse(_accessor.IsValueOn(coordB), "B (inactive, 0.5f) should remain inactive.");

            Assert.IsTrue(_accessor.IsValueOn(coordC), "C (active, 1.0f) should remain active.");
            Assert.IsFalse(_accessor.IsValueOn(coordD), "D (inactive, 1.0f) should remain inactive.");
            Assert.IsTrue(_accessor.IsValueOn(coordE), "E (active, 0.52f) should remain active (no exact match).");
        }

        [Test]
        public void DoDeactivate_ToleranceMatch_DeactivatesCorrectVoxels()
        {
            Activate.DoDeactivate(_grid, 0.5f, 0.05f, threaded: false); // Tolerance 0.05f -> range [0.45f, 0.55f]

            Assert.IsFalse(_accessor.IsValueOn(coordA), "A (active, 0.5f) should become inactive.");
            Assert.IsFalse(_accessor.IsValueOn(coordB), "B (inactive, 0.5f) should remain inactive."); // Was already inactive
            Assert.IsFalse(_accessor.IsValueOn(coordE), "E (active, 0.52f) should become inactive due to tolerance.");
            Assert.IsFalse(_accessor.IsValueOn(coordF), "F (inactive, 0.48f) should remain inactive."); // Was already inactive

            Assert.IsTrue(_accessor.IsValueOn(coordH), "H (active, 0.60f) should remain active (outside tolerance).");
            Assert.IsTrue(_accessor.IsValueOn(coordC), "C (active, 1.0f) should remain active.");
        }

        [Test]
        public void DoDeactivate_NoMatch_ShouldNotChangeStates()
        {
            bool initialA = _accessor.IsValueOn(coordA);
            bool initialC = _accessor.IsValueOn(coordC);
            bool initialE = _accessor.IsValueOn(coordE);

            Activate.DoDeactivate(_grid, 9.9f, 0.0f, threaded: false);

            Assert.AreEqual(initialA, _accessor.IsValueOn(coordA));
            Assert.AreEqual(initialC, _accessor.IsValueOn(coordC));
            Assert.AreEqual(initialE, _accessor.IsValueOn(coordE));
        }
    }
}
