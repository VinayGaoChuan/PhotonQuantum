#if !UNITY_EDITOR && !BATTLE_SERVER
using System;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;

public class BuildLogBackend : ILogBackend
{
    [DebuggerHidden, DebuggerStepThrough]
    public void Log(LogData data)
    {
        var level = data.Level;
        var message = data.Message;
        var stackTraceText = data.StackTraceText;
        bool isNativeUnityForward = data.Channel == "__UnityNative__";
        // 将任意路径（含绝对路径）规范化为 Unity 可识别的 Assets 相对路径
        string assetsPath = ToAssetsRelative(data.FilePath);

#if OPTIMIZE_GM
        // When OPTIMIZE_GM is enabled, bypass Unity Debug and write to GM panel directly.
        string msgText;
        if (level == LogLevel.Exception && message is Exception ex1)
        {
            // 保留完整异常文本
            msgText = ex1.ToString();
        }
        else
        {
            msgText = (message ?? "NULL").ToString();
        }

        if (!string.IsNullOrWhiteSpace(stackTraceText))
        {
            msgText += "\n" + stackTraceText;
        }

        // 追加 Unity 可点击的文件链接（若能解析到）
        if (!string.IsNullOrEmpty(assetsPath) && data.LineNumber > 0)
        {
            msgText += $"\n(at {assetsPath}:{data.LineNumber})";
        }

#else
        if (level == LogLevel.Exception && message is Exception ex)
        {
            message = ex.ToString();
        }

        var sb = StringTool.GetStringBuilder();

        // 主体消息
        sb.Append(message ?? "NULL").Append("\n(at ").Append(assetsPath).Append(':').Append(data.LineNumber).Append(')').Append("\n[") .Append(FrameCounter.CurrentFrame) .Append("][").Append(data.MemberName ?? "UNKNOWN") .Append(']');
        if (!string.IsNullOrWhiteSpace(stackTraceText))
        {
            sb.Append('\n').Append(stackTraceText);
        }

        string final = sb.ToString();

        if (isNativeUnityForward)
        {
            return;
        }

        switch (level)
        {
            case LogLevel.Warning:
                Debug.LogWarning(final);
                break;
            case LogLevel.Error:
            case LogLevel.Exception:
                Logger._suppressHandle = true;
                try
                {
                    Debug.LogError(final);
                }
                finally
                {
                    Logger._suppressHandle = false;
                }
                break;
            default:
                Debug.Log(final);
                break;
        }
#endif
    }

    // 将任意 filePath 映射为 "Assets/..." 相对路径；若失败则返回 null
    private static string ToAssetsRelative(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // 统一分隔符
        path = path.Replace('\\', '/');

        // 优先匹配 "Assets/" 片段
        int idx = path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            return path.Substring(idx); // 已经是 Assets/ 开头
        }

        // 有些构建产物可能是 "/Assets/..."（前面多一个斜杠）
        idx = path.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            return path.Substring(idx + 1); // 去掉前导 '/'
        }

        // 映射失败（比如第三方包路径或非工程文件）
        return null;
    }
}

#endif
