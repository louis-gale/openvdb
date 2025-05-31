// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Maps;
using OpenVDB.Core.Math.Operators;
using OpenVDB.Core.Tree;
using OpenVDB.Core;
using System;

namespace OpenVDB.Core.Tests.Math.Operators
{
    [TestFixture]
    public class WorldSpaceOperatorsTests
    {
        private const float EpsilonF = 1e-4f; // Tolerance for float comparisons

        // Helper to populate a FloatGrid with a scalar function
        private FloatGrid CreateScalarGrid(Func<double, double, double, float> func, CoordBBox domain)
        {
            var grid = new FloatGrid(0.0f); // Background 0
            var accessor = grid.GetAccessor();
            for (int x = domain.Min.X; x <= domain.Max.X; ++x)
            {
                for (int y = domain.Min.Y; y <= domain.Max.Y; ++y)
                {
                    for (int z = domain.Min.Z; z <= domain.Max.Z; ++z)
                    {
                        accessor.SetValue(new Coord(x, y, z), func(x, y, z));
                    }
                }
            }
            return grid;
        }

        // Test Functions (from FiniteDifferenceTests)
        private float LinearFunc(double x, double y, double z) => (float)(2 * x + 3 * y + 4 * z + 5);
        private Vec3<float> GradLinearFunc(double x, double y, double z) => new Vec3<float>(2, 3, 4);
        private float QuadFunc(double x, double y, double z) => (float)(x * x + 2 * y * y + 3 * z * z);
        private Vec3<float> GradQuadFunc(double x, double y, double z) => new Vec3<float>((float)(2 * x), (float)(4 * y), (float)(6 * z));

        private readonly CoordBBox TestDomain = new CoordBBox(new Coord(-5, -5, -5), new Coord(5, 5, 5));
        private readonly Coord TestCoord = new Coord(1, 1, 1); // Use non-origin for more general polynomial tests

        [TestCase(DScheme.FD_1ST)]
        [TestCase(DScheme.BD_1ST)]
        [TestCase(DScheme.CD_2ND)]
        [TestCase(DScheme.CD_4TH)]
        [TestCase(DScheme.CD_6TH)]
        public void WSGradient_WithTranslationMap_LinearFunc_ShouldEqualISGradient(DScheme scheme)
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();

            var translationVec = new Vec3<double>(10.0, 20.0, 30.0);
            var translationMap = new TranslationMap(translationVec);

            // Calculate ISGradient
            Vec3<float> isGrad = ISGradient.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);

            // Calculate WSGradient with TranslationMap
            Vec3<float> wsGrad = WSGradient.Result<float, ITreeValueAccessor<float>, TranslationMap>(
                translationMap, accessor, TestCoord, scheme);

            Assert.IsTrue(isGrad.IsApproxEqual(wsGrad, EpsilonF),
                $"WSGradient with TranslationMap should equal ISGradient for scheme {scheme}. ISG: {isGrad}, WSG: {wsGrad}");

