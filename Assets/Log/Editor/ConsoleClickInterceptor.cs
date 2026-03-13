#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ShortcutManagement;
using UnityEngine;

static class ConsoleClickInterceptor
{
    // 匹配 "(at Assets/.../Foo.cs:123)"
    static readonly Regex s_Regex = new Regex(@"\(at\s+(Assets/.+?\.cs):(\d+)\)", RegexOptions.Compiled);

    // 跳过的路径（可按需修改）
    static readonly List<string> skipList = new() { "Assets/Framework/Log" };

    static bool s_IsOpening;

    // 仍然保留：只对 Editor 本地日志有效（Unity 走“打开资产”路径时会进来）
    [OnOpenAsset(-1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        if (s_IsOpening) return false;

        // 仅当 ConsoleWindow 聚焦时处理
        if (EditorWindow.focusedWindow?.GetType().Name != "ConsoleWindow")
            return false;

        if (!TryGetActiveConsoleText(out var activeText))
            return false;

        var matches = ParseFrames(activeText);
        if (matches.Count == 0) return false;

        // Unity 已经解析到的那一帧
        var initialScript = EditorUtility.InstanceIDToObject(instanceID) as MonoScript;
        var initialPath = initialScript ? AssetDatabase.GetAssetPath(initialScript) : null;

        var startIndex = matches.FindIndex(m => string.Equals(m.path, initialPath, StringComparison.OrdinalIgnoreCase) && m.line == line);
        if (startIndex < 0) return false; // 找不到说明不是我们要处理的场景

        // 如果不是跳过项，让 Unity 默认行为处理
        if (!skipList.Any(skip => matches[startIndex].path.Contains(skip)))
            return false;

        // 否则往下找第一条不在跳过列表里的帧
        var chosen = FindNextNonSkipped(matches, startIndex);
        return Open(chosen.path, chosen.line);
    }
    
    [Shortcut("Console/Open From Active Log", KeyCode.Alpha1, ShortcutModifiers.Alt)]
    public static void OpenFromActiveLog()
    {
        if (EditorWindow.focusedWindow?.GetType().Name != "ConsoleWindow")
        {
            Debug.LogWarning("焦点不在 Console 窗口。请先点一下 Console。");
            return;
        }

        if (!TryGetActiveConsoleText(out var activeText))
        {
            Debug.LogWarning("没有选中的 Console 日志，或无法读取栈文本。");
            return;
        }

        var matches = ParseFrames(activeText);
        if (matches.Count == 0)
        {
            Debug.LogWarning("日志中未找到形如 \"(at Assets/...:行号)\" 的帧。");
            return;
        }

        // 从第一帧开始，跳过你不想进的路径（如封装的日志工具）
        var startIndex = 0;
        var chosen = FindNextNonSkipped(matches, startIndex);

        if (!Open(chosen.path, chosen.line))
            Debug.LogWarning($"无法打开：{chosen.path}:{chosen.line}");
    }

    // --- helpers ---

    static bool TryGetActiveConsoleText(out string activeText)
    {
        activeText = null;
        var editorAsm = typeof(EditorWindow).Assembly;
        var consoleType = editorAsm.GetType("UnityEditor.ConsoleWindow");
        if (consoleType == null) return false;

        var focused = EditorWindow.focusedWindow;
        if (focused == null || focused.GetType() != consoleType) return false;

        var field = consoleType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
        activeText = field?.GetValue(focused) as string;
        return !string.IsNullOrEmpty(activeText);
    }

    static List<(string path, int line)> ParseFrames(string text)
    {
        return s_Regex.Matches(text)
            .Cast<Match>()
            .Select(m => (m.Groups[1].Value, int.Parse(m.Groups[2].Value)))
            .ToList();
    }

    static (string path, int line) FindNextNonSkipped(List<(string path, int line)> frames, int startIndex)
    {
        for (int i = startIndex; i < frames.Count; i++)
        {
            if (!skipList.Any(skip => frames[i].path.Contains(skip)))
                return frames[i];
        }
        return frames[Mathf.Clamp(startIndex, 0, frames.Count - 1)];
    }

    static bool Open(string assetPath, int line)
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
        if (script == null) return false;

        s_IsOpening = true;
        EditorApplication.delayCall += () => s_IsOpening = false;
        AssetDatabase.OpenAsset(script, line);
        return true;
    }
}
#endif
