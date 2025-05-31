// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Numerics;
using OpenVDB.Core.Math.Maps;
using OpenVDB.Core.Tree;

namespace OpenVDB.Core.Math.Operators
{
    public static class WSGradient
    {
        // Helper for generic conversion - consider placing in Vec3 or a utility class if widely needed
        private static Vec3<TOut> ConvertVec3<TIn, TOut>(Vec3<TIn> vIn)
            where TIn : struct, IFloatingPointIeee754<TIn>
            where TOut : struct, IFloatingPointIeee754<TOut>
        {
            return new Vec3<TOut>(
                TOut.CreateChecked(vIn.X),
                TOut.CreateChecked(vIn.Y),
                TOut.CreateChecked(vIn.Z)
            );
        }

        // General Case
        public static Vec3<TValue> Result<TValue, TAccessor, TMap>(
            TMap map,
            TAccessor accessor,
            Coord ijk,
            DScheme scheme)
            where TAccessor : ITreeValueAccessor<TValue>
            where TMap : class, IMap // Ensure TMap is a class implementing IMap
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (accessor == null) throw new ArgumentNullException(nameof(accessor));

            // Specializations
            if (map is TranslationMap tMap)
            {
                return ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme);
            }
            if (map is UniformScaleMap usMap) // Includes UniformScaleTranslateMap via ToAffineMap then IJT path
            {
                if (scheme == DScheme.CD_2ND || scheme == DScheme.CD_4TH || scheme == DScheme.CD_6TH) // Common optimized paths
                {
                    Vec3<TValue> isGrad = ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme);
                    TValue invScale = TValue.One / TValue.CreateChecked(usMap.Scale.X); // Uniform scale
                    return isGrad * invScale;
                }
                // Fall through to general for other schemes or rely on general IJT
            }
            if (map is ScaleMap sMap) // Includes ScaleTranslateMap
            {
                 if (scheme == DScheme.CD_2ND || scheme == DScheme.CD_4TH || scheme == DScheme.CD_6TH)
                {
                    Vec3<TValue> isGrad = ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme);
                    var invScaleVec = new Vec3<TValue>(
                        TValue.One / TValue.CreateChecked(sMap.Scale.X),
                        TValue.One / TValue.CreateChecked(sMap.Scale.Y),
                        TValue.One / TValue.CreateChecked(sMap.Scale.Z)
                    );
                    return isGrad * invScaleVec; // Component-wise product
                }
            }
            if (map is UniformScaleTranslateMap ustMap)
            {
                 if (scheme == DScheme.CD_2ND || scheme == DScheme.CD_4TH || scheme == DScheme.CD_6TH)
                {
                    Vec3<TValue> isGrad = ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme);
                    TValue invScale = TValue.One / TValue.CreateChecked(ustMap.Scale.X);
                    return isGrad * invScale;
                }
            }
             if (map is ScaleTranslateMap stMap)
            {
                 if (scheme == DScheme.CD_2ND || scheme == DScheme.CD_4TH || scheme == DScheme.CD_6TH)
                {
                    Vec3<TValue> isGrad = ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme);
                     var invScaleVec = new Vec3<TValue>(
                        TValue.One / TValue.CreateChecked(stMap.Scale.X),
                        TValue.One / TValue.CreateChecked(stMap.Scale.Y),
                        TValue.One / TValue.CreateChecked(stMap.Scale.Z)
                    );
                    return isGrad * invScaleVec;
                }
            }
            if (map is UnitaryMap uMap) // Rotations, reflections
            {
                // For unitary map, J^-1^T = J. So, WSG = J * ISG
                Vec3<TValue> isGrad = ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme);
                // ApplyJacobian (which is J for unitary)
                return ConvertVec3<double, TValue>(uMap.ApplyJacobian(ConvertVec3<TValue,double>(isGrad)));
            }


            // General case: transform the index-space gradient by the inverse Jacobian transpose.
            // ISGradient.Result gives Vec3<TValue>. ApplyIJT needs Vec3<double>.
            Vec3<double> isGradDouble = ConvertVec3<TValue, double>(ISGradient.Result<TValue, TAccessor>(accessor, ijk, scheme));
            Vec3<double> wsGradDouble;

            // ApplyIJT also needs domainPos for non-linear maps, simplified here.
            // The C++ version passes ijk.asVec3d() as domainPos.
            if (!map.IsLinear)
            {
                // Attempt to use the more general IMap method if available from MapBase
                // This still needs to be fully implemented for NonlinearFrustumMap for example.
                wsGradDouble = map.ApplyIJT(isGradDouble);
                // Placeholder for: map.ApplyIJT(isGradDouble, ijk.ToVec3d());
                // For now, using the simplified ApplyIJT as many maps default to it.
                // This will be inaccurate for non-linear maps where Jacobian varies with position.
                 Console.WriteLine($"Warning: WSGradient for non-linear map {map.TypeName} using simplified ApplyIJT (no domainPos). Results may be inaccurate.");
            }
            else
            {
                 wsGradDouble = map.ApplyIJT(isGradDouble);
            }

            return ConvertVec3<double, TValue>(wsGradDouble);
        }
    }

    // Placeholders for other World-Space operators
    public static class WSGradientBiased { /* ... */ }
    public static class WSGradientNormSqrd { /* ... */ }
    public static class WSDivergence { /* ... */ }
    public static class WSCurl { /* ... */ }
    public static class WSLaplacian { /* ... */ }
    public static class WSCPT { /* ... */ } // Closest Point Transform
    public static class WSMeanCurvature { /* ... */ }
}
