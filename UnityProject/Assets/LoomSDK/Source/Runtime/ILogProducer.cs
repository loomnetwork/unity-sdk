using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loom.Client;
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
