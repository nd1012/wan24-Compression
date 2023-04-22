namespace wan24.Compression
{
    /// <summary>
    /// Compression extensions
    /// </summary>
    public static class CompressionExtensions
    {
        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="options">Options</param>
        /// <returns>Compressed data</returns>
        public static byte[] Compress(this Span<byte> data, CompressionOptions? options = null)
        {
            options = CompressionHelper.GetDefaultOptions(options?.Clone());
            options.LeaveOpen = true;
            using MemoryStream compressed = new();
            using (MemoryStream ms = new())
            {
                ms.Write(data);
                ms.Position = 0;
                ms.Compress(compressed, options);
            }
            return compressed.ToArray();
        }

        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="options">Options</param>
        /// <returns>Compressed data</returns>
        public static byte[] Compress(this Memory<byte> data, CompressionOptions? options = null) => data.Span.Compress(options);

        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="options">Options</param>
        /// <returns>Compressed data</returns>
        public static byte[] Compress(this byte[] data, CompressionOptions? options = null) => data.AsSpan().Compress(options);

        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data">Compressed data</param>
        /// <param name="options">Options</param>
        /// <returns>Decompressed data</returns>
        public static byte[] Decompress(this Span<byte> data, CompressionOptions? options = null)
        {
            options = CompressionHelper.GetDefaultOptions(options?.Clone());
            options.LeaveOpen = true;
            using MemoryStream decompressed = new();
            using (MemoryStream ms = new())
            {
                ms.Write(data);
                ms.Position = 0;
                ms.Decompress(decompressed, options);
            }
            return decompressed.ToArray();
        }

        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data">Compressed data</param>
        /// <param name="options">Options</param>
        /// <returns>Decompressed data</returns>
        public static byte[] Decompress(this Memory<byte> data, CompressionOptions? options = null) => data.Span.Decompress(options);

        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data">Compressed data</param>
        /// <param name="options">Options</param>
        /// <returns>Decompressed data</returns>
        public static byte[] Decompress(this byte[] data, CompressionOptions? options = null) => data.AsSpan().Decompress(options);
    }
}
