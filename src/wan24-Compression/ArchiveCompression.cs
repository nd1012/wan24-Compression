using wan24.Core;
using wan24.StreamSerializerExtensions;

namespace wan24.Compression
{
    /// <summary>
    /// Archive compression (compression and extraction is possible on the fly during up-/download f.e.)
    /// </summary>
    public partial class ArchiveCompression : DisposableBase
    {
        /// <summary>
        /// Wildcard search pattern
        /// </summary>
        protected const string WILDCARD = "*";

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
        /// <param name="source">Source stream (should be seekable to avoid chunking)</param>
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
        /// Add a folder
        /// </summary>
        /// <param name="path">Absolute local path</param>
        /// <param name="root">Item root path</param>
        /// <param name="recursive">If to recurse into sub-folders</param>
        /// <param name="includeEmptyFolders">If to include empty folders</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If not to compress files</param>
        /// <param name="enumerationOptions">Filesystem enumeration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddFolderAsync(
            string path,
            string root = "/",
            bool recursive = true,
            bool includeEmptyFolders = true,
            CompressionOptions? options = null,
            bool uncompressed = false,
            EnumerationOptions? enumerationOptions = null,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            // Validate parameters
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException();
            //TODO Validate item root path
            // Find all folders to investigate
            Queue<string> folders = [];
            folders.Enqueue(path);
            if (enumerationOptions is not null) enumerationOptions.ReturnSpecialDirectories = false;
            if (recursive)
                foreach (string folder in Directory.EnumerateDirectories(path, searchPattern: WILDCARD, enumerationOptions ?? new EnumerationOptions()
                {
                    RecurseSubdirectories = recursive
                }))
                    folders.Enqueue(folder);
            // Add folders and files
            IEnumerable<string> files;
            FileStream fs;
            if (enumerationOptions is not null)
            {
                enumerationOptions.RecurseSubdirectories = false;
            }
            else
            {
                enumerationOptions = new();
            }
            while (folders.TryDequeue(out string? folder))
            {
                files = Directory.EnumerateFiles(folder, searchPattern: WILDCARD, enumerationOptions);
                // Handle an empty folder
                if (!files.Any() && !Directory.EnumerateDirectories(folder, searchPattern: WILDCARD, enumerationOptions).Any())
                {
                    if (includeEmptyFolders)
                        await AddFolderAsync(FsHelper.NormalizeLinuxDisplayPath(Path.Combine(root, folder[path.Length..])), cancellationToken).DynamicContext();
                    continue;
                }
                // Add files from the current folder
                foreach (string file in files)
                {
                    fs = FsHelper.CreateFileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan | FileOptions.Asynchronous);
                    await using (fs.DynamicContext())
                        await AddFileAsync(
                            FsHelper.NormalizeLinuxDisplayPath(Path.Combine(root, file[path.Length..])),
                            fs,
                            options,
                            uncompressed,
                            cancellationToken
                            )
                            .DynamicContext();
                }
            }
        }

        /// <summary>
        /// Add filesystem items
        /// </summary>
        /// <param name="paths">Paths to files and folders (key is the local path, value the path in the archive</param>
        /// <param name="recursive">If to recurse into sub-folders of a folder</param>
        /// <param name="includeEmptyFolders">If to include empty folders</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If not to compress files</param>
        /// <param name="enumerationOptions">Filesystem enumeration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddFsItemsAsync(
            IEnumerable<KeyValuePair<string, string>> paths,
            bool recursive = true,
            bool includeEmptyFolders = true,
            CompressionOptions? options = null,
            bool uncompressed = false,
            EnumerationOptions? enumerationOptions = null,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            FileStream fs;
            foreach (KeyValuePair<string, string> kvp in paths)
                if (Directory.Exists(kvp.Key))
                {
                    await AddFolderAsync(kvp.Key, kvp.Value, recursive, includeEmptyFolders, options, uncompressed, enumerationOptions, cancellationToken).DynamicContext();
                }
                else if (File.Exists(kvp.Key))
                {
                    fs = FsHelper.CreateFileStream(kvp.Key, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan | FileOptions.Asynchronous);
                    await using (fs.DynamicContext())
                        await AddFileAsync(kvp.Value, fs, options, uncompressed, cancellationToken).DynamicContext();
                }
                else
                {
                    throw new FileNotFoundException($"Local path \"{kvp.Key}\" not found");
                }
        }

        /// <summary>
        /// Add a key/value
        /// </summary>
        /// <param name="key">Item key</param>
        /// <param name="value">Source stream (should be seekable to avoid chunking)</param>
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

        /// <summary>
        /// Add key/values
        /// </summary>
        /// <param name="values">Key/values (values won't be disposed; use seekable streams to avoid chunking)</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddKeyValueAsync(
            IEnumerable<KeyValuePair<string, Stream>> values,
            CompressionOptions? options = null,
            bool uncompressed = false,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            foreach (KeyValuePair<string, Stream> kvp in values)
                await AddKeyValueAsync(kvp.Key, kvp.Value, options, uncompressed, cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Add key/values
        /// </summary>
        /// <param name="values">Key/values (values won't be disposed; use seekable streams to avoid chunking)</param>
        /// <param name="options">Options</param>
        /// <param name="uncompressed">If uncompressed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public virtual async Task AddKeyValueAsync(
            IAsyncEnumerable<KeyValuePair<string, Stream>> values,
            CompressionOptions? options = null,
            bool uncompressed = false,
            CancellationToken cancellationToken = default
            )
        {
            EnsureUndisposed();
            await foreach (KeyValuePair<string, Stream> kvp in values.DynamicContext().WithCancellation(cancellationToken))
                await AddKeyValueAsync(kvp.Key, kvp.Value, options, uncompressed, cancellationToken).DynamicContext();
        }
    }
}
