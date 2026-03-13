#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// 编译期/导入期：自动收集 ZLog 频道并合并到 LogChannelSettings.asset
/// - 增量：监听 .cs 变更，局部重扫，索引持久化到 Library/LogChannels.index.json
/// - 合并：编译完成/防抖 delayCall 时，合并索引频道至资产（新增默认启用）
/// - 菜单：全量重建索引 / 强制更新资产 / 打开设置（打开设置已在 LogChannelSettingsEditorUtil 中提供）
/// 解析规则：支持字符串字面量、@逐字串、nameof(...)、同文件内 const/static readonly string 常量。
/// </summary>
public static class LogChannelAutoIndexer
{
    private const string SettingsFolder = "Assets/Log/Editor";
    private const string SettingsAssetPath = SettingsFolder + "/LogChannelSettings.asset";

    private static readonly Regex s_ValidChannelRegex = new Regex("^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

    private static readonly Dictionary<string, HashSet<string>> s_Index = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private static bool s_MergeScheduled;
    // Track whether a timing session is active so we can span rebuild + merge
    private static bool s_TimingActive;

    [Serializable]
    private class FileChannels
    {
        public string path;
        public List<string> channels;
    }

    [Serializable]
    private class ChannelIndexData
    {
        public List<FileChannels> files = new List<FileChannels>();
    }

    static LogChannelAutoIndexer()
    {
        LoadIndex();
        try { CompilationPipeline.compilationFinished += OnCompilationFinished; } catch {}
        // 防抖：Domain Reload 后也尝试合并一次
        EditorApplication.delayCall += RequestMergeToAsset;
    }

    private static void OnCompilationFinished(object _)
    {
        RequestMergeToAsset();
    }

    // =============== 菜单 ===============
    //[MenuItem("OptimizeTool/Logging/Rebuild Channel Index", false, 2001)]
    public static void RebuildIndexMenu()
    {
        // Start timing for a full rebuild + merge (merge happens via delayCall)
        s_TimingActive = true;
        LogChannelSettingsEditorUtil.BeginAutoCollectTiming();
        RebuildIndex();
    }

    //[MenuItem("OptimizeTool/Logging/Force Update Settings", false, 2002)]
    public static void ForceUpdateSettingsMenu()
    {
        // Measure just the merge when forced via menu
        s_TimingActive = true;
        LogChannelSettingsEditorUtil.BeginAutoCollectTiming();
        DoMerge();
    }

    // =============== 索引 I/O ===============
    private static string GetProjectRoot() => Directory.GetParent(Application.dataPath).FullName;
    private static string GetIndexPathAbsolute() => Path.Combine(GetProjectRoot(), "Library", "LogChannels.index.json");

    private static void LoadIndex()
    {
        s_Index.Clear();
        var path = GetIndexPathAbsolute();
        try
        {
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path, Encoding.UTF8);
            var data = JsonUtility.FromJson<ChannelIndexData>(json);
            if (data?.files == null) return;
            foreach (var f in data.files)
            {
                if (string.IsNullOrEmpty(f?.path) || f.channels == null) continue;
                var set = new HashSet<string>(f.channels.Where(IsValidChannel), StringComparer.Ordinal);
                set.Add("Default");
                s_Index[f.path.Replace('\\', '/')] = set;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LogChannelAutoIndexer] Load index failed: {e.Message}");
        }
    }

