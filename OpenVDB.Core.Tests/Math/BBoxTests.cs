// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class BBoxTests
    {
        [Test]
        public void CoordBBox_DefaultConstructor_ShouldBeEmpty()
        {
            var bbox = BBox<Coord, int>.CreateEmpty(); // Using Coord, int
            Assert.IsTrue(bbox.IsEmpty);
            Assert.AreEqual(Coord.MaxValue, bbox.Min); // Specific to CreateEmpty for Coord
            Assert.AreEqual(Coord.MinValue, bbox.Max);
        }

        [Test]
        public void BBoxD_DefaultConstructor_ShouldBeEmpty()
        {
            var bbox = BBox<Vec3<double>, double>.CreateEmpty(); // Using Vec3<double>, double
            Assert.IsTrue(bbox.IsEmpty);
            Assert.AreEqual(new Vec3<double>(double.PositiveInfinity), bbox.Min);
            Assert.AreEqual(new Vec3<double>(double.NegativeInfinity), bbox.Max);
        }


        [Test]
        public void Constructor_MinMax_ShouldInitializeCorrectly()
        {
            var min = new Coord(1, 2, 3);
            var max = new Coord(4, 5, 6);
            var bbox = new BBox<Coord, int>(min, max);
            Assert.AreEqual(min, bbox.Min);
            Assert.AreEqual(max, bbox.Max);
            Assert.IsFalse(bbox.IsEmpty);
        }

        [Test]
        public void Constructor_MinMaxSorted_ShouldSortIfNotSorted()
        {
            var p1 = new Vec3<float>(5,1,6);
            var p2 = new Vec3<float>(0,8,2);
            var bbox = new BBox<Vec3<float>, float>(p1, p2, sorted: false);

            Assert.AreEqual(0, bbox.Min.X); Assert.AreEqual(1, bbox.Min.Y); Assert.AreEqual(2, bbox.Min.Z);
            Assert.AreEqual(5, bbox.Max.X); Assert.AreEqual(8, bbox.Max.Y); Assert.AreEqual(6, bbox.Max.Z);
        }

        [Test]
        public void Constructor_MinLength_ShouldCreateCube()
        {
            var min = new Coord(1,1,1);
            int length = 3; // This means max will be min + (length-1) for integral types
            var bbox = new BBox<Coord, int>(min, length);
            Assert.AreEqual(min, bbox.Min);
            Assert.AreEqual(new Coord(1+3-1, 1+3-1, 1+3-1), bbox.Max); // (3,3,3)
            Assert.AreEqual(new Coord(3,3,3), bbox.Extents());
        }

        [Test]
        public void Constructor_MinLength_Float_ShouldCreateCube()
        {
            var min = new Vec3<float>(1.0f, 1.0f, 1.0f);
            float length = 3.0f; // Max will be min + length for float types
            var bbox = new BBox<Vec3<float>, float>(min, length);
            Assert.AreEqual(min, bbox.Min);
            Assert.AreEqual(new Vec3<float>(1.0f + 3.0f, 1.0f + 3.0f, 1.0f + 3.0f), bbox.Max); // (4,4,4)
            var ext = bbox.Extents(); // Max - Min for float
            Assert.AreEqual(3.0f, ext.X, 1e-6f);
            Assert.AreEqual(3.0f, ext.Y, 1e-6f);
            Assert.AreEqual(3.0f, ext.Z, 1e-6f);
        }


        [Test]
        public void Extents_And_Volume_CoordBBox_ShouldBeCorrect()
        {
            var min = new Coord(0, 0, 0);
            var max = new Coord(2, 3, 4); // Dimensions are (3,4,5)
            var bbox = new CoordBBox(min, max);

            var extents = bbox.Extents();
            Assert.AreEqual(3, extents.X); // max.X - min.X + 1 for integral
            Assert.AreEqual(4, extents.Y);
            Assert.AreEqual(5, extents.Z);
            Assert.AreEqual(3 * 4 * 5, bbox.Volume());
        }

        [Test]
        public void Extents_And_Volume_BBoxD_ShouldBeCorrect()
        {
            var min = new Vec3<double>(0,0,0);
            var max = new Vec3<double>(2,3,4);
            var bbox = new BBoxD(min, max);

            var extents = bbox.Extents(); // max - min for float
            Assert.AreEqual(2.0, extents.X, 1e-9);
            Assert.AreEqual(3.0, extents.Y, 1e-9);
            Assert.AreEqual(4.0, extents.Z, 1e-9);
            Assert.AreEqual(2.0 * 3.0 * 4.0, bbox.Volume(), 1e-9);
        }

        [Test]
        public void IsInside_Point_ShouldReturnCorrectly()
        {
            var bbox = new CoordBBox(new Coord(0,0,0), new Coord(5,5,5));
            Assert.IsTrue(bbox.IsInside(new Coord(1,1,1)));
            Assert.IsTrue(bbox.IsInside(new Coord(0,0,0)));
            Assert.IsTrue(bbox.IsInside(new Coord(5,5,5)));
            Assert.IsFalse(bbox.IsInside(new Coord(6,5,5)));
            Assert.IsFalse(bbox.IsInside(new Coord(-1,5,5)));
        }

        [Test]
        public void IsInside_BBox_ShouldReturnCorrectly()
        {
            var outer = new CoordBBox(new Coord(0,0,0), new Coord(10,10,10));
            var inner = new CoordBBox(new Coord(1,1,1), new Coord(5,5,5));
            var partiallyOverlapping = new CoordBBox(new Coord(5,5,5), new Coord(15,15,15));
            var outside = new CoordBBox(new Coord(20,20,20), new Coord(30,30,30));

            Assert.IsTrue(outer.IsInside(inner));
            Assert.IsFalse(outer.IsInside(partiallyOverlapping)); // For full IsInside, other must be fully contained
            Assert.IsFalse(inner.IsInside(outer));
            Assert.IsFalse(outer.IsInside(outside));
        }

        [Test]
        public void HasOverlap_ShouldReturnCorrectly()
        {
            var b1 = new CoordBBox(new Coord(0,0,0), new Coord(5,5,5));
            var b2 = new CoordBBox(new Coord(3,3,3), new Coord(7,7,7)); // Overlaps
            var b3 = new CoordBBox(new Coord(6,6,6), new Coord(10,10,10)); // Does not overlap b1
            var b4 = new CoordBBox(new Coord(0,0,0), new Coord(2,2,2)); // b4 is inside b1

            Assert.IsTrue(b1.HasOverlap(b2));
            Assert.IsTrue(b2.HasOverlap(b1));
            Assert.IsFalse(b1.HasOverlap(b3));
            Assert.IsFalse(b3.HasOverlap(b1));
            Assert.IsTrue(b1.HasOverlap(b4));
            Assert.IsTrue(b4.HasOverlap(b1));
        }

        [Test]
        public void Expand_ByPoint_ShouldEnclosePoint()
        {
            var bbox = new CoordBBox(new Coord(0,0,0), new Coord(2,2,2));
            bbox.Expand(new Coord(5,1,1));
            Assert.AreEqual(new Coord(0,0,0), bbox.Min);
            Assert.AreEqual(new Coord(5,2,2), bbox.Max);

            bbox.Expand(new Coord(-1,3,0));
            Assert.AreEqual(new Coord(-1,0,0), bbox.Min);
            Assert.AreEqual(new Coord(5,3,2), bbox.Max);
        }

        [Test]
        public void Expand_ByBBox_ShouldEncloseOtherBBox()
        {
            var b1 = new CoordBBox(new Coord(0,0,0), new Coord(2,2,2));
            var b2 = new CoordBBox(new Coord(1,1,1), new Coord(5,5,5));
            b1.Expand(b2);
            Assert.AreEqual(new Coord(0,0,0), b1.Min);
            Assert.AreEqual(new Coord(5,5,5), b1.Max);
        }

        [Test]
        public void Expand_ByPadding_ShouldExpandAllSides()
        {
            var bbox = new CoordBBox(new Coord(0,0,0), new Coord(2,2,2));
            bbox.Expand(1); // Pad by 1
            Assert.AreEqual(new Coord(-1,-1,-1), bbox.Min);
            Assert.AreEqual(new Coord(3,3,3), bbox.Max);
        }


        [Test]
        public void Translate_ShouldMoveMinAndMax()
        {
            var bbox = new CoordBBox(new Coord(0,0,0), new Coord(2,2,2));
            bbox.Translate(new Coord(10,20,30));
            Assert.AreEqual(new Coord(10,20,30), bbox.Min);
            Assert.AreEqual(new Coord(12,22,32), bbox.Max);
        }
    }
}
