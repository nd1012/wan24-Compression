using wan24.Core;
using wan24.StreamSerializerExtensions;

namespace wan24.Compression
{
    /// <summary>
    /// Archive decompression (extraction is possible on the fly during download f.e.)
    /// </summary>
    public partial class ArchiveDecompression : DisposableBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="version">Archive version</param>
        /// <param name="options">Options</param>
        /// <param name="chunkSize">Chunk size for compressed streams</param>
        /// <param name="leaveOpen">If to leave the <c>source</c> open when disposing</param>
        /// <param name="maxKeyLength">Maximum item key / (file) path length in bytes</param>
        protected ArchiveDecompression(
            in Stream source, 
            in byte version, 
            in CompressionOptions options, 
            in int chunkSize,
            in bool leaveOpen, 
            in int? maxKeyLength
            )
            : base()
        {
            Source = source;
            Version = version;
            Options = CompressionHelper.GetDefaultOptions(options) with
            {
                LeaveOpen = true
            };
            SerializerVersion = Options.CustomSerializerVersion ?? throw new InvalidDataException("Missing serializer version");
            LeaveOpen = leaveOpen;
            MaxKeyLength = maxKeyLength ?? Options.MaxKeyLength;
            ChunkSize = chunkSize;
        }

        /// <summary>
        /// Default maximum chunk size for a compressed stream in bytes
        /// </summary>
        public static int MaxChunkSize { get; set; } = Settings.BufferSize;

        /// <summary>
        /// Source
        /// </summary>
        public Stream Source { get; }

        /// <summary>
        /// Archive version
        /// </summary>
        public byte Version { get; }

        /// <summary>
        /// Options
        /// </summary>
        public CompressionOptions Options { get; }

        /// <summary>
        /// Serializer version number
        /// </summary>
        public int SerializerVersion { get; protected set; }

        /// <summary>
        /// If to leave the <see cref="Source"/> open when disposing
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// Maximum item key / (file) path length in bytes
        /// </summary>
        public int MaxKeyLength { get; set; }

        /// <summary>
        /// Chunk size for a compressed stream in bytes
        /// </summary>
        public int ChunkSize { get; }

        /// <summary>
        /// Read the next item information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Item information (the value must be red completly before reading the next item information; don't forget to dispose)</returns>
        public virtual async Task<ArchiveItemInfo> ReadItemInfoAsync(CancellationToken cancellationToken)
        {
            EnsureUndisposed();
            ArchiveItemTypes type = await ReadItemFlagsAsync(cancellationToken).DynamicContext();
            if (type == ArchiveItemTypes.None) return await CreateEmptyItemInfoAsync(cancellationToken).DynamicContext();
            string key = await ReadItemKeyAsync(type, cancellationToken).DynamicContext();
            (ArchiveItemTypes typeOnly, bool includesOptions, bool isCompressed, bool isChunked, bool isEmpty, CompressionOptions options) =
                await GetItemOptionsAsync(type, key, cancellationToken).DynamicContext();
            long? len = await GetItemLengthAsync(type, typeOnly, key, includesOptions, isCompressed, isChunked, isEmpty, options, cancellationToken).DynamicContext();
            if (isEmpty || typeOnly == ArchiveItemTypes.Folder)
                return await CreateNonValueItemInformationAsync(type, typeOnly, key, includesOptions, isCompressed, isChunked, isEmpty, options, len, cancellationToken)
                    .DynamicContext();
            return isCompressed
                ? await CreateCompressedItemInformationAsync(type, typeOnly, key, includesOptions, isCompressed, isChunked, isEmpty, options, len, cancellationToken)
                    .DynamicContext()
                : await CreateUncompressedItemInformationAsync(type, typeOnly, key, includesOptions, isCompressed, isChunked, isEmpty, options, len, cancellationToken)
                    .DynamicContext();
        }

