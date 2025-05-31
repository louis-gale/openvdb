// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
// Bring in the new math types
using OpenVDB.Math;

namespace OpenVDB
{
    // Basic type aliases from Types.h
    // C# uses built-in types for most of these.
    // Specific-width integer types are directly available in C#.
    using Index32 = System.UInt32;
    using Index64 = System.UInt64;
    using Index = System.UInt32; // Default Index type
    using Int16 = System.Int16;
    using Int32 = System.Int32;
    using Int64 = System.Int64;
    // using Int = System.Int32; // Default Int type - 'Int' might be ambiguous, prefer Int32
    using Byte = System.Byte;
    using Real = System.Double; // double precision float

    // System.Half is available in .NET 5+
    using Half = System.Half;

    // Concrete vector and matrix types using the new math structs
    public using Vec2R = OpenVDB.Math.Vec2<double>; // Real is double
    public using Vec2I = OpenVDB.Math.Vec2<uint>;   // Index32 is uint
    public using Vec2f = OpenVDB.Math.Vec2<float>;
    public using Vec2H = OpenVDB.Math.Vec2<Half>;
    public using Vec2i = OpenVDB.Math.Vec2<int>;
    public using Vec2s = OpenVDB.Math.Vec2<float>; // C++ Vec2s often alias to Vec2f
    public using Vec2d = OpenVDB.Math.Vec2<double>;

    public using Vec3R = OpenVDB.Math.Vec3<double>; // Real is double
    public using Vec3I = OpenVDB.Math.Vec3<uint>;   // Index32 is uint
    public using Vec3f = OpenVDB.Math.Vec3<float>;
    public using Vec3H = OpenVDB.Math.Vec3<Half>;
    public using Vec3U8 = OpenVDB.Math.Vec3<byte>;
    public using Vec3U16 = OpenVDB.Math.Vec3<ushort>;
    public using Vec3i = OpenVDB.Math.Vec3<int>;
    public using Vec3s = OpenVDB.Math.Vec3<float>; // C++ Vec3s often alias to Vec3f
    public using Vec3d = OpenVDB.Math.Vec3<double>;

    public using Vec4R = OpenVDB.Math.Vec4<double>; // Real is double
    public using Vec4I = OpenVDB.Math.Vec4<uint>;   // Index32 is uint
    public using Vec4f = OpenVDB.Math.Vec4<float>;
    public using Vec4H = OpenVDB.Math.Vec4<Half>;
    public using Vec4i = OpenVDB.Math.Vec4<int>;
    public using Vec4s = OpenVDB.Math.Vec4<float>; // C++ Vec4s often alias to Vec4f
    public using Vec4d = OpenVDB.Math.Vec4<double>;

    public using Mat3R = OpenVDB.Math.Mat3<double>; // Real is double
    public using Mat3s = OpenVDB.Math.Mat3<float>;
    public using Mat3d = OpenVDB.Math.Mat3<double>;

    public using Mat4R = OpenVDB.Math.Mat4<double>; // Real is double
    public using Mat4s = OpenVDB.Math.Mat4<float>;
    public using Mat4d = OpenVDB.Math.Mat4<double>;

    public using QuatR = OpenVDB.Math.Quat<double>; // Real is double
    public using Quats = OpenVDB.Math.Quat<float>;
    public using Quatd = OpenVDB.Math.Quat<double>;

    // Coord is now defined in OpenVDB.Math.Coord.cs
    // Using alias for convenience if needed locally, or fully qualify.
    // public using Coord = OpenVDB.Math.Coord;

    // BBox related type aliases
    // CoordBBox uses BBox<Coord, int>
    // BBoxD uses BBox<Vec3<double>, double>
    public using CoordBBox = OpenVDB.Math.BBox<OpenVDB.Math.Coord, int>;
    public using BBoxD = OpenVDB.Math.BBox<OpenVDB.Math.Vec3<double>, double>;
    // It seems the original BBoxD in Types.cs was a placeholder and didn't fully map to a generic BBox<Vec3d>.
    // The new BBoxD alias above correctly maps to BBox<Vec3<double>, double>.

