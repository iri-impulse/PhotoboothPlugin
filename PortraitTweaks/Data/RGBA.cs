using System;
using System.Numerics;

namespace PortraitTweaks.Data;

/// <summary>
/// A color represented as RGBA values in the range 0-255.
/// </summary>
public readonly struct RGBA
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public RGBA(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public RGBA(Vector4 vec)
    {
        R = (byte)Math.Floor(Math.Clamp(vec.X * 255f, 0f, 255f));
        G = (byte)Math.Floor(Math.Clamp(vec.Y * 255f, 0f, 255f));
        B = (byte)Math.Floor(Math.Clamp(vec.Z * 255f, 0f, 255f));
        A = (byte)Math.Floor(Math.Clamp(vec.W * 255f, 0f, 255f));
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
