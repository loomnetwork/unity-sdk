namespace Loom.Client
{
    /// <summary>
    /// Stores <see cref="DAppChainClient"/> configuration options.
    /// </summary>
    public sealed class DAppChainClientConfiguration
    {
        /// <summary>
        /// Specifies the amount of time after which a call will time out, in milliseconds.
        /// </summary>
        public int CallTimeout { get; set; } = 5000;

        /// <summary>
        /// Specifies the amount of time after which a static will time out, in milliseconds.
        /// </summary>
        public int StaticCallTimeout { get; set; } = 5000;

        /// <summary>
        /// Whether clients will attempt to connect automatically when in Disconnected state
        /// before communicating.
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// Maximum number of times a tx should be resent after being rejected because of a bad nonce.
        /// Defaults to 5.
        /// </summary>
        public int InvalidNonceTxRetries { get; set; } = 5;
    }
}
