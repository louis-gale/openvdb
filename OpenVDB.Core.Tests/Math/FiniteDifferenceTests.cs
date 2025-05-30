// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Math;
using OpenVDB.Core.Tree; // For ITreeValueAccessor
using OpenVDB.Core;     // For FloatGrid, Vec3SGrid etc.
using System;

namespace OpenVDB.Core.Tests.Math
{
    [TestFixture]
    public class FiniteDifferenceTests
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

        // Helper to populate a Vec3SGrid with a vector function
        private Vec3SGrid CreateVectorGrid(Func<double, double, double, Vec3<float>> func, CoordBBox domain)
        {
            var grid = new Vec3SGrid(Vec3<float>.Zero); // Background (0,0,0)
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

        // Define test functions and their analytical derivatives
        // Linear: f(x,y,z) = 2x + 3y + 4z + 5
        private float LinearFunc(double x, double y, double z) => (float)(2 * x + 3 * y + 4 * z + 5);
        private Vec3<float> GradLinearFunc(double x, double y, double z) => new Vec3<float>(2, 3, 4);

        // Quadratic: f(x,y,z) = x^2 + 2y^2 + 3z^2
        private float QuadFunc(double x, double y, double z) => (float)(x * x + 2 * y * y + 3 * z * z);
        private Vec3<float> GradQuadFunc(double x, double y, double z) => new Vec3<float>((float)(2 * x), (float)(4 * y), (float)(6 * z));
        
        // Cubic: f(x,y,z) = x^3 - 2y^3 + z^3
        private float CubicFunc(double x, double y, double z) => (float)(x*x*x - 2*y*y*y + z*z*z);
        private Vec3<float> GradCubicFunc(double x, double y, double z) => new Vec3<float>((float)(3*x*x), (float)(-6*y*y), (float)(3*z*z));

        // Quartic: f(x,y,z) = x^4
        private float QuarticFunc(double x, double y, double z) => (float)(x*x*x*x);
        private Vec3<float> GradQuarticFunc(double x, double y, double z) => new Vec3<float>((float)(4*x*x*x), 0, 0);

        // Quintic: f(x,y,z) = x^5
        private float QuinticFunc(double x, double y, double z) => (float)(x*x*x*x*x);
        private Vec3<float> GradQuinticFunc(double x, double y, double z) => new Vec3<float>((float)(5*x*x*x*x), 0, 0);

        // Sextic: f(x,y,z) = x^6
        private float SexticFunc(double x, double y, double z) => (float)(x*x*x*x*x*x);
        private Vec3<float> GradSexticFunc(double x, double y, double z) => new Vec3<float>((float)(6*x*x*x*x*x), 0, 0);


        // Test domain for stencils up to +/-3
        private readonly CoordBBox TestDomain = new CoordBBox(new Coord(-5, -5, -5), new Coord(5, 5, 5));
        private readonly Coord TestCoord = new Coord(0, 0, 0); // Test at origin for simplicity with polynomials
        private readonly Coord TestCoordOffset = new Coord(1, 1, 1); // Test at non-origin

        // Test cases for D1 schemes
        [TestCase(DScheme.FD_1ST, 2f)]
        [TestCase(DScheme.BD_1ST, 2f)]
        [TestCase(DScheme.CD_2ND, 2f)]
        [TestCase(DScheme.CD_4TH, 2f)]
        [TestCase(DScheme.CD_6TH, 2f)]
        [TestCase(DScheme.CD_2NDT, 2f)] // (G(i+2)-G(i-2))/4. For 2x, this is (8- (-8))/4 = 16/4 = 4. Expected is 2. This scheme is for 2*dx spacing.
                                        // If testing with dx=1, (2*(x+2) - 2*(x-2))/4 = (2x+4 - 2x+4)/4 = 8/4 = 2. It works.
        public void D1_InX_LinearFunc_ShouldBeExact(DScheme scheme, float expectedDerivative)
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float derivative = D1.InX<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);
            Assert.AreEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} failed for linear function.");
        }
        
        // Test D1.InY and D1.InZ for Linear function similarly
        [TestCase(DScheme.FD_1ST, 3f)]
        [TestCase(DScheme.BD_1ST, 3f)]
        [TestCase(DScheme.CD_2ND, 3f)]
        [TestCase(DScheme.CD_4TH, 3f)]
        [TestCase(DScheme.CD_6TH, 3f)]
        [TestCase(DScheme.CD_2NDT, 3f)]
        public void D1_InY_LinearFunc_ShouldBeExact(DScheme scheme, float expectedDerivative)
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float derivative = D1.InY<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);
            Assert.AreEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} failed for linear function (Y).");
        }

        [TestCase(DScheme.FD_1ST, 4f)]
        [TestCase(DScheme.BD_1ST, 4f)]
        [TestCase(DScheme.CD_2ND, 4f)]
        [TestCase(DScheme.CD_4TH, 4f)]
        [TestCase(DScheme.CD_6TH, 4f)]
        [TestCase(DScheme.CD_2NDT, 4f)]
        public void D1_InZ_LinearFunc_ShouldBeExact(DScheme scheme, float expectedDerivative)
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float derivative = D1.InZ<float, ITreeValueAccessor<float>>(accessor, TestCoord, scheme);
            Assert.AreEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} failed for linear function (Z).");
        }


        [TestCase(DScheme.CD_2ND, TestCoordOffset)] // 2*x at (1,1,1) = 2
        [TestCase(DScheme.CD_4TH, TestCoordOffset)]
        [TestCase(DScheme.CD_6TH, TestCoordOffset)]
        public void D1_InX_QuadraticFunc_ShouldBeExactForSecondOrderAndHigher(DScheme scheme, Coord coord)
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDerivative = GradQuadFunc(coord.X, coord.Y, coord.Z).X;
            float derivative = D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, scheme);
            Assert.AreEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} failed for quadratic function.");
        }
        // FD_1ST and BD_1ST will have error for quadratic, so they are not "exact"
        [Test]
        public void D1_InX_QuadraticFunc_FirstOrderSchemes_ShouldHaveError()
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDerivative = GradQuadFunc(TestCoordOffset.X, TestCoordOffset.Y, TestCoordOffset.Z).X; // 2*1 = 2
            
            float derivFD = D1.InX<float, ITreeValueAccessor<float>>(accessor, TestCoordOffset, DScheme.FD_1ST); // ( (1+1)^2 - 1^2 ) / 1 = 4-1=3
            Assert.AreNotEqual(expectedDerivative, derivFD, EpsilonF); 
            Assert.AreEqual(3.0f, derivFD, EpsilonF);


            float derivBD = D1.InX<float, ITreeValueAccessor<float>>(accessor, TestCoordOffset, DScheme.BD_1ST); // ( 1^2 - (1-1)^2 ) / 1 = 1-0=1
            Assert.AreNotEqual(expectedDerivative, derivBD, EpsilonF);
            Assert.AreEqual(1.0f, derivBD, EpsilonF);
        }
        
        [TestCase(DScheme.CD_4TH, TestCoordOffset)] // 4*x^3 at (1,1,1) = 4
        [TestCase(DScheme.CD_6TH, TestCoordOffset)]
        public void D1_InX_QuarticFunc_ShouldBeExactForFourthOrderAndHigher(DScheme scheme, Coord coord)
        {
            var grid = CreateScalarGrid(QuarticFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDerivative = GradQuarticFunc(coord.X, coord.Y, coord.Z).X;
            float derivative = D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, scheme);
            Assert.AreEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} failed for quartic function.");
        }
        
        [TestCase(DScheme.CD_2ND, TestCoordOffset)] // CD_2ND should have error for quartic
        public void D1_InX_QuarticFunc_LowerOrderSchemes_ShouldHaveError(DScheme scheme, Coord coord)
        {
            var grid = CreateScalarGrid(QuarticFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDerivative = GradQuarticFunc(coord.X, coord.Y, coord.Z).X; // 4*1^3 = 4
            float derivative = D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, scheme);
            Assert.AreNotEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} should not be exact for quartic.");
            // CD_2ND on x^4 at x=1: ((1+1)^4 - (1-1)^4) / 2 = (16 - 0)/2 = 8. Expected 4.
            Assert.AreEqual(8.0f, derivative, EpsilonF);
        }

        [TestCase(DScheme.CD_6TH, TestCoordOffset)] // 6*x^5 at (1,1,1) = 6
        public void D1_InX_SexticFunc_ShouldBeExactForSixthOrder(DScheme scheme, Coord coord)
        {
            var grid = CreateScalarGrid(SexticFunc, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDerivative = GradSexticFunc(coord.X, coord.Y, coord.Z).X;
            float derivative = D1.InX<float, ITreeValueAccessor<float>>(accessor, coord, scheme);
            Assert.AreEqual(expectedDerivative, derivative, EpsilonF, $"Scheme {scheme} failed for sextic function.");
        }


        [Test]
        public void D1_Gradient_LinearFunc_ShouldBeExact()
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var expectedGrad = GradLinearFunc(TestCoord.X, TestCoord.Y, TestCoord.Z);
            
            // Test with CD_2ND as representative
            var grad = D1.Gradient<float, ITreeValueAccessor<float>>(accessor, TestCoord, DScheme.CD_2ND);
            Assert.IsTrue(expectedGrad.IsApproxEqual(grad, EpsilonF));
        }

        [Test]
        public void D1_Gradient_QuadraticFunc_ShouldBeExactForCD2()
        {
            var grid = CreateScalarGrid(QuadFunc, TestDomain);
            var accessor = grid.GetAccessor();
            var expectedGrad = GradQuadFunc(TestCoordOffset.X, TestCoordOffset.Y, TestCoordOffset.Z); // At (1,1,1) -> (2,4,6)
            
            var grad = D1.Gradient<float, ITreeValueAccessor<float>>(accessor, TestCoordOffset, DScheme.CD_2ND);
            Assert.IsTrue(expectedGrad.IsApproxEqual(grad, EpsilonF));
        }

        // Vector field F(x,y,z) = (2x, 3y, 4z) -> Div(F) = 2 + 3 + 4 = 9
        private Vec3<float> VectorFuncLinear(double x, double y, double z) => new Vec3<float>((float)(2*x), (float)(3*y), (float)(4*z));
        private float DivVectorFuncLinear(double x, double y, double z) => 2f + 3f + 4f; // 9

        // Vector field F(x,y,z) = (x^2, y^2, z^2) -> Div(F) = 2x + 2y + 2z
        private Vec3<float> VectorFuncQuad(double x, double y, double z) => new Vec3<float>((float)(x*x), (float)(y*y), (float)(z*z));
        private float DivVectorFuncQuad(double x, double y, double z) => (float)(2*x + 2*y + 2*z);

        [TestCase(DScheme.FD_1ST)]
        [TestCase(DScheme.BD_1ST)]
        [TestCase(DScheme.CD_2ND)]
        public void D1_Divergence_LinearVectorFunc_ShouldBeExact(DScheme scheme)
        {
            var grid = CreateVectorGrid(VectorFuncLinear, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDiv = DivVectorFuncLinear(TestCoord.X, TestCoord.Y, TestCoord.Z); // At (0,0,0) -> 9
            
            float divergence = D1.Divergence<float, ITreeValueAccessor<Vec3<float>>>(accessor, TestCoord, scheme);
            Assert.AreEqual(expectedDiv, divergence, EpsilonF, $"Scheme {scheme} failed for linear vector field divergence.");
        }
        
        [TestCase(DScheme.CD_2ND, TestCoordOffset)] // At (1,1,1) -> 2*1 + 2*1 + 2*1 = 6
        public void D1_Divergence_QuadraticVectorFunc_ShouldBeExactForCD2(DScheme scheme, Coord coord)
        {
            var grid = CreateVectorGrid(VectorFuncQuad, TestDomain);
            var accessor = grid.GetAccessor();
            float expectedDiv = DivVectorFuncQuad(coord.X, coord.Y, coord.Z);
            
            float divergence = D1.Divergence<float, ITreeValueAccessor<Vec3<float>>>(accessor, coord, scheme);
            Assert.AreEqual(expectedDiv, divergence, EpsilonF, $"Scheme {scheme} failed for quadratic vector field divergence.");
        }
        
        [Test]
        public void D1_WENO5_And_HJWENO5_Placeholders_ShouldThrowNotImplemented()
        {
            var grid = CreateScalarGrid(LinearFunc, TestDomain);
            var accessor = grid.GetAccessor();
            Assert.Throws<NotImplementedException>(() => D1.InX<float, ITreeValueAccessor<float>>(accessor, TestCoord, DScheme.WENO5));
            Assert.Throws<ArgumentException>(() => D1.InX<float, ITreeValueAccessor<float>>(accessor, TestCoord, DScheme.HJWENO5)); 
            // Current D1.InX throws ArgumentException for HJWENO5 if velocity is not provided.
            // A specific HjWeno5InX method would be called instead if velocity was available.
        }
    }
}
