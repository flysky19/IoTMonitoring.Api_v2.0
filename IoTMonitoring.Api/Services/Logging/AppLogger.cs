using System;
using System.IO;
using IoTMonitoring.Api.Services.Logging.Interfaces;

namespace IoTMonitoring.Api.Services.Logging
{
    public class AppLogger : IAppLogger
    {
        private readonly string _logDirectory;
        private readonly string _logFileName;
        private static readonly object _lockObject = new object();

        public AppLogger()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _logFileName = $"app-{DateTime.Now:yyyy-MM-dd}.log";

            // 로그 디렉토리 생성
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void LogInformation(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        public void LogError(string message, Exception exception)
        {
            WriteLog("ERROR", $"{message}\nException: {exception}");
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        public void LogCritical(string message)
        {
            WriteLog("CRITICAL", message);
        }

        public void LogCritical(string message, Exception exception)
        {
            WriteLog("CRITICAL", $"{message}\nException: {exception}");
        }

        private void WriteLog(string level, string message)
        {
            lock (_lockObject)
            {
                try
                {
                    var logFilePath = Path.Combine(_logDirectory, _logFileName);
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);

                    // 콘솔에도 출력 (개발 환경에서 유용)
                    Console.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    // 로깅 실패 시 콘솔에만 출력
                    Console.WriteLine($"로깅 실패: {ex.Message}");
                }
            }
        }
    }
}