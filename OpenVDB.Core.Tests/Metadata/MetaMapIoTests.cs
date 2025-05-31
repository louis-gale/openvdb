// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.Metadata;
using OpenVDB.Core.IO;
using System.IO;
using OpenVDB.Math; // For Vec3DMetadata etc.

namespace OpenVDB.Core.Tests.Metadata
{
    [TestFixture]
    public class MetaMapIoTests
    {
        [Test]
        public void MetaMap_WriteAndRead_ShouldPreserveData()
        {
            var originalMap = new MetaMap();
            originalMap.Insert("myInt", new Int32Metadata(123));
            originalMap.Insert("myString", new StringMetadata("hello VDB"));
            originalMap.Insert("myBool", new BoolMetadata(true));
            originalMap.Insert("myDouble", new DoubleMetadata(123.456));
            originalMap.Insert("myVec3d", new Vec3DMetadata(new Vec3<double>(1,2,3)));

            var streamMeta = new StreamMetadata(); // Default stream metadata

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    originalMap.Write(writer, streamMeta);
                }

                ms.Seek(0, SeekOrigin.Begin);

                var readMap = new MetaMap();
                using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    readMap.Read(reader, streamMeta);
                }

                Assert.AreEqual(originalMap.Count, readMap.Count);
                Assert.AreEqual(originalMap.GetValue<int>("myInt"), readMap.GetValue<int>("myInt"));
                Assert.AreEqual(originalMap.GetValue<string>("myString"), readMap.GetValue<string>("myString"));
                Assert.AreEqual(originalMap.GetValue<bool>("myBool"), readMap.GetValue<bool>("myBool"));
                Assert.AreEqual(originalMap.GetValue<double>("myDouble"), readMap.GetValue<double>("myDouble"));

                var originalVec = originalMap.GetValue<Vec3<double>>("myVec3d");
                var readVec = readMap.GetValue<Vec3<double>>("myVec3d");
                Assert.IsTrue(originalVec.IsApproxEqual(readVec, 1e-9));
            }
        }
    }
}
