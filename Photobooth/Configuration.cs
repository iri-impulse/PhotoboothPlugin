using System;
using Dalamud.Configuration;

namespace Photobooth;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool AutoOpenWhenEditingPortrait { get; set; } = true;

    public WindowAttachment? AttachWindow { get; set; } = null;

    public bool ShowCoordinates { get; set; } = false;

    // Not a config-window setting, but we do save it when you change it.
    public bool CompensateFoV { get; set; } = false;

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
