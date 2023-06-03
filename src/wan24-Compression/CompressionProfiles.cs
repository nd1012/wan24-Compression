using System.Collections.Concurrent;

namespace wan24.Compression
{
    /// <summary>
    /// Crypto profiles
    /// </summary>
    public static class CompressionProfiles
    {
        /// <summary>
        /// Registered profiles
        /// </summary>
        public static readonly ConcurrentDictionary<string, CompressionOptions> Registered;

        /// <summary>
        /// Constructor
        /// </summary>
        static CompressionProfiles() => Registered = new(new KeyValuePair<string, CompressionOptions>[]
        {
            new(
                GZipCompressionAlgorithm.PROFILE_GZIP_RAW,
                new CompressionOptions()
                    .IncludeNothing()
                    .WithAlgorithm(GZipCompressionAlgorithm.ALGORITHM_NAME)
                ),
            new(
                BrotliCompressionAlgorithm.PROFILE_BROTLI_RAW,
                new CompressionOptions()
                    .IncludeNothing()
                    .WithAlgorithm(BrotliCompressionAlgorithm.ALGORITHM_NAME)
                )
        });

        /// <summary>
        /// Get a profile
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Profile</returns>
        public static CompressionOptions GetProfile(string key)
            => Registered.TryGetValue(key, out CompressionOptions? res) ? res.Clone() : throw new ArgumentException("Unknown profile", nameof(key));
    }
}
