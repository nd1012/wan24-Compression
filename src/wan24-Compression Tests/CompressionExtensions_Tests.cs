using wan24.Compression;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class CompressionExtensions_Tests
    {
        public static readonly byte[] Data = new byte[81920];

        [TestMethod]
        public void General_Tests()
        {
            byte[] compressed = Data.Compress();
            Assert.IsTrue(compressed.Length < Data.Length);
            byte[] uncompressed = compressed.Decompress();
            Assert.IsTrue(Data.SequenceEqual(uncompressed));
        }
    }
}
