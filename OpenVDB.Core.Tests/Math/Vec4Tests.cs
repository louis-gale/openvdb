// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class Vec4Tests
    {
        [Test]
        public void Constructor_Default_ShouldBeZero()
        {
            var v = new Vec4<float>();
            Assert.AreEqual(0f, v.X);
            Assert.AreEqual(0f, v.Y);
            Assert.AreEqual(0f, v.Z);
            Assert.AreEqual(0f, v.W);
        }

        [Test]
        public void Constructor_SingleValue_ShouldInitializeAllComponents()
        {
            var v = new Vec4<float>(5f);
            Assert.AreEqual(5f, v.X);
            Assert.AreEqual(5f, v.Y);
            Assert.AreEqual(5f, v.Z);
            Assert.AreEqual(5f, v.W);
        }

        [Test]
        public void Constructor_XYZW_ShouldInitializeCorrectly()
        {
            var v = new Vec4<float>(1f, 2f, 3f, 4f);
            Assert.AreEqual(1f, v.X);
            Assert.AreEqual(2f, v.Y);
            Assert.AreEqual(3f, v.Z);
            Assert.AreEqual(4f, v.W);
        }

        [Test]
        public void Constructor_Array_ShouldInitializeCorrectly()
        {
            var arr = new float[] { 3f, 4f, 5f, 6f };
            var v = new Vec4<float>(arr);
            Assert.AreEqual(3f, v.X);
            Assert.AreEqual(4f, v.Y);
            Assert.AreEqual(5f, v.Z);
            Assert.AreEqual(6f, v.W);
        }

        [Test]
        public void Indexer_Get_ShouldReturnCorrectValues()
        {
            var v = new Vec4<double>(1.0, 2.0, 3.0, 4.0);
            Assert.AreEqual(1.0, v[0]);
            Assert.AreEqual(2.0, v[1]);
            Assert.AreEqual(3.0, v[2]);
            Assert.AreEqual(4.0, v[3]);
            Assert.Throws<IndexOutOfRangeException>(() => { var x = v[4]; });
        }

        [Test]
        public void Indexer_Set_ShouldUpdateCorrectValues()
        {
            var v = new Vec4<double>(1.0, 2.0, 3.0, 4.0);
            v[0] = 5.0;
            v[1] = 6.0;
            v[2] = 7.0;
            v[3] = 8.0;
            Assert.AreEqual(5.0, v.X);
            Assert.AreEqual(6.0, v.Y);
            Assert.AreEqual(7.0, v.Z);
            Assert.AreEqual(8.0, v.W);
            Assert.Throws<IndexOutOfRangeException>(() => { v[4] = 9.0; });
        }

        [Test]
        public void Add_Vector_ShouldReturnCorrectSum()
        {
            var v1 = new Vec4<float>(1f, 2f, 3f, 4f);
            var v2 = new Vec4<float>(5f, 6f, 7f, 8f);
            var result = v1 + v2;
            Assert.AreEqual(6f, result.X);
            Assert.AreEqual(8f, result.Y);
            Assert.AreEqual(10f, result.Z);
            Assert.AreEqual(12f, result.W);
        }

        [Test]
        public void Dot_Product_ShouldBeCorrect()
        {
            var v1 = new Vec4<float>(1f, 2f, 3f, 4f);
            var v2 = new Vec4<float>(5f, 6f, 7f, 8f);
            float dot = v1.Dot(v2); // 1*5 + 2*6 + 3*7 + 4*8 = 5 + 12 + 21 + 32 = 70
            Assert.AreEqual(70f, dot);
        }

        [Test]
        public void Length_ShouldReturnCorrectLength()
        {
            var v = new Vec4<double>(1.0, 2.0, 3.0, 4.0);
            // 1 + 4 + 9 + 16 = 30
            Assert.AreEqual(System.Math.Sqrt(30.0), v.Length(), 1e-9);
        }

        [Test]
        public void Normalize_ShouldResultInUnitVector()
        {
            var v = new Vec4<double>(1.0, 2.0, 3.0, 4.0);
            double len = System.Math.Sqrt(30.0);
            v.Normalize();
            Assert.AreEqual(1.0, v.Length(), 1e-9);
            Assert.AreEqual(1.0 / len, v.X, 1e-9);
            Assert.AreEqual(2.0 / len, v.Y, 1e-9);
            Assert.AreEqual(3.0 / len, v.Z, 1e-9);
            Assert.AreEqual(4.0 / len, v.W, 1e-9);
        }
    }
}
