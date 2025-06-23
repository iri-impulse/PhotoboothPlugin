using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;
using ImGuiNET;
using PortraitTweaks.Maths;

namespace PortraitTweaks.UI;

public static partial class ImPT
{
    /// <summary>
    /// A widget for picking a "half-direction", that is, a point on a
    /// front-facing hemisphere, in terms of latitude and longitude.
    /// </summary>
    public static bool HalfDirection(
        string label,
        ref SphereLL dir,
        Vector2 topleft,
        Vector2 bottomright,
        float aspectRatio = 1f,
        string? disabledReason = null
    )
    {
        var borderColor = 0xD0FFFFFF;
        var disabledColor = 0x80FFFFFF;
        var textColor = 0x80FFFFFF;

        var xy = new Vector2(dir.LonDegrees, dir.LatDegrees);

        using var id = ImRaii.PushId(label);
        using var group = ImRaii.Group();

        var hasLabel = !label.StartsWith("##");
        if (hasLabel)
        {
            ImGui.Text(label);
        }

        var padPx = 12f;
        var handlePx = 10f;

        var width = ImGui.CalcItemWidth();
        var screenSize = new Vector2(width, MathF.Ceiling(aspectRatio * width));
        var viewSize = bottomright - topleft;
        var pad = padPx * viewSize / screenSize;
        ImGeo.BeginViewport("viewport", topleft - pad, bottomright + pad, screenSize);

        // Bounding box for visibility.
        var pixels = ImGeo.ScaleToView(Vector2.One);
        ImGeo.AddRect(topleft - pad, bottomright + pad, borderColor);

        // Display the numerical values.
        var textSize = ImGeo.CalcTextSize("H: -XXX.0° ");
        var textPos = new Vector2(topleft.X + pad.X, bottomright.Y - textSize.Y / 2);
        ImGeo.AddText(textPos, textColor, $"H: {xy.X:F1}°");
        ImGeo.AddText(textPos + new Vector2(textSize.X, 0), textColor, $"V: {xy.Y:F1}°");

        // Do a drag handle for the direction.
        var changed = false;
        if (disabledReason is null)
        {
            changed |= ImGeo.DragHandleCircle("handle", ref xy, handlePx * pixels.X);
        }
        else
        {
            ImGeo.AddCircle(xy, handlePx * pixels.X, disabledColor);
            var disabledSize = ImGui.CalcTextSize(disabledReason);
            var midTop = new Vector2(
                topleft.X + (viewSize.X - disabledSize.X * pixels.X) / 2,
                topleft.Y
            );
            ImGeo.AddText(midTop, disabledColor, disabledReason);
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
