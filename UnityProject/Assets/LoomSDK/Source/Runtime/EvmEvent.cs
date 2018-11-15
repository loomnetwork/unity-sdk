using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Loom.Chaos.NaCl;
using Loom.Client.Internal;
using Loom.Client.Protobuf;
using Loom.Nethereum.Contracts;
using Loom.Nethereum.Hex.HexTypes;
using Loom.Nethereum.RPC.Eth.DTOs;
using Loom.Newtonsoft.Json;
using Loom.Newtonsoft.Json.Linq;

namespace Loom.Client
{
    public class EvmEvent
    {
        protected EvmContract Contract { get; }
        protected EventBuilder EventBuilder { get; }

        public EvmEvent(EvmContract contract, EventBuilder eventBuilder)
        {
            this.Contract = contract;
            this.EventBuilder = eventBuilder;
        }

        public NewFilterInput CreateFilterInput(BlockParameter fromBlock = null, BlockParameter toBlock = null)
        {
            return EventBuilder.CreateFilterInput(fromBlock, toBlock);
        }

        public NewFilterInput CreateFilterInput(object[] filterTopic1, BlockParameter fromBlock = null,
            BlockParameter toBlock = null)
        {
            return EventBuilder.CreateFilterInput(filterTopic1, fromBlock, toBlock);
        }

        public NewFilterInput CreateFilterInput(object[] filterTopic1, object[] filterTopic2,
            BlockParameter fromBlock = null, BlockParameter toBlock = null)
        {
            return EventBuilder.CreateFilterInput(filterTopic1, filterTopic2, fromBlock, toBlock);
        }

        public NewFilterInput CreateFilterInput(object[] filterTopic1, object[] filterTopic2, object[] filterTopic3,
            BlockParameter fromBlock = null, BlockParameter toBlock = null)
        {
            return EventBuilder.CreateFilterInput(filterTopic1, filterTopic2, filterTopic3, fromBlock, toBlock);
        }

        public static List<EventLog<T>> DecodeAllEvents<T>(FilterLog[] logs) where T : new()
        {
            return EventBuilder.DecodeAllEvents<T>(logs);
        }

        public async Task<FilterLog[]> GetAllChanges(NewFilterInput filterInput)
        {
            EthFilterLogList logs = await GetAllChangesInternal(filterInput);
            return
                logs.EthBlockLogs
                    .Select(ConvertEthFilterLogToFilterLog)
                    .ToArray();
        }

        public bool IsLogForEvent(JToken log)
        {
            return EventBuilder.IsLogForEvent(log);
        }

        public bool IsLogForEvent(FilterLog log)
        {
            return EventBuilder.IsLogForEvent(log);
        }

        public List<EventLog<T>> DecodeAllEventsForEvent<T>(FilterLog[] logs) where T : new()
        {
            return EventBuilder.DecodeAllEventsForEvent<T>(logs);
        }

        public List<EventLog<T>> DecodeAllEventsForEvent<T>(JArray logs) where T : new()
        {
            return EventBuilder.DecodeAllEventsForEvent<T>(logs);
        }

        private async Task<EthFilterLogList> GetAllChangesInternal(NewFilterInput filterInput)
        {
            return await this.Contract.Client.CallExecutor.StaticCall(async () =>
            {
                string base64 = await this.Contract.Client.ReadClient.SendAsync<string, FilterRpcModel>("getevmlogs",
                    new FilterRpcModel
                    {
                        Filter = JsonConvert.SerializeObject(filterInput)
                    });

                byte[] bytes = CryptoBytes.FromBase64String(base64);
                return EthFilterLogList.Parser.ParseFrom(bytes);
            });
        }

        private static FilterLog ConvertEthFilterLogToFilterLog(EthFilterLog log)
        {
            return new FilterLog
            {
                Address = Address.FromBytes(log.Address.ToByteArray()).LocalAddress,
                Data = log.Data.ToBase64(),
                Removed = log.Removed,
                Topics = log.Topics.Select(t => (object) t.ToStringUtf8()).ToArray(),
                Type = "",
                BlockHash = CryptoBytes.ToHexStringLower(log.BlockHash.ToByteArray()),
                BlockNumber = new HexBigInteger(log.BlockNumber),
                LogIndex = new HexBigInteger(log.LogIndex),
                TransactionHash = CryptoBytes.ToHexStringLower(log.TransactionHash.ToByteArray()),
                TransactionIndex = new HexBigInteger(log.TransactionIndex)
            };
        }
    }
}
