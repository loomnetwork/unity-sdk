#if UNITY_WEBGL

using AOT;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.Unity3d.WebGL
{
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
        public Action OnMessage;
    }

    /// <summary>
    /// WebSocket JSON-RPC client implemented with browser WebSockets.
    /// </summary>
    internal class WSRPCClient : IRPCClient
    {
        private static Dictionary<int, WebSocket> sockets = new Dictionary<int, WebSocket>();
        private static bool isLibInitialized = false;

        [DllImport("__Internal")]
        private static extern void InitWebSocketManagerLib(
            Action<int> openCallback,
            Action<int,string> closeCallback,
            Action<int> msgCallback);
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
        [DllImport("__Internal")]
        private static extern string GetWebSocketMessage(int socketId);

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketOpen(int socketId)
        {
            var socket = sockets[socketId];
            socket.OnOpen();
            socket.OnOpen = null;
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
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
        private static void OnWebSocketMessage(int socketId)
        {
            var socket = sockets[socketId];
            socket.OnMessage();
            socket.OnMessage = null;
        }

        private static readonly string LogTag = "Loom.WSRPCClient";

        private Uri url;
        private int socketId = 0;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public WSRPCClient(string url)
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
                    tcs.TrySetException(new Exception(err));
                }
            };
            try
            {
                this.Disconnect();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
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
                tcs.TrySetException(e);
            }
            return tcs.Task;
        }

        private async Task SendAsync<T>(string method, T args)
        {
            await this.EnsureConnectionAsync();
            var reqMsg = new JsonRpcRequest<T>(method, args, Guid.NewGuid().ToString());
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            Logger.Log(LogTag, "RPC Req: " + reqMsgBody);
            WebSocketSend(this.socketId, reqMsgBody);
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            var webSocket = sockets[this.socketId];
            var tcs = new TaskCompletionSource<T>();
            webSocket.OnMessage = () =>
            {
                var msgBody = GetWebSocketMessage(this.socketId);
                if (!string.IsNullOrEmpty(msgBody))
                {
                    Logger.Log(LogTag, "RPC Resp Body: " + msgBody);
                    var respMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(msgBody);
                    tcs.TrySetResult(respMsg.Result);
                }
                else
                {
                    tcs.TrySetResult(default(T));
                }
            };
            await this.SendAsync<U>(method, args);
            return await tcs.Task;
        }
    }
}

#endif