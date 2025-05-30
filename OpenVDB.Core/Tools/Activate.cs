// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic; // For potential list-based iteration if tree iterators are not ready
using System.Numerics;
using OpenVDB.Core.Tree;
using OpenVDB.Math;

namespace OpenVDB.Core.Tools
{
    internal static class ActivateInternal
    {
        // Base class for common logic (optional, but can be useful)
        internal abstract class ValueMatcherOperation<TValue> where TValue : struct
        {
            protected readonly TValue _targetValue;
            protected readonly TValue _tolerance;
            protected readonly bool _exactMatch; // True if tolerance is zero

            protected ValueMatcherOperation(TValue targetValue, TValue tolerance)
            {
                _targetValue = targetValue;
                _tolerance = tolerance;
                
                if (tolerance is IFloatingPointIeee754<TValue> tolFloat)
                {
                    _exactMatch = TValue.IsZero(tolFloat);
                }
                else if (tolerance is IComparisonOperators<TValue, TValue, bool> tolComp) // For integral types
                {
                     _exactMatch = tolComp.Equals(tolerance, default(TValue)); // Check if tolerance is zero for integral types
                }
                else
                {
                    _exactMatch =EqualityComparer<TValue>.Default.Equals(tolerance, default(TValue));
                }
            }

            public bool IsMatch(TValue inputValue)
            {
                if (_exactMatch)
                {
                    return EqualityComparer<TValue>.Default.Equals(inputValue, _targetValue);
                }
                else if (_targetValue is IFloatingPointIeee754<TValue> targetFloat && 
                         inputValue is IFloatingPointIeee754<TValue> inputFloat &&
                         _tolerance is IFloatingPointIeee754<TValue> tolFloat)
                {
                    return TValue.Abs(targetFloat - inputFloat) <= tolFloat;
                }
                else if (_targetValue is IComparable<TValue> targetComparable) // Fallback for non-float IComparable types
                {
                    // This simple comparison might not be what OpenVDB C++ does for non-float tolerance.
                    // C++ typically uses tolerance only for float/double.
                    // For integer types, it's usually an exact match.
                    // If tolerance is non-zero for integers, it's an error or specific range check.
                    // For simplicity, we assume if tolerance is non-zero for non-floats, it means exact match.
                    // This part needs clarification based on OpenVDB's intent for non-float tolerance.
                    // The C++ code uses math::isApproxEqual which is for Real types.
                    // Let's assume non-float types must match exactly if tolerance is not zero (which is odd).
                    // Or, more likely, tolerance for non-floats means an exact match is required.
                    return EqualityComparer<TValue>.Default.Equals(inputValue, _targetValue);
                }
                return EqualityComparer<TValue>.Default.Equals(inputValue, _targetValue);
            }

            // These Process* methods are conceptual placeholders.
            // A real implementation needs access to tree node structures and iterators.
            // For now, they might take an accessor and a list of Coords to process for that node.
            public virtual void ProcessNodeValues(ITreeValueAccessor<TValue> accessor, IEnumerable<Coord> coordsToProcess)
            {
                // To be implemented by derived ActivateOperation/DeactivateOperation
                throw new NotImplementedException("ProcessNodeValues must be implemented in derived operation.");
            }
        }

        internal class ActivateOperation<TValue> : ValueMatcherOperation<TValue> where TValue : struct
        {
            public ActivateOperation(TValue targetValue, TValue tolerance) 
                : base(targetValue, tolerance) { }

            public override void ProcessNodeValues(ITreeValueAccessor<TValue> accessor, IEnumerable<Coord> coordsToProcess)
            {
                foreach (var coord in coordsToProcess)
                {
                    if (!accessor.IsValueOn(coord)) // Process only currently inactive voxels
                    {
                        TValue val = accessor.GetValue(coord); // Get potentially inactive value
                        if (IsMatch(val))
                        {
                            accessor.SetValueOn(coord, val); // Activate with its current value
                        }
                    }
                }
            }
        }

        internal class DeactivateOperation<TValue> : ValueMatcherOperation<TValue> where TValue : struct
        {
            public DeactivateOperation(TValue targetValue, TValue tolerance)
                : base(targetValue, tolerance) { }
            
