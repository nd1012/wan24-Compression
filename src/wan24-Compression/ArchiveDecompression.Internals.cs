using wan24.Core;
using wan24.StreamSerializerExtensions;

namespace wan24.Compression
{
    // Internals
    public partial class ArchiveDecompression
    {
        /// <summary>
        /// Read and validate item flags
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item flags</returns>
        protected virtual Task<ArchiveItemTypes> ReadItemFlagsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int typeFlags = Source.ReadByte();
            if (typeFlags < -1) throw new InvalidDataException("Failed to read the item type flags");
            ArchiveItemTypes res = (ArchiveItemTypes)typeFlags;
            if (!res.IsValid()) throw new InvalidDataException($"Invalid item flags value \"{res}\"");
            return Task.FromResult(res);
        }

        /// <summary>
        /// Create none-item information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>None-item information</returns>
        protected virtual Task<ArchiveItemInfo> CreateEmptyItemInfoAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ArchiveItemInfo()
            {
                Type = ArchiveItemTypes.None,
                Key = string.Empty,
                Options = Options
            });

        /// <summary>
        /// Read and validate the item key
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key</returns>
        protected virtual async Task<string> ReadItemKeyAsync(ArchiveItemTypes type, CancellationToken cancellationToken)
        {
            string res = await Source.ReadStringAsync(SerializerVersion, minLen: 1, maxLen: MaxKeyLength, cancellationToken: cancellationToken).DynamicContext();
            //TODO Validate key for a file or folder
            return res;
        }

        /// <summary>
        /// Get and validate the item options
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="key">Key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Options</returns>
        protected virtual async Task<(ArchiveItemTypes TypeOnly, bool IncludesOptions, bool IsCompressed, bool IsChunked, bool IsEmpty, CompressionOptions Options)>
            GetItemOptionsAsync(ArchiveItemTypes type, string key, CancellationToken cancellationToken)
        {
            bool isEmpty = (type & ArchiveItemTypes.Empty) == ArchiveItemTypes.Empty, 
                includesOptions = !isEmpty && (type & ArchiveItemTypes.Options) == ArchiveItemTypes.Options,
                isChunked = !isEmpty && (type & ArchiveItemTypes.Chunked) == ArchiveItemTypes.Chunked,
                isCompressed = !isEmpty && (type & ArchiveItemTypes.Uncompressed) != ArchiveItemTypes.Uncompressed;
            ArchiveItemTypes typeOnly = type & ~ArchiveItemTypes.FLAGS;
            if (includesOptions && typeOnly == ArchiveItemTypes.Folder) throw new InvalidDataException("Folder item type includes compression options");
            CompressionOptions options = includesOptions
                ? await CompressionHelper.ReadOptionsAsync(
                    Source,
                    Stream.Null,
                    Options,
                    cancellationToken
                    ).DynamicContext()
                : Options with { };
            options.LeaveOpen = true;
            if (includesOptions && !isChunked && !options.UncompressedLengthIncluded)
                throw new InvalidDataException("Missing uncompressed value length in the compression options");
            if (typeOnly == ArchiveItemTypes.Folder && (isChunked || isCompressed || isEmpty))
                throw new InvalidDataException($"Invalid item flags \"{type}\" for a folder");
            (ArchiveItemTypes TypeOnly, bool IncludesOptions, bool IsCompressed, bool IsChunked, bool IsEmpty, CompressionOptions Options) res =
                (typeOnly, includesOptions, isCompressed, isChunked, isEmpty, options);
            return res;
        }

        /// <summary>
        /// Get the item length
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="typeOnly">Type without flags</param>
        /// <param name="key">Key</param>
        /// <param name="includesOptions">If options are included</param>
        /// <param name="isCompressed">If compressed</param>
        /// <param name="isChunked">If chunked</param>
        /// <param name="isEmpty">If empty</param>
        /// <param name="options">Options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item length in bytes or <see langword="null"/>, if empty or chunked</returns>
        protected virtual Task<long?> GetItemLengthAsync(
            ArchiveItemTypes type,
            ArchiveItemTypes typeOnly,
            string key,
            bool includesOptions,
            bool isCompressed,
            bool isChunked,
            bool isEmpty,
            CompressionOptions options,
            CancellationToken cancellationToken
            )
        {
            long? len = null;
            switch (typeOnly)
            {
                case ArchiveItemTypes.File:
                case ArchiveItemTypes.KeyValue:
                    if (!includesOptions && !isChunked && !isEmpty)
                    {
                        // Read the length later when having the decompression stream
                    }
                    else if (isCompressed && !isChunked)
                    {
                        if (!options.UncompressedLengthIncluded) throw new InvalidDataException("Missing uncompressed value length");
                        len = options.UncompressedDataLength;
                    }
                    else if (!isChunked && !isEmpty)
                    {
                        throw new InvalidDataException("Value length expected");
                    }
                    break;
                case ArchiveItemTypes.Folder:
                    break;
                default:
                    throw new InvalidDataException($"Unsupported item flags \"{type}\"");
            }
            return Task.FromResult(len);
        }

        /// <summary>
        /// Create non-value item information
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="typeOnly">Type without flags</param>
        /// <param name="key">Key</param>
        /// <param name="includesOptions">If options are included</param>
        /// <param name="isCompressed">If compressed</param>
        /// <param name="isChunked">If chunked</param>
        /// <param name="isEmpty">If empty</param>
        /// <param name="options">Options</param>
        /// <param name="len">Value lengt in bytes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item information</returns>
        protected virtual Task<ArchiveItemInfo> CreateNonValueItemInformationAsync(
            ArchiveItemTypes type,
            ArchiveItemTypes typeOnly,
            string key,
            bool includesOptions,
            bool isCompressed,
            bool isChunked,
            bool isEmpty,
            CompressionOptions options,
            long? len,
            CancellationToken cancellationToken
            )
            => Task.FromResult(new ArchiveItemInfo()
            {
                Type = type,
                Key = key,
                Options = options
            });

        /// <summary>
        /// Create compressed item information
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="typeOnly">Type without flags</param>
        /// <param name="key">Key</param>
        /// <param name="includesOptions">If options are included</param>
        /// <param name="isCompressed">If compressed</param>
        /// <param name="isChunked">If chunked</param>
        /// <param name="isEmpty">If empty</param>
        /// <param name="options">Options</param>
        /// <param name="len">Value lengt in bytes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item information</returns>
        protected virtual async Task<ArchiveItemInfo> CreateCompressedItemInformationAsync(
            ArchiveItemTypes type,
            ArchiveItemTypes typeOnly,
            string key,
            bool includesOptions,
            bool isCompressed,
            bool isChunked,
            bool isEmpty,
            CompressionOptions options,
            long? len,
            CancellationToken cancellationToken
            )
        {
            // Ensure having a valid value length
            if (!isChunked)
                if (!includesOptions || !options.UncompressedLengthIncluded)
                {
                    if (len.HasValue) throw new InvalidProgramException();
                    len = await Source.ReadNumberAsync<long>(SerializerVersion, cancellationToken: cancellationToken).DynamicContext();
                    if (len.Value < 1) throw new InvalidDataException($"Invalid uncompressed value length {len}");
                }
                else if (!len.HasValue)
                {
                    throw new InvalidProgramException();
                }
            // Return an unchunked (but length limited) compression stream
            if (!isChunked)
                return new()
                {
                    Type = type,
                    Key = key,
                    Options = options,
                    Length = len,
                    Value = new LimitedLengthStream(CompressionHelper.GetDecompressionStream(Source, options), maxLength: len ?? throw new InvalidProgramException())
                };
            // Return a chunked compression stream
            ChunkStream chunker = await ChunkStream.FromExistingAsync(Source, cancellationToken: cancellationToken).DynamicContext();
            try
            {
                if (chunker.ChunkSize > MaxChunkSize)
                    throw new OutOfMemoryException($"Chunked compression stream with chunk size {chunker.ChunkSize} bytes exceeds max. chunk size of {MaxChunkSize} bytes");
                return new()
                {
                    Type = type,
                    Key = key,
                    Options = options,
                    Length = len,
                    Value = CompressionHelper.GetDecompressionStream(chunker, options)
                };
            }
            catch
            {
                await chunker.DisposeAsync().DynamicContext();
                throw;
            }
        }

        /// <summary>
        /// Create uncompressed item information
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="typeOnly">Type without flags</param>
        /// <param name="key">Key</param>
        /// <param name="includesOptions">If options are included</param>
        /// <param name="isCompressed">If compressed</param>
        /// <param name="isChunked">If chunked</param>
        /// <param name="isEmpty">If empty</param>
        /// <param name="options">Options</param>
        /// <param name="len">Value lengt in bytes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item information</returns>
        protected virtual async Task<ArchiveItemInfo> CreateUncompressedItemInformationAsync(
            ArchiveItemTypes type,
            ArchiveItemTypes typeOnly,
            string key,
            bool includesOptions,
            bool isCompressed,
            bool isChunked,
            bool isEmpty,
            CompressionOptions options,
            long? len,
            CancellationToken cancellationToken
            )
            => new()
            {
                Type = type,
                Key = key,
                Options = options,
                Length = len,
                Value = isChunked
                    ? await ChunkStream.FromExistingAsync(
                        Source,
                        chunkSize: options.ChunkSize < 1
                            ? CompressionOptions.DefaultChunkSize
                            : options.ChunkSize,
                        leaveOpen: true,
                        cancellationToken: cancellationToken
                        ).DynamicContext()
                    : new LimitedLengthStream(Source, maxLength: len ?? throw new InvalidProgramException(), leaveOpen: true)
            };

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!LeaveOpen) Source.Dispose();
        }

        /// <inheritdoc/>
        protected override async Task DisposeCore()
        {
            if (!LeaveOpen) await Source.DisposeAsync().DynamicContext();
        }
    }
}
