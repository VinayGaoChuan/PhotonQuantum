#if WX_MINIGAME && !UNITY_EDITOR
using System;
using System.Diagnostics;

public class MiniGameLogBackend : ILogBackend
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
        var sb = StringTool.GetStringBuilder();
        sb.Append('[').Append(FrameCounter.CurrentFrame).Append("][").Append(data.MemberName).Append("]").Append(message ?? "NULL").Append(data.FilePath).Append(':').Append(data.LineNumber);
        if (!string.IsNullOrWhiteSpace(data.StackTraceText))
        {
            sb.Append('\n').Append(data.StackTraceText);
        }
        MiniGame.MiniGameCustomConfig._Js_Log(sb.ToString());
    }
}
#endif
