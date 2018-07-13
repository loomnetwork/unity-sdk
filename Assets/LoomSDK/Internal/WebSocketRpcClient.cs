#if !UNITY_WEBGL || UNITY_EDITOR

using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;

namespace Loom.Unity3d.Internal
{
    /// <summary>
    /// WebSocket JSON-RPC client implemented with WebSocketSharp.
    /// </summary>
    internal class WebSocketRpcClient : IRpcClient
    {
        private const string LogTag = "Loom.WebSocketRpcClient";

        private readonly WebSocket client;
        private readonly Uri url;
        private ILogger logger;
        private RpcConnectionState? lastConnectionState;
        private event EventHandler<JsonRpcEventData> OnEventMessage;

        public event RpcClientConnectionStateChangedHandler ConnectionStateChanged;

        public bool IsConnectable => true;

        public RpcConnectionState ConnectionState
        {
            get
            {
                switch (this.client.ReadyState)
                {
                    case WebSocketState.Connecting:
                        return RpcConnectionState.Connecting;
                    case WebSocketState.Open:
                        return RpcConnectionState.Connected;
                    case WebSocketState.Closing:
                        return RpcConnectionState.Disconnecting;
                    case WebSocketState.Closed:
                        return RpcConnectionState.Disconnected;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger
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

                if (this.logger == value)
                    return;

                this.logger = value;
                this.client.Log.Output = WebSocketProxyLoggerOutputFactory.CreateWebSocketProxyLoggerOutput(value);
            }
        }

        public WebSocketRpcClient(string url)
        {
            this.client = new WebSocket(url);
            this.client.WaitTime = TimeSpan.FromMilliseconds(500);
            this.client.Log.Level = LogLevel.Trace;
            this.url = new Uri(url);
            this.Logger = NullLogger.Instance;
            this.client.OnError += (sender, e) =>
            {
                this.Logger.Log(LogTag, "Error: " + e.Message);
            };
        }

        void IDisposable.Dispose()
        {
            ((IDisposable)this.client).Dispose();
        }

        public Task DisconnectAsync()
        {
            // TODO: should be listening for disconnection all the time
            // and auto-reconnect if there are event subscriptions
            var tcs = new TaskCompletionSource<CloseEventArgs>();
            EventHandler<CloseEventArgs> handler = null;
            handler = (sender, e) =>
            {
                this.client.OnClose -= handler;
                tcs.TrySetResult(e);
                NotifyConnectionStateChanged();
            };
            this.client.OnClose += handler;
            try
            {
                this.client.CloseAsync(CloseStatusCode.Normal, "Client disconnected.");
                NotifyConnectionStateChanged();
            }
            catch (Exception)
            {
                this.client.OnClose -= handler;
                NotifyConnectionStateChanged();
                throw;
            }
            return tcs.Task;
        }

        private Task EnsureConnectionAsync()
        {
            if (this.client.ReadyState == WebSocketState.Open)
            {
                return Task.CompletedTask;
            }
            var tcs = new TaskCompletionSource<object>();
            EventHandler openHandler = null;
            EventHandler<CloseEventArgs> closeHandler = null;
            openHandler = (sender, e) =>
            {
                this.client.OnOpen -= openHandler;
                this.client.OnClose -= closeHandler;
                tcs.TrySetResult(null);
                Logger.Log(LogTag, "Connected to " + this.url.AbsoluteUri);
                NotifyConnectionStateChanged();
            };
            closeHandler = (sender, e) =>
            {
                tcs.SetException(new RpcClientException($"WebSocket closed unexpectedly with error {e.Code}: {e.Reason}"));
                NotifyConnectionStateChanged();
            };
            this.client.OnOpen += openHandler;
            this.client.OnClose += closeHandler;
            try
            {
                this.client.ConnectAsync();
            }
            catch (Exception)
            {
                this.client.OnOpen -= openHandler;
                this.client.OnClose -= closeHandler;
                throw;
            }
            return tcs.Task;
        }

        public Task SubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            var isFirstSub = this.OnEventMessage == null;
            this.OnEventMessage += handler;
            if (isFirstSub)
            {
                this.client.OnMessage += this.WSSharpRPCClient_OnMessage;
            }
            // TODO: once re-sub on reconnect is implemented this should only
            // be done on first sub
            return this.SendAsync<object, object>("subevents", new object());
        }

