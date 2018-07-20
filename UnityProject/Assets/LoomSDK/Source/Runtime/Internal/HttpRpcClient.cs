using System.Threading.Tasks;
using System;
using Loom.Newtonsoft.Json;
using System.Text;
using Loom.Client.Unity.Internal.UnityAsyncAwaitUtil;
using UnityEngine.Networking;
using UnityEngine;

namespace Loom.Client.Internal
{
    internal class HttpRpcClient : IRpcClient
    {
        private const string LogTag = "Loom.HttpRpcClient";

        private readonly Uri url;

        public event RpcClientConnectionStateChangedHandler ConnectionStateChanged
        {
            add { throw new NotSupportedException(); }
            remove { throw new NotSupportedException(); }
        }

        public RpcConnectionState ConnectionState => RpcConnectionState.NonApplicable;

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public Task SubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler)
        {
            throw new NotImplementedException();
        }

        public HttpRpcClient(string url)
        {
            this.url = new Uri(url);
            this.Logger = NullLogger.Instance;
        }

        public void Dispose()
        {
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<T> SendAsync<T, U>(string method, U args)
        {
            string body = JsonConvert.SerializeObject(new JsonRpcRequest<U>(method, args, Guid.NewGuid().ToString()));
            Logger.Log(LogTag, "[Request Body] " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.url.AbsoluteUri, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                this.HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "[Response Body] " + r.downloadHandler.text);
                    var respMsg = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(r.downloadHandler.text);
                    if (respMsg.Error != null)
                    {
                        throw new RpcClientException(String.Format(
                            "JSON-RPC Error {0} ({1}): {2}",
                            respMsg.Error.Code, respMsg.Error.Message, respMsg.Error.Data
                        ));
                    }
                    return respMsg.Result;
                }
                Logger.Log(LogTag, "[Empty Response Body]");
            }
            return default(T);
        }

        private void HandleError(UnityWebRequest r)
        {
            if (r.isNetworkError)
            {
                throw new RpcClientException(String.Format("HTTP '{0}' request to '{1}' failed", r.method, r.url));
            }
            else if (r.isHttpError)
            {
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    // TODO: extract error message if any
                }
                throw new RpcClientException(String.Format("HTTP Error {0}", r.responseCode));
            }
        }
    }
}
