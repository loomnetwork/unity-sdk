using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Loom.Client
{
    public delegate void RpcClientConnectionStateChangedHandler(IRpcClient sender, RpcConnectionState state);

    public interface IRpcClient : IDisposable
    {
        event RpcClientConnectionStateChangedHandler ConnectionStateChanged;
        event EventHandler<JsonRpcEventData> EventReceived;

        RpcConnectionState ConnectionState { get; }
        Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        Task ConnectAsync();
        Task DisconnectAsync();
        Task SubscribeToEventsAsync(ICollection<string> topics);
        Task UnsubscribeFromEventAsync(string topic);
    }

    [Obsolete("Use IRpcClient", true)]
    public interface IRPCClient : IRpcClient
    {
    }
}
