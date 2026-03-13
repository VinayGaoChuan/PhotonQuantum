#if !BATTLE_SERVER
using System;
using System.Diagnostics;
using UnityEngine;

public class FrameCounter : MonoBehaviour
{
    private static Stopwatch _stopwatch = Stopwatch.StartNew();

    // 主线程更新，其他线程安全读取
    public static int CurrentFrame { get; private set; }
    public static long TimeNow { get; private set; }

    void Update()
    {
        CurrentFrame = Time.frameCount;
        TimeNow = (long)(Time.time * 1000);
    }
}
#else
public class FrameCounter
{
    // 主线程更新，其他线程安全读取
    public static int CurrentFrame { get; private set; }
    public static long TimeNow { get; private set; }
}
#endif
