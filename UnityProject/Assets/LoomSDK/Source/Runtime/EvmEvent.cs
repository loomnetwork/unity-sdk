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
using Loom.Nethereum.ABI.Model;

namespace Loom.Client
{
    /// <summary>
    /// Represent a Solidity event.
    /// </summary>
    public class EvmEvent<T> where T : new()
    {
        public EvmContract Contract { get; }

        public EventABI EventAbi { get; }

        public EvmEvent(EvmContract contract, EventABI eventAbi)
        {
            this.Contract = contract;
            this.EventAbi = eventAbi;
        }

        public async Task<FilterLog[]> GetAllChangesRaw(NewFilterInput filterInput)
        {
            EthFilterLogList logs = await GetAllChangesInternal(filterInput);
            return
                logs.EthBlockLogs
                    .Select(ConvertEthFilterLogToFilterLog)
                    .ToArray();
        }

        public async Task<List<EventLog<T>>> GetAllChanges(NewFilterInput filterInput)
        {
            FilterLog[] changes = await GetAllChangesRaw(filterInput);
            return this.EventAbi.DecodeAllEvents<T>(changes);
        }

        private async Task<EthFilterLogList> GetAllChangesInternal(NewFilterInput filterInput)
        {
            return await this.Contract.Client.CallExecutor.StaticCall(
                async () =>
                {
                    string base64 = await this.Contract.Client.ReadClient.SendAsync<string, FilterRpcModel>(
                        "getevmlogs",
                        new FilterRpcModel
                        {
                            Filter = JsonConvert.SerializeObject(filterInput)
                        }
                    );

                    byte[] bytes = CryptoBytes.FromBase64String(base64);
                    return EthFilterLogList.Parser.ParseFrom(bytes);
                },
                new CallDescription("getevmlogs", true)
            );
        }

        private static FilterLog ConvertEthFilterLogToFilterLog(EthFilterLog log)
        {
            return new FilterLog
            {
                Address = Address.FromBytes(log.Address.ToByteArray()).LocalAddress,
                Data = CryptoUtils.BytesToHexString(log.Data.ToByteArray()),
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
