using Google.Protobuf;
using Chaos.NaCl;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

namespace Loom.Unity3d
{
    #region JSON RPC Interfaces

    internal class TxJsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string Version { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public string[] Params { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        public TxJsonRpcRequest(string method, string[] args, string id = "")
        {
            Version = "2.0";
            Method = method;
            Params = args;
            Id = id;
        }
    }

    public class TxJsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string Version { get; set; }

        /// <summary>
        /// ID of the request associated with this response.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        public class ErrorData
        {
            [JsonProperty("code")]
            public long Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public string Data { get; set; }
        }

        [JsonProperty("error")]
        public ErrorData Error;
    }

    public class BroadcastTxResult
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty("log")]
        public string Log { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        /// <summary>
        /// Block height at which the Tx was committed.
        /// </summary>
        [JsonProperty("height")]
        public long Height { get; set; }
    }

    public class BroadcastTxResponse : TxJsonRpcResponse
    {
        [JsonProperty("result")]
        public BroadcastTxResult Result { get; set; }
    }

    #endregion

    /// <summary>
    /// Writes to & reads from a Loom DAppChain.
    /// </summary>
    public class DAppChainClient
    {
        private static readonly string LogTag = "Loom.DAppChainClient";

        private string url;

        /// <summary>
        /// Middleware to apply when committing transactions.
        /// </summary>
        public TxMiddleware TxMiddleware { get; set; }

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        public DAppChainClient(string url)
        {
            this.url = url;
            this.Logger = NullLogger.Instance;
        }

        /// <summary>
        /// Commits a transactions to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction to commit.</param>
        /// <returns>Commit metadata.</returns>
        public async Task<BroadcastTxResult> CommitTx(IMessage tx)
        {
            byte[] txBytes = tx.ToByteArray();
            if (this.TxMiddleware != null)
            {
                txBytes = await this.TxMiddleware.Handle(txBytes);
            }
            string payload = CryptoBytes.ToBase64String(txBytes);
            Logger.Log(LogTag, "Tx: " + payload);
            var req = new TxJsonRpcRequest("broadcast_tx_commit", new string[] { payload }, Guid.NewGuid().ToString());
            var resp = await this.PostTx(req);
            if (resp.Error != null)
            {
                throw new Exception(String.Format("Failed to commit Tx: {0} / {1} / {2}",
                    resp.Error.Code, resp.Error.Message, resp.Error.Data));
            }
            return resp.Result;
        }

        /// <summary>
        /// Queries the DAppChain state.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="path">DApp-specific path to query.</param>
        /// <param name="queryStr">DApp-specific query parameters.</param>
        /// <returns>Deserialized response.</returns>
        public async Task<T> QueryAsync<T>(string path, string queryStr = null)
        {
            // TODO: Provide a nicer interface for building the query string.
            var uriBuilder = new UriBuilder(this.url) { Path = path };
            if (!String.IsNullOrEmpty(queryStr))
            {
                uriBuilder.Query = queryStr;
            }
            using (var r = new UnityWebRequest(uriBuilder.Uri.AbsoluteUri, "GET"))
            {
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                HandleError(r);
                Logger.Log(LogTag, "HTTP response body: " + r.downloadHandler.text);
                return JsonConvert.DeserializeObject<T>(r.downloadHandler.text);
            }
            
        }

        private async Task<BroadcastTxResponse> PostTx(TxJsonRpcRequest tx)
        {
            string body = JsonConvert.SerializeObject(tx);
            Logger.Log(LogTag, "PostTx Body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.url, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "Response: " + r.downloadHandler.text);
                    return JsonConvert.DeserializeObject<BroadcastTxResponse>(r.downloadHandler.text);
                }
            }
            return null;
        }

        private static void HandleError(UnityWebRequest r)
        {
            if (r.isNetworkError)
            {
                throw new Exception(String.Format("HTTP '{0}' request to '{1}' failed", r.method, r.url));
            }
            else if (r.isHttpError)
            {
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    // TOOD: extract error message if any
                }
                throw new Exception(String.Format("HTTP Error {0}", r.responseCode));
            }
        }
    }
}