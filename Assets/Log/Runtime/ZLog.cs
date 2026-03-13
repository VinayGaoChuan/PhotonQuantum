using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Debug = System.Diagnostics.Debug;

#if !BATTLE_SERVER
using UnityEngine;
using Object = UnityEngine.Object;
#endif

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Exception
}

public struct LogData
{
    public object Message;
    public string MemberName;
    public string FilePath;
    public int LineNumber;
    public LogLevel Level;
    public string Channel; // only used by Log/LogObject; null otherwise
    public string StackTraceText;
    public bool logEnabled;
    public Object Context; // optional context for Editor logs
}

public interface ILogBackend
{
    void Log(LogData data);
}

public static class ZLog
{
    private static readonly List<ILogBackend> _backends = new List<ILogBackend>();
    private const string NativeUnityChannel = "__UnityNative__";

    [ThreadStatic] public static bool _suppressHandle;

    public static bool LogEnabled { get; set; }

#if !BATTLE_SERVER
    private struct PendingUnityLog
    {
        public string Condition;
        public string StackTrace;
        public LogType Type;
    }

    private static readonly ConcurrentQueue<PendingUnityLog> _pendingUnityLogs = new ConcurrentQueue<PendingUnityLog>();
    private static int _mainThreadId;
#endif

    static ZLog()
    {
#if !BATTLE_SERVER
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        ConfigureUnityStackTraceSettings();
#endif
#if UNITY_EDITOR
        _backends.Add(new EditorLogBackend());
        _backends.Add(new RunSessionFileLogBackend());
        Application.logMessageReceivedThreaded -= HandleLogMessageThreaded;
        Application.logMessageReceivedThreaded += HandleLogMessageThreaded;
#else
#if BATTLE_SERVER
#if DEBUG
        _backends.Add(new NLogBackend());
#else
        _backends.Add(new BattleServerLogBackend());
#endif
#else
#if FIREBASE_LOG
        _backends.Add(new FirebaseLogBackend());
        //具体初始化交给业务层做
        InitFireBaseSDK();
#else
        _backends.Add(new BuildLogBackend());
#endif
        Application.logMessageReceivedThreaded -= HandleLogMessageThreaded;
        Application.logMessageReceivedThreaded += HandleLogMessageThreaded;
#endif
#endif
#if DUMP
        _backends.Add(new DumpLogBackend());
#endif
//#if !UNITY_EDITOR
//        // 捕获 UniTask 未观察到的异常，统一写入日志
//        UniTaskScheduler.UnobservedTaskException -= OnUniTaskUnobservedException;
//        UniTaskScheduler.UnobservedTaskException += OnUniTaskUnobservedException;
//#endif
    }

    public static void EnsureInitialized()
    {
    }

#if !UNITY_EDITOR && !BATTLE_SERVER
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BootstrapRuntimeLogging()
    {
        EnsureInitialized();
    }
#endif

#if FIREBASE_LOG
    public static bool IsFireBaseInitSuccess { get; private set; }
    private static void InitFireBaseSDK()
    {
#if CHANNEL_GLOBAL
        IsFireBaseInitSuccess = false;
        Logger.Log("[SDK][Firebase]Try begin initialize Firebase.");
        Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Warning;
        // Initialize Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
#if DEBUG
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                // Crashlytics will use the DefaultInstance, as well;
                // this ensures that Crashlytics is initialized.
                Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                //if (mConfig.EnableDebug)
                //{
                var s = StringTool.GetStringBuilder();
                s.Append(" ApiKey = ");
                s.Append(app.Options.ApiKey);
                s.Append(" AppId = ");
                s.Append(app.Options.AppId);
                s.Append(" DatabaseUrl = ");
                s.Append(app.Options.DatabaseUrl);
                s.Append(" MessageSenderId = ");
                s.Append(app.Options.MessageSenderId);
                s.Append(" StorageBucket = ");
                s.Append(app.Options.StorageBucket);
                s.Append(" ProjectId = ");
                s.Append(app.Options.ProjectId);
                Logger.Log($"[SDK][Firebase]Firebase Init Success! AppName = {app.Name} Options =[{s.ToString()}]");
                //}
#endif
                // Set a flag here for indicating that your project is ready to use Firebase.
                IsFireBaseInitSuccess = true;
            }
            else
            {
                Logger.LogError($"[SDK][Firebase]Firebase Init Fail Could not resolve all Firebase dependencies: {dependencyStatus}");
                // Firebase Unity SDK is not safe to use here.
            }
        });
