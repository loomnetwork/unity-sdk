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
        public Action OnMessage;
    }

    /// <summary>
    /// WebSocket JSON-RPC client implemented with browser WebSockets.
    /// </summary>
    internal class WSRPCClient : IRPCClient
    {
        private static Dictionary<int, WebSocket> sockets = new Dictionary<int, WebSocket>();

        [DllImport("__Internal")]
        private static extern int WebSocketCreate(Action<int> openCallback, Action<int> msgCallback);
        [DllImport("__Internal")]
        private static extern WebSocketState GetWebSocketState(int sockedId);
        [DllImport("__Internal")]
        private static extern void WebSocketConnect(int socketId, string url);
        [DllImport("__Internal")]
        private static extern void WebSocketSend(int socketId, string msg);
        [DllImport("__Internal")]
        private static extern string GetWebSocketMessage(int socketId);

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketOpen(int socketId)
        {
            sockets[socketId].OnOpen();
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketMessage(int socketId)
        {
            sockets[socketId].OnMessage();
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
            this.url = new Uri(url);
            this.Logger = NullLogger.Instance;
            this.socketId = WebSocketCreate(OnWebSocketOpen, OnWebSocketMessage);
            sockets.Add(this.socketId, new WebSocket());
        }

        void IDisposable.Dispose()
        {
            this.Disconnect();
        }

        public Task Disconnect()
        {
            // TODO
            return Task.CompletedTask;
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