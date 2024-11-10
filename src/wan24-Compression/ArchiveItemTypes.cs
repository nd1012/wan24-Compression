using wan24.Core;

namespace wan24.Compression
{
    /// <summary>
    /// Archive compression item types
    /// </summary>
    [Flags]
    public enum ArchiveItemTypes : byte
    {
        /// <summary>
        /// None
        /// </summary>
        [DisplayText("None")]
        None = 0,
        /// <summary>
        /// File
        /// </summary>
        [DisplayText("File")]
        File = 1,
        /// <summary>
        /// Folder
        /// </summary>
        [DisplayText("Folder")]
        Folder = 2,
        /// <summary>
        /// Key/value
        /// </summary>
        [DisplayText("Key/value")]
        KeyValue = 3,
        /// <summary>
        /// If the file/value was written chunked
        /// </summary>
        [DisplayText("Chunked")]
        Chunked = 16,
        /// <summary>
        /// If the file/value was empty
        /// </summary>
        [DisplayText("Empty file")]
        Empty = 32,
        /// <summary>
        /// If custom compression options are included
        /// </summary>
        [DisplayText("Custom compression options included")]
        Options = 64,
        /// <summary>
        /// If the file/value is uncompressed
        /// </summary>
        [DisplayText("Is uncompressed")]
        Uncompressed = 128,
        /// <summary>
        /// All flags
        /// </summary>
        [DisplayText("All flags")]
        FLAGS = Chunked | Empty | Options | Uncompressed
    }
}
