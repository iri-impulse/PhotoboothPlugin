using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PortraitTweaks.Controls;
using PortraitTweaks.Maths;
using PortraitTweaks.UI.Canvas;
using PortraitTweaks.UI.Stateless;

namespace PortraitTweaks.UI.Panels;

internal class CameraPanel(PortraitController portrait, CameraController camera)
    : Panel(FontAwesomeIcon.Camera, "Camera")
{
    private readonly PortraitController _portrait = portrait;
    private readonly CameraController _camera = camera;

    private bool _followCharacter = false;

    // The part of the body to face when facing/tracking the character.
    private static readonly ushort _Part = 6;

    public override string? Help { get; } =
        "Left click: drag a handle to rotate the camera (red) or pivot point (white).\n"
        + "Right click: drag anywhere to slide camera and pivot together.\n"
        + "Mousewheel: adjust camera distance.";

    public override void Reset()
    {
        _followCharacter = false;
    }

    protected override void DrawBody()
    {
        var e = Editor.Current();
        if (!e.IsValid)
        {
            return;
        }

        var changed = false;

        // Track moving character.
        var newSubject = e.CharacterPosition(_Part);
        if (_portrait.IsAnimationStable)
        {
            _camera.SetSubjectPosition(newSubject, _followCharacter);
            changed |= _followCharacter;
        }

        // Camera canvas area.
        changed |= CameraViewport(e);

        changed |= CameraWidgets();

        if (changed)
        {
            _camera.Save(e);
            e.SetHasChanged(true);
        }
    }

    private bool CameraWidgets()
    {
        var changed = false;

        var startX = ImGui.GetCursorPosX();
        var entireWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            var buttonWidth = 0.3f * entireWidth;
            if (ImGui.Button("Face Character", new Vector2(buttonWidth, 0)))
            {
                _camera.FaceSubject();
                changed = true;
            }

            ImGui.Checkbox("Track Motion", ref _followCharacter);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    "Continuously adjust the camera as the character moves\nUseful for PVP limit breaks and other poses with extreme movement."
                );
            }
        }

        ImGui.SameLine();
        var leftWidth = Math.Max(ImGui.GetCursorPosX(), startX + 0.3f * entireWidth);
        ImGui.SameLine(leftWidth);
        using (ImRaii.Group())
        {
            using var _ = ImRaii.ItemWidth(-float.Epsilon);

            // Height slider.
            var hIcon = FontAwesomeIcon.RulerVertical;
            var pivotY = _camera.Pivot.Y;
            if (
                ImPT.IconSliderFloat(
                    "##height",
                    hIcon,
                    ref pivotY,
                    CameraConsts.PivotYMin,
                    CameraConsts.PivotYMax,
                    "%.1f",
                    "Height"
                )
            )
            {
                _camera.SetPivotPositionY(pivotY, false);
                changed = true;
            }

            // Pitch slider.
            var pIcon = FontAwesomeIcon.ArrowsUpDown;
            var pitch = -_camera.Direction.LatDegrees;
            if (
                ImPT.IconSliderFloat(
                    "##pitch",
                    pIcon,
                    ref pitch,
                    -CameraConsts.PitchMax,
                    -CameraConsts.PitchMin,
                    "%.1f",
                    "Pitch up/down"
                )
            )
            {
                _camera.SetCameraPitchRadians(-MathF.Tau * pitch / 360);
                changed = true;
            }
        }

        return changed;
    }

    private bool CameraViewport(Editor e)
    {
        var changed = false;

        var subject = _camera.Subject;
        var cameraXZ = new Vector2(_camera.Camera.X, _camera.Camera.Z);
        var pivotXZ = new Vector2(_camera.Pivot.X, _camera.Pivot.Z);
        var subjectXZ = new Vector2(subject.X, subject.Z);

        using var canvas = new CameraCanvas();

        // Camera view wedge.
        canvas.AddCameraWedge(cameraXZ, _camera.Direction.LonRadians, _camera.ZoomRadians);

        // Draggable sun for light angle.
        var lightDirection = _portrait.GetDirectionalLightDirection();
        if (canvas.DragSun(ref lightDirection))
        {
            _portrait.SetDirectionalLightDirection(lightDirection);
            changed = true;
        }

        // Character center indicator.
        canvas.AddPlayerMarker(subjectXZ, e.CharacterDirection());

        // Camera target handle.
        var targetXZ = _camera.TargetXZ;
        var newTargetXZ = targetXZ;
        if (canvas.DragTarget(ref newTargetXZ))
        {
            _camera.SetTargetPositionXZ(newTargetXZ);
            changed = true;
        }

        if (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive())
        {
            canvas.AddOrbitIndicator(targetXZ, cameraXZ);
        }

        // Camera pivot handle.
        var newPivotXZ = pivotXZ;
        if (canvas.DragPivot(ref newPivotXZ))
        {
            _camera.SetPivotPositionXZ(newPivotXZ, true);
            changed = true;
        }

        if (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive())
        {
            canvas.AddOrbitIndicator(pivotXZ, subjectXZ);
        }

        // Camera position handle.
        var newCameraXZ = cameraXZ;
        if (canvas.DragCamera(ref newCameraXZ))
        {
            _camera.SetCameraPositionXZ(newCameraXZ);
            changed = true;
        }

        if (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive())
        {
            canvas.AddOrbitIndicator(cameraXZ, targetXZ);
        }

        // Right click pan.
        var panning =
            !changed
            && ImGui.IsItemHovered()
            && ImGui.IsMouseDown(ImGuiMouseButton.Right)
            && !ImGui.IsAnyItemActive();
        if (panning)
        {
            var delta = ImGeo.ScaleToView(ImGui.GetIO().MouseDelta);
            if (delta.LengthSquared() > 1e-6f)
            {
                var pivotDelta = new Vector3(delta.X, 0, delta.Y);
                _camera.Translate(pivotDelta);
                changed = true;
            }
        }

        // Mousewheel zoom.
        if (ImGui.IsItemHovered())
        {
            var delta = ImGui.GetIO().MouseWheel;
            if (delta != 0)
            {
                _camera.AdjustCameraDistance(-delta);
                changed = true;
            }
        }

        return changed;
    }
}
