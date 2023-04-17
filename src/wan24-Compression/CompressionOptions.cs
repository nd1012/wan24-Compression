using System.IO.Compression;

namespace wan24.Compression
{
    /// <summary>
    /// Compression options
    /// </summary>
    public sealed class CompressionOptions
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CompressionOptions() { }

        /// <summary>
        /// Algorithm
        /// </summary>
        public string? Algorithm { get; set; }

        /// <summary>
        /// Serializer version
        /// </summary>
        public int? SerializerVersion { get; set; }

        /// <summary>
        /// Uncompressed data length in bytes (used internal when using the compression helper)
        /// </summary>
        public long UncompressedDataLength { get; set; } = -1;

        /// <summary>
        /// Serializer version included?
        /// </summary>
        public bool SerializerVersionIncluded { get; set; } = true;

        /// <summary>
        /// Algorithm included?
        /// </summary>
        public bool AlgorithmIncluded { get; set; } = true;

        /// <summary>
        /// Length in bytes included?
        /// </summary>
        public bool LengthIncluded { get; set; } = true;

        /// <summary>
        /// Compression flags included?
        /// </summary>
        public bool FlagsIncluded { get; set; } = true;

        /// <summary>
        /// Compression level
        /// </summary>
        public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;

        /// <summary>
        /// Leave the compression target/decompression source stream open?
        /// </summary>
        public bool LeaveOpen { get; set; } = true;

        /// <summary>
        /// Compression flags
        /// </summary>
        public CompressionFlags Flags
        {
            get
            {
                CompressionFlags res = CompressionFlags.None;
                if (SerializerVersionIncluded) res |= CompressionFlags.SerializerVersionIncluded;
                if (AlgorithmIncluded) res |= CompressionFlags.AlgorithmIncluded;
                if(LengthIncluded) res |= CompressionFlags.LengthIncluded;
                return res;
            }
            set
            {
                SerializerVersionIncluded = value.HasFlag(CompressionFlags.SerializerVersionIncluded);
                AlgorithmIncluded = value.HasFlag(CompressionFlags.AlgorithmIncluded);
                LengthIncluded = value.HasFlag(CompressionFlags.LengthIncluded);
            }
        }

        /// <summary>
        /// Get a clone
        /// </summary>
        /// <returns></returns>
        public CompressionOptions Clone() => new()
        {
            Algorithm = Algorithm,
            FlagsIncluded = FlagsIncluded,
            Flags = Flags,
            Level = Level,
            LeaveOpen = LeaveOpen
        };
    }
}
