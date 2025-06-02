// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Numerics; // For IFloatingPointIeee754

namespace OpenVDB.Math
{
    using RayF = Ray<float>;
    using RayD = Ray<double>;

    [Serializable]
    public struct Ray<T> where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public struct TimeSpan : IEquatable<TimeSpan>
        {
            public T T0 { get; set; }
            public T T1 { get; set; }

            public TimeSpan(T t0, T t1)
            {
                T0 = t0;
                T1 = t1;
            }

            public void Set(T t0, T t1)
            {
                T0 = t0;
                T1 = t1;
            }

            public void Get(out T t0, out T t1)
            {
                t0 = T0;
                t1 = T1;
            }

            public bool IsValid(T epsilon = default)
            {
                if (epsilon == default) epsilon = T.Epsilon;
                return (T1 - T0) > epsilon;
            }

            public T Mid() => (T0 + T1) * T.CreateChecked(0.5);

            public void Scale(T s)
            {
                if (s <= T.Zero) throw new ArgumentOutOfRangeException(nameof(s), "Scale factor must be positive.");
                T0 *= s;
                T1 *= s;
            }

            public bool Test(T t) => (t >= T0 && t <= T1);

            public bool Equals(TimeSpan other) => T0.Equals(other.T0) && T1.Equals(other.T1);
            public override bool Equals(object obj) => obj is TimeSpan other && Equals(other);
            public static bool operator ==(TimeSpan left, TimeSpan right) => left.Equals(right);
            public static bool operator !=(TimeSpan left, TimeSpan right) => !left.Equals(right);
            public override int GetHashCode() => HashCode.Combine(T0, T1);
            public override string ToString() => $"[{T0}, {T1}]";
        }

        public Vec3<T> Eye { get; private set; }
        public Vec3<T> Dir { get; private set; } // Should ideally be unit vector for some operations
        public Vec3<T> InvDir { get; private set; }
        public TimeSpan Time { get; set; }

        public T MinTime => Time.T0;
        public T MaxTime => Time.T1;

        public Ray(Vec3<T> eye, Vec3<T> direction, T t0 = default, T t1 = default)
        {
            Eye = eye;
            Dir = direction; // User is responsible for normalization if needed
            // Handle potential division by zero for InvDir carefully
            InvDir = new Vec3<T>(
                T.IsZero(direction.X) ? T.PositiveInfinity : T.One / direction.X,
                T.IsZero(direction.Y) ? T.PositiveInfinity : T.One / direction.Y,
                T.IsZero(direction.Z) ? T.PositiveInfinity : T.One / direction.Z
            );

            if (t0 == default) t0 = T.Epsilon; // Smallest positive value
            if (t1 == default) t1 = T.MaxValue; // Effectively infinity

            if (t0 < T.Zero || t1 < T.Zero) throw new ArgumentOutOfRangeException("Ray times t0 and t1 must be non-negative.");
            Time = new TimeSpan(t0, t1);
        }

        public void SetEye(Vec3<T> eye) => Eye = eye;

        public void SetDir(Vec3<T> dir)
        {
            Dir = dir;
            InvDir = new Vec3<T>(
                T.IsZero(dir.X) ? T.PositiveInfinity : T.One / dir.X,
                T.IsZero(dir.Y) ? T.PositiveInfinity : T.One / dir.Y,
                T.IsZero(dir.Z) ? T.PositiveInfinity : T.One / dir.Z
            );
        }

        public void NormalizeDir()
        {
            T len = Dir.Length();
            if (len > T.Epsilon)
            {
                SetDir(Dir / len);
            }
        }


        public void SetMinTime(T t0)
        {
            if (t0 < T.Zero) throw new ArgumentOutOfRangeException(nameof(t0), "MinTime must be non-negative.");
            var tempTime = Time;
            tempTime.T0 = t0;
            Time = tempTime;
        }

        public void SetMaxTime(T t1)
        {
             if (t1 < T.Zero) throw new ArgumentOutOfRangeException(nameof(t1), "MaxTime must be non-negative.");
            var tempTime = Time;
            tempTime.T1 = t1;
            Time = tempTime;
        }

        public void SetTimes(T t0 = default, T t1 = default)
        {
            if (t0 == default) t0 = T.Epsilon;
            if (t1 == default) t1 = T.MaxValue;
            if (t0 < T.Zero || t1 < T.Zero) throw new ArgumentOutOfRangeException("Ray times t0 and t1 must be non-negative.");
            Time = new TimeSpan(t0, t1);
        }

