// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math; // Assuming this is the namespace for Vec2<T>

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class Vec2Tests
    {
        [Test]
        public void Constructor_Default_ShouldBeZero()
        {
            var v = new Vec2<float>();
            Assert.AreEqual(0f, v.X);
            Assert.AreEqual(0f, v.Y);
        }

        [Test]
        public void Constructor_SingleValue_ShouldInitializeAllComponents()
        {
            var v = new Vec2<float>(5f);
            Assert.AreEqual(5f, v.X);
            Assert.AreEqual(5f, v.Y);
        }

        [Test]
        public void Constructor_XY_ShouldInitializeCorrectly()
        {
            var v = new Vec2<float>(1f, 2f);
            Assert.AreEqual(1f, v.X);
            Assert.AreEqual(2f, v.Y);
        }

        [Test]
        public void Constructor_Array_ShouldInitializeCorrectly()
        {
            var arr = new float[] { 3f, 4f };
            var v = new Vec2<float>(arr);
            Assert.AreEqual(3f, v.X);
            Assert.AreEqual(4f, v.Y);
        }
        
        [Test]
        public void Indexer_Get_ShouldReturnCorrectValues()
        {
            var v = new Vec2<double>(1.0, 2.0);
            Assert.AreEqual(1.0, v[0]);
            Assert.AreEqual(2.0, v[1]);
            Assert.Throws<IndexOutOfRangeException>(() => { var x = v[2]; });
        }

        [Test]
        public void Indexer_Set_ShouldUpdateCorrectValues()
        {
            var v = new Vec2<double>(1.0, 2.0);
            v[0] = 3.0;
            v[1] = 4.0;
            Assert.AreEqual(3.0, v.X);
            Assert.AreEqual(4.0, v.Y);
            Assert.Throws<IndexOutOfRangeException>(() => { v[2] = 5.0; });
        }

        [Test]
        public void StaticZero_ShouldReturnZeroVector()
        {
            var v = Vec2<float>.Zero();
            Assert.AreEqual(0f, v.X);
            Assert.AreEqual(0f, v.Y);
        }

        [Test]
        public void StaticOnes_ShouldReturnOnesVector()
        {
            var v = Vec2<double>.Ones();
            Assert.AreEqual(1.0, v.X);
            Assert.AreEqual(1.0, v.Y);
        }

        [Test]
        public void Add_Vector_ShouldReturnCorrectSum()
        {
            var v1 = new Vec2<float>(1f, 2f);
            var v2 = new Vec2<float>(3f, 4f);
            var result = v1 + v2;
            Assert.AreEqual(4f, result.X);
            Assert.AreEqual(6f, result.Y);
        }

        [Test]
        public void Add_Scalar_ShouldReturnCorrectSum()
        {
            var v1 = new Vec2<float>(1f, 2f);
            var result = v1 + 3f;
            Assert.AreEqual(4f, result.X);
            Assert.AreEqual(5f, result.Y);
        }


        [Test]
        public void Subtract_Vector_ShouldReturnCorrectDifference()
        {
            var v1 = new Vec2<float>(5f, 3f);
            var v2 = new Vec2<float>(1f, 2f);
            var result = v1 - v2;
            Assert.AreEqual(4f, result.X);
            Assert.AreEqual(1f, result.Y);
        }

        [Test]
        public void Multiply_Scalar_ShouldReturnCorrectProduct()
        {
            var v = new Vec2<float>(2f, 3f);
            var result = v * 2f;
            Assert.AreEqual(4f, result.X);
            Assert.AreEqual(6f, result.Y);
        }
        
        [Test]
        public void Multiply_Vector_ShouldReturnComponentWiseProduct()
        {
            var v1 = new Vec2<float>(2f, 3f);
            var v2 = new Vec2<float>(4f, 5f);
            var result = v1 * v2;
            Assert.AreEqual(8f, result.X);
            Assert.AreEqual(15f, result.Y);
        }

        [Test]
        public void Divide_Scalar_ShouldReturnCorrectQuotient()
        {
            var v = new Vec2<float>(4f, 6f);
            var result = v / 2f;
            Assert.AreEqual(2f, result.X);
            Assert.AreEqual(3f, result.Y);
        }
        
        [Test]
        public void Divide_Vector_ShouldReturnComponentWiseQuotient()
        {
            var v1 = new Vec2<float>(8f, 15f);
            var v2 = new Vec2<float>(4f, 5f);
            var result = v1 / v2;
            Assert.AreEqual(2f, result.X);
            Assert.AreEqual(3f, result.Y);
        }


        [Test]
        public void Dot_ShouldReturnCorrectDotProduct()
        {
            var v1 = new Vec2<float>(1f, 2f);
            var v2 = new Vec2<float>(3f, 4f);
            float result = v1.Dot(v2);
            Assert.AreEqual(1f * 3f + 2f * 4f, result); // 3 + 8 = 11
        }

        [Test]
        public void LengthSqr_ShouldReturnCorrectSquaredLength()
        {
            var v = new Vec2<float>(3f, 4f);
            Assert.AreEqual(3f * 3f + 4f * 4f, v.LengthSqr()); // 9 + 16 = 25
        }

        [Test]
        public void Length_ShouldReturnCorrectLength()
        {
            var v = new Vec2<double>(3.0, 4.0);
            Assert.AreEqual(System.Math.Sqrt(25.0), v.Length(), 1e-9);
        }

        [Test]
        public void Normalize_ShouldResultInUnitVector()
        {
            var v = new Vec2<double>(3.0, 4.0);
            v.Normalize();
            Assert.AreEqual(1.0, v.Length(), 1e-9);
            Assert.AreEqual(3.0 / 5.0, v.X, 1e-9);
            Assert.AreEqual(4.0 / 5.0, v.Y, 1e-9);
        }
        
        [Test]
        public void Normalized_ShouldReturnUnitVector()
        {
            var v = new Vec2<double>(3.0, 4.0);
            var normalizedV = v.Normalized();
            Assert.AreEqual(1.0, normalizedV.Length(), 1e-9);
            Assert.AreEqual(3.0/5.0, normalizedV.X, 1e-9);
            Assert.AreEqual(4.0/5.0, normalizedV.Y, 1e-9);
            // Original vector should be unchanged
            Assert.AreEqual(3.0, v.X); 
            Assert.AreEqual(4.0, v.Y);
        }


        [Test]
        public void Equality_Operator_ShouldCompareCorrectly()
        {
            var v1 = new Vec2<float>(1f, 2f);
            var v2 = new Vec2<float>(1f, 2f);
            var v3 = new Vec2<float>(2f, 1f);
            Assert.IsTrue(v1 == v2);
            Assert.IsFalse(v1 == v3);
        }
        
        [Test]
        public void IsApproxEqual_ShouldCompareCorrectly()
        {
            var v1 = new Vec2<double>(1.0, 2.0);
            var v2 = new Vec2<double>(1.00000001, 2.00000001);
            var v3 = new Vec2<double>(1.01, 2.01);
            Assert.IsTrue(v1.IsApproxEqual(v2, 1e-7));
            Assert.IsFalse(v1.IsApproxEqual(v3, 1e-7));
        }
    }
}
