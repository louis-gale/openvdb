// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Numerics; // For IFloatingPointIeee754
using OpenVDB.Core.Tree; // For ITreeValueAccessor

namespace OpenVDB.Core.Math
{
    /// <summary>
    /// Stencil types for first-order accurate finite-difference approximations of derivatives.
    /// </summary>
    public enum DScheme
    {
        Invalid = 0,
        FD_1ST,  // First-order accurate forward  (1st order bias) dx = G(i+1) - G(i)
        BD_1ST,  // First-order accurate backward (1st order bias) dx = G(i) - G(i-1)
        CD_2ND,  // Second-order accurate central (2nd order bias) dx = (G(i+1) - G(i-1))/2
        CD_4TH,  // Fourth-order accurate central (4th order bias)
        CD_6TH,  // Sixth-order accurate central  (6th order bias)
        WENO5,   // Fifth order accurate Weighted Essentially Non-Oscillatory scheme
        HJWENO5, // Fifth order accurate Hamilton-Jacobi Weighted Essentially Non-Oscillatory scheme
        CD_2NDT  // Second-order accurate central assuming 2*dx spacing
    }

    /// <summary>
    /// Stencil types for second-order accurate finite-difference approximations of derivatives.
    /// </summary>
    public enum DDScheme
    {
        Invalid = 0,
        CD_SECOND, // Second-order accurate central differencing
        CD_FOURTH, // Fourth-order accurate central differencing
        CD_SIXTH   // Sixth-order accurate central differencing
    }

    /// <summary>
    /// Stencil types for biased finite-difference approximations of derivatives.
    /// </summary>
    public enum BiasedGradientScheme
    {
        FIRST_BIAS,  // O(dx) Fwd/Bwd difference (Box)
        SECOND_BIAS, // O(dx^2) Fwd/Bwd difference (Linear)
        THIRD_BIAS,  // O(dx^3) Fwd/Bwd difference (Quadratic)
        WENO5_BIAS,  // O(dx^5) WENO
        HJWENO5_BIAS // O(dx^5) HJ-WENO
    }

    // Helper struct to define stencil offsets and weights
    // This is a simplified concept for now. C++ uses complex template metaprogramming.
    internal struct StencilCoeff<T> where T : struct, IFloatingPointIeee754<T>
    {
        public readonly int[] Offsets;
        public readonly T[] Weights; // For D1, denominator is usually separate
        public readonly T Denominator;

        public StencilCoeff(int[] offsets, T[] weights, T denominator)
        {
            Offsets = offsets;
            Weights = weights;
            Denominator = denominator;
        }
    }


    /// <summary>
    /// First derivative finite difference schemes.
    /// </summary>
    public static class D1
    {
        // Helper to get value, assuming background is 0 or handled appropriately by accessor for FD.
        private static TValue Get<TValue, TAccessor>(TAccessor accessor, Coord ijk, int dx, int dy, int dz)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            return accessor.GetValue(ijk.OffsetBy(dx, dy, dz));
        }

