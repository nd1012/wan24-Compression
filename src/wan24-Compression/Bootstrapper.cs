using wan24.Core;

[assembly: Bootstrapper(typeof(wan24.Compression.Bootstrapper), nameof(wan24.Compression.Bootstrapper.Boot))]

namespace wan24.Compression
{
    /// <summary>
    /// Bootstrapper
    /// </summary>
    public static class Bootstrapper
    {
        /// <summary>
        /// Boot
        /// </summary>
        public static void Boot()
        {
            StatusProviderTable.Providers["Compression"] = CompressionHelper.State;
        }
    }
}
