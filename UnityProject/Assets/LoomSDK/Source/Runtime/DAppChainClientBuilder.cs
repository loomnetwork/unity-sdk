using System;
using UnityEngine;

namespace Loom.Client
{
    /// <summary>
    /// Builds an instance of <see cref="DAppChainClient"/>.
    /// </summary>
    public class DAppChainClientBuilder
    {
        private IRpcClient reader;
        private IRpcClient writer;
        private ILogger logger;
        private DAppChainClientConfiguration configuration;
        private TxMiddleware txMiddleware;
        private IDAppChainClientCallExecutor callExecutor;

        public DAppChainClientBuilder()
        {
        }

        public static DAppChainClientBuilder Configure()
        {
            return new DAppChainClientBuilder();
        }

        public DAppChainClientBuilder WithWriter(IRpcClient writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            this.writer = writer;
            return this;
        }

        public DAppChainClientBuilder WithReader(IRpcClient reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            this.reader = reader;
            return this;
        }

        public DAppChainClientBuilder WithTxMiddleware(TxMiddleware txMiddleware)
        {
            if (txMiddleware == null)
                throw new ArgumentNullException(nameof(txMiddleware));

            this.txMiddleware = txMiddleware;
            return this;
        }

        public DAppChainClientBuilder WithCallExecutor(IDAppChainClientCallExecutor callExecutor)
        {
            if (callExecutor == null)
                throw new ArgumentNullException(nameof(callExecutor));

            this.callExecutor = callExecutor;
            return this;
        }

        public DAppChainClientBuilder WithConfiguration(DAppChainClientConfiguration configuration)
        {
            this.configuration = configuration;
            return this;
        }

        public DAppChainClientBuilder WithLogger(ILogger logger)
        {
            this.logger = logger;
            return this;
        }

        public DAppChainClient Create()
        {
            DAppChainClientConfiguration configuration = this.configuration ?? new DAppChainClientConfiguration();
            IDAppChainClientCallExecutor callExecutor = this.callExecutor ?? new DefaultDAppChainClientCallExecutor(this.configuration);

            return new DAppChainClient(this.writer, this.reader, configuration, callExecutor)
            {
                TxMiddleware = this.txMiddleware,
                Logger = this.logger ?? NullLogger.Instance
            };
        }

    }

}