        public Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            this.OnEventMessage -= handler;
            if (this.OnEventMessage == null)
            {
                this.client.OnMessage -= this.WSSharpRPCClient_OnMessage;
                return this.SendAsync<object, object>("unsubevents", new object());
            }
            return Task.CompletedTask;
        }

        private async Task SendAsync<T>(string method, T args, string msgId)
        {
            var tcs = new TaskCompletionSource<object>();
            await this.EnsureConnectionAsync();
            var reqMsg = new JsonRpcRequest<T>(method, args, msgId);
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            Logger.Log(LogTag, "[Request Body] " + reqMsgBody);

            ErrorEventArgs errorEventArgs = null;
            EventHandler<ErrorEventArgs> errorHandler = (sender, eventArgs) =>
            {
                errorEventArgs = eventArgs;
            };
            this.client.OnError += errorHandler;
            this.client.SendAsync(reqMsgBody, (bool success) =>
            {
                if (success)
                {
                    tcs.TrySetResult(null);
                }
                else
                {
                    tcs.TrySetException(new RpcClientException("Send error", errorEventArgs.Exception));
                }
            });
            this.client.OnError -= errorHandler;
            await tcs.Task;
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            var tcs = new TaskCompletionSource<T>();
            var msgId = Guid.NewGuid().ToString();
            EventHandler<MessageEventArgs> handler = null;
            handler = (sender, e) =>
            {
                try
                {
                    // TODO: set a timeout and throw exception when it's exceeded
                    if (e.IsText && !string.IsNullOrEmpty(e.Data))
                    {
                        var partialMsg = JsonConvert.DeserializeObject<JsonRpcResponse>(e.Data);
                        if (partialMsg.Id == msgId)
                        {
                            this.client.OnMessage -= handler;
                            if (partialMsg.Error != null)
                            {
                                throw new RpcClientException(String.Format(
                                    "JSON-RPC Error {0} ({1}): {2}",
                                    partialMsg.Error.Code, partialMsg.Error.Message, partialMsg.Error.Data
                                ));
                            }
                            else
                            {
                                var fullMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(e.Data);
                                tcs.TrySetResult(fullMsg.Result);
                            }
                        }
                    }
                    else
                    {
                        Logger.Log(LogTag, "[ignoring msg]");
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }

                NotifyConnectionStateChanged();
            };
            this.client.OnMessage += handler;
            try
            {
                await this.SendAsync<U>(method, args, msgId);
            }
            catch (Exception e)
            {
                this.client.OnMessage -= handler;
                throw e;
            }
            return await tcs.Task;
        }

        private void NotifyConnectionStateChanged()
        {
            RpcConnectionState state = ConnectionState;
            if (this.lastConnectionState == null || this.lastConnectionState == state)
                return;

            this.lastConnectionState = state;
            ConnectionStateChanged?.Invoke(this, state);
        }

        private void WSSharpRPCClient_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.IsText && !string.IsNullOrEmpty(e.Data))
                {
                    Logger.Log(LogTag, "[WSSharpRPCClient_OnMessage msg body] " + e.Data);
                    var partialMsg = JsonConvert.DeserializeObject<JsonRpcResponse>(e.Data);
                    if (partialMsg.Id == "0")
                    {
                        if (partialMsg.Error != null)
                        {
                            throw new RpcClientException(String.Format(
                                "JSON-RPC Error {0} ({1}): {2}",
                                partialMsg.Error.Code, partialMsg.Error.Message, partialMsg.Error.Data
                            ));
                        }
                        else
                        {
                            var fullMsg = JsonConvert.DeserializeObject<JsonRpcEvent>(e.Data);
                            this.OnEventMessage?.Invoke(this, fullMsg.Result);
                        }
                    }
                }
                else
                {
                    Logger.Log(LogTag, "[WSSharpRPCClient_OnMessage ignoring msg]");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(LogTag, "[WSSharpRPCClient_OnMessage error] " + ex);
            }

            NotifyConnectionStateChanged();
        }
    }
}

#endif