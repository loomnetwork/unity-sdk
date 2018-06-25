using System;
using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public interface IRpcClient : IDisposable
    {
        bool IsConnected { get; }
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
