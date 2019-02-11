using System;
using System.Threading.Tasks;
using Loom.Client.Internal;

namespace Loom.Client
{
    /// <summary>
    /// Middleware handlers are expected to transform the input data and return the result.
    /// Handlers should not modify the original input data in any way.
    /// </summary>
    public interface ITxMiddlewareHandler
    {
        Task<byte[]> Handle(byte[] txData);

        void HandleTxResult(BroadcastTxResult result);

        void HandleTxException(LoomException exception);
    }
}
