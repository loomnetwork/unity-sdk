using System;
using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public delegate int RpcClientConnectionStateChangedHandler(IRpcClient sender, RpcConnectionState state);

    public interface IRpcClient : IDisposable
    {
        event RpcClientConnectionStateChangedHandler ConnectionStateChanged;

        bool IsConnectable { get; }
        RpcConnectionState ConnectionState { get; }
        Task<TResult> SendAsync<TResult, TArgs>(string method, TArgs args);
        Task DisconnectAsync();
        Task SubscribeAsync(EventHandler<JsonRpcEventData> handler);
        Task UnsubscribeAsync(EventHandler<JsonRpcEventData> handler);
    }

    [Obsolete("Use IRpcClient", true)]
    public interface IRPCClient : IRpcClient
    {
    }
}
