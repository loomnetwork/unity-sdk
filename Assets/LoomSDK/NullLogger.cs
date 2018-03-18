using System;
using UnityEngine;

namespace Loom.Unity3d
{
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        public ILogHandler logHandler { get; set; }
        public bool logEnabled { get; set; }
        public LogType filterLogType { get; set; }

        public bool IsLogTypeAllowed(LogType logType)
        {
            throw new NotImplementedException();
        }

        public void Log(LogType logType, object message)
        {
        }

        public void Log(LogType logType, object message, UnityEngine.Object context)
        {
        }

        public void Log(LogType logType, string tag, object message)
        {
        }

        public void Log(LogType logType, string tag, object message, UnityEngine.Object context)
        {
        }

        public void Log(object message)
        {
        }

        public void Log(string tag, object message)
        {
        }

        public void Log(string tag, object message, UnityEngine.Object context)
        {
        }

        public void LogError(string tag, object message)
        {
        }

        public void LogError(string tag, object message, UnityEngine.Object context)
        {
        }

        public void LogException(Exception exception)
        {
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
        }

        public void LogWarning(string tag, object message)
        {
        }

        public void LogWarning(string tag, object message, UnityEngine.Object context)
        {
        }
    }
}
