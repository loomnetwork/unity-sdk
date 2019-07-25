using UnityEngine;

namespace Loom.Client
{
    public interface ILogProducer {
        /// <summary>
        /// Logger to be used for logging, defaults to <see cref="NullLogger"/>.
        /// </summary>
        ILogger Logger { get; set; }
    }
}
