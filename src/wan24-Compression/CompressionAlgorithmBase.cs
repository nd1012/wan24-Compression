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
        protected readonly CompressionOptions _DefaultOptions = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="value">Value</param>
        protected CompressionAlgorithmBase(string name, int value)
        {
            Name = name;
            Value = value;
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
        public abstract Stream GetCompressionStream(Stream compressedTarget, CompressionOptions? options = null);

        /// <summary>
        /// Decompress a stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <returns>Target</returns>
        public virtual Stream Decompress(Stream compressedSource, Stream uncompressedTarget, CompressionOptions? options = null)
        {
            (options, _, long len) = ReadOptions(compressedSource, uncompressedTarget, options);
            long pos = len > -1 ? uncompressedTarget.Position : -1;
            using Stream compression = GetDecompressionStream(compressedSource, options);
            compression.CopyTo(uncompressedTarget);
            if (len > -1 && uncompressedTarget.Position - pos != len) throw new InvalidDataException($"Uncompressed data length mismatch (expected {len}, got {uncompressedTarget.Position - pos})");
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
            (options, _, long len) = await ReadOptionsAsync(compressedSource, uncompressedTarget, options, cancellationToken).DynamicContext();
            long pos = len > -1 ? uncompressedTarget.Position : -1;
            Stream compression = GetDecompressionStream(compressedSource, options);
            await using (compression.DynamicContext())
                await compression.CopyToAsync(uncompressedTarget, cancellationToken).DynamicContext();
            if (len > -1 && uncompressedTarget.Position - pos != len) throw new InvalidDataException($"Uncompressed data length mismatch (expected {len}, got {uncompressedTarget.Position - pos})");
        }

        /// <summary>
        /// Get a decompression stream
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="options">Options</param>
        /// <returns>Decompression stream</returns>
        public abstract Stream GetDecompressionStream(Stream compressedSource, CompressionOptions? options = null);

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
            if (options.SerializerVersionIncluded) compressedTarget.Write(StreamSerializer.VERSION.GetBytes());
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
            if (options.SerializerVersionIncluded) await compressedTarget.WriteAsync(StreamSerializer.VERSION.GetBytes(), cancellationToken).DynamicContext();
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
        /// <returns>Red options, serializer version and the uncompressed data length</returns>
        public virtual (CompressionOptions Options, int? SerializerVersion, long UncompressedDataLength) ReadOptions(
            Stream compressedSource, 
            Stream uncompressedTarget, 
            CompressionOptions? options = null
            )
        {
            options = options?.Clone() ?? DefaultOptions;
            if (options.FlagsIncluded) options.Flags = (CompressionFlags)compressedSource.ReadOneByte();
            int? serializerVersion = null;
            if (options.SerializerVersionIncluded)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
                try
                {
                    if (compressedSource.Read(buffer.AsSpan(0, sizeof(int))) != sizeof(int)) throw new IOException("Failed to read serializer version number");
                    serializerVersion = buffer.AsSpan(0, sizeof(int)).ToInt();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                if (serializerVersion < 1 || serializerVersion > StreamSerializer.VERSION) throw new InvalidDataException($"Unsupported serializer version #{serializerVersion}");
                options.SerializerVersion = serializerVersion;
            }
            if (options.AlgorithmIncluded && compressedSource.ReadNumber<int>(serializerVersion) != Value) throw new InvalidDataException("Compression algorithm mismatch");
            long len = -1;
            if (options.UncompressedLengthIncluded)
            {
                len = compressedSource.ReadNumber<long>(serializerVersion);
                if (len < 0) throw new InvalidDataException($"Invalid uncompressed data length ({len})");
                options.UncompressedDataLength = len;
            }
            return (options, serializerVersion, len);
        }

        /// <summary>
        /// Read the options
        /// </summary>
        /// <param name="compressedSource">Source stream</param>
        /// <param name="uncompressedTarget">Target stream</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Red options, serializer version and the uncompressed data length</returns>
        public virtual async Task<(CompressionOptions Options, int? SerializerVersion, long UncompressedDataLength)> ReadOptionsAsync(
            Stream compressedSource, 
            Stream uncompressedTarget, 
            CompressionOptions? options = null,
            CancellationToken cancellationToken = default
            )
        {
            options = options?.Clone() ?? DefaultOptions;
            if (options.FlagsIncluded) options.Flags = (CompressionFlags)compressedSource.ReadOneByte();
            int? serializerVersion = null;
            if (options.SerializerVersionIncluded)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
                try
                {
                    if (await compressedSource.ReadAsync(buffer.AsMemory(0, sizeof(int)), cancellationToken).DynamicContext() != sizeof(int))
                        throw new IOException("Failed to read serializer version number");
                    serializerVersion = buffer.AsSpan(0, sizeof(int)).ToInt();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                if (serializerVersion < 1 || serializerVersion > StreamSerializer.VERSION) throw new InvalidDataException($"Unsupported serializer version #{serializerVersion}");
                options.SerializerVersion = serializerVersion;
            }
            if (options.AlgorithmIncluded && await compressedSource.ReadNumberAsync<int>(serializerVersion, cancellationToken: cancellationToken).DynamicContext() != Value)
                throw new InvalidDataException("Compression algorithm mismatch");
            long len = -1;
            if (options.UncompressedLengthIncluded)
            {
                len = await compressedSource.ReadNumberAsync<long>(serializerVersion, cancellationToken: cancellationToken).DynamicContext();
                if (len < 0) throw new InvalidDataException($"Invalid uncompressed data length ({len})");
                options.UncompressedDataLength = len;
            }
            return (options, serializerVersion, len);
        }
    }
}
