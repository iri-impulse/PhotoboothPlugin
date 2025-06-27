using System;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using static Photobooth.Configuration;

namespace Photobooth.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;

    private static string[] AttachmentNames =>
        ["Freely movable", "Left side", "Right side", "Automatic"];
    private static WindowAttachment?[] Attachments =>
        [null, WindowAttachment.Left, WindowAttachment.Right, WindowAttachment.Auto];

    public ConfigWindow(Plugin plugin)
        : base($"{Plugin.PluginName} Options")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

        AllowPinning = false;
        AllowClickthrough = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 180),
            MaximumSize = new Vector2(360, float.MaxValue),
        };

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

        HintText($"You can open the window manually with {Plugin.CommandName}.");

        var showCoordinates = _configuration.ShowCoordinates;
        if (ImGui.Checkbox("Show camera coordinates", ref showCoordinates))
        {
            _configuration.ShowCoordinates = showCoordinates;
            _configuration.Save();
        }

        HintText("This can be useful for reproducing a portrait later.");

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

        ImGui.Spacing();
        if (ImGui.Button("Done", new(60, 0)))
        {
            IsOpen = false;
        }
    }

    private static void HintText(string text)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey2);
        using var indent = ImRaii.PushIndent();
        ImGui.TextWrapped(text);
    }
}
