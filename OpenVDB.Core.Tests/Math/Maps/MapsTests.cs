// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Math;
using OpenVDB.Math.Maps;

namespace OpenVDB.Core.Tests.Math.Maps
{
    [TestFixture]
    public class MapsTests
    {
        private static readonly Vec3<double> TestVec = new Vec3<double>(1, 2, 3);
        private const double Epsilon = 1e-9;

        [Test]
        public void IdentityMap_ShouldTransformCorrectly()
        {
            var map = new IdentityMap();
            Assert.AreEqual(TestVec, map.ApplyMap(TestVec));
            Assert.AreEqual(TestVec, map.ApplyInverseMap(TestVec));
            Assert.IsTrue(map.IsLinear);
            Assert.IsTrue(map.HasUniformScale);
            Assert.AreEqual(1.0, map.Determinant, Epsilon);
            Assert.AreEqual(new Vec3<double>(1,1,1), map.VoxelSize);
        }

        [Test]
        public void TranslationMap_ShouldTransformCorrectly()
        {
            var translation = new Vec3<double>(10, 20, 30);
            var map = new TranslationMap(translation);
            var expected = TestVec + translation;
            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyInverseMap(expected), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsTrue(map.HasUniformScale);
            Assert.AreEqual(1.0, map.Determinant, Epsilon);
        }

        [Test]
        public void ScaleMap_ShouldTransformCorrectly()
        {
            var scale = new Vec3<double>(2, 3, 4);
            var map = new ScaleMap(scale);
            var expected = new Vec3<double>(TestVec.X * scale.X, TestVec.Y * scale.Y, TestVec.Z * scale.Z);
            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyInverseMap(expected), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsFalse(map.HasUniformScale); // Non-uniform scale
            Assert.AreEqual(scale.X * scale.Y * scale.Z, map.Determinant, Epsilon);
        }

        [Test]
        public void UniformScaleMap_ShouldTransformCorrectly()
        {
            double scaleFactor = 2.0;
            var map = new UniformScaleMap(scaleFactor);
            var expected = TestVec * scaleFactor;
            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyInverseMap(expected), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsTrue(map.HasUniformScale);
            Assert.AreEqual(scaleFactor * scaleFactor * scaleFactor, map.Determinant, Epsilon);
        }

        [Test]
        public void ScaleTranslateMap_ShouldTransformCorrectly()
        {
            var scale = new Vec3<double>(2, 3, 4);
            var translate = new Vec3<double>(10, 20, 30);
            var map = new ScaleTranslateMap(scale, translate);
            var expected = (TestVec * scale) + translate;

            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyInverseMap(expected), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsFalse(map.HasUniformScale);
        }
        
        [Test]
        public void UniformScaleTranslateMap_ShouldTransformCorrectly()
        {
            double scaleFactor = 2.0;
            var translate = new Vec3<double>(10, 20, 30);
            var map = new UniformScaleTranslateMap(scaleFactor, translate);
            var expected = (TestVec * scaleFactor) + translate;

            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyInverseMap(expected), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsTrue(map.HasUniformScale);
        }


        [Test]
        public void AffineMap_WithIdentityMatrix_ShouldBehaveAsIdentity()
        {
            var map = new AffineMap(Mat4<double>.Identity);
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(map.ApplyInverseMap(TestVec), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsTrue(map.HasUniformScale); // Identity is uniform scale
            Assert.IsTrue(map.IsIdentity());
        }

        [Test]
        public void AffineMap_WithScaleMatrix_ShouldScale()
        {
            var scaleMatrix = Mat4<double>.CreateScale(new Vec3<double>(2, 3, 4));
            var map = new AffineMap(scaleMatrix);
            var expected = new Vec3<double>(TestVec.X * 2, TestVec.Y * 3, TestVec.Z * 4);
            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(TestVec), Epsilon));
        }
        
        [Test]
        public void UnitaryMap_WithRotation_ShouldRotate()
        {
            // 90 degrees around Y axis
            var rotMat3 = Mat3<double>.CreateRotation(new Vec3<double>(0,1,0), System.Math.PI / 2.0);
            var map = new UnitaryMap(rotMat3);
            var point = new Vec3<double>(1,0,0); // Point on X axis
            var expected = new Vec3<double>(0,0,-1); // Rotated to -Z axis

            Assert.IsTrue(expected.IsApproxEqual(map.ApplyMap(point), Epsilon));
            Assert.IsTrue(map.IsLinear);
            Assert.IsTrue(map.HasUniformScale);
        }

        [Test]
        public void MapComposition_ScaleThenTranslate_ShouldEqualScaleTranslateMap()
        {
            var scale = new Vec3<double>(2,2,2);
            var translate = new Vec3<double>(10,20,30);

            IMap sMap = new ScaleMap(scale);
            IMap tMap = new TranslationMap(translate);
            
            // Equivalent to: p' = (p * S) + T
            IMap composedMap = sMap.PostTranslate(translate); // ScaleMap.PostTranslate returns ScaleTranslateMap
            
            var stMap = new ScaleTranslateMap(scale, translate);

            Assert.IsInstanceOf<ScaleTranslateMap>(composedMap);
            Assert.IsTrue(stMap.ApplyMap(TestVec).IsApproxEqual(composedMap.ApplyMap(TestVec), Epsilon));
        }
        
        [Test]
        public void MapComposition_TranslateThenScale_ShouldBeCorrect()
        {
            var scale = new Vec3<double>(2,2,2);
            var translate = new Vec3<double>(10,20,30);

            IMap tMap = new TranslationMap(translate);
            
            // Equivalent to: p' = (p + T) * S = p*S + T*S
            IMap composedMap = tMap.PostScale(scale); // TranslationMap.PostScale returns ScaleTranslateMap
            
            var stMap = new ScaleTranslateMap(scale, translate * scale);

            Assert.IsInstanceOf<ScaleTranslateMap>(composedMap);
             Assert.IsTrue(stMap.ApplyMap(TestVec).IsApproxEqual(composedMap.ApplyMap(TestVec), Epsilon));
        }

        [Test]
        public void ToAffineMap_FromSimpleMaps_ShouldBehaveCorrectly()
        {
            var t = new Vec3<double>(5,5,5);
            var s = new Vec3<double>(2,2,2);

            var tMap = new TranslationMap(t);
            var sMap = new ScaleMap(s);

            var affineT = tMap.ToAffineMap();
            var affineS = sMap.ToAffineMap();

            Assert.IsTrue((TestVec + t).IsApproxEqual(affineT.ApplyMap(TestVec), Epsilon));
            Assert.IsTrue((TestVec * s).IsApproxEqual(affineS.ApplyMap(TestVec), Epsilon));
        }
        
        [Test]
        public void InverseMap_ForSimpleMaps_ShouldBeCorrect()
        {
            var tMap = new TranslationMap(new Vec3<double>(1,2,3));
            var sMap = new ScaleMap(new Vec3<double>(2,0.5,4));

            var invTMap = tMap.InverseMap();
            var invSMap = sMap.InverseMap();

            Assert.IsTrue(TestVec.IsApproxEqual(invTMap.ApplyMap(tMap.ApplyMap(TestVec)), Epsilon));
            Assert.IsTrue(TestVec.IsApproxEqual(invSMap.ApplyMap(sMap.ApplyMap(TestVec)), Epsilon));
        }
    }
}
