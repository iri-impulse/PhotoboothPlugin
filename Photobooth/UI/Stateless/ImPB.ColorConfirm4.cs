using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using F = Dalamud.Bindings.ImGui.ImGuiColorEditFlags;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    private const ImGuiColorEditFlags PassthroughButtonFlags =
        F.AlphaPreview | F.AlphaPreviewHalf | F.NoBorder | F.NoTooltip;

    private const ImGuiColorEditFlags PassthroughPickerFlags =
        F.NoInputs | F.AlphaBar | F.PickerHueBar | F.PickerHueWheel | F.NoTooltip;

    public static bool ColorConfirm4(
        string label,
        ref Vector4 color,
        ref Vector4 prevColor,
        ImGuiColorEditFlags flags,
        string? title = null
    )
    {
        using var id = ImRaii.PushId(label);
        var style = ImGui.GetStyle();

        var buttonFlags = flags & PassthroughButtonFlags;
        var pickerFlags = (flags & PassthroughPickerFlags) | F.NoSidePreview;

        var h = ImGui.GetFrameHeight();

        var pressed = false;
        using (ImRaii.Group())
        {
            pressed |= ImGui.ColorButton(label, color, buttonFlags, new(1.5f * h, h));
            ImGui.SameLine(0, style.ItemSpacing.X);
            ImGui.Text(label);
        }
        pressed |= ImGui.IsItemClicked(ImGuiMouseButton.Left);

        if (pressed)
        {
            var left = ImGui.GetItemRectMin().X;
            var bottom = ImGui.GetItemRectMax().Y;
            ImGui.SetNextWindowPos(new(left, bottom));
            ImGui.OpenPopup("##popup");
            prevColor = color;
        }

        if (!ImGui.IsPopupOpen("##popup"))
        {
            return false;
        }

        using var popup = ImRaii.Popup("##popup");

        var popupTitle = title ?? label;
        ImGui.Text(popupTitle);
        ImGui.Spacing();
        var changed = ImGui.ColorPicker4("##picker", ref color, pickerFlags);

        ImGui.SameLine(0, style.ItemSpacing.X);

        using (ImRaii.Group())
        {
            var swatchSize = new Vector2(h * 3, h * 2);
            var buttonSize = new Vector2(h * 3, h);

            ImGui.ColorButton("##current", color, buttonFlags, swatchSize);
            ImGui.Text("Current");
            ImGui.Dummy(new(h, h / 2f));

            var usePrev = ImGui.ColorButton("##previous", prevColor, buttonFlags, swatchSize);
            ImGui.Text("Previous");
            ImGui.Dummy(new(h, h / 2f));

            var bottom = ImGui.GetWindowContentRegionMax().Y;
            ImGui.SetCursorPosY(bottom - 2 * h - style.ItemSpacing.Y);
            var cancel = ImGui.Button("Cancel", buttonSize);
            var okay = ImGui.Button("Okay", buttonSize);

            if (cancel || usePrev)
            {
                color = prevColor;
                changed = true;
            }

            if (okay || cancel)
            {
                ImGui.CloseCurrentPopup();
            }
        }

        return changed;
    }
}
