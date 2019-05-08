using System;

namespace Loom.Client
{
    /// <summary>
    /// Represents an error that occured during an RPC.
    /// </summary>
    public class RpcClientException : LoomException
    {
        public long Code { get; }

        public IRpcClient RpcClient { get; }

        public RpcClientException(long code, IRpcClient rpcClient)
        {
            Code = code;
            RpcClient = rpcClient;
        }
        public RpcClientException(string message, long code, IRpcClient rpcClient) : base(message)
        {
            Code = code;
            RpcClient = rpcClient;
        }
        public RpcClientException(string message, Exception innerException, long code, IRpcClient rpcClient) : base(message, innerException)
        {
            Code = code;
            RpcClient = rpcClient;
        }
    }
}