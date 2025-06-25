using System;
using System.Numerics;
using ImGuiNET;
using PortraitTweaks.Controls;
using PortraitTweaks.Maths;

namespace PortraitTweaks.UI.Canvas;

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
    private const uint TargetColor = 0xFF00FF00u;
    private const uint SunColor = 0xFF00EEEEu;
    private const uint SunActiveColor = 0xFF44FFFFu;
    private const uint OrbitColor = 0x60FFFFFFu;

    private const float HandleSize = 10f;
    private const float PlayerSize = 10f;
    private const float SunSize = 8f;
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
        return ImGeo.DragHandleCircle("##CameraPivot", ref pivotXZ, HandlePixels, PivotColor);
    }

    public bool DragTarget(ref Vector2 targetXZ)
    {
        return ImGeo.DragHandleCircle("##CameraTarget", ref targetXZ, HandlePixels, TargetColor);
    }

    private void AddBackground()
    {
        // Boundary area for camera pivot.
        ImGeo.AddRect(_Min, _Max, BoundaryColor);
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
