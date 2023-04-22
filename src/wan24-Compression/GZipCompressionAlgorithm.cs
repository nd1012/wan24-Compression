using System.IO.Compression;

namespace wan24.Compression
{
    /// <summary>
    /// GZip compression algorithm
    /// </summary>
    public sealed class GZipCompressionAlgorithm : CompressionAlgorithmBase
    {
        /// <summary>
        /// Algorithm name
        /// </summary>
        public const string ALGORITHM_NAME = "GZip";
        /// <summary>
        /// Algorithm value
        /// </summary>
        public const int ALGORITHM_VALUE = 0;

        /// <summary>
        /// Static constructor
        /// </summary>
        static GZipCompressionAlgorithm() => Instance = new();

        /// <summary>
        /// Constructor
        /// </summary>
        private GZipCompressionAlgorithm() : base(ALGORITHM_NAME, ALGORITHM_VALUE) { }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static GZipCompressionAlgorithm Instance { get; }

        /// <inheritdoc/>
        public override Stream GetCompressionStream(Stream compressedTarget, CompressionOptions? options = null)
        {
            options ??= DefaultOptions;
            return new GZipStream(compressedTarget, options.Level, options.LeaveOpen);
        }

        /// <inheritdoc/>
        public override Stream GetDecompressionStream(Stream source, CompressionOptions? options = null)
        {
            options ??= DefaultOptions;
            return new GZipStream(source, CompressionMode.Decompress, options.LeaveOpen);
        }
    }
}
