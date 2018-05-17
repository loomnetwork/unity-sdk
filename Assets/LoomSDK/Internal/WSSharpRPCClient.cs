#if !UNITY_WEBGL || UNITY_EDITOR

using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;

namespace Loom.Unity3d
{
    /// <summary>
    /// WebSocket JSON-RPC client implemented with WebSocketSharp.
    /// </summary>
    internal class WSSharpRPCClient : IRPCClient
    {
        private static readonly string LogTag = "Loom.WSSharpRPCClient";

        private WebSocket client;
        private Uri url;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public WSSharpRPCClient(string url)
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
            var tcs = new TaskCompletionSource<CloseEventArgs>();
            EventHandler<CloseEventArgs> handler = null;
            handler = (sender, e) =>
            {
                this.client.OnClose -= handler;
                tcs.TrySetResult(e);
            };
            this.client.OnClose += handler;
            try
            {
                this.client.CloseAsync(CloseStatusCode.Normal, "Client disconnected.");
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
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
            EventHandler handler = null;
            handler = (sender, e) =>
            {
                this.client.OnOpen -= handler;
                tcs.TrySetResult(null);
                Logger.Log(LogTag, "Connected to " + this.url.AbsoluteUri);
            };
            this.client.OnOpen += handler;
            try
            {
                this.client.ConnectAsync();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            return tcs.Task;
        }

        private async Task SendAsync<T>(string method, T args)
        {
            var tcs = new TaskCompletionSource<object>();
            await this.EnsureConnectionAsync();
            var reqMsg = new JsonRpcRequest<T>(method, args, Guid.NewGuid().ToString());
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            Logger.Log(LogTag, "RPC Req: " + reqMsgBody);
            try
            {
                this.client.SendAsync(reqMsgBody, (bool success) =>
                {
                    if (success)
                    {
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        // TODO: sub to this.client.OnError() and store the error when it happens,
                        // then throw it in here.
                        tcs.TrySetException(new Exception("Send error"));
                    }
                });
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
            await tcs.Task;
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            var tcs = new TaskCompletionSource<T>();
            EventHandler<MessageEventArgs> handler = null;
            handler = async (sender, e) =>
            {
                await new WaitForUpdate();
                this.client.OnMessage -= handler;
                if (e.IsText)
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Log(LogTag, "RPC Resp Body: " + e.Data);
                        var respMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(e.Data);
                        tcs.TrySetResult(respMsg.Result);
                    }
                    else
                    {
                        tcs.TrySetResult(default(T));
                    }
                }
                else
                {
                    throw new Exception("Unexpected message type!");
                }
            };
            this.client.OnMessage += handler;
            await this.SendAsync<U>(method, args);
            return await tcs.Task;
        }
    }
}

#endif