            public override void ProcessNodeValues(ITreeValueAccessor<TValue> accessor, IEnumerable<Coord> coordsToProcess)
            {
                foreach (var coord in coordsToProcess)
                {
                    if (accessor.IsValueOn(coord)) // Process only currently active voxels
                    {
                        TValue val = accessor.GetValue(coord);
                        if (IsMatch(val))
                        {
                            accessor.SetValueOff(coord); // Deactivate
                        }
                    }
                }
            }
        }
    }


    public static class Activate
    {
        // Simplified top-down traversal placeholder.
        // A real traversal would visit root, then internal nodes, then leaf nodes,
        // using appropriate iterators (ValueOffIter for Activate, ValueOnIter for Deactivate)
        // or by iterating over all voxels in a node's domain and checking their state.
        private static void TraverseAndProcess<TGrid, TTree, TValue, TOperation>(
            TGrid grid, TOperation op)
            where TGrid : Grid<TTree, TValue>
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
            where TOperation : ActivateInternal.ValueMatcherOperation<TValue>
        {
            var accessor = grid.GetAccessor();
            CoordBBox bbox = grid.EvalActiveVoxelBoundingBox(); // Process active bbox as a simplification

            if (grid.IsEmpty && op is ActivateInternal.ActivateOperation<TValue>)
            {
                // If activating and grid is empty, check if background matches target.
                // This case is complex: if background matches, the entire grid conceptually becomes active.
                // This is not handled by simple iteration. For now, we'll iterate BBox.
                // A full solution might involve changing tree's background or dense filling.
                 if (op.IsMatch(grid.Background))
                 {
                     Console.WriteLine($"Warning: Activate called on empty grid where background ({grid.Background}) matches target. Full activation not implemented by simple BBox iteration.");
                     // If we were to activate everything, we'd need to fill.
                     // For now, if bbox is empty, this loop won't run.
                 }
            }
            
            if (bbox.IsEmpty && !(op is ActivateInternal.ActivateOperation<TValue> && op.IsMatch(grid.Background)))
            {
                // If not activating a matching background on an empty grid, there's nothing to do.
                return; 
            }

            // Create a list of coordinates to process. In a real scenario, this would
            // come from tree iterators (ValueOff for Activate, ValueOn for Deactivate).
            // For this placeholder, we iterate all cells in the active bounding box.
            // This is INEFFICIENT and NOT CORRECT for sparse grids but allows testing op logic.
            var coordsToConsider = new List<Coord>();
            if (!bbox.IsEmpty)
            {
                 for (int k = bbox.Min.Z; k <= bbox.Max.Z; ++k)
                    for (int j = bbox.Min.Y; j <= bbox.Max.Y; ++j)
                        for (int i = bbox.Min.X; i <= bbox.Max.X; ++i)
                            coordsToConsider.Add(new Coord(i, j, k));
            }
            // If activating an empty grid with matching background, we might need to consider a default region or throw.
            // For now, if bbox is empty, coordsToConsider will be empty.

            op.ProcessNodeValues(accessor, coordsToConsider);
            
            Console.WriteLine($"Warning: Activate/Deactivate used simplified BBox iteration. True tree traversal not yet implemented.");
        }

        public static void DoActivate<TGrid, TTree, TValue>(
            TGrid grid, TValue value, TValue tolerance, bool threaded = true)
            where TGrid : Grid<TTree, TValue>
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var op = new ActivateInternal.ActivateOperation<TValue>(value, tolerance);
            TraverseAndProcess<TGrid, TTree, TValue, ActivateInternal.ActivateOperation<TValue>>(grid, op);
            // Threading not implemented for this placeholder traversal
        }

        public static void DoDeactivate<TGrid, TTree, TValue>(
            TGrid grid, TValue value, TValue tolerance, bool threaded = true)
            where TGrid : Grid<TTree, TValue>
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var op = new ActivateInternal.DeactivateOperation<TValue>(value, tolerance);
            TraverseAndProcess<TGrid, TTree, TValue, ActivateInternal.DeactivateOperation<TValue>>(grid, op);
            // Threading not implemented for this placeholder traversal
        }
    }
}
