using System;
using System.Numerics;
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

    private Vector4 _prevAmbient = new();
    private Vector4 _prevDiffuse = new();

    protected override void DrawBody()
    {
        var flags =
            ImGuiColorEditFlags.AlphaBar
            | ImGuiColorEditFlags.AlphaPreviewHalf
            | ImGuiColorEditFlags.NoTooltip
            | ImGuiColorEditFlags.NoInputs
            | ImGuiColorEditFlags.NoBorder;

        var startX = ImGui.GetCursorPosX();
        var entireWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            var ambient = _portrait.GetAmbientLightColor().ToVector4();
            if (
                ImPT.ColorConfirm4(
                    "Ambient",
                    ref ambient,
                    ref _prevAmbient,
                    flags,
                    "Ambient light color"
                )
            )
            {
                _portrait.SetAmbientLightColor(new RGBA(ambient));
            }

            var directional = _portrait.GetDirectionalLightColor().ToVector4();
            if (
                ImPT.ColorConfirm4(
                    "Directional",
                    ref directional,
                    ref _prevDiffuse,
                    flags,
                    "Directional light color"
                )
            )
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
