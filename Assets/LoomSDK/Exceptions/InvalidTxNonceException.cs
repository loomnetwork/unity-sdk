namespace Loom.Unity3d
{
    /// <summary>
    /// Used to signal that a transaction was rejected because it had a bad nonce.
    /// </summary>
    public class InvalidTxNonceException : TxCommitException
    {
        public InvalidTxNonceException(int code, string error) : base(code, error)
        {
        }
    }
}