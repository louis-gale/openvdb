// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Numerics; // For IFloatingPointIeee754
using OpenVDB.Core.Tree;
using OpenVDB.Math;

namespace OpenVDB.Core.Tools
{
    /// <summary>
    /// Interface for check predicates used with the Diagnose tool.
    /// </summary>
    /// <typeparam name="TValue">The type of value the predicate checks.</typeparam>
    public interface ICheckPredicate<in TValue>
    {
        /// <summary>
        /// Checks if the given value satisfies the predicate's condition.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value fails the check (i.e., an error is found), false otherwise.</returns>
        bool CheckFailed(TValue value);

        /// <summary>
        /// A description of the check being performed.
        /// </summary>
        string GetDescription();

        /// <summary>
        /// A description of the failure, typically including the problematic value.
        /// </summary>
        string GetFailureDescription(TValue value);
    }

    // Simple Value Checkers

    public struct CheckNaN<TValue> : ICheckPredicate<TValue> where TValue : IFloatingPointIeee754<TValue>
    {
        public string GetDescription() => "Checking for NaN (Not-a-Number) values";
        public bool CheckFailed(TValue value) => TValue.IsNaN(value);
        public string GetFailureDescription(TValue value) => $"NaN value found: {value}";
    }

    public struct CheckInf<TValue> : ICheckPredicate<TValue> where TValue : IFloatingPointIeee754<TValue>
    {
        public string GetDescription() => "Checking for Infinity values";
        public bool CheckFailed(TValue value) => TValue.IsInfinity(value);
        public string GetFailureDescription(TValue value) => $"Infinity value found: {value}";
    }

    public struct CheckFinite<TValue> : ICheckPredicate<TValue> where TValue : IFloatingPointIeee754<TValue>
    {
        public string GetDescription() => "Checking for non-finite (NaN or Infinity) values";
        public bool CheckFailed(TValue value) => !TValue.IsFinite(value);
        public string GetFailureDescription(TValue value) => $"Non-finite value found: {value}";
    }

    public struct CheckMagnitude<TValue> : ICheckPredicate<TValue> where TValue : struct, IFloatingPointIeee754<TValue>
    {
        private readonly TValue _maxMagnitude;
        public CheckMagnitude(TValue maxMagnitude) { _maxMagnitude = maxMagnitude; }
        public string GetDescription() => $"Checking for values with magnitude > {_maxMagnitude}";
        public bool CheckFailed(TValue value) => TValue.Abs(value).CompareTo(_maxMagnitude) > 0;
        public string GetFailureDescription(TValue value) => $"Value {value} exceeds max magnitude {_maxMagnitude}";
    }

    public struct CheckRange<TValue> : ICheckPredicate<TValue> where TValue : struct, IComparable<TValue>
    {
        private readonly TValue _min, _max;
        public CheckRange(TValue min, TValue max) { _min = min; _max = max; }
        public string GetDescription() => $"Checking for values outside range [{_min}, {_max}]";
        public bool CheckFailed(TValue value) => value.CompareTo(_min) < 0 || value.CompareTo(_max) > 0;
        public string GetFailureDescription(TValue value) => $"Value {value} is outside range [{_min}, {_max}]";
    }

    public struct CheckMin<TValue> : ICheckPredicate<TValue> where TValue : struct, IComparable<TValue>
    {
        private readonly TValue _minValue;
        public CheckMin(TValue minValue) { _minValue = minValue; }
        public string GetDescription() => $"Checking for values < {_minValue}";
        public bool CheckFailed(TValue value) => value.CompareTo(_minValue) < 0;
        public string GetFailureDescription(TValue value) => $"Value {value} is less than min {_minValue}";
    }

    public struct CheckMax<TValue> : ICheckPredicate<TValue> where TValue : struct, IComparable<TValue>
    {
        private readonly TValue _maxValue;
        public CheckMax(TValue maxValue) { _maxValue = maxValue; }
        public string GetDescription() => $"Checking for values > {_maxValue}";
        public bool CheckFailed(TValue value) => value.CompareTo(_maxValue) > 0;
        public string GetFailureDescription(TValue value) => $"Value {value} is greater than max {_maxValue}";
    }
    
    // Placeholder for more complex checks
    public struct CheckNormGrad<TValue> : ICheckPredicate<TValue> where TValue : struct, IFloatingPointIeee754<TValue>
    {
        public string GetDescription() => "Checking normalized gradient vectors (NOT IMPLEMENTED)";
        public bool CheckFailed(TValue value) => false; // Placeholder
        public string GetFailureDescription(TValue value) => "";
    }

