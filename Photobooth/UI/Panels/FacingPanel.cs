using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Photobooth.Controls;
using Photobooth.Maths;
using Photobooth.UI.Canvas;
using Photobooth.UI.Stateless;

namespace Photobooth.UI.Panels;

internal class FacingPanel(PortraitController portrait, Configuration config)
    : Panel(FontAwesomeIcon.Eye, "Head & Gaze")
{
    private readonly PortraitController _portrait = portrait;
    private readonly Configuration _config = config;

    private static readonly Vector2 _EyesMin = new(-20, -12);
    private static readonly Vector2 _EyesMax = new(20, 5);

    private static readonly Vector2 _HeadMin = new(-68, -79);
    private static readonly Vector2 _HeadMax = new(68, 79);

    private struct FacingControlState
    {
        public bool EditingManually;
        public bool ChangedManually;
        public float ManualYaw;
        public float ManualPitch;
    }

    private FacingControlState _headState;
    private FacingControlState _eyesState;

    public override string? Help { get; } =
        "Drag or double click to change head or gaze direction.\nNot available with certain poses.";

    protected override void DrawBody()
    {
        var e = Editor.Current();
        if (!e.IsValid)
        {
            return;
        }

        using var width = ImRaii.ItemWidth(ImGui.GetContentRegionAvail().X * 0.5f - 5);

        var head = _portrait.GetHeadDirection();
        var hType = e.HeadControlType();
        if (PickFacing("Head Direction", ref head, ref _headState, _HeadMax, _HeadMin, hType))
        {
            _portrait.SetHeadDirection(head);
            _headState.EditingManually = false;
        }
        ImGui.SameLine();

        var eye = _portrait.GetEyeDirection();
        var eType = e.GazeControlType();
        if (PickFacing("Eye Direction", ref eye, ref _eyesState, _EyesMax, _EyesMin, eType))
        {
            _portrait.SetEyeDirection(eye);
            _eyesState.EditingManually = false;
        }
    }

    private bool PickFacing(
        string label,
        ref SphereLL dir,
        ref FacingControlState state,
        Vector2 topleft,
        Vector2 bottomright,
        Editor.ControlType control
    )
    {
        using var id = ImRaii.PushId(label);
        using var group = ImRaii.Group();

        var changed = false;
        var message = FaceControlMessage(control);
        using (var canvas = new FacingCanvas(label, topleft, bottomright))
        {
            if (message is string disabledReason)
            {
                canvas.AddTopText(disabledReason);
                canvas.DummyDirection(ref dir);
            }
            else
            {
                changed |= canvas.DragDirection(ref dir);
            }
        }

        using (ImRaii.Disabled(message is not null))
        {
            changed |= ManualControls(ref dir, ref state);
        }

        return changed;
    }

    private bool ManualControls(ref SphereLL dir, ref FacingControlState state)
    {
        var changed = false;
        var apply = false;
        var discard = false;

        // There's no width getter for non-text buttons, and text buttons have extra padding.
        var fullWidth = ImGui.CalcItemWidth();
        var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonsWidth =
            ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Cross, "")
            + ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Check, "")
            - 2 * 3 * ImGuiHelpers.GlobalScale
            + itemSpacing;
        var inputWidth = (fullWidth - buttonsWidth - 3 * itemSpacing) / 2;

        if (!state.EditingManually)
        {
            state.ManualYaw = dir.LonDegrees;
            state.ManualPitch = dir.LatDegrees;
            if (ImGui.Button("Edit", new Vector2(buttonsWidth, 0)))
            {
                state.EditingManually = true;
                state.ChangedManually = false;
            }
        }
        else
        {
            apply = ImGuiComponents.IconButton(FontAwesomeIcon.Check);
            ImGui.SameLine(0, itemSpacing);
            discard = ImGuiComponents.IconButton(FontAwesomeIcon.Times);
        }

        using (ImRaii.Disabled(!state.EditingManually))
        {
            var editing = state.EditingManually;
            using var width = ImRaii.ItemWidth(inputWidth);
            ImGui.SameLine(0, 2 * itemSpacing);
            var changedX = ImPB.CoordinateFloat("X", ref state.ManualYaw, !editing);
            ImGui.SameLine(0, itemSpacing);
            var changedY = ImPB.CoordinateFloat("Y", ref state.ManualPitch, !editing);
            state.ChangedManually |= changedX || changedY;
        }

        if (discard)
        {
            state.EditingManually = false;
            state.ChangedManually = false;
        }
        else if (apply)
        {
            state.EditingManually = false;
            dir.SetDegrees(state.ManualPitch, state.ManualYaw);
            changed = state.ChangedManually;
        }

        return changed;
    }

    private static string? FaceControlMessage(Editor.ControlType ty)
    {
        return ty switch
        {
            Editor.ControlType.Locked => "controlled by pose",
            Editor.ControlType.Free => null,
            Editor.ControlType.Camera => "facing camera",
            _ => "confused, try again",
        };
    }
}
