using System;
using Dalamud.Configuration;

namespace PortraitTweaks;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool AutoOpenWhenEditingPortrait { get; set; } = true;

    public WindowAttachment? AttachWindow { get; set; } = null;

    public enum WindowAttachment
    {
        Left = 1,
        Right = 2,
        Auto = 3,
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
