// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class Mat3Tests
    {
        [Test]
        public void Constructor_Default_AllElementsShouldBeZeroOrAsExpectedByStructInit()
        {
            var m = new Mat3<float>();
            // Default struct constructor in C# initializes fields to their default (0 for float)
            Assert.AreEqual(0f, m.M00); Assert.AreEqual(0f, m.M01); Assert.AreEqual(0f, m.M02);
            Assert.AreEqual(0f, m.M10); Assert.AreEqual(0f, m.M11); Assert.AreEqual(0f, m.M12);
            Assert.AreEqual(0f, m.M20); Assert.AreEqual(0f, m.M21); Assert.AreEqual(0f, m.M22);
        }

        [Test]
        public void Constructor_IndividualComponents_ShouldInitializeCorrectly()
        {
            var m = new Mat3<float>(
                1f, 2f, 3f,
                4f, 5f, 6f,
                7f, 8f, 9f);
            Assert.AreEqual(1f, m.M00); Assert.AreEqual(2f, m.M01); Assert.AreEqual(3f, m.M02);
            Assert.AreEqual(4f, m.M10); Assert.AreEqual(5f, m.M11); Assert.AreEqual(6f, m.M12);
            Assert.AreEqual(7f, m.M20); Assert.AreEqual(8f, m.M21); Assert.AreEqual(9f, m.M22);
        }

        [Test]
        public void StaticIdentity_ShouldReturnIdentityMatrix()
        {
            var m = Mat3<double>.Identity;
            Assert.AreEqual(1.0, m.M00); Assert.AreEqual(0.0, m.M01); Assert.AreEqual(0.0, m.M02);
            Assert.AreEqual(0.0, m.M10); Assert.AreEqual(1.0, m.M11); Assert.AreEqual(0.0, m.M12);
            Assert.AreEqual(0.0, m.M20); Assert.AreEqual(0.0, m.M21); Assert.AreEqual(1.0, m.M22);
        }

        [Test]
        public void Indexer_Get_ShouldReturnCorrectValues()
        {
            var m = new Mat3<float>(1,2,3,4,5,6,7,8,9);
            Assert.AreEqual(1, m[0,0]); Assert.AreEqual(2, m[0,1]); Assert.AreEqual(3, m[0,2]);
            Assert.AreEqual(4, m[1,0]); Assert.AreEqual(5, m[1,1]); Assert.AreEqual(6, m[1,2]);
            Assert.AreEqual(7, m[2,0]); Assert.AreEqual(8, m[2,1]); Assert.AreEqual(9, m[2,2]);
            Assert.Throws<IndexOutOfRangeException>(() => { var x = m[3,0]; });
        }

        [Test]
        public void Indexer_Set_ShouldUpdateCorrectValues()
        {
            var m = Mat3<float>.ZeroMatrix;
            m[0,0] = 1; m[1,1] = 2; m[2,2] = 3;
            Assert.AreEqual(1, m.M00); Assert.AreEqual(2, m.M11); Assert.AreEqual(3, m.M22);
            Assert.AreEqual(0, m.M01); // Check other elements remain zero
        }

        [Test]
        public void Multiply_Matrix_ShouldReturnCorrectProduct()
        {
            var m1 = new Mat3<float>(
                1f, 2f, 3f,
                4f, 5f, 6f,
                7f, 8f, 9f);
            var m2 = Mat3<float>.Identity;
            var result = m1 * m2;
            Assert.IsTrue(result.IsApproxEqual(m1, 1e-7f));

            m2 = new Mat3<float>(
                2f, 0f, 0f,
                0f, 2f, 0f,
                0f, 0f, 2f);
            result = m1 * m2;
            var expected = new Mat3<float>(
                2f, 4f, 6f,
                8f, 10f, 12f,
                14f, 16f, 18f);
            Assert.IsTrue(result.IsApproxEqual(expected, 1e-7f));
        }

        [Test]
        public void Multiply_Vector_ShouldReturnCorrectProduct_MatTimesVec()
        {
            var m = new Mat3<float>(
                1f, 2f, 3f,
                4f, 5f, 6f,
                7f, 8f, 9f);
            var v = new Vec3<float>(1f, 2f, 3f);
            var result = m * v;
            // 1*1 + 2*2 + 3*3 = 1+4+9 = 14
            // 4*1 + 5*2 + 6*3 = 4+10+18 = 32
            // 7*1 + 8*2 + 9*3 = 7+16+27 = 50
            Assert.AreEqual(14f, result.X);
            Assert.AreEqual(32f, result.Y);
            Assert.AreEqual(50f, result.Z);
        }
        
        [Test]
        public void Multiply_Vector_ShouldReturnCorrectProduct_VecTimesMat()
        {
            var v = new Vec3<float>(1f, 2f, 3f);
            var m = new Mat3<float>(
                1f, 4f, 7f, // Note: transpose of previous matrix for easier check
                2f, 5f, 8f,
                3f, 6f, 9f);
            var result = v * m; 
            // This is v.x*m.col0 + v.y*m.col1 + v.z*m.col2 for each component
            // For result.X: 1*1 + 2*2 + 3*3 = 1+4+9 = 14
            // For result.Y: 1*4 + 2*5 + 3*6 = 4+10+18 = 32
            // For result.Z: 1*7 + 2*8 + 3*9 = 7+16+27 = 50
            Assert.AreEqual(14f, result.X);
            Assert.AreEqual(32f, result.Y);
            Assert.AreEqual(50f, result.Z);
        }


        [Test]
        public void Transpose_ShouldReturnTransposedMatrix()
        {
            var m = new Mat3<float>(
                1f, 2f, 3f,
                4f, 5f, 6f,
                7f, 8f, 9f);
            var transposed = m.Transposed();
            var expected = new Mat3<float>(
                1f, 4f, 7f,
                2f, 5f, 8f,
                3f, 6f, 9f);
            Assert.IsTrue(transposed.Equals(expected));
        }

        [Test]
        public void Determinant_ShouldBeCorrect()
        {
            var m = new Mat3<float>( // Identity
                1f, 0f, 0f,
                0f, 1f, 0f,
                0f, 0f, 1f);
            Assert.AreEqual(1f, m.Determinant());

            var m2 = new Mat3<float>( // Scaled identity
                2f, 0f, 0f,
                0f, 3f, 0f,
                0f, 0f, 4f);
            Assert.AreEqual(2f*3f*4f, m2.Determinant()); // 24

            var m3 = new Mat3<float>(
                1f, 2f, 3f,
                0f, 1f, 4f,
                5f, 6f, 0f);
            // 1*(1*0 - 4*6) - 2*(0*0 - 4*5) + 3*(0*6 - 1*5)
            // 1*(-24) - 2*(-20) + 3*(-5)
            // -24 + 40 - 15 = 1
            Assert.AreEqual(1f, m3.Determinant());
        }
        
        [Test]
        public void Inverse_ForIdentity_ShouldReturnIdentity()
        {
            var m = Mat3<double>.Identity;
            var inv = m.Inverse();
            Assert.IsTrue(inv.IsApproxEqual(Mat3<double>.Identity, 1e-9));
        }

        [Test]
        public void Inverse_ForScalableMatrix_ShouldReturnCorrectInverse()
        {
            var m = new Mat3<double>(
                2.0, 0.0, 0.0,
                0.0, 4.0, 0.0,
                0.0, 0.0, 5.0);
            var expectedInv = new Mat3<double>(
                0.5, 0.0, 0.0,
                0.0, 0.25, 0.0,
                0.0, 0.0, 0.2);
            var inv = m.Inverse();
            Assert.IsTrue(inv.IsApproxEqual(expectedInv, 1e-9));

            var product = m * inv;
            Assert.IsTrue(product.IsApproxEqual(Mat3<double>.Identity, 1e-9));
        }
    }
}
