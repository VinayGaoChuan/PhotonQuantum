#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object; // 为 StringTool

public class EditorLogBackend : ILogBackend
{
    public bool LogEnabled
    {
        get { return Debug.unityLogger.filterLogType != LogType.Error; }
        set
        {
#if BATTLE_SERVER
            isLog = value;
#else
            if (value)
            {
                Debug.unityLogger.filterLogType = LogType.Log;
            }
            else
            {
                Debug.unityLogger.filterLogType = LogType.Error;
            }
#endif
        }
    }

    [DebuggerHidden, DebuggerStepThrough]
    public void Log(LogData data)
    {
        var level = data.Level;
        var message = data.Message;
        var fileName = Path.GetFileName(data.FilePath);
        var sb = StringTool.GetStringBuilder();
        string title = sb.Append("[").Append(data.Channel ?? "Default").Append("] ").ToColor(ColorEnum.SteelBlue);
        sb = StringTool.GetStringBuilder();
        object messageText = message;
        if (level == LogLevel.Exception && message is Exception ex)
        {
            messageText = ex.ToString();
        }

        sb.Append(title).Append(messageText ?? "NULL").Append("\n[").Append(FrameCounter.CurrentFrame).Append(']').Append(fileName).Append(':').Append(data.LineNumber).Append("(").Append(data.MemberName).Append(")");
        if (!string.IsNullOrWhiteSpace(data.StackTraceText))
        {
            sb.Append("\n").Append(data.StackTraceText);
        }
        string final = sb.ToString();
        
        bool hasContext = data.Context != null;
        ZLog._suppressHandle = true;
        try
        {
            switch (level)
            {
                case LogLevel.Warning:
                    if (hasContext) Debug.LogWarning(final, data.Context);
                    else Debug.LogWarning(final);
                    break;
                case LogLevel.Error:
                case LogLevel.Exception:
                    if (hasContext) Debug.LogError(final, data.Context);
                    else Debug.LogError(final);
                    break;
                default:
                    if (hasContext) Debug.Log(final, data.Context);
                    else Debug.Log(final);
                    break;
            }
        }
        finally
        {
            ZLog._suppressHandle = false;
        }
    }
}
#endif
