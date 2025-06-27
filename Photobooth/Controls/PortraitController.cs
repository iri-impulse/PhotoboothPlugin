using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using Photobooth.Maths;

namespace Photobooth.Controls;

public class PortraitController : IDisposable
{
    public ExportedPortraitData Data;

    private PortraitChanges _changes = PortraitChanges.None;

    // If unset, fall back to SetPoseTimed when setting durations. This is more
    // reliable but causes atrocious flickering.
    public static bool UseNew { get; set; } = true;

    public bool IsAnimationStable => _framesSinceReset > 3;

    private bool _progressPinnedNow = false;
    private bool _progressPinnedLast = false;

    private int _framesSinceReset = 0;

    // For manual fine-tuing.
    public static float ManualCorrectionFactor { get; set; } = 1.25f;
    public static bool UseManualCorrectionFactor { get; set; } = false;

    public PortraitController(ExportedPortraitData data)
    {
        this.Data = data;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var e = Editor.Current();
        if (!e.IsValid)
        {
            return;
        }

        var changed = ApplyChanges(e);
        if (changed)
        {
            e.SetHasChanged(true);
            e.UpdateUI(in Data);
        }

        _progressPinnedLast = _progressPinnedNow;
        _framesSinceReset = Math.Min(60, _framesSinceReset + 1);
    }

    private BannerTimeline BannerTimeline
    {
        get
        {
            if (field.RowId != Data.BannerTimeline)
            {
                var sheet = Plugin.DataManager.GetExcelSheet<BannerTimeline>();
                field = sheet.GetRowAt(Data.BannerTimeline);
            }
            return field;
        }
    }

    public void SetPinned(bool pinned)
    {
        _progressPinnedNow = pinned;
    }

    public unsafe void CopyData(Editor e)
    {
        var portrait = e.Portrait;
        var prevProgress = Data.AnimationProgress;
        var prevPose = Data.BannerTimeline;

        fixed (ExportedPortraitData* data = &Data)
        {
            portrait->ExportPortraitData(data);

            if (_progressPinnedNow || _progressPinnedLast || Data.BannerTimeline == 0)
            {
                // While dragging the animation slider, the reported progress is
                // sometimes 0, and sometimes off by a frame. Either way, we can
                // just preserve our previous progress while dragging.
                _changes = _changes & PortraitChanges.AnimationProgress;
                Data.AnimationProgress = prevProgress;
            }
            else
            {
                _changes = PortraitChanges.None;
            }
        }
    }

    public bool HasChanges()
    {
        return _changes != PortraitChanges.None;
    }

