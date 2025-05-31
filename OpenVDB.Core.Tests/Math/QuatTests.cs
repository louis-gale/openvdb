// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;
using System; // For Math.PI

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class QuatTests
    {
        [Test]
        public void Constructor_Default_ShouldBeZeroQuaternion()
        {
            // Default constructor for struct will init to all zeros.
            // The Quat<T> constructor with no args sets to identity if all params are default.
            // Let's test the parameterless struct constructor behavior first.
            var q = new Quat<float>();
            Assert.AreEqual(0f, q.X);
            Assert.AreEqual(0f, q.Y);
            Assert.AreEqual(0f, q.Z);
            Assert.AreEqual(0f, q.W); // Default for struct.
                                      // If Quat<T>.Init() was called by a parameterless constructor, W would be 1.
                                      // The current Quat<T> doesn't have a parameterless ctor to enforce identity.
        }

        [Test]
        public void Constructor_XYZW_ShouldInitializeCorrectly()
        {
            var q = new Quat<float>(1f, 2f, 3f, 4f);
            Assert.AreEqual(1f, q.X);
            Assert.AreEqual(2f, q.Y);
            Assert.AreEqual(3f, q.Z);
            Assert.AreEqual(4f, q.W);
        }

        [Test]
        public void StaticIdentity_ShouldReturnIdentityQuaternion()
        {
            var q = Quat<double>.Identity;
            Assert.AreEqual(0.0, q.X);
            Assert.AreEqual(0.0, q.Y);
            Assert.AreEqual(0.0, q.Z);
            Assert.AreEqual(1.0, q.W);
        }

        [Test]
        public void Constructor_AxisAngle_ShouldCreateCorrectQuaternion()
        {
            var axis = new Vec3<double>(1.0, 0.0, 0.0); // X-axis
            double angle = System.Math.PI / 2.0; // 90 degrees

            var q = new Quat<double>(axis, angle);

            double sinHalfAngle = System.Math.Sin(angle / 2.0);
            Assert.AreEqual(axis.X * sinHalfAngle, q.X, 1e-9);
            Assert.AreEqual(axis.Y * sinHalfAngle, q.Y, 1e-9);
            Assert.AreEqual(axis.Z * sinHalfAngle, q.Z, 1e-9);
            Assert.AreEqual(System.Math.Cos(angle / 2.0), q.W, 1e-9);
        }

        [Test]
        public void Length_And_Normalize_ShouldWorkCorrectly()
        {
            var q = new Quat<double>(1.0, 2.0, 3.0, 4.0);
            double lenSqr = 1*1 + 2*2 + 3*3 + 4*4; // 1 + 4 + 9 + 16 = 30
            double len = System.Math.Sqrt(lenSqr);

            Assert.AreEqual(lenSqr, q.LengthSqr(), 1e-9);
            Assert.AreEqual(len, q.Length(), 1e-9);

            var normalizedQ = q.Normalized();
            Assert.AreEqual(1.0, normalizedQ.Length(), 1e-9);
            Assert.AreEqual(q.X / len, normalizedQ.X, 1e-9);

            q.Normalize();
            Assert.AreEqual(1.0, q.Length(), 1e-9);
        }

        [Test]
        public void Multiply_Quaternion_ShouldCombineRotations()
        {
            // 90 deg rotation around X
            var qx = new Quat<double>(new Vec3<double>(1,0,0), System.Math.PI / 2.0);
            // 90 deg rotation around Y
            var qy = new Quat<double>(new Vec3<double>(0,1,0), System.Math.PI / 2.0);

            // Combined rotation: first Y, then X (standard q2 * q1 means q1 followed by q2)
            // So qx * qy means: apply qy, then apply qx
            var qCombined = qx * qy;

            var v = new Vec3<double>(0,0,1); // Point on Z axis

            // Apply qy: (0,0,1) rotated by 90 deg around Y becomes (1,0,0)
            var v_qy = qy.RotateVector(v);
            Assert.AreEqual(1.0, v_qy.X, 1e-9);
            Assert.AreEqual(0.0, v_qy.Y, 1e-9);
            Assert.AreEqual(0.0, v_qy.Z, 1e-9);

            // Apply qx to result: (1,0,0) rotated by 90 deg around X remains (1,0,0)
            var v_qx_qy = qx.RotateVector(v_qy);
            Assert.AreEqual(1.0, v_qx_qy.X, 1e-9);
            Assert.AreEqual(0.0, v_qx_qy.Y, 1e-9);
            Assert.AreEqual(0.0, v_qx_qy.Z, 1e-9);

            // Compare with combined rotation
            var v_combined = qCombined.RotateVector(v);
            Assert.AreEqual(v_qx_qy.X, v_combined.X, 1e-9);
            Assert.AreEqual(v_qx_qy.Y, v_combined.Y, 1e-9);
            Assert.AreEqual(v_qx_qy.Z, v_combined.Z, 1e-9);
        }

        [Test]
        public void Conjugate_And_Inverse_ShouldBeCorrectForUnitQuaternion()
        {
            var q = new Quat<double>(new Vec3<double>(1,0,0), System.Math.PI / 2.0); // Unit quaternion
            q.Normalize(); // Ensure it's unit

            var conjugate = q.Conjugate();
            var inverse = q.Inverse();

            Assert.AreEqual(-q.X, conjugate.X, 1e-9);
            Assert.AreEqual(-q.Y, conjugate.Y, 1e-9);
            Assert.AreEqual(-q.Z, conjugate.Z, 1e-9);
            Assert.AreEqual( q.W, conjugate.W, 1e-9);

            // For unit quaternion, inverse is the conjugate
            Assert.AreEqual(conjugate.X, inverse.X, 1e-9);
            Assert.AreEqual(conjugate.Y, inverse.Y, 1e-9);
            Assert.AreEqual(conjugate.Z, inverse.Z, 1e-9);
            Assert.AreEqual(conjugate.W, inverse.W, 1e-9);

            var identity = q * inverse;
            Assert.AreEqual(0.0, identity.X, 1e-9);
            Assert.AreEqual(0.0, identity.Y, 1e-9);
            Assert.AreEqual(0.0, identity.Z, 1e-9);
            Assert.AreEqual(1.0, identity.W, 1e-9);
        }

        [Test]
        public void ToMat3_FromAxisAngle_ShouldProduceCorrectRotationMatrix()
        {
            // 90 degrees around Y axis
            var axis = new Vec3<double>(0, 1, 0);
            double angle = System.Math.PI / 2.0;
            var q = new Quat<double>(axis, angle);
            var mat = q.ToMat3();

            // Expected matrix for 90 deg rotation around Y:
            // [cos(90)  0  sin(90)]   [0  0  1]
            // [   0     1    0   ] = [0  1  0]
            // [-sin(90) 0  cos(90)]   [-1 0  0]

            Assert.AreEqual(System.Math.Cos(angle), mat.M00, 1e-9); Assert.AreEqual(0.0, mat.M01, 1e-9); Assert.AreEqual(System.Math.Sin(angle), mat.M02, 1e-9);
            Assert.AreEqual(0.0, mat.M10, 1e-9); Assert.AreEqual(1.0, mat.M11, 1e-9); Assert.AreEqual(0.0, mat.M12, 1e-9);
            Assert.AreEqual(-System.Math.Sin(angle), mat.M20, 1e-9); Assert.AreEqual(0.0, mat.M21, 1e-9); Assert.AreEqual(System.Math.Cos(angle), mat.M22, 1e-9);

            var v = new Vec3<double>(1,0,0); // Point on X axis
            var rotatedV = mat * v; // Mat3 * Vec3 multiplication

            // (1,0,0) rotated 90 deg around Y should be (0,0,-1)
            Assert.AreEqual(0.0, rotatedV.X, 1e-9);
            Assert.AreEqual(0.0, rotatedV.Y, 1e-9);
            Assert.AreEqual(-1.0, rotatedV.Z, 1e-9);

            var rotatedVQuat = q.RotateVector(new Vec3<double>(1,0,0));
             Assert.AreEqual(rotatedV.X, rotatedVQuat.X, 1e-9);
             Assert.AreEqual(rotatedV.Y, rotatedVQuat.Y, 1e-9);
             Assert.AreEqual(rotatedV.Z, rotatedVQuat.Z, 1e-9);
        }

        [Test]
        public void Slerp_ShouldInterpolateCorrectly()
        {
            var q1 = Quat<double>.Identity; // Rotation of 0 degrees
            var q2 = new Quat<double>(new Vec3<double>(0,0,1), System.Math.PI); // 180 degrees around Z

            // Midpoint interpolation (t=0.5) should be 90 degrees around Z
            var qMid = Quat<double>.Slerp(q1, q2, 0.5);
            var expectedMid = new Quat<double>(new Vec3<double>(0,0,1), System.Math.PI / 2.0);

            Assert.IsTrue(qMid.IsApproxEqual(expectedMid, 1e-9) || qMid.IsApproxEqual(-expectedMid, 1e-9)); // Quaternion can be q or -q

            var v = new Vec3<double>(1,0,0);
            var rotatedV = qMid.RotateVector(v);
            // (1,0,0) rotated 90 deg around Z should be (0,1,0)
            Assert.AreEqual(0.0, rotatedV.X, 1e-9);
            Assert.AreEqual(1.0, rotatedV.Y, 1e-9);
            Assert.AreEqual(0.0, rotatedV.Z, 1e-9);

            // Test t=0 and t=1
            var qStart = Quat<double>.Slerp(q1, q2, 0.0);
             Assert.IsTrue(qStart.IsApproxEqual(q1, 1e-9) || qStart.IsApproxEqual(-q1, 1e-9));

            var qEnd = Quat<double>.Slerp(q1, q2, 1.0);
            Assert.IsTrue(qEnd.IsApproxEqual(q2, 1e-9) || qEnd.IsApproxEqual(-q2, 1e-9));
        }
    }
}
