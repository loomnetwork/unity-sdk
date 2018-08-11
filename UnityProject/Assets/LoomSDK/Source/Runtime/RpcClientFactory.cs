using UnityEngine;
using System;
using Loom.Client.Internal;

namespace Loom.Client
{
    public class RpcClientFactory
    {
        private ILogger logger = NullLogger.Instance;
        private string websocketUrl;
        private string httpUrl;

        public static RpcClientFactory Configure()
        {
            return new RpcClientFactory();
        }

        public RpcClientFactory WithLogger(ILogger logger)
        {
            this.logger = logger ?? NullLogger.Instance;
            return this;
        }

        public RpcClientFactory WithWebSocket(string url)
        {
            this.websocketUrl = url;
            return this;
        }

        public RpcClientFactory WithHTTP(string url)
        {
            this.httpUrl = url;
            return this;
        }

        public IRpcClient Create()
        {
            if (this.websocketUrl != null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return new Unity.WebGL.Internal.WebSocketRpcClient(this.websocketUrl) { Logger = logger };
#else
                return new WebSocketRpcClient(this.websocketUrl) { Logger = this.logger };
#endif
            } else if (this.httpUrl != null)
            {
                return new HttpRpcClient(this.httpUrl) { Logger = this.logger };
            }

            throw new InvalidOperationException("RpcClientFactory configuration invalid.");
        }
    }

    [Obsolete("Use RpcClientFactory")]
    public class RPCClientFactory : RpcClientFactory
    {
    }
}