    /// <summary>
    /// Dummy type for a voxel with a binary mask value, e.g., the active state.
    /// </summary>
    public struct ValueMask { }

    /// <summary>
    /// Integer wrapper, required to distinguish PointIndexGrid and
    /// PointDataGrid from Int32Grid and Int64Grid.
    /// Kind is a dummy parameter used to create distinct types.
    /// </summary>
    public struct PointIndex<TIntType, TKind> where TIntType : struct // TKind could be an enum or int
    {
        private TIntType mIndex;

        public PointIndex(TIntType i) { mIndex = i; }

        public PointIndex(object i)
        {
            mIndex = (TIntType)Convert.ChangeType(i, typeof(TIntType));
        }
        public static implicit operator TIntType(PointIndex<TIntType, TKind> pi) => pi.mIndex;
    }

    public enum PointIndexKind { Kind0 }
    public enum PointDataIndexKind { Kind1 }

    public using PointIndex32 = PointIndex<uint, PointIndexKind>; // uint is Index32
    public using PointIndex64 = PointIndex<ulong, PointIndexKind>; // ulong is Index64
    public using PointDataIndex32 = PointIndex<uint, PointDataIndexKind>;
    public using PointDataIndex64 = PointIndex<ulong, PointDataIndexKind>;


    /// <summary>
    /// Grid classification types.
    /// </summary>
    public enum GridClass
    {
        Unknown = 0,
        LevelSet,
        FogVolume,
        Staggered
    }
    public static class GridClassConstants { public const int NumGridClasses = (int)GridClass.Staggered + 1; }


    public const Real LevelSetHalfWidth = 3.0;

    /// <summary>
    /// The type of a vector determines how transforms are applied to it.
    /// </summary>
    public enum VecType
    {
        Invariant = 0,
        Covariant,
        CovariantNormalize,
        ContravariantRelative,
        ContravariantAbsolute
    }
    public static class VecTypeConstants { public const int NumVecTypes = (int)VecType.ContravariantAbsolute + 1; }


    /// <summary>
    /// Specify how grids should be merged during certain (typically multithreaded) operations.
    /// </summary>
    public enum MergePolicy
    {
        MergeActiveStates = 0,
        MergeNodes,
        MergeActiveStatesAndNodes
    }

