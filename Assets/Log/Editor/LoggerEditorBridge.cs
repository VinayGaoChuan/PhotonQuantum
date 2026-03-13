#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Bridges Editor-only channel settings to the runtime Logger without direct references.
/// </summary>
[InitializeOnLoad]
public static class LoggerEditorBridge
{
    static LoggerEditorBridge()
    {
        // Provide the resolver so Logger can query channel enable state in Editor.
        ZLog.ChannelEnabledResolver = LogChannelSettingsEditorUtil.IsChannelEnabled;
        EditorApplication.update -= ZLog.FlushPendingUnityLogs;
        EditorApplication.update += ZLog.FlushPendingUnityLogs;
    }
}
#endif

