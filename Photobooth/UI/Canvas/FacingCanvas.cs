using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Photobooth.Maths;

namespace Photobooth.UI.Canvas;

/// <summary>
/// A canvas widget for picking a "half-direction", that is, a point on a
/// front-facing hemisphere, in terms of latitude and longitude.
/// </summary>
public class FacingCanvas : IDisposable
{
    private const float AspectRatio = 0.8f;

    private const uint BorderColor = 0xD0FFFFFF;
    private const uint DisabledColor = 0x80FFFFFF;
    private const uint TextColor = 0xFF606060u;

    private const float Padding = 12f;
    private const float HandleSize = 10f;

    private Vector2 _topLeft;
    private Vector2 _bottomRight;
    private Vector2 _screenSize;
    private Vector2 _viewPadding;
    private Vector2 _viewSize => _bottomRight - _topLeft;

    private readonly ImRaii.Id? _id = null;
    private readonly ImRaii.IEndObject? _group = null;
    private readonly ImGeo.Canvas? _canvas = null;

    public void Dispose()
    {
        _canvas?.Dispose();
        _group?.Dispose();
        _id?.Dispose();
    }

    public FacingCanvas(string label, Vector2 topLeft, Vector2 bottomRight)
    {
        _topLeft = topLeft;
        _bottomRight = bottomRight;

        _id = ImRaii.PushId(label);
        _group = ImRaii.Group();

        var hasLabel = !label.StartsWith("##");
        if (hasLabel)
        {
            ImGui.Text(label);
        }

        var width = ImGui.CalcItemWidth();
        _screenSize = new Vector2(width, MathF.Ceiling(AspectRatio * width));

        _viewPadding = Padding * _viewSize / _screenSize * ImGuiHelpers.GlobalScale;

        _canvas = new ImGeo.Canvas(
            "viewport",
            _topLeft - _viewPadding,
            _bottomRight + _viewPadding,
            _screenSize
        );

        AddBoundingBox();
    }

    /// <summary>
    /// Shorthand form, for when you don't need to anything else on the canvas.
    /// </summary>
    public static bool PickFacing(
        string label,
        ref SphereLL dir,
        Vector2 topleft,
        Vector2 bottomright,
        string? disabledReason = null
    )
    {
        using var canvas = new FacingCanvas(label, topleft, bottomright);

        if (disabledReason is not null)
        {
            canvas.AddTopText(disabledReason, DisabledColor);
            canvas.DummyDirection(ref dir);
            return false;
        }

        return canvas.DragDirection(ref dir);
    }

    public void AddTopText(string text, uint col = TextColor)
    {
        var textSize = ImGeo.CalcTextSize(text);
        var textOffset = new Vector2((_viewSize.X - textSize.X) / 2, 0);
        ImGeo.AddText(_topLeft + textOffset, col, text);
    }

    public bool DragDirection(ref SphereLL dir)
    {
        AddCoordinates(dir);

        var vec = dir.Degrees.Swap();
        var handlePx = HandleSize * ImGeo.GetPixelSize().X;

        var changed = false;

        // Do a regular drag handle.
        changed |= ImGeo.DragHandleCircle("##handle", ref vec, handlePx);

        // Also let double click set the direction.
        if (ImGeo.IsMouseInView() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            vec = ImGeo.MouseViewPos();
            changed = true;
        }

        // Clamp the xy coordinates to the specified bounds.
        var min_deg = Vector2.Min(_topLeft, _bottomRight);
        var max_deg = Vector2.Max(_topLeft, _bottomRight);
        vec = Vector2.Clamp(vec, min_deg, max_deg);

        if (changed)
        {
            dir.SetDegrees(vec.Y, vec.X);
        }

        return changed;
    }

    public void DummyDirection(ref SphereLL dir)
    {
        AddCoordinates(dir);
        var handlePx = HandleSize * ImGeo.GetPixelSize().X;
        ImGeo.AddCircle(dir.Degrees.Swap(), handlePx, DisabledColor);
    }

    private void AddBoundingBox()
    {
        // Bounding box for visibility.
        ImGeo.AddRect(_topLeft - _viewPadding, _bottomRight + _viewPadding, BorderColor);
    }

    private void AddCoordinates(SphereLL dir)
    {
        var lon = dir.LonDegrees;
        var lat = dir.LatDegrees;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        var text = $"{lon, 5:##0.0} H / {lat, 5:##0.0} V";
        var size = ImGui.CalcTextSize(text);
        var pos = ImGui.GetItemRectMax() - size - ImGui.GetStyle().ItemSpacing;

        ImGeo.GetActiveDrawList().AddText(pos, TextColor, text);
    }
}
