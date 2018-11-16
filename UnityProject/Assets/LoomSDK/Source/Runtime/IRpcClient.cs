using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Loom.Client
{
    public delegate void RpcClientConnectionStateChangedHandler(IRpcClient sender, RpcConnectionState state);

    public interface IRpcClient : IDisposable
    {
        event RpcClientConnectionStateChangedHandler ConnectionStateChanged;

        RpcConnectionState ConnectionState { get; }
        Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SubscribeAsync(EventHandler<JsonRpcEventData> handler, ICollection<string> topics);
        Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler);
    }

    [Obsolete("Use IRpcClient", true)]
    public interface IRPCClient : IRpcClient
    {
    }
}
