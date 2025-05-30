// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math
{
    public static class MathUtil
    {
        public const double DefaultEpsilonD = 1.0e-7;
        public const float DefaultEpsilonF = 1.0e-7f;

        public static bool IsExactlyEqual(double a, double b) => a == b;
        public static bool IsExactlyEqual(float a, float b) => a == b;

        public static bool IsApproxEqual(double a, double b, double epsilon = DefaultEpsilonD)
        {
            return System.Math.Abs(a - b) <= epsilon;
        }

        public static bool IsApproxEqual(float a, float b, float epsilon = DefaultEpsilonF)
        {
            return System.Math.Abs(a - b) <= epsilon;
        }
        
        // Relative or absolute approximate equality
        public static bool IsRelOrApproxEqual(double a, double b, double epsilon = DefaultEpsilonD, double relTol = DefaultEpsilonD)
        {
            if (System.Math.Abs(a - b) <= epsilon) return true;
            double maxVal = System.Math.Max(System.Math.Abs(a), System.Math.Abs(b));
            return System.Math.Abs(a - b) <= maxVal * relTol;
        }

        public static bool IsRelOrApproxEqual(float a, float b, float epsilon = DefaultEpsilonF, float relTol = DefaultEpsilonF)
        {
            if (System.Math.Abs(a - b) <= epsilon) return true;
            float maxVal = System.Math.Max(System.Math.Abs(a), System.Math.Abs(b));
            return System.Math.Abs(a - b) <= maxVal * relTol;
        }


        public static bool IsApproxZero(double a, double epsilon = DefaultEpsilonD)
        {
            return System.Math.Abs(a) <= epsilon;
        }

        public static bool IsApproxZero(float a, float epsilon = DefaultEpsilonF)
        {
            return System.Math.Abs(a) <= epsilon;
        }
        
        public static T Abs<T>(T value) where T : IComparable<T>, ISignedNumber<T>
        {
            return T.IsNegative(value) ? -value : value;
        }

        // The C++ 'promote' template is tricky. C# generics don't work the same way for automatic type promotion
        // in arithmetic operations. Typically, you'd cast, or the operations are defined for specific types (float, double).
        // For this port, operators will assume compatible types or rely on C# casting rules.
        // If specific promotion logic is needed (e.g., float + double = double), it's usually handled by
        // providing overloads or by casting operands before the operation.

        // Placeholder for Math.h functions like isUnitary, eulerAngles etc. These will be added as needed.
    }

    // Enum for rotation order, if needed for Quaternions or Matrices later
    public enum RotationOrder
    {
        XYZ, XZY, YXZ, YZX, ZXY, ZYX
        // Add other orders if present in the original C++
    }
    
    public enum Axis
    {
        XAxis,
        YAxis,
        ZAxis
    }

    public static partial class MathUtil // Using partial class if it gets too large
    {
        /// <summary>
        /// Calculates the squared norm of the Godunov flux difference.
        /// This is used in Hamilton-Jacobi equation solvers, particularly with WENO schemes.
        /// It selects components from upwind (gradP) and downwind (gradM) gradients
        /// based on the sign of the value at the interface.
        /// </summary>
        /// <typeparam name="TValue">The floating point type of the gradient components.</typeparam>
        /// <param name="positiveSign">True if the value at the point of interest is positive.</param>
        /// <param name="downwindGrad">The downwind gradient (typically D- or backward biased).</param>
        /// <param name="upwindGrad">The upwind gradient (typically D+ or forward biased).</param>
        /// <returns>The squared Godunov norm.</returns>
        public static TValue GodunovsNormSqrd<TValue>(
            bool positiveSign,
            Vec3<TValue> downwindGrad, // grad_minus
            Vec3<TValue> upwindGrad)   // grad_plus
            where TValue : struct, System.Numerics.IFloatingPointIeee754<TValue>
        {
            TValue sum = TValue.Zero;
            TValue zero = TValue.Zero;

            for (int i = 0; i < 3; ++i) // Iterate over X, Y, Z components
            {
                TValue dPlus = upwindGrad[i];
                TValue dMinus = downwindGrad[i];
                TValue term;

                if (positiveSign) // Value at point > 0
                {
                    // max(0, D-)^2 + min(0, D+)^2
                    TValue maxDMinus = dMinus > zero ? dMinus : zero; // max(0, dMinus)
                    TValue minDPlus = dPlus < zero ? dPlus : zero;   // min(0, dPlus)
                    term = maxDMinus * maxDMinus + minDPlus * minDPlus;
                }
                else // Value at point <= 0
                {
                    // max(0, D+)^2 + min(0, D-)^2
                    TValue maxDPlus = dPlus > zero ? dPlus : zero;   // max(0, dPlus)
                    TValue minDMinus = dMinus < zero ? dMinus : zero; // min(0, dMinus)
                    term = maxDPlus * maxDPlus + minDMinus * minDMinus;
                }
                sum += term;
            }
            return sum;
        }
    }
}
