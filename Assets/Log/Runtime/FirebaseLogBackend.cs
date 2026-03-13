#if !UNITY_EDITOR && FIREBASE_LOG
using System;
using System.Diagnostics;
using System.IO;
using Firebase.Crashlytics;

public class FirebaseLogBackend : ILogBackend
{
    private sealed class LoggedStackTraceException : Exception
    {
        private readonly string _stackTrace;

        public LoggedStackTraceException(string message, string stackTrace) : base(message)
        {
            _stackTrace = stackTrace;
        }

        public override string StackTrace => string.IsNullOrWhiteSpace(_stackTrace) ? base.StackTrace : _stackTrace;
    }

    [DebuggerHidden, DebuggerStepThrough]
    public void Log(LogData data)
    {
        var level = data.Level;
        var message = data.Message;
        string assetsPath = string.IsNullOrEmpty(data.FilePath) ? "UnknownFile" : Path.GetFileNameWithoutExtension(data.FilePath);
        var sb = StringTool.GetStringBuilder();
        object messageText = message;
        if (level == LogLevel.Exception && message is Exception ex)
        {
            messageText = ex.ToString();
        }

        sb.Append(messageText ?? "NULL").Append("(at ").Append(assetsPath).Append(':').Append(data.LineNumber).Append(')').Append("[") .Append(FrameCounter.CurrentFrame) .Append("][").Append(data.MemberName ?? "UNKNOWN") .Append(']');
        if (!string.IsNullOrWhiteSpace(data.StackTraceText))
        {
            sb.Append('\n').Append(data.StackTraceText);
        }
        string full = sb.ToString();
        if (Logger.IsFireBaseInitSuccess)
        {
            if (level == LogLevel.Info || level == LogLevel.Warning)
            {
                // 普通日志和警告可用 Crashlytics.Log
                Crashlytics.Log(full);
            }
            else
            {   
                Crashlytics.Log(full);
                if (message is Exception ex)
                {
                    Crashlytics.LogException(ex);
                }
                else
                {
                    Crashlytics.LogException(new LoggedStackTraceException(full, data.StackTraceText));
                }
            }
        }
        
        Console.WriteLine(full);
    }
}

#endif
