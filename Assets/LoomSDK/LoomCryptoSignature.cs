namespace Loom.Unity3d
{
    public struct LoomCryptoSignature
    {
        /// <summary>
        /// 64-byte signature.
        /// </summary>
        public byte[] Signature { get; }

        /// <summary>
        /// 32-byte public key.
        /// </summary>
        public byte[] PublicKey { get; }

        public LoomCryptoSignature(byte[] signature, byte[] publicKey) {
            Signature = signature;
            PublicKey = publicKey;
        }
    }
}