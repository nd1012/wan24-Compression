using System.Security.Cryptography;
using wan24.Compression;
using wan24.Core;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class Archive_Tests
    {
        [TestMethod/*, Timeout(3000)*/]
        public async Task General_TestsAsync()
        {
            CompressionOptions options = BrotliCompressionAlgorithm.Instance.DefaultOptions;
            byte[] TestData = RandomNumberGenerator.GetBytes(ushort.MaxValue);
            using MemoryStream ms = new();
            using (ArchiveCompression compression = new(ms, options, leaveOpen: true))
            {
                using (MemoryStream file = new())
                {
                    await compression.AddFileAsync("/empty.dat", file);
                    file.Write(TestData);
                    file.Position = 0;
                    await compression.AddFileAsync("/compressed.dat", file);
                    file.Position = 0;
                    await compression.AddFileAsync("/uncompressed.dat", file, uncompressed: true);
                }
                await compression.AddFolderAsync("/folder");
                await compression.AddKeyValueAsync("test", TestData);
            }
            Assert.AreNotEqual(0, ms.Length);

            string dir = Path.GetFullPath("tempdir"),
                fn;
            if (Directory.Exists(dir)) await FsHelper.DeleteFolderAsync(dir);
            FsHelper.CreateFolder(dir);
            ms.Position = 0;
            using ArchiveDecompression decompression = await ArchiveDecompression.CreateFromAsync(ms, options: options);
            Dictionary<string, byte[]> kvp = await decompression.ExtractToAsync(dir);
            Assert.AreEqual(1, kvp.Count);
            Assert.IsTrue(kvp.ContainsKey("test"));
            Assert.IsTrue(kvp["test"].SequenceEqual(TestData));
            fn = Path.Combine(dir, "emnpty.dat");
            Assert.IsTrue(File.Exists(fn));
            Assert.AreEqual(0, new FileInfo(fn).Length);
            foreach (string name in new string[] { "compressed.dat", "uncompressed.dat" })
            {
                fn = Path.Combine(dir, name);
                Assert.IsTrue(File.Exists(fn), $"{name} not found");
                Assert.IsTrue(File.ReadAllBytes(fn).SequenceEqual(TestData), $"{name} content mismatch");
            }
            Assert.IsTrue(Directory.Exists(Path.Combine(dir, "folder")));
            await FsHelper.DeleteFolderAsync(dir);
        }
    }
}
