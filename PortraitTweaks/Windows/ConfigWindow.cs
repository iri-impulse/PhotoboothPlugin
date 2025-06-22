using System;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using static PortraitTweaks.Configuration;

namespace PortraitTweaks.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;

    public ConfigWindow(Plugin plugin)
        : base($"{Plugin.PluginName} Options")
    {
        Flags =
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse;

        AllowPinning = false;
        AllowClickthrough = false;
        Size = new Vector2(320, 180);
        SizeCondition = ImGuiCond.Always;

        _configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var autoOpen = _configuration.AutoOpenWhenEditingPortrait;
        if (ImGui.Checkbox("Open window automatically", ref autoOpen))
        {
            _configuration.AutoOpenWhenEditingPortrait = autoOpen;
            _configuration.Save();
        }

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey2))
        {
            ImGui.TextWrapped(
                $"Type {Plugin.CommandName} while editing a portrait to manually toggle the {Plugin.PluginName} window."
            );
        }

        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Attach to portrait window?");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(-float.Epsilon);
        var attachmentIx = Array.IndexOf(Attachments, _configuration.AttachWindow);
        if (ImGui.Combo("##attachment", ref attachmentIx, AttachmentNames, AttachmentNames.Length))
        {
            _configuration.AttachWindow = Attachments[attachmentIx];
            _configuration.Save();
        }

        var style = ImGui.GetStyle();
        var size = ImGui.CalcTextSize("Done") + 2 * style.FramePadding;
        ImGui.SetCursorPos(ImGui.GetWindowContentRegionMax() - size - style.WindowPadding / 2);
        if (ImGui.Button("Done"))
        {
            IsOpen = false;
        }
    }

    private static string[] AttachmentNames =>
        ["Freely movable", "Left side", "Right side", "Automatic"];

    private static WindowAttachment?[] Attachments =>
        [null, WindowAttachment.Left, WindowAttachment.Right, WindowAttachment.Auto];
}
