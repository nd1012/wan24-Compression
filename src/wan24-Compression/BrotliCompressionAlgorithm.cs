using System.IO.Compression;

namespace wan24.Compression
{
    /// <summary>
    /// Brotli compression algorithm
    /// </summary>
    public sealed class BrotliCompressionAlgorithm : CompressionAlgorithmBase
    {
        /// <summary>
        /// Algorithm name
        /// </summary>
        public const string ALGORITHM_NAME = "Brotli";
        /// <summary>
        /// Algorithm value
        /// </summary>
        public const int ALGORITHM_VALUE = 1;

        /// <summary>
        /// Static constructor
        /// </summary>
        static BrotliCompressionAlgorithm() => Instance = new();

        /// <summary>
        /// Constructor
        /// </summary>
        private BrotliCompressionAlgorithm() : base(ALGORITHM_NAME, ALGORITHM_VALUE) { }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static BrotliCompressionAlgorithm Instance { get; }

        /// <inheritdoc/>
        public override Stream GetCompressionStream(Stream compressedTarget, CompressionOptions? options = null)
        {
            options ??= new()
            {
                AlgorithmIncluded = false
            };
            return new BrotliStream(compressedTarget, options.Level, options.LeaveOpen);
        }

        /// <inheritdoc/>
        public override Stream GetDecompressionStream(Stream source, CompressionOptions? options = null)
        {
            options ??= new()
            {
                AlgorithmIncluded = false
            };
            return new BrotliStream(source, CompressionMode.Decompress, options.LeaveOpen);
        }
    }
}
