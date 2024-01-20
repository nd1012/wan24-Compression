using wan24.Core;

namespace wan24.Compression
{
    /// <summary>
    /// Compression flags
    /// </summary>
    [Flags]
    public enum CompressionFlags : byte
    {
        /// <summary>
        /// None
        /// </summary>
        [DisplayText("None")]
        None = 0,
        /// <summary>
        /// Serializer version included
        /// </summary>
        [DisplayText("Serializer version included")]
        SerializerVersionIncluded = 1,
        /// <summary>
        /// Algorithm value included
        /// </summary>
        [DisplayText("Algorithm value included")]
        AlgorithmIncluded = 2,
        /// <summary>
        /// Uncompressed data length in bytes included
        /// </summary>
        [DisplayText("Uncompressed data length in bytes included")]
        UncompressedLengthIncluded = 4,
        /// <summary>
        /// All flags
        /// </summary>
        [DisplayText("All flags")]
        ALL = SerializerVersionIncluded | AlgorithmIncluded | UncompressedLengthIncluded
    }
}
