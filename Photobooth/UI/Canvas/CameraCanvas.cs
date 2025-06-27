using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Photobooth.Controls;
using Photobooth.Maths;

namespace Photobooth.UI.Canvas;

/// <summary>
/// A canvas for the main camera view. Does not know where camera data comes
/// from or what to do with it, just knows how to draw it all.
/// </summary>
public class CameraCanvas : IDisposable
{
    // Tweakable stuff.
    private const uint BoundaryColor = 0xFFA0A0A0u;
    private const uint WedgeColor = 0x606060FFu;
    private const uint PlayerColor = 0xFF00EEEEu;
    private const uint CameraColor = 0xFF8080FFu;
    private const uint PivotColor = 0xFFE0E0E0u;
    private const uint TargetColor = PivotColor;
    private const uint SunColor = 0xFF00EEEEu;
    private const uint SunActiveColor = 0xFF44FFFFu;
    private const uint OrbitColor = 0x60FFFFFFu;
    private const uint PositionTextColor = 0xFF606060u;

    private const float HandleSize = 10f;
    private const float PlayerSize = 10f;
    private const float SunSize = 8f;
    private const float PivotSize = 6f;
    private const float WedgeLen = 300f;
    private const float SunOrbit = 230f;

    private static readonly Vector2 _Min = CameraConsts.PivotMin.XZ();
    private static readonly Vector2 _Max = CameraConsts.PivotMax.XZ();

    private Vector2 _pixels;
    private float HandlePixels => HandleSize * _pixels.X;

    private readonly ImGeo.Canvas _canvas;

    public void Dispose()
    {
        _canvas.Dispose();
    }

    public CameraCanvas(string label = "##CameraViewport")
    {
        var maxD = CameraConsts.DistanceMax;
        var min = CameraConsts.PivotMin;
        var max = CameraConsts.PivotMax;

        // Dimensions of the camera box, which other elements are relative to.
        var span = _Max - _Min + 2 * maxD * Vector2.One;

        // Draw and respond to controls.
        ImGui.SetNextItemWidth(-float.Epsilon);
        var w = ImGui.CalcItemWidth();
        var h = w * span.Y / span.X;

        _canvas = new ImGeo.Canvas(
            label,
            _Min - maxD * Vector2.One,
            _Max + maxD * Vector2.One,
            new Vector2(w, h)
        );

        _pixels = ImGeo.GetPixelSize();

        AddBackground();
    }

    public bool DragSun(ref SphereLL lightDirection)
    {
        // Light indicator.
        var lightAngle = -lightDirection.LonRadians + MathF.PI / 2;
        var (s, c) = MathF.SinCos(lightAngle);
        var lightPos = new Vector2(c, s) * SunOrbit;

        var newLightPos = lightPos;
        var changed = ImGeo.DragHandleCircle("##LightPosition", ref newLightPos, HandlePixels, 0);

        if (changed)
        {
            var newAngle = -(MathF.Atan2(newLightPos.Y, newLightPos.X) - MathF.PI / 2);
            lightDirection.SetRadians(lightDirection.LatRadians, newAngle);
        }

        if (ImGeo.IsHandleActive())
        {
            AddOrbitIndicator(lightPos, Vector2.Zero);
        }

        var active = ImGeo.IsHandleHovered() || ImGeo.IsHandleActive();
        AddLightMarker(lightPos, lightAngle, active);

        return changed;
    }

    public bool DragCamera(ref Vector2 cameraXZ)
    {
        return ImGeo.DragHandleCircle("##CameraPosition", ref cameraXZ, HandlePixels, CameraColor);
    }

    public bool DragPivot(ref Vector2 pivotXZ)
    {
        return ImGeo.DragHandleCircle("##CameraPivot", ref pivotXZ, HandlePixels, 0);
    }

    public bool DragTarget(ref Vector2 targetXZ)
    {
        return ImGeo.DragHandleCircle("##CameraTarget", ref targetXZ, HandlePixels, TargetColor);
    }

    public void AddCameraWedge(Vector2 cameraXZ, float facingAngle, float widthAngle)
    {
        var theta = -MathF.PI / 2 - facingAngle;
        var phi = widthAngle / 2;

        ImGeo.PathLineTo(cameraXZ);
        ImGeo.PathClear();
        ImGeo.PathArcTo(cameraXZ, WedgeLen, theta - phi, theta + phi);
        ImGeo.PathLineTo(cameraXZ);
        ImGeo.PathFillConvex(WedgeColor);
    }

    public void AddOrbitIndicator(Vector2 planet, Vector2 center)
    {
        var radius = Vector2.Distance(planet, center);
        ImGeo.AddCircle(center, radius, OrbitColor);
    }

    public void AddPlayerMarker(Vector2 pos, float angle)
    {
        var size = PlayerSize * _pixels.X;
        var (s, c) = MathF.SinCos(angle);
        var front = new Vector2(c, s) * size;
        var left = new Vector2(-s, c) * 0.8f * size - front;
        var right = new Vector2(s, -c) * 0.8f * size - front;

        ImGeo.AddTriangleFilled(pos + front, pos + left, pos + right, PlayerColor);
    }

    public void AddCameraApparatus(Vector2 cameraXZ, Vector2 pivotXZ, Vector2 targetXZ)
    {
        var forward = Vector2.Normalize(targetXZ - cameraXZ);

        AddPositionText(cameraXZ, targetXZ);
        ImGeo.AddLine(cameraXZ, targetXZ, PivotColor);
        AddTargetMarker(targetXZ, forward);
        AddPivotMarker(pivotXZ, forward);
    }

    private void AddPositionText(Vector2 cameraXZ, Vector2 targetXZ)
    {
        var cameraYaw = (cameraXZ - targetXZ).Atan2();
        var angle = (cameraYaw / MathF.Tau * 360f + 360f) % 360f;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        var text = $"{cameraXZ.X, 7:##0.00} X / {cameraXZ.Y, 7:##0.00} Y / {angle, 6:##0.00}°";
        var size = ImGui.CalcTextSize(text);
        var pos = ImGui.GetItemRectMax() - size - ImGui.GetStyle().ItemSpacing;

        ImGeo.GetActiveDrawList().AddText(pos, PositionTextColor, text);
    }

    private void AddBackground()
    {
        // Boundary area for camera pivot.
        ImGeo.AddRect(_Min, _Max, BoundaryColor);
    }

    private void AddPivotMarker(Vector2 pos, Vector2 forward)
    {
        forward = Vector2.Normalize(forward) * PivotSize * _pixels.X;
        var right = new Vector2(-forward.Y, forward.X);
        ImGeo.AddQuadFilled(pos - forward, pos + right, pos + forward, pos - right, PivotColor);
    }

    private void AddTargetMarker(Vector2 pos, Vector2 forward)
    {
        var unit = Vector2.Normalize(forward) * HandleSize * _pixels.X;
        var right = new Vector2(-unit.Y, unit.X) * 0.8f;
        var end = unit * MathF.Sqrt(3) / 2;
        ImGeo.AddTriangleFilled(
            pos + end,
            pos + right - unit / 2,
            pos - right - unit / 2,
            PivotColor
        );
    }

    private void AddLightMarker(Vector2 pos, float angle, bool hovered)
    {
        var col = hovered ? SunActiveColor : SunColor;

        var spikiness = 1.5f;
        var size = SunSize * _pixels.X;

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
