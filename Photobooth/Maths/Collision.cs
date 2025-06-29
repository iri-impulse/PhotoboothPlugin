using System;
using System.Numerics;

namespace Photobooth.Maths;

internal static class Collision
{
    private const float Epsilon = 1e-6f;

    /// <summary>
    /// A 1D line, defined by two points.
    /// </summary>
    public record struct Line(Vector3 A, Vector3 B)
    {
        public readonly Vector3 At(float t)
        {
            return A + t * (B - A);
        }

        public readonly Vector3 Closest(Vector3 point, out float t)
        {
            var ab = B - A;
            var ap = point - A;
            t = Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab);
            return A + t * ab;
        }

        public readonly bool Intersects(in Box box, out float t0, out float t1)
        {
            // Liang-Barsky algorithm for line-box intersection. We keep track
            // of how far along the line the first and second intersection
            // (starting from A) are, which is to say the segment of the line
            // that lies inside the box.
            t0 = float.MinValue;
            t1 = float.MaxValue;

            var d = B - A;

            var min = box.Min;
            var max = box.Max;

            // Go through each edge, considered as an oriented halfplane, and
            // "clip off" the part of the segment that's on the wrong side of
            // the halfplane.
            for (var edge = 0; edge < 6; edge++)
            {
                var (p, q) = edge switch
                {
                    0 => (-d.X, -(min.X - A.X)),
                    1 => (d.X, (max.X - A.X)),
                    2 => (-d.Y, -(min.Y - A.Y)),
                    3 => (d.Y, (max.Y - A.Y)),
                    4 => (-d.Z, -(min.Z - A.Z)),
                    5 => (d.Z, (max.Z - A.Z)),
                    _ => throw new NotImplementedException(),
                };

                var r = q / p;

                if (MathF.Abs(p) < Epsilon && q < 0)
                {
                    // Line is parallel to the edge and outside the box.
                    return false;
                }

                if (p < 0)
                {
                    t0 = Math.Max(t0, r);
                }
                else if (p > 0)
                {
                    t1 = Math.Min(t1, r);
                }
            }

            // If the segment hasn't been clipped away, it intersects the box.
            return t0 <= t1;
        }
    }

    /// <summary>
    /// A 1D circle (boundary only), defined by its center and radius.
    /// </summary>
    public record struct Circle(Vector2 C, float R)
    {
        public readonly Vector2 Closest(Vector2 point)
        {
            var direction = point - C;
            var distance = direction.Length();

            if (distance < Epsilon)
            {
                // We're equally close to every point, and have to choose.
                return C + R * Vector2.UnitX;
            }

            return C + direction * (R / distance);
        }
    }

    /// <summary>
    /// A 2D axis-aligned rectangle, defined by two opposite corners.
    /// </summary>
    public record struct Box(Vector3 Min, Vector3 Max)
    {
        public readonly bool Contains(Vector3 point)
        {
            return point.X >= Min.X
                && point.X <= Max.X
                && point.Y >= Min.Y
                && point.Y <= Max.Y
                && point.Z >= Min.Z
                && point.Z <= Max.Z;
        }
    }
}
