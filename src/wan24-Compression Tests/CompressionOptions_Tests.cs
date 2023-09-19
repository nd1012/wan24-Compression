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

        [TestMethod]
        public void MaxUncompressedDataLength_Tests()
        {
            CompressionOptions options = new();
            options.WithFlagsIncluded(CompressionFlags.ALL)
                .WithMaxUncompressedDataLength(1);
            byte[] data = new byte[] { 1, 2 },
                compressed = data.Compress(options),
                decompressed = Array.Empty<byte>();

            // Limit from header
            Assert.AreNotEqual(0, compressed.Length);
            Assert.ThrowsException<InvalidDataException>(() => decompressed = compressed.Decompress(options), $"Decompressed length: {decompressed.Length}");

            // Limit from length limited stream
            options.IncludeNothing();
            compressed = data.Compress(options);
            Assert.AreNotEqual(0, compressed.Length);
            decompressed = Array.Empty<byte>();
            Assert.ThrowsException<InvalidDataException>(() => decompressed = compressed.Decompress(options), $"Decompressed length: {decompressed.Length}");
        }

        public static void CompareOptions(CompressionOptions a, CompressionOptions b, bool serialized = true)
        {
            Assert.AreEqual(a.FlagsIncluded, b.FlagsIncluded);
            Assert.AreEqual(a.AlgorithmIncluded, b.AlgorithmIncluded);
            Assert.AreEqual(a.Algorithm, b.Algorithm);
            Assert.AreEqual(a.SerializerVersionIncluded, b.SerializerVersionIncluded);
            if (serialized) return;
            Assert.AreEqual(a.SerializerVersion, b.SerializerVersion);
            Assert.AreEqual(a.UncompressedDataLength, b.UncompressedDataLength);
            Assert.AreEqual(a.Level, b.Level);
            Assert.AreEqual(a.LeaveOpen, b.LeaveOpen);
        }
    }
}
