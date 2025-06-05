using System;

namespace IoTMonitoring.Api.Services.Logging.Interfaces
{
    public interface IAppLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception exception);
        void LogDebug(string message);
        void LogCritical(string message);
        void LogCritical(string message, Exception exception);
    }
}