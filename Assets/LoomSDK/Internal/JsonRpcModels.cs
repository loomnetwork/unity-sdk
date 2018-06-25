using Newtonsoft.Json;

namespace Loom.Unity3d.Internal
{
    internal class JsonRpcRequest<T>
    {
        [JsonProperty("jsonrpc")]
        public string Version;

        [JsonProperty("method")]
        public string Method;

        [JsonProperty("params")]
        public T Params;

        [JsonProperty("id")]
        public string Id;

        public JsonRpcRequest(string method, T args, string id = "")
        {
            Version = "2.0";
            Method = method;
            Params = args;
            Id = id;
        }
    }

    internal class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string Version;

        public class ErrorData
        {
            [JsonProperty("code")]
            public long Code;

            [JsonProperty("message")]
            public string Message;

            [JsonProperty("data")]
            public string Data;
        }

        [JsonProperty("error")]
        public ErrorData Error;

        /// <summary>
        /// ID of the request associated with this response.
        /// </summary>
        [JsonProperty("id")]
        public string Id;
    }

    internal class JsonRpcResponse<T> : JsonRpcResponse
    {
        [JsonProperty("result")]
        public T Result;
    }

    internal class JsonRpcEvent : JsonRpcResponse
    {
        [JsonProperty("result")]
        public JsonRpcEventData Result;
    }

    internal class BroadcastTxResult
    {
        [JsonProperty("code")]
        public string Code { get; set; }

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

        [JsonProperty("check_tx")]
        public TxResult CheckTx { get; set; }

        [JsonProperty("deliver_tx")]
        public TxResult DeliverTx { get; set; }

        public class TxResult
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("log")]
            public string Error { get; set; }

            [JsonProperty("data")]
            public byte[] Data { get; set; }
        }
    }
}
