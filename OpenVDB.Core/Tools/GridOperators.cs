// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenVDB.Core.Tree;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Maps;
using OpenVDB.Core.Math.Operators; // For WSGradient etc.

namespace OpenVDB.Core.Tools
{
    // Placeholder for interrupt mechanism
    public interface IGridOperatorInterrupt
    {
        bool WasInterrupted(); // C++ uses operator()
        void CheckInterrupted(); // Helper to throw if interrupted
    }

    // Helper class, similar to C++ gridop::GridOperator
    // TInputGrid and TOutputGrid are concrete grid types
    // TOperator is a struct/static class with a static Result method
    // TInValue is the value type of the input grid
    // TOutValue is the value type of the output grid
    // TMap is the type of the map from the input grid's transform
    // TInAccessor is ITreeValueAccessor<TInValue>
    // TOutAccessor is ITreeValueAccessor<TOutValue>
    public class GridOperator<TInputGrid, TOutputGrid, TOperator, TInValue, TOutValue, TMap, TInAccessor, TOutAccessor>
        where TInputGrid : GridBase // Grid<TInTree, TInValue>
        where TOutputGrid : GridBase // Grid<TOutTree, TOutValue>
        where TMap : class, IMap
        where TInAccessor : class, ITreeValueAccessor<TInValue>
        where TOutAccessor : class, ITreeValueAccessor<TOutValue>
        where TInValue : struct
        where TOutValue : struct
    {
        protected readonly TInputGrid _inputGrid;
        protected readonly TOutputGrid _outputGrid; // Assumed to be initialized with desired background
        protected readonly TMap _map; // Map from input grid's transform
        protected readonly TInAccessor _inputAccessor;
        protected readonly TOutAccessor _outputAccessor;
        protected readonly BoolGrid _maskGrid; // Optional mask
        protected readonly ITreeValueAccessor<bool> _maskAccessor; // Optional mask accessor

        // Delegate for the static TOperator.Result method
        // Signature: TOperator.Result(TMap map, TInAccessor accessor, Coord ijk) -> TOutValue
        // Or specific schemes: TOperator.Result(TMap map, TInAccessor accessor, Coord ijk, DScheme scheme) -> TOutValue
        // This generic delegate will be tricky due to varied TOperator.Result signatures.
        // For now, assume a simplified, common signature or handle specific cases in Process.
        // Let's assume for this stage the operator is passed in a way that Process can call it.
        // For this example, Process will call specific static methods on TOperator.

        public GridOperator(TInputGrid inputGrid, TOutputGrid outputGrid, BoolGrid maskGrid = null)
        {
            _inputGrid = inputGrid ?? throw new ArgumentNullException(nameof(inputGrid));
            _outputGrid = outputGrid ?? throw new ArgumentNullException(nameof(outputGrid));

            // Try to cast/get the map. This is a simplification.
            // C++ GridOperator takes the map explicitly in its constructor.
            _map = inputGrid.Transform.GetMap() as TMap;
            if (_map == null && !(inputGrid.Transform.GetMap() is GenericMap)) // Allow GenericMap to be passed if TMap is IMap
            {
                 // If TMap is IMap, we can use GetMap() directly.
                if (typeof(TMap) == typeof(IMap))
                {
                    _map = inputGrid.Transform.GetMap() as TMap; // This should work
                }
                if (_map == null)
                {
                    // Try to get it via ToAffineMap if TMap is AffineMap
                    if (typeof(TMap) == typeof(AffineMap))
                    {
                         _map = inputGrid.Transform.GetMap().ToAffineMap() as TMap;
                    }
                }
                 if (_map == null)
                    throw new ArgumentException($"Input grid's transform map is not compatible with TMap ({typeof(TMap).Name}). Map is {inputGrid.Transform.GetMap().TypeName}.", nameof(inputGrid));
            }


            _inputAccessor = inputGrid.GetAccessor() as TInAccessor;
            if (_inputAccessor == null)
                throw new ArgumentException($"Input grid's accessor is not compatible with TInAccessor ({typeof(TInAccessor).Name}).", nameof(inputGrid));

            _outputAccessor = outputGrid.GetAccessor() as TOutAccessor;
            if (_outputAccessor == null)
                throw new ArgumentException($"Output grid's accessor is not compatible with TOutAccessor ({typeof(TOutAccessor).Name}).", nameof(outputGrid));

            _maskGrid = maskGrid;
            if (_maskGrid != null)
            {
                _maskAccessor = _maskGrid.GetAccessor() as ITreeValueAccessor<bool>;
                if (_maskAccessor == null)
                    throw new ArgumentException("Mask grid's accessor is not compatible with ITreeValueAccessor<bool>.", nameof(maskGrid));
            }
        }

        // This Process method needs to be adapted for different TOperator signatures.
        // This is a conceptual placeholder. Specific Process methods might be needed per operator type.
        public void Process(
            Func<TMap, TInAccessor, Coord, TOutValue> operation, // Simplified delegate
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            // Simplified iteration: C++ uses ValueOnCIter on input grid.
            // Here, iterate active bounding box of input grid.
            CoordBBox bbox = _inputGrid.EvalActiveVoxelBoundingBox();
            if (bbox.IsEmpty) return;

            Action<Coord> body = (Coord coord) =>
            {
                if (interrupt != null && interrupt.WasInterrupted()) return;

                if (_maskAccessor != null && !_maskAccessor.GetValue(coord)) // If mask provided and current voxel is false in mask
                {
                    return; // Skip processing this voxel
                }

                // Check if input voxel is active (important if iterating over bbox of potentially sparse grid)
                if (!_inputAccessor.IsValueOn(coord)) // This relies on placeholder IsValueOn
                {
                    // If input is not active, output might get background or a specific value
                    // For many operators, if input is inactive, output is background or no-op.
                    // This behavior is operator-specific. For now, we skip if input not active.
                    return;
                }

                TOutValue result = operation(_map, _inputAccessor, coord);
                _outputAccessor.SetValueOn(coord, result); // Activate and set value
            };

            if (!threaded || bbox.Volume() < 2000) // Heuristic for threading overhead
            {
                for (int k = bbox.Min.Z; k <= bbox.Max.Z; ++k)
                for (int j = bbox.Min.Y; j <= bbox.Max.Y; ++j)
                for (int i = bbox.Min.X; i <= bbox.Max.X; ++i)
                {
                    body(new Coord(i, j, k));
                }
            }
            else
            {
                Console.WriteLine("Warning: Threaded GridOperator.Process is a simplified placeholder, running serially.");
                 for (int k = bbox.Min.Z; k <= bbox.Max.Z; ++k)
                for (int j = bbox.Min.Y; j <= bbox.Max.Y; ++j)
                for (int i = bbox.Min.X; i <= bbox.Max.X; ++i)
                {
                    body(new Coord(i, j, k));
                }
                // Placeholder for parallel execution:
                // Parallel.For(bbox.Min.Z, bbox.Max.Z + 1, k => { ... similar loops for j, i ... body() });
            }
        }
    }


