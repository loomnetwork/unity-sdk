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

    internal class JsonRpcErrorResponse
    {
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

    internal class JsonRpcResponse<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }

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

        public class TxResult
        {
            [JsonProperty("code")]
            public int Code { get; set; }
            [JsonProperty("log")]
            public string Error { get; set; }
        }

        [JsonProperty("check_tx")]
        public TxResult CheckTx { get; set; }
        [JsonProperty("deliver_tx")]
        public TxResult DeliverTx { get; set; }
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
        [JsonProperty("result")]
        public ulong Result { get; set; }
    }

    #endregion

    public static class IdentityExtensions
    {
        /// <summary>
        /// Generate a DAppChain address for the given identity.
        /// Address generation is based on the identity public key and the chain ID,
        /// the algorithm is deterministic.
        /// </summary>
        /// <param name="identity">Identity with a valid public key.</param>
        /// <param name="chainId">Identifier of a DAppChain.</param>
        /// <returns>An address</returns>
        public static Address ToAddress(this Identity identity, string chainId)
        {
            return new Address
            {
                ChainId = chainId,
                Local = ByteString.CopyFrom(CryptoUtils.LocalAddressFromPublicKey(identity.PublicKey))
            };
        }
    }

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
        /// <param name="url">Loom DAppChain URL e.g. "http://localhost"</param>
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
        /// <param name="caller">Address of the caller.</param>
        /// <param name="contract">Address of a contract on the Loom DAppChain.</param>
        /// <param name="method">Qualified name of the contract method to call in the format "contractName.methodName".</param>
        /// <param name="args">Arguments to pass to the contract.</param>
        /// <returns>Commit metadata.</returns>
        public async Task<BroadcastTxResult> CallAsync(Address caller, Address contract, string method, IMessage args)
        {
            var methodTx = new ContractMethodCall
            {
                Method = method,
                Data = Google.Protobuf.WellKnownTypes.Any.Pack(args)
            };
            var requestBytes = new Request
            {
                ContentType = EncodingType.Protobuf3,
                Body = methodTx.ToByteString()
            }.ToByteString();

            var callTxBytes = new CallTx
            {
                VmType = VMType.Plugin,
                Input = requestBytes
            }.ToByteString();

            var msgTxBytes = new MessageTx
            {
                From = caller,
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
        private async Task<BroadcastTxResult> CommitTxAsync(IMessage tx)
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
            var result = resp.Result;
            if (result != null)
            {
                if (result.CheckTx.Code != 0)
                {
                    if (string.IsNullOrEmpty(result.CheckTx.Error))
                    {
                        throw new Exception(String.Format("Failed to commit Tx: {0}", result.CheckTx.Code));
                    }
                    throw new Exception(String.Format("Failed to commit Tx: {0}", result.CheckTx.Error));
                }
                if (result.DeliverTx.Code != 0)
                {
                    if (string.IsNullOrEmpty(result.DeliverTx.Error))
                    {
                        throw new Exception(String.Format("Failed to commit Tx: {0}", result.DeliverTx.Code));
                    }
                    throw new Exception(String.Format("Failed to commit Tx: {0}", result.DeliverTx.Error));
                }
            }
            return result;
        }

        /// <summary>
        /// Queries the current state of a contract.
        /// </summary>
        /// <typeparam name="T">The expected response type, must be deserializable with Newtonsoft.Json.</typeparam>
        /// <param name="contract">Address of the contract to query.</param>
        /// <param name="method">Qualified name of the contract method to call in the format "contractName.methodName".</param>
        /// <param name="queryParams">Query parameters object, must be serializable with Newtonsoft.Json.</param>
        /// <returns>Deserialized response.</returns>
        public async Task<T> QueryAsync<T>(Address contract, string method, object queryParams = null)
        {
            var query = new ContractMethodCallJSON
            {
                Method = method,
                Data = ByteString.CopyFromUtf8(JsonConvert.SerializeObject(queryParams))
            };
            var contractAddr = "0x" + CryptoUtils.BytesToHexString(contract.Local.ToByteArray());
            var req = new QueryJsonRpcRequest("query", contractAddr, query, Guid.NewGuid().ToString());
            string body = JsonConvert.SerializeObject(req);
            Logger.Log(LogTag, "Query body: " + body);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(body);
            using (var r = new UnityWebRequest(this.readUrl, "POST"))
            {
                r.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                this.HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "Response: " + r.downloadHandler.text);
                    var resp = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(r.downloadHandler.text);
                    return resp.Result;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Gets a nonce for the given public key.
        /// </summary>
        /// <param name="key">A hex encoded public key, e.g. 441B9DCC47A734695A508EDF174F7AAF76DD7209DEA2D51D3582DA77CE2756BE</param>
        /// <returns>The nonce.</returns>
        public async Task<ulong> GetNonceAsync(string key)
        {
            var uriBuilder = new UriBuilder(this.readUrl)
            {
                Path = "nonce",
                Query = string.Format("key=\"{0}\"", key)
            };
            using (var r = new UnityWebRequest(uriBuilder.Uri.AbsoluteUri, "GET"))
            {
                r.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                await r.SendWebRequest();
                this.HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "HTTP response body: " + r.downloadHandler.text);
                    var resp = JsonConvert.DeserializeObject<NonceResponse>(r.downloadHandler.text);
                    return resp.Result;
                }
            }
            return 0;
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
                this.HandleError(r);
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    Logger.Log(LogTag, "Response: " + r.downloadHandler.text);
                    return JsonConvert.DeserializeObject<BroadcastTxResponse>(r.downloadHandler.text);
                }
            }
            return null;
        }

        private void HandleError(UnityWebRequest r)
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
            else if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
            {
                JsonRpcErrorResponse resp = null;
                try
                {
                    resp = JsonConvert.DeserializeObject<JsonRpcErrorResponse>(r.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Logger.LogError(LogTag, e.Message);
                }
                if (resp.Error != null)
                {
                    throw new Exception(String.Format("JSON-RPC Error {0} ({1}): {2}", resp.Error.Code, resp.Error.Message, resp.Error.Data));
                }
            }
        }
    }
}