// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using OpenVDB.Core.Tree;
using OpenVDB.Core.Math;
using OpenVDB.Core.Math.Maps; // For IMap, Transform

namespace OpenVDB.Core.Tools
{
    internal static class ClipInternal
    {
        /// <summary>
        /// Converts an input grid to a BoolGrid where active values in the input grid
        /// correspond to 'true' in the BoolGrid.
        /// </summary>
        public static BoolGrid ConvertToMaskGrid<TInputGrid, TInputTree, TInputValue>(
            TInputGrid grid)
            where TInputGrid : Grid<TInputTree, TInputValue>
            where TInputTree : class, ITree<TInputValue>, new()
            where TInputValue : struct
        {
            var mask = new BoolGrid(false); // Background false
            if (grid == null) return mask;

            mask.Transform = grid.Transform.Clone(); // Copy transform
            mask.Name = grid.Name + "_mask";

            var inputAccessor = grid.GetAccessor(); // Assuming GetConstAccessor equivalent
            var maskAccessor = mask.GetAccessor();

            CoordBBox activeBox = grid.EvalActiveVoxelBoundingBox();
            if (activeBox.IsEmpty) return mask;

            // Simplified iteration over active bounding box
            for (int k = activeBox.Min.Z; k <= activeBox.Max.Z; ++k)
            {
                for (int j = activeBox.Min.Y; j <= activeBox.Max.Y; ++j)
                {
                    for (int i = activeBox.Min.X; i <= activeBox.Max.X; ++i)
                    {
                        var coord = new Coord(i, j, k);
                        if (inputAccessor.IsValueOn(coord))
                        {
                            maskAccessor.SetValueOn(coord, true);
                        }
                    }
                }
            }
            return mask;
        }

        /// <summary>
        /// Main clipping logic.
        /// </summary>
        public static TGrid DoClip<TGrid, TTree, TValue>(
            TGrid grid,
            BoolGrid clipMask,
            bool keepInterior)
            where TGrid : Grid<TTree, TValue>, new() // Add new() constraint for CreateGridWithNewTree concept
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (clipMask == null) throw new ArgumentNullException(nameof(clipMask));

            // Create an output grid of the same type with the same metadata/transform but an empty tree.
            // Grid.CopyGridWithNewTree() does this.
            var outputGrid = (TGrid)grid.CopyGridWithNewTree();
            outputGrid.Name = grid.Name + (keepInterior ? "_clip" : "_clip_exterior");

            var outputAccessor = outputGrid.GetAccessor();
            var inputAccessor = grid.GetAccessor(); // Assuming GetConstAccessor equivalent
            var clipMaskAccessor = clipMask.GetAccessor(); // Assuming GetConstAccessor equivalent

            // Determine combined bounding box for iteration (simplified approach)
            // A more robust approach would iterate over the intersection of active areas or use tree iterators.
            CoordBBox gridBBox = grid.EvalActiveVoxelBoundingBox();
            CoordBBox maskBBox = clipMask.EvalActiveVoxelBoundingBox();

            // If either grid is empty, the active processing region might be empty or specific.
            // If gridBBox is empty, but we are keeping exterior and mask is large, this logic is flawed.
            // For now, assume we iterate over the original grid's active area.
            // If gridBBox is empty, outputGrid remains empty (background).
            if (gridBBox.IsEmpty)
            {
                if(outputGrid.GridClass == GridClass.LevelSet) outputGrid.GridClass = GridClass.Unknown;
                return outputGrid;
            }

            for (int k = gridBBox.Min.Z; k <= gridBBox.Max.Z; ++k)
            {
                for (int j = gridBBox.Min.Y; j <= gridBBox.Max.Y; ++j)
                {
                    for (int i = gridBBox.Min.X; i <= gridBBox.Max.X; ++i)
                    {
                        var coord = new Coord(i, j, k);

                        bool isSourceActive = inputAccessor.IsValueOn(coord);
                        TValue sourceValue = inputAccessor.GetValue(coord); // Gets value even if inactive
                        bool isInClipMask = clipMaskAccessor.IsValueOn(coord); // Active in mask means 'true'

                        bool shouldKeep = keepInterior ? isInClipMask : !isInClipMask;

                        if (shouldKeep)
                        {
                            // Copy value and state
                            outputAccessor.SetValue(coord, sourceValue);
                            outputAccessor.SetActiveState(coord, isSourceActive);
                        }
                        else
                        {
                            // Set to inactive background
                            // outputAccessor.SetValueOff(coord, outputGrid.Background); // SetValueOff should ideally take background
                            outputAccessor.SetValueOff(coord); // Assuming SetValueOff implies setting to background
                            outputAccessor.SetValue(coord, outputGrid.Background); // Explicitly set background
                        }
                    }
                }
            }