    public static class GridOperators
    {
        // Type converter placeholders - these are complex with C# generics.
        // Will use explicit output grid types for now.
        // public static Type ScalarToVectorGridType<TGridIn>(TGridIn inputGrid) { ... }
        // public static Type VectorToScalarGridType<TGridIn>(TGridIn inputGrid) { ... }

        // --- Gradient ---
        public static Vec3SGrid Gradient(
            FloatGrid inputGrid,
            DScheme scheme = DScheme.CD_2ND,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            var outputGrid = new Vec3SGrid(Vec3<float>.Zero); // Output grid with zero background
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_grad";

            var opExecutor = new GridOperator<FloatGrid, Vec3SGrid,
                                /*TOperator: WSGradient*/ object, // TOperator type is conceptual here
                                float, Vec3<float>,
                                IMap, // TMap: use IMap for broader compatibility initially
                                ITreeValueAccessor<float>, ITreeValueAccessor<Vec3<float>>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => WSGradient.Result<float, ITreeValueAccessor<float>, IMap>(map, accessor, coord, scheme),
                threaded, interrupt);

            return outputGrid;
        }

        // --- Magnitude ---
        // Define Op struct for Magnitude
        private struct MagnitudeOp<TVec, TValue>
            where TVec: struct, IReadOnlyVec<TValue>
            where TValue: struct, IFloatingPointIeee754<TValue>
        {
            public static TValue Result(IMap map, ITreeValueAccessor<TVec> accessor, Coord ijk)
            {
                TVec vec = accessor.GetValue(ijk);
                TValue sumSq = TValue.Zero;
                for(int i=0; i<vec.Size; ++i) sumSq += vec[i] * vec[i];
                return TValue.Sqrt(sumSq);
            }
        }

        public static FloatGrid Magnitude(
            Vec3SGrid inputGrid,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            var outputGrid = new FloatGrid(0.0f);
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_mag";

            var opExecutor = new GridOperator<Vec3SGrid, FloatGrid,
                                /*TOperator: MagnitudeOp*/ object,
                                Vec3<float>, float,
                                IMap,
                                ITreeValueAccessor<Vec3<float>>, ITreeValueAccessor<float>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => MagnitudeOp<Vec3<float>, float>.Result(map, accessor, coord),
                threaded, interrupt);