    /// <summary>
    /// Provides string names for types, similar to C++ typeNameAsString.
    /// </summary>
    public static class TypeName
    {
        private static readonly Dictionary<Type, string> _typeNames = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(ValueMask), "mask" },
            { typeof(Half), "half" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(sbyte), "int8" },   // int8_t
            { typeof(byte), "uint8" },  // uint8_t
            { typeof(short), "int16" },  // int16_t
            { typeof(ushort), "uint16" },// uint16_t
            { typeof(int), "int32" },    // int32_t
            { typeof(uint), "uint32" },  // uint32_t
            { typeof(long), "int64" },   // int64_t
            { typeof(ulong), "uint64" }, // uint64_t
            { typeof(Vec2i), "vec2i" },
            { typeof(Vec2s), "vec2s" },
            { typeof(Vec2d), "vec2d" },
            { typeof(Vec3U8), "vec3u8"},
            { typeof(Vec3U16), "vec3u16"},
            { typeof(Vec3i), "vec3i" },
            { typeof(Vec3f), "vec3s" }, // C++ Vec3s is float
            { typeof(Vec3d), "vec3d" },
            { typeof(Vec4i), "vec4i" },
            { typeof(Vec4f), "vec4s" }, // C++ Vec4s is float
            { typeof(Vec4d), "vec4d" },
            { typeof(string), "string" },
            { typeof(Mat3s), "mat3s" },
            { typeof(Mat3d), "mat3d" },
            { typeof(Mat4s), "mat4s" },
            { typeof(Mat4d), "mat4d" },
            { typeof(Quats), "quats" },
            { typeof(Quatd), "quatd" },
            { typeof(PointIndex32), "ptidx32" },
            { typeof(PointIndex64), "ptidx64" },
            { typeof(PointDataIndex32), "ptdataidx32" },
            { typeof(PointDataIndex64), "ptdataidx64" }
        };

        public static string GetName<T>() => GetName(typeof(T));

        public static string GetName(Type type)
        {
            if (_typeNames.TryGetValue(type, out string name))
            {
                return name;
            }
            if (type.IsGenericType)
            {
                string genericArgs = string.Join(", ", Array.ConvertAll(type.GetGenericArguments(), GetName));
                var typeName = type.Name;
                var backtick = typeName.IndexOf('`');
                if (backtick > 0) typeName = typeName.Substring(0, backtick);
                return $"{typeName}<{genericArgs}>";
            }
            return type.Name;
        }
    }


    public class CombineArgs<TAValueType, TBValueType>
        where TAValueType : struct
        where TBValueType : struct
    {
        private TAValueType _resultVal;
        public TAValueType AVal { get; private set; }
        public TBValueType BVal { get; private set; }
        private Func<TAValueType> _getResultVal;
        private Action<TAValueType> _setResultVal;

        public TAValueType ResultVal
        {
            get => _getResultVal();
            set => _setResultVal(value);
        }

        public bool AIsActive { get; private set; }
        public bool BIsActive { get; private set; }
        public bool ResultIsActive { get; set; }

        public CombineArgs(TAValueType a, TBValueType b, bool aOn = false, bool bOn = false)
        {
            AVal = a;
            BVal = b;
            _resultVal = default(TAValueType);
            _getResultVal = () => _resultVal;
            _setResultVal = (val) => _resultVal = val;
            AIsActive = aOn;
            BIsActive = bOn;
            UpdateResultActive();
        }

        // This constructor is tricky with ref struct in C# for direct field reference.
        // Using delegates to get/set external value.
        public CombineArgs(TAValueType a, TBValueType b, Func<TAValueType> getResult, Action<TAValueType> setResult, bool aOn = false, bool bOn = false)
        {
            AVal = a;
            BVal = b;
            _getResultVal = getResult;
            _setResultVal = setResult;
            AIsActive = aOn;
            BIsActive = bOn;
            UpdateResultActive();
        }

        public CombineArgs<TAValueType, TBValueType> SetResult(TAValueType val) { ResultVal = val; return this; }
        public CombineArgs<TAValueType, TBValueType> SetAVal(TAValueType a) { AVal = a; return this; }
        public CombineArgs<TAValueType, TBValueType> SetBVal(TBValueType b) { BVal = b; return this; }
        public CombineArgs<TAValueType, TBValueType> SetAIsActive(bool b) { AIsActive = b; UpdateResultActive(); return this; }
        public CombineArgs<TAValueType, TBValueType> SetBIsActive(bool b) { BIsActive = b; UpdateResultActive(); return this; }

        private void UpdateResultActive()
        {
            ResultIsActive = AIsActive || BIsActive;
        }
    }

    public class CombineArgs<TValueType> : CombineArgs<TValueType, TValueType>
        where TValueType : struct
    {
        public CombineArgs(TValueType a, TValueType b, bool aOn = false, bool bOn = false)
            : base(a, b, aOn, bOn) { }

        public CombineArgs(TValueType a, TValueType b, Func<TValueType> getResult, Action<TValueType> setResult, bool aOn = false, bool bOn = false)
            : base(a, b, getResult, setResult, aOn, bOn) { }
    }


    public class SwappedCombineOp<TValueType, TCombineOpDelegate>
        where TValueType : struct
        where TCombineOpDelegate : Delegate
    {
        private readonly TCombineOpDelegate _op;

        public SwappedCombineOp(TCombineOpDelegate op)
        {
            _op = op;
        }

        public void Invoke(CombineArgs<TValueType> args)
        {
            TValueType tempResult = default;
            var swappedArgs = new CombineArgs<TValueType>(
                args.BVal, args.AVal,
                () => tempResult, (val) => tempResult = val, // Use delegates for temp storage
                args.BIsActive, args.AIsActive);

            _op.DynamicInvoke(swappedArgs);

            args.SetResult(tempResult);
            args.ResultIsActive = swappedArgs.ResultIsActive;
        }
    }

    public struct ShallowCopy { }
    public struct TopologyCopy { }
    public struct DeepCopy { }
    public struct Steal { }
    public struct PartialCreate { }

} // namespace OpenVDB
