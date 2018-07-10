#if UNITY_WEBGL

using AOT;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.Unity3d.Internal.WebGL
{
    /// <summary>
    /// WebSocket JSON-RPC client implemented with browser WebSockets.
    /// </summary>
    internal class WebSocketRpcClient : IRpcClient
    {
        private const string LogTag = "Loom.WebSocketRpcClient";
        private static readonly Dictionary<int, WebSocket> sockets = new Dictionary<int, WebSocket>();
        private static bool isLibInitialized = false;

        private readonly Uri url;
        private readonly int socketId = 0;
        private event EventHandler<JsonRpcEventData> OnEventMessage;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public WebSocketRpcClient(string url)
        {
            if (!isLibInitialized)
            {
                InitWebSocketManagerLib(OnWebSocketOpen, OnWebSocketClose, OnWebSocketMessage);
                isLibInitialized = true;
            }
            this.url = new Uri(url);
            this.Logger = NullLogger.Instance;
            this.socketId = WebSocketCreate();
            sockets.Add(this.socketId, new WebSocket());
        }

        public bool IsConnected => GetWebSocketState(this.socketId) == WebSocketState.Open;

        public void Dispose()
        {
            this.Disconnect();
            sockets.Remove(this.socketId);
        }

        private void Disconnect()
        {
            // NOTE: The current implementation of this function will destroy the
            // browser-side socket so it can't be re-opened... this could be
            // fixed in the JS code if needed.
            WebSocketClose(this.socketId);
        }

        public Task DisconnectAsync()
        {
            var currentState = GetWebSocketState(this.socketId);
            if ((currentState == WebSocketState.Closed) || (currentState == WebSocketState.Closing))
            {
                return Task.CompletedTask;
            }
            var webSocket = sockets[this.socketId];
            var tcs = new TaskCompletionSource<object>();
            webSocket.OnClose = (err) =>
            {
                if (string.IsNullOrEmpty(err))
                {
                    tcs.TrySetResult(null);
                }
                else
                {
                    tcs.TrySetException(new RpcClientException(err));
                }
            };
            try
            {
                this.Disconnect();
            }
            catch (Exception e)
            {
                webSocket.OnClose = null;
                throw e;
            }
            return tcs.Task;
        }

        private Task EnsureConnectionAsync()
        {
            if (GetWebSocketState(this.socketId) == WebSocketState.Open)
            {
                return Task.CompletedTask;
            }
            var webSocket = sockets[this.socketId];
            var tcs = new TaskCompletionSource<object>();
            webSocket.OnOpen = () =>
            {
                tcs.TrySetResult(null);
                Logger.Log(LogTag, "Connected to " + this.url.AbsoluteUri);
            };
            try
            {
                WebSocketConnect(this.socketId, this.url.AbsoluteUri);
            }
            catch (Exception e)
            {
                webSocket.OnOpen = null;
                throw e;
            }
            return tcs.Task;
        }

        public Task SubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            var isFirstSub = this.OnEventMessage == null;
            this.OnEventMessage += handler;
            if (isFirstSub)
            {
                var webSocket = sockets[this.socketId];
                webSocket.OnMessage += this.WSRPCClient_OnMessage;
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
                var webSocket = sockets[this.socketId];
                webSocket.OnMessage -= this.WSRPCClient_OnMessage;
                return this.SendAsync<object, object>("unsubevents", new object());
            }
            return Task.CompletedTask;
        }

        private async Task SendAsync<T>(string method, T args, string msgId)
        {
            await this.EnsureConnectionAsync();
            var reqMsg = new JsonRpcRequest<T>(method, args, msgId);
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            Logger.Log(LogTag, "RPC Req: " + reqMsgBody);
            WebSocketSend(this.socketId, reqMsgBody);
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            var webSocket = sockets[this.socketId];
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
                            webSocket.OnMessage -= handler;
                            Logger.Log(LogTag, "RPC Resp Body: " + msgBody);
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
                        Logger.Log(LogTag, "[ignoring msg]");
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };
            webSocket.OnMessage += handler;
            try
            {
                await this.SendAsync<U>(method, args, msgId);
            }
            catch (Exception e)
            {
                webSocket.OnMessage -= handler;
                throw e;
            }
            return await tcs.Task;
        }

        private void WSRPCClient_OnMessage(object sender, string msgBody)
        {
            try
            {
                if (!string.IsNullOrEmpty(msgBody))
                {
                    Logger.Log(LogTag, "[WSRPCClient_OnMessage msg body] " + msgBody);
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
                            this.OnEventMessage?.Invoke(this, fullMsg.Result);
                        }
                    }
                }
                else
                {
                    Logger.Log(LogTag, "[WSRPCClient_OnMessage ignoring msg]");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogTag, "[WSRPCClient_OnMessage error] " + ex.Message);
            }
        }

        [DllImport("__Internal")]
        private static extern void InitWebSocketManagerLib(
            Action<int> openCallback,
            Action<int,string> closeCallback,
            Action<int,string> msgCallback);
        [DllImport("__Internal")]
        private static extern int WebSocketCreate();
        [DllImport("__Internal")]
        private static extern WebSocketState GetWebSocketState(int sockedId);
        [DllImport("__Internal")]
        private static extern void WebSocketConnect(int socketId, string url);
        [DllImport("__Internal")]
        private static extern void WebSocketClose(int socketId);
        [DllImport("__Internal")]
        private static extern void WebSocketSend(int socketId, string msg);

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketOpen(int socketId)
        {
            var socket = sockets[socketId];
            socket.OnOpen();
            socket.OnOpen = null;
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        private static void OnWebSocketClose(int socketId, string err)
        {
            // If DisconnectAsync() was called from Dispose() the socket is no longer
            if (sockets.ContainsKey(socketId))
            {
                var socket = sockets[socketId];
                if (socket.OnClose != null)
                {
                    socket.OnClose(err);
                    socket.OnClose = null;
                }
            }
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketMessage(int socketId, string msg)
        {
            var socket = sockets[socketId];
            socket.OnMessage?.Invoke(socket, msg);
        }

        internal enum WebSocketState : int
        {
            Connecting = 0,
            Open = 1,
            Closing = 2,
            Closed = 3
        }

        internal class WebSocket
        {
            public Action OnOpen;
            public Action<string> OnClose;
            public EventHandler<string> OnMessage;
        }
    }
}

#endif