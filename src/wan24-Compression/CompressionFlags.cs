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
        /// Length in bytes included
        /// </summary>
        LengthIncluded = 4
    }
}
