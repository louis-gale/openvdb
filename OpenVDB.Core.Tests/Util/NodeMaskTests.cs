// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Util;
using System;
using System.IO;

namespace OpenVDB.Core.Tests.Util
{
    [TestFixture]
    public class NodeMaskTests
    {
        [Test]
        public void Constructor_InitializesCorrectly()
        {
            var maskAllOff = new NodeMask(3, false); // 8x8x8 = 512 bits
            Assert.AreEqual(3, maskAllOff.Log2Dim);
            Assert.AreEqual(512, maskAllOff.Count);
            Assert.IsTrue(maskAllOff.IsAllOff());
            Assert.IsFalse(maskAllOff.IsAllOn());
            Assert.AreEqual(0, maskAllOff.CountOn());
            Assert.AreEqual(512, maskAllOff.CountOff());

            var maskAllOn = new NodeMask(2, true); // 4x4x4 = 64 bits
            Assert.AreEqual(2, maskAllOn.Log2Dim);
            Assert.AreEqual(64, maskAllOn.Count);
            Assert.IsTrue(maskAllOn.IsAllOn());
            Assert.IsFalse(maskAllOn.IsAllOff());
            Assert.AreEqual(64, maskAllOn.CountOn());
            Assert.AreEqual(0, maskAllOn.CountOff());

            Assert.Throws<ArgumentOutOfRangeException>(() => new NodeMask(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NodeMask(11)); // Current arbitrary upper limit
        }

        [Test]
        public void SetAndGet_IndividualBits_WorkCorrectly()
        {
            var mask = new NodeMask(3); // 512 bits, all off
            Assert.IsFalse(mask.IsOn(0));
            Assert.IsTrue(mask.IsOff(0));

            mask.SetOn(0);
            Assert.IsTrue(mask.IsOn(0));
            Assert.IsFalse(mask.IsOff(0));

            mask.Set(10, true);
            Assert.IsTrue(mask.IsOn(10));

            mask.SetOff(10);
            Assert.IsFalse(mask.IsOn(10));

            Assert.Throws<ArgumentOutOfRangeException>(() => mask.IsOn(512));
            Assert.Throws<ArgumentOutOfRangeException>(() => mask.SetOn(-1));
        }

        [Test]
        public void SetAll_And_CheckAll_WorkCorrectly()
        {
            var mask = new NodeMask(2); // 64 bits
            mask.SetAll(true);
            Assert.IsTrue(mask.IsAllOn());
            Assert.IsFalse(mask.IsAllOff());
            Assert.AreEqual(64, mask.CountOn());

            mask.SetAll(false);
            Assert.IsTrue(mask.IsAllOff());
            Assert.IsFalse(mask.IsAllOn());
            Assert.AreEqual(0, mask.CountOn());
        }

        [Test]
        public void IsConstant_DetectsConstantStates()
        {
            var mask = new NodeMask(2);
            bool state;

            mask.SetAll(true);
            Assert.IsTrue(mask.IsConstant(out state));
            Assert.IsTrue(state);

            mask.SetAll(false);
            Assert.IsTrue(mask.IsConstant(out state));
            Assert.IsFalse(state);

            mask.SetOn(0); // No longer constant
            Assert.IsFalse(mask.IsConstant(out state));

            var emptyMask = new NodeMask(0); // 1 bit
            emptyMask.SetAll(false);
            Assert.IsTrue(emptyMask.IsConstant(out state));
            Assert.IsFalse(state);
        }

        [Test]
        public void CountOnAndCountOff_AreCorrect()
        {
            var mask = new NodeMask(3); // 512 bits
            mask.SetOn(0);
            mask.SetOn(10);
            mask.SetOn(511);
            Assert.AreEqual(3, mask.CountOn());
            Assert.AreEqual(512 - 3, mask.CountOff());

            mask.SetAll(true);
            Assert.AreEqual(512, mask.CountOn());
            Assert.AreEqual(0, mask.CountOff());
        }

        [Test]
        public void WriteAndRead_PreservesMaskState()
        {
            var originalMask = new NodeMask(3);
            originalMask.SetOn(0);
            originalMask.SetOn(15);
            originalMask.SetOn(255);
            originalMask.SetOff(10); // Was false by default, ensure it stays false

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalMask.Write(writer);
                }

                ms.Seek(0, SeekOrigin.Begin);

                // For Read, the NodeMask must be constructed with the same Log2Dim
                var newMask = new NodeMask(3); 
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newMask.Read(reader);
                }

