using System;
using System.Collections.Generic;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    public static bool CoordinateFloat(
        string label,
        ref float value,
        // Until Dalamud's ImGui binding supports it, we have to pass this through manually.
        bool disabled = false,
        string format = "%.2f"
    )
    {
        var fullWidth = ImGui.CalcItemWidth();
        var usedWidth = 0f;

        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0x00000000, disabled);
        using var id = ImRaii.PushId(label);
        using var group = ImRaii.Group();

        var hasLabel = !label.StartsWith("##");
        if (hasLabel)
        {
            var textStr = label;
            if (textStr.Contains('#'))
            {
                textStr = textStr[..textStr.IndexOf('#', StringComparison.Ordinal)];
            }
            ImGui.AlignTextToFramePadding();
            ImGui.Text(textStr);
            ImGui.SameLine();
            usedWidth += ImGui.GetItemRectSize().X + ImGui.GetStyle().ItemSpacing.X;
        }

        ImGui.SetNextItemWidth(fullWidth - usedWidth);
        return ImGui.InputFloat("##input", ref value, 0, 0, format);
    }
}
