// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Numerics;
using OpenVDB.Core.Tree;
using OpenVDB.Math;

namespace OpenVDB.Core.Math.Operators
{
    /// <summary>
    /// Computes the gradient in index space using specified finite difference schemes.
    /// </summary>
    public static class ISGradient
    {
        public static Vec3<TValue> Result<TValue, TAccessor>(
            TAccessor accessor,
            Coord ijk,
            DScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            return new Vec3<TValue>(
                D1.InX<TValue, TAccessor>(accessor, ijk, scheme),
                D1.InY<TValue, TAccessor>(accessor, ijk, scheme),
                D1.InZ<TValue, TAccessor>(accessor, ijk, scheme)
            );
        }
    }

    /// <summary>
    /// Computes a biased gradient in index space, choosing forward or backward
    /// differences based on a bias vector.
    /// </summary>
    public static class ISGradientBiased
    {
        private static DScheme GetFD_Scheme(BiasedGradientScheme scheme)
        {
            switch (scheme)
            {
                case BiasedGradientScheme.FIRST_BIAS: return DScheme.FD_1ST;
                case BiasedGradientScheme.SECOND_BIAS: return DScheme.CD_2ND; // C++ uses FD_2ND_ORDER which is not directly in DScheme enum
                                                                            // It's a 3-point forward stencil: (-3f0 + 4f1 - f2)/2h
                                                                            // This is not FD_1ST. For now, map to CD_2ND as a placeholder,
                                                                            // or needs specific FD_2ND/BD_2ND in D1.cs
                                                                            // For simplicity, will use FD_1ST/BD_1ST for now and note this.
                                                                            // Let's assume for this port, SECOND_BIAS implies using a central scheme if available
                                                                            // or higher order one-sided if they were defined.
                                                                            // C++ BIAS_SCHEME<SECOND_BIAS>::FD is FD_2ND_ORDER.
                                                                            // Let's map to best available forward/backward for now.
                    Console.WriteLine($"Warning: BiasedGradientScheme.{scheme} mapped to DScheme.FD_1ST (FD part). Full higher-order bias not yet ported.");
                    return DScheme.FD_1ST;
                case BiasedGradientScheme.THIRD_BIAS:
                    Console.WriteLine($"Warning: BiasedGradientScheme.{scheme} mapped to DScheme.FD_1ST (FD part). Full higher-order bias not yet ported.");
                    return DScheme.FD_1ST;
                case BiasedGradientScheme.WENO5_BIAS: return DScheme.WENO5; // WENO5 itself is centrally biased but uses upwind stencils
                case BiasedGradientScheme.HJWENO5_BIAS: return DScheme.HJWENO5; // HJWENO5 is inherently biased by velocity
                default: throw new ArgumentOutOfRangeException(nameof(scheme));
            }
        }

        private static DScheme GetBD_Scheme(BiasedGradientScheme scheme)
        {
            switch (scheme)
            {
                case BiasedGradientScheme.FIRST_BIAS: return DScheme.BD_1ST;
                case BiasedGradientScheme.SECOND_BIAS:
                     Console.WriteLine($"Warning: BiasedGradientScheme.{scheme} mapped to DScheme.BD_1ST (BD part). Full higher-order bias not yet ported.");
                    return DScheme.BD_1ST;
                case BiasedGradientScheme.THIRD_BIAS:
                     Console.WriteLine($"Warning: BiasedGradientScheme.{scheme} mapped to DScheme.BD_1ST (BD part). Full higher-order bias not yet ported.");
                    return DScheme.BD_1ST;
                case BiasedGradientScheme.WENO5_BIAS: return DScheme.WENO5; // See above
                case BiasedGradientScheme.HJWENO5_BIAS: return DScheme.HJWENO5; // See above
                default: throw new ArgumentOutOfRangeException(nameof(scheme));
            }
        }

