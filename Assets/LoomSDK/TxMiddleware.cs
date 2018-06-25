﻿using System.Threading.Tasks;

namespace Loom.Unity3d
{
    public class TxMiddleware : ITxMiddlewareHandler
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
