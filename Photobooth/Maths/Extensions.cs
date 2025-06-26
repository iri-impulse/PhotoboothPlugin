using System;
using System.Numerics;

namespace Photobooth.Maths;

internal static class Extensions
{
    /// <summary>
    /// Drop the Y coordinate from a Vector3, giving a point on the XZ plane.
    /// </summary>
    public static Vector2 XZ(this Vector3 v)
    {
        return new(v.X, v.Z);
    }

    /// <summary>
    /// Add a Y coordinate to a Vector2 representing a point on the XZ plane.
    /// </summary>
    public static Vector3 InsertY(this Vector2 v, float y)
    {
        return new(v.X, y, v.Y);
    }

    public static Vector2 Swap(this Vector2 v)
    {
        return new(v.Y, v.X);
    }

    public static float Atan2(this Vector2 v)
    {
        return MathF.Atan2(v.Y, v.X);
    }
}