        public static Vec3<TValue> Result<TValue, TAccessor, TBiasValue>(
            TAccessor accessor,
            Coord ijk,
            BiasedGradientScheme scheme,
            Vec3<TBiasValue> biasVector)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
            where TBiasValue : struct, IFloatingPointIeee754<TBiasValue> // Bias vector components are floats
        {
            if (scheme == BiasedGradientScheme.HJWENO5_BIAS)
            {
                // HJWENO5 in D1.cs expects direct velocity, not just a bias vector to pick FD/BD version of HJWENO5.
                // This operator in C++ passes the bias vector component as velocity.
                return new Vec3<TValue>(
                    D1.HjWeno5InX(accessor, ijk, Double.CreateChecked(biasVector.X)), // Assuming biasVector component is velocity
                    D1.HjWeno5InY(accessor, ijk, Double.CreateChecked(biasVector.Y)),
                    D1.HjWeno5InZ(accessor, ijk, Double.CreateChecked(biasVector.Z))
                );
            }
            if (scheme == BiasedGradientScheme.WENO5_BIAS) // Standard WENO5 is centrally biased
            {
                 return ISGradient.Result(accessor, ijk, DScheme.WENO5);
            }


            DScheme fdScheme = GetFD_Scheme(scheme);
            DScheme bdScheme = GetBD_Scheme(scheme);

            TValue dx = biasVector.X < TBiasValue.Zero ? D1.InX(accessor, ijk, fdScheme) : D1.InX(accessor, ijk, bdScheme);
            TValue dy = biasVector.Y < TBiasValue.Zero ? D1.InY(accessor, ijk, fdScheme) : D1.InY(accessor, ijk, bdScheme);
            TValue dz = biasVector.Z < TBiasValue.Zero ? D1.InZ(accessor, ijk, fdScheme) : D1.InZ(accessor, ijk, bdScheme);

            return new Vec3<TValue>(dx, dy, dz);
        }
    }

    /// <summary>
    /// Computes the squared norm of the Godunov flux difference for a gradient.
    /// </summary>
    public static class ISGradientNormSqrd
    {
        // Helper to map BiasedGradientScheme to DScheme for FD/BD
        // Note: This is simplified. C++ uses template metaprogramming to get specific FD_Nth_Order_Bias stencils.
        // For now, we map to basic FD_1ST/BD_1ST or WENO/HJWENO.
        private static DScheme GetFD_Scheme(BiasedGradientScheme scheme)
        {
            switch (scheme)
            {
                case BiasedGradientScheme.FIRST_BIAS: return DScheme.FD_1ST;
                case BiasedGradientScheme.SECOND_BIAS: // C++ uses FD_2ND_ORDER_BIAS
                case BiasedGradientScheme.THIRD_BIAS:  // C++ uses FD_3RD_ORDER_BIAS
                    Console.WriteLine($"Warning: BiasedGradientScheme.{scheme} for ISGradientNormSqrd mapped to DScheme.FD_1ST. Higher-order bias not fully ported.");
                    return DScheme.FD_1ST;
                case BiasedGradientScheme.WENO5_BIAS: return DScheme.WENO5;
                case BiasedGradientScheme.HJWENO5_BIAS: return DScheme.HJWENO5; // HJWENO5 needs velocity for D1.InX etc.
                default: throw new ArgumentOutOfRangeException(nameof(scheme));
            }
        }

        private static DScheme GetBD_Scheme(BiasedGradientScheme scheme)
        {
            switch (scheme)
            {
                case BiasedGradientScheme.FIRST_BIAS: return DScheme.BD_1ST;
                case BiasedGradientScheme.SECOND_BIAS:
                case BiasedGradientScheme.THIRD_BIAS:
                     Console.WriteLine($"Warning: BiasedGradientScheme.{scheme} for ISGradientNormSqrd mapped to DScheme.BD_1ST. Higher-order bias not fully ported.");
                    return DScheme.BD_1ST;
                case BiasedGradientScheme.WENO5_BIAS: return DScheme.WENO5;
                case BiasedGradientScheme.HJWENO5_BIAS: return DScheme.HJWENO5;
                default: throw new ArgumentOutOfRangeException(nameof(scheme));
            }
        }

