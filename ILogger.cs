using System;

namespace ClearDir
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message, Exception? exception = null);
    }
}
