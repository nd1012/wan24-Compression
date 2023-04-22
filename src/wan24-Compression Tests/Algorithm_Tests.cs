using wan24.Compression;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class Algorithm_Tests
    {
        public readonly int[] Algorithms = new int[]
        {
            GZipCompressionAlgorithm.ALGORITHM_VALUE,
            BrotliCompressionAlgorithm.ALGORITHM_VALUE
        };

        [TestMethod]
        public void Sync_Tests()
        {
            foreach (int algo in Algorithms) Sync_Tests(algo);
        }

        [TestMethod]
        public async Task Async_Tests()
        {
            foreach (int algo in Algorithms) await Async_Tests(algo);
        }

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