        public static TValue InX<TValue, TAccessor>(TAccessor accessor, Coord ijk, DScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            switch (scheme)
            {
                case DScheme.FD_1ST: // G(i+1) - G(i)
                    return Get(accessor, ijk, 1, 0, 0) - Get(accessor, ijk, 0, 0, 0);
                case DScheme.BD_1ST: // G(i) - G(i-1)
                    return Get(accessor, ijk, 0, 0, 0) - Get(accessor, ijk, -1, 0, 0);
                case DScheme.CD_2ND: // (G(i+1) - G(i-1))/2
                    return (Get(accessor, ijk, 1, 0, 0) - Get(accessor, ijk, -1, 0, 0)) / TValue.CreateChecked(2.0);
                case DScheme.CD_4TH: // (-G(i+2) + 8G(i+1) - 8G(i-1) + G(i-2))/12
                    return (-Get(accessor, ijk, 2, 0, 0) + TValue.CreateChecked(8.0) * Get(accessor, ijk, 1, 0, 0)
                            - TValue.CreateChecked(8.0) * Get(accessor, ijk, -1, 0, 0) + Get(accessor, ijk, -2, 0, 0))
                           / TValue.CreateChecked(12.0);
                case DScheme.CD_6TH:
                    return (Get(accessor, ijk, 3,0,0) - TValue.CreateChecked(9.0) * Get(accessor, ijk, 2,0,0)
                           + TValue.CreateChecked(45.0) * Get(accessor, ijk, 1,0,0) - TValue.CreateChecked(45.0) * Get(accessor, ijk, -1,0,0)
                           + TValue.CreateChecked(9.0) * Get(accessor, ijk, -2,0,0) - Get(accessor, ijk, -3,0,0))
                           / TValue.CreateChecked(60.0);
                case DScheme.CD_2NDT: // (G(i+2) - G(i-2))/4  (Central diff over 2*dx, so G(i+2dx)-G(i-2dx) / (4dx) )
                                      // Assuming dx=1, this is (G(i+2) - G(i-2))/4
                    return (Get(accessor, ijk, 2,0,0) - Get(accessor, ijk, -2,0,0)) / TValue.CreateChecked(4.0);

                // WENO5 and HJWENO5 are complex and require helper functions. Placeholder for now.
                case DScheme.WENO5:
                case DScheme.HJWENO5:
                    throw new NotImplementedException($"Scheme {scheme} not fully implemented for D1.InX.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme), $"Unsupported scheme: {scheme}");
            }
        }

        public static TValue InY<TValue, TAccessor>(TAccessor accessor, Coord ijk, DScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            switch (scheme)
            {
                case DScheme.FD_1ST: return Get(accessor, ijk, 0, 1, 0) - Get(accessor, ijk, 0, 0, 0);
                case DScheme.BD_1ST: return Get(accessor, ijk, 0, 0, 0) - Get(accessor, ijk, 0, -1, 0);
                case DScheme.CD_2ND: return (Get(accessor, ijk, 0, 1, 0) - Get(accessor, ijk, 0, -1, 0)) / TValue.CreateChecked(2.0);
                case DScheme.CD_4TH:
                    return (-Get(accessor, ijk, 0, 2, 0) + TValue.CreateChecked(8.0) * Get(accessor, ijk, 0, 1, 0)
                            - TValue.CreateChecked(8.0) * Get(accessor, ijk, 0, -1, 0) + Get(accessor, ijk, 0, -2, 0))
                           / TValue.CreateChecked(12.0);
                case DScheme.CD_6TH:
                     return (Get(accessor, ijk, 0,3,0) - TValue.CreateChecked(9.0) * Get(accessor, ijk, 0,2,0)
                           + TValue.CreateChecked(45.0) * Get(accessor, ijk, 0,1,0) - TValue.CreateChecked(45.0) * Get(accessor, ijk, 0,-1,0)
                           + TValue.CreateChecked(9.0) * Get(accessor, ijk, 0,-2,0) - Get(accessor, ijk, 0,-3,0))
                           / TValue.CreateChecked(60.0);
                case DScheme.CD_2NDT:
                    return (Get(accessor, ijk, 0,2,0) - Get(accessor, ijk, 0,-2,0)) / TValue.CreateChecked(4.0);
                case DScheme.WENO5:
                case DScheme.HJWENO5:
                    throw new NotImplementedException($"Scheme {scheme} not fully implemented for D1.InY.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme), $"Unsupported scheme: {scheme}");
            }
        }

        public static TValue InZ<TValue, TAccessor>(TAccessor accessor, Coord ijk, DScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
             switch (scheme)
            {
                case DScheme.FD_1ST: return Get(accessor, ijk, 0, 0, 1) - Get(accessor, ijk, 0, 0, 0);
                case DScheme.BD_1ST: return Get(accessor, ijk, 0, 0, 0) - Get(accessor, ijk, 0, 0, -1);
                case DScheme.CD_2ND: return (Get(accessor, ijk, 0, 0, 1) - Get(accessor, ijk, 0, 0, -1)) / TValue.CreateChecked(2.0);
                case DScheme.CD_4TH:
                    return (-Get(accessor, ijk, 0, 0, 2) + TValue.CreateChecked(8.0) * Get(accessor, ijk, 0, 0, 1)
                            - TValue.CreateChecked(8.0) * Get(accessor, ijk, 0, 0, -1) + Get(accessor, ijk, 0, 0, -2))
                           / TValue.CreateChecked(12.0);
                case DScheme.CD_6TH:
                     return (Get(accessor, ijk, 0,0,3) - TValue.CreateChecked(9.0) * Get(accessor, ijk, 0,0,2)
                           + TValue.CreateChecked(45.0) * Get(accessor, ijk, 0,0,1) - TValue.CreateChecked(45.0) * Get(accessor, ijk, 0,0,-1)
                           + TValue.CreateChecked(9.0) * Get(accessor, ijk, 0,0,-2) - Get(accessor, ijk, 0,0,-3))
                           / TValue.CreateChecked(60.0);
                case DScheme.CD_2NDT:
                    return (Get(accessor, ijk, 0,0,2) - Get(accessor, ijk, 0,0,-2)) / TValue.CreateChecked(4.0);
                case DScheme.WENO5:
                case DScheme.HJWENO5:
                    throw new NotImplementedException($"Scheme {scheme} not fully implemented for D1.InZ.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme), $"Unsupported scheme: {scheme}");
            }
        }

        // Gradient (Vec3<TValue>)
        public static Vec3<TValue> Gradient<TValue, TAccessor>(TAccessor accessor, Coord ijk, DScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            return new Vec3<TValue>(
                InX<TValue, TAccessor>(accessor, ijk, scheme),
                InY<TValue, TAccessor>(accessor, ijk, scheme),
                InZ<TValue, TAccessor>(accessor, ijk, scheme)
            );
        }

        // Divergence for Vec3<TValue> field
        public static TValue Divergence<TValue, TAccessor>(TAccessor accessor, Coord ijk, DScheme scheme)
            where TAccessor : ITreeValueAccessor<Vec3<TValue>> // Accessor returns Vec3<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            // Div(F) = dFx/dx + dFy/dy + dFz/dz
            // D1Vec will get component, then apply D1.
            // For now, direct implementation for clarity.

            // dFx/dx
            TValue dFx_dx;
            switch (scheme)
            {
                case DScheme.FD_1ST: dFx_dx = accessor.GetValue(ijk.OffsetBy(1,0,0)).X - accessor.GetValue(ijk).X; break;
                case DScheme.BD_1ST: dFx_dx = accessor.GetValue(ijk).X - accessor.GetValue(ijk.OffsetBy(-1,0,0)).X; break;
                case DScheme.CD_2ND: dFx_dx = (accessor.GetValue(ijk.OffsetBy(1,0,0)).X - accessor.GetValue(ijk.OffsetBy(-1,0,0)).X) / TValue.CreateChecked(2.0); break;
                // Add other schemes if needed, or use D1Vec logic below
                default: throw new NotImplementedException($"Divergence for scheme {scheme} not fully implemented.");
            }

            // dFy/dy
            TValue dFy_dy;
             switch (scheme)
            {
                case DScheme.FD_1ST: dFy_dy = accessor.GetValue(ijk.OffsetBy(0,1,0)).Y - accessor.GetValue(ijk).Y; break;
                case DScheme.BD_1ST: dFy_dy = accessor.GetValue(ijk).Y - accessor.GetValue(ijk.OffsetBy(0,-1,0)).Y; break;
                case DScheme.CD_2ND: dFy_dy = (accessor.GetValue(ijk.OffsetBy(0,1,0)).Y - accessor.GetValue(ijk.OffsetBy(0,-1,0)).Y) / TValue.CreateChecked(2.0); break;
                default: throw new NotImplementedException($"Divergence for scheme {scheme} not fully implemented.");
            }

            // dFz/dz
            TValue dFz_dz;
            switch (scheme)
            {
                case DScheme.FD_1ST: dFz_dz = accessor.GetValue(ijk.OffsetBy(0,0,1)).Z - accessor.GetValue(ijk).Z; break;
                case DScheme.BD_1ST: dFz_dz = accessor.GetValue(ijk).Z - accessor.GetValue(ijk.OffsetBy(0,0,-1)).Z; break;
                case DScheme.CD_2ND: dFz_dz = (accessor.GetValue(ijk.OffsetBy(0,0,1)).Z - accessor.GetValue(ijk.OffsetBy(0,0,-1)).Z) / TValue.CreateChecked(2.0); break;
                default: throw new NotImplementedException($"Divergence for scheme {scheme} not fully implemented.");
            }

            return dFx_dx + dFy_dy + dFz_dz;
        }

        // Curl for Vec3<TValue> field (returns Vec3<TValue>)
        public static Vec3<TValue> Curl<TValue, TAccessor>(TAccessor accessor, Coord ijk, DScheme scheme)
            where TAccessor : ITreeValueAccessor<Vec3<TValue>>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            // Curl(F) = (dFz/dy - dFy/dz, dFx/dz - dFz/dx, dFy/dx - dFx/dy)
            // This requires applying D1.InY to Fz, D1.InZ to Fy etc.
            // For example, dFz/dy:
            // TValue dFz_dy = D1.InY<TValue, ComponentAccessor<Vec3<TValue>, TValue, TAccessor>>(new ComponentAccessor(accessor, 2), ijk, scheme);
            // This is getting complex without D1Vec.
            throw new NotImplementedException("D1.Curl requires D1Vec or more complex component access.");
        }
    }

