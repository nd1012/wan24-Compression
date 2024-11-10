using System.Diagnostics.Contracts;
using System.Text;
using wan24.Core;
using wan24.StreamSerializerExtensions;

namespace wan24.Compression
{
    // Internals
    public partial class ArchiveCompression
    {
        /// <summary>
        /// If the archive header was written
        /// </summary>
        protected bool ArchiveHeaderWritten = false;

        /// <summary>
        /// Write an item
        /// </summary>
        /// <param name="type">Item type</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual async Task WriteItemAsync(
            ArchiveItemTypes type,
            string key,
            Stream? value = null,
            CompressionOptions? options = null,
            bool uncompressed = false,
            CancellationToken cancellationToken = default
            )
        {
            ValidateItemKeyLength(key);
            (type, bool writeOptions, bool isEmpty, bool seekable) = await GetItemOptionsAsync(type, key, value, options, uncompressed, cancellationToken).DynamicContext();
            await WriteArchiveHeaderAsync(cancellationToken).DynamicContext();
            Console.WriteLine($"WRITE {key} TO {Target.Position}");
            await WriteItemHeaderAsync(key, value, type, writeOptions, isEmpty, seekable, options, uncompressed, cancellationToken).DynamicContext();
            if (isEmpty)
            {
                Console.WriteLine($"WROTE {key} UNTIL {Target.Position}");
                return;
            }
            Contract.Assert(value is not null);
            if (uncompressed)
            {
                await WriteItemUncompressedAsync(key, value, type, isEmpty, seekable, options, cancellationToken).DynamicContext();
                Console.WriteLine($"WROTE {key} UNTIL {Target.Position}");
                return;
            }
            options = await GetItemCompressionOptionsAsync(key, value, type, writeOptions, isEmpty, seekable, options, cancellationToken).DynamicContext();
            if (writeOptions) await WriteItemCompressionOptionsAsync(key, value, type, writeOptions, isEmpty, seekable, options, cancellationToken).DynamicContext();
            await WriteItemCompressedAsync(key, value, type, writeOptions, isEmpty, seekable, options, cancellationToken).DynamicContext();
            Console.WriteLine($"WROTE {key} UNTIL {Target.Position}");
        }

        /// <summary>
        /// Validate the key / (file) path length
        /// </summary>
        /// <param name="key">Key / (file) path</param>
        protected virtual void ValidateItemKeyLength(in string key)
        {
            using RentedMemory<byte> buffer = new(len: Encoding.UTF8.GetMaxByteCount(key.Length), clean: false);
            int len = key.GetBytes(buffer.Memory.Span);
            if (len > MaxKeyLength)
                throw new ArgumentOutOfRangeException(nameof(key), $"Key / (file) path length of {len} bytes exceeds the maximum length of {MaxKeyLength} bytes");
        }

