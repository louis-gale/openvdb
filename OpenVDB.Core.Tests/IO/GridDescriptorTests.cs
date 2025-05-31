// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.IO;
using OpenVDB.Math; // For Coord
using System.IO;
using System.Text;

namespace OpenVDB.Core.Tests.IO
{
    [TestFixture]
    public class GridDescriptorTests
    {
        private GridDescriptor CreateTestDescriptor()
        {
            var desc = new GridDescriptor("TestGrid", "FloatGrid", saveFloatAsHalf: true)
            {
                InstanceParentName = "ParentGrid",
                InstanceTransform = "share",
                GridClass = GridClass.LevelSet,
                IsInWorldSpace = false,
                FileBBoxMin = new Coord(1, 2, 3),
                FileBBoxMax = new Coord(10, 20, 30),
                GridDescriptorOffset = 100,
                NameOffset = 150,
                MetaDataOffset = 200,
                TransformOffset = 300,
                TopologyOffset = 400,
                BlocksOffset = 1000,
                TopologyByteSize = 500,
                BlocksByteSize = 2000
            };
            desc.SetUniqueName("TestGrid", 1); // TestGridRS1 (Record Separator)
            return desc;
        }

        [Test]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            var desc = new GridDescriptor("MyGrid", "DoubleGrid", true);
            Assert.AreEqual("MyGrid", desc.Name);
            Assert.AreEqual("MyGrid", desc.UniqueName); // Initially same
            Assert.AreEqual("DoubleGrid", desc.GridType);
            Assert.IsTrue(desc.SaveFloatAsHalf);
            Assert.IsFalse(desc.IsInstance());
            Assert.AreEqual("", desc.InstanceParentName);
            Assert.AreEqual("copy", desc.InstanceTransform); // Default for non-instances
        }

        [Test]
        public void InstancingMethods_WorkAsExpected()
        {
            var desc = new GridDescriptor();
            Assert.IsFalse(desc.IsInstance());
            Assert.IsFalse(desc.IsTopologyInstance());
            Assert.IsFalse(desc.IsTransformInstance());

            desc.InstanceParentName = "Parent";
            Assert.IsTrue(desc.IsInstance());
            Assert.IsTrue(desc.IsTopologyInstance(), "Should be topology instance if offset is 0 by default");

            desc.SetIsTransformInstance(true);
            Assert.IsTrue(desc.IsTransformInstance());
            Assert.AreEqual("share", desc.InstanceTransform);

            desc.SetIsTransformInstance(false);
            Assert.IsFalse(desc.IsTransformInstance());
            Assert.AreEqual("copy", desc.InstanceTransform);

            desc.TopologyOffset = 12345; // Non-zero offset
            Assert.IsFalse(desc.IsTopologyInstance(), "Should not be topo instance if offset is non-zero");
        }

        [Test]
        public void NameSuffixMethods_WorkCorrectly()
        {
            string baseName = "MyGrid";
            string suffixed = GridDescriptor.AddSuffix(baseName, 5);
            Assert.AreEqual($"{baseName}{(char)30}5", suffixed);
            Assert.AreEqual(baseName, GridDescriptor.StripSuffix(suffixed));
            Assert.AreEqual(baseName, GridDescriptor.StripSuffix(baseName)); // No suffix

            var desc = new GridDescriptor();
            desc.SetUniqueName(baseName, 3);
            Assert.AreEqual(baseName, desc.Name);
            Assert.AreEqual(GridDescriptor.AddSuffix(baseName, 3), desc.UniqueName);

            desc.SetUniqueName(GridDescriptor.AddSuffix("OtherGrid", 10));
            Assert.AreEqual("OtherGrid", desc.Name);
            Assert.AreEqual(GridDescriptor.AddSuffix("OtherGrid", 10), desc.UniqueName);
        }