    // Placeholder for D1Vec, D2 - to be implemented
    // public static class D1Vec { } // Will be implemented below
    // public static class D2 { }    // Will be implemented below

    // Helper functions for WENO schemes
    // Based on openvdb/math/FiniteDifference.h internal details
    internal static class WenoHelpers
    {
        private const double WENO_EPSILON = 1.0e-6; // Epsilon used in C++ WENO

        // Optimal weights for WENO5 (central) and HJWENO5 (biased)
        // For central WENO5 (used in D1::weno5):
        // C++ D1::weno5 uses c0=0.1, c1=0.6, c2=0.3 if v_m2 > v_p2 (backward leaning data)
        // and c0=0.3, c1=0.6, c2=0.1 if v_m2 <= v_p2 (forward leaning data or centered)
        // These are applied to stencils d0, d1, d2 respectively.
        // d0 = (3*v0 - 4*vm1 + vm2)/2
        // d1 = (vp1 - vm1)/2
        // d2 = (-3*v0 + 4*vp1 - vp2)/2
        private static readonly double[] WENO5_OptWeights_BackwardBias = { 0.1, 0.6, 0.3 };
        private static readonly double[] WENO5_OptWeights_ForwardBias = { 0.3, 0.6, 0.1 };

        // For HJWENO5 (used in D1::hjWENO5Impl):
        // D- (plus=false in C++, backward bias): Optimal weights {0.3, 0.6, 0.1} for stencils p0, p1, p2
        // D+ (plus=true in C++, forward bias):  Optimal weights {0.1, 0.6, 0.3} for stencils p0, p1, p2
        private static readonly double[] HJWENO5_OptWeights_DMinus = { 0.3, 0.6, 0.1 }; // For p0, p1, p2 of D-
        private static readonly double[] HJWENO5_OptWeights_DPlus = { 0.1, 0.6, 0.3 };  // For p0, p1, p2 of D+


