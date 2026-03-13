using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

/// <summary>
/// Channel settings asset (data container). Only used in Editor.
/// </summary>
#if UNITY_EDITOR
public class LogChannelSettings : ScriptableObject
{
    [Serializable]
    public class ChannelEntry
    {
        public string Name = "Default";
        public bool Enabled = true;
    }

    public List<ChannelEntry> Channels = new List<ChannelEntry>();

    // If true, enabling one channel disables others.
    public bool isToggle = false;

    // Last auto-collect elapsed time in milliseconds.
    public long LastAutoCollectElapsedMs = 0;
}
#endif
