using System.Collections.Concurrent;
using wan24.Core;

namespace wan24.Compression
{
    /// <summary>
    /// Compression helper
    /// </summary>
    public static class CompressionHelper
    {
        /// <summary>
        /// Default compression algorithm
        /// </summary>
        private static CompressionAlgorithmBase _DefaultAlgorithm;

        /// <summary>
        /// Registered compression algorithms
        /// </summary>
        public static readonly ConcurrentDictionary<string, CompressionAlgorithmBase> Algorithms;

        /// <summary>
        /// Constructor
        /// </summary>
        static CompressionHelper()
        {
            Algorithms = new(new KeyValuePair<string, CompressionAlgorithmBase>[]
            {
                new(GZipCompressionAlgorithm.Instance.Name, GZipCompressionAlgorithm.Instance),
                new(BrotliCompressionAlgorithm.Instance.Name, BrotliCompressionAlgorithm.Instance)
            });
            _DefaultAlgorithm = BrotliCompressionAlgorithm.Instance;
        }

        /// <summary>
        /// An object for thread synchronization
        /// </summary>
        public static object SyncObject { get; } = new();

        /// <summary>
        /// Default compression algorithm
        /// </summary>
        public static CompressionAlgorithmBase DefaultAlgorithm
        {
            get => _DefaultAlgorithm;
            set
            {
                lock (SyncObject) _DefaultAlgorithm = value;
            }
        }

        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="uncompressedSource">Source</param>
        /// <param name="compressedTarget">Target</param>
        /// <param name="options">Options</param>
        /// <returns>Target</returns>
        public static Stream Compress(this Stream uncompressedSource, Stream compressedTarget, CompressionOptions? options = null)
        {
            options = GetDefaultOptions(options);
            if (!Algorithms.TryGetValue(options.Algorithm!, out CompressionAlgorithmBase? algo)) throw new ArgumentException("Invalid algorithm", nameof(options));
            return algo.Compress(uncompressedSource, compressedTarget, options);
        }

        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="uncompressedSource">Source</param>
        /// <param name="compressedTarget">Target</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task CompressAsync(
            this Stream uncompressedSource,
            Stream compressedTarget, 
            CompressionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options = GetDefaultOptions(options);
            if (!Algorithms.TryGetValue(options.Algorithm!, out CompressionAlgorithmBase? algo)) throw new ArgumentException("Invalid algorithm", nameof(options));
            await algo.CompressAsync(uncompressedSource, compressedTarget, options, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Get a compression stream
        /// </summary>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Compression stream</returns>
        public static Stream GetCompressionStream(this Stream compressedTarget, CompressionOptions? options = null)
        {
            options = GetDefaultOptions(options);
            if (!Algorithms.TryGetValue(options.Algorithm!, out CompressionAlgorithmBase? algo)) throw new ArgumentException("Invalid algorithm", nameof(options));
            return algo.GetCompressionStream(compressedTarget, options);
        }

        /// <summary>
        /// Decompress a stream
        /// </summary>
        /// <param name="compressedSource">Source</param>
        /// <param name="uncompressedTarget">Target</param>
        /// <param name="options">Options</param>
        /// <returns>Target</returns>
        public static Stream Decompress(this Stream compressedSource, Stream uncompressedTarget, CompressionOptions? options = null)
        {
            options = GetDefaultOptions(options);
            if (!Algorithms.TryGetValue(options.Algorithm!, out CompressionAlgorithmBase? algo)) throw new ArgumentException("Invalid algorithm", nameof(options));
            options = algo.ReadOptions(compressedSource, uncompressedTarget, options).Options;
            options.Flags = CompressionFlags.None;
            options.FlagsIncluded = false;
            return algo.Decompress(compressedSource, uncompressedTarget, options);
        }

        /// <summary>
        /// Decompress a stream
        /// </summary>
        /// <param name="compressedSource">Source</param>
        /// <param name="uncompressedTarget">Target</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task DecompressAsync(this Stream compressedSource, Stream uncompressedTarget, CompressionOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = GetDefaultOptions(options);
            if (!Algorithms.TryGetValue(options.Algorithm!, out CompressionAlgorithmBase? algo)) throw new ArgumentException("Invalid algorithm", nameof(options));
            options = (await algo.ReadOptionsAsync(compressedSource, uncompressedTarget, options, cancellationToken).DynamicContext()).Options;
            options.Flags = CompressionFlags.None;
            options.FlagsIncluded = false;
            await algo.DecompressAsync(compressedSource, uncompressedTarget, options, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Get a decompression stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="options">Options</param>
        /// <returns>Decompression stream</returns>
        public static Stream GetDecompressionStream(this Stream compressedSource, CompressionOptions? options = null)
        {
            options = GetDefaultOptions(options);
            if (!Algorithms.TryGetValue(options.Algorithm!, out CompressionAlgorithmBase? algo)) throw new ArgumentException("Invalid algorithm", nameof(options));
            return algo.GetDecompressionStream(compressedSource, options);
        }

        /// <summary>
        /// Get the default options used by the compression helper
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions GetDefaultOptions(CompressionOptions? options = null)
        {
            if (options == null)
            {
                options = DefaultAlgorithm.DefaultOptions;
                options.Algorithm = DefaultAlgorithm.Name;
                options.AlgorithmIncluded = true;
                options.LeaveOpen = false;
            }
            else
            {
                options.Algorithm ??= DefaultAlgorithm.Name;
                if (!Algorithms.TryGetValue(options.Algorithm, out _)) throw new ArgumentException("Invalid compression algorithm", nameof(options));
            }
            return options;
        }

        /// <summary>
        /// Get the compression algorithm name
        /// </summary>
        /// <param name="algo">Compression algorithm value</param>
        /// <returns>Compression algorithm name</returns>
        public static string GetAlgorithmName(int algo)
            => Algorithms.Values.Where(a => a.Value == algo).Select(a => a.Name).FirstOrDefault()
                ?? throw new ArgumentException("Invalid algorithm", nameof(algo));

        /// <summary>
        /// Get the compression algorithm value
        /// </summary>
        /// <param name="algo">Compression algorithm name</param>
        /// <returns>Compression algorithm value</returns>
        public static int GetAlgorithmValue(string algo)
            => Algorithms.TryGetValue(algo, out CompressionAlgorithmBase? a)
                ? a.Value
                : throw new ArgumentException("Invalid algorithm", nameof(algo));
    }
}
