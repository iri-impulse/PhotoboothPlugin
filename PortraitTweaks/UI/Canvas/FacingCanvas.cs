using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PortraitTweaks.Maths;

namespace PortraitTweaks.UI.Canvas;

public static class FacingCanvas
{
    private static readonly float _AspectRatio = 0.8f;

    private static readonly uint _BorderColor = 0xD0FFFFFF;
    private static readonly uint _DisabledColor = 0x80FFFFFF;
    private static readonly uint _TextColor = 0x80FFFFFF;

    private static readonly float _Padding = 12f;
    private static readonly float _HandleSize = 10f;

    /// <summary>
    /// A widget for picking a "half-direction", that is, a point on a
    /// front-facing hemisphere, in terms of latitude and longitude.
    /// </summary>
    public static bool PickFacing(
        string label,
        ref SphereLL dir,
        Vector2 topleft,
        Vector2 bottomright,
        string? disabledReason = null
    )
    {
        var xy = new Vector2(dir.LonDegrees, dir.LatDegrees);

        using var id = ImRaii.PushId(label);
        using var group = ImRaii.Group();

        var hasLabel = !label.StartsWith("##");
        if (hasLabel)
        {
            ImGui.Text(label);
        }

        var width = ImGui.CalcItemWidth();
        var screenSize = new Vector2(width, MathF.Ceiling(_AspectRatio * width));
        var viewSize = bottomright - topleft;
        var pad = _Padding * viewSize / screenSize * ImGuiHelpers.GlobalScale;

        ImGeo.BeginViewport("viewport", topleft - pad, bottomright + pad, screenSize);

        var pixels = ImGeo.GetPixelSize();
        var handlePx = _HandleSize * pixels.X;

        // Bounding box for visibility.
        ImGeo.AddRect(topleft - pad, bottomright + pad, _BorderColor);

        // Display the numerical values.
        var textSize = ImGeo.CalcTextSize("H: -XXX.0° ");
        var textPos = new Vector2(topleft.X + pad.X, bottomright.Y - textSize.Y / 2);
        ImGeo.AddText(textPos, _TextColor, $"H: {xy.X:F1}°");
        ImGeo.AddText(textPos + new Vector2(textSize.X, 0), _TextColor, $"V: {xy.Y:F1}°");

        // Do a drag handle for the direction.
        var changed = false;
        if (disabledReason is null)
        {
            changed |= ImGeo.DragHandleCircle("handle", ref xy, handlePx);
        }
        else
        {
            ImGeo.AddCircle(xy, handlePx, _DisabledColor);
            var disabledSize = ImGeo.CalcTextSize(disabledReason);
            var midTop = new Vector2(topleft.X + (viewSize.X - disabledSize.X) / 2, topleft.Y);
            ImGeo.AddText(midTop, _DisabledColor, disabledReason);
        }

        // Also let double click set the direction.
        if (ImGeo.IsMouseInView() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            xy = ImGeo.MouseViewPos();
            changed = true;
        }

        // Clamp the xy coordinates to the specified bounds.
        var min_deg = Vector2.Min(topleft, bottomright);
        var max_deg = Vector2.Max(topleft, bottomright);
        xy = Vector2.Clamp(xy, min_deg, max_deg);

        // Clean up after ourselves (
        ImGeo.EndViewport();

        if (changed)
        {
            dir.SetDegrees(xy.Y, xy.X);
        }
        return changed;
    }
}
