using Newtonsoft.Json;
using System;

namespace Loom.Unity3d
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
}