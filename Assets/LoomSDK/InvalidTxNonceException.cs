using System;

namespace Loom.Unity3d
{
    /// <summary>
    /// Used to signal that a transaction was rejected because it had a bad nonce.
    /// </summary>
    public class InvalidTxNonceException : Exception
    {
        public InvalidTxNonceException()
        {
        }

        public InvalidTxNonceException(string msg) : base(msg)
        {
        }
    }
}