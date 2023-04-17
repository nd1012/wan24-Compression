using Microsoft.VisualStudio.TestTools.UnitTesting;
using wan24.Compression;

namespace wan24_Compression_Algorithms_Tests
{
    public class Algorithm_Tests
    {
        public static readonly byte[] Data = new byte[81920];

        public static void Sync_Tests(int algo)
        {
            string name = CompressionHelper.GetAlgorithmName(algo);
            // Default
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Algorithm = name;
                options.LeaveOpen = true;
                using MemoryStream data = new(Data);
                using MemoryStream compressed = new();
                data.Compress(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                compressed.Decompress(uncompressed, options);
                Assert.IsTrue(Data.SequenceEqual(uncompressed.ToArray()));
            }
            // Nothing included
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Algorithm = name;
                options.Flags = CompressionFlags.None;
                options.LeaveOpen = true;
                using MemoryStream data = new(Data);
                using MemoryStream compressed = new();
                data.Compress(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                compressed.Decompress(uncompressed, options);
                Assert.IsTrue(Data.SequenceEqual(uncompressed.ToArray()));
            }
        }

        public static async Task Async_Tests(int algo)
        {
            string name = CompressionHelper.GetAlgorithmName(algo);
            // Default
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Algorithm = name;
                options.LeaveOpen = true;
                using MemoryStream data = new(Data);
                using MemoryStream compressed = new();
                await data.CompressAsync(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                await compressed.DecompressAsync(uncompressed, options);
                Assert.IsTrue(Data.SequenceEqual(uncompressed.ToArray()));
            }
            // Nothing included
            {
                CompressionOptions options = CompressionHelper.GetDefaultOptions();
                options.Algorithm = name;
                options.Flags = CompressionFlags.None;
                options.LeaveOpen = true;
                using MemoryStream data = new(Data);
                using MemoryStream compressed = new();
                await data.CompressAsync(compressed, options);
                Assert.IsTrue(compressed.Length < data.Length);
                compressed.Position = 0;
                using MemoryStream uncompressed = new();
                await compressed.DecompressAsync(uncompressed, options);
                Assert.IsTrue(Data.SequenceEqual(uncompressed.ToArray()));
            }
        }
    }
}
