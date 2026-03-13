using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
#if !BATTLE_SERVER
using UnityEngine;
#endif

public static class StringTool
{
    #region StringBuilder

#if !WX_MINIGAME
    [ThreadStatic]
#endif
    // 每个线程都会有自己独立的 StringBuilder 实例
    private static StringBuilder _threadLocalStringBuilder;

    public static StringBuilder GetStringBuilder()
    {
        if (_threadLocalStringBuilder == null)
        {
            _threadLocalStringBuilder = new StringBuilder();
        }
        else
        {
            _threadLocalStringBuilder.Clear();
        }

        return _threadLocalStringBuilder;
    }

    public static string StringBuilder(string e1, string e2)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(string e1, string e2, string e3)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        stringBuilder.Append(e3 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(string e1, string e2, string e3, string e4)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        stringBuilder.Append(e3 ?? "NULL");
        stringBuilder.Append(e4 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(string e1, string e2, string e3, string e4, string e5)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        stringBuilder.Append(e3 ?? "NULL");
        stringBuilder.Append(e4 ?? "NULL");
        stringBuilder.Append(e5 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(string e1, string e2, string e3, string e4, string e5, string e6)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        stringBuilder.Append(e3 ?? "NULL");
        stringBuilder.Append(e4 ?? "NULL");
        stringBuilder.Append(e5 ?? "NULL");
        stringBuilder.Append(e6 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(string e1, string e2, string e3, string e4, string e5, string e6, string e7)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        stringBuilder.Append(e3 ?? "NULL");
        stringBuilder.Append(e4 ?? "NULL");
        stringBuilder.Append(e5 ?? "NULL");
        stringBuilder.Append(e6 ?? "NULL");
        stringBuilder.Append(e7 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(string e1, string e2, string e3, string e4, string e5, string e6, string e7, string e8)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.Append(e1 ?? "NULL");
        stringBuilder.Append(e2 ?? "NULL");
        stringBuilder.Append(e3 ?? "NULL");
        stringBuilder.Append(e4 ?? "NULL");
        stringBuilder.Append(e5 ?? "NULL");
        stringBuilder.Append(e6 ?? "NULL");
        stringBuilder.Append(e7 ?? "NULL");
        stringBuilder.Append(e8 ?? "NULL");
        return stringBuilder.ToString();
    }

    public static string StringBuilder(params string[] e)
    {
        var stringBuilder = GetStringBuilder();
        for (int i = 0; i < e.Length; i++)
        {
            stringBuilder.Append(e[i] ?? "NULL");
        }

        return stringBuilder.ToString();
    }
#if UNITY_EDITOR
    [JetBrains.Annotations.StringFormatMethod("formatString")]
#endif
    public static string StringBuilderFormat(string format, string s1)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.AppendFormat(format ?? "NULL", s1 ?? "NULL");
        return stringBuilder.ToString();
    }
#if UNITY_EDITOR
    [JetBrains.Annotations.StringFormatMethod("formatString")]
#endif
    public static string StringBuilderFormat(string format, string s1, string s2)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.AppendFormat(format ?? "NULL", s1 ?? "NULL", s2 ?? "NULL");
        return stringBuilder.ToString();
    }
#if UNITY_EDITOR
    [JetBrains.Annotations.StringFormatMethod("formatString")]
#endif
    public static string StringBuilderFormat(string format, string s1, string s2, string s3)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.AppendFormat(format ?? "NULL", s1 ?? "NULL", s2 ?? "NULL", s3 ?? "NULL");
        return stringBuilder.ToString();
    }
#if UNITY_EDITOR
    [JetBrains.Annotations.StringFormatMethod("formatString")]
#endif
    public static string StringBuilderFormat(string format, params object[] ss)
    {
        var stringBuilder = GetStringBuilder();
        stringBuilder.AppendFormat(format ?? "NULL", ss);
        return stringBuilder.ToString();
    }

    private static string StringBuilder(string[] ss, string s1)
    {
        var stringBuilder = GetStringBuilder();
        for (int i = 0; i < ss.Length; i++)
        {
            stringBuilder.Append(ss[i] ?? "NULL");
        }

        stringBuilder.Append(s1 ?? "NULL");

        return stringBuilder.ToString();
    }

    #endregion

    public static string StackTrace()
    {
#if !BATTLE_SERVER
        return StackTraceUtility.ExtractStackTrace();
#else
        return Environment.StackTrace;
#endif
    }

    public static string NewLine()
    {
        return Environment.NewLine;
    }
}