    private static void SaveIndex()
    {
        var path = GetIndexPathAbsolute();
        try
        {
            var data = new ChannelIndexData();
            foreach (var kv in s_Index)
            {
                data.files.Add(new FileChannels
                {
                    path = kv.Key,
                    channels = kv.Value.Where(IsValidChannel).Distinct().OrderBy(s => s).ToList()
                });
            }
            var json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LogChannelAutoIndexer] Save index failed: {e.Message}");
        }
    }

    // =============== 合并到资产（防抖） ===============
    private static void RequestMergeToAsset()
    {
        if (s_MergeScheduled) return;
        s_MergeScheduled = true;
        EditorApplication.delayCall += DoMerge;
    }

    private static void DoMerge()
    {
        s_MergeScheduled = false;
        bool localTiming = false;
        // If not started by a menu action, measure merge duration alone
        if (!s_TimingActive)
        {
            s_TimingActive = true;
            localTiming = true;
            LogChannelSettingsEditorUtil.BeginAutoCollectTiming();
        }

        try { MergeToSettingsAsset(); }
        catch (Exception e) { Debug.LogWarning($"[LogChannelAutoIndexer] Merge failed: {e.Message}"); }
        finally
        {
            if (s_TimingActive)
            {
                LogChannelSettingsEditorUtil.EndAutoCollectTimingAndRecord();
                s_TimingActive = false;
            }
        }
    }

    private static void MergeToSettingsAsset()
    {
        var union = new HashSet<string>(StringComparer.Ordinal);
        foreach (var chs in s_Index.Values)
        {
            foreach (var ch in chs)
                if (IsValidChannel(ch)) union.Add(ch);
        }
        union.Add("Default");

        var settings = AssetDatabase.LoadAssetAtPath<LogChannelSettings>(SettingsAssetPath);
        if (settings == null)
        {
            if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
            settings = ScriptableObject.CreateInstance<LogChannelSettings>();
            settings.Channels.Add(new LogChannelSettings.ChannelEntry { Name = "Default", Enabled = true });
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        }

        var existing = new HashSet<string>(settings.Channels.Where(e => e != null && !string.IsNullOrEmpty(e.Name)).Select(e => e.Name), StringComparer.Ordinal);
        var modified = false;
        foreach (var ch in union)
        {
            if (!existing.Contains(ch))
            {
                settings.Channels.Add(new LogChannelSettings.ChannelEntry { Name = ch, Enabled = true });
                modified = true;
            }
        }

        if (!existing.Contains("Default"))
        {
            settings.Channels.Add(new LogChannelSettings.ChannelEntry { Name = "Default", Enabled = true });
            modified = true;
        }

        if (modified)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

    // =============== 资产导入钩子（增量索引） ===============
    private class IndexerAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var changed = false;

            if (importedAssets != null)
            {
                foreach (var p in importedAssets)
                {
                    if (!p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                    changed |= IndexFile(p);
                }
            }

            if (deletedAssets != null)
            {
                foreach (var p in deletedAssets)
                {
                    if (s_Index.Remove(p)) changed = true;
                }
            }

            if (movedFromAssetPaths != null && movedAssets != null)
            {
                for (int i = 0; i < movedFromAssetPaths.Length && i < movedAssets.Length; i++)
                {
                    var oldP = movedFromAssetPaths[i];
                    var newP = movedAssets[i];
                    if (!string.IsNullOrEmpty(oldP) && s_Index.TryGetValue(oldP, out var set))
                    {
                        s_Index.Remove(oldP);
                        s_Index[newP] = set;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                SaveIndex();
                RequestMergeToAsset();
            }
        }
    }

    // =============== 全量重建 ===============
    private static void RebuildIndex()
    {
        s_Index.Clear();
        var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
        var changed = false;
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                changed |= IndexFile(path);
            }
        }
        if (changed) SaveIndex();
        RequestMergeToAsset();
    }

    // =============== 文件解析 ===============
    private static bool IndexFile(string assetPath)
    {
        try
        {
            var abs = Path.Combine(GetProjectRoot(), assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(abs)) return false;
            var text = File.ReadAllText(abs, Encoding.UTF8);
            var consts = ExtractStringConstants(text);
            var channels = ExtractChannelsFromText(text, consts);
            channels.Add("Default");

            if (s_Index.TryGetValue(assetPath, out var oldSet))
            {
                if (SetEquals(oldSet, channels)) return false;
                s_Index[assetPath] = channels;
                return true;
            }
            s_Index[assetPath] = channels;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LogChannelAutoIndexer] Index file failed: {assetPath} -> {e.Message}");
            return false;
        }
    }

    private static bool SetEquals(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var s in a) if (!b.Contains(s)) return false;
        return true;
    }

    private enum ScanState { Normal, LineComment, BlockComment }

    private static Dictionary<string, string> ExtractStringConstants(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        int n = text.Length, i = 0;
        var state = ScanState.Normal;
        while (i < n)
        {
            char c = text[i];
            if (state == ScanState.Normal)
            {
                if (c == '/')
                {
                    if (i + 1 < n)
                    {
                        char n1 = text[i + 1];
                        if (n1 == '/') { state = ScanState.LineComment; i += 2; continue; }
                        if (n1 == '*') { state = ScanState.BlockComment; i += 2; continue; }
                    }
                }
                else if (c == '"') { ParseStringLiteral(text, i, out _, out int e); i = e + 1; continue; }
                else if (c == '@' && i + 1 < n && text[i + 1] == '"') { ParseVerbatimString(text, i, out _, out int e); i = e + 1; continue; }
                else if (c == '\'') { ParseCharLiteral(text, i, out int e); i = e + 1; continue; }

                if (MatchAt(text, i, "const string"))
                {
                    int j = i + "const string".Length;
                    if (IsBoundary(text, i - 1) && IsBoundary(text, j))
                    {
                        SkipWs(text, ref j);
                        string name = ParseIdentifier(text, ref j);
                        if (!string.IsNullOrEmpty(name))
                        {
                            SkipWs(text, ref j);
                            if (j < n && text[j] == '=')
                            {
                                j++; SkipWs(text, ref j);
                                string val = null;
                                if (j < n && text[j] == '"') { ParseStringLiteral(text, j, out val, out int e); j = e + 1; }
                                else if (j + 1 < n && text[j] == '@' && text[j + 1] == '"') { ParseVerbatimString(text, j, out val, out int e); j = e + 1; }
                                else if (MatchAt(text, j, "nameof")) { ParseNameof(text, j, out val, out int e); j = e + 1; }
                                if (val != null && IsValidChannel(val)) map[name] = val;
                            }
                        }
                    }
                }

                if (MatchAt(text, i, "static readonly string"))
                {
                    int j = i + "static readonly string".Length;
                    if (IsBoundary(text, i - 1) && IsBoundary(text, j))
                    {
                        SkipWs(text, ref j);
                        string name = ParseIdentifier(text, ref j);
                        if (!string.IsNullOrEmpty(name))
                        {
                            SkipWs(text, ref j);
                            if (j < n && text[j] == '=')
                            {
                                j++; SkipWs(text, ref j);
                                string val = null;
                                if (j < n && text[j] == '"') { ParseStringLiteral(text, j, out val, out int e); j = e + 1; }
                                else if (j + 1 < n && text[j] == '@' && text[j + 1] == '"') { ParseVerbatimString(text, j, out val, out int e); j = e + 1; }
                                else if (MatchAt(text, j, "nameof")) { ParseNameof(text, j, out val, out int e); j = e + 1; }
                                if (val != null && IsValidChannel(val)) map[name] = val;
                            }
                        }
                    }
                }

                i++;
            }
            else if (state == ScanState.LineComment)
            {
                if (c == '\n' || c == '\r') state = ScanState.Normal;
                i++;
            }
            else // BlockComment
            {
                if (c == '*' && i + 1 < n && text[i + 1] == '/') { state = ScanState.Normal; i += 2; continue; }
                i++;
            }
        }
        return map;
    }

    private static HashSet<string> ExtractChannelsFromText(string text, Dictionary<string, string> consts)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        int n = text.Length, i = 0;
        var state = ScanState.Normal;
        while (i < n)
        {
            char c = text[i];
            if (state == ScanState.Normal)
            {
                if (c == '/')
                {
                    if (i + 1 < n)
                    {
                        char n1 = text[i + 1];
                        if (n1 == '/') { state = ScanState.LineComment; i += 2; continue; }
                        if (n1 == '*') { state = ScanState.BlockComment; i += 2; continue; }
                    }
                }
                else if (c == '"') { ParseStringLiteral(text, i, out _, out int e); i = e + 1; continue; }
                else if (c == '@' && i + 1 < n && text[i + 1] == '"') { ParseVerbatimString(text, i, out _, out int e); i = e + 1; continue; }
                else if (c == '\'') { ParseCharLiteral(text, i, out int e); i = e + 1; continue; }

                if (MatchAt(text, i, "Logger.LogObject(") || MatchAt(text, i, "ZLog.LogObject("))
                {
                    int startArgs = i + (MatchAt(text, i, "Logger.LogObject(") ? "Logger.LogObject(".Length : "ZLog.LogObject(".Length);
                    if (TryParseArgumentList(text, startArgs, out var args, out int endIdx))
                    {
                        string channel = EvaluateChannelArg(args, 2, consts);
                        if (channel != null && IsValidChannel(channel)) result.Add(channel);
                        i = endIdx + 1; // skip past ')'
                        continue;
                    }
                }
                if (MatchAt(text, i, "Logger.Log(") || MatchAt(text, i, "ZLog.Log("))
                {
                    int startArgs = i + (MatchAt(text, i, "Logger.Log(") ? "Logger.Log(".Length : "ZLog.Log(".Length);
                    if (TryParseArgumentList(text, startArgs, out var args, out int endIdx))
                    {
                        string channel = EvaluateChannelArg(args, 1, consts);
                        if (channel != null && IsValidChannel(channel)) result.Add(channel);
                        i = endIdx + 1;
                        continue;
                    }
                }

                i++;
            }
            else if (state == ScanState.LineComment)
            {
                if (c == '\n' || c == '\r') state = ScanState.Normal;
                i++;
            }
            else // BlockComment
            {
                if (c == '*' && i + 1 < n && text[i + 1] == '/') { state = ScanState.Normal; i += 2; continue; }
                i++;
            }
        }
        return result;
    }

    private static bool TryParseArgumentList(string text, int startIndex, out List<(int start, int length, string text)> args, out int endIndex)
    {
        args = new List<(int, int, string)>();
        int i = startIndex, n = text.Length, depth = 1, argStart = i;
        var state = ScanState.Normal;
        while (i < n)
        {
            char c = text[i];
            if (state == ScanState.Normal)
            {
                if (c == '/')
                {
                    if (i + 1 < n)
                    {
                        if (text[i + 1] == '/') { state = ScanState.LineComment; i += 2; continue; }
                        if (text[i + 1] == '*') { state = ScanState.BlockComment; i += 2; continue; }
                    }
                }
                else if (c == '"') { ParseStringLiteral(text, i, out _, out int e); i = e + 1; continue; }
                else if (c == '@' && i + 1 < n && text[i + 1] == '"') { ParseVerbatimString(text, i, out _, out int e); i = e + 1; continue; }
                else if (c == '\'') { ParseCharLiteral(text, i, out int e); i = e + 1; continue; }
                else if (c == '(') { depth++; i++; continue; }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        var seg = text.Substring(argStart, i - argStart);
                        if (!string.IsNullOrWhiteSpace(seg)) args.Add((argStart, seg.Length, seg));
                        endIndex = i;
                        return true;
                    }
                    i++;
                    continue;
                }
                else if (c == ',' && depth == 1)
                {
                    var seg = text.Substring(argStart, i - argStart);
                    if (!string.IsNullOrWhiteSpace(seg)) args.Add((argStart, seg.Length, seg));
                    i++;
                    argStart = i;
                    continue;
                }
                i++;
            }
            else if (state == ScanState.LineComment)
            {
                if (c == '\n' || c == '\r') state = ScanState.Normal;
                i++;
            }
            else // BlockComment
            {
                if (c == '*' && i + 1 < n && text[i + 1] == '/') { state = ScanState.Normal; i += 2; continue; }
                i++;
            }
        }
        endIndex = n - 1;
        return false;
    }

    private static string EvaluateChannelArg(List<(int start, int length, string text)> args, int positionalIndex, Dictionary<string, string> consts)
    {
        // 优先命名参数 channel:
        foreach (var a in args)
        {
            var t = a.text.TrimStart();
            if (t.StartsWith("channel", StringComparison.Ordinal))
            {
                int idx = t.IndexOf(':');
                if (idx < 0) idx = t.IndexOf('=');
                if (idx >= 0)
                {
                    var expr = t.Substring(idx + 1).Trim();
                    var val = EvaluateChannelExpr(expr, consts);
                    if (val != null) return val;
                }
            }
        }

        // 否则用位置参数
        if (args.Count > positionalIndex)
        {
            var t = args[positionalIndex].text.Trim();
            return EvaluateChannelExpr(t, consts);
        }

        return null;
    }

    private static string EvaluateChannelExpr(string expr, Dictionary<string, string> consts)
    {
        if (string.IsNullOrWhiteSpace(expr)) return null;
        expr = TrimOuterParens(expr.Trim());

        if (expr.StartsWith("nameof", StringComparison.Ordinal))
        {
            ParseNameof(expr, 0, out var v, out _);
            return v;
        }
        if (expr.StartsWith("@\"", StringComparison.Ordinal))
        {
            ParseVerbatimString(expr, 0, out var v, out _);
            return v;
        }
        if (expr.StartsWith("\"", StringComparison.Ordinal))
        {
            ParseStringLiteral(expr, 0, out var v, out _);
            return v;
        }

        // 标识符（可带限定名）：取最后一段，并在 consts 里解析
        var id = expr;
        var paren = id.IndexOf('(');
        if (paren >= 0) id = id.Substring(0, paren);
        id = id.Trim();
        if (id.Contains('.')) id = id.Split('.').Last().Trim();
        if (consts != null && consts.TryGetValue(id, out var cval)) return cval;
        return null;
    }

    private static string TrimOuterParens(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '(' && s[^1] == ')')
        {
            int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '(') depth++;
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0 && i != s.Length - 1) return s; // 外部还有内容，不是包裹
                }
                else if (c == '"' || c == '\'') break;
            }
            if (depth == 0) return s.Substring(1, s.Length - 2).Trim();
        }
        return s;
    }

    private static bool MatchAt(string text, int index, string token)
    {
        if (index < 0 || index + token.Length > text.Length) return false;
        return string.CompareOrdinal(text, index, token, 0, token.Length) == 0;
    }

    private static bool IsBoundary(string text, int index)
    {
        if (index < 0 || index >= text.Length) return true;
        char ch = text[index];
        return !char.IsLetterOrDigit(ch) && ch != '_';
    }

    private static void SkipWs(string text, ref int i)
    {
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
    }

    private static string ParseIdentifier(string text, ref int i)
    {
        int n = text.Length;
        if (i >= n) return null;
        char c = text[i];
        if (!(char.IsLetter(c) || c == '_')) return null;
        int start = i++;
        while (i < n)
        {
            char ch = text[i];
            if (!(char.IsLetterOrDigit(ch) || ch == '_')) break;
            i++;
        }
        return text.Substring(start, i - start);
    }

    private static void ParseStringLiteral(string text, int startQuote, out string value, out int endIndex)
    {
        value = null;
        int i = startQuote + 1;
        var sb = new StringBuilder();
        while (i < text.Length)
        {
            char c = text[i++];
            if (c == '\\' && i < text.Length)
            {
                char esc = text[i++];
                switch (esc)
                {
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case '0': sb.Append('\0'); break;
                    case 'a': sb.Append('\a'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'v': sb.Append('\v'); break;
                    case 'u':
                    case 'x':
                    case 'U':
                        int max = esc == 'U' ? 8 : 4;
                        int hStart = i, hLen = 0;
                        while (i < text.Length && hLen < max && IsHex(text[i])) { i++; hLen++; }
                        if (hLen > 0)
                        {
                            var hex = text.Substring(hStart, hLen);
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                sb.Append((char)code);
                        }
                        break;
                    default:
                        sb.Append(esc);
                        break;
                }
            }
            else if (c == '"')
            {
                endIndex = i - 1;
                value = sb.ToString();
                return;
            }
            else
            {
                sb.Append(c);
            }
        }
        endIndex = text.Length - 1;
    }

    private static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static void ParseVerbatimString(string text, int startAt, out string value, out int endIndex)
    {
        value = null; // starts with @"
        int i = startAt + 2;
        var sb = new StringBuilder();
        while (i < text.Length)
        {
            char c = text[i++];
            if (c == '"')
            {
                if (i < text.Length && text[i] == '"') { sb.Append('"'); i++; continue; }
                endIndex = i - 1;
                value = sb.ToString();
                return;
            }
            sb.Append(c);
        }
        endIndex = text.Length - 1;
    }

    private static void ParseCharLiteral(string text, int startAt, out int endIndex)
    {
        int i = startAt + 1;
        while (i < text.Length)
        {
            char c = text[i++];
            if (c == '\\') { if (i < text.Length) i++; }
            else if (c == '\'') { endIndex = i - 1; return; }
        }
        endIndex = text.Length - 1;
    }

    private static void ParseNameof(string text, int startAt, out string value, out int endIndex)
    {
        value = null;
        int i = startAt + "nameof".Length;
        SkipWs(text, ref i);
        if (i >= text.Length || text[i] != '(') { endIndex = startAt; return; }
        i++; int depth = 1; int start = i;
        while (i < text.Length && depth > 0)
        {
            char c = text[i++];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '"') { ParseStringLiteral(text, i - 1, out _, out int e); i = e + 1; }
            else if (c == '\'') { ParseCharLiteral(text, i - 1, out int e); i = e + 1; }
        }
        endIndex = i - 1;
        var inside = text.Substring(start, endIndex - start).Trim();
        if (!string.IsNullOrEmpty(inside))
        {
            var parts = inside.Split('.');
            value = parts[parts.Length - 1].Trim();
        }
    }

    private static bool IsValidChannel(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        return s_ValidChannelRegex.IsMatch(s);
    }
}
#endif
