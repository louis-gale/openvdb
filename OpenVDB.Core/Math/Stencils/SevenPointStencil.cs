// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Core.Tree;

namespace OpenVDB.Core.Math.Stencils
{
    /// <summary>
    /// Represents a 7-point stencil (center + face neighbors) for grid operations.
    /// Values are typically accessed relative to the current center coordinate.
    /// </summary>
    /// <typeparam name="TValue">The type of value the stencil operates on.</typeparam>
    public class SevenPointStencil<TValue> : StencilBase<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SevenPointStencil{TValue}"/> class.
        /// </summary>
        /// <param name="accessor">The accessor to the grid data.</param>
        /// <param name="center">The initial center coordinate of the stencil.</param>
        public SevenPointStencil(ITreeValueAccessor<TValue> accessor, Coord center = default)
            : base(accessor, center)
        {
        }

        // Convenience properties/methods for accessing specific stencil points

        /// <summary> Gets the value at the stencil point (center.X + 1, center.Y, center.Z). </summary>
        public TValue PlusX() => GetValue(1, 0, 0);

        /// <summary> Gets the value at the stencil point (center.X - 1, center.Y, center.Z). </summary>
        public TValue MinusX() => GetValue(-1, 0, 0);

        /// <summary> Gets the value at the stencil point (center.X, center.Y + 1, center.Z). </summary>
        public TValue PlusY() => GetValue(0, 1, 0);

        /// <summary> Gets the value at the stencil point (center.X, center.Y - 1, center.Z). </summary>
        public TValue MinusY() => GetValue(0, -1, 0);

        /// <summary> Gets the value at the stencil point (center.X, center.Y, center.Z + 1). </summary>
        public TValue PlusZ() => GetValue(0, 0, 1);

        /// <summary> Gets the value at the stencil point (center.X, center.Y, center.Z - 1). </summary>
        public TValue MinusZ() => GetValue(0, 0, -1);

        // No additional data members beyond what StencilBase provides.
        // The "seven-point" nature is defined by how these accessors/methods are used
        // by algorithms (e.g., Laplacian operator using CenterValue and its 6 face neighbors).
    }
}
