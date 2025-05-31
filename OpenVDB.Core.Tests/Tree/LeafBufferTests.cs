// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Tree;
using OpenVDB.Core.Util; // For NodeMask
using System;
using System.IO;
using System.Linq;

namespace OpenVDB.Core.Tests.Tree
{
    [TestFixture]
    public class LeafBufferTests
    {
        private const float EpsilonF = 1e-6f;

        [Test]
        public void Constructor_StartAsUniform_InitializesCorrectly()
        {
            int log2Dim = 3; // 8x8x8 = 512 values
            float initialValue = 123.45f;
            var buffer = new LeafBuffer<float>(log2Dim, initialValue, startAsUniform: true);

            Assert.IsTrue(buffer.IsUniform);
            Assert.AreEqual(initialValue, buffer.UniformValue);
            Assert.IsFalse(buffer.IsAllocated); // Data array should be null
            Assert.AreEqual(log2Dim, buffer.Log2Dim);
            Assert.AreEqual(1 << (log2Dim * 3), buffer.Size);
        }

        [Test]
        public void Constructor_StartAsNonUniform_InitializesCorrectly()
        {
            int log2Dim = 2; // 4x4x4 = 64 values
            float initialValue = 77.0f;
            var buffer = new LeafBuffer<float>(log2Dim, initialValue, startAsUniform: false);

            Assert.IsFalse(buffer.IsUniform);
            Assert.IsTrue(buffer.IsAllocated);
            Assert.AreEqual(log2Dim, buffer.Log2Dim);
            Assert.AreEqual(1 << (log2Dim * 3), buffer.Size);

            for (int i = 0; i < buffer.Size; ++i)
            {
                Assert.AreEqual(initialValue, buffer.GetValue(i), EpsilonF);
            }
        }

        [Test]
        public void Constructor_Log2DimZero_InitializesCorrectly()
        {
            var bufferUniform = new LeafBuffer<int>(0, 10, startAsUniform: true);
            Assert.AreEqual(0, bufferUniform.Log2Dim);
            Assert.AreEqual(1, bufferUniform.Size);
            Assert.IsTrue(bufferUniform.IsUniform);
            Assert.AreEqual(10, bufferUniform.UniformValue);

            var bufferNonUniform = new LeafBuffer<int>(0, 20, startAsUniform: false);
            Assert.AreEqual(0, bufferNonUniform.Log2Dim);
            Assert.AreEqual(1, bufferNonUniform.Size);
            Assert.IsFalse(bufferNonUniform.IsUniform);
            Assert.AreEqual(20, bufferNonUniform.GetValue(0));
        }

        [Test]
        public void GetValue_FromUniformBuffer_ReturnsUniformValue()
        {
            var buffer = new LeafBuffer<double>(2, 1.23, startAsUniform: true);
            for (int i = 0; i < buffer.Size; ++i)
            {
                Assert.AreEqual(1.23, buffer.GetValue(i));
            }
        }

        [Test]
        public void GetValue_FromNonUniformBuffer_ReturnsCorrectValue()
        {
            var buffer = new LeafBuffer<int>(1, 0, startAsUniform: false); // 2x2x2 = 8 values, all 0
            buffer.SetValue(3, 99); // Change one value
            Assert.AreEqual(99, buffer.GetValue(3));
            Assert.AreEqual(0, buffer.GetValue(0));
        }

        [Test]
        public void GetValue_AfterUniformToNonUniformTransition_ReturnsCorrectValue()
        {
            float uniformVal = 5.0f;
            var buffer = new LeafBuffer<float>(1, uniformVal, startAsUniform: true);

            // Setting a value transitions it to non-uniform
            float newValue = 10.0f;
            int indexToChange = 2;
            buffer.SetValue(indexToChange, newValue);

            Assert.AreEqual(newValue, buffer.GetValue(indexToChange), EpsilonF);
            // Other values should still hold the original uniform value
            for(int i=0; i < buffer.Size; ++i)
            {
                if (i == indexToChange) Assert.AreEqual(newValue, buffer.GetValue(i), EpsilonF);
                else Assert.AreEqual(uniformVal, buffer.GetValue(i), EpsilonF);
            }
        }

