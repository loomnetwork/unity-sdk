#if UNITY_WEBGL

using Loom.Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.ComponentModel;
using Loom.Client.Internal;
using UnityEngine;

namespace Loom.Client.Unity.WebGL.Internal
{
    /// <summary>
    /// WebSocket JSON-RPC client implemented with browser WebSockets.
    /// </summary>
    internal class WebSocketRpcClient : BaseRpcClient
    {
        private const string LogTag = "Loom.WebSocketRpcClient";

        private readonly Uri url;
        private readonly WebSocket webSocket;
        private event EventHandler<JsonRpcEventData> eventReceived;

        public override RpcConnectionState ConnectionState
        {
            get
            {
                WebSocket.WebSocketState state = this.webSocket.State;
                switch (state)
                {
                    case WebSocket.WebSocketState.Connecting:
                        return RpcConnectionState.Connecting;
                    case WebSocket.WebSocketState.Open:
                        return RpcConnectionState.Connected;
                    case WebSocket.WebSocketState.Closing:
                        return RpcConnectionState.Disconnecting;
                    case WebSocket.WebSocketState.Closed:
                        return RpcConnectionState.Disconnected;
                    default:
                        throw new InvalidEnumArgumentException(nameof(state), (int) state, typeof(WebSocket.WebSocketState));
                }
            }
        }

        public WebSocketRpcClient(string url)
        {
            this.url = new Uri(url);
            this.webSocket = new WebSocket();
        }
        
        public override async Task ConnectAsync()
        {
            AssertNotAlreadyConnectedOrConnecting();
            var tcs = new TaskCompletionSource<object>();

            Action openedHandler = () =>
            {
                tcs.TrySetResult(null);
                this.Logger.Log(LogTag, "Connected to " + this.url.AbsoluteUri);
            };

            this.webSocket.Opened += openedHandler;
            try
            {
                this.webSocket.Connect(this.url.AbsoluteUri);
                NotifyConnectionStateChanged();
            }
            catch (Exception)
            {
                this.webSocket.Opened -= openedHandler;
                NotifyConnectionStateChanged();
                throw;
            }
            await tcs.Task;
        }

        public override async Task DisconnectAsync()
        {
            var currentState = this.webSocket.State;
            if ((currentState == WebSocket.WebSocketState.Closed) || (currentState == WebSocket.WebSocketState.Closing))
            {
                return;
            }
            var tcs = new TaskCompletionSource<object>();

            Action<string> closedHandler = (err) =>
            {
                if (string.IsNullOrEmpty(err))
                {
                    tcs.TrySetResult(null);
                } else
                {
                    tcs.TrySetException(new RpcClientException(err));
                }
            };

            this.webSocket.Closed += closedHandler;
            try
            {
                Disconnect();
            }
            catch (Exception)
            {
                this.webSocket.Closed -= closedHandler;
                NotifyConnectionStateChanged();
                throw;
            }
            await tcs.Task;
        }

        public override async Task SubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            var isFirstSub = this.eventReceived == null;
            this.eventReceived += handler;
            if (isFirstSub)
            {
                this.webSocket.MessageReceived += WSRPCClient_MessageReceived;
            }
            // TODO: once re-sub on reconnect is implemented this should only
            // be done on first sub
            await SendAsync<object, object>("subevents", new object());
        }

        public override async Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            this.eventReceived -= handler;
            if (this.eventReceived == null)
            {
                this.webSocket.MessageReceived -= WSRPCClient_MessageReceived;
                await SendAsync<object, object>("unsubevents", new object());
            }
        }

        public override async Task<T> SendAsync<T, U>(string method, U args)
        {
            var tcs = new TaskCompletionSource<T>();
            var msgId = Guid.NewGuid().ToString();
            EventHandler<string> handler = null;
            handler = (sender, msgBody) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(msgBody))
                    {
                        var partialMsg = JsonConvert.DeserializeObject<JsonRpcResponse>(msgBody);
                        if (partialMsg.Id == msgId)
                        {
                            this.webSocket.MessageReceived -= handler;
                            this.Logger.Log(LogTag, "RPC Resp Body: " + msgBody);
                            if (partialMsg.Error != null)
                            {
                                throw new RpcClientException(String.Format(
                                    "JSON-RPC Error {0} ({1}): {2}",
                                    partialMsg.Error.Code, partialMsg.Error.Message, partialMsg.Error.Data
                                ));
                            }
                            else
                            {
                                var fullMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(msgBody);
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

                NotifyConnectionStateChanged();
            };
            this.webSocket.MessageReceived += handler;
            try
            {
                await SendAsync<U>(method, args, msgId);
                NotifyConnectionStateChanged();
            }
            catch (Exception)
            {
                this.webSocket.MessageReceived -= handler;
                NotifyConnectionStateChanged();
                throw;
            }
            return await tcs.Task;
        }

        protected override void Dispose(bool disposing)
        {
            this.webSocket.Dispose();
        }

        private Task SendAsync<T>(string method, T args, string msgId)
        {
            var reqMsg = new JsonRpcRequest<T>(method, args, msgId);
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            this.Logger.Log(LogTag, "RPC Req: " + reqMsgBody);
            this.webSocket.Send(reqMsgBody);
            return Task.CompletedTask;
        }

        private void Disconnect()
        {
            // NOTE: The current implementation of this function will destroy the
            // browser-side socket so it can't be re-opened... this could be
            // fixed in the JS code if needed.
            this.webSocket.Close();

            NotifyConnectionStateChanged();
        }

        private void WSRPCClient_MessageReceived(object sender, string msgBody)
        {
            try
            {
                if (!string.IsNullOrEmpty(msgBody))
                {
                    this.Logger.Log(LogTag, "[WSRPCClient_MessageReceived msg body] " + msgBody);
                    var partialMsg = JsonConvert.DeserializeObject<JsonRpcResponse>(msgBody);
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
                            var fullMsg = JsonConvert.DeserializeObject<JsonRpcEvent>(msgBody);
                            this.eventReceived?.Invoke(this, fullMsg.Result);
                        }
                    }
                }
                else
                {
                    this.Logger.Log(LogTag, "[WSRPCClient_MessageReceived ignoring msg]");
                }
            }
            catch (Exception ex)
            {
                this.Logger.Log(LogTag, "[WSRPCClient_MessageReceived error] " + ex.Message);
            }

            NotifyConnectionStateChanged();
        }

    }

}

#endif