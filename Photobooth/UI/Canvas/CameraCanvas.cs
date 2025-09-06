using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
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
    private const uint PlayerColor = 0xFFF0B000u;
    private const uint CameraColor = 0xFF8080FFu;
    private const uint PivotColor = 0xFFE8E8E8u;
    private const uint AxisColor = 0xFFE0E0E0u;
    private const uint TargetColor = AxisColor;
    private const uint TargetActiveColor = 0xFFF0F0F0u;
    private const uint SunColor = 0xFF00EEEEu;
    private const uint SunActiveColor = 0xFF44FFFFu;
    private const uint OrbitColor = 0x60FFFFFFu;
    private const uint PositionTextColor = 0xFF606060u;
    private const uint LegendTextColor = 0xB0FFFFFFu;

    private const float HandleSize = 10f;
    private const float PlayerSize = 10f;
    private const float CameraSize = 6f;
    private const float SunSize = 8f;
    private const float PivotSize = 6f;
    private const float WedgeLen = 300f;
    private const float SunOrbit = 230f;

    private static readonly Vector2 _Min = CameraConsts.PivotMin.XZ();
    private static readonly Vector2 _Max = CameraConsts.PivotMax.XZ();

    private Vector2 _pixels;
    private float HandlePixels => HandleSize * _pixels.X;

    private readonly ImGeo.Canvas _canvas;

    // Since we draw everything after updates have been processed, we need to
    // keep track of these during the frame.
    private Items _activeItems = Items.None;
    private Items _hoveredItems = Items.None;

    private enum Items
    {
        None = 0,
        Camera = 1 << 1,
        Target = 1 << 2,
        Light = 1 << 3,
        Pivot = 1 << 4,
        Player = 1 << 5,
        All = Camera | Target | Light | Pivot | Player,
    }

    public void Dispose()
    {
        _canvas.Dispose();
    }

    public CameraCanvas(string label = "##CameraViewport")
    {
        // Dimensions of the camera box, which other elements are relative to.
        var maxD = CameraConsts.DistanceMax;
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

    private void CheckHandle(Items item)
    {
        _hoveredItems |= ImGeo.IsHandleHovered() ? item : Items.None;
        _activeItems |= ImGeo.IsHandleActive() ? item : Items.None;
    }

    private bool IsActive(Items item)
    {
        return _activeItems.HasFlag(item);
    }

    private bool IsHovered(Items item)
    {
        return _hoveredItems.HasFlag(item);
    }

    private bool IsActiveOrHovered(Items item)
    {
        return IsActive(item) || IsHovered(item);
    }

    private static (Vector2, float) SunPosition(SphereLL lightDirection)
    {
        var angle = -lightDirection.LonRadians + MathF.PI / 2;
        var (s, c) = MathF.SinCos(angle);
        var pos = new Vector2(c, s) * SunOrbit;
        return (pos, angle);
    }

    public bool DragSun(ref SphereLL lightDirection)
    {
        var (lightPos, _) = SunPosition(lightDirection);
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

        CheckHandle(Items.Light);

        return changed;
    }

    public bool DragCamera(ref Vector2 cameraXZ)
    {
        var changed = ImGeo.DragHandleCircle("##Camera", ref cameraXZ, HandlePixels, 0);
        CheckHandle(Items.Camera);
        return changed;
    }

    public bool DragTarget(ref Vector2 targetXZ)
    {
        var changed = ImGeo.DragHandleCircle("##Target", ref targetXZ, HandlePixels, 0);
        CheckHandle(Items.Target);
        return changed;
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

    public void AddPlayerMarker(
        Vector2 pos,
        float angle,
        float size = PlayerSize,
        uint col = PlayerColor
    )
    {
        var scale = size * _pixels.X;
        var (s, c) = MathF.SinCos(angle);
        var front = new Vector2(c, s) * scale;
        var left = new Vector2(-s, c) * 0.8f * scale - front;
        var right = new Vector2(s, -c) * 0.8f * scale - front;

        if (
            Vector2.Distance(pos, ImGeo.MouseViewPos()) < 1.5f * scale
            && _hoveredItems == Items.None
            && _activeItems == Items.None
        )
        {
            _hoveredItems |= Items.Player;
        }

        ImGeo.AddTriangleFilled(pos + front, pos + left, pos + right, col);
    }

    public void AddCameraApparatus(Vector2 cameraXZ, Vector2 pivotXZ, Vector2 targetXZ)
    {
        var forward = Vector2.Normalize(targetXZ - cameraXZ);

        ImGeo.AddLine(cameraXZ, targetXZ, AxisColor);

        AddPivotMarker(pivotXZ, forward);
        if (
            Vector2.Distance(pivotXZ, ImGeo.MouseViewPos()) < PivotSize * _pixels.X
            && _hoveredItems == Items.None
            && _activeItems == Items.None
        )
        {
            _hoveredItems |= Items.Pivot;
        }

        if (IsActiveOrHovered(Items.Camera))
        {
            ImGeo.DummyHandleCircle(cameraXZ, HandlePixels, false, false, CameraColor);
        }

        AddCameraMarker(cameraXZ, forward);

        if (IsActiveOrHovered(Items.Target))
        {
            ImGeo.DummyHandleCircle(targetXZ, HandlePixels, false, false, TargetColor);
        }

        AddTargetMarker(targetXZ, forward);
    }

    public void AddPositionText(Vector2 cameraXZ, Vector2 targetXZ)
    {
        var cameraYaw = (cameraXZ - targetXZ).Atan2();
        var angle = (cameraYaw / MathF.Tau * 360f + 360f) % 360f;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        var text = $"{cameraXZ.X, 7:##0.00} X / {cameraXZ.Y, 7:##0.00} Y / {angle, 6:##0.00}°";
        var size = ImGui.CalcTextSize(text);
        var pos = ImGui.GetItemRectMax() - size - ImGui.GetStyle().ItemSpacing;

        ImGeo.GetActiveDrawList().AddText(pos, PositionTextColor, text);
    }

    public void AddLightMarker(SphereLL lightDirection)
    {
        var (pos, angle) = SunPosition(lightDirection);
        AddLightIcon(pos, angle);
    }

    public void AddLegend()
    {
        var items = _activeItems | _hoveredItems;

        var lineHeight = ImGui.CalcTextSize("(#) Fimbulwinter eidolon").Y;
        var iconOffset = new Vector2(-lineHeight, 0.5f * lineHeight);
        var centerTop =
            ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X / 2, lineHeight / 4);

        var offset = Vector2.Zero;
        Vector2 showText(string text)
        {
            var textSize = ImGui.CalcTextSize(text);
            var textPos = offset + centerTop + new Vector2(-textSize.X / 2, 0);
            offset.Y += lineHeight;
            ImGeo.GetActiveDrawList().AddText(textPos, LegendTextColor, text);
            return ImGeo.ScreenToView(textPos + iconOffset);
        }

        uint iconColor(uint col)
        {
            return (col & 0x00FFFFFFu) | 0xA0000000u;
        }

        if (items.HasFlag(Items.Player))
        {
            var iconPos = showText("Player position");
            AddPlayerMarker(iconPos, 0, 7f, iconColor(PlayerColor));
        }

        if (items.HasFlag(Items.Camera))
        {
            var iconPos = showText("Camera position");
            AddCameraMarker(iconPos, Vector2.UnitY, 5f, iconColor(CameraColor));
        }

        if (items.HasFlag(Items.Target))
        {
            var iconPos = showText("Camera target");
            var color = iconColor(TargetColor);
            var direction = Vector2.UnitX;
            ImGeo.AddLine(iconPos - direction * 20f, iconPos - direction * 7f, color);
            AddTargetMarker(iconPos, direction, 7f, color);
        }

        if (items.HasFlag(Items.Light))
        {
            var iconPos = showText("Light direction");
            AddLightIcon(iconPos, 0, 5f, iconColor(SunColor));
        }

        if (items.HasFlag(Items.Pivot))
        {
            var iconPos = showText("Pivot point (default UI)");
            AddPivotMarker(iconPos, Vector2.UnitX, 5f, iconColor(PivotColor));
        }
    }

    private void AddBackground()
    {
        // Boundary area for camera pivot.
        ImGeo.AddRect(_Min, _Max, BoundaryColor);
    }

    private void AddPivotMarker(
        Vector2 pos,
        Vector2 forward,
        float size = PivotSize,
        uint col = PivotColor
    )
    {
        var pixelForward = Vector2.Normalize(forward) * _pixels.X;
        var u = size * pixelForward;
        var r = new Vector2(-u.Y, u.X);
        ImGeo.AddQuadFilled(pos - u, pos + r, pos + u, pos - r, col);
    }

    private void AddCameraMarker(
        Vector2 pos,
        Vector2 forward,
        float size = CameraSize,
        uint col = CameraColor
    )
    {
        var pixelForward = Vector2.Normalize(forward) * _pixels.X;
        var u = pixelForward * size;
        var r = new Vector2(-u.Y, u.X);

        ImGeo.AddQuadFilled(pos + u + r, pos + u - r, pos - u - r, pos - u + r, col);
    }

    private void AddTargetMarker(
        Vector2 pos,
        Vector2 forward,
        float size = HandleSize,
        uint col = TargetColor
    )
    {
        var pixelForward = Vector2.Normalize(forward) * _pixels.X;
        var u = pixelForward * size * 0.8f;
        var r = new Vector2(-u.Y, u.X);

        ImGeo.AddTriangleFilled(
            pos + u + pixelForward,
            pos + r - u * (MathF.Sqrt(3) - 1),
            pos - r - u * (MathF.Sqrt(3) - 1),
            col
        );
    }

    private void AddLightIcon(Vector2 pos, float angle, float size = SunSize, uint? col = null)
    {
        var scale = size * _pixels.X;
        var color = col ?? (IsActiveOrHovered(Items.Light) ? SunActiveColor : SunColor);

        var spikiness = 1.5f;
        ImGeo.AddCircleFilled(pos, scale, color);
        for (var i = 0; i < 8; i++)
        {
            // Draw a triangle pointing outward.
            var theta = angle + MathF.Tau * i / 8;
            var (s, c) = MathF.SinCos(theta);
            var front = new Vector2(c, s) * scale * spikiness;
            var left = new Vector2(-s, c) * scale;
            var right = new Vector2(s, -c) * scale;
            ImGeo.AddTriangleFilled(pos + front, pos + left, pos + right, color);
        }
    }
}