        [Test]
        public void SetValue_OnUniformBuffer_TransitionsToNonUniformAndSetsValue()
        {
            float uniformVal = 7.0f;
            int log2Dim = 1; // 8 values
            var buffer = new LeafBuffer<float>(log2Dim, uniformVal, startAsUniform: true);

            Assert.IsTrue(buffer.IsUniform);
            Assert.IsFalse(buffer.IsAllocated);

            float newValue = 15.0f;
            int indexToSet = 5;
            buffer.SetValue(indexToSet, newValue);

            Assert.IsFalse(buffer.IsUniform, "Buffer should be non-uniform after SetValue.");
            Assert.IsTrue(buffer.IsAllocated, "Data array should be allocated after SetValue on uniform buffer.");
            Assert.AreEqual(newValue, buffer.GetValue(indexToSet), EpsilonF);

            for (int i = 0; i < buffer.Size; ++i)
            {
                if (i == indexToSet)
                    Assert.AreEqual(newValue, buffer.GetValue(i), EpsilonF);
                else
                    Assert.AreEqual(uniformVal, buffer.GetValue(i), EpsilonF, $"Value at index {i} should be original uniform value.");
            }
        }

        [Test]
        public void SetValue_OnNonUniformBuffer_UpdatesCorrectly()
        {
            var buffer = new LeafBuffer<int>(1, 0, startAsUniform: false); // All zeros
            buffer.SetValue(3, 100);
            buffer.SetValue(0, 200);
            buffer.SetValue(3, 300); // Overwrite

            Assert.AreEqual(200, buffer.GetValue(0));
            Assert.AreEqual(0, buffer.GetValue(1));
            Assert.AreEqual(300, buffer.GetValue(3));
        }

        [Test]
        public void Allocate_OnUniformBuffer_FillsDataWithUniformValueAndBecomesNonUniform()
        {
            float uniformVal = 9.9f;
            var buffer = new LeafBuffer<float>(2, uniformVal, startAsUniform: true);

            Assert.IsTrue(buffer.IsUniform);
            Assert.IsFalse(buffer.IsAllocated);

            buffer.Allocate();

            Assert.IsFalse(buffer.IsUniform, "Buffer should be non-uniform after Allocate if it was uniform.");
            Assert.IsTrue(buffer.IsAllocated);
            for (int i = 0; i < buffer.Size; ++i)
            {
                Assert.AreEqual(uniformVal, buffer.GetValue(i), EpsilonF);
            }
        }

        [Test]
        public void Allocate_OnAlreadyAllocatedBuffer_IsNoOpAndRemainsNonUniform()
        {
            var buffer = new LeafBuffer<int>(1, 0, startAsUniform: false); // Already allocated and non-uniform
            buffer.SetValue(0, 10);

            Assert.IsFalse(buffer.IsUniform);
            Assert.IsTrue(buffer.IsAllocated);

            buffer.Allocate(); // Should be a no-op essentially, or just ensure state

            Assert.IsFalse(buffer.IsUniform); // Should remain non-uniform
            Assert.IsTrue(buffer.IsAllocated);
            Assert.AreEqual(10, buffer.GetValue(0)); // Value should be preserved
        }

        [Test]
        public void Fill_OnNonUniformBuffer_ResetsToUniform()
        {
            var buffer = new LeafBuffer<float>(1, 0.0f, startAsUniform: false);
            buffer.SetValue(0, 1.0f);
            buffer.SetValue(1, 2.0f);
            Assert.IsFalse(buffer.IsUniform);

            float newUniformValue = 99.0f;
            buffer.Fill(newUniformValue);

            Assert.IsTrue(buffer.IsUniform);
            Assert.AreEqual(newUniformValue, buffer.UniformValue);
            Assert.IsFalse(buffer.IsAllocated, "Data array should be released after Fill.");
            Assert.AreEqual(newUniformValue, buffer.GetValue(0), EpsilonF); // GetValue should return new uniform value
        }

