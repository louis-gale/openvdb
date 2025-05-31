// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Operators;
using OpenVDB.Core.Tree;
using OpenVDB.Core;
using System;

namespace OpenVDB.Core.Tests.Math.Operators
{
    [TestFixture]
    public class IndexSpaceOperatorsTests
    {
        private const float EpsilonF = 1e-4f;

        // Helper to populate a FloatGrid with a scalar function
        private FloatGrid CreateScalarGrid(Func<double, double, double, float> func, CoordBBox domain)
        {
            var grid = new FloatGrid(0.0f);
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

        // Helper to populate a Vec3SGrid with a vector function
        private Vec3SGrid CreateVectorGrid(Func<double, double, double, Vec3<float>> func, CoordBBox domain)
        {
            var grid = new Vec3SGrid(Vec3<float>.Zero);
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
        private Vec3<float> VectorFuncLinear(double x, double y, double z) => new Vec3<float>((float)(2*x), (float)(3*y), (float)(4*z));
        private float DivVectorFuncLinear(double x, double y, double z) => 2f + 3f + 4f; // 9

        private readonly CoordBBox TestDomain = new CoordBBox(new Coord(-5, -5, -5), new Coord(5, 5, 5));
        private readonly Coord TestCoord = new Coord(1, 1, 1); // Use non-origin for more general polynomial tests

        [TestCase(DScheme.FD_1ST)]
        [TestCase(DScheme.BD_1ST)]
        [TestCase(DScheme.CD_2ND)]
        [TestCase(DScheme.CD_4TH)]
        [TestCase(DScheme.CD_6TH)]
        public void ISGradient_LinearFunc_ShouldBeExact(DScheme scheme)
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            Vec3<float> expectedGrad = GradLinearFunc(TestCoord.X, TestCoord.Y, TestCoord.Z);

            Vec3<float> grad = ISGradient.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);

            Assert.IsTrue(expectedGrad.IsApproxEqual(grad, EpsilonF), $"Scheme {scheme} failed. Expected {expectedGrad}, got {grad}");
        }

        [TestCase(DScheme.CD_2ND)]
        [TestCase(DScheme.CD_4TH)]
        [TestCase(DScheme.CD_6TH)]
        public void ISGradient_QuadraticFunc_ShouldBeExactForSufficientOrder(DScheme scheme)
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            Vec3<float> expectedGrad = GradQuadFunc(TestCoord.X, TestCoord.Y, TestCoord.Z); // At (1,1,1) -> (2,4,6)

