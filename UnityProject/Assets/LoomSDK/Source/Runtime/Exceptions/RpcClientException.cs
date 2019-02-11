using System;

namespace Loom.Client
{
    /// <summary>
    /// Represents an error that occured during an RPC.
    /// </summary>
    public class RpcClientException : LoomException
    {
        public long Code { get; }

        public RpcClientException(int code)
        {
            this.Code = code;
        }

        public RpcClientException(string message, long code) : base(message)
        {
            this.Code = code;
        }

        public RpcClientException(string message, int code, Exception innerException) : base(message, innerException)
        {
            this.Code = code;
        }
    }
}