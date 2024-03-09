using System.IO.Compression;

namespace wan24.Compression
{
    /// <summary>
    /// GZip compression algorithm
    /// </summary>
    public sealed record class GZipCompressionAlgorithm : CompressionAlgorithmBase
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
        /// Algorithm display name
        /// </summary>
        public const string DISPLAY_NAME = "GZip";
        /// <summary>
        /// GZip raw (without header) profile key
        /// </summary>
        public const string PROFILE_GZIP_RAW = "GZIP_RAW";

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
        public override string DisplayName => DISPLAY_NAME;

        /// <inheritdoc/>
        protected override Stream CreateCompressionStream(Stream compressedTarget, CompressionOptions options)
            => new GZipStream(compressedTarget, options.Level, options.LeaveOpen);

        /// <inheritdoc/>
        protected override Stream CreateDecompressionStream(Stream source, CompressionOptions options)
            => new GZipStream(source, CompressionMode.Decompress, options.LeaveOpen);
    }
}
