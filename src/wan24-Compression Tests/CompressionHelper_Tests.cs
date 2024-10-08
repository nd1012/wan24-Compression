﻿using wan24.Compression;
using wan24.Compression.Tests;
using wan24.Tests;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class CompressionHelper_Tests : TestBase
    {
        [TestMethod]
        public void Sync_Tests()
        {
            // Default
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.LeaveOpen = true;
                using MemoryStream data = new(TestData.Data);
                using MemoryStream compressed = new();
                data.Compress(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                compressed.Decompress(uncompressed, options);
                Assert.IsTrue(TestData.Data.SequenceEqual(uncompressed.ToArray()));
            }
            // Nothing included
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Flags = CompressionFlags.None;
                options.LeaveOpen = true;
                using MemoryStream data = new(TestData.Data);
                using MemoryStream compressed = new();
                data.Compress(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                compressed.Decompress(uncompressed, options);
                Assert.IsTrue(TestData.Data.SequenceEqual(uncompressed.ToArray()));
            }
        }

        [TestMethod]
        public async Task Async_Tests()
        {
            // Default
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.LeaveOpen = true;
                using MemoryStream data = new(TestData.Data);
                using MemoryStream compressed = new();
                await data.CompressAsync(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                await compressed.DecompressAsync(uncompressed, options);
                Assert.IsTrue(TestData.Data.SequenceEqual(uncompressed.ToArray()));
            }
            // Nothing included
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Flags = CompressionFlags.None;
                options.LeaveOpen = true;
                using MemoryStream data = new(TestData.Data);
                using MemoryStream compressed = new();
                await data.CompressAsync(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                await compressed.DecompressAsync(uncompressed, options);
                Assert.IsTrue(TestData.Data.SequenceEqual(uncompressed.ToArray()));
            }
        }
    }
}
