// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Maps;
using OpenVDB.Core.IO; // For StreamMetadata, if I/O methods were tested
using System;
using System.IO;

namespace OpenVDB.Core.Tests.Math.Maps
{
    [TestFixture]
    public class GenericMapTests
    {
        private const double Epsilon = 1e-9;
        private static readonly Vec3<double> TestPoint = new Vec3<double>(1, 2, 3);

        [Test]
        public void Constructor_WithConcreteMap_ShouldWrapCorrectly()
        {
            var translationVec = new Vec3<double>(10, 20, 30);
            var concreteMap = new TranslationMap(translationVec);
            var genericMap = new GenericMap(concreteMap);

            Assert.AreSame(concreteMap, genericMap.GetWrappedMapTestHook(), "GenericMap should initially hold the same instance if constructor doesn't clone.");
            // Note: The C++ GenericMap takes a Ptr, implying shared ownership. If our IMap is class, it's a reference.
            // If constructor GenericMap(IMap map) is changed to map.Clone(), this test would change.
            // Current C# GenericMap(IMap map) does not clone.
        }

        [Test]
        public void Constructor_WithTransform_ShouldWrapMapsCopy()
        {
            var translationVec = new Vec3<double>(10, 20, 30);
            var concreteMap = new TranslationMap(translationVec);
            var transform = new Transform(concreteMap);

            var genericMap = new GenericMap(transform);
            var wrappedMap = genericMap.GetWrappedMapTestHook();

            Assert.AreNotSame(concreteMap, wrappedMap, "GenericMap from Transform should wrap a copy of the transform's map.");
            Assert.IsInstanceOf<TranslationMap>(wrappedMap);
            Assert.IsTrue(translationVec.IsApproxEqual(((TranslationMap)wrappedMap).Translation, Epsilon));
        }


        [Test]
        public void BasicDelegation_TranslationMap_ShouldWork()
        {
            var translationVec = new Vec3<double>(10, 20, 30);
            var concreteMap = new TranslationMap(translationVec);
            var genericMap = new GenericMap(concreteMap);

            Assert.AreEqual(concreteMap.TypeName, genericMap.TypeName);
            Assert.AreEqual(concreteMap.IsLinear, genericMap.IsLinear);
            Assert.AreEqual(concreteMap.HasUniformScale, genericMap.HasUniformScale);
            Assert.AreEqual(concreteMap.Determinant, genericMap.Determinant, Epsilon);
            Assert.IsTrue(concreteMap.VoxelSize.IsApproxEqual(genericMap.VoxelSize, Epsilon));

            var expectedMappedPoint = concreteMap.ApplyMap(TestPoint);
            Assert.IsTrue(expectedMappedPoint.IsApproxEqual(genericMap.ApplyMap(TestPoint), Epsilon));

            var expectedInverseMappedPoint = concreteMap.ApplyInverseMap(TestPoint);
            Assert.IsTrue(expectedInverseMappedPoint.IsApproxEqual(genericMap.ApplyInverseMap(TestPoint), Epsilon));
        }

        [Test]
        public void BasicDelegation_ScaleMap_ShouldWork()
        {
            var scaleVec = new Vec3<double>(2, 3, 4);
            var concreteMap = new ScaleMap(scaleVec);
            var genericMap = new GenericMap(concreteMap);

            Assert.AreEqual(concreteMap.TypeName, genericMap.TypeName);
            Assert.AreEqual(concreteMap.IsLinear, genericMap.IsLinear);
            Assert.AreEqual(concreteMap.HasUniformScale, genericMap.HasUniformScale);
            Assert.AreEqual(concreteMap.Determinant, genericMap.Determinant, Epsilon); // 2*3*4 = 24
            Assert.IsTrue(concreteMap.VoxelSize.IsApproxEqual(genericMap.VoxelSize, Epsilon)); // abs(2,3,4)

            var expectedMappedPoint = concreteMap.ApplyMap(TestPoint); // (1*2, 2*3, 3*4) = (2,6,12)
            Assert.IsTrue(expectedMappedPoint.IsApproxEqual(genericMap.ApplyMap(TestPoint), Epsilon));

            var expectedInverseMappedPoint = concreteMap.ApplyInverseMap(TestPoint); // (1/2, 2/3, 3/4)
            Assert.IsTrue(expectedInverseMappedPoint.IsApproxEqual(genericMap.ApplyInverseMap(TestPoint), Epsilon));
        }