            // Also verify against analytical result
            Vec3<float> analyticalGrad = GradLinearFunc(TestCoord.X, TestCoord.Y, TestCoord.Z);
            Assert.IsTrue(analyticalGrad.IsApproxEqual(wsGrad, EpsilonF),
                $"WSGradient with TranslationMap result incorrect for scheme {scheme}. Expected: {analyticalGrad}, Got: {wsGrad}");
        }

        [TestCase(DScheme.CD_2ND)]
        [TestCase(DScheme.CD_4TH)]
        [TestCase(DScheme.CD_6TH)]
        public void WSGradient_WithTranslationMap_QuadraticFunc_ShouldEqualISGradient(DScheme scheme)
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();

            var translationVec = new Vec3<double>(10.0, 20.0, 30.0);
            var translationMap = new TranslationMap(translationVec);

            Vec3<float> isGrad = ISGradient.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);
            Vec3<float> wsGrad = WSGradient.Result<float, ITreeValueAccessor<float>, TranslationMap>(
                translationMap, accessor, TestCoord, scheme);

            Assert.IsTrue(isGrad.IsApproxEqual(wsGrad, EpsilonF),
                $"WSGradient with TranslationMap should equal ISGradient for scheme {scheme} on quadratic. ISG: {isGrad}, WSG: {wsGrad}");

            Vec3<float> analyticalGrad = GradQuadFunc(TestCoord.X, TestCoord.Y, TestCoord.Z);
             Assert.IsTrue(analyticalGrad.IsApproxEqual(wsGrad, EpsilonF),
                $"WSGradient with TranslationMap result incorrect for scheme {scheme} on quadratic. Expected: {analyticalGrad}, Got: {wsGrad}");
        }

        [Test]
        public void WSGradient_GeneralCase_WithNonTranslationMap_ShouldThrowNotImplemented()
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();

            // Use a ScaleMap, which is not the specialized TranslationMap
            var scaleMap = new ScaleMap(new Vec3<double>(2.0, 2.0, 2.0));

            // The generic Result method in WSGradient is expected to throw NotImplementedException
            // as its general case logic (map.ApplyIJT) is not fully implemented or tested for all maps.
            Assert.Throws<System.NotImplementedException>(() =>
                WSGradient.Result<float, ITreeValueAccessor<float>, ScaleMap>(
                    scaleMap, accessor, TestCoord, DScheme.CD_2ND)
            );
        }

        [Test]
        public void WSGradient_Specializations_UniformScaleMap_CD2_ShouldBeImplementedAndCorrect()
        {
            // f(x,y,z) = 2x + 3y + 4z + 5. Grad = (2,3,4)
            // Map: UniformScaleMap(scale=2.0). Inverse Jacobian Transpose is also scale by 1/2.0.
            // ISGradient = (2,3,4). WSGradient = (2,3,4) * (1/2.0) = (1, 1.5, 2)
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var map = new UniformScaleMap(2.0);

            var expectedWSGrad = new Vec3<float>(1.0f, 1.5f, 2.0f);

            var wsGrad = WSGradient.Result<float, ITreeValueAccessor<float>, UniformScaleMap>(map, accessor, TestCoord, DScheme.CD_2ND);

            Assert.IsTrue(expectedWSGrad.IsApproxEqual(wsGrad, EpsilonF), $"Expected {expectedWSGrad}, got {wsGrad}");
        }

        [Test]
        public void WSGradient_Specializations_ScaleMap_CD2_ShouldBeImplementedAndCorrect()
        {
            // f(x,y,z) = 2x + 3y + 4z + 5. Grad = (2,3,4)
            // Map: ScaleMap(scale=(2,1,0.5)). Inverse Jacobian Transpose is scale by (1/2, 1/1, 1/0.5=2).
            // ISGradient = (2,3,4). WSGradient = (2*0.5, 3*1, 4*2) = (1, 3, 8)
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var map = new ScaleMap(new Vec3<double>(2.0, 1.0, 0.5));

            var expectedWSGrad = new Vec3<float>(1.0f, 3.0f, 8.0f);

            var wsGrad = WSGradient.Result<float, ITreeValueAccessor<float>, ScaleMap>(map, accessor, TestCoord, DScheme.CD_2ND);

            Assert.IsTrue(expectedWSGrad.IsApproxEqual(wsGrad, EpsilonF), $"Expected {expectedWSGrad}, got {wsGrad}");
        }

        [Test]
        public void WSGradient_Specializations_ScaleTranslateMap_CD2_ShouldBeImplementedAndCorrect()
        {
            // f(x,y,z) = 2x + 3y + 4z + 5. Grad = (2,3,4)
            // Map: ScaleTranslateMap(scale=(2,1,0.5), translate=(10,20,30)). Translation doesn't affect gradient.
            // Inverse Jacobian Transpose is scale by (1/2, 1/1, 1/0.5=2).
            // ISGradient = (2,3,4). WSGradient = (2*0.5, 3*1, 4*2) = (1, 3, 8)
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var map = new ScaleTranslateMap(new Vec3<double>(2.0, 1.0, 0.5), new Vec3<double>(10,20,30));

            var expectedWSGrad = new Vec3<float>(1.0f, 3.0f, 8.0f);

            var wsGrad = WSGradient.Result<float, ITreeValueAccessor<float>, ScaleTranslateMap>(map, accessor, TestCoord, DScheme.CD_2ND);

            Assert.IsTrue(expectedWSGrad.IsApproxEqual(wsGrad, EpsilonF), $"Expected {expectedWSGrad}, got {wsGrad}");
        }
    }
}