        public static TValue Result<TValue, TAccessor>(
            TAccessor accessor,
            Coord ijk,
            BiasedGradientScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            TValue valCenter = accessor.GetValue(ijk);
            bool positiveSign = valCenter > TValue.Zero;

            Vec3<TValue> upwindGradient;   // D+ (typically forward differences if valCenter < 0, or chosen by HJWENO if valCenter > 0)
            Vec3<TValue> downwindGradient; // D- (typically backward differences if valCenter > 0, or chosen by HJWENO if valCenter < 0)

            if (scheme == BiasedGradientScheme.HJWENO5_BIAS)
            {
                // For HJWENO5, the D+/D- choice is internal to the HJWENO5 derivative calculation based on velocity.
                // Here, the "velocity" is implicitly derived from the sign of the gradient components of phi itself.
                // This differs from HJ advection where velocity is an external field.
                // C++ ISGradientNormSqrd for HJWENO5_BIAS uses upwind/downwind stencils directly.
                // For now, this is a placeholder as D1.HjWeno5InX/Y/Z needs velocity.
                // We would need to compute two sets of gradients for HJWENO5 (one D+, one D-)
                // then pass to Godunov. This is complex.
                // Fallback or throw for now.
                throw new NotImplementedException("ISGradientNormSqrd with HJWENO5_BIAS requires specific upwind/downwind HJWENO gradient calculations not yet fully ported to D1 helpers.");
            }

            // For non-HJWENO schemes, determine D+ and D- based on scheme choice
            // D+ (upwind for positiveSign=false, i.e. valCenter <= 0) uses Forward differences
            // D- (downwind for positiveSign=true, i.e. valCenter > 0) uses Backward differences
            upwindGradient = ISGradient.Result(accessor, ijk, GetFD_Scheme(scheme));
            downwindGradient = ISGradient.Result(accessor, ijk, GetBD_Scheme(scheme));

            return MathUtil.GodunovsNormSqrd<TValue>(positiveSign, downwindGradient, upwindGradient);
        }
    }

    public static class ISLaplacian
    {
        public static TValue Result<TValue, TAccessor>(
            TAccessor accessor,
            Coord ijk,
            DDScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TValue : struct, IFloatingPointIeee754<TValue> // Assuming Laplacian applies to scalar fields
        {
            // Depends on D2.InX, D2.InY, D2.InZ from FiniteDifference.cs
            // Refactored to use D2.Laplacian
            return D2.Laplacian<TValue, TAccessor>(accessor, ijk, scheme);
        }
    }

    public static class ISDivergence
    {
        public static TValue Result<TValue, TVec, TAccessor>(
            TAccessor accessor,
            Coord ijk,
            DScheme scheme)
            where TVec : struct, IReadOnlyVec<TValue> // Added IReadOnlyVec constraint
            where TAccessor : ITreeValueAccessor<TVec>
            where TValue : struct, IFloatingPointIeee754<TValue> // Ensure TValue supports needed ops
        {
            // Refactored to use D1Vec
            TValue dFx_dx = D1Vec.InX<TValue, TVec, TAccessor>(accessor, ijk, scheme, 0);
            TValue dFy_dy = D1Vec.InY<TValue, TVec, TAccessor>(accessor, ijk, scheme, 1);
            TValue dFz_dz = D1Vec.InZ<TValue, TVec, TAccessor>(accessor, ijk, scheme, 2);
            return dFx_dx + dFy_dy + dFz_dz;
        }
    }

    public static class ISCurl
    {
        public static Vec3<TValue> Result<TValue, TVec, TAccessor>(
            TAccessor accessor,
            Coord ijk,
            DScheme scheme)
            where TVec : struct, IReadOnlyVec<TValue> // Added IReadOnlyVec constraint
            where TAccessor : ITreeValueAccessor<TVec>
            where TValue : struct, IFloatingPointIeee754<TValue> // Ensure TValue supports needed ops
        {
            // Implemented using D1Vec
            TValue Cdx = D1Vec.InY<TValue, TVec, TAccessor>(accessor, ijk, scheme, 2) - D1Vec.InZ<TValue, TVec, TAccessor>(accessor, ijk, scheme, 1); // dFz/dy - dFy/dz
            TValue Cdy = D1Vec.InZ<TValue, TVec, TAccessor>(accessor, ijk, scheme, 0) - D1Vec.InX<TValue, TVec, TAccessor>(accessor, ijk, scheme, 2); // dFx/dz - dFz/dx
            TValue Cdz = D1Vec.InX<TValue, TVec, TAccessor>(accessor, ijk, scheme, 1) - D1Vec.InY<TValue, TVec, TAccessor>(accessor, ijk, scheme, 0); // dFy/dx - dFx/dy
            return new Vec3<TValue>(Cdx, Cdy, Cdz);
        }
    }
}
