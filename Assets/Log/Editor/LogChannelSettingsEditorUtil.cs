#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LogChannelSettingsEditorUtil
{
    private const string SettingsFolder = "Assets/FrameworkEditor/Editor/Log";
    private const string AssetPath = SettingsFolder + "/LogChannelSettings.asset";
    private static readonly Stopwatch _autoCollectStopwatch = new Stopwatch();

    public static bool IsChannelEnabled(string channel)
    {
        var settings = GetOrCreateSettings();
        if (settings == null) return true;
        if (string.IsNullOrEmpty(channel)) return true;
        var entry = settings.Channels.FirstOrDefault(e => e != null && e.Name == channel);
        if (entry == null)
        {
            // auto-add missing channel as enabled by default
            entry = new LogChannelSettings.ChannelEntry { Name = channel, Enabled = true };
            settings.Channels.Add(entry);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        return entry.Enabled;
    }
 
    [MenuItem("Tools/Log/Open Channel Settings", false, 2000)]
    public static void OpenSettings()
    {
        var settings = GetOrCreateSettings();
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    private static LogChannelSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LogChannelSettings>(AssetPath);
        if (settings == null)
        {
            if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
            settings = ScriptableObject.CreateInstance<LogChannelSettings>();
            // ensure default channel exists
            settings.Channels.Add(new LogChannelSettings.ChannelEntry { Name = "Default", Enabled = true });
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
        }
        return settings;
    }

    // Timing helpers for external auto-collect processes to record duration using Stopwatch
    public static void BeginAutoCollectTiming()
    {
        _autoCollectStopwatch.Restart();
    }

    public static void EndAutoCollectTimingAndRecord()
    {
        _autoCollectStopwatch.Stop();
        var settings = GetOrCreateSettings();
        // Use ceiling to avoid 0ms from truncation when the operation is very fast
        var ms = (long)System.Math.Ceiling(_autoCollectStopwatch.Elapsed.TotalMilliseconds);
        if (ms < 0) ms = 0;
        settings.LastAutoCollectElapsedMs = ms;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}

// Custom inspector for LogChannelSettings asset
[CustomEditor(typeof(LogChannelSettings))]
public class LogChannelSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var settings = (LogChannelSettings)target;

        EditorGUILayout.LabelField("Log Channel Settings", EditorStyles.boldLabel);

        // isToggle switch
        bool newIsToggle = EditorGUILayout.ToggleLeft("isToggle", settings.isToggle);
        if (newIsToggle != settings.isToggle)
        {
            settings.isToggle = newIsToggle;
            EditorUtility.SetDirty(settings);
        }

        // Last auto-collect elapsed time (ms), read-only
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Last Auto Collect (ms)", settings.LastAutoCollectElapsedMs.ToString());
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // Buttons row
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All"))
        {
            foreach (var c in settings.Channels)
            {
                if (c != null) c.Enabled = true;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Disable All"))
        {
            foreach (var c in settings.Channels)
            {
                if (c != null) c.Enabled = false;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Confirm Clear",
                    "是否清除所有非 Default 的频道？",
                    "确定", "取消"))
            {
                settings.Channels.RemoveAll(e => e == null || e.Name != "Default");
                // Ensure at least one Default exists
                if (!settings.Channels.Any(e => e != null && e.Name == "Default"))
                {
                    settings.Channels.Add(new LogChannelSettings.ChannelEntry { Name = "Default", Enabled = true });
                }
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Channels list editor
        if (settings.Channels == null)
        {
            EditorGUILayout.HelpBox("Channels list is null.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < settings.Channels.Count; i++)
        {
            var entry = settings.Channels[i];
            if (entry == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            bool newEnabled = EditorGUILayout.Toggle(entry.Enabled, GUILayout.Width(20));
            // Channel name is read-only in asset inspector
            EditorGUILayout.LabelField(entry.Name);

            if (newEnabled != entry.Enabled)
            {
                if (settings.isToggle && newEnabled)
                {
                    // When single-select mode is on, enabling one disables others
                    foreach (var other in settings.Channels)
                    {
                        if (other != null && !ReferenceEquals(other, entry)) other.Enabled = false;
                    }
                }
                entry.Enabled = newEnabled;
                EditorUtility.SetDirty(settings);
            }

            // Prevent renaming channels in the asset inspector

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
