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
            Algorithms = new(
            [
                new(GZipCompressionAlgorithm.Instance.Name, GZipCompressionAlgorithm.Instance),
                new(BrotliCompressionAlgorithm.Instance.Name, BrotliCompressionAlgorithm.Instance)
            ]);
            _DefaultAlgorithm = BrotliCompressionAlgorithm.Instance;
        }

        /// <summary>
        /// An object for thread synchronization
        /// </summary>
        public static object SyncObject { get; } = new();

        /// <summary>
        /// State
        /// </summary>
        public static IEnumerable<Status> State
        {
            get
            {
                yield return new("Default", DefaultAlgorithm.Name, "Default algorithm name");
                foreach (CompressionAlgorithmBase algo in Algorithms.Values)
                    foreach (Status status in algo.State)
                        yield return new(status.Name, status.State, status.Description, $"Compression\\{algo.DisplayName}");
            }
        }

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
            return GetAlgorithm(options.Algorithm!).Compress(uncompressedSource, compressedTarget, options);
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
            await GetAlgorithm(options.Algorithm!).CompressAsync(uncompressedSource, compressedTarget, options, cancellationToken).DynamicContext();
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
            return GetAlgorithm(options.Algorithm!).GetCompressionStream(compressedTarget, options);
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
            CompressionAlgorithmBase algo = GetAlgorithm(options.Algorithm!);
            options = algo.ReadOptions(compressedSource, uncompressedTarget, options);
            options = GetDefaultOptions(options);
            algo = GetAlgorithm(options.Algorithm!);
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
            CompressionAlgorithmBase algo = GetAlgorithm(options.Algorithm!);
            options = (await algo.ReadOptionsAsync(compressedSource, uncompressedTarget, options, cancellationToken).DynamicContext());
            options = GetDefaultOptions(options);
            algo = GetAlgorithm(options.Algorithm!);
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
            return GetAlgorithm(options.Algorithm!).GetDecompressionStream(compressedSource, options);
        }

        /// <summary>
        /// Write the options
        /// </summary>
        /// <param name="uncompressedSource">Source stream</param>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Written options</returns>
        public static CompressionOptions WriteOptions(Stream uncompressedSource, Stream compressedTarget, CompressionOptions? options = null)
        {
            options = GetDefaultOptions(options);
            return GetAlgorithm(options.Algorithm!).WriteOptions(uncompressedSource, compressedTarget, options);
        }

        /// <summary>
        /// Write the options
        /// </summary>
        /// <param name="uncompressedSource">Source stream</param>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Written options</returns>
        public static async Task<CompressionOptions> WriteOptionsAsync(
            Stream uncompressedSource, 
            Stream compressedTarget, 
            CompressionOptions? options = null, 
            CancellationToken cancellationToken = default
            )
        {
            options = GetDefaultOptions(options);
            return await GetAlgorithm(options.Algorithm!).WriteOptionsAsync(uncompressedSource, compressedTarget, options, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Read the options
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Red options</returns>
        public static CompressionOptions ReadOptions(Stream compressedSource, Stream uncompressedTarget, CompressionOptions? options = null)
        {
            options = GetDefaultOptions(options);
            return GetAlgorithm(options.Algorithm!).ReadOptions(compressedSource, uncompressedTarget, options);
        }

        /// <summary>
        /// Read the options
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Red options</returns>
        public static async Task<CompressionOptions> ReadOptionsAsync(
            Stream compressedSource,
            Stream uncompressedTarget,
            CompressionOptions? options = null,
            CancellationToken cancellationToken = default
            )
        {
            options = GetDefaultOptions(options);
            return await GetAlgorithm(options.Algorithm!).ReadOptionsAsync(compressedSource, uncompressedTarget, options, cancellationToken).DynamicContext();
        }


        /// <summary>
        /// Get the default options used by the compression helper
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions GetDefaultOptions(CompressionOptions? options = null)
        {
            if (options is null)
            {
                options = DefaultAlgorithm.DefaultOptions;
            }
            else
            {
                options.Algorithm ??= DefaultAlgorithm.Name;
            }
            return options;
        }

        /// <summary>
        /// Get an algorithm
        /// </summary>
        /// <param name="name">Algorithm name</param>
        /// <returns>Algorithm</returns>
        public static CompressionAlgorithmBase GetAlgorithm(string name)
            => Algorithms.TryGetValue(name, out CompressionAlgorithmBase? algo)
                ? algo
                : throw new ArgumentException("Invalid algorithm", nameof(name));

        /// <summary>
        /// Get an algorithm
        /// </summary>
        /// <param name="value">Algorithm value</param>
        /// <returns>Algorithm</returns>
        public static CompressionAlgorithmBase GetAlgorithm(int value)
            => Algorithms.Values.FirstOrDefault(a => a.Value == value) ?? throw new ArgumentException("Invalid algorithm", nameof(value));
    }
}