    public struct CheckEikonal<TValue> : ICheckPredicate<TValue> where TValue : struct, IFloatingPointIeee754<TValue>
    {
        public string GetDescription() => "Checking Eikonal equation: |gradient| = 1 (NOT IMPLEMENTED)";
        public bool CheckFailed(TValue value) => false; // Placeholder
        public string GetFailureDescription(TValue value) => "";
    }

    public struct CheckDivergence<TValue> : ICheckPredicate<TValue> where TValue : struct, IFloatingPointIeee754<TValue>
    {
        public string GetDescription() => "Checking divergence (NOT IMPLEMENTED)";
        public bool CheckFailed(TValue value) => false; // Placeholder
        public string GetFailureDescription(TValue value) => "";
    }

    /// <summary>
    /// Tool for diagnosing issues within a grid by applying various check predicates.
    /// </summary>
    public class Diagnose<TGrid, TTree, TValue>
        where TGrid : Grid<TTree, TValue>
        where TTree : class, ITree<TValue>, new()
        where TValue : struct // Could be IFloatingPointIeee754<TValue> for many checks
    {
        private TGrid _grid;
        private BoolGrid _maskGrid; // Grid<Tree.Tree<bool>, bool>

        public long FailureCount { get; private set; }
        public long ValueCount { get; private set; } // Active values checked

        public BoolGrid Mask => _maskGrid;

        public Diagnose(TGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            // Initialize mask grid with the same transform as the input grid,
            // and background value 'false' (no failure).
            _maskGrid = new BoolGrid(false); // Background is false
            _maskGrid.Transform = grid.Transform.Clone();
            _maskGrid.Name = grid.Name + "_diag_mask";
            Clear();
        }

        public void Clear()
        {
            FailureCount = 0;
            ValueCount = 0;
            _maskGrid.Clear(); // Clear to background 'false'
        }

        /// <summary>
        /// Applies a check predicate to all active values in the grid.
        /// </summary>
        /// <typeparam name="TPredicate">The type of the check predicate.</typeparam>
        /// <param name="predicate">The predicate instance to apply.</param>
        /// <param name="updateMask">If true, the internal mask grid is updated with failure locations.</param>
        /// <param name="threaded">If true, attempt to run checks in parallel (currently placeholder).</param>
        public void Check<TPredicate>(TPredicate predicate, bool updateMask = true, bool threaded = false)
            where TPredicate : struct, ICheckPredicate<TValue> // Predicate is a struct
        {
            // For now, using a simplified iteration over active values.
            // A full implementation would use ValueOnCIter or similar from the grid.
            // This placeholder iterates all voxels in the active bounding box.
            // This is NOT equivalent to C++ version which uses ValueOnCIter.
            
            // This is a major simplification. The C++ version uses ValueTransformer::forEach
            // which operates on iterators. We don't have full iterators yet.
            // So, we'll iterate through the bounding box and use an accessor.
            // This will be slow and only check values within the active bounding box,
            // not necessarily only active voxels if the tree is sparse within that box.
            
            var accessor = _grid.GetAccessor();
            var activeBox = _grid.EvalActiveVoxelBoundingBox();
            
            if (activeBox.IsEmpty) return;

            // This iteration is a placeholder for proper ValueOnCIter traversal
            for (int k = activeBox.Min.Z; k <= activeBox.Max.Z; ++k)
            {
                for (int j = activeBox.Min.Y; j <= activeBox.Max.Y; ++j)
                {
                    for (int i = activeBox.Min.X; i <= activeBox.Max.X; ++i)
                    {
                        var coord = new Coord(i, j, k);
                        if (accessor.IsValueOn(coord)) // Check if it's actually active
                        {
                            ValueCount++;
                            TValue val = accessor.GetValue(coord);
                            if (predicate.CheckFailed(val))
                            {
                                FailureCount++;
                                if (updateMask)
                                {
                                    _maskGrid.GetAccessor().SetValue(coord, true); // Mark failure
                                }
                            }
                        }
                    }
                }
            }
             Console.WriteLine($"Warning: Diagnose.Check uses simplified BBox iteration, not true ValueOnCIter. Threading not implemented. Predicate: {predicate.GetDescription()}");
        }
    }

    /// <summary>
    /// Static class containing diagnostic utility methods.
    /// </summary>
    public static class Diagnostics
    {
        // checkLevelSet, checkFogVolume, uniqueInactiveValues will be added here.
        // These are complex and will be placeholders for now.