    /// <summary>
    /// Apply changes to a CharaViewPortrait, somewhat delicately.
    /// </summary>
    public unsafe bool ApplyChanges(Editor e)
    {
        // CharaViewPortrait::ImportPortraitData flickers the whole portrait
        // for 1-2 frames, since it uses SetPoseTimed. We set each changed item
        // individually to minimize flickering, camera jitter, and so on.

        var portrait = e.Portrait;
        var changed = _changes != PortraitChanges.None;

        if (_progressPinnedNow)
        {
            var progress = Data.AnimationProgress;

            if (BannerTimeline.Type == 2 || BannerTimeline.Type == 20)
            {
                progress -= CorrectionFactor(Framework.Instance()->FrameRate);
                e.ToggleAnimationPlayback(false);
            }

            e.SetAnimationProgress(progress);
        }
        else if (_progressPinnedLast || _changes.Take(PortraitChanges.AnimationProgress))
        {
            // SetPoseTimed misses the target by 0.001, it seems? Without this
            // correction we're not round-trip accurate.
            portrait->SetPoseTimed(Data.BannerTimeline, Data.AnimationProgress + 0.001f);
            _framesSinceReset = 0;
        }

        if (_changes.Take(PortraitChanges.AmbientLightColor))
        {
            portrait->SetAmbientLightingColor(
                Data.AmbientLightingColorRed,
                Data.AmbientLightingColorGreen,
                Data.AmbientLightingColorBlue
            );
            portrait->SetAmbientLightingBrightness(Data.AmbientLightingBrightness);
        }

        if (_changes.Take(PortraitChanges.DirectionalLightColor))
        {
            portrait->SetDirectionalLightingColor(
                Data.DirectionalLightingColorRed,
                Data.DirectionalLightingColorGreen,
                Data.DirectionalLightingColorBlue
            );
            portrait->SetDirectionalLightingBrightness(Data.DirectionalLightingBrightness);
        }

        if (_changes.Take(PortraitChanges.DirectionalLightDirection))
        {
            portrait->SetDirectionalLightingAngle(
                Data.DirectionalLightingVerticalAngle,
                Data.DirectionalLightingHorizontalAngle
            );
        }

        if (_changes.Take(PortraitChanges.EyeDirection))
        {
            portrait->SetEyeDirection((float)Data.EyeDirection.X, (float)Data.EyeDirection.Y);
        }

        if (_changes.Take(PortraitChanges.HeadDirection))
        {
            portrait->SetHeadDirection((float)Data.HeadDirection.X, (float)Data.HeadDirection.Y);
        }

        if (_changes.Take(PortraitChanges.Pose))
        {
            portrait->SetPoseTimed(Data.BannerTimeline, Data.AnimationProgress);
            _changes.Take(PortraitChanges.AnimationProgress);
        }

        if (_changes.Take(PortraitChanges.Expression))
        {
            portrait->SetExpression(Data.Expression);
        }

        if (_changes.Take(PortraitChanges.CameraZoom))
        {
            portrait->SetCameraZoom(Data.CameraZoom);
        }

        if (_changes.Take(PortraitChanges.CameraPosition | PortraitChanges.CameraTarget))
        {
            fixed (ExportedPortraitData* data = &Data)
            {
                portrait->SetCameraPosition(&data->CameraPosition, &data->CameraTarget);
            }
        }

        if (_changes.Take(PortraitChanges.ImageRotation))
        {
            portrait->ImageRotation = Data.ImageRotation;
        }

        if (_changes.Take(PortraitChanges.BannerBg))
        {
            portrait->SetBackground(Data.BannerBg);
        }

        return changed;
    }

    public float GetAnimationProgress()
    {
        return Data.AnimationProgress;
    }

    public void SetAnimationProgress(float progress)
    {
        Data.AnimationProgress = progress;
        _changes |= PortraitChanges.AnimationProgress;
    }

    public SphereLL GetHeadDirection()
    {
        return SphereLL.FromRadians((float)Data.HeadDirection.Y, (float)Data.HeadDirection.X);
    }

    public void SetHeadDirection(SphereLL d)
    {
        Data.HeadDirection.X = (Half)d.LonRadians;
        Data.HeadDirection.Y = (Half)d.LatRadians;
        _changes |= PortraitChanges.HeadDirection;
    }

    public SphereLL GetEyeDirection()
    {
        return SphereLL.FromRadians((float)Data.EyeDirection.Y, (float)Data.EyeDirection.X);
    }

    public void SetEyeDirection(SphereLL d)
    {
        Data.EyeDirection.X = (Half)d.LonRadians;
        Data.EyeDirection.Y = (Half)d.LatRadians;
        _changes |= PortraitChanges.EyeDirection;
    }

    public SphereLL GetDirectionalLightDirection()
    {
        var v = Data.DirectionalLightingVerticalAngle;
        var h = Data.DirectionalLightingHorizontalAngle;

        return SphereLL.FromDegrees(v, h).Normalized();
    }

    public void SetDirectionalLightDirection(SphereLL d)
    {
        // Portraits want angles in [-180, 180] degrees.
        var lat = d.LatDegrees;
        var lon = (d.LonDegrees % 360 + 180) % 360 - 180;
        Data.DirectionalLightingVerticalAngle = (short)MathF.Floor(lat);
        Data.DirectionalLightingHorizontalAngle = (short)MathF.Floor(lon);
        _changes |= PortraitChanges.DirectionalLightDirection;
    }

    public RGBA GetAmbientLightColor()
    {
        return new RGBA(
            Data.AmbientLightingColorRed,
            Data.AmbientLightingColorGreen,
            Data.AmbientLightingColorBlue,
            Data.AmbientLightingBrightness
        );
    }

