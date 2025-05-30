// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tools;
using OpenVDB.Core.Tree; // For ITreeValueAccessor
using OpenVDB.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVDB.Core.Tests.Tools
{
    // Dummy Accessor for testing SetValueOn* methods
    // In a real scenario, this would be from an actual grid.
    internal class TestAccessor<T> : ITreeValueAccessor<T> where T : struct
    {
        public Dictionary<Coord, T> Data = new Dictionary<Coord, T>();
        public T Background = default;

        public TestAccessor(T background) { Background = background; }

        public T GetValue(Coord xyz) => Data.TryGetValue(xyz, out T val) ? val : Background;
        public void SetValue(Coord xyz, T value) => Data[xyz] = value;
        public bool IsValueOn(Coord xyz) => Data.ContainsKey(xyz); // Simplified: on if in dictionary
        public void SetValueOn(Coord xyz, T value) => Data[xyz] = value;
        public void SetValueOff(Coord xyz) => Data.Remove(xyz);
    }


    [TestFixture]
    public class ValueTransformerTests
    {
        private const double Epsilon = 1e-9;

        [Test]
        public void ForEach_Serial_ShouldModifyValues()
        {
            var data = new List<Tuple<Coord, float, bool>>
            {
                Tuple.Create(new Coord(0, 0, 0), 1.0f, true),
                Tuple.Create(new Coord(1, 0, 0), 2.0f, true),
                Tuple.Create(new Coord(0, 1, 0), 3.0f, true)
            };
            
            // The DummyGridIterator<object> and Action<DummyGridIterator<object>> will require casting
            // or a more flexible DummyGridIterator. Let's adapt the test data and action.
            // For simplicity, we'll assume the operation modifies an external collection based on iterator values.
            var externalDataStore = data.ToDictionary(t => t.Item1, t => t.Item2);

            // The DummyGridIterator in ValueTransformer.cs takes List<Tuple<Coord, TValue, bool>>
            // The ForEach takes TIterator : class, IGridIterator<object>
            // This implies TValue for DummyGridIterator must be object, or ForEach signature needs adjustment.
            // Let's assume we can create a list of Tuple<Coord, object, bool> for the dummy iterator.

            var objectData = data.Select(t => Tuple.Create(t.Item1, (object)t.Item2, t.Item3)).ToList();
            var iterator = new DummyGridIterator<object>(objectData);

            Action<DummyGridIterator<object>> operation = (iter) =>
            {
                if (iter.Value is float val)
                {
                    externalDataStore[iter.Coord] = val * 2.0f;
                }
            };

            ValueTransformer.ForEach(iterator, operation, threaded: false);

            Assert.AreEqual(2.0f, externalDataStore[new Coord(0, 0, 0)]);
            Assert.AreEqual(4.0f, externalDataStore[new Coord(1, 0, 0)]);
            Assert.AreEqual(6.0f, externalDataStore[new Coord(0, 1, 0)]);
        }

        [Test]
        public void TransformValues_Serial_ShouldTransformAndWriteToOutputGrid()
        {
            var inputData = new List<Tuple<Coord, float, bool>>
            {
                Tuple.Create(new Coord(0, 0, 0), 1.5f, true),
                Tuple.Create(new Coord(1, 1, 1), 2.7f, true),
                Tuple.Create(new Coord(2, 2, 2), -3.3f, true)
            };
            var objectInputData = inputData.Select(t => Tuple.Create(t.Item1, (object)t.Item2, t.Item3)).ToList();
            var inputIterator = new DummyGridIterator<object>(objectInputData);

            var outputGrid = new Int32Grid(-1); // Background -1
            var outputAccessor = outputGrid.GetAccessor(); // Assuming this works with placeholder tree

            // Func<TIterator, TOutValue>
            // TInIterator is DummyGridIterator<object>
            // TOutValue is int
            Func<DummyGridIterator<object>, int> transformFunction = (iter) =>
            {
                if (iter.Value is float floatVal)
                {
                    return (int)(floatVal * 10.0f);
                }
                return -999; // Should not happen if input data is correct
            };
            
            // The constraints on TransformValues are:
            // TInIterator : class, IGridIterator<TInValue>
            // TOutGrid : Grid<TOutTree, TOutValue>
            // TOutTree : class, ITree<TOutValue>, new()
            // For this test: TInValue is object, TOutValue is int
            // We need a version of TransformValues that matches these types.
            // The current ValueTransformer.TransformValues has TInIterator : class, IGridIterator<TInValue>
            // So, TInValue is inferred from TInIterator.
            // The current DummyGridIterator<object> means TInValue is object.
            
            ValueTransformer.TransformValues<DummyGridIterator<object>, object, Int32Grid, OpenVDB.Core.Tree.Tree<int>, int>(
                inputIterator, outputGrid, transformFunction, threaded: false);

            Assert.AreEqual(15, outputAccessor.GetValue(new Coord(0, 0, 0)));
            Assert.AreEqual(27, outputAccessor.GetValue(new Coord(1, 1, 1)));
            Assert.AreEqual(-33, outputAccessor.GetValue(new Coord(2, 2, 2)));
            Assert.AreEqual(-1, outputAccessor.GetValue(new Coord(3,3,3))); // Background
        }

        // Accumulator for testing
        private class SumAccumulator
        {
            public float Sum { get; set; } = 0.0f;
            public SumAccumulator Add(float val) { Sum += val; return this; }
            public SumAccumulator Join(SumAccumulator other) { Sum += other.Sum; return this; }
        }

        [Test]
        public void Accumulate_Serial_ShouldCalculateSumCorrectly()
        {
            var data = new List<Tuple<Coord, float, bool>>
            {
                Tuple.Create(new Coord(0,0,0), 1.0f, true),
                Tuple.Create(new Coord(1,0,0), 2.5f, true),
                Tuple.Create(new Coord(0,1,0), 3.5f, true)
            };
            var objectData = data.Select(t => Tuple.Create(t.Item1, (object)t.Item2, t.Item3)).ToList();
            var iterator = new DummyGridIterator<object>(objectData);

            Func<SumAccumulator> seedFactory = () => new SumAccumulator();
            Func<DummyGridIterator<object>, SumAccumulator, SumAccumulator> accumulateOp = (iter, acc) =>
            {
                if (iter.Value is float val) acc.Add(val);
                return acc;
            };
            Func<SumAccumulator, SumAccumulator, SumAccumulator> joinOp = (acc1, acc2) => acc1.Join(acc2);
            Func<SumAccumulator, float> finalizer = acc => acc.Sum;

            // TValue is object for DummyGridIterator<object>
            float totalSum = ValueTransformer.Accumulate<DummyGridIterator<object>, object, SumAccumulator, float>(
                iterator, seedFactory, accumulateOp, joinOp, finalizer, threaded: false);

            Assert.AreEqual(1.0f + 2.5f + 3.5f, totalSum, Epsilon);
        }

        [Test]
        public void SetValueOn_Operations_ShouldModifyAccessorDataCorrectly()
        {
            var accessor = new TestAccessor<float>(-1.0f); // Background -1.0f
            var coord = new Coord(1, 1, 1);

            // Set initial value
            accessor.SetValue(coord, 10.0f);
            Assert.AreEqual(10.0f, accessor.GetValue(coord));

            // Test SetValueOnMin
            ValueTransformer.SetValueOnMin(accessor, coord, 5.0f); // Should change
            Assert.AreEqual(5.0f, accessor.GetValue(coord));
            ValueTransformer.SetValueOnMin(accessor, coord, 15.0f); // Should not change
            Assert.AreEqual(5.0f, accessor.GetValue(coord));

            // Test SetValueOnMax
            ValueTransformer.SetValueOnMax(accessor, coord, 20.0f); // Should change
            Assert.AreEqual(20.0f, accessor.GetValue(coord));
            ValueTransformer.SetValueOnMax(accessor, coord, 10.0f); // Should not change
            Assert.AreEqual(20.0f, accessor.GetValue(coord));

            // Test SetValueOnSum
            ValueTransformer.SetValueOnSum(accessor, coord, 7.0f); // 20 + 7 = 27
            Assert.AreEqual(27.0f, accessor.GetValue(coord));

            // Test SetValueOnMult
            ValueTransformer.SetValueOnMult(accessor, coord, 2.0f); // 27 * 2 = 54
            Assert.AreEqual(54.0f, accessor.GetValue(coord));
        }
    }
}