        [Test]
        public void Fill_OnUniformBuffer_ChangesUniformValue()
        {
            var buffer = new LeafBuffer<int>(1, 10, startAsUniform: true);
            Assert.IsTrue(buffer.IsUniform);
            Assert.AreEqual(10, buffer.UniformValue);

            buffer.Fill(20);
            Assert.IsTrue(buffer.IsUniform);
            Assert.AreEqual(20, buffer.UniformValue);
            Assert.IsFalse(buffer.IsAllocated);
        }

        // Helper for I/O tests
        private void TestBufferIO<T>(LeafBuffer<T> originalBuffer, NodeMask mask, T backgroundForRead,
                                    bool saveHalf, bool readHalf, float tolerance = EpsilonF) where T : struct
        {
            Assert.AreEqual(originalBuffer.Size, mask.Count, "Mask size must match buffer size for I/O test.");

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalBuffer.Write(writer, mask, saveHalf);
                }

                ms.Seek(0, SeekOrigin.Begin);

                var newBuffer = new LeafBuffer<T>(originalBuffer.Log2Dim, default(T), startAsUniform: true); // Start with default
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newBuffer.Read(reader, mask, backgroundForRead, readHalf);
                }

                Assert.AreEqual(originalBuffer.IsUniform && mask.IsAllOn(), newBuffer.IsUniform, "IsUniform state after I/O mismatch.");
                if (newBuffer.IsUniform)
                {
                    if (typeof(T) == typeof(float))
                        Assert.AreEqual(Convert.ToSingle(originalBuffer.UniformValue), Convert.ToSingle(newBuffer.UniformValue), tolerance);
                    else if (typeof(T) == typeof(double))
                         Assert.AreEqual(Convert.ToDouble(originalBuffer.UniformValue), Convert.ToDouble(newBuffer.UniformValue), tolerance);
                    else
                        Assert.AreEqual(originalBuffer.UniformValue, newBuffer.UniformValue);
                }
                else
                {
                    for (int i = 0; i < originalBuffer.Size; ++i)
                    {
                        T expectedValue = mask.IsOn(i) ? originalBuffer.GetValue(i) : backgroundForRead;
                        if (typeof(T) == typeof(float))
                             Assert.AreEqual(Convert.ToSingle(expectedValue), Convert.ToSingle(newBuffer.GetValue(i)), tolerance, $"Mismatch at index {i}");
                        else if (typeof(T) == typeof(double))
                             Assert.AreEqual(Convert.ToDouble(expectedValue), Convert.ToDouble(newBuffer.GetValue(i)), tolerance, $"Mismatch at index {i}");
                        else
                            Assert.AreEqual(expectedValue, newBuffer.GetValue(i), $"Mismatch at index {i}");
                    }
                }
            }
        }

        [Test]
        public void Io_UniformBuffer_AllOnMask_ShouldPreserveState()
        {
            var buffer = new LeafBuffer<float>(2, 7.5f, startAsUniform: true);
            var mask = new NodeMask(2, true); // All on
            TestBufferIO(buffer, mask, 0.0f, false, false);
        }

        [Test]
        public void Io_NonUniformBuffer_WithMask_ShouldReconstructCorrectly()
        {
            int log2Dim = 1; // 8 values
            var buffer = new LeafBuffer<float>(log2Dim, 0.0f, startAsUniform: false);
            buffer.SetValue(0, 1.0f);
            buffer.SetValue(2, 2.0f);
            buffer.SetValue(5, 5.0f);

            var mask = new NodeMask(log2Dim, false);
            mask.SetOn(0); // Active
            mask.SetOn(2); // Active
            // Index 5 is active in buffer, but mask will say it's off
            // Index 7 is inactive in buffer, mask will say it's off (so background)

            float backgroundForRead = -1.0f;
            TestBufferIO(buffer, mask, backgroundForRead, false, false);
        }

        [Test]
        public void Io_NonUniformBuffer_AllOffMask_ShouldBecomeUniformBackground()
        {
            var buffer = new LeafBuffer<float>(1, 0.0f, startAsUniform: false);
            buffer.SetValue(0, 1.0f); // Some non-uniform data

            var mask = new NodeMask(1, false); // All off
            float backgroundForRead = 99.0f;

            // Perform IO
            LeafBuffer<float> newBuffer;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    buffer.Write(writer, mask, false);
                }
                ms.Seek(0, SeekOrigin.Begin);
                newBuffer = new LeafBuffer<float>(buffer.Log2Dim, default(float), startAsUniform: true);
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newBuffer.Read(reader, mask, backgroundForRead, false);
                }
            }

            Assert.IsTrue(newBuffer.IsUniform, "Buffer should be uniform if mask was all off.");
            Assert.AreEqual(backgroundForRead, newBuffer.UniformValue, EpsilonF);
        }

        [Test]
        public void Io_HalfFloat_UniformBuffer_ShouldApproximatelyPreserveValue()
        {
            var buffer = new LeafBuffer<float>(1, 123.456f, startAsUniform: true);
            var mask = new NodeMask(1, true); // All on

            // Perform IO with half float
            LeafBuffer<float> newBuffer;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    buffer.Write(writer, mask, saveFloatAsHalf: true);
                }
                ms.Seek(0, SeekOrigin.Begin);
                newBuffer = new LeafBuffer<float>(buffer.Log2Dim, default(float), startAsUniform: true);
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newBuffer.Read(reader, mask, 0.0f, readFloatAsHalf: true);
                }
            }

            Assert.IsTrue(newBuffer.IsUniform);
            float originalValue = buffer.UniformValue;
            float readValue = newBuffer.UniformValue;
            float expectedHalfPrecisionValue = (float)(Half)originalValue; // Convert to half and back to float

            Assert.AreEqual(expectedHalfPrecisionValue, readValue, EpsilonF * 100, // Half precision has larger tolerance
                $"Half float I/O failed for uniform buffer. Original: {originalValue}, Read: {readValue}, Expected (via Half): {expectedHalfPrecisionValue}");
        }

        [Test]
        public void Io_HalfFloat_NonUniformBuffer_ShouldApproximatelyPreserveValues()
        {
            var buffer = new LeafBuffer<float>(1, 0.0f, startAsUniform: false);
            float[] originalValues = { 1.23f, -4.56f, 78.9f, 0.0f, 100.123f, -200.45f, 300.0f, 400.7f };
            for(int i=0; i < originalValues.Length; ++i) buffer.SetValue(i, originalValues[i]);

            var mask = new NodeMask(1, true); // All on

            LeafBuffer<float> newBuffer;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    buffer.Write(writer, mask, saveFloatAsHalf: true);
                }
                ms.Seek(0, SeekOrigin.Begin);
                newBuffer = new LeafBuffer<float>(buffer.Log2Dim, default(float), startAsUniform: true);
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newBuffer.Read(reader, mask, 0.0f, readFloatAsHalf: true);
                }
            }

            Assert.IsFalse(newBuffer.IsUniform);
            for(int i=0; i < originalValues.Length; ++i)
            {
                float expectedHalfPrecisionValue = (float)(Half)originalValues[i];
                Assert.AreEqual(expectedHalfPrecisionValue, newBuffer.GetValue(i), EpsilonF * 100, $"Mismatch at index {i}");
            }
        }
    }
}
