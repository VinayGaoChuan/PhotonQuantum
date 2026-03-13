#if DUMP
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
#if !BATTLE_SERVER
using UnityEngine;
using Object = UnityEngine.Object;
#endif

/// <summary>
/// 帧同步校验 DUMP 后台：仅在定义 DUMP 宏时生效。
/// 统一将日志写入本地文件，每条一行，便于前后端文件对比。
/// 行格式： [frame]|member|file:line|level|message
/// 其中 level 用数字（Info=0, Warning=1, Error=2, Exception=3）简短且稳定。
/// </summary>
public sealed class DumpLogBackend : ILogBackend, IDisposable
{
    private static readonly object _lock = new object();
    private static StreamWriter _writer;
    private static string _filePath;
    private static string _sessionTag;


    /// <summary>
    /// 可选：设置一次会话标签（如 battleId、前/后端标记），用于区分文件名。
    /// 不增加 Logger 新 API，外部在需要时调用即可（编译仅在 DUMP 下）。
    /// 例：DumpLogBackend.SetSession($"Battle_{battleId}_Client");
    /// </summary>
    [Conditional("DUMP")]
    public static void SetSession(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        foreach (var c in Path.GetInvalidFileNameChars()) tag = tag.Replace(c, '_');
        lock (_lock)
        {
            _sessionTag = tag;
            ReopenWriter_NoLock();
        }
    }

    [DebuggerHidden, DebuggerStepThrough]
    public void Log(LogData data)
    {
        var level = data.Level;
        var message = data.Message;
        if (level == LogLevel.Exception && message is Exception ex)
            message = ex.Message; // 只记录 Message，避免平台差异栈信息导致不可比

        string fileName = Path.GetFileName(data.FilePath); // 只保留文件名，避免路径差异
        var sb = StringTool.GetStringBuilder();
        sb.Append('[').Append(FrameCounter.CurrentFrame).Append(']');
        sb.Append('|').Append(data.MemberName);
        sb.Append('|').Append(fileName).Append(':').Append(data.LineNumber);
        sb.Append('|').Append((int)level);

        // 统一为单行，避免换行导致对比困难
        var text = (message?.ToString() ?? "NULL")
            .Replace("\r\n", "\n").Replace('\r', '\n').Replace('\n', ' ');
        sb.Append('|').Append(text);

        if (!string.IsNullOrWhiteSpace(data.StackTraceText))
        {
            var stackText = data.StackTraceText
                .Replace("\r\n", "\n").Replace('\r', '\n').Replace('\n', ' ');
            sb.Append("|stack:").Append(stackText);
        }

        string line = sb.ToString();

        lock (_lock)
        {
            EnsureOpen_NoLock();
            _writer?.WriteLine(line);
            if (level >= LogLevel.Error) _writer?.Flush(); // 出错时尽量落盘
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }

    // ---------- 内部 ----------

    private static void EnsureOpen_NoLock()
    {
        if (_writer == null) ReopenWriter_NoLock();
    }

    private static void ReopenWriter_NoLock()
    {
        try
        {
            _writer?.Dispose();
            string baseDir;
#if !BATTLE_SERVER
            baseDir = Path.Combine(Application.persistentDataPath, "Dumps");
#else
            baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Dumps");
#endif
            Directory.CreateDirectory(baseDir);

            string time = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string pid = Process.GetCurrentProcess().Id.ToString();
            string fileName = string.IsNullOrEmpty(_sessionTag)
                ? $"dump_{time}_p{pid}.log"
                : $"dump_{time}_{_sessionTag}_p{pid}.log";

            _filePath = Path.Combine(baseDir, fileName);
            _writer = new StreamWriter(new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                                       Encoding.UTF8, 64 * 1024) { AutoFlush = false };

            // 头部说明（只占几行，便于定位和确认）
            _writer.WriteLine("# DUMP v1");
            _writer.WriteLine("# format: [frame]|member|file:line|level|message");
            _writer.WriteLine($"# TimeUTC={time}");
            _writer.WriteLine($"# ProcId={pid}");
#if !BATTLE_SERVER
            _writer.WriteLine("# Platform=Unity");
#else
            _writer.WriteLine("# Platform=Server");
#endif
            _writer.Flush();
        }
        catch
        {
            // 打开失败不影响运行；若无权限/路径问题，DUMP 将静默失效
        }
    }
}
#endif
