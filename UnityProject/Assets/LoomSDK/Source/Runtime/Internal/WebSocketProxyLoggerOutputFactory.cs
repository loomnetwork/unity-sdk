#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using Loom.WebSocketSharp;
using UnityEngine;

namespace Loom.Client.Internal
{
    internal static class WebSocketProxyLoggerOutputFactory
    {
        public static Action<LogData, string> CreateWebSocketProxyLoggerOutput(WebSocketRpcClient webSocketRpcClient, ILogger logger)
        {
            string tag = "WebSocket (" + webSocketRpcClient.Url + ")";
            return (data, s) =>
            {
                switch (data.Level)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        logger.Log(tag, data.Message);
                        break;
                    case LogLevel.Warn:
                        logger.LogWarning(tag, data.Message);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        logger.LogError(tag, data.Message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }
    }
}

#endif
