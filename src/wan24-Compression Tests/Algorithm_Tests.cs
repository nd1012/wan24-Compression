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
            foreach (int algo in Algorithms) wan24_Compression_Algorithms_Tests.Algorithm_Tests.Sync_Tests(algo);
        }

        [TestMethod]
        public async Task Async_Tests()
        {
            foreach (int algo in Algorithms) await wan24_Compression_Algorithms_Tests.Algorithm_Tests.Async_Tests(algo);
        }
    }
}
