using System;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    private const float RepeatRateSeconds = 0.1f;
    private const float RepeatDelaySeconds = 0.5f;

    private static float _LastMouseDownDuration = 0f;

    private static bool TickHappened()
    {
        var left = ImGuiMouseButton.Left;
        if (!ImGui.IsMouseDown(left))
        {
            _LastMouseDownDuration = 0f;
            return false;
        }

        var downFor = ImGui.GetIO().MouseDownDuration[(int)left];
        var lapped = downFor % RepeatRateSeconds < _LastMouseDownDuration % RepeatRateSeconds;
        _LastMouseDownDuration = downFor;
        return downFor > RepeatDelaySeconds && lapped;
    }

    /// <summary>
    /// A slider with nudge buttons, supporting shift+click for slower nudges
    /// and a moderate key repeat rate (separate form ImGui's default).
    /// </summary>
    public static bool NudgeFloat(string label, ref float value, float min, float max, float step)
    {
        var ticked = TickHappened();
        var shift = ImGui.IsKeyDown(ImGuiKey.ModShift);
        var ctrl = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
        step *=
            shift ? 0.1f
            : ctrl ? 10f
            : 1f;

        var style = ImGui.GetStyle();

        ImGui.PushButtonRepeat(false);
        using var _id = ImRaii.PushId(label);
        using var _group = ImRaii.Group();

        var changed = false;

        // Slider.
        var sliderWidth =
            ImGui.CalcItemWidth()
            - 2 * ImGui.GetFrameHeight()
            - style.ItemInnerSpacing.X
            - 2 * style.FramePadding.X;

        ImGui.SetNextItemWidth(sliderWidth);
        changed |= ImGui.SliderFloat("##progressbar", ref value, 0, max, "%.3f");

        // Minus button.
        ImGui.SameLine(0, style.ItemInnerSpacing.X);
        var minusClicked = ImGuiComponents.IconButton(FontAwesomeIcon.Minus);
        if (minusClicked || (ticked && ImGui.IsItemActive()))
        {
            value = Math.Clamp(value - step, 0, max);
            changed = true;
        }

        // Plus button.
        ImGui.SameLine(0, style.ItemInnerSpacing.X);
        var plusClicked = ImGuiComponents.IconButton(FontAwesomeIcon.Plus);
        if (plusClicked || (ticked && ImGui.IsItemActive()))
        {
            value = Math.Clamp(value + step, 0, max);
            changed = true;
        }

        // Clean this up (there's no ImRaii for button repeat?).
        ImGui.PopButtonRepeat();

        return changed;
    }
}
