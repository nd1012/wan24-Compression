using wan24.Core;

[assembly: Bootstrapper(typeof(wan24.Compression.Bootstrap), nameof(wan24.Compression.Bootstrap.Boot))]

namespace wan24.Compression
{
    /// <summary>
    /// Bootstrapper
    /// </summary>
    public static class Bootstrap
    {
        /// <summary>
        /// Boot
        /// </summary>
        public static void Boot()
        {
            StatusProvider.Providers["Compression"] = CompressionHelper.State;
        }
    }
}
