using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace wan24.Compression.Tests
{
    public static class AlgorithmTests
    {
        public static void TestAllAlgorithms()
        {
            Assert.IsFalse(CompressionHelper.Algorithms.IsEmpty);
            foreach (CompressionAlgorithmBase algo in CompressionHelper.Algorithms.Values) SyncAlgorithm_Tests(algo.Value);
        }

        public static async Task TestAllAlgorithmsAsync()
        {
            Assert.IsFalse(CompressionHelper.Algorithms.IsEmpty);
            foreach (CompressionAlgorithmBase algo in CompressionHelper.Algorithms.Values) await AsyncAlgorithm_Tests(algo.Value);
        }

        public static void SyncAlgorithm_Tests(int algo)
        {
            Console.WriteLine($"Synchronous algorithm #{algo} tests");
            string name = CompressionHelper.GetAlgorithm(algo).Name;
            // Default
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Algorithm = name;
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
                options.Algorithm = name;
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

        public static async Task AsyncAlgorithm_Tests(int algo)
        {
            Console.WriteLine($"Asynchronous algorithm #{algo} tests");
            string name = CompressionHelper.GetAlgorithm(algo).Name;
            // Default
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Algorithm = name;
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
                options.Algorithm = name;
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
