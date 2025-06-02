// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System.Numerics;

namespace OpenVDB.Math
{
    [Serializable]
    public struct BBox<TPoint, TElement> : IEquatable<BBox<TPoint, TElement>>
        where TPoint : struct // Further constraints might be needed or helper interfaces
        where TElement : struct, INumber<TElement>, IMinMaxValue<TElement>
    {
        public TPoint Min { get; set; }
        public TPoint Max { get; set; }

        // Helper to manage point operations generically.
        // This is a simplified approach. A more robust solution might use interfaces
        // implemented by Vec3<T> and Coord, or specific BBoxCoord, BBoxVec3d structs.
        private static class PointOps
        {
            public static bool IsIntegral() => typeof(TElement) == typeof(int) || typeof(TElement) == typeof(long) || typeof(TElement) == typeof(uint) || typeof(TElement) == typeof(ushort) || typeof(TElement) == typeof(byte) || typeof(TElement) == typeof(sbyte);

            public static TPoint Create(TElement x, TElement y, TElement z)
            {
                if (typeof(TPoint) == typeof(Vec3<TElement>))
                    return (TPoint)(object)new Vec3<TElement>(x, y, z);
                if (typeof(TPoint) == typeof(Coord) && typeof(TElement) == typeof(int))
                    return (TPoint)(object)new Coord((int)(object)x, (int)(object)y, (int)(object)z);

                throw new NotSupportedException($"TPoint type {typeof(TPoint)} with TElement {typeof(TElement)} not supported for Create.");
            }

            public static TPoint Create(TElement val)
            {
                if (typeof(TPoint) == typeof(Vec3<TElement>))
                    return (TPoint)(object)new Vec3<TElement>(val);
                if (typeof(TPoint) == typeof(Coord) && typeof(TElement) == typeof(int))
                    return (TPoint)(object)new Coord((int)(object)val);

                throw new NotSupportedException($"TPoint type {typeof(TPoint)} not supported for single value Create.");
            }

            public static TElement GetX(TPoint p)
            {
                if (p is Vec3<TElement> v)
                    return v.X;
                if (p is Coord c && typeof(TElement) == typeof(int))
                    return (TElement)(object)c.X;

                throw new NotSupportedException("GetX not supported for TPoint type.");
            }

            public static TElement GetY(TPoint p)
            {
                if (p is Vec3<TElement> v)
                    return v.Y;
                if (p is Coord c && typeof(TElement) == typeof(int))
                    return (TElement)(object)c.Y;

                throw new NotSupportedException("GetY not supported for TPoint type.");
            }

            public static TElement GetZ(TPoint p)
            {
                if (p is Vec3<TElement> v)
                    return v.Z;
                if (p is Coord c && typeof(TElement) == typeof(int))
                    return (TElement)(object)c.Z;

                throw new NotSupportedException("GetZ not supported for TPoint type.");
            }

            public static TPoint Add(TPoint p, TPoint q)
            {
                if (p is Vec3<TElement> pv && q is Vec3<TElement> qv)
                    return (TPoint)(object)(pv + qv);
                if (p is Coord pc && q is Coord qc && typeof(TElement) == typeof(int))
                    return (TPoint)(object)(pc + qc);

                throw new NotSupportedException("Add not supported.");
            }

            public static TPoint Add(TPoint p, TElement val)
            {
                if (p is Vec3<TElement> pv)
                    return (TPoint)(object)(pv + val);
                if (p is Coord pc && typeof(TElement) == typeof(int))
                    return (TPoint)(object)(pc.OffsetBy((int)(object)val)); // Coord + int

                throw new NotSupportedException("Add scalar not supported.");
            }

            public static TPoint Subtract(TPoint p, TPoint q)
            {
                if (p is Vec3<TElement> pv && q is Vec3<TElement> qv)
                    return (TPoint)(object)(pv - qv);
                if (p is Coord pc && q is Coord qc && typeof(TElement) == typeof(int))
                    return (TPoint)(object)(pc - qc);

                throw new NotSupportedException("Subtract not supported.");
            }

            public static TPoint MinComponents(TPoint p1, TPoint p2)
            {
                if (typeof(TPoint) == typeof(Vec3<TElement>))
                {
                    var v1 = (Vec3<TElement>)(object)p1;
                    var v2 = (Vec3<TElement>)(object)p2;

                    return (TPoint)(object)new Vec3<TElement>(TElement.Min(v1.X, v2.X), TElement.Min(v1.Y, v2.Y), TElement.Min(v1.Z, v2.Z));
                }

                if (typeof(TPoint) == typeof(Coord) && typeof(TElement) == typeof(int))
                {
                    var c1 = (Coord)(object)p1;
                    var c2 = (Coord)(object)p2;

                    return (TPoint)(object)Coord.MinComponent(c1, c2);
                }

                throw new NotSupportedException("MinComponents not supported.");
            }

            public static TPoint MaxComponents(TPoint p1, TPoint p2)
            {
                if (typeof(TPoint) == typeof(Vec3<TElement>))
                {
                    var v1 = (Vec3<TElement>)(object)p1;
                    var v2 = (Vec3<TElement>)(object)p2;

                    return (TPoint)(object)new Vec3<TElement>(TElement.Max(v1.X, v2.X), TElement.Max(v1.Y, v2.Y), TElement.Max(v1.Z, v2.Z));
                }

                if (typeof(TPoint) == typeof(Coord) && typeof(TElement) == typeof(int))
                {
                    var c1 = (Coord)(object)p1;
                    var c2 = (Coord)(object)p2;

                    return (TPoint)(object)Coord.MaxComponent(c1, c2);
                }

                throw new NotSupportedException("MaxComponents not supported.");
            }

            public static bool IsLessThanOrEqual(TPoint p1, TPoint p2) // Component-wise <=
            {
                if (p1 is Vec3<TElement> v1 && p2 is Vec3<TElement> v2)
                    return v1.X <= v2.X && v1.Y <= v2.Y && v1.Z <= v2.Z;
                if (p1 is Coord c1 && p2 is Coord c2 && typeof(TElement) == typeof(int))
                    return c1.X <= c2.X && c1.Y <= c2.Y && c1.Z <= c2.Z; // Lexicographic might be an alternative for Coords

                throw new NotSupportedException("IsLessThanOrEqual not supported.");
            }

            public static bool IsGreaterThanOrEqual(TPoint p1, TPoint p2) // Component-wise >=
            {
                if (p1 is Vec3<TElement> v1 && p2 is Vec3<TElement> v2)
                    return v1.X >= v2.X && v1.Y >= v2.Y && v1.Z >= v2.Z;
                if (p1 is Coord c1 && p2 is Coord c2 && typeof(TElement) == typeof(int))
                    return c1.X >= c2.X && c1.Y >= c2.Y && c1.Z >= c2.Z;

                throw new NotSupportedException("IsGreaterThanOrEqual not supported.");
            }
        }

        public BBox(TPoint min, TPoint max)
        {
            Min = min;
            Max = max;
        }

        public BBox(TPoint min, TPoint max, bool sorted)
        {
            Min = min;
            Max = max;

            if (!sorted)
            {
                this.Sort();
            }
        }

        public BBox(TPoint min, TElement length)
        {
            Min = min;
            TElement sizeOffset = PointOps.IsIntegral() ? length - TElement.One : length;
            Max = PointOps.Add(min, sizeOffset);
        }

        public BBox(TElement[] xyz, bool sorted = true)
        {
            if (xyz == null || xyz.Length < 6)
                throw new ArgumentException("Array must contain at least 6 elements for min and max points.");

            var p1 = PointOps.Create(xyz[0], xyz[1], xyz[2]);
            var p2 = PointOps.Create(xyz[3], xyz[4], xyz[5]);

            if (sorted)
            {
                Min = p1;
                Max = p2;
            }
            else
            {
                Min = PointOps.MinComponents(p1, p2);
                Max = PointOps.MaxComponents(p1, p2);
            }
        }

        public static BBox<TPoint, TElement> CreateEmpty()
        {
            var minVal = PointOps.Create(TElement.MaxValue);
            var maxVal = PointOps.Create(TElement.MinValue);

            if (typeof(TElement) == typeof(float))
            {
                minVal = (TPoint)(object)new Vec3<float>(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                maxVal = (TPoint)(object)new Vec3<float>(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            }
            else if (typeof(TElement) == typeof(double))
            {
                minVal = (TPoint)(object)new Vec3<double>(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
                maxVal = (TPoint)(object)new Vec3<double>(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
            }
            else if (typeof(TPoint) == typeof(Coord))
            {
                // For Coord (int elements)
                minVal = (TPoint)(object)Coord.MaxValue;
                maxVal = (TPoint)(object)Coord.MinValue;
            }

            return new BBox<TPoint, TElement>(minVal, maxVal);
        }


        public void Sort()
        {
            var currentMin = Min;
            var currentMax = Max;
            Min = PointOps.MinComponents(currentMin, currentMax);
            Max = PointOps.MaxComponents(currentMin, currentMax);
        }

        public bool IsEmpty
        {
            get
            {
                if (PointOps.IsIntegral())
                {
                    return PointOps.GetX(Min) > PointOps.GetX(Max) ||
                           PointOps.GetY(Min) > PointOps.GetY(Max) ||
                           PointOps.GetZ(Min) > PointOps.GetZ(Max);
                }

                return PointOps.GetX(Min) >= PointOps.GetX(Max) ||
                       PointOps.GetY(Min) >= PointOps.GetY(Max) ||
                       PointOps.GetZ(Min) >= PointOps.GetZ(Max);
            }
        }

        public bool HasVolume => !IsEmpty;

        public bool IsSorted // min <= max component-wise
        {
            get
            {
                // For floating point, C++ uses tolerance.
                // This simple version doesn't use tolerance for float comparisons here.
                return PointOps.IsLessThanOrEqual(Min, Max);
            }
        }

        public Vec3<double> GetCenter()
        {
            var minD = new Vec3<double>(Convert.ToDouble(PointOps.GetX(Min)), Convert.ToDouble(PointOps.GetY(Min)), Convert.ToDouble(PointOps.GetZ(Min)));
            var maxD = new Vec3<double>(Convert.ToDouble(PointOps.GetX(Max)), Convert.ToDouble(PointOps.GetY(Max)), Convert.ToDouble(PointOps.GetZ(Max)));

            return (minD + maxD) * 0.5;
        }

        public TPoint Extents()
        {
            if (IsEmpty)
                return PointOps.Create(TElement.Zero);

            var diff = PointOps.Subtract(Max, Min);

            return PointOps.IsIntegral() ? PointOps.Add(diff, TElement.One) : diff;
        }

        public TElement Volume()
        {
            if (IsEmpty)
                return TElement.Zero;

            var ext = Extents();

            return PointOps.GetX(ext) * PointOps.GetY(ext) * PointOps.GetZ(ext);
        }

        public bool IsInside(TPoint p)
        {
            // C++ version uses tolerance for floating point.
            return PointOps.IsGreaterThanOrEqual(p, Min) && PointOps.IsLessThanOrEqual(p, Max);
        }

        public bool IsInside(BBox<TPoint, TElement> other)
        {
            return PointOps.IsGreaterThanOrEqual(other.Min, Min) && PointOps.IsLessThanOrEqual(other.Max, Max);
        }

        public bool HasOverlap(BBox<TPoint, TElement> other)
        {
            if (IsEmpty || other.IsEmpty)
                return false;

            return PointOps.IsLessThanOrEqual(Min, other.Max) && PointOps.IsGreaterThanOrEqual(Max, other.Min);
        }

        public void Expand(TElement padding)
        {
            if (typeof(TPoint) == typeof(Coord))
            {
                var cMin = (Coord)(object)Min;
                var cMax = (Coord)(object)Max;
                var padInt = (int)(object)TElement.Abs(padding);
                cMin.Offset(-padInt);
                cMax.Offset(padInt);
                Min = (TPoint)(object)cMin;
                Max = (TPoint)(object)cMax;
            }
            else if (typeof(TPoint) == typeof(Vec3<TElement>))
            {
                var pad = TElement.Abs(padding);
                var vMin = (Vec3<TElement>)(object)Min;
                var vMax = (Vec3<TElement>)(object)Max;
                Min = (TPoint)(object)(vMin - pad);
                Max = (TPoint)(object)(vMax + pad);
            }
            else
            {
                throw new NotSupportedException("Expand by padding not supported for TPoint.");
            }
        }

        public void Expand(TPoint p)
        {
            Min = PointOps.MinComponents(Min, p);
            Max = PointOps.MaxComponents(Max, p);
        }

        public void Expand(BBox<TPoint, TElement> other)
        {
            if (!other.IsEmpty) // Only expand if the other box is not empty
            {
                if (IsEmpty) // If this box is empty, just become the other box
                {
                    Min = other.Min;
                    Max = other.Max;
                }
                else
                {
                    Min = PointOps.MinComponents(Min, other.Min);
                    Max = PointOps.MaxComponents(Max, other.Max);
                }
            }
        }

        public void Translate(TPoint t)
        {
            Min = PointOps.Add(Min, t);
            Max = PointOps.Add(Max, t);
        }

        public bool Equals(BBox<TPoint, TElement> other)
        {
            // C++ uses isApproxEqual for float types.
            // For simplicity, using direct equality from TPoint.
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        public override bool Equals(object obj) => obj is BBox<TPoint, TElement> other && Equals(other);
        public static bool operator ==(BBox<TPoint, TElement> b1, BBox<TPoint, TElement> b2) => b1.Equals(b2);
        public static bool operator !=(BBox<TPoint, TElement> b1, BBox<TPoint, TElement> b2) => !b1.Equals(b2);
        public override int GetHashCode() => HashCode.Combine(Min, Max);
        public override string ToString() => $"{Min} -> {Max}";
    }
}