using System.IO.Compression;

namespace wan24.Compression
{
    /// <summary>
    /// Brotli compression algorithm
    /// </summary>
    public sealed record class BrotliCompressionAlgorithm : CompressionAlgorithmBase
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
        /// Algorithm display name
        /// </summary>
        public const string DISPLAY_NAME = "Brotli";
        /// <summary>
        /// Brotli raw (without header) profile key
        /// </summary>
        public const string PROFILE_BROTLI_RAW = "BROTLI_RAW";

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
        public override string DisplayName => DISPLAY_NAME;

        /// <inheritdoc/>
        protected override Stream CreateCompressionStream(Stream compressedTarget, CompressionOptions options)
            => new BrotliStream(compressedTarget, options.Level, options.LeaveOpen);

        /// <inheritdoc/>
        protected override Stream CreateDecompressionStream(Stream source, CompressionOptions options)
            => new BrotliStream(source, CompressionMode.Decompress, options.LeaveOpen);
    }
}
