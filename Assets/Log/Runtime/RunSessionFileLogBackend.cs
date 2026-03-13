#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class RunSessionFileLogBackend : ILogBackend
{
    private const int MaxLogFileCount = 20;
    private const string SessionFileTimeFormat = "yyyy_MM_dd-HH_mm_ss";

    private static readonly object SyncRoot = new object();

    private static FileStream _stream;
    private static StreamWriter _writer;
    private static string _currentSessionFilePath;
    private static bool _sessionStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        Shutdown();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartSessionForPlayMode()
    {
        BeginNewSession();
        ZLog.EnsureInitialized();
    }

    public static void BeginNewSession()
    {
        lock (SyncRoot)
        {
            ShutdownWriterNoLock();
            InitializeSessionNoLock();
        }
    }

    public static void Shutdown()
    {
        lock (SyncRoot)
        {
            ShutdownWriterNoLock();
        }
    }

    public void Log(LogData data)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (!_sessionStarted)
            {
                InitializeSessionNoLock();
            }

            if (_writer == null)
            {
                return;
            }

            try
            {
                _writer.WriteLine(FormatLogLine(data));
                FlushWriterNoLock();
            }
            catch
            {
                ShutdownWriterNoLock();
            }
        }
    }

    private static void InitializeSessionNoLock()
    {
        if (_sessionStarted)
        {
            return;
        }

        try
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var logDirectory = Path.Combine(projectRoot, "LogData");
            Directory.CreateDirectory(logDirectory);

            _currentSessionFilePath = Path.Combine(logDirectory, $"{DateTime.Now.ToString(SessionFileTimeFormat)}.log");
            _stream = new FileStream(_currentSessionFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.WriteThrough);
            _writer = new StreamWriter(_stream, new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            _sessionStarted = true;

            _writer.WriteLine($"# ArmyGroup Runtime Log");
            _writer.WriteLine($"SessionStart: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            _writer.WriteLine($"UnityVersion: {Application.unityVersion}");
            _writer.WriteLine($"ProjectRoot: {projectRoot}");
            _writer.WriteLine();
            FlushWriterNoLock();

            TrimOldLogsNoLock(logDirectory);
        }
        catch
        {
            ShutdownWriterNoLock();
        }
    }

    private static void TrimOldLogsNoLock(string logDirectory)
    {
        try
        {
            var files = new DirectoryInfo(logDirectory)
                .GetFiles("*.log", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .ToList();

            for (int i = MaxLogFileCount; i < files.Count; i++)
            {
                if (string.Equals(files[i].FullName, _currentSessionFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                files[i].Delete();
            }
        }
        catch
        {
        }
    }

    private static void ShutdownWriterNoLock()
    {
        if (_writer != null)
        {
            try
            {
                _writer.Flush();
                _writer.Dispose();
            }
            catch
            {
            }
        }

        _writer = null;
        _stream = null;
        _currentSessionFilePath = null;
        _sessionStarted = false;
    }

    private static void FlushWriterNoLock()
    {
        if (_writer == null)
        {
            return;
        }

        _writer.Flush();
        _stream?.Flush(true);
    }

    private static string FormatLogLine(LogData data)
    {
        var builder = new StringBuilder(512);
        var fileName = string.IsNullOrEmpty(data.FilePath) ? "UnknownFile" : Path.GetFileName(data.FilePath);
        var channel = string.IsNullOrEmpty(data.Channel) ? "Default" : data.Channel;

        builder.Append('[')
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append("] [")
            .Append(data.Level)
            .Append("] [")
            .Append(channel)
            .Append("] ")
            .Append(FormatMessage(data))
            .AppendLine();

        builder.Append("  Source: ")
            .Append(fileName)
            .Append(':')
            .Append(data.LineNumber)
            .Append(" (")
            .Append(string.IsNullOrEmpty(data.MemberName) ? "UNKNOWN" : data.MemberName)
            .Append(')');

        if (!string.IsNullOrEmpty(data.FilePath))
        {
            builder.Append(" <- ").Append(data.FilePath);
        }

        if (data.Context != null)
        {
            builder.AppendLine();
            builder.Append("  Context: ").Append(data.Context.name);
        }

        if (!string.IsNullOrWhiteSpace(data.StackTraceText))
        {
            builder.AppendLine();
            builder.AppendLine("  StackTrace:");
            builder.Append(data.StackTraceText);
        }

        builder.AppendLine();
        return builder.ToString();
    }

    private static string FormatMessage(LogData data)
    {
        if (data.Level == LogLevel.Exception && data.Message is Exception ex)
        {
            return ex.ToString();
        }

        return data.Message?.ToString() ?? "NULL";
    }
}
#endif
