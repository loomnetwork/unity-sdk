using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Client.Internal
{
    internal abstract class BaseRpcClient : IRpcClient, ILogProducer
    {
        private ILogger logger = NullLogger.Instance;
        private RpcConnectionState? lastConnectionState;

        protected bool disposed = false;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public virtual ILogger Logger {
            get {
                return logger;
            }
            set {
                if (value == null)
                {
                    value = NullLogger.Instance;
                }

                this.logger = value;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual event RpcClientConnectionStateChangedHandler ConnectionStateChanged;
        public abstract RpcConnectionState ConnectionState { get; }
        public abstract Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        public abstract Task DisconnectAsync();
        public abstract Task SubscribeAsync(EventHandler<JsonRpcEventData> handler);
        public abstract Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler);

        protected abstract void Dispose(bool disposing);

        protected void NotifyConnectionStateChanged()
        {
            RpcConnectionState state = ConnectionState;
            if (this.lastConnectionState != null && this.lastConnectionState == state)
                return;

            this.lastConnectionState = state;
            ConnectionStateChanged?.Invoke(this, state);
        }

        ~BaseRpcClient()
        {
            Dispose(false);
        }
    }
}
