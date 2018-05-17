using UnityEngine;
using System;

namespace Loom.Unity3d
{
    public class RPCClientFactory
    {
        private ILogger logger;
        private string websocketUrl;
        private string httpUrl;

        public static RPCClientFactory Configure()
        {
            return new RPCClientFactory();
        }

        public RPCClientFactory WithLogger(ILogger logger)
        {
            this.logger = logger;
            return this;
        }

        public RPCClientFactory WithWebSocket(string url)
        {
            this.websocketUrl = url;
            return this;
        }

        public RPCClientFactory WithHTTP(string url)
        {
            this.httpUrl = url;
            return this;
        }
        
        public IRPCClient Create()
        {
            var logger = this.logger ?? NullLogger.Instance;
            if (this.websocketUrl != null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return new WebGL.WSRPCClient(this.websocketUrl) { Logger = logger };
#else
                return new WSSharpRPCClient(this.websocketUrl) { Logger = logger };
#endif    
            }
            else if (this.httpUrl != null)
            {
                return new HTTPRPCClient(this.httpUrl) { Logger = logger };
            }
            throw new InvalidOperationException("RPCClientFactory configuration invalid.");
        }
    }
}