        /// <summary>
        /// Get item options
        /// </summary>
        /// <param name="type">Item type</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual Task<(ArchiveItemTypes Type, bool WriteOptions, bool IsEmpty, bool Seekable)> GetItemOptionsAsync(
            ArchiveItemTypes type,
            string key,
            Stream? value,
            CompressionOptions? options,
            bool uncompressed,
            CancellationToken cancellationToken
            )
        {
            bool writeOptions = options is not null,
                isEmpty = type == ArchiveItemTypes.Folder,
                seekable = value?.CanSeek ?? false;
            if (!isEmpty)
            {
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                if (seekable && value.GetRemainingBytes() < 1)
                {
                    type |= ArchiveItemTypes.Empty;
                    isEmpty = true;
                }
                else if (!seekable)
                {
                    type |= ArchiveItemTypes.Chunked;
                }
                if (!isEmpty)
                    if (uncompressed)
                    {
                        type |= ArchiveItemTypes.Uncompressed;
                    }
                    else if (writeOptions)
                    {
                        type |= ArchiveItemTypes.Options;
                    }
            }
            (ArchiveItemTypes Type, bool WriteOptions, bool IsEmpty, bool Seekable) res = (type, writeOptions, isEmpty, seekable);
            return Task.FromResult(res);
        }

        /// <summary>
        /// Write the archive header
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual async Task WriteArchiveHeaderAsync(CancellationToken cancellationToken)
        {
            if (ArchiveHeaderWritten) return;
            Target.WriteByte(VERSION);
            await CompressionHelper.WriteOptionsAsync(
                Stream.Null,
                Target,
                Options with
                {
                    UncompressedLengthIncluded = false,
                    SerializerVersionIncluded = true
                },
                cancellationToken
                ).DynamicContext();
            await Target.WriteNumberAsync(ChunkSize, cancellationToken).DynamicContext();
            ArchiveHeaderWritten = true;
        }

        /// <summary>
        /// Write an item header
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="type">Item type</param>
        /// <param name="writeOptions">If to write compression options</param>
        /// <param name="isEmpty">If the value stream is empty (or <see langword="null"/>)</param>
        /// <param name="seekable">If the value stream is seekable</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If to write the value uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual async Task WriteItemHeaderAsync(
            string key,
            Stream? value,
            ArchiveItemTypes type,
            bool writeOptions,
            bool isEmpty,
            bool seekable,
            CompressionOptions? options,
            bool uncompressed,
            CancellationToken cancellationToken
            )
        {
            Target.WriteByte((byte)type);
            await Target.WriteStringAsync(key, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Write the item uncompressed
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="type">Item type</param>
        /// <param name="isEmpty">If the value stream is empty</param>
        /// <param name="seekable">If the value stream is seekable</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual async Task WriteItemUncompressedAsync(
            string key,
            Stream value,
            ArchiveItemTypes type,
            bool isEmpty,
            bool seekable,
            CompressionOptions? options,
            CancellationToken cancellationToken
            )
        {
            if (seekable)
            {
                // Unchunked
                await Target.WriteNumberAsync(value.GetRemainingBytes(), cancellationToken).DynamicContext();
                await value.CopyToAsync(Target, cancellationToken).DynamicContext();
            }
            else
            {
                // Chunked
                options ??= Options;
                using ChunkStream chunker = ChunkStream.CreateNew(
                    Target,
                    options.ChunkSize < 1
                        ? CompressionOptions.DefaultChunkSize
                        : options.ChunkSize,
                    leaveOpen: true
                    );
                await value.CopyToAsync(chunker, cancellationToken).DynamicContext();
                await chunker.FlushFinalChunkAsync(cancellationToken).DynamicContext();
            }
        }

        /// <summary>
        /// Get the item compression options
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="type">Item type</param>
        /// <param name="writeOptions">If to write compression options</param>
        /// <param name="isEmpty">If the value stream is empty</param>
        /// <param name="seekable">If the value stream is seekable</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item compression options</returns>
        protected virtual Task<CompressionOptions> GetItemCompressionOptionsAsync(
            string key,
            Stream value,
            ArchiveItemTypes type,
            bool writeOptions,
            bool isEmpty,
            bool seekable,
            CompressionOptions? options,
            CancellationToken cancellationToken
            )
        {
            options = (options ?? Options) with
            {
                LeaveOpen = true,
                UncompressedLengthIncluded = writeOptions && seekable
            };
            if (seekable)
            {
                options.ChunkSize = 0;
            }
            else if (options.ChunkSize < 1)
            {
                options.ChunkSize = CompressionOptions.DefaultChunkSize;
            }
            return Task.FromResult(options);
        }

        /// <summary>
        /// Write the item compression options
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="type">Item type</param>
        /// <param name="writeOptions">If to write compression options</param>
        /// <param name="isEmpty">If the value stream is empty</param>
        /// <param name="seekable">If the value stream is seekable</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual async Task WriteItemCompressionOptionsAsync(
            string key,
            Stream value,
            ArchiveItemTypes type,
            bool writeOptions,
            bool isEmpty,
            bool seekable,
            CompressionOptions options,
            CancellationToken cancellationToken
            )
            => await CompressionHelper.WriteOptionsAsync(value, Target, options, cancellationToken).DynamicContext();


        /// <summary>
        /// Write the item compressed
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value stream</param>
        /// <param name="type">Item type</param>
        /// <param name="writeOptions">If to write compression options</param>
        /// <param name="isEmpty">If the value stream is empty</param>
        /// <param name="seekable">If the value stream is seekable</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        protected virtual async Task WriteItemCompressedAsync(
            string key,
            Stream value,
            ArchiveItemTypes type,
            bool writeOptions,
            bool isEmpty,
            bool seekable,
            CompressionOptions options,
            CancellationToken cancellationToken
            )
        {
            if (options.ChunkSize < 1 && (!writeOptions || !options.UncompressedLengthIncluded))
                await Target.WriteNumberAsync(value.GetRemainingBytes(), cancellationToken).DynamicContext();
            if (options.ChunkSize > 0)
            {
                // Chunked
                ChunkStream chunker = await ChunkStream.CreateNewAsync(Target, options.ChunkSize, cancellationToken: cancellationToken).DynamicContext();
                await using (chunker.DynamicContext())
                {
                    Stream compression = CompressionHelper.GetCompressionStream(chunker, options);
                    await using (compression.DynamicContext())
                        await value.CopyToAsync(compression, cancellationToken).DynamicContext();
                    await chunker.FlushFinalChunkAsync(cancellationToken).DynamicContext();
                }
            }
            else
            {
                // Unchunked
                Stream compression = CompressionHelper.GetCompressionStream(Target, options);
                await using (compression.DynamicContext())
                    await value.CopyToAsync(compression, cancellationToken).DynamicContext();
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!LeaveOpen) Target.Dispose();
        }

        /// <inheritdoc/>
        protected override async Task DisposeCore()
        {
            if (!LeaveOpen) await Target.DisposeAsync().DynamicContext();
        }
    }
}
