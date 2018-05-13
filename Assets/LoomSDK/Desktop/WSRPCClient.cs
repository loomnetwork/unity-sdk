using System.Threading.Tasks;
using System.Net.WebSockets;
using System;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using UnityEngine;

namespace Loom.Unity3d
{
    internal class WSRPCClient : IRPCClient
    {
        private static readonly string LogTag = "Loom.WSRPCClient";
        
        private ClientWebSocket client;
        private Uri url;
        private byte[] responseBuffer;

        public int MaxMessageSize;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public WSRPCClient(string url)
        {
            this.client = new ClientWebSocket();
            this.client.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(5000);
            this.url = new Uri(url);
            this.Logger = NullLogger.Instance;
            this.responseBuffer = new byte[4096];
            this.MaxMessageSize = 16 * 1024;
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        public async Task Disconnect()
        {
            await this.client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        private async Task EnsureConnectionAsync()
        {
            if (this.client.State == WebSocketState.Open)
            {
                return;
            }
            await this.client.ConnectAsync(this.url, CancellationToken.None);
            Logger.Log(LogTag, "Connected to " + this.url.AbsoluteUri);
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            await this.EnsureConnectionAsync();
            var reqMsg = new JsonRpcRequest<U>(method, args);
            var reqMsgBody = JsonConvert.SerializeObject(reqMsg);
            Logger.Log(LogTag, "RPC Req: " + reqMsgBody);
            var reqBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(reqMsgBody));
            await this.client.SendAsync(reqBytes, WebSocketMessageType.Text, true, CancellationToken.None);
            Logger.Log(LogTag, "RPC Req Sent");
            using (var memStream = new MemoryStream())
            {
                int msgSize = 0;
                WebSocketReceiveResult result = null;
                var respBytes = new ArraySegment<byte>(this.responseBuffer);
                do
                {
                    result = await this.client.ReceiveAsync(respBytes, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new Exception("Socket closed.");
                    }
                    if (msgSize + result.Count > this.MaxMessageSize)
                    {
                        throw new Exception("Message exceeded max size!");
                    }
                    Logger.Log(LogTag, string.Format("reqBytes.Offset: {0}, result.Count: {1}", reqBytes.Offset, result.Count));
                    memStream.Write(reqBytes.Array, reqBytes.Offset, result.Count);
                    msgSize += result.Count;
                } while (!result.EndOfMessage);

                memStream.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using (var reader = new StreamReader(memStream, Encoding.UTF8))
                    {
                        var respMsgBody = reader.ReadToEnd();
                        Logger.Log(LogTag, "RPC Resp: " + respMsgBody);
                        var respMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(respMsgBody);
                        return respMsg.Result;
                    }
                }
                else
                {
                    throw new Exception("Unexpected message type!");
                }
            }
        }
    }
}
