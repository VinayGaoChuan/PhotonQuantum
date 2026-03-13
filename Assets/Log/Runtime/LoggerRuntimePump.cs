#if !UNITY_EDITOR && !BATTLE_SERVER
using UnityEngine;

public sealed class LoggerRuntimePump : MonoBehaviour
{
    private static bool _created;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (_created)
        {
            return;
        }

        ZLog.EnsureInitialized();

        var go = new GameObject(nameof(LoggerRuntimePump));
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        go.AddComponent<LoggerRuntimePump>();
        _created = true;
    }

    private void Update()
    {
        ZLog.FlushPendingUnityLogs();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ZLog.FlushPendingUnityLogs();
        }
    }

    private void OnApplicationQuit()
    {
        ZLog.FlushPendingUnityLogs();
    }
}
#endif
