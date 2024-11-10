using System.Diagnostics.CodeAnalysis;
using wan24.Core;

namespace wan24.Compression
{
    /// <summary>
    /// Archive item information
    /// </summary>
    public class ArchiveItemInfo() : BasicAllDisposableBase()
    {
        /// <summary>
        /// Type
        /// </summary>
        public required ArchiveItemTypes Type { get; init; }

        /// <summary>
        /// If there is no next item (if <see langword="true"/>, there is no next item to read)
        /// </summary>
        public bool IsNone => Type == ArchiveItemTypes.None;

        /// <summary>
        /// If the item is a file (if <see langword="true"/>, and <see cref="IsEmpty"/> is <see langword="false"/>, <see cref="Value"/> is not <see langword="null"/> 
        /// and must be red completly before reading the next item)
        /// </summary>
        public bool IsFile => (Type & ~ArchiveItemTypes.FLAGS) == ArchiveItemTypes.File;

        /// <summary>
        /// If the item is a folder
        /// </summary>
        public bool IsFolder => (Type & ~ArchiveItemTypes.FLAGS) == ArchiveItemTypes.Folder;

        /// <summary>
        /// If the item is a key/value (if <see langword="true"/>, and <see cref="IsEmpty"/> is <see langword="false"/>, <see cref="Value"/> is not <see langword="null"/> 
        /// and must be red completly before reading the next item)
        /// </summary>
        public bool IsKeyValue => (Type & ~ArchiveItemTypes.FLAGS) == ArchiveItemTypes.KeyValue;

        /// <summary>
        /// If the value is chunked
        /// </summary>
        public bool IsChunked => !IsFolder && !IsEmpty && (Type & ArchiveItemTypes.Chunked) == ArchiveItemTypes.Chunked;

        /// <summary>
        /// If the value is uncompressed
        /// </summary>
        public bool IsCompressed => !IsFolder && !IsEmpty && (Type & ArchiveItemTypes.Uncompressed) != ArchiveItemTypes.Uncompressed;

        /// <summary>
        /// If the value is empty (or <see langword="null"/>)
        /// </summary>
        public bool IsEmpty
        {
            [MemberNotNullWhen(returnValue: false, nameof(Value))]
            get => IsFolder || (Type & ArchiveItemTypes.Empty) == ArchiveItemTypes.Empty;
        }

        /// <summary>
        /// Key / (file) path
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// Value length in bytes, if not chunked
        /// </summary>
        public long? Length { get; init; }

        /// <summary>
        /// Compression options
        /// </summary>
        public required CompressionOptions Options { get; init; }

        /// <summary>
        /// Value stream (will be disposed)
        /// </summary>
        public Stream? Value { get; init; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) => Value?.Dispose();

        /// <inheritdoc/>
        protected override async Task DisposeCore()
        {
            if (Value is not null) await Value.DisposeAsync().DynamicContext();
        }
    }
}
