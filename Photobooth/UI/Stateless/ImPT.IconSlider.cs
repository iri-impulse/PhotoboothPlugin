using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Photobooth.UI.Stateless;

public static partial class ImPT
{
    /// <summary>
    /// A slider with an icon next to it, and possibly a tooltip on the icon.
    /// </summary>
    public static bool IconSliderFloat(
        string label,
        FontAwesomeIcon icon,
        ref float value,
        float min,
        float max,
        string format = "%.3f",
        string? tooltip = null
    )
    {
        using var group = ImRaii.Group();
        IconSliderButton(icon, tooltip);
        ImGui.SameLine();
        return ImGui.SliderFloat(label, ref value, min, max, format);
    }

    public static bool IconSliderInt(
        string label,
        FontAwesomeIcon icon,
        ref int value,
        int min,
        int max,
        string format = "%d",
        string? tooltip = null
    )
    {
        using var group = ImRaii.Group();
        IconSliderButton(icon, tooltip);
        ImGui.SameLine();
        return ImGui.SliderInt(label, ref value, min, max, format);
    }

    private static void IconSliderButton(FontAwesomeIcon icon, string? tooltip = null)
    {
        var style = ImGui.GetStyle();
        var color = style.Colors[((int)ImGuiCol.Button)].WithAlpha(0.2f);

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, style.ItemInnerSpacing);

        ImGuiComponents.DisabledButton(icon, null, color, color, color);
        if (tooltip is not null && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
