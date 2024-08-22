using wan24.Compression.Tests;
using wan24.Tests;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class Algorithm_Tests : TestBase
    {
        [TestMethod]
        public void Sync_Tests()
        {
            AlgorithmTests.TestAllAlgorithms();
        }

        [TestMethod]
        public async Task Async_Tests()
        {
            await AlgorithmTests.TestAllAlgorithmsAsync();
        }
    }
}
