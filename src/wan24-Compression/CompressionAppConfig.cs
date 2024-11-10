using System.IO.Compression;
using System.Text.Json.Serialization;
using wan24.Core;

namespace wan24.Compression
{
    /// <summary>
    /// Compression app configuration (<see cref="AppConfig"/>; should be applied AFTER bootstrapping (<see cref="AppConfigAttribute.AfterBootstrap"/>))
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    public class CompressionAppConfig() : AppConfigBase()
    {
        /// <summary>
        /// Applied compression app configuration
        /// </summary>
        [JsonIgnore]
        public static CompressionAppConfig? AppliedCompressionConfig { get; protected set; }

        /// <summary>
        /// Default compression algorithm name
        /// </summary>
        public string? DefaultAlgorithm { get; set; }

        /// <summary>
        /// Default flags
        /// </summary>
        public CompressionFlags? DefaultFlags { get; set; }

        /// <summary>
        /// Default flags included?
        /// </summary>
        public bool? DefaultFlagsIncluded { get; set; }

        /// <summary>
        /// Default compression level
        /// </summary>
        public CompressionLevel? DefaultCompressionLevel { get; set; }

        /// <summary>
        /// Disabled algorithm names
        /// </summary>
        public string[]? DisabledAlgorithms { get; set; }

        /// <summary>
        /// Default archive chunk size for compressed streams of <see cref="ArchiveCompression"/> in bytes
        /// </summary>
        public int? DefaultArchiveChunkSize { get; set; }

        /// <summary>
        /// Default maximum archive chunk size for compressed streams of <see cref="ArchiveCompression"/> in bytes
        /// </summary>
        public int? MaxArchiveChunkSize { get; set; }

        /// <summary>
        /// Default chunk size for non-seekable source streams for <see cref="ArchiveCompression"/> in bytes
        /// </summary>
        public int? DefaultChunkSize { get; set; }

        /// <summary>
        /// Default maximum key / (file) path length for <see cref="ArchiveCompression"/> in bytes
        /// </summary>
        public int? DefaultMaxKeyLength { get; set; }

        /// <inheritdoc/>
        public override void Apply()
        {
            if (SetApplied)
            {
                if (AppliedCompressionConfig is not null) throw new InvalidOperationException();
                AppliedCompressionConfig = this;
            }
            if (DefaultAlgorithm is not null) CompressionHelper.DefaultAlgorithm = CompressionHelper.GetAlgorithm(DefaultAlgorithm);
            if (DefaultFlags.HasValue) CompressionOptions.DefaultFlags = DefaultFlags.Value;
            if (DefaultFlagsIncluded.HasValue) CompressionOptions.DefaultFlagsIncluded = DefaultFlagsIncluded.Value;
            if (DefaultCompressionLevel.HasValue) CompressionOptions.DefaultCompressionLevel = DefaultCompressionLevel.Value;
            if (DefaultArchiveChunkSize.HasValue) ArchiveCompression.DefaultChunkSize = DefaultArchiveChunkSize.Value;
            if (MaxArchiveChunkSize.HasValue) ArchiveDecompression.MaxChunkSize = MaxArchiveChunkSize.Value;
            if (DefaultChunkSize.HasValue) CompressionOptions.DefaultChunkSize = DefaultChunkSize.Value;
            if (DefaultMaxKeyLength.HasValue) CompressionOptions.DefaultMaxKeyLength = DefaultMaxKeyLength.Value;
            ApplyProperties(afterBootstrap: false);
            if (DisabledAlgorithms is not null)
            {
                if (DefaultAlgorithm is not null && DisabledAlgorithms.Contains(DefaultAlgorithm))
                    throw new InvalidDataException("Found default algorithm in disabled algorithms");
                foreach (string algo in DisabledAlgorithms)
                    CompressionHelper.Algorithms.TryRemove(algo, out _);
            }
            ApplyProperties(afterBootstrap: true);
        }

        /// <inheritdoc/>
        public override async Task ApplyAsync(CancellationToken cancellationToken = default)
        {
            if (SetApplied)
            {
                if (AppliedCompressionConfig is not null) throw new InvalidOperationException();
                AppliedCompressionConfig = this;
            }
            if (DefaultAlgorithm is not null) CompressionHelper.DefaultAlgorithm = CompressionHelper.GetAlgorithm(DefaultAlgorithm);
            if (DefaultFlags.HasValue) CompressionOptions.DefaultFlags = DefaultFlags.Value;
            if (DefaultFlagsIncluded.HasValue) CompressionOptions.DefaultFlagsIncluded = DefaultFlagsIncluded.Value;
            if (DefaultCompressionLevel.HasValue) CompressionOptions.DefaultCompressionLevel = DefaultCompressionLevel.Value;
            if (DefaultArchiveChunkSize.HasValue) ArchiveCompression.DefaultChunkSize = DefaultArchiveChunkSize.Value;
            if (MaxArchiveChunkSize.HasValue) ArchiveDecompression.MaxChunkSize = MaxArchiveChunkSize.Value;
            if (DefaultChunkSize.HasValue) CompressionOptions.DefaultChunkSize = DefaultChunkSize.Value;
            if (DefaultMaxKeyLength.HasValue) CompressionOptions.DefaultMaxKeyLength = DefaultMaxKeyLength.Value;
            await ApplyPropertiesAsync(afterBootstrap: false, cancellationToken).DynamicContext();
            if (DisabledAlgorithms is not null)
                foreach (string algo in DisabledAlgorithms)
                    CompressionHelper.Algorithms.TryRemove(algo, out _);
            await ApplyPropertiesAsync(afterBootstrap: true, cancellationToken).DynamicContext();
        }
    }
}
