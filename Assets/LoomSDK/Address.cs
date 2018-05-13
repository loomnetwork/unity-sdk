using Google.Protobuf;

namespace Loom.Unity3d
{
    // Extend the generated Address protobuf type with some utility methods.
    public partial class Address
    {
        /// <summary>
        /// Creates an Address instance from a hex string representing an address.
        /// </summary>
        /// <param name="hexAddressStr">Hex encoded string, may start with "0x".</param>
        /// <param name="chainId">Identifier of a DAppChain.</param>
        /// <returns>An address</returns>
        public static Address FromHexString(string hexAddressStr, string chainId = "default")
        {
            return new Address
            {
                ChainId = chainId,
                Local = ByteString.CopyFrom(CryptoUtils.HexStringToBytes(hexAddressStr))
            };
        }

        /// <summary>
        /// Creates an Address instance from a 32-byte public key.
        /// </summary>
        /// <param name="publicKey">32-byte public key.</param>
        /// <param name="chainId">Identifier of a DAppChain.</param>
        /// <returns>An address</returns>
        public static Address FromPublicKey(byte[] publicKey, string chainId = "default")
        {
            return new Address
            {
                ChainId = chainId,
                Local = ByteString.CopyFrom(CryptoUtils.LocalAddressFromPublicKey(publicKey))
            };
        }
    }
}