#endif
    }
#endif

#if !BATTLE_SERVER
    private static void HandleLogMessageThreaded(string condition, string stackTrace, LogType type)
    {
        if (_suppressHandle) return;

        if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception)
        {
            return;
        }

        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            ForwardUnityLog(condition, stackTrace, type);
            return;
        }

        _pendingUnityLogs.Enqueue(new PendingUnityLog
        {
            Condition = condition,
            StackTrace = stackTrace,
            Type = type
        });
    }

    public static void FlushPendingUnityLogs()
    {
        while (_pendingUnityLogs.TryDequeue(out var pending))
        {
            ForwardUnityLog(pending.Condition, pending.StackTrace, pending.Type);
        }
    }

    private static void ForwardUnityLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Assert)
        {
            InternalLog(condition, LogLevel.Error, "UnityLog", null, 0, NativeUnityChannel, null, stackTrace);
        }
        else if (type == LogType.Exception)
        {
            InternalLog(condition, LogLevel.Exception, "UnityLog", null, 0, NativeUnityChannel, null, stackTrace);
        }
    }
#endif

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogErrorObject(string message, Object context, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => InternalLog(message, LogLevel.Error, memberName, filePath, lineNumber, null, context);

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogErrorObjectChannel(string message, Object context, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsChannelEnabled(channel)) return;
        InternalLog(message, LogLevel.Error, memberName, filePath, lineNumber, channel, context);
    }

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogWarningObject(object message, Object context, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => InternalLog(message, LogLevel.Warning, memberName, filePath, lineNumber, null, context);

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogWarningObjectChannel(object message, Object context, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsChannelEnabled(channel)) return;
        InternalLog(message, LogLevel.Warning, memberName, filePath, lineNumber, channel, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("ENABLE_LOG"), DebuggerHidden, DebuggerStepThrough]
    public static void LogObject(string message, Object context, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsChannelEnabled(channel)) return;
        InternalLog(message, LogLevel.Info, memberName, filePath, lineNumber, channel, context);
    }

    [Conditional("UNITY_EDITOR"), DebuggerHidden, DebuggerStepThrough]
    public static void Log(string message, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsChannelEnabled(channel)) return;
        InternalLog(message, LogLevel.Info, memberName, filePath, lineNumber, channel, null);
    }

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogMobile(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => InternalLog(message, LogLevel.Info, memberName, filePath, lineNumber, null, null);

    // ======== Log Warning ========

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogWarning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => InternalLog(message, LogLevel.Warning, memberName, filePath, lineNumber, null, null);

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogWarningChannel(string message, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsChannelEnabled(channel)) return;
        InternalLog(message, LogLevel.Warning, memberName, filePath, lineNumber, channel, null);
    }

    // ======== Log Error ========
    [DebuggerHidden, DebuggerStepThrough]
    public static void LogError(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => InternalLog(message, LogLevel.Error, memberName, filePath, lineNumber, null, null);

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogErrorChannel(string message, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (!IsChannelEnabled(channel)) return;
        InternalLog(message, LogLevel.Error, memberName, filePath, lineNumber, channel, null);
    }

    // ======== Log Exception ========
    [DebuggerHidden, DebuggerStepThrough]
    public static void LogException(Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (exception == null) return;
        _suppressHandle = true;
        try
        {
            InternalLog(exception, LogLevel.Exception, memberName, filePath, lineNumber, null, null);
        }
        finally
        {
            _suppressHandle = false;
        }
    }

    [DebuggerHidden, DebuggerStepThrough]
    public static void LogExceptionChannel(Exception exception, string channel = "Default", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        if (exception == null) return;
        if (!IsChannelEnabled(channel)) return;
        _suppressHandle = true;
        try
        {
            InternalLog(exception, LogLevel.Exception, memberName, filePath, lineNumber, channel, null);
        }
        finally
        {
            _suppressHandle = false;
        }
    }

    private static void InternalLog(object message, LogLevel level, string memberName, string filePath, int lineNumber, string channel, Object context)
        => InternalLog(message, level, memberName, filePath, lineNumber, channel, context, null);

    private static void InternalLog(object message, LogLevel level, string memberName, string filePath, int lineNumber, string channel, Object context, string explicitStackTraceText)
    {
        // 分发
        var data = new LogData
        {
            Message = message,
            MemberName = memberName,
            FilePath = filePath,
            LineNumber = lineNumber,
            Level = level,
            Channel = channel,
            StackTraceText = CaptureLogStackTrace(level, message, explicitStackTraceText),
            logEnabled = LogEnabled,
            Context = context
        };
        foreach (var bk in _backends)
            bk.Log(data);
    }

    private static string CaptureLogStackTrace(LogLevel level, object message, string explicitStackTraceText)
    {
        if (!string.IsNullOrWhiteSpace(explicitStackTraceText))
        {
            return explicitStackTraceText;
        }

        if (level == LogLevel.Exception && message is Exception ex && !string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            return ex.StackTrace;
        }

        if (level != LogLevel.Error && level != LogLevel.Exception)
        {
            return null;
        }

        if (message == null)
        {
            return null;
        }

        var trace = StringTool.StackTrace();
        if (string.IsNullOrWhiteSpace(trace))
        {
            return null;
        }

        var lines = trace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var builder = StringTool.GetStringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (ShouldSkipStackTraceLine(line))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static bool ShouldSkipStackTraceLine(string line)
    {
        return line.Contains("UnityEngine.StackTraceUtility:ExtractStackTrace", StringComparison.Ordinal)
               || line.Contains("StringTool:StackTrace", StringComparison.Ordinal)
               || line.Contains("ZLog:CaptureLogStackTrace", StringComparison.Ordinal)
               || line.Contains("ZLog:InternalLog", StringComparison.Ordinal)
               || line.Contains("ZLog:LogError", StringComparison.Ordinal)
               || line.Contains("ZLog:LogErrorObject", StringComparison.Ordinal)
               || line.Contains("ZLog:LogWarning", StringComparison.Ordinal)
               || line.Contains("ZLog:LogWarningObject", StringComparison.Ordinal)
               || line.Contains("ZLog:LogException", StringComparison.Ordinal)
               || line.Contains("Logger:CaptureLogStackTrace", StringComparison.Ordinal)
               || line.Contains("Logger:InternalLog", StringComparison.Ordinal)
               || line.Contains("Logger:LogError", StringComparison.Ordinal)
               || line.Contains("Logger:LogErrorObject", StringComparison.Ordinal)
               || line.Contains("Logger:LogWarning", StringComparison.Ordinal)
               || line.Contains("Logger:LogException", StringComparison.Ordinal);
    }

#if !BATTLE_SERVER
    private static void ConfigureUnityStackTraceSettings()
    {
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
    }
#endif

#if UNITY_EDITOR
    // Editor bridge: set from an Editor-only script to resolve channel enable state
    public static Func<string, bool> ChannelEnabledResolver { get; set; }
#endif
    private static bool IsChannelEnabled(string channel)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(channel)) return true; // treat null/empty as enabled
        var resolver = ChannelEnabledResolver;
        return resolver != null ? resolver(channel) : true;
#else
        return true;
#endif
    }
//#if !UNITY_EDITOR
//    // 统一处理 UniTask 未观察到的异常
//    private static void OnUniTaskUnobservedException(Exception ex)
//    {
//        LogException(ex);
//    }
//#endif
}
