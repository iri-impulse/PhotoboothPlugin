using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Photobooth.Maths;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    /// <summary>
    /// A spherical direction via two sliders.
    /// </summary>
    public static bool Direction(
        string label,
        ref SphereLL dir,
        float x_deg_min,
        float x_deg_max,
        float y_deg_min,
        float y_deg_max
    )
    {
        using var id = ImRaii.PushId(label);
        using var group = ImRaii.Group();

        var hasLabel = !label.StartsWith("##");

        if (hasLabel)
        {
            ImGui.Text(label);
        }

        var changed = false;

        var lat = dir.LatDegrees;
        var lon = dir.LonDegrees;

        var iconColor = ImGui.GetStyle().Colors[((int)ImGuiCol.Button)].WithAlpha(0.2f);

        var latIcon = FontAwesomeIcon.ArrowsAltV;
        changed |= IconSliderFloat(
            "##lat",
            latIcon,
            ref lat,
            y_deg_min,
            y_deg_max,
            "V: %.0f°",
            "Vertical angle"
        );

        var lonIcon = FontAwesomeIcon.ArrowsAltH;
        changed |= IconSliderFloat(
            "##lon",
            lonIcon,
            ref lon,
            x_deg_min,
            x_deg_max,
            "H: %.0f°",
            "Horizontal angle"
        );

        if (changed)
        {
            dir.SetDegrees(lat, lon);
        }

        return changed;
    }
}
