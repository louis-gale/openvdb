// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Maps; // Location of IMap and concrete map classes
using OpenVDB.Core.IO;      // For StreamMetadata
using System.IO;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class TransformIoTests
    {
        private const double Epsilon = 1e-9;
        private static readonly Vec3<double> TestPoint = new Vec3<double>(1, 2, 3);

        private void TestTransformSerialization<TMap>(Transform originalTransform, Action<TMap, TMap> assertMapDataEqual) where TMap : class, IMap
        {
            var streamMeta = new StreamMetadata(); // Use default StreamMetadata

            using (var ms = new MemoryStream())
            {
                // Serialize
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalTransform.Write(writer, streamMeta);
                }

                ms.Seek(0, SeekOrigin.Begin);

                // Deserialize
                var newTransform = new Transform(); // Default transform
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newTransform.Read(reader, streamMeta);
                }

                // Assertions
                Assert.IsInstanceOf<TMap>(newTransform.GetMap());
                var originalMap = originalTransform.GetMap() as TMap;
                var newMap = newTransform.GetMap() as TMap;
                Assert.IsNotNull(originalMap);
                Assert.IsNotNull(newMap);

                assertMapDataEqual(originalMap, newMap);

                Assert.AreEqual(originalTransform.IsLinear, newTransform.IsLinear);
                Assert.AreEqual(originalTransform.HasUniformScale, newTransform.HasUniformScale);

                var p1 = originalTransform.IndexToWorld(TestPoint);
                var p2 = newTransform.IndexToWorld(TestPoint);
                Assert.IsTrue(p1.IsApproxEqual(p2, Epsilon), $"Transformed points do not match for {typeof(TMap).Name}. Expected {p1}, got {p2}");

                var inv_p1 = originalTransform.WorldToIndex(TestPoint);
                var inv_p2 = newTransform.WorldToIndex(TestPoint);
                Assert.IsTrue(inv_p1.IsApproxEqual(inv_p2, Epsilon), $"Inverse transformed points do not match for {typeof(TMap).Name}. Expected {inv_p1}, got {inv_p2}");
            }
        }

        [Test]
        public void IdentityMap_Serialization_ShouldPreserveMap()
        {
            var map = new IdentityMap();
            var transform = new Transform(map);
            TestTransformSerialization<IdentityMap>(transform, (orig, read) =>
            {
                // No data members to compare for IdentityMap
                Assert.Pass();
            });
        }

        [Test]
        public void TranslationMap_Serialization_ShouldPreserveMap()
        {
            var translation = new Vec3<double>(10, 20, 30);
            var map = new TranslationMap(translation);
            var transform = new Transform(map);
            TestTransformSerialization<TranslationMap>(transform, (orig, read) =>
            {
                Assert.IsTrue(orig.Translation.IsApproxEqual(read.Translation, Epsilon));
            });
        }

        [Test]
        public void ScaleMap_Serialization_ShouldPreserveMap()
        {
            var scale = new Vec3<double>(2, 3, 4);
            var map = new ScaleMap(scale);
            var transform = new Transform(map);
            TestTransformSerialization<ScaleMap>(transform, (orig, read) =>
            {
                Assert.IsTrue(orig.Scale.IsApproxEqual(read.Scale, Epsilon));
            });
        }

        [Test]
        public void UniformScaleMap_Serialization_ShouldPreserveMap()
        {
            double scaleFactor = 2.5;
            var map = new UniformScaleMap(scaleFactor);
            var transform = new Transform(map);
            TestTransformSerialization<UniformScaleMap>(transform, (orig, read) =>
            {
                Assert.AreEqual(orig.UniformScaleValue, read.UniformScaleValue, Epsilon);
                Assert.IsTrue(orig.Scale.IsApproxEqual(read.Scale, Epsilon)); // Check underlying Vec3 scale
            });
        }

        [Test]
        public void ScaleTranslateMap_Serialization_ShouldPreserveMap()
        {
            var scale = new Vec3<double>(2, 0.5, -1);
            var translate = new Vec3<double>(-5, 15, 100.1);
            var map = new ScaleTranslateMap(scale, translate);
            var transform = new Transform(map);
            TestTransformSerialization<ScaleTranslateMap>(transform, (orig, read) =>
            {
                Assert.IsTrue(orig.Scale.IsApproxEqual(read.Scale, Epsilon));
                Assert.IsTrue(orig.Translation.IsApproxEqual(read.Translation, Epsilon));
            });
        }

        [Test]
        public void UniformScaleTranslateMap_Serialization_ShouldPreserveMap()
        {
            double scaleFactor = -1.5;
            var translate = new Vec3<double>(10, -20, 0);
            var map = new UniformScaleTranslateMap(scaleFactor, translate);
            var transform = new Transform(map);
            TestTransformSerialization<UniformScaleTranslateMap>(transform, (orig, read) =>
            {
                Assert.AreEqual(orig.UniformScaleValue, read.UniformScaleValue, Epsilon);
                Assert.IsTrue(orig.Translation.IsApproxEqual(read.Translation, Epsilon));
            });
        }

        [Test]
        public void AffineMap_Serialization_ShouldPreserveMap()
        {
            var matrix = Mat4<double>.CreateScale(new Vec3<double>(1,2,3)) *
                         Mat4<double>.CreateRotationX(0.5) *
                         Mat4<double>.CreateTranslation(new Vec3<double>(10,20,30));
            var map = new AffineMap(matrix);
            var transform = new Transform(map);
            TestTransformSerialization<AffineMap>(transform, (orig, read) =>
            {
                Assert.IsTrue(orig.Matrix.IsApproxEqual(read.Matrix, Epsilon));
            });
        }

        [Test]
        public void UnitaryMap_Serialization_ShouldPreserveMap()
        {
            var rotMatrix = Mat3<double>.CreateRotation(new Vec3<double>(0,1,0).Normalized(), System.Math.PI / 4.0); // 45 deg around Y
            var map = new UnitaryMap(rotMatrix);
            var transform = new Transform(map);
            TestTransformSerialization<UnitaryMap>(transform, (orig, read) =>
            {
                // UnitaryMap wraps an AffineMap; compare their matrices
                Assert.IsTrue(orig.ToAffineMap().Matrix.IsApproxEqual(read.ToAffineMap().Matrix, Epsilon));
            });
        }

        [Test]
        public void NonlinearFrustumMap_Serialization_ShouldPreserveData_Placeholder()
        {
            var bbox = new BBox<Vec3<double>, double>(new Vec3<double>(0,0,0), new Vec3<double>(10,10,10));
            double taper = 0.5;
            double depth = 20.0;
            var affinePart = new AffineMap(Mat4<double>.CreateTranslation(new Vec3<double>(1,2,3)));

            var map = new NonlinearFrustumMap(bbox, taper, depth, affinePart);
            var transform = new Transform(map);

            TestTransformSerialization<NonlinearFrustumMap>(transform, (orig, read) =>
            {
                // Access internal fields for comparison - requires them to be public or have getters
                // For this test, we'll rely on the Read/WriteData methods being symmetrical
                // and the point transformation test in TestTransformSerialization.
                // A more detailed test would compare _bbox, _taper, _depth, and _secondMap.
                // This assumes that NonlinearFrustumMap.ReadData correctly re-initializes these.
                // For now, if IndexToWorld behaves the same, we assume data is preserved.
                Assert.IsTrue(orig.GetBBox().Min.IsApproxEqual(read.GetBBox().Min, Epsilon)); // Requires GetBBox()
                Assert.IsTrue(orig.GetBBox().Max.IsApproxEqual(read.GetBBox().Max, Epsilon));
                Assert.AreEqual(orig.GetTaper(), read.GetTaper(), Epsilon); // Requires GetTaper()
                Assert.AreEqual(orig.GetDepth(), read.GetDepth(), Epsilon); // Requires GetDepth()
                Assert.IsTrue(orig.SecondMap.Matrix.IsApproxEqual(read.SecondMap.Matrix, Epsilon)); // Requires SecondMap getter
            });
        }
    }
}

// Helper methods/properties needed in NonlinearFrustumMap for the test above:
// public BBox<Vec3<double>, double> GetBBox() => _bbox;
// public double GetTaper() => _taper;
// public double GetDepth() => _depth;
// public AffineMap SecondMap => _secondMap;
// These should be added to NonlinearFrustumMap.cs
