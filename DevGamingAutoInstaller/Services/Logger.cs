using System;
using System.IO;
using System.Text;

namespace DevGamingAutoInstaller.Services;

public sealed class Logger
{
    private readonly string _logPath;
    private readonly object _syncRoot = new();

    public Logger(string logPath)
    {
        _logPath = logPath;
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        lock (_syncRoot)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }
}
