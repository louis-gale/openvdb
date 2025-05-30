// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class Vec3Tests
    {
        [Test]
        public void Constructor_Default_ShouldBeZero()
        {
            var v = new Vec3<float>();
            Assert.AreEqual(0f, v.X);
            Assert.AreEqual(0f, v.Y);
            Assert.AreEqual(0f, v.Z);
        }

        [Test]
        public void Constructor_SingleValue_ShouldInitializeAllComponents()
        {
            var v = new Vec3<float>(5f);
            Assert.AreEqual(5f, v.X);
            Assert.AreEqual(5f, v.Y);
            Assert.AreEqual(5f, v.Z);
        }

        [Test]
        public void Constructor_XYZ_ShouldInitializeCorrectly()
        {
            var v = new Vec3<float>(1f, 2f, 3f);
            Assert.AreEqual(1f, v.X);
            Assert.AreEqual(2f, v.Y);
            Assert.AreEqual(3f, v.Z);
        }

        [Test]
        public void Constructor_Array_ShouldInitializeCorrectly()
        {
            var arr = new float[] { 3f, 4f, 5f };
            var v = new Vec3<float>(arr);
            Assert.AreEqual(3f, v.X);
            Assert.AreEqual(4f, v.Y);
            Assert.AreEqual(5f, v.Z);
        }

        [Test]
        public void Indexer_Get_ShouldReturnCorrectValues()
        {
            var v = new Vec3<double>(1.0, 2.0, 3.0);
            Assert.AreEqual(1.0, v[0]);
            Assert.AreEqual(2.0, v[1]);
            Assert.AreEqual(3.0, v[2]);
            Assert.Throws<IndexOutOfRangeException>(() => { var x = v[3]; });
        }

        [Test]
        public void Indexer_Set_ShouldUpdateCorrectValues()
        {
            var v = new Vec3<double>(1.0, 2.0, 3.0);
            v[0] = 4.0;
            v[1] = 5.0;
            v[2] = 6.0;
            Assert.AreEqual(4.0, v.X);
            Assert.AreEqual(5.0, v.Y);
            Assert.AreEqual(6.0, v.Z);
            Assert.Throws<IndexOutOfRangeException>(() => { v[3] = 7.0; });
        }
        
        [Test]
        public void Add_Vector_ShouldReturnCorrectSum()
        {
            var v1 = new Vec3<float>(1f, 2f, 3f);
            var v2 = new Vec3<float>(4f, 5f, 6f);
            var result = v1 + v2;
            Assert.AreEqual(5f, result.X);
            Assert.AreEqual(7f, result.Y);
            Assert.AreEqual(9f, result.Z);
        }

        [Test]
        public void Cross_Product_ShouldBeCorrect()
        {
            var v1 = new Vec3<float>(1f, 0f, 0f); // X-axis
            var v2 = new Vec3<float>(0f, 1f, 0f); // Y-axis
            var result = v1.Cross(v2);           // Should be Z-axis
            Assert.AreEqual(0f, result.X);
            Assert.AreEqual(0f, result.Y);
            Assert.AreEqual(1f, result.Z);

            var v3 = new Vec3<float>(2f, 3f, 4f);
            var v4 = new Vec3<float>(5f, 6f, 7f);
            var cross = v3.Cross(v4);
            // 3*7 - 4*6 = 21 - 24 = -3
            // 4*5 - 2*7 = 20 - 14 = 6
            // 2*6 - 3*5 = 12 - 15 = -3
            Assert.AreEqual(-3f, cross.X);
            Assert.AreEqual(6f, cross.Y);
            Assert.AreEqual(-3f, cross.Z);
        }
        
        [Test]
        public void Length_ShouldReturnCorrectLength()
        {
            var v = new Vec3<double>(2.0, 3.0, 6.0); // 4 + 9 + 36 = 49, sqrt is 7
            Assert.AreEqual(7.0, v.Length(), 1e-9);
        }

        [Test]
        public void Normalize_ShouldResultInUnitVector()
        {
            var v = new Vec3<double>(2.0, 3.0, 6.0);
            v.Normalize();
            Assert.AreEqual(1.0, v.Length(), 1e-9);
            Assert.AreEqual(2.0 / 7.0, v.X, 1e-9);
            Assert.AreEqual(3.0 / 7.0, v.Y, 1e-9);
            Assert.AreEqual(6.0 / 7.0, v.Z, 1e-9);
        }
        
        [Test]
        public void Dot_Product_ShouldBeCorrect()
        {
            var v1 = new Vec3<float>(1f, 2f, 3f);
            var v2 = new Vec3<float>(4f, 5f, 6f);
            float dot = v1.Dot(v2); // 1*4 + 2*5 + 3*6 = 4 + 10 + 18 = 32
            Assert.AreEqual(32f, dot);
        }
    }
}
