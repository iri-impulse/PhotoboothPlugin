using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    /// <summary>
    /// A SliderFloat with an icon next to it, and possibly a tooltip.
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
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        return ImGui.SliderFloat(label, ref value, min, max, format);
    }

    /// <summary>
    /// A SliderInt with an icon next to it, and possibly a tooltip.
    /// </summary>
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
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        return ImGui.SliderInt(label, ref value, min, max, format);
    }

    private static void IconSliderButton(FontAwesomeIcon icon, string? tooltip = null)
    {
        var style = ImGui.GetStyle();
        var color = style.Colors[((int)ImGuiCol.Button)].WithAlpha(0.2f);

        ImGuiComponents.DisabledButton(icon, null, color, color, color);
        if (tooltip is not null && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
