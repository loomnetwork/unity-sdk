using Loom.Newtonsoft.Json;
using System;

namespace Loom.Client
{
    public class JsonRpcEventData
    {
        [JsonProperty("topics")]
        public string[] Topics { get; internal set; }

        [JsonProperty("caller")]
        public Address CallerAddress { get; internal set; }

        [JsonProperty("address")]
        public Address ContractAddress { get; internal set; }

        [JsonProperty("block_height")]
        public string BlockHeight { get; internal set; }

        [JsonProperty("encoded_body")]
        public byte[] Data { get; internal set; }

        // Ignore these fields until there's a concrete use for them.*/
        /*
        [JsonProperty("plugin_name")]
        public string PluginName { get; internal set; }
        [JsonProperty("original_request")]
        public byte[] OriginalRequest { get; internal set; }
        */
    }

    public class BroadcastTxResult
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

    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string Version;

        public class ErrorData
        {
            [JsonProperty("code")]
            public string Code;

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
}