        public void ScaleTimes(T scale)
        {
            var tempTime = Time;
            tempTime.Scale(scale);
            Time = tempTime;
        }

        public Vec3<T> GetPoint(T time) => Eye + Dir * time;
        public Vec3<T> Start => GetPoint(MinTime);
        public Vec3<T> End => GetPoint(MaxTime);
        public Vec3<T> Mid => GetPoint(Time.Mid());

        public bool IsValidTimeSpan(T epsilon = default) => Time.IsValid(epsilon);
        public bool TestTime(T time) => Time.Test(time);

        public bool Intersects(BBox<Vec3<T>, T> bbox, out T tMin, out T tMax)
        {
            tMin = MinTime;
            tMax = MaxTime;

            for (int i = 0; i < 3; ++i)
            {
                T dimMin = bbox.Min[i];
                T dimMax = bbox.Max[i];
                T eyeDim = Eye[i];
                T invDirDim = InvDir[i];

                T a = (dimMin - eyeDim) * invDirDim;
                T b = (dimMax - eyeDim) * invDirDim;

                if (a > b) (a, b) = (b, a); // Swap

                if (a > tMin) tMin = a;
                if (b < tMax) tMax = b;

                if (tMin > tMax) return false;
            }
            return true;
        }

        public bool Intersects(BBox<Vec3<T>, T> bbox)
        {
            return Intersects(bbox, out _, out _);
        }

        public bool Clip(BBox<Vec3<T>, T> bbox)
        {
            if (Intersects(bbox, out T t0, out T t1))
            {
                Time = new TimeSpan(t0, t1);
                return true;
            }
            return false;
        }

        public bool Intersects(Vec3<T> sphereCenter, T sphereRadius, out T t0, out T t1)
        {
            Vec3<T> origin = Eye - sphereCenter;
            T a = Dir.LengthSqr();
            T b = T.CreateChecked(2.0) * Dir.Dot(origin);
            T c = origin.LengthSqr() - sphereRadius * sphereRadius;
            T discriminant = b * b - T.CreateChecked(4.0) * a * c;

            t0 = MinTime; // Initialize to ray's current min/max
            t1 = MaxTime;

            if (discriminant < T.Zero) return false;

            T sqrtD = T.Sqrt(discriminant);
            // In C++, Q = -0.5 * (B + (B < 0 ? -sqrtD : sqrtD))
            // which is same as -0.5 * (B + sgn(B)*sqrtD) if B!=0
            // Simplified: t = (-B +/- sqrtD) / 2A
            T q;
            if (b < T.Zero)
                q = (sqrtD - b) * T.CreateChecked(0.5);
            else
                q = (-sqrtD - b) * T.CreateChecked(0.5);


            T r0 = q / a;
            T r1 = c / q;

            if (r0 > r1) (r0, r1) = (r1, r0); // Swap

            // Intersect with ray's valid time span
            if (r0 > t0) t0 = r0;
            if (r1 < t1) t1 = r1;

            return t0 <= t1;
        }

        public bool Intersects(Vec3<T> sphereCenter, T sphereRadius)
        {
            return Intersects(sphereCenter, sphereRadius, out _, out _);
        }

        public bool Clip(Vec3<T> sphereCenter, T sphereRadius)
        {
            if(Intersects(sphereCenter, sphereRadius, out T t0, out T t1))
            {
                Time = new TimeSpan(t0, t1);
                return true;
            }
            return false;
        }

        public bool Intersects(Vec3<T> planeNormal, T planeDistance, out T time)
        {
            time = T.Zero;
            T cosAngle = Dir.Dot(planeNormal);
            if (T.Abs(cosAngle) < T.Epsilon) return false; // Parallel

            time = (planeDistance - Eye.Dot(planeNormal)) / cosAngle;
            return TestTime(time);
        }

        public bool Intersects(Vec3<T> planeNormal, Vec3<T> pointOnPlane, out T time)
        {
            return Intersects(planeNormal, pointOnPlane.Dot(planeNormal), out time);
        }

        public override string ToString() => $"Eye={Eye}, Dir={Dir}, Time=[{MinTime}, {MaxTime}]";
    }
}
