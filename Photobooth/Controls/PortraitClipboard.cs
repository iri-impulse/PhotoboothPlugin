using System;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Photobooth.Maths;

namespace Photobooth.Controls;

internal static class PortraitClipboard
{
    private const string Prefix = "PhotoboothPortrait:v1:";

    public static string Export(PortraitController portrait, CameraController camera)
    {
        var settings = PortraitClipboardSettings.From(portrait.Data, camera);
        var json = JsonSerializer.Serialize(settings);
        return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static bool TryImport(
        string text,
        PortraitController portrait,
        CameraController camera,
        out string message
    )
    {
        message = string.Empty;

        if (!text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            message = "Clipboard does not contain Photobooth portrait settings.";
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(text[Prefix.Length..]));
            var settings = JsonSerializer.Deserialize<PortraitClipboardSettings>(json);
            if (settings == null || settings.Version != 1)
            {
                message = "Unsupported Photobooth portrait settings version.";
                return false;
            }

            portrait.Import(settings);
            camera.Import(settings.Camera);
            message = "Portrait settings pasted.";
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            message = "Could not read Photobooth portrait settings from clipboard.";
            return false;
        }
    }

    internal sealed class PortraitClipboardSettings
    {
        public int Version { get; set; } = 1;

        public int BannerTimeline { get; set; }
        public float AnimationProgress { get; set; }
        public int Expression { get; set; }

        public float HeadX { get; set; }
        public float HeadY { get; set; }
        public float EyeX { get; set; }
        public float EyeY { get; set; }

        public byte AmbientRed { get; set; }
        public byte AmbientGreen { get; set; }
        public byte AmbientBlue { get; set; }
        public byte AmbientBrightness { get; set; }

        public byte DirectionalRed { get; set; }
        public byte DirectionalGreen { get; set; }
        public byte DirectionalBlue { get; set; }
        public byte DirectionalBrightness { get; set; }
        public short DirectionalVerticalAngle { get; set; }
        public short DirectionalHorizontalAngle { get; set; }

        public short ImageRotation { get; set; }
        public byte CameraZoom { get; set; }
        public CameraClipboardSettings Camera { get; set; } = new();

        public static PortraitClipboardSettings From(
            FFXIVClientStructs.FFXIV.Client.UI.Misc.ExportedPortraitData data,
            CameraController camera
        )
        {
            return new PortraitClipboardSettings
            {
                BannerTimeline = data.BannerTimeline,
                AnimationProgress = data.AnimationProgress,
                Expression = data.Expression,
                HeadX = (float)data.HeadDirection.X,
                HeadY = (float)data.HeadDirection.Y,
                EyeX = (float)data.EyeDirection.X,
                EyeY = (float)data.EyeDirection.Y,
                AmbientRed = data.AmbientLightingColorRed,
                AmbientGreen = data.AmbientLightingColorGreen,
                AmbientBlue = data.AmbientLightingColorBlue,
                AmbientBrightness = data.AmbientLightingBrightness,
                DirectionalRed = data.DirectionalLightingColorRed,
                DirectionalGreen = data.DirectionalLightingColorGreen,
                DirectionalBlue = data.DirectionalLightingColorBlue,
                DirectionalBrightness = data.DirectionalLightingBrightness,
                DirectionalVerticalAngle = data.DirectionalLightingVerticalAngle,
                DirectionalHorizontalAngle = data.DirectionalLightingHorizontalAngle,
                ImageRotation = data.ImageRotation,
                CameraZoom = data.CameraZoom,
                Camera = CameraClipboardSettings.From(camera),
            };
        }
    }

    internal sealed class CameraClipboardSettings
    {
        public float PivotX { get; set; }
        public float PivotY { get; set; }
        public float PivotZ { get; set; }
        public float Distance { get; set; }
        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public byte Zoom { get; set; }

        public static CameraClipboardSettings From(CameraController camera)
        {
            return new CameraClipboardSettings
            {
                PivotX = camera.Pivot.X,
                PivotY = camera.Pivot.Y,
                PivotZ = camera.Pivot.Z,
                Distance = camera.Distance,
                Pitch = camera.Direction.LatRadians,
                Yaw = camera.Direction.LonRadians,
                Zoom = camera.Zoom,
            };
        }

        public Vector3 Pivot => new(PivotX, PivotY, PivotZ);

        public SphereLL Direction => SphereLL.FromRadians(Pitch, Yaw);
    }
}