                Assert.AreEqual(originalMask.Count, newMask.Count);
                Assert.AreEqual(originalMask.Log2Dim, newMask.Log2Dim);
                Assert.AreEqual(originalMask.CountOn(), newMask.CountOn());
                for (int i = 0; i < originalMask.Count; ++i)
                {
                    Assert.AreEqual(originalMask.IsOn(i), newMask.IsOn(i), $"Bit at index {i} differs.");
                }
            }
        }
        
        [Test]
        public void WriteAndRead_EmptyMask_DoesNotThrow()
        {
            var originalMask = new NodeMask(0); // 1 bit total
            originalMask.SetAll(false);

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalMask.Write(writer);
                }
                ms.Seek(0, SeekOrigin.Begin);
                var newMask = new NodeMask(0);
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    newMask.Read(reader);
                }
                Assert.IsTrue(newMask.IsAllOff());
            }
        }


        [Test]
        public void BitwiseOperations_WorkAsExpected()
        {
            var maskA = new NodeMask(2); // 4x4x4 = 64 bits
            var maskB = new NodeMask(2);

            maskA.SetOn(0); maskA.SetOn(1); // A = 1100...
            maskB.SetOn(1); maskB.SetOn(2); // B = 0110...

            // AND
            var maskAnd = maskA & maskB; // Expected: 0100...
            Assert.IsFalse(maskAnd.IsOn(0));
            Assert.IsTrue(maskAnd.IsOn(1));
            Assert.IsFalse(maskAnd.IsOn(2));
            Assert.AreEqual(1, maskAnd.CountOn());

            // OR
            var maskOr = maskA | maskB; // Expected: 1110...
            Assert.IsTrue(maskOr.IsOn(0));
            Assert.IsTrue(maskOr.IsOn(1));
            Assert.IsTrue(maskOr.IsOn(2));
            Assert.AreEqual(3, maskOr.CountOn());

            // XOR
            var maskXor = maskA ^ maskB; // Expected: 1010...
            Assert.IsTrue(maskXor.IsOn(0));
            Assert.IsFalse(maskXor.IsOn(1));
            Assert.IsTrue(maskXor.IsOn(2));
            Assert.AreEqual(2, maskXor.CountOn());

            // NOT
            var maskNotA = ~maskA; // Expected: 0011... (first 4 bits if A was 1100)
            Assert.IsFalse(maskNotA.IsOn(0));
            Assert.IsFalse(maskNotA.IsOn(1));
            Assert.IsTrue(maskNotA.IsOn(2)); // Assuming A had bit 2 off
            Assert.IsTrue(maskNotA.IsOn(3)); // Assuming A had bit 3 off
            Assert.AreEqual(maskA.Count - maskA.CountOn(), maskNotA.CountOn());
        }

        [Test]
        public void BitwiseOperations_DimensionMismatch_ThrowsArgumentException()
        {
            var maskA = new NodeMask(2);
            var maskC = new NodeMask(3);
            Assert.Throws<ArgumentException>(() => { var x = maskA & maskC; });
            Assert.Throws<ArgumentException>(() => { var x = maskA | maskC; });
            Assert.Throws<ArgumentException>(() => { var x = maskA ^ maskC; });
        }
        
        [Test]
        public void CopyAndClone_CreateIndependentMasks()
        {
            var originalMask = new NodeMask(2, true);
            originalMask.SetOff(5);

            var copiedMask = originalMask.Copy();
            var clonedMask = (NodeMask)originalMask.Clone();

            Assert.AreNotSame(originalMask, copiedMask);
            Assert.AreNotSame(originalMask, clonedMask);
            Assert.AreEqual(originalMask.CountOn(), copiedMask.CountOn());
            Assert.AreEqual(originalMask.CountOn(), clonedMask.CountOn());
            Assert.IsTrue(copiedMask.IsOn(0)); Assert.IsFalse(copiedMask.IsOn(5));
            Assert.IsTrue(clonedMask.IsOn(0)); Assert.IsFalse(clonedMask.IsOn(5));

            // Modify original, copies should not change
            originalMask.SetOn(5);
            originalMask.SetOff(0);

            Assert.AreEqual(originalMask.Count -1, copiedMask.CountOn()); // Copied should still have 5 off
            Assert.IsTrue(copiedMask.IsOn(0)); 
            Assert.IsFalse(copiedMask.IsOn(5));
        }
    }
}
