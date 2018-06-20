using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Loom.Unity3d
{
    #region JSON RPC Interfaces

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

    public class JsonRpcEventData
    {
        // TODO: Create custom Newtonsoft JsonConverter to deserialize the protobuf,
        //       the tricky bit is getting it to work with the AOT version of Newtonsoft.
        public class Address
        {
            [JsonProperty("chain_id")]
            public string ChainID;
            [JsonProperty("local")]
            public byte[] Local;
        }

        [JsonProperty("topics")]
        public string[] Topics { get; internal set; }

        [JsonProperty("caller")]
        public Address CallerAddress { get; internal set; }

        [JsonProperty("address")]
        public Address ContractAddress { get; internal set; }

        [JsonProperty("block_height")]
        public UInt64 BlockHeight { get; internal set; }

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

    #endregion

    public interface IRPCClient : IDisposable
    {
        bool IsConnected { get; }
        Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        Task DisconnectAsync();
        Task SubscribeAsync(EventHandler<JsonRpcEventData> handler);
        Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler);
    }
}
