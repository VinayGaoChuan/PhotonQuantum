#if BATTLE_SERVER && !UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;

public class BattleServerLogBackend : ILogBackend
{
    [DebuggerHidden, DebuggerStepThrough]
    public void Log(LogData data)
    {
        var level = data.Level;
        var message = data.Message;
        if (level == LogLevel.Exception && message is Exception ex)
        {
            message = ex.ToString();
        }

        string levelStr = level.ToString();
        switch (level)
        {
            case LogLevel.Info:
                levelStr = levelStr.ToColor(ColorEnum.Green);
                break;
            case LogLevel.Warning:
                levelStr = levelStr.ToColor(ColorEnum.Yellow);
                break;
            case LogLevel.Error:
            case LogLevel.Exception:
                levelStr = levelStr.ToColor(ColorEnum.Red);
                break;
            default:
                break;
        }

        var sb = StringTool.GetStringBuilder();
        var fileName = Path.GetFileName(data.FilePath);
        sb.Append('[').Append(levelStr).Append("]").Append(message ?? "NULL").Append("\n").Append(fileName).Append(':').Append(data.LineNumber).Append("(").Append(data.MemberName).Append(")");
        if (!string.IsNullOrWhiteSpace(data.StackTraceText))
        {
            sb.Append('\n').Append(data.StackTraceText);
        }
        Console.WriteLine(sb.ToString());
    }
}
#endif