    public void SetAmbientLightColor(RGBA color)
    {
        Data.AmbientLightingColorRed = color.R;
        Data.AmbientLightingColorGreen = color.G;
        Data.AmbientLightingColorBlue = color.B;
        Data.AmbientLightingBrightness = ClampBrightness(color.A);
        _changes |= PortraitChanges.AmbientLightColor;
    }

    public RGBA GetDirectionalLightColor()
    {
        return new RGBA(
            Data.DirectionalLightingColorRed,
            Data.DirectionalLightingColorGreen,
            Data.DirectionalLightingColorBlue,
            Data.DirectionalLightingBrightness
        );
    }

    public void SetDirectionalLightColor(RGBA color)
    {
        Data.DirectionalLightingColorRed = color.R;
        Data.DirectionalLightingColorGreen = color.G;
        Data.DirectionalLightingColorBlue = color.B;
        Data.DirectionalLightingBrightness = ClampBrightness(color.A);
        _changes |= PortraitChanges.DirectionalLightColor;
    }

    public Vector3 GetCameraTarget()
    {
        return new Vector3(
            (float)Data.CameraTarget.X,
            (float)Data.CameraTarget.Y,
            (float)Data.CameraTarget.Z
        );
    }

    public void SetCameraTarget(Vector3 target)
    {
        Data.CameraTarget.X = (Half)target.X;
        Data.CameraTarget.Y = (Half)target.Y;
        Data.CameraTarget.Z = (Half)target.Z;
        _changes |= PortraitChanges.CameraTarget;
    }

    public short GetImageRotation()
    {
        return Data.ImageRotation;
    }

    public void SetImageRotation(short rotation)
    {
        Data.ImageRotation = Math.Clamp(
            rotation,
            CameraConsts.RotationMin,
            CameraConsts.RotationMax
        );
        _changes |= PortraitChanges.ImageRotation;
    }

    public byte GetCameraZoom()
    {
        return Data.CameraZoom;
    }

    // This is duplicative with the one in CustomCamera, but right now only
    // PortraitController is wired up to send changes to the default UI.
    public void SetCameraZoom(byte zoom)
    {
        Data.CameraZoom = Math.Clamp(zoom, CameraConsts.ZoomMin, CameraConsts.ZoomMax);
        _changes |= PortraitChanges.CameraZoom;
    }

    private static byte ClampBrightness(byte brightness)
    {
        // The dimmest you can make either light source is 20.
        return Math.Clamp(brightness, (byte)20, (byte)255);
    }

    // LB-type (non-emote) animations need some babying. They only
    // display right while the animation is unpaused... which makes
    // the visible state one frame ahead of the requested progress.
    // So we force-unpause it and play it a little behind instead.
    private static float CorrectionFactor(float fps)
    {
        if (UseManualCorrectionFactor)
        {
            return ManualCorrectionFactor;
        }

        // I fit a curve to this, after experimenting with the manual slider
        // in the debug window to determine best values at various FPS. This
        // cannot possibly be right but I can't guess what underlying cause
        // produces this curve.

        // 60 -> 1.25
        // 54 -> 1.33
        // 50 -> ~1.4
        // 45 -> ~1.52
        // 40 -> ~1.63
        // 30 -> 2.0
        // 20 -> 3.0
        // 10 -> 6.0

        var a = 0.9176475f;
        var d = 13.67424f;
        var c = 7.921653f;
        var b = 1.768327f;
        return a + (d - a) / (1 + MathF.Pow(fps / c, b));
    }
}

public enum PortraitChanges
{
    None = 0,
    AmbientLightColor = 1 << 1,
    DirectionalLightColor = 1 << 2,
    DirectionalLightDirection = 1 << 3,
    EyeDirection = 1 << 4,
    HeadDirection = 1 << 5,
    CameraPosition = 1 << 6,
    CameraTarget = 1 << 7,
    CameraZoom = 1 << 8,
    ImageRotation = 1 << 9,
    Pose = 1 << 10,
    Expression = 1 << 11,
    AnimationProgress = 1 << 12,
    BannerBg = 1 << 13,
}

internal static class Extensions
{
    internal static bool Take(ref this PortraitChanges target, PortraitChanges other)
    {
        var taken = (target & other) != 0;
        target &= ~other;
        return taken;
    }
}