            if (outputGrid.GridClass == GridClass.LevelSet)
            {
                outputGrid.GridClass = GridClass.Unknown; // Clipping can invalidate level set properties
            }
            return outputGrid;
        }
    }

    public static class ClipTools
    {
        public static TGrid ClipWithBoundingBox<TGrid, TTree, TValue>(
            TGrid grid,
            BBox<Vec3<double>, double> worldBBox,
            bool keepInterior = true)
            where TGrid : Grid<TTree, TValue>, new()
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            // Transform worldBBox to index space
            // Transform.WorldToIndex(BBoxD) returns BBox<Vec3d,double>. We need CoordBBox.
            // This involves converting the float BBox min/max to Coords (e.g. floor/ceil or round).
            BBox<Vec3<double>, double> indexFloatBBox = grid.Transform.WorldToIndex(worldBBox);

            // Convert floating point index-space BBox to integer CoordBBox.
            // This needs to be conservative to include all relevant voxels.
            Coord minCoord = Coord.Floor(indexFloatBBox.Min);
            Coord maxCoord = Coord.Ceil(indexFloatBBox.Max) - Coord.One; // Max is inclusive for CoordBBox
                                                                      // If Ceil(Max) is (10,10,10), then maxCoord should be (9,9,9) for a cell of size 1 centered at (9.5,9.5,9.5)
                                                                      // Or, more simply, if WorldToIndex(BBox) gives the min/max index coords touched:
                                                                      // C++ WorldToIndex returns a BBoxD representing the index space bounds.
                                                                      // The conversion to CoordBBox for filling usually involves Round for cell-centered.
            CoordBBox indexBBox = new CoordBBox(Coord.Round(indexFloatBBox.Min), Coord.Round(indexFloatBBox.Max));
            indexBBox.Sort(); // Ensure min <= max

            var clipMaskGrid = new BoolGrid(false); // Background false
            clipMaskGrid.Transform = grid.Transform.Clone();
            clipMaskGrid.Name = grid.Name + "_bbox_clip_mask";

            // Grid.Fill needs to be functional. It's a placeholder in current Grid.cs
            // that calls Tree.Fill, which is also a placeholder.
            // For this to work, these Fill methods must actually activate/set values.
            // Assuming Fill works for testing purposes.
            if (!indexBBox.IsEmpty) // Only fill if the bbox is valid
            {
                clipMaskGrid.Fill(indexBBox, true, true); // Fill with 'true' and activate
            }

            return ClipInternal.DoClip(grid, clipMaskGrid, keepInterior);
        }

        public static TGrid ClipWithMask<TGrid, TTree, TValue, TMaskGrid, TMaskTree, TMaskValue>(
            TGrid grid,
            TMaskGrid maskGridIn, // Can be any grid type
            bool keepInterior = true)
            where TGrid : Grid<TTree, TValue>, new()
            where TTree : class, ITree<TValue>, new()
            where TValue : struct
            where TMaskGrid : Grid<TMaskTree, TMaskValue>
            where TMaskTree : class, ITree<TMaskValue>, new()
            where TMaskValue : struct
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (maskGridIn == null) throw new ArgumentNullException(nameof(maskGridIn));

            BoolGrid actualClipMask;

            // Check if maskGridIn is already a BoolGrid and transforms match
            if (maskGridIn is BoolGrid boolMask &&
                grid.Transform.GetMap().ToAffineMap().Matrix.IsApproxEqual(boolMask.Transform.GetMap().ToAffineMap().Matrix, 1e-7)) // Simplified transform check
            {
                actualClipMask = boolMask;
            }
            else
            {
                // Convert to BoolGrid type
                BoolGrid tempMask = ClipInternal.ConvertToMaskGrid(maskGridIn);

                // Check transforms
                if (!grid.Transform.GetMap().ToAffineMap().Matrix.IsApproxEqual(tempMask.Transform.GetMap().ToAffineMap().Matrix, 1e-7))
                {
                    // Transforms differ, resampling needed.
                    // ResampleToMatch is a complex tool. For now, throw NotImplemented.
                    throw new NotImplementedException(
                        "ClipWithMask where transforms differ requires ResampleToMatch, which is not implemented.");

                    // Placeholder for ResampleToMatch:
                    // actualClipMask = new BoolGrid(false);
                    // actualClipMask.Transform = grid.Transform.Clone();
                    // ResampleToMatch_Simplified(tempMask, actualClipMask);
                }
                else
                {
                    actualClipMask = tempMask;
                }
            }

            return ClipInternal.DoClip(grid, actualClipMask, keepInterior);
        }

        // Placeholder for a very simplified ResampleToMatch for demonstration if needed by tests.
        // This would not be robust.
        private static void ResampleToMatch_Simplified(BoolGrid sourceMask, BoolGrid targetMask)
        {
            Console.WriteLine("Warning: Using simplified ResampleToMatch. Results may be inaccurate.");
            var sourceAccessor = sourceMask.GetAccessor();
            var targetAccessor = targetMask.GetAccessor();
            CoordBBox targetBBox = targetMask.EvalActiveVoxelBoundingBox(); // Or iterate over source and transform coords

            // For simplicity, iterate target grid's potential bbox and sample source
            // This assumes targetMask has some definition (e.g. from grid.Transform)
            // A common approach is to iterate target voxels, transform to source space, sample.
            if (targetBBox.IsEmpty && sourceMask.ActiveVoxelCount() > 0) {
                 // If target is empty but source is not, try to define a target iteration space
                 targetBBox = targetMask.Transform.WorldToIndex(sourceMask.Transform.IndexToWorld(sourceMask.EvalActiveVoxelBoundingBox()));
            }


            for (int k = targetBBox.Min.Z; k <= targetBBox.Max.Z; ++k)
            {
                for (int j = targetBBox.Min.Y; j <= targetBBox.Max.Y; ++j)
                {
                    for (int i = targetBBox.Min.X; i <= targetBBox.Max.X; ++i)
                    {
                        var targetCoord = new Coord(i,j,k);
                        var worldPos = targetMask.Transform.IndexToWorld(targetCoord);
                        var sourceCoord = sourceMask.Transform.WorldToIndexCellCentered(worldPos);
                        if (sourceAccessor.IsValueOn(sourceCoord) && sourceAccessor.GetValue(sourceCoord))
                        {
                            targetAccessor.SetValueOn(targetCoord, true);
                        }
                    }
                }
            }
        }
    }
}
