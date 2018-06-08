using Google.Protobuf;
using System;
using System.Numerics;

namespace Loom.Unity3d
{
    public static class BigUIntBigIntegerExtensions
    {
        /* TODO
        /// <summary>
        /// Converts bytes representing a Loom BigUInt (big-endian) to a BigInteger (little-endian).
        /// </summary>
        /// <param name="value">ByteString to convert.</param>
        /// <returns>BigInteger representation of a BigUInt.</returns>
        public static BigInteger ToBigUInt(this ByteString value)
        {

        }
        */

        /// <summary>
        /// Converts a BigInteger (little-endian) to the byte representation a Loom BigUInt (big-endian).
        /// </summary>
        /// <param name="value">BigInteger to convert.</param>
        /// <returns>ByteString representation of a BigUInt.</returns>
        public static ByteString ToBigUIntByteString(this BigInteger value)
        {
            var bytes = value.ToByteArray();
            Array.Reverse(bytes);
            return ByteString.CopyFrom(bytes);
        }        
    }
}