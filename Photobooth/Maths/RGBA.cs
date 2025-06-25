using System;
using System.Numerics;

namespace Photobooth.Maths;

/// <summary>
/// A color represented as RGBA values in the range 0-255.
/// </summary>
public readonly record struct RGBA(byte R, byte G, byte B, byte A = 255)
{
    public RGBA(Vector4 vec)
        : this(FloatTo255(vec.X), FloatTo255(vec.Y), FloatTo255(vec.Z), FloatTo255(vec.W)) { }

    public static byte FloatTo255(float f)
    {
        return (byte)Math.Floor(Math.Clamp(f * 255f, 0f, 255f));
    }

    public readonly Vector4 ToVector4()
    {
        return new Vector4(R / 255f, G / 255f, B / 255f, A / 255f);
    }

    public override string ToString()
    {
        return $"RGBA({R}, {G}, {B}, {A})";
    }
}
