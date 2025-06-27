using System;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Photobooth.UI.Stateless;

public static partial class ImPT
{
    private static long _NudgeButtonStart = 0;

    /// <summary>
    /// A slider with nudge buttons, supporting shift+click for slower nudges
    /// and a moderate key repeat rate.
    /// </summary>
    public static bool NudgeFloat(string label, ref float value, float min, float max, float step)
    {
        step = ImGui.IsKeyDown(ImGuiKey.ModShift) ? step / 10 : step;

        var repeatMs = 100;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sinceStart = now - _NudgeButtonStart;

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

        if (ImGui.IsItemActivated())
        {
            _NudgeButtonStart = now;
        }
        else if (ImGui.IsItemActive() && (now - _NudgeButtonStart) > repeatMs)
        {
            _NudgeButtonStart = now + sinceStart % repeatMs;
            minusClicked = true;
        }

        if (minusClicked)
        {
            value = Math.Clamp(value - step, 0, max);
            changed = true;
        }

        // Plus button.
        ImGui.SameLine(0, style.ItemInnerSpacing.X);
        var plusClicked = ImGuiComponents.IconButton(FontAwesomeIcon.Plus);

        if (ImGui.IsItemActivated())
        {
            _NudgeButtonStart = now;
        }
        else if (ImGui.IsItemActive() && sinceStart > repeatMs)
        {
            _NudgeButtonStart = now + sinceStart % repeatMs;
            plusClicked = true;
        }

        if (plusClicked)
        {
            value = Math.Clamp(value + step, 0, max);
            changed = true;
        }

        // Clean this up (there's no ImRaii for button repeat?).
        ImGui.PopButtonRepeat();

        return changed;
    }
}
