using System;

namespace Photobooth.Maths;

internal static class Photography
{
    public const float FullFrameX = 36f;
    public const float FullFrameY = 24f;

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
