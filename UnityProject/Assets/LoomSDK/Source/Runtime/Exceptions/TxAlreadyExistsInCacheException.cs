namespace Loom.Client
{
    /// <summary>
    /// Used to signal that a byte-exact transaction was already made.
    /// </summary>
    public class TxAlreadyExistsInCacheException : TxCommitException
    {
        public TxAlreadyExistsInCacheException(int code, string error) : base(code, error)
        {
        }
    }
}
