using System;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Photobooth.Controls;
using Photobooth.Maths;
using Photobooth.UI.Stateless;

namespace Photobooth.UI.Panels;

internal class LightingPanel(PortraitController portrait)
    : Panel(FontAwesomeIcon.Lightbulb, "Lighting")
{
    private readonly PortraitController _portrait = portrait;

    protected override void DrawBody()
    {
        var colorFlags =
            ImGuiColorEditFlags.AlphaBar
            | ImGuiColorEditFlags.NoTooltip
            | ImGuiColorEditFlags.NoOptions
            | ImGuiColorEditFlags.NoInputs;

        var startX = ImGui.GetCursorPosX();
        var entireWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            var ambientVec4 = _portrait.GetAmbientLightColor().ToVector4();
            if (ImGui.ColorEdit4("Ambient##_picker", ref ambientVec4, colorFlags))
            {
                _portrait.SetAmbientLightColor(new RGBA(ambientVec4));
            }

            var directional = _portrait.GetDirectionalLightColor().ToVector4();
            if (ImGui.ColorEdit4("Directional##_picker", ref directional, colorFlags))
            {
                _portrait.SetDirectionalLightColor(new RGBA(directional));
            }
        }

        ImGui.SameLine();
        var leftWidth = Math.Max(ImGui.GetCursorPosX(), startX + 0.3f * entireWidth);
        ImGui.SameLine(leftWidth);
        using (ImRaii.Group())
        {
            using var width = ImRaii.ItemWidth(-float.Epsilon);

            var lightDir = _portrait.GetDirectionalLightDirection();
            if (ImPT.Direction("##lightDirection", ref lightDir, -180, 180, -90, 90))
            {
                _portrait.SetDirectionalLightDirection(lightDir);
            }
        }
    }
}