        /// <summary>
        /// Extract the archive contents 
        /// </summary>
        /// <param name="path">Target path (folder will be created, if not exists)</param>
        /// <param name="overwrite">If to overwrite existing files</param>
        /// <param name="maxKeyValueLength">Maximum key/value value length in bytes</param>
        /// <param name="itemInfoHandler">Item information handler</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Key/values</returns>
        public virtual async Task<Dictionary<string, byte[]>> ExtractToAsync(
            string path,
            bool overwrite = false,
            int maxKeyValueLength = ushort.MaxValue,
            ItemInfoHandler_Delegate? itemInfoHandler = null,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path)) FsHelper.CreateFolder(path);
            bool hasItemInfoHandler = itemInfoHandler is not null;
            Dictionary<string, byte[]> res = [];
            ArchiveItemInfo info;
            while (true)
            {
                Console.WriteLine($"READ FROM {Source.Position}");
                info = await ReadItemInfoAsync(cancellationToken).DynamicContext();
                try
                {
                    Console.WriteLine($"ITEM {info.Type} {info.Key} {info.Length}");
                    if (info.Type == ArchiveItemTypes.None) return res;
                    if (hasItemInfoHandler && !await itemInfoHandler!(this, info, cancellationToken).DynamicContext()) continue;
                    if (!await HandleItemInfoAsync(info, cancellationToken).DynamicContext()) continue;
                    switch (info.Type & ~ArchiveItemTypes.FLAGS)
                    {
                        case ArchiveItemTypes.File:
                            await ExtractFileAsync(path, info, overwrite, cancellationToken).DynamicContext();
                            break;
                        case ArchiveItemTypes.Folder:
                            await ExtractFolderAsync(path, info, cancellationToken).DynamicContext();
                            break;
                        case ArchiveItemTypes.KeyValue:
                            res[info.Key] = await ExtractValueAsync(info, maxKeyValueLength, cancellationToken).DynamicContext();
                            break;
                        default:
                            throw new InvalidProgramException($"Invalid item type \"{info.Type}\"");
                    }
                }
                finally
                {
                    await info.DisposeAsync().DynamicContext();
                }
            }
        }

        /// <summary>
        /// Extract a file
        /// </summary>
        /// <param name="path">Target base path</param>
        /// <param name="info">Info (won't be disposed)</param>
        /// <param name="overwrite">If to overwrite an existing file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task ExtractFileAsync(
            string path,
            ArchiveItemInfo info,
            bool overwrite = false,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            if (!info.IsFile) throw new InvalidOperationException("Not a file");
            string name = Path.Combine(Path.GetFullPath(path), info.Key[1..]);
            if (File.Exists(name))
            {
                if (!overwrite) throw new IOException($"Won't overwrite existing file \"{name}\"");
                File.Delete(name);
            }
            else if (Directory.Exists(name))
            {
                throw new IOException($"Can't overwrite existing folder \"{name}\"");
            }
            FileStream fs = FsHelper.CreateFileStream(name);
            await using (fs.DynamicContext())
                await ExtractFileAsync(fs, info, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Extract a file
        /// </summary>
        /// <param name="target">Target stream (won't be disposed)</param>
        /// <param name="info">Info (won't be disposed)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task ExtractFileAsync(
            Stream target,
            ArchiveItemInfo info,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            if (!info.IsFile) throw new InvalidOperationException("Not a file");
            if (!info.IsEmpty)
                await info.Value.CopyToAsync(target, cancellationToken).DynamicContext();
            if (info.Value is not null) await info.Value.CopyToAsync(Stream.Null, cancellationToken).DynamicContext();//FIXME Won't work
        }

        /// <summary>
        /// Extract a folder
        /// </summary>
        /// <param name="path">Target base path</param>
        /// <param name="info">Info (won't be disposed)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual Task ExtractFolderAsync(
            string path,
            ArchiveItemInfo info,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            if (!info.IsFolder) throw new InvalidOperationException("Not a folder");
            string name = Path.Combine(Path.GetFullPath(path), info.Key[1..]);
            if (File.Exists(name)) throw new IOException($"Folder \"{name}\" is an existing file");
            if (!Directory.Exists(name)) FsHelper.CreateFolder(name);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Extract a value
        /// </summary>
        /// <param name="info">Info (won't be disposed)</param>
        /// <param name="maxKeyValueLength">Maximum key/value value length in bytes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value</returns>
        public virtual async Task<byte[]> ExtractValueAsync(
            ArchiveItemInfo info,
            int maxKeyValueLength = ushort.MaxValue,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            if (!info.IsKeyValue) throw new InvalidOperationException("Not a key/value");
            // Handle an empty value
            if (info.IsEmpty) return [];
            // Handle a chunked value
            if (info.IsChunked)
            {
                using MemoryPoolStream ms = new();
                using LimitedLengthStream limitedMs = new(ms, maxLength: maxKeyValueLength);
                await info.Value.CopyToAsync(limitedMs, cancellationToken).DynamicContext();
                return ms.ToArray();
            }
            // Handle an unchunked value
            if (!info.Length.HasValue) throw new InvalidProgramException();
            if (info.Length.Value > maxKeyValueLength)
                throw new InvalidDataException($"Value length of {info.Length} bytes exceeds the max. length of {maxKeyValueLength} bytes");
            byte[] value = new byte[info.Length.Value];
            await info.Value.ReadExactlyAsync(value, cancellationToken).DynamicContext();
            return value;
        }

        /// <summary>
        /// Delegate for an archive item information handler
        /// </summary>
        /// <param name="decompression">Archive decompression</param>
        /// <param name="info">Item information (will be disposed)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>If the item should be processed</returns>
        public delegate Task<bool> ItemInfoHandler_Delegate(ArchiveDecompression decompression, ArchiveItemInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Create from a stream
        /// </summary>
        /// <param name="source">Compressed archive source stream</param>
        /// <param name="uncompressedLengthIncluded">If the item header / compression options include the uncompressed value length, if possible</param>
        /// <param name="leaveOpen">If to leave the <c>source</c> open when disposing</param>
        /// <param name="maxKeyLength">Maximum item key / (file) path length in bytes</param>
        /// <param name="options">Default compression options to use for reading the archive header</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Instance</returns>
        public static async Task<ArchiveDecompression> CreateFromAsync(
            Stream source,
            bool uncompressedLengthIncluded = true,
            bool leaveOpen = false,
            int? maxKeyLength = null,
            CompressionOptions? options = null,
            CancellationToken cancellationToken = default
            )
        {
            try
            {
                int version = source.ReadByte();
                if (version < 0) throw new InvalidDataException("Failed to read the archive version");
                if (version < 1) throw new InvalidDataException("Invalid archive version");
                if (version > ArchiveCompression.VERSION) throw new InvalidDataException($"Unsupported archive version #{version}");
                options ??= CompressionHelper.GetDefaultOptions();
                options.UncompressedLengthIncluded = false;
                options.SerializerVersionIncluded = true;
                options = await CompressionHelper.ReadOptionsAsync(source, Stream.Null, options, cancellationToken).DynamicContext();
                if (!options.SerializerVersionIncluded) throw new InvalidDataException("Compression options without serializer version");
                options.UncompressedLengthIncluded = uncompressedLengthIncluded;
                int chunkSize = await source.ReadNumberAsync<int>(
                    options.CustomSerializerVersion ?? throw new InvalidProgramException(), 
                    cancellationToken: cancellationToken
                    ).DynamicContext();
                if (chunkSize < 1 || chunkSize > MaxChunkSize) throw new InvalidDataException($"Invalid chuk size {chunkSize} bytes");
                return new(source, (byte)version, options, chunkSize, leaveOpen, maxKeyLength);
            }
            catch
            {
                if (!leaveOpen) await source.DisposeAsync().DynamicContext();
                throw;
            }
        }
    }
}
