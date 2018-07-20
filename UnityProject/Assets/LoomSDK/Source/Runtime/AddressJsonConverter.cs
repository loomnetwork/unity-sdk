using System;
using System.Text;
using Loom.Newtonsoft.Json;

namespace Loom.Client.Internal
{
    internal class AddressJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Address address = (Address) value;
            AddressJsonModel jsonModel = new AddressJsonModel
            {
                ChainId = address.ChainId,
                Local = CryptoUtils.HexStringToBytes(address.LocalAddress)
            };
            serializer.Serialize(writer, jsonModel);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            AddressJsonModel jsonModel = serializer.Deserialize<AddressJsonModel>(reader);
            return new Address(CryptoUtils.BytesToHexString(jsonModel.Local), String.IsNullOrEmpty(jsonModel.ChainId) ? Address.kDefaultChainId : jsonModel.ChainId);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Address);
        }

        private struct AddressJsonModel
        {
            [JsonProperty("chain_id")]
            public string ChainId;

            [JsonProperty("local")]
            public byte[] Local;
        }
    }
}
