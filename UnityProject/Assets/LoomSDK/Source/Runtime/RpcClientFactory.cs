using UnityEngine;
using System;
using Loom.Client.Internal;

namespace Loom.Client
{
    public class RpcClientFactory
    {
        private ILogger logger;
        private string websocketUrl;
        private string httpUrl;

        public static RpcClientFactory Configure()
        {
            return new RpcClientFactory();
        }

        public RpcClientFactory WithLogger(ILogger logger)
        {
            this.logger = logger;
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
            var logger = this.logger ?? NullLogger.Instance;
            if (this.websocketUrl != null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return new Unity.Internal.WebGL.WebSocketRpcClient(this.websocketUrl) { Logger = logger };
#else
                return new WebSocketRpcClient(this.websocketUrl) { Logger = logger };
#endif
            } else if (this.httpUrl != null)
            {
                return new HttpRpcClient(this.httpUrl) { Logger = logger };
            }

            throw new InvalidOperationException("RpcClientFactory configuration invalid.");
        }
    }

    [Obsolete("Use RpcClientFactory")]
    public class RPCClientFactory : RpcClientFactory
    {
    }
}
