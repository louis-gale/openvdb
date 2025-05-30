// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

namespace OpenVDB.Core.Math
{
    /// <summary>
    /// Interface for read-only access to vector components by index.
    /// </summary>
    /// <typeparam name="TValue">The type of the vector components.</typeparam>
    public interface IReadOnlyVec<out TValue> where TValue : struct
    {
        TValue this[int component] { get; }
        int Size { get; }
    }
}