        public static List<string> CheckLevelSet<TGrid, TTree, TValue>(
            Grid<TTree, TValue> grid, int checkCount = 9)
            where TGrid : Grid<TTree, TValue> // Ensure TGrid is the specific grid type if needed
            where TTree : class, ITree<TValue>, new()
            where TValue : struct, IFloatingPointIeee754<TValue>, IComparable<TValue> // Common constraints for level set checks
        {
            var errors = new List<string>();
            if (grid == null) { errors.Add("Input grid is null."); return errors; }

            var diagnose = new Diagnose<TGrid, TTree, TValue>((TGrid)grid); // Cast needed if TGrid is more specific

            if (checkCount >= 1) // Check value type
            {
                if (!(typeof(TValue) == typeof(float) || typeof(TValue) == typeof(double)))
                    errors.Add($"Level set grid '{grid.Name}' has non-floating point value type: {grid.ValueTypeName}");
            }
            if (checkCount >= 2) // Check grid class
            {
                if (grid.GridClass != GridClass.LevelSet)
                    errors.Add($"Level set grid '{grid.Name}' has incorrect grid class: {grid.GridClass}");
            }
            // ... other fast checks ...

            if (checkCount >= 5) // Check for non-finite values
            {
                diagnose.Clear();
                diagnose.Check(new CheckFinite<TValue>());
                if (diagnose.FailureCount > 0)
                    errors.Add($"{diagnose.FailureCount} non-finite values found in '{grid.Name}' (e.g., NaN, Infinity)");
            }
            
            // Placeholder for Eikonal check (complex)
            if (checkCount >= 8)
            {
                diagnose.Clear();
                // diagnose.Check(new CheckEikonal<TValue>()); // Needs stencil access
                // if (diagnose.FailureCount > 0) errors.Add("Eikonal check failed (not fully implemented).");
                 errors.Add("Warning: Eikonal check for level sets is not yet implemented.");
            }

            Console.WriteLine($"Placeholder: checkLevelSet for '{grid.Name}' up to checkCount={checkCount}. Errors: {errors.Count}");
            return errors;
        }

        public static List<string> CheckFogVolume<TGrid, TTree, TValue>(
            Grid<TTree, TValue> grid, int checkCount = 6)
            where TGrid : Grid<TTree, TValue>
            where TTree : class, ITree<TValue>, new()
            where TValue : struct, IFloatingPointIeee754<TValue>, IComparable<TValue> 
        {
            var errors = new List<string>();
            if (grid == null) { errors.Add("Input grid is null."); return errors; }

            var diagnose = new Diagnose<TGrid, TTree, TValue>((TGrid)grid);

            if (checkCount >= 1 && grid.GridClass != GridClass.FogVolume)
                errors.Add($"Fog volume grid '{grid.Name}' has incorrect grid class: {grid.GridClass}");

            if (checkCount >= 2) // Check background is zero
            {
                if (!TValue.IsZero(grid.Background))
                     errors.Add($"Fog volume grid '{grid.Name}' has non-zero background: {grid.Background}");
            }
            
            if (checkCount >= 3) // Check for values outside [0,1] for normalized fog
            {
                // This check is heuristic, fog volumes are not strictly normalized
                // errors.Add("Warning: Fog volume range check [0,1] is heuristic and not always applicable.");
                // diagnose.Clear();
                // diagnose.Check(new CheckRange<TValue>(TValue.Zero, TValue.One));
                // if (diagnose.FailureCount > 0)
                //    errors.Add($"{diagnose.FailureCount} values found outside [0,1] in fog volume '{grid.Name}'");
            }


            Console.WriteLine($"Placeholder: checkFogVolume for '{grid.Name}' up to checkCount={checkCount}. Errors: {errors.Count}");
            return errors;
        }

        public static List<TValue> UniqueInactiveValues<TGrid, TTree, TValue>(
            Grid<TTree, TValue> grid, int numValuesToFind = 10)
            where TGrid : Grid<TTree, TValue>
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
        {
            var uniqueValues = new List<TValue>();
            if (grid == null) return uniqueValues;

            // This requires a ValueOffCIter, which is not fully implemented.
            // Placeholder logic:
            Console.WriteLine("Warning: uniqueInactiveValues is a placeholder and does not iterate inactive values yet.");
            // Example of how it might work if iterators were available:
            // var seen = new HashSet<TValue>();
            // var iter = grid.beginValueOff(); // Assuming this exists and works
            // while(iter.MoveNext() && uniqueValues.Count < numValuesToFind)
            // {
            //    if(seen.Add(iter.Value))
            //    {
            //        uniqueValues.Add(iter.Value);
            //    }
            // }
            return uniqueValues;
        }
    }
}