        // Generic WENO5 implementation (central bias, adaptive optimal weights)
        // Matches C++ D1::weno5(v_m2, v_m1, v_0, v_p1, v_p2)
        public static TValue Weno5<TValue>(TValue v_m2, TValue v_m1, TValue v_0, TValue v_p1, TValue v_p2)
             where TValue : struct, IFloatingPointIeee754<TValue>
        {
            TValue half = TValue.CreateChecked(0.5);
            TValue two = TValue.CreateChecked(2.0);
            TValue three = TValue.CreateChecked(3.0);
            TValue four = TValue.CreateChecked(4.0);
            TValue twelve = TValue.CreateChecked(12.0);
            TValue thirteen_div_twelve = TValue.CreateChecked(13.0) / twelve;
            TValue one_fourth = TValue.CreateChecked(0.25);
            TValue epsilon = TValue.CreateChecked(WENO_EPSILON);

            // Candidate stencils (for f'(x_i) at v_0)
            TValue d0 = (three * v_0 - four * v_m1 + v_m2) * half;
            TValue d1 = (v_p1 - v_m1) * half;
            TValue d2 = (v_p2 - four * v_p1 + three * v_0) * -half; // Equivalent to (-vp2 + 4*vp1 - 3*v0)/2

            // Smoothness indicators (beta coefficients)
            TValue beta0_term1 = v_m2 - two * v_m1 + v_0;
            TValue beta0_term2 = v_m2 - four * v_m1 + three * v_0;
            TValue beta0 = thirteen_div_twelve * beta0_term1 * beta0_term1 + one_fourth * beta0_term2 * beta0_term2;

            TValue beta1_term1 = v_m1 - two * v_0 + v_p1;
            TValue beta1_term2 = v_m1 - v_p1;
            TValue beta1 = thirteen_div_twelve * beta1_term1 * beta1_term1 + one_fourth * beta1_term2 * beta1_term2;

            TValue beta2_term1 = v_0 - two * v_p1 + v_p2;
            TValue beta2_term2 = three * v_0 - four * v_p1 + v_p2;
            TValue beta2 = thirteen_div_twelve * beta2_term1 * beta2_term1 + one_fourth * beta2_term2 * beta2_term2;

            // Choose optimal weights based on data trend (as in C++ D1::weno5)
            double[] cOpt = (v_m2 > v_p2) ? WENO5_OptWeights_BackwardBias : WENO5_OptWeights_ForwardBias;

            // Weights (alpha coefficients)
            TValue alpha0_den = (beta0 + epsilon) * (beta0 + epsilon);
            TValue alpha1_den = (beta1 + epsilon) * (beta1 + epsilon);
            TValue alpha2_den = (beta2 + epsilon) * (beta2 + epsilon);

            TValue alpha0 = TValue.CreateChecked(cOpt[0]) / alpha0_den;
            TValue alpha1 = TValue.CreateChecked(cOpt[1]) / alpha1_den;
            TValue alpha2 = TValue.CreateChecked(cOpt[2]) / alpha2_den;

            TValue sum_alpha = alpha0 + alpha1 + alpha2;

            // Normalized weights (omega)
            // Handle sum_alpha == 0 case to avoid NaN, though epsilon should prevent it.
            if (TValue.IsZero(sum_alpha)) sum_alpha = TValue.One; // Or distribute optimal weights directly

            TValue w0 = alpha0 / sum_alpha;
            TValue w1 = alpha1 / sum_alpha;
            TValue w2 = alpha2 / sum_alpha;

            return w0 * d0 + w1 * d1 + w2 * d2;
        }

