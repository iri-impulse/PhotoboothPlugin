using System;
using System.Numerics;
using Photobooth.Maths;

namespace Photobooth.Controls;

/// <summary>
/// The camera system as controlled by our UI. It has 6 degrees of freedom,
/// because along with the camera position and orientation it also tracks
/// a target point along the line of sight.
/// </summary>
/// <remarks>
/// We represent this as an XYZ camera position, an XZ target point, and a
/// pitch, because that's more or less how we want to think of it for the UI.
/// </remarks>
internal class CustomCamera
{
    public static Vector2 TargetMinXZ { get; } =
        CameraConsts.PivotMin.XZ() - CameraConsts.DistanceMax * Vector2.One;
    public static Vector2 TargetMaxXZ { get; } =
        CameraConsts.PivotMax.XZ() + CameraConsts.DistanceMax * Vector2.One;

    public Vector3 Camera { get; private set; } = Vector3.Zero;
    public Vector2 TargetXZ { get; private set; } = Vector2.Zero;
    public float Pitch { get; private set; } = 0f;

    public Vector2 CameraXZ => Camera.XZ();

    public float DistanceXZ => Vector2.Distance(CameraXZ, TargetXZ);

    public float Yaw =>
        -MathF.Atan2(TargetXZ.Y - CameraXZ.Y, TargetXZ.X - CameraXZ.X) - MathF.PI / 2;

    // The notional target Y position, imputed by following the camera's line
    // of sight forward at the current pitch.
    public float TargetY => Camera.Y + DistanceXZ * MathF.Tan(Pitch);
    public Vector3 Target => new(TargetXZ.X, TargetY, TargetXZ.Y);

    public SphereLL Direction => SphereLL.FromRadians(Pitch, Yaw);

    public void SetCamera(Vector3 camera)
    {
        Camera = camera;
    }

    public void SetPitch(float pitch)
    {
        Pitch = pitch;
    }

    public void SetTargetXZ(Vector2 targetXZ)
    {
        // Enforce some limits here to make sure the target can't accidentally
        // escape (due to bugs, floating point issues, etc).
        TargetXZ = Vector2.Clamp(targetXZ, TargetMinXZ, TargetMaxXZ);
    }

    public void SetTargetViaYaw(float yaw)
    {
        // 0 is camera up, pi/2 is camera left, and we're controlling the
        // antipodal target here.
        var (s, c) = MathF.SinCos(-yaw - MathF.PI / 2);
        SetTargetXZ(CameraXZ + DistanceXZ * new Vector2(c, s));
    }

    public void Translate(Vector3 delta)
    {
        Camera += delta;
        SetTargetXZ(TargetXZ + delta.XZ());
    }
}
