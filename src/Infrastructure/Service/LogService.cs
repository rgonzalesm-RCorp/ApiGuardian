using System;
using System.IO;
using ApiGuardian.Application.Interfaces;

namespace ApiGuardian.Infrastructure.Services;

public class LogService : ILogService
{
    private readonly string _logDirectory;

    public LogService()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
    }

    private void WriteLog(string level, string message, Exception? ex = null)
    {
        string logFile = Path.Combine(_logDirectory, $"{DateTime.Now:yyyyMMdd}.log");
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        if (ex != null)
            logMessage += $"{Environment.NewLine}Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}";

        File.AppendAllText(logFile, logMessage + Environment.NewLine);
    }

    public void Info(string message) => WriteLog("INFO", message);
    public void Warning(string message) => WriteLog("WARN", message);
    public void Error(string message, Exception? ex = null) => WriteLog("ERROR", message, ex);
}
