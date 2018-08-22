#if UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace Loom.Client.Unity.WebGL.Internal
{
    internal class WebSocket : IDisposable
    {
        private static readonly Dictionary<int, WebSocket> sockets = new Dictionary<int, WebSocket>();

        private readonly int socketId;

        public event Action Opened;
        public event Action<string> Closed;
        public event EventHandler<string> MessageReceived;

        public WebSocketState State => GetWebSocketState(this.socketId);

        static WebSocket() {
            InitWebSocketManagerLib(OnWebSocketOpen, OnWebSocketClose, OnWebSocketMessage);
        }

        public WebSocket()
        {
            this.socketId = WebSocketCreate();
            sockets.Add(this.socketId, this);
        }

        public void Connect(string url)
        {
            WebSocketConnect(this.socketId, url);
        }

        public void Close()
        {
            WebSocketClose(this.socketId);
        }

        public void Send(string message)
        {
            WebSocketSend(this.socketId, message);
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketOpen(int socketId)
        {
            WebSocket socket = sockets[socketId];
            socket.Opened?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        private static void OnWebSocketClose(int socketId, string err)
        {
            // If DisconnectAsync() was called from Dispose() the socket is no longer
            WebSocket socket;
            if (sockets.TryGetValue(socketId, out socket))
            {
                socket.Closed?.Invoke(err);
            }
        }

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnWebSocketMessage(int socketId, string msg)
        {
            WebSocket socket = sockets[socketId];
            socket.MessageReceived?.Invoke(socket, msg);
        }

        private void ReleaseUnmanagedResources()
        {
            WebSocketClose(this.socketId);
            sockets.Remove(this.socketId);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~WebSocket()
        {
            ReleaseUnmanagedResources();
        }

        [DllImport("__Internal")]
        private static extern void InitWebSocketManagerLib(
            Action<int> openCallback,
            Action<int,string> closeCallback,
            Action<int,string> msgCallback);

        [DllImport("__Internal")]
        private static extern int WebSocketCreate();

        [DllImport("__Internal")]
        private static extern WebSocket.WebSocketState GetWebSocketState(int sockedId);

        [DllImport("__Internal")]
        private static extern void WebSocketConnect(int socketId, string url);

        [DllImport("__Internal")]
        private static extern void WebSocketClose(int socketId);

        [DllImport("__Internal")]
        private static extern void WebSocketSend(int socketId, string msg);

        public enum WebSocketState : int
        {
            Connecting = 0,
            Open = 1,
            Closing = 2,
            Closed = 3
        }
    }
}

#endif