// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Core.Tree;
using System;
using System.Collections.Generic;

namespace OpenVDB.Core.Math.Stencils
{
    /// <summary>
    /// Represents a cubic box-shaped stencil (e.g., 3x3x3, 5x5x5) centered on a voxel.
    /// </summary>
    /// <typeparam name="TValue">The type of value the stencil operates on.</typeparam>
    public class BoxStencil<TValue> : StencilBase<TValue>
    {
        private int _radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxStencil{TValue}"/> class.
        /// </summary>
        /// <param name="accessor">The accessor to the grid data.</param>
        /// <param name="radius">The radius of the box. A radius of 1 creates a 3x3x3 stencil.</param>
        /// <param name="center">The initial center coordinate of the stencil.</param>
        public BoxStencil(ITreeValueAccessor<TValue> accessor, int radius = 1, Coord center = default)
            : base(accessor, center)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");
            _radius = radius;
        }

        /// <summary>
        /// Gets the current radius of the stencil.
        /// </summary>
        public int GetRadius() => _radius;

        /// <summary>
        /// Sets a new radius for the stencil.
        /// </summary>
        public void SetRadius(int radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");
            _radius = radius;
        }

        /// <summary>
        /// Gets the side length of the box stencil (e.g., radius 1 means 3x3x3, so size is 3).
        /// </summary>
        public int GetSize() => 2 * _radius + 1;

        /// <summary>
        /// Gets the minimum local offset of the box (e.g., (-radius, -radius, -radius)).
        /// </summary>
        public Coord GetMin() => new Coord(-_radius, -_radius, -_radius);

        /// <summary>
        /// Gets the maximum local offset of the box (e.g., (radius, radius, radius)).
        /// </summary>
        public Coord GetMax() => new Coord(_radius, _radius, _radius);

        /// <summary>
        /// Returns an enumerable collection of all values within the stencil's box.
        /// Iteration order is X-fastest, then Y, then Z.
        /// </summary>
        public IEnumerable<TValue> GetAllValues()
        {
            for (int k = -_radius; k <= _radius; ++k)
            {
                for (int j = -_radius; j <= _radius; ++j)
                {
                    for (int i = -_radius; i <= _radius; ++i)
                    {
                        yield return GetValue(i, j, k);
                    }
                }
            }
        }

        /// <summary>
        /// Returns an enumerable collection of all coordinates (as local offsets) within the stencil's box.
        /// Iteration order is X-fastest, then Y, then Z.
        /// </summary>
        public IEnumerable<Coord> GetAllLocalCoords()
        {
            for (int k = -_radius; k <= _radius; ++k)
            {
                for (int j = -_radius; j <= _radius; ++j)
                {
                    for (int i = -_radius; i <= _radius; ++i)
                    {
                        yield return new Coord(i, j, k);
                    }
                }
            }
        }
    }
}
