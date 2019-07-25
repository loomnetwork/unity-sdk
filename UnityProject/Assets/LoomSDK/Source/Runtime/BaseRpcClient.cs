using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Loom.Client.Internal
{
    public abstract class BaseRpcClient : IRpcClient, ILogProducer
    {
        private ILogger logger = NullLogger.Instance;
        private RpcConnectionState? lastConnectionState;

        protected bool disposed = false;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public virtual ILogger Logger
        {
            get
            {
                return this.logger;
            }
            set
            {
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

        public event RpcClientConnectionStateChangedHandler ConnectionStateChanged;

        public event EventHandler<JsonRpcEventData> EventReceived;

        public abstract RpcConnectionState ConnectionState { get; }
        public abstract Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        public abstract Task ConnectAsync();
        public abstract Task DisconnectAsync();
        public abstract Task SubscribeToEventsAsync(ICollection<string> topics);
        public abstract Task UnsubscribeFromEventAsync(string topic);

        protected abstract void Dispose(bool disposing);

        protected void NotifyConnectionStateChanged()
        {
            RpcConnectionState state = this.ConnectionState;
            if (this.lastConnectionState != null && this.lastConnectionState == state)
                return;

            this.lastConnectionState = state;
            ConnectionStateChanged?.Invoke(this, state);
        }

        protected void InvokeEventReceived(JsonRpcEventData eventData)
        {
            EventReceived?.Invoke(this, eventData);
        }

        protected void HandleJsonRpcResponseError(JsonRpcResponse partialMsg)
        {
            if (partialMsg.Error.Data != null && partialMsg.Error.Data.EndsWith("Tx already exists in cache"))
            {
                throw new TxAlreadyExistsInCacheException(int.Parse(partialMsg.Error.Code), partialMsg.Error.Data);
            }

            throw new RpcClientException(
                String.Format(
                    "JSON-RPC Error {0} ({1}): {2}",
                    partialMsg.Error.Code,
                    partialMsg.Error.Message,
                    partialMsg.Error.Data
                ),
                long.Parse(partialMsg.Error.Code),
                this
            );
        }

        protected void AssertIsConnected()
        {
            RpcConnectionState connectionState = this.ConnectionState;
            if (connectionState == RpcConnectionState.Connected)
                return;

            throw new RpcClientException(
                $"Client must be in {nameof(RpcConnectionState.Connected)} state, " +
                $"current state is {connectionState}",
                1,
                this
            );
        }
        
        protected void AssertNotAlreadyConnectedOrConnecting()
        {
            RpcConnectionState connectionState = this.ConnectionState;
            
            if (connectionState == RpcConnectionState.Connecting)
            {
                throw new RpcClientException("An attempt to connect while in process of connecting", 1, this);
            }
            
            if (connectionState == RpcConnectionState.Connected)
            {
                throw new RpcClientException("An attempt to connect when already connected", 1, this);
            }
        }

        ~BaseRpcClient()
        {
            Dispose(false);
        }
    }
}
