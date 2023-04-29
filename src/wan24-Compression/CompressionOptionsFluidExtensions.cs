using System.IO.Compression;

namespace wan24.Compression
{
    /// <summary>
    /// <see cref="CompressionOptions"/> fluid extensions
    /// </summary>
    public static class CompressionOptionsFluidExtensions
    {
        /// <summary>
        /// Set compression algorithm options
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="algo">Algorithm name</param>
        /// <param name="level">Compression level</param>
        /// <param name="included">Algorithm included?</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithAlgorithm(this CompressionOptions options, string algo, CompressionLevel level = CompressionLevel.Optimal, bool included = true)
        {
            CompressionHelper.GetAlgorithm(algo);
            options.Algorithm = algo;
            options.Level = level;
            options.AlgorithmIncluded = included;
            return options;
        }

        /// <summary>
        /// Set compression algorithm options
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="algo">Algorithm value</param>
        /// <param name="level">Compression level</param>
        /// <param name="included">Algorithm included?</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithAlgorithm(this CompressionOptions options, int algo, CompressionLevel level = CompressionLevel.Optimal, bool included = true)
            => WithAlgorithm(options, CompressionHelper.GetAlgorithm(algo).Name, level, included);

        /// <summary>
        /// Include the serializer version
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithSerializerVersion(this CompressionOptions options)
        {
            options.SerializerVersionIncluded = true;
            return options;
        }

        /// <summary>
        /// Disable including the serializer version
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithoutSerializerVersion(this CompressionOptions options)
        {
            options.SerializerVersionIncluded = false;
            return options;
        }

        /// <summary>
        /// Include the uncompressed data length
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithUncompressedDataLength(this CompressionOptions options)
        {
            options.UncompressedLengthIncluded = true;
            return options;
        }

        /// <summary>
        /// Disable including the uncompressed data length
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithoutUncompressedDataLength(this CompressionOptions options)
        {
            options.UncompressedLengthIncluded = false;
            return options;
        }

        /// <summary>
        /// Include (and set) the compression flags
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="flags">Flags</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithFlagsIncluded(this CompressionOptions options, CompressionFlags? flags = null)
        {
            options.FlagsIncluded = true;
            if (flags != null) options.Flags = flags.Value;
            return options;
        }

        /// <summary>
        /// Disable including the compression flags
        /// </summary>
        /// <param name="options">Options</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithoutFlagsIncluded(this CompressionOptions options)
        {
            options.FlagsIncluded = false;
            return options;
        }

        /// <summary>
        /// Add flags
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="flags">Flags</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithAdditionalFlags(this CompressionOptions options, params CompressionFlags[] flags)
        {
            CompressionFlags f = options.Flags;
            foreach (CompressionFlags flag in flags) f |= flag;
            options.Flags = f;
            return options;
        }

        /// <summary>
        /// Remove flags
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="flags">Flags</param>
        /// <returns>Options</returns>
        public static CompressionOptions WithoutFlags(this CompressionOptions options, params CompressionFlags[] flags)
        {
            CompressionFlags f = options.Flags;
            foreach (CompressionFlags flag in flags) f &= ~flag;
            options.Flags = f;
            return options;
        }
    }
}
