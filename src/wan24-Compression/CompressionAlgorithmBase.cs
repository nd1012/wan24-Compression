using System.Buffers;
using wan24.Core;
using wan24.StreamSerializerExtensions;

namespace wan24.Compression
{
    /// <summary>
    /// Compression algorithm
    /// </summary>
    public abstract class CompressionAlgorithmBase
    {
        /// <summary>
        /// Default options
        /// </summary>
        protected readonly CompressionOptions _DefaultOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="value">Value</param>
        protected CompressionAlgorithmBase(string name, int value)
        {
            Name = name;
            Value = value;
            _DefaultOptions = new()
            {
                Algorithm = name
            };
        }

        /// <summary>
        /// Algorithm name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Algorithm value
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Default options
        /// </summary>
        public CompressionOptions DefaultOptions => _DefaultOptions.Clone();

        /// <summary>
        /// Compress a stream
        /// </summary>
        /// <param name="uncompressedSource">Source stream</param>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Target</returns>
        public virtual Stream Compress(Stream uncompressedSource, Stream compressedTarget, CompressionOptions? options = null)
        {
            options = WriteOptions(uncompressedSource, compressedTarget, options);
            using Stream compression = GetCompressionStream(compressedTarget, options);
            uncompressedSource.CopyTo(compression);
            return compressedTarget;
        }

        /// <summary>
        /// Compress a stream
        /// </summary>
        /// <param name="uncompressedSource">Source stream</param>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task CompressAsync(Stream uncompressedSource, Stream compressedTarget, CompressionOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = await WriteOptionsAsync(uncompressedSource, compressedTarget, options, cancellationToken).DynamicContext();
            Stream compression = GetCompressionStream(compressedTarget, options);
            await using (compression.DynamicContext())
                await uncompressedSource.CopyToAsync(compression, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Get a compression stream
        /// </summary>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Compression stream</returns>
        public Stream GetCompressionStream(Stream compressedTarget, CompressionOptions? options = null) => CreateCompressionStream(compressedTarget, options ?? DefaultOptions);

        /// <summary>
        /// Decompress a stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Target</returns>
        public virtual Stream Decompress(Stream compressedSource, Stream uncompressedTarget, CompressionOptions? options = null)
        {
            options = ReadOptions(compressedSource, uncompressedTarget, options);
            long pos = options.UncompressedDataLength > -1 ? uncompressedTarget.Position : -1;
            using Stream compression = GetDecompressionStream(compressedSource, options);
            compression.CopyTo(uncompressedTarget);
            if (options.UncompressedDataLength > -1 && uncompressedTarget.Position - pos != options.UncompressedDataLength)
                throw new InvalidDataException($"Uncompressed data length mismatch (expected {options.UncompressedDataLength}, got {uncompressedTarget.Position - pos})");
            return uncompressedTarget;
        }

        /// <summary>
        /// Decompress a stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task DecompressAsync(Stream compressedSource, Stream uncompressedTarget, CompressionOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = await ReadOptionsAsync(compressedSource, uncompressedTarget, options, cancellationToken).DynamicContext();
            long pos = options.UncompressedDataLength > -1 ? uncompressedTarget.Position : -1;
            Stream compression = GetDecompressionStream(compressedSource, options);
            await using (compression.DynamicContext())
                await compression.CopyToAsync(uncompressedTarget, cancellationToken).DynamicContext();
            if (options.UncompressedDataLength > -1 && uncompressedTarget.Position - pos != options.UncompressedDataLength)
                throw new InvalidDataException($"Uncompressed data length mismatch (expected {options.UncompressedDataLength}, got {uncompressedTarget.Position - pos})");
        }

        /// <summary>
        /// Get a decompression stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="options">Options</param>
        /// <returns>Decompression stream</returns>
        public Stream GetDecompressionStream(Stream compressedSource, CompressionOptions? options = null)
        {
            options ??= DefaultOptions;
            Stream res = CreateDecompressionStream(compressedSource, options);
            if (options.MaxUncompressedDataLength > 0) res = new LimitedLengthStream(res, options.MaxUncompressedDataLength)
            {
                ThrowOnReadOverflow = true
            };
            return res;
        }

        /// <summary>
        /// Write the options
        /// </summary>
        /// <param name="uncompressedSource">Source stream</param>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Written options</returns>
        public virtual CompressionOptions WriteOptions(Stream uncompressedSource, Stream compressedTarget, CompressionOptions? options = null)
        {
            options ??= DefaultOptions;
            if (options.FlagsIncluded) compressedTarget.Write((byte)options.Flags);
            if (options.SerializerVersionIncluded) compressedTarget.WriteSerializerVersion();
            if (options.AlgorithmIncluded) compressedTarget.WriteNumber(Value);
            if (options.UncompressedLengthIncluded) compressedTarget.WriteNumber(uncompressedSource.Length - uncompressedSource.Position);
            return options;
        }

