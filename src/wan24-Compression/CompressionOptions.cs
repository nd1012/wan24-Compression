using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using wan24.Core;
using wan24.StreamSerializerExtensions;

namespace wan24.Compression
{
    /// <summary>
    /// Compression options
    /// </summary>
    public sealed class CompressionOptions : StreamSerializerBase
    {
        /// <summary>
        /// Object version
        /// </summary>
        public const int VERSION = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        public CompressionOptions() : base(VERSION) { }

        /// <summary>
        /// Algorithm
        /// </summary>
        [StringLength(byte.MaxValue)]
        public string? Algorithm { get; set; }

        /// <summary>
        /// Serializer version
        /// </summary>
        [Range(1, byte.MaxValue)]
        public int? SerializerVersion { get; set; }

        /// <summary>
        /// Uncompressed data length in bytes (used internal when using the compression helper)
        /// </summary>
        [Range(-1, long.MaxValue)]
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
        /// Uncompressed data length in bytes included?
        /// </summary>
        public bool UncompressedLengthIncluded { get; set; } = true;

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
        public bool LeaveOpen { get; set; }

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
                if (UncompressedLengthIncluded) res |= CompressionFlags.UncompressedLengthIncluded;
                return res;
            }
            set
            {
                SerializerVersionIncluded = value.HasFlag(CompressionFlags.SerializerVersionIncluded);
                AlgorithmIncluded = value.HasFlag(CompressionFlags.AlgorithmIncluded);
                UncompressedLengthIncluded = value.HasFlag(CompressionFlags.UncompressedLengthIncluded);
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

        /// <inheritdoc/>
        protected override void Serialize(Stream stream)
        {
            stream.WriteStringNullable(Algorithm)
                .Write(FlagsIncluded)
                .WriteEnum(Flags)
                .WriteEnum(Level)
                .Write(LeaveOpen);
        }

        /// <inheritdoc/>
        protected override async Task SerializeAsync(Stream stream, CancellationToken cancellationToken)
        {
            await stream.WriteStringNullableAsync(Algorithm, cancellationToken).DynamicContext();
            await stream.WriteAsync(FlagsIncluded, cancellationToken).DynamicContext();
            await stream.WriteEnumAsync(Flags, cancellationToken).DynamicContext();
            await stream.WriteEnumAsync(Level, cancellationToken).DynamicContext();
            await stream.WriteAsync(LeaveOpen, cancellationToken).DynamicContext();
        }

        /// <inheritdoc/>
        protected override void Deserialize(Stream stream, int version)
        {
            Algorithm = stream.ReadStringNullable(version, minLen: 1, maxLen: byte.MaxValue);
            FlagsIncluded = stream.ReadBool(version);
            Flags = stream.ReadEnum<CompressionFlags>(version);
            Level = stream.ReadEnum<CompressionLevel>(version);
            LeaveOpen = stream.ReadBool(version);
        }

        /// <inheritdoc/>
        protected override async Task DeserializeAsync(Stream stream, int version, CancellationToken cancellationToken)
        {
            Algorithm = await stream.ReadStringNullableAsync(version, minLen: 1, maxLen: byte.MaxValue, cancellationToken: cancellationToken).DynamicContext();
            FlagsIncluded = await stream.ReadBoolAsync(version, cancellationToken: cancellationToken).DynamicContext();
            Flags = await stream.ReadEnumAsync<CompressionFlags>(version, cancellationToken: cancellationToken).DynamicContext();
            Level = await stream.ReadEnumAsync<CompressionLevel>(version, cancellationToken: cancellationToken).DynamicContext();
            LeaveOpen = await stream.ReadBoolAsync(version, cancellationToken: cancellationToken).DynamicContext();
        }

        /// <summary>
        /// Cast as serialized data
        /// </summary>
        /// <param name="options">Options</param>
        public static implicit operator byte[](CompressionOptions options) => options.ToBytes();

        /// <summary>
        /// Cast from serialized data
        /// </summary>
        /// <param name="data">Data</param>
        public static explicit operator CompressionOptions(byte[] data) => data.ToObject<CompressionOptions>();
    }
}
