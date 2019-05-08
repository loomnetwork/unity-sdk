using System.Threading.Tasks;

namespace Loom.Client
{
    public class TxMiddleware : ITxMiddlewareHandler
    {
        public ITxMiddlewareHandler[] Handlers { get; }

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

        public void HandleTxResult(BroadcastTxResult result)
        {
            for (int i = 0; i < this.Handlers.Length; i++)
            {
                this.Handlers[i].HandleTxResult(result);
            }
        }

        public void HandleTxException(LoomException exception)
        {
            for (int i = 0; i < this.Handlers.Length; i++)
            {
                this.Handlers[i].HandleTxException(exception);
            }
        }
    }
}