            Vec3<float> grad = ISGradient.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);
            Assert.IsTrue(expectedGrad.IsApproxEqual(grad, EpsilonF), $"Scheme {scheme} failed. Expected {expectedGrad}, got {grad}");
        }

        [Test]
        public void ISGradientBiased_FirstBias_ShouldSelectCorrectSchemes()
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain); // f(x,y,z) = x^2 + 2y^2 + 3z^2
            var accessor = grid.GetAccessor();
            var coord = TestCoord; // (1,1,1)

            // Bias (+,+,+) -> all backward
            var biasPositive = new Vec3<float>(1, 1, 1);
            var gradBackward = ISGradientBiased.Result<float, ITreeValueAccessor<float>, float>(accessor, coord, BiasedGradientScheme.FIRST_BIAS, biasPositive);
            var expectedBackward = new Vec3<float>(
                D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.BD_1ST),
                D1.InY<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.BD_1ST),
                D1.InZ<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.BD_1ST)
            );
            Assert.IsTrue(expectedBackward.IsApproxEqual(gradBackward, EpsilonF));

            // Bias (-,-,-) -> all forward
            var biasNegative = new Vec3<float>(-1, -1, -1);
            var gradForward = ISGradientBiased.Result<float, ITreeValueAccessor<float>, float>(accessor, coord, BiasedGradientScheme.FIRST_BIAS, biasNegative);
            var expectedForward = new Vec3<float>(
                D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.FD_1ST),
                D1.InY<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.FD_1ST),
                D1.InZ<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.FD_1ST)
            );
            Assert.IsTrue(expectedForward.IsApproxEqual(gradForward, EpsilonF));

            // Bias mixed (+,-,+)
            var biasMixed = new Vec3<float>(1, -1, 1);
            var gradMixed = ISGradientBiased.Result<float, ITreeValueAccessor<float>, float>(accessor, coord, BiasedGradientScheme.FIRST_BIAS, biasMixed);
            var expectedMixed = new Vec3<float>(
                D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.BD_1ST),
                D1.InY<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.FD_1ST),
                D1.InZ<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.BD_1ST)
            );
            Assert.IsTrue(expectedMixed.IsApproxEqual(gradMixed, EpsilonF));
        }

        [Test]
        public void ISGradientBiased_HJWENO5_ShouldCallHJWENOOrThrow()
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var bias = new Vec3<float>(1,1,1); // Positive velocity for all components

            // D1.HjWeno5InX/Y/Z are placeholders that throw NotImplementedException
            Assert.Throws<NotImplementedException>(() =>
                ISGradientBiased.Result<float, ITreeValueAccessor<float>, float>(accessor, TestCoord, BiasedGradientScheme.HJWENO5_BIAS, bias)
            );
        }

        [Test]
        public void ISGradientNormSqrd_FirstBias_LinearFunc_ShouldComputeCorrectly()
        {
            // f(x,y,z) = 2x + 3y + 4z + 5. At (1,1,1), value = 14 (positiveSign = true)
            // Grad = (2,3,4)
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var coord = TestCoord; // (1,1,1)

            float valCenter = accessor.GetValue(coord); // 2*1 + 3*1 + 4*1 + 5 = 14
            bool positiveSign = valCenter > 0; // True

            // For FIRST_BIAS: FD_Scheme=FD_1ST, BD_Scheme=BD_1ST
            // D- (downwind for positiveSign=true) uses BD_1ST
            // D+ (upwind for positiveSign=true) uses FD_1ST
            // Since func is linear, FD_1ST and BD_1ST gradients are both (2,3,4)
            var downwindGrad = ISGradient.Result<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.BD_1ST); // (2,3,4)
            var upwindGrad   = ISGradient.Result<float, ITreeValueAccessor<float>>(accessor, coord, DScheme.FD_1ST); // (2,3,4)

            float expectedNormSq = MathUtil.GodunovsNormSqrd<float>(positiveSign, downwindGrad, upwindGrad);
            // positiveSign = true: sum( max(0, D-)^2 + min(0, D+)^2 )
            // D- = (2,3,4), D+ = (2,3,4)
            // max(0,2)^2 + min(0,2)^2 = 2^2 + 0^2 = 4
            // max(0,3)^2 + min(0,3)^2 = 3^2 + 0^2 = 9
            // max(0,4)^2 + min(0,4)^2 = 4^2 + 0^2 = 16
            // Expected = 4 + 9 + 16 = 29

            float computedNormSq = ISGradientNormSqrd.Result<float, ITreeValueAccessor<float>>(accessor, coord, BiasedGradientScheme.FIRST_BIAS);
            Assert.AreEqual(expectedNormSq, computedNormSq, EpsilonF);
        }

        [Test]
        public void ISGradientNormSqrd_HJWENO5_ShouldThrowNotImplemented()
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            Assert.Throws<NotImplementedException>(() =>
                ISGradientNormSqrd.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, BiasedGradientScheme.HJWENO5_BIAS)
            );
        }

        [Test]
        public void ISLaplacian_CD_SECOND_QuadraticFunc_ShouldBeExact()
        {
            // f(x,y,z) = x^2 + 2y^2 + 3z^2
            // d2f/dx2 = 2, d2f/dy2 = 4, d2f/dz2 = 6
            // Laplacian = 2 + 4 + 6 = 12
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedLaplacian = 2f + 4f + 6f;

            float laplacian = ISLaplacian.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, DDScheme.CD_SECOND);
            Assert.AreEqual(expectedLaplacian, laplacian, EpsilonF);
        }

        [Test]
        public void ISLaplacian_OtherSchemes_ShouldThrowNotImplemented()
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            Assert.Throws<NotImplementedException>(() => ISLaplacian.Result<float, ITreeValueAccessor<float>>(accessor, TestCoord, DDScheme.CD_FOURTH));
        }

        [Test]
        public void ISDivergence_CD_SECOND_LinearVectorFunc_ShouldBeExact()
        {
            // F=(2x,3y,4z), Div(F) = 2+3+4=9
            var grid = CreateVectorGrid(VectorFuncLinear, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDiv = DivVectorFuncLinear(TestCoord.X, TestCoord.Y, TestCoord.Z);

            float divergence = ISDivergence.Result<float, Vec3<float>, ITreeValueAccessor<Vec3<float>>>(accessor, TestCoord, DScheme.CD_2ND);
            Assert.AreEqual(expectedDiv, divergence, EpsilonF);
        }

        [Test]
        public void ISDivergence_OtherSchemes_MayThrowNotImplemented()
        {
            var grid = CreateVectorGrid(VectorFuncLinear, TestDomain);
            var accessor = grid.GetAccessor();
            // CD_4TH in D1.Divergence is not implemented, so ISDivergence should throw.
            Assert.Throws<NotImplementedException>(() =>
                ISDivergence.Result<float, Vec3<float>, ITreeValueAccessor<Vec3<float>>>(accessor, TestCoord, DScheme.CD_4TH));
        }


        [Test]
        public void ISCurl_ShouldThrowNotImplemented()
        {
            var grid = CreateVectorGrid(VectorFuncLinear, TestDomain);
            var accessor = grid.GetAccessor();
            Assert.Throws<NotImplementedException>(() =>
                ISCurl.Result<float, Vec3<float>, ITreeValueAccessor<Vec3<float>>>(accessor, TestCoord, DScheme.CD_2ND));
        }
    }
}
