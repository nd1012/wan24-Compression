using System.IO.Compression;
using wan24.Compression;
using wan24.StreamSerializerExtensions;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class CompressionOptions_Tests
    {
        [TestMethod]
        public void General_Tests()
        {
            // Reference options
            CompressionOptions options = new()
            {
                Algorithm = BrotliCompressionAlgorithm.ALGORITHM_NAME,
                AlgorithmIncluded = true,
                SerializerVersion = StreamSerializer.VERSION,
                SerializerVersionIncluded = true,
                UncompressedDataLength = 123,
                UncompressedLengthIncluded = true,
                Level = CompressionLevel.SmallestSize,
                FlagsIncluded = true,
                LeaveOpen = true
            };
            // Cloning
            CompareOptions(options, options.Clone(), false);
            // Casting
            byte[] optionsData = (byte[])options;
            CompareOptions(options, (CompressionOptions)optionsData);
            // Synchronous serialization
            using MemoryStream ms = new();
            ms.WriteSerialized(options);
            ms.Position = 0;
            CompareOptions(options, ms.ReadSerialized<CompressionOptions>());
            // Asynchronous serialization
            ms.SetLength(0);
            ms.WriteSerializedAsync(options).Wait();
            ms.Position = 0;
            CompareOptions(options, ms.ReadSerializedAsync<CompressionOptions>().Result);
        }

        public void CompareOptions(CompressionOptions a, CompressionOptions b, bool serialized = true)
        {
            Assert.AreEqual(a.FlagsIncluded, b.FlagsIncluded);
            Assert.AreEqual(a.AlgorithmIncluded, b.AlgorithmIncluded);
            Assert.AreEqual(a.Algorithm, b.Algorithm);
            Assert.AreEqual(a.SerializerVersionIncluded, b.SerializerVersionIncluded);
            Assert.AreEqual(a.SerializerVersion, b.SerializerVersion);
            if (serialized) return;
            Assert.AreEqual(a.UncompressedDataLength, b.UncompressedDataLength);
            Assert.AreEqual(a.Level, b.Level);
            Assert.AreEqual(a.LeaveOpen, b.LeaveOpen);
        }
    }
}
