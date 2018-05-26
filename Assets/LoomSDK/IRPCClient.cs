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

    #endregion

    public class EventData
    {
        public class Address
        {
            public string ChainID;
            public byte[] Local;
        }

        [JsonProperty("caller")]
        public Address CallerAddress;

        [JsonProperty("address")]
        public Address ContractAddress;

        // PluginName  string       `json:"plugin"`
        // BlockHeight int64        `json:"blockHeight"`

        [JsonProperty("encodedData")]
        public byte[] Data;
        
        // RawRequest[]byte       `json:"rawRequest"`
    }
    
    public interface IRPCClient : IDisposable
    {
        Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        Task DisconnectAsync();
        Task SubscribeAsync(EventHandler<EventData> handler);
        Task UnsubscribeAsync(EventHandler<EventData> handler);
    }
}