        // HJWENO5 biased schemes
        // HJWENO5 D- (backward bias, for positive velocity component u+ > 0)
        // Matches C++ hjWENO5Impl(v_m2, v_m1, v_0, v_p1, v_p2, plus=false)
        public static TValue HjWeno5Minus<TValue>(TValue v_m2, TValue v_m1, TValue v_0, TValue v_p1, TValue v_p2)
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            TValue half = TValue.CreateChecked(0.5);
            TValue two = TValue.CreateChecked(2.0);
            TValue three = TValue.CreateChecked(3.0);
            TValue four = TValue.CreateChecked(4.0);
            TValue thirteen_div_twelve = TValue.CreateChecked(13.0) / TValue.CreateChecked(12.0);
            TValue one_fourth = TValue.CreateChecked(0.25);
            TValue epsilon = TValue.CreateChecked(WENO_EPSILON);

            // Candidate stencils for D- (backward biased)
            TValue d0 = (three * v_0 - four * v_m1 + v_m2) * half;
            TValue d1 = (v_p1 - v_m1) * half;
            TValue d2 = (v_p2 + two * v_m1 - three * v_0) * -half; // (-v_p2 - 2*v_m1 + 3*v_0)/2

            // Smoothness Indicators (beta coefficients) for HJWENO5 D-
            TValue beta0 = thirteen_div_twelve * Sqr(v_0 - two*v_m1 + v_m2) + one_fourth * Sqr(v_0 - four*v_m1 + three*v_m2);
            TValue beta1 = thirteen_div_twelve * Sqr(v_p1 - two*v_0 + v_m1) + one_fourth * Sqr(v_p1 - v_m1);
            TValue beta2 = thirteen_div_twelve * Sqr(v_p2 - two*v_p1 + v_0) + one_fourth * Sqr(three*v_0 - four*v_p1 + v_p2); // Note: C++ uses SQR(v[0] - v[2]) which is SQR(v0-vp2) for this IS for D-
                                                                                                                            // The original Jiang-Shu 1996 for these stencils:
                                                                                                                            // IS_k for (k-1, k, k+1) stencil: (13/12)(f_{k-1}-2f_k+f_{k+1})^2 + (1/4)(f_{k-1}-f_{k+1})^2
                                                                                                                            // IS_k for (k-2, k-1, k) stencil: (13/12)(f_{k-2}-2f_{k-1}+f_k)^2 + (1/4)(f_{k-2}-4f_{k-1}+3f_k)^2
                                                                                                                            // Let's use the direct C++ formulas for betas for HJWENO5
            beta0 = thirteen_div_twelve * Sqr(v_m2 - two*v_m1 + v_0) + one_fourth * Sqr(v_m2 - four*v_m1 + three*v_0); // For d0 = (3v0 - 4vm1 + vm2)/2
            beta1 = thirteen_div_twelve * Sqr(v_m1 - two*v_0 + v_p1) + one_fourth * Sqr(v_m1 - v_p1);                   // For d1 = (vp1 - vm1)/2
            beta2 = thirteen_div_twelve * Sqr(v_0 - two*v_p1 + v_p2) + one_fourth * Sqr(three*v_0 - four*v_p1 + v_p2); // For d2 = (-3v0 + 4vp1 - vp2)/2 (if d2 was forward)
                                                                                                                    // C++ HJWENO5 p2_m stencil: (-v_p2 - 2*v_m1 + 3*v_0)/2.
                                                                                                                    // Beta for this stencil (k=0, using points i, i-1, i-2 where stencil center is i):
                                                                                                                    // This needs careful mapping. C++ code for hjweno5Impl:
                                                                                                                    // b[0] = IS(v[2], v[1], v[0]); v[2]=v0, v[1]=vm1, v[0]=vm2
                                                                                                                    // b[1] = IS(v[3], v[2], v[1]); v[3]=vp1, v[2]=v0, v[1]=vm1
                                                                                                                    // b[2] = IS(v[4], v[3], v[2]); v[4]=vp2, v[3]=vp1, v[2]=v0
                                                                                                                    // where IS(a,b,c) = (13/12)(a-2b+c)^2 + (1/4)(a-c)^2 for plus=false (D-)
            beta0 = thirteen_div_twelve * Sqr(v_0 - two*v_m1 + v_m2) + one_fourth * Sqr(v_0 - v_m2);
            beta1 = thirteen_div_twelve * Sqr(v_p1 - two*v_0 + v_m1) + one_fourth * Sqr(v_p1 - v_m1);
            beta2 = thirteen_div_twelve * Sqr(v_p2 - two*v_p1 + v_0) + one_fourth * Sqr(v_p2 - v_0);

