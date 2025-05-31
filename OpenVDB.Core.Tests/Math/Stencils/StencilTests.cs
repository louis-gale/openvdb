// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Stencils;
using OpenVDB.Core.Tree; // For ITreeValueAccessor
using OpenVDB.Core;     // For FloatGrid
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVDB.Core.Tests.Math.Stencils
{
    [TestFixture]
    public class StencilTests
    {
        private FloatGrid _testGrid;
        private ITreeValueAccessor<float> _accessor;

        // Test function: value = x + y*10 + z*100
        private float TestGridValueFunction(int x, int y, int z) => (float)(x + y * 10 + z * 100);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Create a grid large enough for stencil operations around a center point.
            // E.g., if max stencil radius or extent is 3, need at least 3 cells padding.
            // Let's use a domain from -5 to 5 for each axis.
            var domain = new CoordBBox(new Coord(-5, -5, -5), new Coord(5, 5, 5));
            _testGrid = new FloatGrid(0.0f); // Background 0
            _accessor = _testGrid.GetAccessor();

            for (int x = domain.Min.X; x <= domain.Max.X; ++x)
            {
                for (int y = domain.Min.Y; y <= domain.Max.Y; ++y)
                {
                    for (int z = domain.Min.Z; z <= domain.Max.Z; ++z)
                    {
                        _accessor.SetValue(new Coord(x, y, z), TestGridValueFunction(x, y, z));
                    }
                }
            }
        }

        [Test]
        public void StencilBase_Functionality_IsCorrect()
        {
            var center = new Coord(1, 2, 3);
            // Use SevenPointStencil to test StencilBase behavior
            var stencil = new SevenPointStencil<float>(_accessor, center);

            // Test GetCenterCoord and MoveTo
            Assert.AreEqual(center, stencil.GetCenterCoord());
            var newCenter = new Coord(0, -1, 1);
            stencil.MoveTo(newCenter);
            Assert.AreEqual(newCenter, stencil.GetCenterCoord());

            // Test GetAccessor
            Assert.AreSame(_accessor, stencil.GetAccessor());

            // Test GetValue(di, dj, dk) and GetValue(Coord offset)
            // Stencil is now at (0, -1, 1)
            // Value at (0, -1, 1) is 0 + (-1)*10 + 1*100 = -10 + 100 = 90
            Assert.AreEqual(TestGridValueFunction(0, -1, 1), stencil.GetValue(0, 0, 0));
            Assert.AreEqual(TestGridValueFunction(0, -1, 1), stencil.CenterValue());

            // Value at (0+1, -1+0, 1+0) = (1, -1, 1) => 1 + (-1)*10 + 1*100 = 1 - 10 + 100 = 91
            Assert.AreEqual(TestGridValueFunction(1, -1, 1), stencil.GetValue(1, 0, 0));
            Assert.AreEqual(TestGridValueFunction(1, -1, 1), stencil.GetValue(new Coord(1, 0, 0)));
        }

        [Test]
        public void SevenPointStencil_AccessMethods_ReturnCorrectValues()
        {
            var center = new Coord(1, 1, 1); // Value = 1 + 10 + 100 = 111
            var stencil = new SevenPointStencil<float>(_accessor, center);

            Assert.AreEqual(TestGridValueFunction(1, 1, 1), stencil.CenterValue());
            Assert.AreEqual(TestGridValueFunction(2, 1, 1), stencil.PlusX());    // 1+100 + 10 + 2 = 113
            Assert.AreEqual(TestGridValueFunction(0, 1, 1), stencil.MinusX());   // 1+100 + 10 + 0 = 111
            Assert.AreEqual(TestGridValueFunction(1, 2, 1), stencil.PlusY());    // 1+100 + 20 + 1 = 122
            Assert.AreEqual(TestGridValueFunction(1, 0, 1), stencil.MinusY());   // 1+100 + 0 + 1 = 102
            Assert.AreEqual(TestGridValueFunction(1, 1, 2), stencil.PlusZ());    // 1+200 + 10 + 1 = 212
            Assert.AreEqual(TestGridValueFunction(1, 1, 0), stencil.MinusZ());   // 1+0 + 10 + 1 = 12

            // Move and test again
            stencil.MoveTo(new Coord(0,0,0)); // Value = 0
            Assert.AreEqual(TestGridValueFunction(0,0,0), stencil.CenterValue());
            Assert.AreEqual(TestGridValueFunction(1,0,0), stencil.PlusX());
            Assert.AreEqual(TestGridValueFunction(0,0,-1), stencil.MinusZ());
        }

        [Test]
        public void BoxStencil_PropertiesAndGetValue_WorkCorrectly()
        {
            var center = new Coord(1, 0, -1); // Value = 1 + 0*10 + (-1)*100 = 1 - 100 = -99
            int radius = 1; // 3x3x3 box
            var stencil = new BoxStencil<float>(_accessor, radius, center);

            Assert.AreEqual(radius, stencil.GetRadius());
            Assert.AreEqual(2 * radius + 1, stencil.GetSize());
            Assert.AreEqual(new Coord(-radius, -radius, -radius), stencil.GetMin());
            Assert.AreEqual(new Coord(radius, radius, radius), stencil.GetMax());

            // Test Center
            Assert.AreEqual(TestGridValueFunction(1, 0, -1), stencil.CenterValue());
            Assert.AreEqual(TestGridValueFunction(1, 0, -1), stencil.GetValue(0,0,0));

            // Test Corners of the box (local offsets (1,1,1) and (-1,-1,-1))
            // Global: (1+1, 0+1, -1+1) = (2,1,0) => 2 + 10 + 0 = 12
            Assert.AreEqual(TestGridValueFunction(2, 1, 0), stencil.GetValue(1,1,1));
            // Global: (1-1, 0-1, -1-1) = (0,-1,-2) => 0 - 10 - 200 = -210
            Assert.AreEqual(TestGridValueFunction(0,-1,-2), stencil.GetValue(-1,-1,-1));

            // Test point outside stencil (using GetValue which might access outside defined box if accessor allows)
            // This tests StencilBase.GetValue, not specifically if it's "in" the box.
            Assert.AreEqual(TestGridValueFunction(center.X + radius + 1, center.Y, center.Z), stencil.GetValue(radius + 1, 0, 0));
        }

        [Test]
        public void BoxStencil_GetAllLocalCoords_ReturnsCorrectOffsets()
        {
            int radius = 1;
            var stencil = new BoxStencil<float>(_accessor, radius); // Center at (0,0,0) by default
            var localCoords = stencil.GetAllLocalCoords().ToList();

            int expectedCount = (2 * radius + 1) * (2 * radius + 1) * (2 * radius + 1);
            Assert.AreEqual(expectedCount, localCoords.Count);

            var expectedSet = new HashSet<Coord>();
            for(int i = -radius; i <= radius; i++)
                for(int j = -radius; j <= radius; j++)
                    for(int k = -radius; k <= radius; k++)
                        expectedSet.Add(new Coord(i,j,k));

            foreach(var coord in localCoords)
            {
                Assert.IsTrue(expectedSet.Contains(coord), $"Unexpected local coord {coord} in BoxStencil iteration.");
            }
            Assert.AreEqual(expectedSet.Count, localCoords.Distinct().Count(), "Duplicate coords found or not all expected coords present.");
        }

        [Test]
        public void BoxStencil_GetAllValues_ReturnsCorrectValues()
        {
            var center = new Coord(1,1,1);
            int radius = 1;
            var stencil = new BoxStencil<float>(_accessor, radius, center);
            var values = stencil.GetAllValues().ToList();

            int expectedCount = (2 * radius + 1) * (2 * radius + 1) * (2 * radius + 1);
            Assert.AreEqual(expectedCount, values.Count);

            // Check a few values explicitly
            // Center value: (1,1,1) -> 1+10+100 = 111
            // Min corner: center + (-1,-1,-1) = (0,0,0) -> 0
            // Max corner: center + (1,1,1) = (2,2,2) -> 2+20+200 = 222
            Assert.IsTrue(values.Contains(TestGridValueFunction(1,1,1)));
            Assert.IsTrue(values.Contains(TestGridValueFunction(0,0,0)));
            Assert.IsTrue(values.Contains(TestGridValueFunction(2,2,2)));

            // Verify all values match direct accessor calls
            int idx = 0;
            for (int k_offset = -radius; k_offset <= radius; ++k_offset)
            {
                for (int j_offset = -radius; j_offset <= radius; ++j_offset)
                {
                    for (int i_offset = -radius; i_offset <= radius; ++i_offset)
                    {
                        float expectedValue = _accessor.GetValue(center.OffsetBy(i_offset, j_offset, k_offset));
                        Assert.AreEqual(expectedValue, values[idx++]);
                    }
                }
            }
        }

        [Test]
        public void BoxStencil_SetRadius_UpdatesProperties()
        {
            var stencil = new BoxStencil<float>(_accessor, 1);
            Assert.AreEqual(1, stencil.GetRadius());
            Assert.AreEqual(3, stencil.GetSize());

            stencil.SetRadius(2);
            Assert.AreEqual(2, stencil.GetRadius());
            Assert.AreEqual(5, stencil.GetSize());

            Assert.Throws<ArgumentOutOfRangeException>(() => stencil.SetRadius(-1));
        }
    }
}