        /// <summary>
        /// Write the options
        /// </summary>
        /// <param name="uncompressedSource">Source stream</param>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Written options</returns>
        public virtual async Task<CompressionOptions> WriteOptionsAsync(
            Stream uncompressedSource, 
            Stream compressedTarget, 
            CompressionOptions? options = null, 
            CancellationToken cancellationToken = default
            )
        {
            options ??= DefaultOptions;
            if (options.FlagsIncluded) await compressedTarget.WriteAsync((byte)options.Flags, cancellationToken).DynamicContext();
            if (options.SerializerVersionIncluded) await compressedTarget.WriteSerializerVersionAsync(cancellationToken).DynamicContext();
            if (options.AlgorithmIncluded) await compressedTarget.WriteNumberAsync(Value, cancellationToken).DynamicContext();
            if (options.UncompressedLengthIncluded) await compressedTarget.WriteNumberAsync(uncompressedSource.Length - uncompressedSource.Position, cancellationToken).DynamicContext();
            return options;
        }

        /// <summary>
        /// Read the options
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Red options</returns>
        public virtual CompressionOptions ReadOptions(
            Stream compressedSource, 
            Stream uncompressedTarget, 
            CompressionOptions? options = null
            )
        {
            options = options?.Clone() ?? DefaultOptions;
            if (options.FlagsIncluded) options.Flags = (CompressionFlags)compressedSource.ReadOneByte();
            int? serializerVersion = options.SerializerVersionIncluded ? options.SerializerVersion = compressedSource.ReadSerializerVersion() : null;
            if (options.AlgorithmIncluded && compressedSource.ReadNumber<int>(serializerVersion) != Value) throw new InvalidDataException("Compression algorithm mismatch");
            if (options.UncompressedLengthIncluded)
            {
                options.UncompressedDataLength = compressedSource.ReadNumber<long>(serializerVersion);
                if (options.UncompressedDataLength < 0) throw new InvalidDataException($"Invalid uncompressed data length ({options.UncompressedDataLength})");
                if (options.MaxUncompressedDataLength > 0 && options.UncompressedDataLength > options.MaxUncompressedDataLength)
                    throw new InvalidDataException($"Uncompressed data length {options.UncompressedDataLength} byte exceeds the maximum of {options.MaxUncompressedDataLength} byte");
            }
            return options;
        }

        /// <summary>
        /// Read the options
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Red options</returns>
        public virtual async Task<CompressionOptions> ReadOptionsAsync(
            Stream compressedSource, 
            Stream uncompressedTarget, 
            CompressionOptions? options = null,
            CancellationToken cancellationToken = default
            )
        {
            options = options?.Clone() ?? DefaultOptions;
            if (options.FlagsIncluded) options.Flags = (CompressionFlags)compressedSource.ReadOneByte();
            int? serializerVersion = options.SerializerVersionIncluded ? options.SerializerVersion = await compressedSource.ReadSerializerVersionAsync(cancellationToken).DynamicContext() : null;
            if (options.AlgorithmIncluded && await compressedSource.ReadNumberAsync<int>(serializerVersion, cancellationToken: cancellationToken).DynamicContext() != Value)
                throw new InvalidDataException("Compression algorithm mismatch");
            if (options.UncompressedLengthIncluded)
            {
                options.UncompressedDataLength = await compressedSource.ReadNumberAsync<long>(serializerVersion, cancellationToken: cancellationToken).DynamicContext();
                if (options.UncompressedDataLength < 0) throw new InvalidDataException($"Invalid uncompressed data length ({options.UncompressedDataLength})");
                if (options.MaxUncompressedDataLength > 0 && options.UncompressedDataLength > options.MaxUncompressedDataLength)
                    throw new InvalidDataException($"Uncompressed data length {options.UncompressedDataLength} byte exceeds the maximum of {options.MaxUncompressedDataLength} byte");
            }
            return options;
        }

        /// <summary>
        /// Create a compression stream
        /// </summary>
        /// <param name="compressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Compression stream</returns>
        protected abstract Stream CreateCompressionStream(Stream compressedTarget, CompressionOptions options);

        /// <summary>
        /// Create a decompression stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="options">Options</param>
        /// <returns>Decompression stream</returns>
        protected abstract Stream CreateDecompressionStream(Stream compressedSource, CompressionOptions options);
    }
}
