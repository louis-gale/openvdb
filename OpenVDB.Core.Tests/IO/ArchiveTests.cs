// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using OpenVDB.Core.IO;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenVDB.Core.Tests.IO
{
    [TestFixture]
    public class ArchiveTests
    {
        [Test]
        public void Archive_DefaultConstructor_SetsDefaultValues()
        {
            var archive = new Archive();

            Assert.AreEqual(IoConstants.CurrentFileVersion, archive.FileVersion);
            Assert.AreEqual(OpenVDB.Version.MajorVersion, archive.LibraryVersionMajor);
            Assert.AreEqual(OpenVDB.Version.MinorVersion, archive.LibraryVersionMinor);
            Assert.IsNotNull(archive.UUID);
            Assert.AreEqual(36, archive.UUID.Length); // Standard GUID string length
            Assert.IsTrue(archive.IsInstancingEnabled);
            Assert.AreEqual(CompressionUtil.DefaultCompressionFlags, archive.Compression);
            Assert.IsTrue(archive.IsGridStatsMetadataEnabled);
        }

        [Test]
        public void Archive_CopyConstructor_CopiesSettings()
        {
            var original = new Archive();
            original.IsInstancingEnabled = false;
            original.Compression = CompressionFlags.Zip | CompressionFlags.ActiveMask;
            original.IsGridStatsMetadataEnabled = false;
            // original.SetUUID("test-uuid"); // Assuming a method to set UUID for testing if needed, or check if UUID is copied.

            var copy = new Archive(original);

            Assert.AreEqual(original.FileVersion, copy.FileVersion);
            Assert.AreEqual(original.LibraryVersionMajor, copy.LibraryVersionMajor);
            Assert.AreEqual(original.LibraryVersionMinor, copy.LibraryVersionMinor);
            Assert.AreEqual(original.UUID, copy.UUID); // UUID should be copied
            Assert.AreEqual(original.IsInstancingEnabled, copy.IsInstancingEnabled);
            Assert.AreEqual(original.Compression, copy.Compression);
            Assert.AreEqual(original.IsGridStatsMetadataEnabled, copy.IsGridStatsMetadataEnabled);
        }

        [Test]
        public void Archive_Clone_CreatesIndependentCopy()
        {
            var original = new Archive();
            original.Compression = CompressionFlags.None;

            var clone = original.Clone();
            clone.Compression = CompressionFlags.Zip; // Modify clone

            Assert.AreEqual(CompressionFlags.None, original.Compression, "Original should not be modified by clone.");
            Assert.AreEqual(CompressionFlags.Zip, clone.Compression, "Clone should have the modified value.");
            Assert.AreEqual(original.UUID, clone.UUID, "UUID should be the same for a settings clone.");
        }

        [Test]
        public void Archive_Properties_SetAndGetCorrectly()
        {
            var archive = new Archive();

            archive.IsInstancingEnabled = false;
            Assert.IsFalse(archive.IsInstancingEnabled);

            archive.Compression = CompressionFlags.Zip;
            Assert.AreEqual(CompressionFlags.Zip, archive.Compression);

            archive.IsGridStatsMetadataEnabled = false;
            Assert.IsFalse(archive.IsGridStatsMetadataEnabled);
        }

        [Test]
        public void WriteHeader_And_ReadHeader_ShouldBeConsistent()
        {
            var archiveWrite = new Archive();
            // archiveWrite.SetUUID("test-uuid-for-header"); // If there was a setter for predictable UUID
            string originalUUID = archiveWrite.UUID; // Get the generated UUID

            using (var ms = new MemoryStream())
            {
                // Write header
                archiveWrite.WriteHeader(ms, gridCount: 0, hasGridOffsets: true);
                long headerEndPos = ms.Position;

                Assert.AreEqual(IoConstants.BaseHeaderSize, headerEndPos, "Header size mismatch.");

                ms.Seek(0, SeekOrigin.Begin);

                // Read header
                var archiveRead = new Archive();
                archiveRead.ReadHeader(ms);

                Assert.AreEqual(IoConstants.CurrentFileVersion, archiveRead.FileVersion);
                Assert.AreEqual(OpenVDB.Version.MajorVersion, archiveRead.LibraryVersionMajor);
                Assert.AreEqual(OpenVDB.Version.MinorVersion, archiveRead.LibraryVersionMinor);
                Assert.AreEqual(originalUUID, archiveRead.UUID);
                // Assert.IsTrue(archiveRead.InputHasGridOffsets); // Accessing protected member for test
                // This can be tested by checking behavior of ReadGridDescriptors if it uses this flag.
                // For now, just ensure ReadHeader doesn't throw and reads expected length.
            }
        }

        [Test]
        public void WriteHeader_CorrectMagicNumberAndVersions()
        {
            using (var ms = new MemoryStream())
            {
                var archive = new Archive();
                archive.WriteHeader(ms, 0, false);
                ms.Seek(0, SeekOrigin.Begin);

                var reader = new BinaryReader(ms, Encoding.ASCII);
                Assert.AreEqual(IoConstants.Magic0, reader.ReadByte());
                Assert.AreEqual(IoConstants.Magic1, reader.ReadByte());
                Assert.AreEqual(IoConstants.Magic2, reader.ReadByte());
                Assert.AreEqual(IoConstants.Magic3, reader.ReadByte());
                Assert.AreEqual(IoConstants.CurrentFileVersion, reader.ReadUInt32());
                Assert.AreEqual(OpenVDB.Version.MajorVersion, reader.ReadUInt16());
                Assert.AreEqual(OpenVDB.Version.MinorVersion, reader.ReadUInt16());
            }
        }

        [Test]
        public void ReadHeader_InvalidMagicNumber_ShouldThrowIOException()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
                {
                    writer.Write((byte)'B'); // Invalid magic
                    writer.Write((byte)'A');
                    writer.Write((byte)'D');
                    writer.Write((byte)'!');
                }
                ms.Seek(0, SeekOrigin.Begin);

                var archive = new Archive();
                Assert.Throws<IOException>(() => archive.ReadHeader(ms));
            }
        }

        [Test]
        public void VersionString_FormatsCorrectly()
        {
            var archive = new Archive();
            string expected = $"{OpenVDB.Version.MajorVersion}.{OpenVDB.Version.MinorVersion}/{IoConstants.CurrentFileVersion}";
            Assert.AreEqual(expected, archive.VersionStringRead);
        }

        private GridDescriptor CreateTestGridDescriptor(string name, string type, long metaOffset, long blocksOffset)
        {
            return new GridDescriptor(name, type)
            {
                MetaDataOffset = metaOffset,
                BlocksOffset = blocksOffset,
                // Populate other fields as necessary for robust testing
            };
        }

        [Test]
        public void ReadHeader_WithGridDescriptors_ShouldPopulateDescriptorsList()
        {
            var archiveWrite = new Archive();
            var desc1 = CreateTestGridDescriptor("Grid1", "FloatGrid", 1000, 2000);
            var desc2 = CreateTestGridDescriptor("Grid2", "DoubleGrid", 3000, 4000);
            var descriptors = new List<GridDescriptor> { desc1, desc2 };

            var streamMetaWrite = new StreamMetadata { FileVersion = IoConstants.CurrentFileVersion };

            using (var ms = new MemoryStream())
            {
                // Write header indicating grid offsets and count
                archiveWrite.WriteHeader(ms, descriptors.Count, hasGridOffsets: true);
                // Write placeholder offsets (as actual offsets are complex to precalculate here)
                // The ReadHeader in Archive currently reads these offsets but doesn't use them to jump.
                // It expects descriptors to follow sequentially if _inputHasGridOffsets is true and ReadGridDescriptors is called by it.
                // So, we directly write descriptors after the grid count.
                // This part of WriteHeader (writing offsets) is simplified in current Archive.cs.
                // For this test, we focus on ReadHeader reading the count and then ReadGridDescriptors.

                // Write descriptors sequentially
                archiveWrite.WriteGridDescriptors(ms, descriptors, streamMetaWrite);

                ms.Seek(0, SeekOrigin.Begin);

                var archiveRead = new Archive();
                // ReadHeader will read the main header, then if hasGridOffsets is true,
                // it reads gridCount and calls its internal ReadGridDescriptors.
                archiveRead.ReadHeader(ms);

                Assert.IsNotNull(archiveRead.GridDescriptors);
                Assert.AreEqual(descriptors.Count, archiveRead.GridDescriptors.Count);

                Assert.AreEqual(desc1.Name, archiveRead.GridDescriptors[0].Name);
                Assert.AreEqual(desc1.GridType, archiveRead.GridDescriptors[0].GridType);
                Assert.AreEqual(desc1.MetaDataOffset, archiveRead.GridDescriptors[0].MetaDataOffset);

                Assert.AreEqual(desc2.Name, archiveRead.GridDescriptors[1].Name);
                Assert.AreEqual(desc2.GridType, archiveRead.GridDescriptors[1].GridType);
                Assert.AreEqual(desc2.BlocksOffset, archiveRead.GridDescriptors[1].BlocksOffset);
            }
        }

        [Test]
        public void WriteHeaderAndDescriptors_ThenReadHeader_ShouldBeConsistent()
        {
            var archiveWrite = new Archive();
            var desc1 = CreateTestGridDescriptor("GridA", "Int32Grid", 500, 1500);
            var desc2 = CreateTestGridDescriptor("GridB", "Vec3SGrid", 2500, 3500);
            var descriptorsToWrite = new List<GridDescriptor> { desc1, desc2 };

            var streamMetaWrite = new StreamMetadata { FileVersion = IoConstants.CurrentFileVersion };

            using (var ms = new MemoryStream())
            {
                // 1. Write Header
                archiveWrite.WriteHeader(ms, descriptorsToWrite.Count, hasGridOffsets: true);
                // (WriteHeader also writes placeholder grid offsets if hasGridOffsets is true)

                // 2. Write Grid Descriptors
                archiveWrite.WriteGridDescriptors(ms, descriptorsToWrite, streamMetaWrite);

                ms.Seek(0, SeekOrigin.Begin);

                // 3. Read back using a new Archive instance
                var archiveRead = new Archive();
                archiveRead.ReadHeader(ms); // This should read header and then descriptors

                Assert.IsNotNull(archiveRead.GridDescriptors);
                Assert.AreEqual(descriptorsToWrite.Count, archiveRead.GridDescriptors.Count);

                for (int i = 0; i < descriptorsToWrite.Count; i++)
                {
                    // GridDescriptor doesn't have a full Equals method, compare key fields
                    Assert.AreEqual(descriptorsToWrite[i].Name, archiveRead.GridDescriptors[i].Name);
                    Assert.AreEqual(descriptorsToWrite[i].GridType, archiveRead.GridDescriptors[i].GridType);
                    Assert.AreEqual(descriptorsToWrite[i].MetaDataOffset, archiveRead.GridDescriptors[i].MetaDataOffset);
                    Assert.AreEqual(descriptorsToWrite[i].BlocksOffset, archiveRead.GridDescriptors[i].BlocksOffset);
                    // Add more assertions for other fields if necessary
                }
            }
        }

        [Test]
        public void ArchiveWrite_PreparesDescriptorsCorrectly()
        {
            var archive = new Archive();
            var grid1 = new FloatGrid { Name = "Temperature" };
            grid1.GridClass = GridClass.FogVolume;
            var grid2 = new DoubleGrid { Name = "Density" }; // Assuming DoubleGrid exists and is registered
            grid2.GridClass = GridClass.LevelSet;

            // Ensure DoubleGrid is registered for this test if GridBase.CreateGrid is used by Archive.Write internally
            // For now, Archive.Write mainly prepares descriptors from the passed grids.
            // It doesn't yet write full grid data.

            var grids = new List<GridBase> { grid1, grid2 };

            using (var ms = new MemoryStream())
            {
                archive.Write(ms, grids, null); // globalMetadata = null

                ms.Seek(0, SeekOrigin.Begin);

                var archiveRead = new Archive();
                archiveRead.ReadHeader(ms); // Reads header and descriptors

                Assert.AreEqual(grids.Count, archiveRead.GridDescriptors.Count);

                var desc1 = archiveRead.GridDescriptors.FirstOrDefault(d => d.Name == "Temperature");
                Assert.IsNotNull(desc1);
                Assert.AreEqual(grid1.GridTypeName, desc1.GridType); // GridTypeName is from tree usually
                Assert.AreEqual(grid1.GridClass, desc1.GridClass);

                var desc2 = archiveRead.GridDescriptors.FirstOrDefault(d => d.Name == "Density");
                Assert.IsNotNull(desc2);
                Assert.AreEqual(grid2.GridTypeName, desc2.GridType);
                Assert.AreEqual(grid2.GridClass, desc2.GridClass);

                // Offsets in descriptors will be placeholders (likely 0) as grid data writing is not implemented
                Assert.AreEqual(0, desc1.MetaDataOffset);
            }
        }
    }
}
