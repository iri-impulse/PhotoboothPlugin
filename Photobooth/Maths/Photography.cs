using System;

namespace Photobooth.Maths;

/// <summary>
/// Camera stuff having to do with real-world cameras.
/// </summary>
internal static class Photography
{
    public const float FullFrameX = 36f; // mm
    public const float FullFrameY = 24f; // mm

    // Field of view equation:
    // AFOV = 2 * atan(sensorSize / (2 * focalLength))

    public static float AFOV(float focalLength, float sensorSize = FullFrameX)
    {
        return 2 * MathF.Atan(sensorSize / (2 * focalLength));
    }

    public static float FocalLength(float fov, float sensorSize = FullFrameX)
    {
        return sensorSize / (2 * MathF.Tan(fov / 2));
    }
}