        [Test]
        public void Clone_ShouldCreateDeepCopyOfWrappedMap()
        {
            var concreteMap = new TranslationMap(new Vec3<double>(5, 5, 5));
            var originalGenericMap = new GenericMap(concreteMap);

            var clonedGenericMap = (GenericMap)originalGenericMap.Clone();

            Assert.AreNotSame(originalGenericMap, clonedGenericMap);

            var originalWrappedMap = originalGenericMap.GetWrappedMapTestHook();
            var clonedWrappedMap = clonedGenericMap.GetWrappedMapTestHook();

            Assert.AreNotSame(originalWrappedMap, clonedWrappedMap, "Cloned GenericMap should wrap a new map instance.");
            Assert.IsInstanceOf<TranslationMap>(clonedWrappedMap);
            Assert.IsTrue(((TranslationMap)originalWrappedMap).Translation.IsApproxEqual(((TranslationMap)clonedWrappedMap).Translation, Epsilon));

            // Modify original concrete map (if GenericMap held it directly) or original GenericMap's wrapped map (if it was cloned on construction)
            // Current GenericMap(IMap) does not clone. So concreteMap is the _map.
            concreteMap.PreTranslate(new Vec3<double>(1,1,1)); // Modifies the map wrapped by originalGenericMap

            // Cloned map should not be affected
            var pointAfterOriginalMod = originalGenericMap.ApplyMap(TestPoint); // (1,2,3) + (6,6,6) = (7,8,9)
            var pointForCloned = clonedGenericMap.ApplyMap(TestPoint);      // (1,2,3) + (5,5,5) = (6,7,8)

            Assert.IsFalse(pointAfterOriginalMod.IsApproxEqual(pointForCloned, Epsilon), "Clone was affected by modification to original's wrapped map.");
            Assert.IsTrue(new Vec3<double>(6+1, 7+1, 8+1).IsApproxEqual(pointAfterOriginalMod, Epsilon)); // (7,8,9)
            Assert.IsTrue(new Vec3<double>(6,7,8).IsApproxEqual(pointForCloned, Epsilon));
        }

        [Test]
        public void InverseMap_ShouldReturnGenericMapWrappingInverse()
        {
            var scaleVec = new Vec3<double>(2, 4, 5); // Det = 40
            var concreteMap = new ScaleMap(scaleVec);
            var genericMap = new GenericMap(concreteMap);

            var inverseGenericMap = (GenericMap)genericMap.InverseMap();
            Assert.IsNotNull(inverseGenericMap);

            var wrappedInverse = inverseGenericMap.GetWrappedMapTestHook();
            Assert.IsInstanceOf<ScaleMap>(wrappedInverse);
            Assert.IsTrue(new Vec3<double>(0.5, 0.25, 0.2).IsApproxEqual(((ScaleMap)wrappedInverse).Scale, Epsilon));

            var transformedPoint = genericMap.ApplyMap(TestPoint);
            var originalPoint = inverseGenericMap.ApplyMap(transformedPoint);
            Assert.IsTrue(TestPoint.IsApproxEqual(originalPoint, Epsilon));
        }

        [Test]
        public void CompositionMethods_ShouldReturnGenericMapWrappingComposedMap()
        {
            var initialTranslation = new Vec3<double>(1,1,1);
            var genericMap = new GenericMap(new TranslationMap(initialTranslation));

            var postTranslation = new Vec3<double>(5,5,5);
            var composedGenericMap = (GenericMap)genericMap.PostTranslate(postTranslation);

            var expectedFinalTranslation = initialTranslation + postTranslation; // (6,6,6)
            var wrappedComposedMap = composedGenericMap.GetWrappedMapTestHook();
            Assert.IsInstanceOf<TranslationMap>(wrappedComposedMap, "PostTranslate on TranslationMap should result in TranslationMap.");
            Assert.IsTrue(expectedFinalTranslation.IsApproxEqual(((TranslationMap)wrappedComposedMap).Translation, Epsilon));

            var finalPoint = composedGenericMap.ApplyMap(TestPoint); // (1,2,3) + (6,6,6) = (7,8,9)
            Assert.IsTrue(new Vec3<double>(7,8,9).IsApproxEqual(finalPoint, Epsilon));
        }

        [Test]
        public void ToAffineMap_ShouldDelegateCorrectly()
        {
            var scale = new Vec3<double>(2,3,4);
            var concreteMap = new ScaleMap(scale);
            var genericMap = new GenericMap(concreteMap);

            var affineFromConcrete = concreteMap.ToAffineMap();
            var affineFromGeneric = genericMap.ToAffineMap();

            Assert.IsTrue(affineFromConcrete.Matrix.IsApproxEqual(affineFromGeneric.Matrix, Epsilon));
        }
    }

    // Helper extension method to access the private _map for testing purposes.
    // This is not ideal for production code but useful for tests.
    internal static class GenericMapTestExtensions
    {
        public static IMap GetWrappedMapTestHook(this GenericMap genericMap)
        {
            // Using reflection to access the private _map field.
            var fieldInfo = typeof(GenericMap).GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (IMap)fieldInfo?.GetValue(genericMap);
        }
    }
}
