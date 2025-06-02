// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using OpenVDB.Core.Tree;
using OpenVDB.Math; // For ITreeValueAccessor

namespace OpenVDB.Core.Math.Stencils
{
    /// <summary>
    /// Abstract base class for stencils operating on a grid via an accessor.
    /// </summary>
    /// <typeparam name="TValue">The type of value the stencil operates on.</typeparam>
    public abstract class StencilBase<TValue>
    {
        protected readonly ITreeValueAccessor<TValue> _accessor;
        protected Coord _centerCoord;

        /// <summary>
        /// Initializes a new instance of the <see cref="StencilBase{TValue}"/> class.
        /// </summary>
        /// <param name="accessor">The accessor to the grid data.</param>
        /// <param name="center">The initial center coordinate of the stencil.</param>
        protected StencilBase(ITreeValueAccessor<TValue> accessor, Coord center = default)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            _centerCoord = center;
        }

        /// <summary>
        /// Moves the center of the stencil to a new coordinate.
        /// </summary>
        /// <param name="newCenter">The new center coordinate.</param>
        public virtual void MoveTo(Coord newCenter)
        {
            _centerCoord = newCenter;
        }

        /// <summary>
        /// Gets the current center coordinate of the stencil.
        /// </summary>
        public Coord GetCenterCoord() => _centerCoord;

        /// <summary>
        /// Gets the grid accessor used by this stencil.
        /// </summary>
        public ITreeValueAccessor<TValue> GetAccessor() => _accessor;

        /// <summary>
        /// Gets the value from the grid at an offset relative to the stencil's center.
        /// </summary>
        /// <param name="di">The offset in the X direction.</param>
        /// <param name="dj">The offset in the Y direction.</param>
        /// <param name="dk">The offset in the Z direction.</param>
        /// <returns>The value at the offsetted grid coordinate.</returns>
        public virtual TValue GetValue(int di, int dj, int dk)
        {
            return _accessor.GetValue(_centerCoord.OffsetBy(di, dj, dk));
        }

        /// <summary>
        /// Gets the value from the grid at a given offset from the stencil's center.
        /// </summary>
        /// <param name="offset">The offset coordinate.</param>
        /// <returns>The value at the offsetted grid coordinate.</returns>
        public virtual TValue GetValue(Coord offset)
        {
            return _accessor.GetValue(_centerCoord + offset);
        }

        /// <summary>
        /// Gets the value at the stencil's center.
        /// </summary>
        public TValue CenterValue() => GetValue(0, 0, 0);
    }
}