            // Optimal weights for D- (backward bias): {0.3, 0.6, 0.1} for stencils d0, d1, d2 (C++: c_m)
            TValue alpha0 = TValue.CreateChecked(HJWENO5_OptWeights_DMinus[0]) / Sqr(beta0 + epsilon);
            TValue alpha1 = TValue.CreateChecked(HJWENO5_OptWeights_DMinus[1]) / Sqr(beta1 + epsilon);
            TValue alpha2 = TValue.CreateChecked(HJWENO5_OptWeights_DMinus[2]) / Sqr(beta2 + epsilon);

            TValue sum_alpha = alpha0 + alpha1 + alpha2;
            if (TValue.IsZero(sum_alpha)) sum_alpha = TValue.One;

            return (alpha0 * d0 + alpha1 * d1 + alpha2 * d2) / sum_alpha;
        }

        // HJWENO5 D+ (forward bias, for negative velocity component u- < 0)
        // Matches C++ hjWENO5Impl(v_m2, v_m1, v_0, v_p1, v_p2, plus=true)
        public static TValue HjWeno5Plus<TValue>(TValue v_m2, TValue v_m1, TValue v_0, TValue v_p1, TValue v_p2)
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            TValue half = TValue.CreateChecked(0.5);
            TValue two = TValue.CreateChecked(2.0);
            TValue three = TValue.CreateChecked(3.0);
            TValue four = TValue.CreateChecked(4.0);
            TValue thirteen_div_twelve = TValue.CreateChecked(13.0) / TValue.CreateChecked(12.0);
            TValue one_fourth = TValue.CreateChecked(0.25);
            TValue epsilon = TValue.CreateChecked(WENO_EPSILON);

