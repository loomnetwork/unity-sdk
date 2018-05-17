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

    internal class JsonRpcResponse<T>
    {
        [JsonProperty("jsonrpc")]
        public string Version;

        [JsonProperty("result")]
        public T Result;

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

    #endregion

    public interface IRPCClient : IDisposable
    {
        Task<T> SendAsync<T, U>(string method, U args);
        Task DisconnectAsync();
    }
}
