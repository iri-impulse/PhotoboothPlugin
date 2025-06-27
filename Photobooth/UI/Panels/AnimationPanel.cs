using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using Photobooth.Controls;
using Photobooth.UI.Stateless;

namespace Photobooth.UI.Panels;

internal class AnimationPanel(PortraitController portrait)
    : Panel(FontAwesomeIcon.Running, "Animation")
{
    private readonly PortraitController _portrait = portrait;

    // For debouncing, since durations take a moment to stabilize.
    private float _lastDuration = 1f;
    private int _lastPose = -1;

    private static readonly float _NudgeAmount = 0.1f;
    public override string Help { get; } =
        "Ctrl+Click the slider to type an exact timestamp, or\n"
        + "hold Shift for a smaller increment with the +/- buttons.";

    public override void Reset()
    {
        _lastDuration = 1f;
        _lastPose = -1;
    }

    protected override void DrawBody()
    {
        var e = Editor.Current();
        if (!e.IsValid)
        {
            return;
        }

        var paused = e.IsAnimationPaused();
        var icon = paused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
        if (ImGuiComponents.IconButton(icon, new(ImGui.GetContentRegionAvail().X * 0.15f, 0)))
        {
            e.ToggleAnimationPlayback(!paused);
        }

        var time = _portrait.GetAnimationProgress();
        var duration = DebounceDuration(e);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-float.Epsilon);
        var timeChanged = ImPB.NudgeFloat("##animation", ref time, 0, duration, _NudgeAmount);

        // In theory there are a few animations (Mesotes mostly) that wouldn't
        // want to pin for held-down nudge buttons, but on net it's worth it.
        _portrait.SetPinned(ImGui.IsItemActive());

        if (timeChanged)
        {
            _portrait.SetAnimationProgress(time);
        }
    }

    /// <summary>
    /// Setting the pose or progress causes reloading, which temporarily keeps
    /// us from directly observing the animation duration. Keep the old one, so
    /// we don't change the slider maximum while dragging.
    /// </summary>
    private unsafe float DebounceDuration(Editor e)
    {
        var pose = e.State->BannerEntry.BannerTimeline;

        if (pose != _lastPose)
        {
            _lastPose = pose;
            _lastDuration = 1f;
        }

        var dur = e.GetAnimationDuration();
        if (dur > 0)
        {
            _lastDuration = dur;
        }

        return _lastDuration;
    }
}
