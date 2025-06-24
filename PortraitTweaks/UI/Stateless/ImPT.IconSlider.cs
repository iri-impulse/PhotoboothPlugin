using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace PortraitTweaks.UI.Stateless;

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
        var style = ImGui.GetStyle();
        var iconColor = style.Colors[((int)ImGuiCol.Button)].WithAlpha(0.2f);
        var innerSpacing = style.ItemInnerSpacing;

        using var group = ImRaii.Group();
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, innerSpacing);

        ImGuiComponents.DisabledButton(icon, null, iconColor, iconColor, iconColor);
        if (tooltip is not null && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }

        ImGui.SameLine();
        return ImGui.SliderFloat(label, ref value, min, max, format);
    }
}
