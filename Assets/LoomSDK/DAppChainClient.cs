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

    public class DAppChainClient
    {
        private string url;

        public DAppChainClient(string url)
        {
            this.url = url;
        }

        public string SignTx(DummyTx tx, byte[] privateKey)
        {
            var sig = CryptoUtils.Sign(tx.ToByteArray(), privateKey);

            var signer = new Signer
            {
                Signature = ByteString.CopyFrom(sig.Signature),
                PublicKey = ByteString.CopyFrom(sig.PublicKey)
            };

            var signedTx = new SignedTx
            {
                Inner = tx.ToByteString(),
                Signers = { signer }
            };

            var payload = CryptoBytes.ToBase64String(signedTx.ToByteArray());
            Debug.Log("Signed Tx: " + payload);
            return payload;
        }

        public async Task<BroadcastTxResult> CommitTx(string signedTx)
        {
            var req = new TxJsonRpcRequest("broadcast_tx_commit", new string[] { signedTx }, Guid.NewGuid().ToString());
            var resp = await this.PostTx(req);
            if (resp.Error != null)
            {
                throw new System.Exception(String.Format("Failed to commit Tx: {0} / {1} / {2}",
                    resp.Error.Code, resp.Error.Message, resp.Error.Data));
            }
            return resp.Result;
        }

        private async Task<BroadcastTxResponse> PostTx(TxJsonRpcRequest tx)
        {
            string body = JsonConvert.SerializeObject(tx);
            Debug.Log("PostTx Body: " + body);
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
                    Debug.Log("Response: " + r.downloadHandler.text);
                    return JsonConvert.DeserializeObject<BroadcastTxResponse>(r.downloadHandler.text);
                }
            }
            return null;
        }

        private static void HandleError(UnityWebRequest r)
        {
            if (r.isNetworkError)
            {
                throw new System.Exception(String.Format("HTTP '{0}' request to '{1}' failed", r.method, r.url));
            }
            else if (r.isHttpError)
            {
                if (r.downloadHandler != null && !String.IsNullOrEmpty(r.downloadHandler.text))
                {
                    // TOOD: extract error message if any
                }
                throw new System.Exception(String.Format("HTTP Error {0}", r.responseCode));
            }
        }
    }
}