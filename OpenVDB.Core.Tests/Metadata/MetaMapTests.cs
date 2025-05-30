// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Metadata;
using System.Linq;

namespace OpenVDB.Core.Tests.Metadata
{
    [TestFixture]
    public class MetaMapTests
    {
        [Test]
        public void MetaMap_DefaultConstructor_IsEmpty()
        {
            var map = new MetaMap();
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void Insert_And_GetMetadata_ShouldWork()
        {
            var map = new MetaMap();
            var intMeta = new Int32Metadata(42);
            map.Insert("myInt", intMeta);

            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.HasMetadata("myInt"));

            var retrievedMeta = map.GetMetadata("myInt");
            Assert.IsNotNull(retrievedMeta);
            Assert.AreNotSame(intMeta, retrievedMeta, "Insert should store a copy.");
            Assert.AreEqual(intMeta.TypeName, retrievedMeta.TypeName);
            Assert.AreEqual(intMeta.Value, (retrievedMeta as Int32Metadata)?.Value);
        }
        
        [Test]
        public void Insert_Generic_And_GetValue_ShouldWork()
        {
            var map = new MetaMap();
            map.Insert("myFloat", 123.45f);

            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.HasMetadata("myFloat"));
            
            float val = map.GetValue<float>("myFloat");
            Assert.AreEqual(123.45f, val, 1e-6f);

            // Test GetValue with default
            float missingVal = map.GetValue<float>("missingFloat", 0.0f);
            Assert.AreEqual(0.0f, missingVal);
            
            Assert.Throws<KeyErrorException>(() => map.GetValue<float>("nonExistentKey"));
            Assert.Throws<TypeErrorException>(() => map.GetValue<int>("myFloat"));
        }
        
        [Test]
        public void Insert_Overwrite_SameType_ShouldUpdateValue()
        {
            var map = new MetaMap();
            map.Insert("myString", "initial");
            map.Insert("myString", "updated");

            Assert.AreEqual(1, map.Count);
            Assert.AreEqual("updated", map.GetValue<string>("myString"));
        }

        [Test]
        public void Insert_Overwrite_DifferentType_ShouldThrowTypeError()
        {
            var map = new MetaMap();
            map.Insert("myMeta", 123); // Int32Metadata
            Assert.Throws<TypeErrorException>(() => map.Insert("myMeta", "new string")); // StringMetadata
        }

        [Test]
        public void RemoveMetadata_ShouldRemoveTheItem()
        {
            var map = new MetaMap();
            map.Insert("temp", 1.0);
            Assert.IsTrue(map.HasMetadata("temp"));
            
            Assert.IsTrue(map.Remove("temp"));
            Assert.IsFalse(map.HasMetadata("temp"));
            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.Remove("temp")); // Removing again should return false
        }

        [Test]
        public void ClearMetadata_ShouldRemoveAllItems()
        {
            var map = new MetaMap();
            map.Insert("item1", 1);
            map.Insert("item2", "text");
            Assert.AreEqual(2, map.Count);

            map.Clear();
            Assert.AreEqual(0, map.Count);
            Assert.IsFalse(map.HasMetadata("item1"));
        }

        [Test]
        public void DeepCopy_Constructor_ShouldCreateIndependentCopy()
        {
            var originalMap = new MetaMap();
            var originalIntMeta = new Int32Metadata(77);
            originalMap.Insert("myInt", originalIntMeta);

            var copiedMap = new MetaMap(originalMap); // Uses copy constructor

            Assert.AreEqual(1, copiedMap.Count);
            Assert.IsTrue(copiedMap.HasMetadata("myInt"));
            Assert.AreEqual(77, copiedMap.GetValue<int>("myInt"));

            // Modify original map and metadata, copied map should not change
            originalMap.Insert("myInt", 100); // Overwrites value in original
            originalMap.Insert("newKey", "newValue");
            
            Assert.AreEqual(77, copiedMap.GetValue<int>("myInt"), "Copied map's value should not change.");
            Assert.IsFalse(copiedMap.HasMetadata("newKey"), "Copied map should not have new key from original.");
            
            // Check that the metadata instances themselves are copies
            var metaFromOriginal = originalMap.GetMetadata("myInt");
            var metaFromCopied = copiedMap.GetMetadata("myInt");
            Assert.AreNotSame(metaFromOriginal, metaFromCopied, "Metadata instances should be distinct copies.");
        }
        
        [Test]
        public void DeepCopy_Method_ShouldCreateIndependentCopy()
        {
            var originalMap = new MetaMap();
            originalMap.Insert("myInt", 77);

            var copiedMap = originalMap.DeepCopy();

            Assert.AreEqual(1, copiedMap.Count);
            Assert.IsTrue(copiedMap.HasMetadata("myInt"));
            Assert.AreEqual(77, copiedMap.GetValue<int>("myInt"));

            originalMap.Insert("myInt", 100);
            Assert.AreEqual(77, copiedMap.GetValue<int>("myInt"));
        }


        [Test]
        public void UnionWith_ShouldMergeMaps()
        {
            var map1 = new MetaMap();
            map1.Insert("int1", 1);
            map1.Insert("string1", "hello");

            var map2 = new MetaMap();
            map2.Insert("int2", 2);
            map2.Insert("string1", "world"); // Overlapping key, same type

            map1.UnionWith(map2);

            Assert.AreEqual(3, map1.Count);
            Assert.AreEqual(1, map1.GetValue<int>("int1"));
            Assert.AreEqual("world", map1.GetValue<string>("string1")); // Value from map2
            Assert.AreEqual(2, map1.GetValue<int>("int2"));
        }
        
        [Test]
        public void UnionWith_TypeMismatch_ShouldThrow()
        {
            var map1 = new MetaMap();
            map1.Insert("sharedKey", 123); // int

            var map2 = new MetaMap();
            map2.Insert("sharedKey", "abc"); // string
            
            Assert.Throws<TypeErrorException>(() => map1.UnionWith(map2));
        }

        [Test]
        public void GetEnumerator_ShouldAllowIteration()
        {
            var map = new MetaMap();
            map.Insert("a", 1);
            map.Insert("b", "two");

            var items = new List<KeyValuePair<string, Metadata>>();
            foreach (var pair in map)
            {
                items.Add(pair);
            }

            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items.Any(kvp => kvp.Key == "a" && (kvp.Value as Int32Metadata)?.Value == 1));
            Assert.IsTrue(items.Any(kvp => kvp.Key == "b" && (kvp.Value as StringMetadata)?.Value == "two"));
        }
        
        [Test]
        public void GetMetadata_Typed_ShouldReturnCorrectTypeOrNull()
        {
            var map = new MetaMap();
            map.Insert("myInt", 10);
            map.Insert("myString", "text");

            var intMeta = map.GetMetadata<Int32Metadata>("myInt");
            Assert.IsNotNull(intMeta);
            Assert.AreEqual(10, intMeta.Value);

            var stringMeta = map.GetMetadata<StringMetadata>("myString");
            Assert.IsNotNull(stringMeta);
            Assert.AreEqual("text", stringMeta.Value);
            
            var floatMeta = map.GetMetadata<FloatMetadata>("myInt"); // Type mismatch
            Assert.IsNull(floatMeta);
            
            var missingMeta = map.GetMetadata<Int32Metadata>("missingKey");
            Assert.IsNull(missingMeta);
        }
    }
}
