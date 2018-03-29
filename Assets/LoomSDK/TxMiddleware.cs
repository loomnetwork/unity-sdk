using System.Threading.Tasks;

namespace Loom.Unity3d
{
    /// <summary>
    /// Middleware handlers are expected to transform the input data and return the result.
    /// Handlers should not modify the original input data in any way.
    /// </summary>
    public interface ITxMiddlewareHandler
    {
        Task<byte[]> Handle(byte[] txData);
    }

    public class TxMiddleware
    {
        ITxMiddlewareHandler[] Handlers { get; set; }

        public TxMiddleware(ITxMiddlewareHandler[] handlers)
        {
            this.Handlers = handlers;
        }

        public async Task<byte[]> Handle(byte[] txData)
        {
            byte[] data = txData;
            for (int i = 0; i < this.Handlers.Length; i++)
            {
                data = await this.Handlers[i].Handle(data);
            }
            return data;
        }
    }
}
