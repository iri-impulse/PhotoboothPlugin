using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PortraitTweaks.Controls;
using PortraitTweaks.UI.Canvas;
using PortraitTweaks.UI.Stateless;

namespace PortraitTweaks.UI.Panels;

internal class FacingPanel(PortraitController portrait) : Panel(FontAwesomeIcon.Eye, "Head & Gaze")
{
    private readonly PortraitController _portrait = portrait;

    private static readonly Vector2 _EyeMin = new(-20, -12);
    private static readonly Vector2 _EyeMax = new(20, 5);

    private static readonly Vector2 _HeadMin = new(-68, -79);
    private static readonly Vector2 _HeadMax = new(68, 79);

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
        var noHead = FaceControlMessage(e.HeadControlType());
        if (FacingCanvas.PickFacing("Head Direction", ref head, _HeadMax, _HeadMin, noHead))
        {
            _portrait.SetHeadDirection(head);
        }
        ImGui.SameLine();

        var eye = _portrait.GetEyeDirection();
        var noEyes = FaceControlMessage(e.GazeControlType());
        if (FacingCanvas.PickFacing("Eye Direction", ref eye, _EyeMax, _EyeMin, noEyes))
        {
            _portrait.SetEyeDirection(eye);
        }
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
