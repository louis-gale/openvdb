// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class CoordTests
    {
        [Test]
        public void Constructor_Default_ShouldBeZero()
        {
            var c = new Coord();
            Assert.AreEqual(0, c.X);
            Assert.AreEqual(0, c.Y);
            Assert.AreEqual(0, c.Z);
        }

        [Test]
        public void Constructor_SingleValue_ShouldInitializeAllComponents()
        {
            var c = new Coord(5);
            Assert.AreEqual(5, c.X);
            Assert.AreEqual(5, c.Y);
            Assert.AreEqual(5, c.Z);
        }

        [Test]
        public void Constructor_XYZ_ShouldInitializeCorrectly()
        {
            var c = new Coord(1, 2, 3);
            Assert.AreEqual(1, c.X);
            Assert.AreEqual(2, c.Y);
            Assert.AreEqual(3, c.Z);
        }

        [Test]
        public void Constructor_Vec3i_ShouldInitializeCorrectly()
        {
            var v = new Vec3<int>(4, 5, 6);
            var c = new Coord(v);
            Assert.AreEqual(4, c.X);
            Assert.AreEqual(5, c.Y);
            Assert.AreEqual(6, c.Z);
        }

        [Test]
        public void Constructor_IntArray_ShouldInitializeCorrectly()
        {
            var arr = new int[] { 7, 8, 9 };
            var c = new Coord(arr);
            Assert.AreEqual(7, c.X);
            Assert.AreEqual(8, c.Y);
            Assert.AreEqual(9, c.Z);
        }


        [Test]
        public void StaticMinMaxValues_ShouldReturnCorrectLimits()
        {
            Assert.AreEqual(int.MinValue, Coord.MinValue.X);
            Assert.AreEqual(int.MaxValue, Coord.MaxValue.X);
        }

        [Test]
        public void StaticConversion_RoundFloorCeil_ShouldWork()
        {
            var vf = new Vec3<float>(1.2f, 2.8f, 3.5f);
            Assert.AreEqual(new Coord(1, 3, 4), Coord.Round(vf)); // .5 rounds to nearest even in C# default, or away from zero
                                                                  // C++ Round might be different. Assuming standard rounding for now.
                                                                  // Let's assume Round is like Math.Round with MidpointRounding.AwayFromZero for closer C++ match
                                                                  // For 3.5 C# Math.Round = 4. For -3.5 C# Math.Round = -4
                                                                  // OpenVDB's Round(x) is floor(x + 0.5)
            Assert.AreEqual(new Coord(1, 3, 3), new Coord((int)System.Math.Floor(vf.X + 0.5f), (int)System.Math.Floor(vf.Y + 0.5f), (int)System.Math.Floor(vf.Z + 0.5f) ));


            Assert.AreEqual(new Coord(1, 2, 3), Coord.Floor(vf));
            Assert.AreEqual(new Coord(2, 3, 4), Coord.Ceil(vf));
        }

        [Test]
        public void Reset_ShouldUpdateValues()
        {
            var c = new Coord(1,2,3);
            c.Reset(4,5,6);
            Assert.AreEqual(new Coord(4,5,6), c);
            c.Reset(7);
            Assert.AreEqual(new Coord(7,7,7), c);
        }

        [Test]
        public void Offset_And_OffsetBy_ShouldWork()
        {
            var c = new Coord(1,2,3);
            c.Offset(1,1,1);
            Assert.AreEqual(new Coord(2,3,4), c);

            var c2 = c.OffsetBy(2,2,2);
            Assert.AreEqual(new Coord(4,5,6), c2);
            Assert.AreEqual(new Coord(2,3,4), c); // c should be unchanged by OffsetBy

            c.Offset(3);
            Assert.AreEqual(new Coord(5,6,7), c);

            var c3 = c.OffsetBy(1);
            Assert.AreEqual(new Coord(6,7,8), c3);
        }

        [Test]
        public void Indexer_ShouldAccessCorrectComponents()
        {
            var c = new Coord(10, 20, 30);
            Assert.AreEqual(10, c[0]);
            Assert.AreEqual(20, c[1]);
            Assert.AreEqual(30, c[2]);
            c[0] = 15;
            Assert.AreEqual(15, c.X);
            Assert.Throws<IndexOutOfRangeException>(() => { var x = c[3]; });
        }

        [Test]
        public void ArithmeticOperators_ShouldComputeCorrectly()
        {
            var c1 = new Coord(1, 2, 3);
            var c2 = new Coord(4, 5, 6);
            Assert.AreEqual(new Coord(5, 7, 9), c1 + c2);
            Assert.AreEqual(new Coord(-3, -3, -3), c1 - c2);
            Assert.AreEqual(new Coord(-1, -2, -3), -c1);
            Assert.AreEqual(new Coord(2, 4, 6), c1 * 2);
            Assert.AreEqual(new Coord(2, 4, 6), 2 * c1);
            Assert.AreEqual(new Coord(0, 1, 1), c1 / 2); // Integer division
        }

        [Test]
        public void BitwiseOperators_ShouldComputeCorrectly()
        {
            var c = new Coord(5, 10, 15); // 0101, 1010, 1111
            Assert.AreEqual(new Coord(2, 5, 7), c >> 1);     // 0010, 0101, 0111
            Assert.AreEqual(new Coord(10, 20, 30), c << 1);  // 1010, 10100, 11110
            Assert.AreEqual(new Coord(5 & 3, 10 & 3, 15 & 3), c & 3); // 0101&0011=1, 1010&0011=2, 1111&0011=3 -> (1,2,3)
            Assert.AreEqual(new Coord(1, 2, 3), c & 3);
            Assert.AreEqual(new Coord(5 | 3, 10 | 3, 15 | 3), c | 3); // 0101|0011=7, 1010|0011=11, 1111|0011=15 -> (7,11,15)
            Assert.AreEqual(new Coord(7, 11, 15), c | 3);
        }

        [Test]
        public void ComparisonOperators_ShouldCompareLexicographically()
        {
            var c1 = new Coord(1, 2, 3);
            var c2 = new Coord(1, 2, 4);
            var c3 = new Coord(1, 3, 0);
            var c4 = new Coord(2, 0, 0);
            var c1_copy = new Coord(1,2,3);

            Assert.IsTrue(c1 == c1_copy);
            Assert.IsFalse(c1 == c2);
            Assert.IsTrue(c1 != c2);

            Assert.IsTrue(c1 < c2);
            Assert.IsTrue(c1 < c3);
            Assert.IsTrue(c1 < c4);
            Assert.IsFalse(c2 < c1);

            Assert.IsTrue(c1 <= c2);
            Assert.IsTrue(c1 <= c1_copy);
            Assert.IsFalse(c2 <= c1);

            Assert.IsTrue(c2 > c1);
            Assert.IsFalse(c1 > c2);

            Assert.IsTrue(c2 >= c1);
            Assert.IsTrue(c1_copy >= c1);
            Assert.IsFalse(c1 >= c2);
        }

        [Test]
        public void MinMaxComponent_ShouldWorkCorrectly()
        {
            var c1 = new Coord(1, 5, 2);
            var c2 = new Coord(3, 2, 4);

            var minStatic = Coord.MinComponent(c1, c2);
            Assert.AreEqual(new Coord(1, 2, 2), minStatic);

            var maxStatic = Coord.MaxComponent(c1, c2);
            Assert.AreEqual(new Coord(3, 5, 4), maxStatic);

            var c1Copy = c1;
            c1Copy.MinComponent(c2);
            Assert.AreEqual(new Coord(1, 2, 2), c1Copy);

            c1Copy = c1; // reset
            c1Copy.MaxComponent(c2);
            Assert.AreEqual(new Coord(3, 5, 4), c1Copy);
        }

        [Test]
        public void MinMaxIndex_ShouldReturnCorrectIndex()
        {
            Assert.AreEqual(0, new Coord(1,2,3).MinIndex());
            Assert.AreEqual(1, new Coord(5,2,3).MinIndex());
            Assert.AreEqual(2, new Coord(5,4,3).MinIndex());
            Assert.AreEqual(0, new Coord(1,1,3).MinIndex()); // Tie-breaking: first index

            Assert.AreEqual(2, new Coord(1,2,3).MaxIndex());
            Assert.AreEqual(0, new Coord(5,2,3).MaxIndex());
            Assert.AreEqual(1, new Coord(1,4,3).MaxIndex());
            Assert.AreEqual(0, new Coord(3,3,1).MaxIndex()); // Tie-breaking: first index
        }

        [Test]
        public void Length_And_LengthSqr_ShouldBeCorrect()
        {
            var c = new Coord(2,3,6); // 4 + 9 + 36 = 49
            Assert.AreEqual(49, c.LengthSqr());
            Assert.AreEqual(7.0, c.Length(), 1e-9);
        }

        [Test]
        public void Abs_ShouldReturnAbsoluteValues()
        {
            var c = new Coord(-1, 2, -3);
            Assert.AreEqual(new Coord(1,2,3), c.Abs());
        }
    }
}
