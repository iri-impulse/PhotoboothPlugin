using System;
using System.Numerics;

namespace Photobooth.Maths;

internal static class Extensions
{
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.X, v.Z);
    }

    public static Vector2 Swap(this Vector2 v)
    {
        return new Vector2(v.Y, v.X);
    }

    public static float Atan2(this Vector2 v)
    {
        return MathF.Atan2(v.Y, v.X);
    }
}
