using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PortraitTweaks.Controls;
using PortraitTweaks.Maths;
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

        // Tweakable stuff.
        var boundaryColor = 0xFFA0A0A0u;
        var wedgeColor = 0x606060FFu;
        var characterColor = 0xFF00EEEEu;
        var cameraColor = 0xFF8080FFu;
        var pivotColor = 0xFFE0E0E0u;
        var targetColor = 0xFF00FF00u;
        var sunColor = 0xFF00EEEEu;
        var sunActiveColor = 0xFF44FFFFu;

        var handleSize = 10f;
        var playerSize = 10f;
        var sunSize = 8f;
        var wedgeLen = 300f;
        var lightRadius = 230f;

        var maxD = CameraConsts.DistanceMax;
        var min = CameraConsts.PivotMin;
        var max = CameraConsts.PivotMax;

        // Dimensions of the camera box, which other elements are relative to.
        var xSpan = max.X - min.X + 2 * maxD;
        var zSpan = max.Z - min.Z + 2 * maxD;

        // Draw and respond to controls.
        ImGui.SetNextItemWidth(-float.Epsilon);
        var w = ImGui.CalcItemWidth();
        var h = w * zSpan / xSpan;
        ImGeo.BeginViewport(
            "##CameraViewport",
            new Vector2(min.X - maxD, min.Z - maxD),
            new Vector2(max.X + maxD, max.Z + maxD),
            new Vector2(w, w * zSpan / xSpan)
        );
        var pixels = ImGeo.GetPixelSize().X;

        var subject = _camera.Subject;
        var cameraXZ = new Vector2(_camera.Camera.X, _camera.Camera.Z);
        var pivotXZ = new Vector2(_camera.Pivot.X, _camera.Pivot.Z);
        var subjectXZ = new Vector2(subject.X, subject.Z);

        // Boundary area for camera pivot.
        ImGeo.AddRect(new Vector2(min.X, min.Z), new Vector2(max.X, max.Z), boundaryColor);

        // Camera wedge.
        var theta = -MathF.PI / 2 - _camera.Direction.LonRadians;
        var phi = _camera.ZoomRadians / 2;

        ImGeo.PathLineTo(cameraXZ);
        ImGeo.PathClear();
        ImGeo.PathArcTo(cameraXZ, wedgeLen, theta - phi, theta + phi);
        ImGeo.PathLineTo(cameraXZ);
        ImGeo.PathFillConvex(wedgeColor);

        // Light indicator.
        var lightDirection = _portrait.GetDirectionalLightDirection();
        var lightAngle = -lightDirection.LonRadians + MathF.PI / 2;
        var lightPos = new Vector2(
            MathF.Cos(lightAngle) * lightRadius,
            MathF.Sin(lightAngle) * lightRadius
        );
        var newLightPos = lightPos;
        if (ImGeo.DragHandleCircle("##LightPosition", ref newLightPos, handleSize * pixels, 0))
        {
            var newAngle = -(MathF.Atan2(newLightPos.Y, newLightPos.X) - MathF.PI / 2);
            var newDirection = SphereLL.FromRadians(lightDirection.LatRadians, newAngle);
            _portrait.SetDirectionalLightDirection(newDirection);
            changed = true;
        }
        var color = ImGeo.IsHandleHovered() || ImGeo.IsHandleActive() ? sunActiveColor : sunColor;
        AddLightMarker(lightPos, lightAngle, sunSize * pixels, color);

        // Character center indicator.
        AddPlayerMarker(subjectXZ, e.CharacterDirection(), playerSize * pixels, characterColor);

        // Handles.
        var handlePixels = handleSize * pixels;

        // Camera target handle.
        var targetXZ = _camera.TargetXZ;
        var newTargetXZ = targetXZ;
        if (ImGeo.DragHandleCircle("##CameraTarget", ref newTargetXZ, handlePixels, targetColor))
        {
            _camera.SetTargetPositionXZ(newTargetXZ);
            changed = true;
        }

        if (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive())
        {
            ImGeo.AddCircle(cameraXZ, Vector2.Distance(cameraXZ, targetXZ), targetColor);
        }

        // Camera pivot handle.
        var newPivotXZ = pivotXZ;
        if (ImGeo.DragHandleCircle("##CameraPivot", ref newPivotXZ, handlePixels, pivotColor))
        {
            _camera.SetPivotPositionXZ(newPivotXZ, true);
            changed = true;
        }

        if (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive())
        {
            ImGeo.AddCircle(subjectXZ, Vector2.Distance(pivotXZ, subjectXZ), pivotColor);
        }

        // Camera position handle.
        var newCameraXZ = cameraXZ;
        if (ImGeo.DragHandleCircle("##CameraPosition", ref newCameraXZ, handlePixels, cameraColor))
        {
            _camera.SetCameraPositionXZ(newCameraXZ);
            changed = true;
        }

        if (ImGeo.IsHandleHovered() || ImGeo.IsHandleActive())
        {
            ImGeo.AddCircle(targetXZ, Vector2.Distance(cameraXZ, targetXZ), cameraColor);
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

        ImGeo.EndViewport();

        return changed;
    }

    private static void AddPlayerMarker(Vector2 position, float angle, float size, uint col)
    {
        var (s, c) = MathF.SinCos(angle);
        var front = new Vector2(c, s) * size;
        var left = new Vector2(-s, c) * 0.8f * size - front;
        var right = new Vector2(s, -c) * 0.8f * size - front;

        ImGeo.AddTriangleFilled(position + front, position + left, position + right, col);
    }

    private static void AddLightMarker(Vector2 pos, float angle, float size, uint col)
    {
        var spikiness = 1.5f;

        ImGeo.AddCircleFilled(pos, size, col);
        for (var i = 0; i < 8; i++)
        {
            // Draw a triangle pointing outward.
            var theta = angle + MathF.Tau * i / 8;
            var (s, c) = MathF.SinCos(theta);
            var front = new Vector2(c, s) * size * spikiness;
            var left = new Vector2(-s, c) * size;
            var right = new Vector2(s, -c) * size;
            ImGeo.AddTriangleFilled(pos + front, pos + left, pos + right, col);
        }
    }
}