            return outputGrid;
        }

        // --- Normalize ---
        private struct NormalizeOp<TVec, TValue>
            where TVec : struct, IReadOnlyVec<TValue>, IEquatable<TVec> // Need methods for normalize
            where TValue : struct, IFloatingPointIeee754<TValue>
        {
            public static TVec Result(IMap map, ITreeValueAccessor<TVec> accessor, Coord ijk)
            {
                TVec vec = accessor.GetValue(ijk);
                // Requires TVec to have a Normalize method or be VecN<T>
                if (vec is Vec3<TValue> v3) { var n = v3.Normalized(); return (TVec)(object)n; }
                if (vec is Vec2<TValue> v2) { var n = v2.Normalized(); return (TVec)(object)n; }
                // Add Vec4 if needed
                return vec; // Return original if not normalizable VecN type
            }
        }
        public static Vec3SGrid Normalize(
            Vec3SGrid inputGrid,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            var outputGrid = new Vec3SGrid(Vec3<float>.Zero);
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_norm";

            var opExecutor = new GridOperator<Vec3SGrid, Vec3SGrid,
                                /*TOperator: NormalizeOp*/ object,
                                Vec3<float>, Vec3<float>,
                                IMap,
                                ITreeValueAccessor<Vec3<float>>, ITreeValueAccessor<Vec3<float>>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => NormalizeOp<Vec3<float>, float>.Result(map, accessor, coord),
                threaded, interrupt);
            return outputGrid;
        }


        // --- Laplacian ---
        public static FloatGrid Laplacian(
            FloatGrid inputGrid,
            DDScheme scheme = DDScheme.CD_SECOND,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            var outputGrid = new FloatGrid(0.0f);
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_laplacian";

            var opExecutor = new GridOperator<FloatGrid, FloatGrid,
                                /*TOperator: WSLaplacian*/ object,
                                float, float,
                                IMap,
                                ITreeValueAccessor<float>, ITreeValueAccessor<float>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => WSLaplacian.Result<float, ITreeValueAccessor<float>, IMap>(map, accessor, coord, scheme),
                threaded, interrupt);
            return outputGrid;
        }

        // --- CPT (Closest Point Transform) ---
        public static Vec3SGrid CPT(
            FloatGrid inputGrid,
            DScheme scheme = DScheme.CD_2ND,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            var outputGrid = new Vec3SGrid(Vec3<float>.Zero);
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_cpt";

            var opExecutor = new GridOperator<FloatGrid, Vec3SGrid,
                                /*TOperator: WSCPT*/ object,
                                float, Vec3<float>,
                                IMap,
                                ITreeValueAccessor<float>, ITreeValueAccessor<Vec3<float>>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => WSCPT.Result<float, ITreeValueAccessor<float>, IMap>(map, accessor, coord, scheme),
                threaded, interrupt);
            return outputGrid;
        }


        // --- Divergence (Placeholder) ---
        public static FloatGrid Divergence(
            Vec3SGrid inputGrid,
            DScheme scheme = DScheme.CD_2ND,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            Console.WriteLine("Warning: GridOperators.Divergence uses WSDivergence which might be a placeholder or only support specific maps.");
            var outputGrid = new FloatGrid(0.0f);
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_div";

             var opExecutor = new GridOperator<Vec3SGrid, FloatGrid,
                                /*TOperator: WSDivergence */ object,
                                Vec3<float>, float,
                                IMap,
                                ITreeValueAccessor<Vec3<float>>, ITreeValueAccessor<float>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => WSDivergence.Result<float, Vec3<float>, ITreeValueAccessor<Vec3<float>>, IMap>(map, accessor, coord, scheme),
                threaded, interrupt);

            return outputGrid;
        }

        // --- Curl (Placeholder) ---
         public static Vec3SGrid Curl(
            Vec3SGrid inputGrid,
            DScheme scheme = DScheme.CD_2ND,
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            Console.WriteLine("Warning: GridOperators.Curl uses WSCurl which might be a placeholder or only support specific maps.");
            var outputGrid = new Vec3SGrid(Vec3<float>.Zero);
            outputGrid.Transform = inputGrid.Transform.Clone();
            outputGrid.Name = inputGrid.Name + "_curl";

            var opExecutor = new GridOperator<Vec3SGrid, Vec3SGrid,
                                /*TOperator: WSCurl */ object,
                                Vec3<float>, Vec3<float>,
                                IMap,
                                ITreeValueAccessor<Vec3<float>>, ITreeValueAccessor<Vec3<float>>>(
                inputGrid, outputGrid, maskGrid);

            opExecutor.Process(
                (map, accessor, coord) => WSCurl.Result<float, Vec3<float>, ITreeValueAccessor<Vec3<float>>, IMap>(map, accessor, coord, scheme),
                threaded, interrupt);
            return outputGrid;
        }


        // --- MeanCurvature (Placeholder) ---
        public static FloatGrid MeanCurvature(
            FloatGrid inputGrid,
            DDScheme scheme = DDScheme.CD_SECOND, // MeanCurvature often uses 2nd order stencils
            BoolGrid maskGrid = null,
            bool threaded = true,
            IGridOperatorInterrupt interrupt = null)
        {
            throw new NotImplementedException("GridOperators.MeanCurvature is not yet implemented.");
        }
    }
}
