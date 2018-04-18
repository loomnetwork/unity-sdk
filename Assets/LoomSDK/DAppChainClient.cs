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

    internal class QueryJsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string Version { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }

        public class QueryParams
        {
            [JsonProperty("contract")]
            public string Contract { get; set; }
            [JsonProperty("query")]
            public object Query { get; set; }
        }
        [JsonProperty("params")]
        public QueryParams Params { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }

        public QueryJsonRpcRequest(string method, string contract, object query, string id = "")
        {
            Version = "2.0";
            Method = method;
            Params = new QueryParams
            {
                Contract = contract,
                Query = query
            };
            Id = id;
        }
    }

    internal class NonceResponse
    {
        public class NonceResult
        {
            [JsonProperty("nonce")]
            public ulong Nonce { get; set; }
        }
        [JsonProperty("result")]
        public NonceResult Result { get; set; }
    }

    #endregion

    /// <summary>
    /// Writes to & reads from a Loom DAppChain.
    /// </summary>
    public class DAppChainClient
    {
        private static readonly string LogTag = "Loom.DAppChainClient";

        private string writeUrl;
        private string readUrl;

        /// <summary>
        /// Middleware to apply when committing transactions.
        /// </summary>
        public TxMiddleware TxMiddleware { get; set; }

        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Constructs a client to read & write data from/to a Loom DAppChain.
        /// </summary>
        /// <param name="url">e.g. "http://localhost"</param>
        /// <param name="writePort">Port number for the write interface.</param>
        /// <param name="readPort">Port number for the read interface.</param>
        public DAppChainClient(string url, int writePort, int readPort)
        {
            this.writeUrl = string.Format("{0}:{1}", url, writePort);
            this.readUrl = string.Format("{0}:{1}", url, readPort);
            this.Logger = NullLogger.Instance;
        }

        /// <summary>
        /// Calls a contract with the given arguments.
        /// Each call generates a new transaction that's committed to the Loom DAppChain.
        /// </summary>
        /// <param name="contract">Address of a contract on the Loom DAppChain.</param>
        /// <param name="args">Arguments to pass to the contract.</param>
        /// <returns>Commit metadata.</returns>
        public async Task<BroadcastTxResult> CallAsync(Address contract, IMessage args)
        {
            var callTxBytes = new CallTx
            {
                VmType = VMType.Plugin,
                Input = args.ToByteString()
            }.ToByteString();

            var msgTxBytes = new MessageTx
            {
                From = null, // TODO: fill this in
                To = contract,
                Data = callTxBytes
            }.ToByteString();

            var tx = new Transaction
            {
                Id = 2,
                Data = msgTxBytes
            };
            return await this.CommitTxAsync(tx);
        }

        /// <summary>
        /// Commits a transaction to the DAppChain.
        /// </summary>
        /// <param name="tx">Transaction to commit.</param>
        /// <returns>Commit metadata.</returns>
        public async Task<BroadcastTxResult> CommitTxAsync(IMessage tx)
        {
            byte[] txBytes = tx.ToByteArray();
            if (this.TxMiddleware != null)
            {
                txBytes = await this.TxMiddleware.Handle(txBytes);
            }
            string payload = CryptoBytes.ToBase64String(txBytes);
            Logger.Log(LogTag, "Tx: " + payload);
            var req = new TxJsonRpcRequest("broadcast_tx_commit", new string[] { payload }, Guid.NewGuid().ToString());
            var resp = await this.PostTxAsync(req);
            if (resp.Error != null)
            {
                throw new Exception(String.Format("Failed to commit Tx: {0} / {1} / {2}",
                    resp.Error.Code, resp.Error.Message, resp.Error.Data));
            }
            return resp.Result;
        }

        /// <summary>
        /// Queries the current state of a contract.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="contract">Address of the contract to query.</param>
        /// <param name="query">Query parameters object, must be serializable with Newtonsoft.Json.</param>
        /// <returns>Deserialized response.</returns>
        public async Task<T> QueryAsync<T>(Address contract, object query = null)
        {
            // TODO: serialize contract address to a hex string
            var req = new QueryJsonRpcRequest("query", "", query, Guid.NewGuid().ToString());
            string body = JsonConvert.SerializeObject(req);
            Logger.Log(LogTag, "Query body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.readUrl, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "Response: " + r.downloadHandler.text);
                    return JsonConvert.DeserializeObject<T>(r.downloadHandler.text);
                }
            }
            return default(T);
        }

        /// <summary>
        /// Gets a nonce for the given public key.
        /// </summary>
        /// <param name="key">A hex encoded public key.</param>
        /// <returns>The nonce.</returns>
        public async Task<ulong> GetNonceAsync(string key)
        {
            var uriBuilder = new UriBuilder(this.readUrl)
            {
                Path = "nonce",
                Query = key
            };
            using (var r = new UnityWebRequest(uriBuilder.Uri.AbsoluteUri, "GET"))
            {
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                HandleError(r);
                Logger.Log(LogTag, "HTTP response body: " + r.downloadHandler.text);
                var resp = JsonConvert.DeserializeObject<NonceResponse>(r.downloadHandler.text);
                return resp.Result.Nonce;
            }
        }

        private async Task<BroadcastTxResponse> PostTxAsync(TxJsonRpcRequest tx)
        {
            string body = JsonConvert.SerializeObject(tx);
            Logger.Log(LogTag, "PostTx Body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.writeUrl, "POST"))
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