// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Metadata;
using OpenVDB.Math; // For Vec types

namespace OpenVDB.Core.Tests.Metadata
{
    [TestFixture]
    public class MetadataTests
    {
        [Test]
        public void BoolMetadata_StoresAndRetrievesValue()
        {
            var metaTrue = new BoolMetadata(true);
            var metaFalse = new BoolMetadata(false);

            Assert.AreEqual("bool", metaTrue.TypeName);
            Assert.IsTrue(metaTrue.Value);
            Assert.AreEqual("true", metaTrue.ValueAsString());
            Assert.IsTrue(metaTrue.AsBool());

            Assert.IsFalse(metaFalse.Value);
            Assert.AreEqual("false", metaFalse.ValueAsString());
            Assert.IsFalse(metaFalse.AsBool());
        }

        [Test]
        public void Int32Metadata_StoresAndRetrievesValue()
        {
            var meta = new Int32Metadata(123);
            Assert.AreEqual("int32", meta.TypeName); // Assuming TypeName.GetName<int>() gives "int32"
            Assert.AreEqual(123, meta.Value);
            Assert.AreEqual("123", meta.ValueAsString());
            Assert.IsTrue(meta.AsBool());

            var metaZero = new Int32Metadata(0);
            Assert.IsFalse(metaZero.AsBool());
        }

        [Test]
        public void FloatMetadata_StoresAndRetrievesValue()
        {
            var meta = new FloatMetadata(123.456f);
            Assert.AreEqual("float", meta.TypeName); // Assuming TypeName.GetName<float>() gives "float"
            Assert.AreEqual(123.456f, meta.Value, 1e-6f);
            Assert.AreEqual(123.456f.ToString(), meta.ValueAsString()); // Default float ToString
            Assert.IsTrue(meta.AsBool());

            var metaZero = new FloatMetadata(0.0f);
            Assert.IsFalse(metaZero.AsBool());
        }

        [Test]
        public void StringMetadata_StoresAndRetrievesValue()
        {
            var meta = new StringMetadata("hello openvdb");
            Assert.AreEqual("string", meta.TypeName);
            Assert.AreEqual("hello openvdb", meta.Value);
            Assert.AreEqual("hello openvdb", meta.ValueAsString());
            Assert.IsTrue(meta.AsBool());

            var metaEmpty = new StringMetadata("");
            Assert.IsFalse(metaEmpty.AsBool());
            
            var metaNull = new StringMetadata(null);
            Assert.AreEqual(string.Empty, metaNull.ValueAsString()); // Handled by null check in ValueAsString
            Assert.IsFalse(metaNull.AsBool());

        }

        [Test]
        public void Vec3SMetadata_StoresAndRetrievesValue()
        {
            var vec = new Vec3<float>(1.1f, 2.2f, 3.3f);
            var meta = new Vec3SMetadata(vec); // Vec3<float>
            Assert.AreEqual("Vec3<float>", meta.TypeName); // Check generated name
            Assert.AreEqual(vec, meta.Value);
            Assert.AreEqual(vec.ToString(), meta.ValueAsString());
            Assert.IsTrue(meta.AsBool()); // AsBool for Vec3 might be true if non-zero length or any component non-zero

            var metaZero = new Vec3SMetadata(Vec3<float>.Zero);
            Assert.IsFalse(metaZero.AsBool()); // Assuming Vec3<float>.Zero converts to 0.0 for AsBool
        }
        
        [Test]
        public void Metadata_Copy_ShouldCreateDeepCopyForTypedMetadata()
        {
            var original = new Int32Metadata(42);
            var copied = original.Copy() as Int32Metadata;

            Assert.IsNotNull(copied);
            Assert.AreNotSame(original, copied);
            Assert.AreEqual(original.Value, copied.Value);

            copied.Value = 100;
            Assert.AreEqual(42, original.Value, "Original should not change after modifying copy.");
        }
        
        [Test]
        public void UnknownMetadata_StoresAndRetrievesData()
        {
            byte[] data = { 1, 2, 3, 4 };
            var meta = new UnknownMetadata("MyCustomType", data);

            Assert.AreEqual("MyCustomType", meta.TypeName);
            Assert.AreEqual(data, meta.Value);
            Assert.AreEqual("<binary_data>", meta.ValueAsString());
            Assert.IsTrue(meta.AsBool());

            var copiedMeta = meta.Copy() as UnknownMetadata;
            Assert.IsNotNull(copiedMeta);
            Assert.AreNotSame(meta.Value, copiedMeta.Value); // Should be a copy of the byte array
            CollectionAssert.AreEqual(meta.Value, copiedMeta.Value);
        }
        
        [Test]
        public void Metadata_Equality_ShouldCompareTypeAndValue()
        {
            var meta1_int42 = new Int32Metadata(42);
            var meta2_int42 = new Int32Metadata(42);
            var meta3_int100 = new Int32Metadata(100);
            var meta4_float42 = new FloatMetadata(42f);

            Assert.IsTrue(meta1_int42.Equals(meta2_int42));
            Assert.IsTrue(meta1_int42 == meta2_int42);
            Assert.IsFalse(meta1_int42.Equals(meta3_int100));
            Assert.IsFalse(meta1_int42 == meta3_int100);
            
            // Comparing different types
            Assert.IsFalse(meta1_int42.Equals(meta4_float42));
            // The custom == operator in Metadata base might call TypeName then ValueAsString.
            // This will depend on the exact implementation of Metadata.Equals.
            // If it uses ValueAsString, "42" == "42", but TypeName check should fail first.
            Assert.IsFalse(meta1_int42 == (Metadata)meta4_float42); 
        }
    }
}