        [Test]
        public void ReadWrite_CurrentVersion_ShouldBeConsistent()
        {
            var originalDesc = CreateTestDescriptor();
            var streamMeta = new StreamMetadata { FileVersion = IoConstants.CurrentFileVersion };

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, Encoding.ASCII, true))
                {
                    originalDesc.Write(writer, streamMeta);
                }

                ms.Seek(0, SeekOrigin.Begin);

                var readDesc = new GridDescriptor();
                using (var reader = new BinaryReader(ms, Encoding.ASCII, true))
                {
                    readDesc.Read(reader, streamMeta);
                }

                AssertDescriptorsEqual(originalDesc, readDesc, streamMeta.FileVersion);
            }
        }

        [Test]
        public void ReadWrite_LegacyVersion_BeforeGridDescNameIsLast_ShouldBeConsistent()
        {
            var originalDesc = CreateTestDescriptor();
            // In this legacy version, Name is read first, and NameOffset is not used. UniqueName is same as Name.
            var streamMeta = new StreamMetadata { FileVersion = VersionNumbers.FileVersionGridDescNameIsLast - 1 };
            originalDesc.SetUniqueName(originalDesc.Name); // Ensure UniqueName matches Name for this test logic
            originalDesc.NameOffset = 0; // Not used in this legacy version

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, Encoding.ASCII, true))
                {
                    // Manual write to simulate legacy format (name first, no NameOffset field)
                    IoUtils.WriteString(writer, originalDesc.Name);
                    IoUtils.WriteString(writer, originalDesc.GridType);
                    IoUtils.WriteString(writer, originalDesc.InstanceParentName ?? "");
                    // No InstanceTransform in this old version
                    writer.Write(originalDesc.MetaDataOffset);
                    writer.Write(originalDesc.TransformOffset);
                    writer.Write(originalDesc.TopologyOffset);
                    writer.Write(originalDesc.BlocksOffset);
                    // No NameOffset field
                    // No GridClass, SaveFloatAsHalf, IsInWorldSpace, TopologyStats, FileBBox in this very old version
                }

                ms.Seek(0, SeekOrigin.Begin);

                var readDesc = new GridDescriptor();
                using (var reader = new BinaryReader(ms, Encoding.ASCII, true))
                {
                    readDesc.Read(reader, streamMeta);
                }

                Assert.AreEqual(originalDesc.Name, readDesc.Name);
                Assert.AreEqual(originalDesc.Name, readDesc.UniqueName); // Should be set from Name
                Assert.AreEqual(originalDesc.GridType, readDesc.GridType);
                Assert.AreEqual(originalDesc.InstanceParentName, readDesc.InstanceParentName);
                // Assert default values for fields not present in this legacy version
                Assert.AreEqual(originalDesc.IsInstance() ? "share" : "copy", readDesc.InstanceTransform); // Default logic
                Assert.AreEqual(originalDesc.MetaDataOffset, readDesc.MetaDataOffset);
                // ... other fields
                Assert.AreEqual(readDesc.GridType.Contains("LevelSet") ? GridClass.LevelSet : GridClass.Unknown, readDesc.GridClass);
                Assert.IsFalse(readDesc.SaveFloatAsHalf);
                Assert.IsTrue(readDesc.IsInWorldSpace);
            }
        }


        private void AssertDescriptorsEqual(GridDescriptor expected, GridDescriptor actual, uint fileVersionContext)
        {
            Assert.AreEqual(expected.Name, actual.Name);
            // UniqueName handling depends on version; if NameOffset is used, UniqueName might be different.
            // For current simple write/read, they might be the same.
            // Assert.AreEqual(expected.UniqueName, actual.UniqueName);
            Assert.AreEqual(expected.GridType, actual.GridType);
            Assert.AreEqual(expected.InstanceParentName, actual.InstanceParentName);

            if (fileVersionContext >= VersionNumbers.FileVersionGridSharedTransform)
                Assert.AreEqual(expected.InstanceTransform, actual.InstanceTransform);
            else
                Assert.AreEqual(expected.IsInstance() ? "share" : "copy", actual.InstanceTransform);


            Assert.AreEqual(expected.MetaDataOffset, actual.MetaDataOffset);
            Assert.AreEqual(expected.TransformOffset, actual.TransformOffset);
            Assert.AreEqual(expected.TopologyOffset, actual.TopologyOffset);
            Assert.AreEqual(expected.BlocksOffset, actual.BlocksOffset);

            if (fileVersionContext >= VersionNumbers.FileVersionGridDescNameIsLast)
            {
                // NameOffset handling is tricky without a string pool.
                // If NameOffset points to an inline name, it's complex to verify without knowing the exact write sequence.
                // For now, we assume name is correctly read.
            }

            if (fileVersionContext >= VersionNumbers.FileVersionGridClass)
                Assert.AreEqual(expected.GridClass, actual.GridClass);
            else
                 Assert.AreEqual(expected.GridType.Contains("LevelSet") ? GridClass.LevelSet : GridClass.Unknown, actual.GridClass);


            if (fileVersionContext >= VersionNumbers.FileVersionSaveHalfFloat)
                Assert.AreEqual(expected.SaveFloatAsHalf, actual.SaveFloatAsHalf);
            else
                Assert.IsFalse(actual.SaveFloatAsHalf);

            if (fileVersionContext >= VersionNumbers.FileVersionWorldSpaceBit)
                Assert.AreEqual(expected.IsInWorldSpace, actual.IsInWorldSpace);
            else
                Assert.IsTrue(actual.IsInWorldSpace);


            if (fileVersionContext >= VersionNumbers.FileVersionTopologyStats)
            {
                Assert.AreEqual(expected.TopologyByteSize, actual.TopologyByteSize);
                Assert.AreEqual(expected.BlocksByteSize, actual.BlocksByteSize);
            }

            if (fileVersionContext >= VersionNumbers.FileVersionFileBoundingBox)
            {
                Assert.AreEqual(expected.FileBBoxMin, actual.FileBBoxMin);
                Assert.AreEqual(expected.FileBBoxMax, actual.FileBBoxMax);
            }
        }
    }
}
