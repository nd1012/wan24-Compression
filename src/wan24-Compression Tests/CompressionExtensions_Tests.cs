using wan24.Compression;
using wan24.Compression.Tests;
using wan24.Tests;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class CompressionExtensions_Tests : TestBase
    {
        [TestMethod]
        public void General_Tests()
        {
            byte[] compressed = TestData.Data.Compress();
            Assert.IsTrue(compressed.Length < TestData.Data.Length);
            byte[] uncompressed = compressed.Decompress();
            Assert.IsTrue(TestData.Data.SequenceEqual(uncompressed));
        }
    }
}
