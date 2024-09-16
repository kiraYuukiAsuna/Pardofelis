using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Util;

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _lock = new object();

    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        var logRecord = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{logLevel}] {formatter(state, exception)}";
        lock (_lock)
        {
            File.AppendAllText(_filePath, logRecord + Environment.NewLine);
        }
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_filePath);
    }

    public void Dispose()
    {
        // Cleanup if necessary
    }
}
