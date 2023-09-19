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
        None = 0,
        /// <summary>
        /// Serializer version included
        /// </summary>
        SerializerVersionIncluded = 1,
        /// <summary>
        /// Algorithm value included
        /// </summary>
        AlgorithmIncluded = 2,
        /// <summary>
        /// Uncompressed data length in bytes included
        /// </summary>
        UncompressedLengthIncluded = 4,
        /// <summary>
        /// All flags
        /// </summary>
        ALL = SerializerVersionIncluded | AlgorithmIncluded | UncompressedLengthIncluded
    }
}
