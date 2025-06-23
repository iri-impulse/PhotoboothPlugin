using System;
using System.Numerics;
using PortraitTweaks.Maths;
using static PortraitTweaks.Controls.CameraConsts;

namespace PortraitTweaks.Controls;

/// <summary>
/// A representation of the built-in portrait camera, which represents only
/// valid states.
/// </summary>
/// <remarks>
/// All boundary values are _independent_, meaning you can set parameters in
/// any order and end up with the same result.
/// </remarks>
internal class BuiltinCamera
{
    public byte Zoom { get; private set; } = ZoomMin;
    public float Twist { get; private set; } = 0;
    public Vector3 Pivot { get; private set; } = Vector3.Zero;
    public float Distance { get; private set; } = DistanceMin;
    public SphereLL Direction { get; private set; } = SphereLL.FromDegrees(0, 0);

    public float FoV => 1.28f - (float)Zoom / 200f;

    public Vector3 Camera => Pivot + Distance * Direction.Direction();

    public void SetZoom(byte zoom)
    {
        Zoom = Math.Clamp(zoom, ZoomMin, ZoomMax);
    }

    public void SetDistance(float distance)
    {
        Distance = Math.Clamp(distance, DistanceMin, DistanceMax);
    }

    public void SetDirection(SphereLL direction)
    {
        var pitch = Math.Clamp(direction.LatRadians, PitchMin, PitchMax);
        Direction = SphereLL.FromRadians(pitch, direction.LonRadians);
    }

    public void SetPivot(Vector3 pivot)
    {
        Pivot = Vector3.Clamp(pivot, PivotMin, PivotMax);
    }

    public Vector3 TryTranslate(Vector3 delta)
    {
        var newPivot = Vector3.Clamp(Pivot + delta, PivotMin, PivotMax);
        var displacement = newPivot - Pivot;

        Pivot = newPivot;
        return displacement;
    }
}
