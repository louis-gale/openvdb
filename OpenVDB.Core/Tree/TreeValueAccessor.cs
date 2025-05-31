// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using OpenVDB.Math;

namespace OpenVDB.Core.Tree
{
    // Delegates for ModifyValue methods
    public delegate void ValueModifierDelegate<TValue>(ref TValue value) where TValue : struct;
    public delegate void ValueStateModifierDelegate<TValue>(ref TValue value, ref bool isActive) where TValue : struct;

    /// <summary>
    /// Provides random access to voxels in a Tree.
    /// This is a basic implementation. A full C++ ValueAccessor has more features
    /// like caching node pointers for performance.
    /// </summary>
    public class TreeValueAccessor<TValue> : ITreeValueAccessor<TValue> where TValue : struct
    {
        private readonly Tree<TValue> _tree;

        public TreeValueAccessor(Tree<TValue> tree)
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }

        public bool TryGetGrid(out GridBase grid) // Helper for SimplifiedNodeInfo
        {
            // This accessor is tied to a Tree, not directly a Grid.
            // However, a Tree is usually owned by a Grid.
            // For now, this concept doesn't fit well here unless Tree holds a ref to its Grid.
            grid = null;
            return false;
        }


        public TValue GetValue(Coord xyz) => _tree.GetValue(xyz);

        public void SetValue(Coord xyz, TValue value) => _tree.SetValue(xyz, value); // SetValue on Tree implies SetValueOn

        public bool IsValueOn(Coord xyz) => _tree.IsValueOn(xyz);

        public void SetActiveState(Coord xyz, bool on) => _tree.SetActiveState(xyz, on);

        public void SetValueOn(Coord xyz, TValue value) => _tree.SetValueOn(xyz, value);

        public void SetValueOff(Coord xyz) => _tree.SetValueOff(xyz, _tree.BackgroundValue); // Set to background and off

        public void SetValueOff(Coord xyz, TValue value) => _tree.SetValueOff(xyz, value);


        public void ModifyValue(Coord xyz, ValueModifierDelegate<TValue> modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            // This requires the tree to have a method that can pass the value by ref.
            // For now, this will delegate to Tree.ModifyValue
             _tree.ModifyValue(xyz, modifier);
        }

        public void ModifyValueAndActiveState(Coord xyz, ValueStateModifierDelegate<TValue> modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            // This requires the tree to have a method that can pass value and state by ref.
            // For now, this will delegate to Tree.ModifyValueAndActiveState
            _tree.ModifyValueAndActiveState(xyz, modifier);
        }
    }
}
