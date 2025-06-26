using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Photobooth.Controls;
using Photobooth.Maths;
using Photobooth.UI.Canvas;
using Photobooth.UI.Stateless;

namespace Photobooth.UI.Panels;

internal class CameraPanel(PortraitController portrait, CameraController camera)
    : Panel(FontAwesomeIcon.Camera, "Camera")
{
    private readonly PortraitController _portrait = portrait;
    private readonly CameraController _camera = camera;

    private bool _followCharacter = false;
    private bool _compensateFoV = false;

    // The part of the body to face when facing/tracking the character.
    private const ushort MiddleFoot = 6;
    private const ushort Head = 26;
    private static readonly ushort _Part = Head;

    public override string? Help { get; } =
        "Left click: drag a handle to move the camera (circle), target (arrow), or pivot (diamond).\n"
        + "Hold shift when dragging to lock distance and only apply rotation.\n"
        + "Right click: drag anywhere to slide the whole camera setup around.\n"
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
            _camera.SetSubjectPosition(newSubject);
            if (_followCharacter)
            {
                _camera.FaceSubject();
                changed = true;
            }
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
            changed |= ResetRotationButton(new Vector2(buttonWidth, 0));
            changed |= FaceCharacterButton(new Vector2(buttonWidth, 0));
            changed |= TrackMotionCheckbox();
            changed |= FoVCompensationCheckbox();
        }

        ImGui.SameLine();
        var leftWidth = Math.Max(ImGui.GetCursorPosX(), startX + 0.3f * entireWidth);
        ImGui.SameLine(leftWidth);
        using (ImRaii.Group())
        {
            using var _ = ImRaii.ItemWidth(-float.Epsilon);

            changed |= ImageRotationSlider();
            changed |= PivotHeightSlider();
            changed |= CameraPitchSlider();
            changed |= CameraZoomSlider();
        }

        return changed;
    }

    private bool ResetRotationButton(Vector2 size)
    {
        var pressed = ImGui.Button("Reset Rotation", size);
        if (pressed)
        {
            _portrait.SetImageRotation(0);
        }

        return pressed;
    }

    private bool FaceCharacterButton(Vector2 size)
    {
        var pressed = ImGui.Button("Face Character", size);
        if (pressed)
        {
            _camera.FaceSubject();
        }
        return pressed;
    }

    private bool TrackMotionCheckbox()
    {
        var changed = ImGui.Checkbox("Track Motion", ref _followCharacter);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                "Continuously adjust the camera as the character moves\nUseful for PVP limit breaks and other poses with extreme movement."
            );
        }
        return changed;
    }

    private bool FoVCompensationCheckbox()
    {
        var changed = ImGui.Checkbox("FoV Adjust", ref _compensateFoV);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                "Move the camera closer or father when changing the lens's field of view,\nattempting to keep the same portion of the image in-frame."
            );
        }
        return changed;
    }

    private bool ImageRotationSlider()
    {
        var rotation = (int)_portrait.GetImageRotation();
        var changed = ImPT.IconSliderInt(
            "##rotation",
            FontAwesomeIcon.Redo,
            ref rotation,
            -CameraConsts.RotationMax,
            CameraConsts.RotationMax,
            "Rotation: %+d°",
            "Image rotation"
        );
        if (changed)
        {
            _portrait.SetImageRotation((short)rotation);
        }
        return changed;
    }

    private bool PivotHeightSlider()
    {
        var pivotY = _camera.Pivot.Y;
        var changed = ImPT.IconSliderFloat(
            "##height",
            FontAwesomeIcon.RulerVertical,
            ref pivotY,
            CameraConsts.PivotYMin,
            CameraConsts.PivotYMax,
            "Height: %.1f",
            "Height"
        );
        if (changed)
        {
            _camera.SetPivotPositionY(pivotY);
        }
        return changed;
    }

    private bool CameraPitchSlider()
    {
        var pitch = -_camera.Direction.LatDegrees;
        var changed = ImPT.IconSliderFloat(
            "##pitch",
            FontAwesomeIcon.ArrowsUpDown,
            ref pitch,
            -CameraConsts.PitchMax,
            -CameraConsts.PitchMin,
            "Angle: %+.0f°",
            "Pitch angle"
        );
        if (changed)
        {
            _camera.SetCameraPitchRadians(-MathF.Tau * pitch / 360);
        }
        return changed;
    }

    private bool CameraZoomSlider()
    {
        var f = _camera.FocalLength;
        var changed = ImPT.IconSliderFloat(
            "##zoom",
            FontAwesomeIcon.SearchPlus,
            ref f,
            CameraController.FocalLengthMin,
            CameraController.FocalLengthMax,
            "Lens: %.0fmm",
            "Focal length (zoom)"
        );
        if (changed)
        {
            _camera.SetFocalLength(f, _compensateFoV);
            _portrait.SetCameraZoom(_camera.Zoom);
        }
        return changed;
    }

    private bool CameraViewport(Editor e)
    {
        var changed = false;

        var subject = _camera.Subject;
        var cameraXZ = _camera.Camera.XZ();
        var pivotXZ = _camera.Pivot.XZ();
        var subjectXZ = subject.XZ();
        var targetXZ = _camera.TargetXZ;

        using var canvas = new CameraCanvas();

        var shiftHeld = ImGui.IsKeyDown(ImGuiKey.ModShift);

        // Camera view wedge.
        canvas.AddCameraWedge(cameraXZ, _camera.Direction.LonRadians, _camera.FoV);
        canvas.AddCameraApparatus(cameraXZ, pivotXZ, targetXZ);

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
        var newTargetXZ = targetXZ;
        if (canvas.DragTarget(ref newTargetXZ))
        {
            _camera.SetTargetPositionXZ(newTargetXZ, shiftHeld);
            changed = true;
        }

        if (shiftHeld && (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive()))
        {
            canvas.AddOrbitIndicator(targetXZ, cameraXZ);
        }

        // Camera position handle.
        var newCameraXZ = cameraXZ;
        if (canvas.DragCamera(ref newCameraXZ))
        {
            _camera.SetCameraPositionXZ(newCameraXZ, shiftHeld);
            changed = true;
        }

        if (shiftHeld && (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive()))
        {
            canvas.AddOrbitIndicator(cameraXZ, targetXZ);
        }

        // Camera pivot handle.
        var newPivotXZ = pivotXZ;
        if (canvas.DragPivot(ref newPivotXZ))
        {
            if (shiftHeld)
            {
                _camera.RotatePivotPositionXZ(newPivotXZ);
            }
            else
            {
                var deltaXZ = newPivotXZ - pivotXZ;
                _camera.Translate(new(deltaXZ.X, 0, deltaXZ.Y));
            }

            changed = true;
        }

        if (shiftHeld && (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive()))
        {
            canvas.AddOrbitIndicator(pivotXZ, subjectXZ);
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
                var pivotDelta = delta.InsertY(0);
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
