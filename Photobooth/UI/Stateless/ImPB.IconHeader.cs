using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    /// <summary>
    /// A non-collapsible header with an icon, a title, and optional help text.
    /// </summary>
    public static void IconHeader(string text, FontAwesomeIcon icon, string? help = null)
    {
        var style = ImGui.GetStyle();
        var headerColor = ImGui.GetColorU32(ImGuiCol.Header);

        using var _ = ImRaii.PushId(text);
        using var colors = new ImRaii.Color();
        colors.Push(ImGuiCol.Button, headerColor);
        colors.Push(ImGuiCol.ButtonHovered, headerColor);
        colors.Push(ImGuiCol.ButtonActive, headerColor);

        var w = ImGui.GetContentRegionAvail().X + style.WindowPadding.X;
        var h = ImGui.GetTextLineHeightWithSpacing() + 2 * style.FramePadding.Y;

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - style.WindowPadding.X / 2);

        var textSize = ImGui.CalcTextSize(text, true);
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconSize = ImGui.CalcTextSize(icon.ToIconString());
        }

        var textStr = text;
        if (textStr.Contains('#'))
        {
            textStr = textStr[..textStr.IndexOf('#', StringComparison.Ordinal)];
        }

        var framePadding = style.FramePadding;
        var iconPadding = 3 * ImGuiHelpers.GlobalScale;

        var cursor = ImGui.GetCursorScreenPos();

        ImGui.Button("", new Vector2(w, h));

        var iconPos = cursor + new Vector2(2 * framePadding.X, framePadding.Y + iconPadding / 2);
        var textPos = new Vector2(
            iconPos.X + iconSize.X + iconPadding + 2 * framePadding.X,
            cursor.Y + framePadding.Y + iconPadding * 0.75f
        );

        var dl = ImGui.GetWindowDrawList();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            dl.AddText(iconPos, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
        }

        dl.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), textStr);

        // Kind of inelegant to have the help here but at least it keeps the
        // knowledge of the header sizing one place.
        if (!string.IsNullOrEmpty(help))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                using var helpColors = new ImRaii.Color();
                var helpIcon = FontAwesomeIcon.Question.ToIconString();
                var helpSize = ImGui.CalcTextSize(helpIcon);

                var textColor = ImGui.GetColorU32(ImGuiCol.Text);
                var helpColor = ColorHelpers.RgbaUintToVector4(textColor).WithAlpha(0.66f);
                var transparent = 0x00000000u;

                var helpWidth = helpSize.X + 2 * framePadding.X;
                var helpPos = new Vector2(
                    ImGui.GetItemRectMax().X - helpWidth,
                    ImGui.GetItemRectMin().Y
                );

                helpColors.Push(ImGuiCol.Button, transparent);
                helpColors.Push(ImGuiCol.ButtonHovered, transparent);
                helpColors.Push(ImGuiCol.ButtonActive, transparent);
                helpColors.Push(ImGuiCol.Text, helpColor);

                var prevCursor = ImGui.GetCursorScreenPos();
                ImGui.SetCursorScreenPos(helpPos);
                ImGui.Button(helpIcon, new(helpWidth, h));
                ImGui.SetCursorScreenPos(prevCursor);
            }

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
            {
                ImGui.SetTooltip(help);
            }
        }
    }
}
