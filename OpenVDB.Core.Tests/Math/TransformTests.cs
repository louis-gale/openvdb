// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;
using OpenVDB.Math.Maps;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class TransformTests
    {
        private static readonly Vec3<double> TestVec = new Vec3<double>(1, 2, 3);
        private const double Epsilon = 1e-9;

        [Test]
        public void DefaultConstructor_ShouldInitializeWithIdentityLikeMap()
        {
            var t = new Transform();
            Assert.IsTrue(t.IsLinear);
            Assert.IsTrue(t.HasUniformScale);
            Assert.IsTrue(t.IsIdentity()); // Check if it behaves like identity
            Assert.IsTrue(TestVec.IsApproxEqual(t.IndexToWorld(TestVec), Epsilon));
        }

        [Test]
        public void ConstructWithMap_ShouldUseGivenMap()
        {
            var translation = new Vec3<double>(10, 20, 30);
            var tMap = new TranslationMap(translation);
            var t = new Transform(tMap);

            var expected = TestVec + translation;
            Assert.IsTrue(expected.IsApproxEqual(t.IndexToWorld(TestVec), Epsilon));
        }

        [Test]
        public void Clone_ShouldCreateDeepCopyOfMap()
        {
            var t = Transform.CreateTranslationTransform(new Vec3<double>(5,5,5));
            var clonedT = t.Clone();

            // Modify original, clone should not change
            t.PostTranslate(new Vec3<double>(5,5,5));

            var originalExpectedAfterMod = new Vec3<double>(10,10,10) + TestVec;
            var clonedExpected = new Vec3<double>(5,5,5) + TestVec;

            Assert.IsTrue(originalExpectedAfterMod.IsApproxEqual(t.IndexToWorld(TestVec), Epsilon));
            Assert.IsTrue(clonedExpected.IsApproxEqual(clonedT.IndexToWorld(TestVec), Epsilon));
            Assert.AreNotSame(t.GetMap(), clonedT.GetMap());
        }

        [Test]
        public void VoxelSize_And_Determinant_ShouldDelegateToMap()
        {
            var scale = new Vec3<double>(2, 3, 4);
            var t = Transform.CreateScaleTransform(scale);
            Assert.AreEqual(scale, t.VoxelSize());
            Assert.AreEqual(24.0, t.Determinant(), Epsilon);
        }

        [Test]
        public void IndexToWorld_And_WorldToIndex_ForPoints_ShouldBeInverse()
        {
            var t = Transform.CreateScaleTranslateTransform(new Vec3<double>(2,2,2), new Vec3<double>(5,5,5));
            var worldP = t.IndexToWorld(TestVec);
            var indexP = t.WorldToIndex(worldP);
            Assert.IsTrue(TestVec.IsApproxEqual(indexP, Epsilon));
        }

        [Test]
        public void IndexToWorld_And_WorldToIndex_ForCoord_ShouldWork()
        {
            var t = Transform.CreateScaleTranslateTransform(new Vec3<double>(2,2,2), new Vec3<double>(0.5,0.5,0.5)); // Use offset to test rounding
            var coord = new Coord(1,2,3);

            var worldP = t.IndexToWorld(coord);
            // Expected: (1*2+0.5, 2*2+0.5, 3*2+0.5) = (2.5, 4.5, 6.5)
            Assert.IsTrue(new Vec3<double>(2.5,4.5,6.5).IsApproxEqual(worldP, Epsilon));

            var cellCenteredCoord = t.WorldToIndexCellCentered(worldP); // Should round back to original
            Assert.AreEqual(coord, cellCenteredCoord);

            var nodeCenteredCoord = t.WorldToIndexNodeCentered(worldP); // Should floor(1,2,3) = (1,2,3) if worldP was exact index value
                                                                       // worldP = (2.5,4.5,6.5) -> worldToIndex -> (1,2,3) -> floor -> (1,2,3)
            var indexP = t.WorldToIndex(worldP); // (2.5-0.5)/2 = 1, (4.5-0.5)/2 = 2, (6.5-0.5)/2=3
            Assert.AreEqual(coord, Coord.Floor(indexP));
        }


        [Test]
        public void IndexToWorld_ForBBox_ShouldTransformBounds_Linear()
        {
            var t = Transform.CreateScaleTransform(new Vec3<double>(2,2,2));
            var indexBox = new CoordBBox(new Coord(1,1,1), new Coord(3,3,3));

            var worldBox = t.IndexToWorld(indexBox);
            // Min = (1,1,1)*2 = (2,2,2)
            // Max = (3,3,3)*2 = (6,6,6)
            // Resulting box needs sorting if map flips axes, but scale doesn't.
            Assert.IsTrue(new Vec3<double>(2,2,2).IsApproxEqual(worldBox.Min, Epsilon));
            Assert.IsTrue(new Vec3<double>(6,6,6).IsApproxEqual(worldBox.Max, Epsilon));
        }

        [Test]
        public void PreMultiply_And_PostMultiply_WithMatrix_ShouldComposeCorrectly()
        {
            var t = Transform.CreateTranslationTransform(new Vec3<double>(10,0,0)); // T1
            var scaleMatrix = Mat4<double>.CreateScale(new Vec3<double>(2,1,1));      // S

            // Test PreMultiply: S * T1
            // (p * S) + T1_vec is not S * T1.
            // S * T1 means: first T1, then S.  p' = (p+T1_vec)*S_mat = p*S_mat + T1_vec*S_mat
            var tPre = t.Clone();
            tPre.PreMultiply(scaleMatrix);

            var expectedPre = (TestVec + new Vec3<double>(10,0,0)) * 2; // X component scaled
            expectedPre.Y = TestVec.Y + 0; // Y and Z translation not scaled by this specific S
            expectedPre.Z = TestVec.Z + 0;
            // Recalculate expectedPre based on actual matrix math for S * T1
            // T1_mat = [1 0 0 0; 0 1 0 0; 0 0 1 0; 10 0 0 1]
            // S_mat  = [2 0 0 0; 0 1 0 0; 0 0 1 0; 0 0 0 1]
            // S_mat * T1_mat = [2 0 0 0; 0 1 0 0; 0 0 1 0; 20 0 0 1] (Assuming S applies to translation part for row vectors)
            // If p' = p * (S*T1_mat) -> (px,py,pz,1) * [2 0 0 0; 0 1 0 0; 0 0 1 0; 20 0 0 1]
            // Result x = px*2 + 20
            var pTransformedByPre = tPre.IndexToWorld(TestVec);
            Assert.AreEqual(TestVec.X*2 + 20, pTransformedByPre.X, Epsilon);
            Assert.AreEqual(TestVec.Y*1 + 0,  pTransformedByPre.Y, Epsilon);


            // Test PostMultiply: T1 * S
            // p' = (p*T1_mat)*S_mat = p*(T1_mat*S_mat)
            // T1_mat * S_mat = [2 0 0 0; 0 1 0 0; 0 0 1 0; 10 0 0 1] (Assuming S applies to translation part for row vectors)
            // Result x = px*2 + 10
            var tPost = Transform.CreateTranslationTransform(new Vec3<double>(10,0,0)); // Reset for Post
            tPost.PostMultiply(scaleMatrix);

            var pTransformedByPost = tPost.IndexToWorld(TestVec);
            Assert.AreEqual(TestVec.X*2 + 10, pTransformedByPost.X, Epsilon);
            Assert.AreEqual(TestVec.Y*1 + 0,  pTransformedByPost.Y, Epsilon);
        }

        [Test]
        public void Composition_ScaleThenTranslate()
        {
            var t = new Transform(); // Identity
            t.PostScale(new Vec3<double>(2,2,2));
            t.PostTranslate(new Vec3<double>(10,20,30));
            // Expected: p' = (p * S) + T_vec = (1,2,3)*2 + (10,20,30) = (2,4,6) + (10,20,30) = (12,24,36)
            var expected = new Vec3<double>(12,24,36);
            Assert.IsTrue(expected.IsApproxEqual(t.IndexToWorld(TestVec), Epsilon));
        }

        [Test]
        public void Composition_TranslateThenScale()
        {
            var t = new Transform(); // Identity
            t.PostTranslate(new Vec3<double>(10,20,30));
            t.PostScale(new Vec3<double>(2,2,2));
            // Expected: p' = (p + T_vec) * S = ((1,2,3) + (10,20,30)) * 2 = (11,22,33)*2 = (22,44,66)
            var expected = new Vec3<double>(22,44,66);
            Assert.IsTrue(expected.IsApproxEqual(t.IndexToWorld(TestVec), Epsilon));
        }
    }
}
