using wan24.Core;
using wan24.StreamSerializerExtensions;

//TODO Add AddFolderAsync (with the possibility to define an archive target root)
//TODO Add AddFsItemsAsync (for IEnumerable<string>)

namespace wan24.Compression
{
    /// <summary>
    /// Archive compression (compression and extraction is possible on the fly during up-/download f.e.)
    /// </summary>
    public partial class ArchiveCompression : DisposableBase
    {
        /// <summary>
        /// Version
        /// </summary>
        public const byte VERSION = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">Target stream (will be disposed)</param>
        /// <param name="options">Options</param>
        /// <param name="leaveOpen">If to leave the <c>target</c> stream open when disposing</param>
        public ArchiveCompression(in Stream target, in CompressionOptions? options = null, in bool leaveOpen = false) : base()
        {
            Target = target;
            Options = CompressionHelper.GetDefaultOptions(options) with
            {
                LeaveOpen = true,
                CustomSerializerVersion = StreamSerializer.Version,
                AlgorithmIncluded = true,
                FlagsIncluded = true
            };
            LeaveOpen = leaveOpen;
            MaxKeyLength = Options.MaxKeyLength;
        }

        /// <summary>
        /// Default chunk size for a compressed stream in bytes
        /// </summary>
        public static int DefaultChunkSize { get; set; } = Settings.BufferSize;

        /// <summary>
        /// Target
        /// </summary>
        public Stream Target { get; }

        /// <summary>
        /// Options (used as default for the archive)
        /// </summary>
        public CompressionOptions Options { get; }

        /// <summary>
        /// If to leave the <see cref="Target"/> open when disposing
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// Maximum item key / (file) path length in bytes
        /// </summary>
        public int MaxKeyLength { get; set; }

        /// <summary>
        /// Chunk size for a compressed stream in bytes
        /// </summary>
        public int ChunkSize { get; set; } = DefaultChunkSize;

        /// <summary>
        /// Add a file
        /// </summary>
        /// <param name="path">Absolute item path including filename</param>
        /// <param name="source">Source stream</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">Uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddFileAsync(
            string path,
            Stream source,
            CompressionOptions? options = null,
            bool uncompressed = false,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            //TODO Validate the path
            await WriteItemAsync(ArchiveItemTypes.File, path, source, options, uncompressed, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Add a folder
        /// </summary>
        /// <param name="path">Absolute item path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddFolderAsync(string path, CancellationToken cancellationToken = default)
        {
            EnsureUndisposed();
            //TODO Validate the path
            await WriteItemAsync(ArchiveItemTypes.Folder, path, cancellationToken: cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Add a key/value
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="value">Source stream</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddKeyValueAsync(
            string key,
            Stream value,
            CompressionOptions? options = null,
            bool uncompressed = false,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            await WriteItemAsync(ArchiveItemTypes.KeyValue, key, value, options, uncompressed, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Add a key/value
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="value">Value</param>
        /// <param name="offset">Value byte offset</param>
        /// <param name="length">Value length (or <c>-1</c> to use the available <c>value</c> length from the given <c>offset</c>)</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddKeyValueAsync(
            string key,
            byte[] value,
            int offset = 0,
            int length = -1,
            CompressionOptions? options = null,
            bool uncompressed = false,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            if (length < 0) length = value.Length - offset;
            value.AsSpan().EnsureValid(offset, length);
            using MemoryStream ms = new(value, offset, length, writable: false);
            await WriteItemAsync(ArchiveItemTypes.KeyValue, key, ms, options, uncompressed, cancellationToken).DynamicContext();
        }
    }
}
