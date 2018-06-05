using Google.Protobuf;
using System;

namespace Loom.Unity3d
{
    // Extend the generated Address protobuf type with some utility methods.
    public partial class Address
    {
        /// <summary>
        /// Hex-string representation of the local part of the address, in the format "0x...".
        /// </summary>
        public string LocalAddressHexString
        {
            get
            {
                return "0x" + CryptoUtils.BytesToHexString(this.Local.ToByteArray());
            }
        }

        public static Address FromAddressString(string addressStr)
        {
            var parts = addressStr.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid DAppChain address string");
            }
            return FromHexString(parts[1], parts[0]);
        }

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

        /// <summary>
        /// Generates a string representation of the address, in the format "chain:0x...".
        /// </summary>
        /// <returns>A string representing the address.</returns>
        public string ToAddressString()
        {
            // TODO: checksum encode the local address bytes like we do in Go
            return string.Format("{0}:{1}", this.ChainId, this.LocalAddressHexString);
        }
    }
}
