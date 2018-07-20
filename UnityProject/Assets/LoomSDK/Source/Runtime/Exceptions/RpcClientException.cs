using System;

namespace Loom.Client
{
    /// <summary>
    /// Represents an error that occured during an RPC.
    /// </summary>
    public class RpcClientException : LoomException
    {
        public RpcClientException()
        {
        }

        public RpcClientException(string message) : base(message)
        {
        }

        public RpcClientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}