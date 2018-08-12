#if !UNITY_WEBGL || UNITY_EDITOR

using System.Threading.Tasks;
using System;
using System.ComponentModel;
using Loom.Newtonsoft.Json;
using Loom.WebSocketSharp;
using UnityEngine;

namespace Loom.Client.Internal
{
    /// <summary>
    /// WebSocket JSON-RPC client implemented with WebSocketSharp.
    /// </summary>
    internal class WebSocketRpcClient : BaseRpcClient
    {
        private const string LogTag = "Loom.WebSocketRpcClient";

        private readonly WebSocket webSocket;
        private readonly Uri url;
        private event EventHandler<JsonRpcEventData> eventReceived;
        private bool anyConnectionStateChangesReceived;

        public override RpcConnectionState ConnectionState
        {
            get
            {
                // HACK: WebSocket default ReadyState value is Connecting,
                // which makes it impossible to distinguish the real Connecting state
                // and state when no connection-related actions have been done.
                // Just return Disconnected until we know anything better for sure.
                if (!anyConnectionStateChangesReceived)
                    return RpcConnectionState.Disconnected;
                
                WebSocketState state = this.webSocket.ReadyState;
                switch (state)
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
                        throw new InvalidEnumArgumentException(nameof(this.webSocket.ReadyState), (int) state, typeof(WebSocketState));
                }
            }
        }

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public override ILogger Logger
        {
            set
            {
                if (base.Logger != value)
                {
                    this.webSocket.Log.Output = WebSocketProxyLoggerOutputFactory.CreateWebSocketProxyLoggerOutput(value);
                }

                base.Logger = value;
            }
        }

        public WebSocketRpcClient(string url)
        {
            this.url = new Uri(url);
            this.webSocket = new WebSocket(url);
            this.webSocket.WaitTime = TimeSpan.FromMilliseconds(500);
            this.webSocket.Log.Level = LogLevel.Trace;
            this.webSocket.OnError += WebSocketOnError;
            this.webSocket.OnOpen += WebSocketOnOpen;
            this.webSocket.OnClose += WebSocketOnClose;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                this.webSocket.OnError -= WebSocketOnError;
                this.webSocket.OnOpen -= WebSocketOnOpen;
                this.webSocket.OnClose -= WebSocketOnClose;
                ((IDisposable) this.webSocket).Dispose();
            }

            this.disposed = true;
        }
        
        public override Task ConnectAsync()
        {
            AssertNotAlreadyConnectedOrConnecting();
            var tcs = new TaskCompletionSource<object>();
            EventHandler openHandler = null;
            EventHandler<CloseEventArgs> closeHandler = null;
            openHandler = (sender, e) =>
            {
                this.webSocket.OnOpen -= openHandler;
                this.webSocket.OnClose -= closeHandler;
                tcs.TrySetResult(null);
                this.Logger.Log(LogTag, "Connected to " + this.url.AbsoluteUri);
            };
            closeHandler = (sender, e) =>
            {
                tcs.SetException(new RpcClientException($"WebSocket closed unexpectedly with error {e.Code}: {e.Reason}"));
            };
            this.webSocket.OnOpen += openHandler;
            this.webSocket.OnClose += closeHandler;
            try
            {
                this.webSocket.ConnectAsync();
            }
            catch (Exception)
            {
                this.webSocket.OnOpen -= openHandler;
                this.webSocket.OnClose -= closeHandler;
                throw;
            }
            return tcs.Task;
        }

        public override Task DisconnectAsync()
        {
            // TODO: should be listening for disconnection all the time
            // and auto-reconnect if there are event subscriptions
            var tcs = new TaskCompletionSource<CloseEventArgs>();
            EventHandler<CloseEventArgs> handler = null;
            handler = (sender, e) =>
            {
                this.webSocket.OnClose -= handler;
                tcs.TrySetResult(e);
            };
            this.webSocket.OnClose += handler;
            try
            {
                this.webSocket.CloseAsync(CloseStatusCode.Normal, "Client disconnected.");
            }
            catch (Exception)
            {
                this.webSocket.OnClose -= handler;
                throw;
            }
            return tcs.Task;
        }

        public override Task SubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            var isFirstSub = this.eventReceived == null;
            this.eventReceived += handler;
            if (isFirstSub)
            {
                this.webSocket.OnMessage += WSSharpRPCClient_OnMessage;
            }
            // TODO: once re-sub on reconnect is implemented this should only
            // be done on first sub
            return SendAsync<object, object>("subevents", new object());
        }

        public override Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            this.eventReceived -= handler;
            if (this.eventReceived == null)
            {
                this.webSocket.OnMessage -= WSSharpRPCClient_OnMessage;
                return SendAsync<object, object>("unsubevents", new object());
            }
            return Task.CompletedTask;
        }

        public override async Task<T> SendAsync<T, U>(string method, U args)
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
                            this.webSocket.OnMessage -= handler;
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
                        this.Logger.Log(LogTag, "[ignoring msg]");
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };
            this.webSocket.OnMessage += handler;
            try
            {
                await SendAsync<U>(method, args, msgId);
            }
            catch (Exception)
            {
                this.webSocket.OnMessage -= handler;
                throw;
            }
            return await tcs.Task;
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            anyConnectionStateChangesReceived = true;
            NotifyConnectionStateChanged();
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            anyConnectionStateChangesReceived = true;
            NotifyConnectionStateChanged();
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            anyConnectionStateChangesReceived = true;
            this.Logger.Log(LogTag, "Error: " + e.Message);
            NotifyConnectionStateChanged();
        }

        private async Task SendAsync<T>(string method, T args, string msgId)
        {
            AssertIsConnected();
            var tcs = new TaskCompletionSource<object>();
            var reqMsg = new JsonRpcRequest<T>(method, args, msgId);
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            this.Logger.Log(LogTag, "[Request Body] " + reqMsgBody);

            ErrorEventArgs errorEventArgs = null;
            EventHandler<ErrorEventArgs> errorHandler = (sender, eventArgs) =>
            {
                errorEventArgs = eventArgs;
            };
            this.webSocket.OnError += errorHandler;
            this.webSocket.SendAsync(reqMsgBody, (bool success) =>
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
            this.webSocket.OnError -= errorHandler;
            await tcs.Task;
        }

        private void WSSharpRPCClient_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.IsText && !string.IsNullOrEmpty(e.Data))
                {
                    this.Logger.Log(LogTag, "[WSSharpRPCClient_OnMessage msg body] " + e.Data);
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
                            this.eventReceived?.Invoke(this, fullMsg.Result);
                        }
                    }
                }
                else
                {
                    this.Logger.Log(LogTag, "[WSSharpRPCClient_OnMessage ignoring msg]");
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(LogTag, "[WSSharpRPCClient_OnMessage error] " + ex);
            }
        }
    }
}

#endif