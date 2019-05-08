using Loom.Newtonsoft.Json;

namespace Loom.Client.Internal
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
        public string Height { get; set; }

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

    internal struct FilterRpcModel
    {
        [JsonProperty("filter")]
        public string Filter { get; set; }
    }
}
