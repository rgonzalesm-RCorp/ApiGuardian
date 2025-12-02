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

    private void WriteLog(string id,  string archivo, string metodo, string level, string message, Exception? ex = null)
    {
        string logFile = Path.Combine(_logDirectory, $"{DateTime.Now:yyyyMMdd}.log");
        string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{id}] [{archivo} --> {metodo}]  {message}";

        if (ex != null)
            logMessage += $"{Environment.NewLine}Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}";

        File.AppendAllText(logFile, logMessage + Environment.NewLine);
        Console.WriteLine(logMessage);
    }

    public void Info(string id, string archivo, string metodo,  string message) => WriteLog(id,  archivo,  metodo,  "INFO", message);
    public void Warning(string id, string archivo, string metodo,  string message) => WriteLog(id, archivo, metodo,"WARN", message);
    public void Error(string id, string archivo, string metodo,  string message, Exception? ex = null) => WriteLog(id, archivo, metodo,"ERROR", message, ex);
}
