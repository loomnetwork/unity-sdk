using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                return this.logger;
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
        public abstract Task ConnectAsync();
        public abstract Task DisconnectAsync();
        public abstract Task SubscribeAsync(EventHandler<JsonRpcEventData> handler);
        public abstract Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler);

        protected abstract void Dispose(bool disposing);

        protected void NotifyConnectionStateChanged()
        {
            RpcConnectionState state = this.ConnectionState;
            if (this.lastConnectionState != null && this.lastConnectionState == state)
                return;

            this.lastConnectionState = state;
            ConnectionStateChanged?.Invoke(this, state);
        }

        protected void AssertIsConnected() {
            RpcConnectionState connectionState = this.ConnectionState;
            if (connectionState == RpcConnectionState.Connected)
                return;
            
            throw new RpcClientException(
                $"Client must be in {nameof(RpcConnectionState.Connected)} state, " +
                $"current state is {connectionState}");
        }
        
        protected void AssertNotAlreadyConnectedOrConnecting() {
            RpcConnectionState connectionState = this.ConnectionState;
            
            if (connectionState == RpcConnectionState.Connecting)
            {
                throw new RpcClientException("An attempt to connect while in process of connecting");
            }
            
            if (connectionState == RpcConnectionState.Connected)
            {
                throw new RpcClientException("An attempt to connect when already connected");
            }
        }

        ~BaseRpcClient()
        {
            Dispose(false);
        }
    }
}