            // Candidate stencils for D+ (forward biased)
            TValue d0 = (v_m2 + two * v_p1 - three * v_0) * half; // (-3v_0 + 2v_p1 + v_m2)/2
            TValue d1 = (v_p1 - v_m1) * half;
            TValue d2 = (three * v_0 - four * v_m1 + v_m2) * -half; // (-3v_0 + 4v_m1 - v_m2)/2 (Symmetric to D- d0)
                                                                   // C++ D+ stencils:
                                                                   // d0 = (v_m2 + 2*v_p1 - 3*v_0)*0.5;
                                                                   // d1 = (v_p1 - v_m1)*0.5;
                                                                   // d2 = (v_0*3 - 4*v_m1 + v_m2)*(-0.5);
            d0 = (v_m2 + two * v_p1 - three * v_0) * half;
            d1 = (v_p1 - v_m1) * half;
            d2 = (three * v_0 - four * v_m1 + v_m2) * -half;


            // Smoothness Indicators (beta coefficients) for HJWENO5 D+
            // C++ hjWENO5Impl for plus=true (D+):
            // b[0] = IS( v[0], v[1], v[2]) -> IS(v_m2, v_m1, v_0)
            // b[1] = IS(v[-1],v[0], v[1]) -> IS(v_m1, v_0,  v_p1)  -- Error in C++ comment, should be v[-1]=v_m1, v[0]=v_0, v[1]=v_p1
            // b[2] = IS(v[-2],v[-1],v[0]) -> IS(v_0,  v_p1, v_p2)
            // where IS(a,b,c) = (13/12)(a-2b+c)^2 + (1/4)*(3a-4b+c)^2 for D+
            beta0 = thirteen_div_twelve * Sqr(v_m2 - two*v_m1 + v_0) + one_fourth * Sqr(three*v_m2 - four*v_m1 + v_0);
            beta1 = thirteen_div_twelve * Sqr(v_m1 - two*v_0 + v_p1) + one_fourth * Sqr(v_m1 - v_p1); // C++ uses (a-c)^2 for this stencil's beta
            beta2 = thirteen_div_twelve * Sqr(v_0 - two*v_p1 + v_p2) + one_fourth * Sqr(v_0 - four*v_p1 + three*v_p2);

            // Optimal weights for D+ (forward bias): {0.1, 0.6, 0.3} for stencils d0, d1, d2 (C++: c_p)
            TValue alpha0 = TValue.CreateChecked(HJWENO5_OptWeights_DPlus[0]) / Sqr(beta0 + epsilon);
            TValue alpha1 = TValue.CreateChecked(HJWENO5_OptWeights_DPlus[1]) / Sqr(beta1 + epsilon);
            TValue alpha2 = TValue.CreateChecked(HJWENO5_OptWeights_DPlus[2]) / Sqr(beta2 + epsilon);

            TValue sum_alpha = alpha0 + alpha1 + alpha2;
            if (TValue.IsZero(sum_alpha)) sum_alpha = TValue.One;

            return (alpha0 * d0 + alpha1 * d1 + alpha2 * d2) / sum_alpha;
        }

        private static TValue Sqr<TValue>(TValue x) where TValue : struct, IMultiplyOperators<TValue, TValue, TValue> => x * x;
    }
}
