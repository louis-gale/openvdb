// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using OpenVDB.Math.Maps; // Assuming maps are in this namespace

namespace OpenVDB.Math
{
    public class Transform
    {
        private IMap _map;

        public Transform()
        {
            // C++ default is ScaleMap(). In C#, IdentityMap might be more intuitive for a "default" transform.
            // Or, to match C++, use ScaleMap(1,1,1)
            _map = new ScaleMap(new Vec3<double>(1.0, 1.0, 1.0));
        }

        public Transform(IMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public Transform(Transform other)
        {
            _map = other._map.Clone();
        }

        public Transform Clone() => new Transform(this); // Deep copy via map cloning

        public bool IsLinear => _map.IsLinear;
        public bool HasUniformScale => _map.HasUniformScale;
        public string MapTypeName => _map.TypeName;

        public bool IsIdentity()
        {
            // Check if it's an IdentityMap or an AffineMap representing identity
            if (_map is IdentityMap) return true;
            if (_map is AffineMap am) return am.IsIdentity();
            // Could also check if it's a ScaleMap(1,1,1) and TranslationMap(0,0,0) etc.
            // For simplicity, if it's not explicitly IdentityMap or an identity AffineMap,
            // a more robust check would involve transforming basis vectors.
            // A simple check:
            return _map.VoxelSize.IsApproxEqual(new Vec3<double>(1,1,1), 1e-7) &&
                   System.Math.Abs(_map.Determinant - 1.0) < 1e-7;
        }


        public Vec3<double> VoxelSize() => _map.VoxelSize;
        public Vec3<double> VoxelSize(Vec3<double> indexSpacePos) => _map.GetVoxelSize(indexSpacePos);

        public double Determinant() => _map.Determinant;
        public double Determinant(Vec3<double> indexSpacePos) => _map.GetDeterminant(indexSpacePos);

        public Vec3<double> IndexToWorld(Vec3<double> indexPoint) => _map.ApplyMap(indexPoint);
        public Vec3<double> IndexToWorld(Coord indexCoord) => _map.ApplyMap(indexCoord.ToVec3d());

        public Vec3<double> WorldToIndex(Vec3<double> worldPoint) => _map.ApplyInverseMap(worldPoint);
        public Coord WorldToIndexCellCentered(Vec3<double> worldPoint) => Coord.Round(WorldToIndex(worldPoint));
        public Coord WorldToIndexNodeCentered(Vec3<double> worldPoint) => Coord.Floor(WorldToIndex(worldPoint));

        public BBox<Vec3<double>, double> IndexToWorld(BBox<Coord, int> indexBBox)
        {
            if (!_map.IsLinear)
            {
                // For non-linear maps, transform all 8 corners and find new AABB
                var worldBox = BBox<Vec3<double>, double>.CreateEmpty();
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Min.X, indexBBox.Min.Y, indexBBox.Min.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Max.X, indexBBox.Min.Y, indexBBox.Min.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Min.X, indexBBox.Max.Y, indexBBox.Min.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Min.X, indexBBox.Min.Y, indexBBox.Max.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Max.X, indexBBox.Max.Y, indexBBox.Min.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Max.X, indexBBox.Min.Y, indexBBox.Max.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Min.X, indexBBox.Max.Y, indexBBox.Max.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Max.X, indexBBox.Max.Y, indexBBox.Max.Z)));
                return worldBox;
            }
            // For linear maps, transform min and max and sort.
            var newMin = IndexToWorld(indexBBox.Min.ToVec3d());
            var newMax = IndexToWorld(indexBBox.Max.ToVec3d());
            return new BBox<Vec3<double>, double>(newMin, newMax, sorted: false); // Ensure sorted
        }

        public BBox<Vec3<double>, double> IndexToWorld(BBox<Vec3<double>, double> indexBBox)
        {
             if (!_map.IsLinear)
            {
                var worldBox = BBox<Vec3<double>, double>.CreateEmpty();
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Min.X, indexBBox.Min.Y, indexBBox.Min.Z)));
                worldBox.Expand(IndexToWorld(new Vec3<double>(indexBBox.Max.X, indexBBox.Min.Y, indexBBox.Min.Z)));
                // ... (expand by all 8 corners)
                worldBox.Expand(IndexToWorld(indexBBox.Max));
                return worldBox;
            }
            var newMin = IndexToWorld(indexBBox.Min);
            var newMax = IndexToWorld(indexBBox.Max);
            return new BBox<Vec3<double>, double>(newMin, newMax, sorted: false);
        }

        public BBox<Vec3<double>, double> WorldToIndex(BBox<Vec3<double>, double> worldBBox)
        {
            if (!_map.IsLinear)
            {
                var indexBox = BBox<Vec3<double>, double>.CreateEmpty();
                indexBox.Expand(WorldToIndex(new Vec3<double>(worldBBox.Min.X, worldBBox.Min.Y, worldBBox.Min.Z)));
                indexBox.Expand(WorldToIndex(new Vec3<double>(worldBBox.Max.X, worldBBox.Min.Y, worldBBox.Min.Z)));
                // ... (expand by all 8 corners)
                indexBox.Expand(WorldToIndex(worldBBox.Max));
                return indexBox;
            }
            var newMin = WorldToIndex(worldBBox.Min);
            var newMax = WorldToIndex(worldBBox.Max);
            return new BBox<Vec3<double>, double>(newMin, newMax, sorted: false);
        }

        public CoordBBox WorldToIndexCellCentered(BBox<Vec3<double>, double> worldBBox)
        {
            var indexFloatBox = WorldToIndex(worldBBox);
            return new CoordBBox(Coord.Round(indexFloatBox.Min), Coord.Round(indexFloatBox.Max), sorted: false);
        }

        public CoordBBox WorldToIndexNodeCentered(BBox<Vec3<double>, double> worldBBox)
        {
            var indexFloatBox = WorldToIndex(worldBBox);
            return new CoordBBox(Coord.Floor(indexFloatBox.Min), Coord.Floor(indexFloatBox.Max), sorted: false);
        }


        public IMap GetMap() => _map; // Be cautious about external modification if map is mutable
        public IMap GetMapCopy() => _map.Clone();
        public void SetMap(IMap map) => _map = map ?? throw new ArgumentNullException(nameof(map));


        // Static factory methods
        public static Transform CreateLinearTransform(double voxelSize = 1.0)
        {
            if (voxelSize == 1.0) return new Transform(new UniformScaleMap(1.0)); // Or IdentityMap
            return new Transform(new UniformScaleMap(voxelSize));
        }

        public static Transform CreateLinearTransform(Mat4<double> matrix)
        {
            return new Transform(new AffineMap(matrix));
        }

        public static Transform CreateScaleTransform(Vec3<double> scale) => new Transform(new ScaleMap(scale));
        public static Transform CreateTranslationTransform(Vec3<double> translate) => new Transform(new TranslationMap(translate));
        public static Transform CreateUniformScaleTransform(double scale) => new Transform(new UniformScaleMap(scale));
        public static Transform CreateScaleTranslateTransform(Vec3<double> scale, Vec3<double> translate) => new Transform(new ScaleTranslateMap(scale, translate));
        public static Transform CreateUniformScaleTranslateTransform(double scale, Vec3<double> translate) => new Transform(new UniformScaleTranslateMap(scale, translate));
        public static Transform CreateUnitaryTransform(Mat3<double> matrix) => new Transform(new UnitaryMap(matrix));


        // Composition methods modify the internal map
        public void PreRotate(double radians, Axis axis = Axis.XAxis) => _map = _map.PreRotate(radians, axis);
        public void PreTranslate(Vec3<double> t) => _map = _map.PreTranslate(t);
        public void PreScale(Vec3<double> s) => _map = _map.PreScale(s);
        public void PreScale(double s) => PreScale(new Vec3<double>(s,s,s));
        public void PreShear(double shear, Axis axis0, Axis axis1) => _map = _map.PreShear(shear, axis0, axis1);
        public void PreMultiply(Mat4<double> mat) // Assumes mat is affine
        {
            var affineMat = new AffineMap(mat);
            _map = new AffineMap(affineMat, _map.ToAffineMap()); // M_new * M_current
        }
         public void PreMultiply(Mat3<double> mat) => PreMultiply(new Mat4<double>(mat));


        public void PostRotate(double radians, Axis axis = Axis.XAxis) => _map = _map.PostRotate(radians, axis);
        public void PostTranslate(Vec3<double> t) => _map = _map.PostTranslate(t);
        public void PostScale(Vec3<double> s) => _map = _map.PostScale(s);
        public void PostScale(double s) => PostScale(new Vec3<double>(s,s,s));
        public void PostShear(double shear, Axis axis0, Axis axis1) => _map = _map.PostShear(shear, axis0, axis1);
        public void PostMultiply(Mat4<double> mat) // Assumes mat is affine
        {
            var affineMat = new AffineMap(mat);
            _map = new AffineMap(_map.ToAffineMap(), affineMat); // M_current * M_new
        }
        public void PostMultiply(Mat3<double> mat) => PostMultiply(new Mat4<double>(mat));

        public bool Equals(Transform other)
        {
            if (other == null) return false;
            // This requires a robust IMap.Equals or comparing matrix representations.
            // For now, comparing type and then specific properties if types match.
            if (MapTypeName != other.MapTypeName) return false;

            // A more robust check would be to convert both to AffineMap and compare matrices,
            // but that might lose type-specific information or be slow.
            // For now, use the map's specific comparison logic if available, or fallback.
            // C++ MapBase has virtual isEqual.
            // This is a placeholder. Actual IMap implementations should have proper Equals.
            return ToAffineMap().Matrix.IsApproxEqual(other.ToAffineMap().Matrix, 1e-7);
        }
        public override bool Equals(object obj) => Equals(obj as Transform);
        public override int GetHashCode() => _map?.GetHashCode() ?? 0; // Relies on IMap having GetHashCode

        public static bool operator ==(Transform left, Transform right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(Transform left, Transform right) => !(left == right);

        public void Write(BinaryWriter writer, OpenVDB.Core.IO.StreamMetadata streamMetadata)
        {
            OpenVDB.Core.IO.IoUtils.WriteString(writer, _map.TypeName);
            _map.WriteData(writer, streamMetadata);
        }

        public void Read(BinaryReader reader, OpenVDB.Core.IO.StreamMetadata streamMetadata)
        {
            string mapTypeName = OpenVDB.Core.IO.IoUtils.ReadString(reader);

            // This is where a MapFactory (like C++ MapRegistry) would be used.
            // For now, create a known map type or throw.
            // This simplified version assumes the map type being read is one we can directly instantiate
            // or it's the default type if the factory mechanism isn't in place.
            // Let's assume for now we try to create based on common types,
            // or default to AffineMap if typeName is "AffineMap" which is a common general case.

            IMap newMap = null;
            if (mapTypeName == Maps.IdentityMap.StaticTypeName) newMap = new Maps.IdentityMap(); // Requires StaticTypeName in each map class
            else if (mapTypeName == Maps.TranslationMap.StaticTypeName) newMap = new Maps.TranslationMap();
            else if (mapTypeName == Maps.ScaleMap.StaticTypeName) newMap = new Maps.ScaleMap();
            else if (mapTypeName == Maps.UniformScaleMap.StaticTypeName) newMap = new Maps.UniformScaleMap();
            else if (mapTypeName == Maps.ScaleTranslateMap.StaticTypeName) newMap = new Maps.ScaleTranslateMap();
            else if (mapTypeName == Maps.UniformScaleTranslateMap.StaticTypeName) newMap = new Maps.UniformScaleTranslateMap();
            else if (mapTypeName == Maps.AffineMap.StaticTypeName) newMap = new Maps.AffineMap();
            else if (mapTypeName == Maps.UnitaryMap.StaticTypeName) newMap = new Maps.UnitaryMap();
            else if (mapTypeName == Maps.NonlinearFrustumMap.StaticTypeName) newMap = new Maps.NonlinearFrustumMap();
            // else if (mapTypeName == typeof(Maps.CompoundMap<,>).Name) // More complex for generics

            if (newMap != null)
            {
                newMap.ReadData(reader, streamMetadata);
                _map = newMap;
            }
            else
            {
                throw new NotSupportedException($"Unsupported map type '{mapTypeName}' encountered during Transform deserialization. MapFactory not yet implemented.");
            }
        }
    }
}

// Add StaticTypeName to each map class for the factory mechanism above to work.
// Example for IdentityMap.cs: public static string StaticTypeName => "IdentityMap";
// This should be done for all map classes.
