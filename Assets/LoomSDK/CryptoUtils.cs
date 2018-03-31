using Chaos.NaCl;
using System.Security.Cryptography;
using System.Text;

namespace Loom.Unity3d
{
    public class LoomCryptoSignature
    {
        /// <summary>
        /// 64-byte signature.
        /// </summary>
        public byte[] Signature { get; internal set; }
        /// <summary>
        /// 32-byte public key.
        /// </summary>
        public byte[] PublicKey { get; internal set; }
    }


    public class CryptoUtils
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generates a 32-byte private key.
        /// </summary>
        /// <returns>A 32-byte array.</returns>
        public static byte[] GeneratePrivateKey()
        {
            var privateKey = new byte[32];
            rngCsp.GetBytes(privateKey);
            return privateKey;
        }

        /// <summary>
        /// Generates a 64-bit signature of the given message.
        /// </summary>
        /// <param name="message">A byte array of any size.</param>
        /// <param name="privateKey32">32-byte private key.</param>
        /// <returns>Signature and public key.</returns>
        public static LoomCryptoSignature Sign(byte[] message, byte[] privateKey32)
        {
            if (privateKey32.Length != 32)
            {
                throw new System.ArgumentException("Expected 32-byte array", "privateKey");
            }
            byte[] publicKey32;
            byte[] privateKey64;
            Ed25519.KeyPairFromSeed(out publicKey32, out privateKey64, privateKey32);
            byte[] signature = Ed25519.Sign(message, privateKey64);
            return new LoomCryptoSignature
            {
                Signature = signature,
                PublicKey = publicKey32
            };
        }

        public static string BytesToHexString(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            string alphabet = "0123456789ABCDEF";

            foreach (byte b in bytes)
            {
                hex.Append(alphabet[(int)(b >> 4)]);
                hex.Append(alphabet[(int)(b & 0xF)]);
            }

            return hex.ToString();
        }

    }

}