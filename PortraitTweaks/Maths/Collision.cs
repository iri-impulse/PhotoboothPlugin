using System;
using System.Numerics;

namespace PortraitTweaks.Maths.Collision;

internal static class Collision
{
    private const float Epsilon = 1e-6f;

    private static Vector2 UnitVector(float angle)
    {
        var (s, c) = MathF.SinCos(angle);
        return new Vector2(s, c);
    }

    private static float NormalizeAngle(float angle)
    {
        var t = (angle % MathF.Tau + MathF.Tau) % MathF.Tau;
        return t > MathF.PI ? t - MathF.Tau : t;
    }

    /// <summary>
    /// A 1D line, defined by two points.
    /// </summary>
    public record struct Line(Vector2 A, Vector2 B)
    {
        public readonly Vector2 At(float t)
        {
            return A + t * (B - A);
        }

        public readonly Vector2 Closest(Vector2 point, out float t)
        {
            var ab = B - A;
            var ap = point - A;
            t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
            return A + t * ab;
        }
    }

    /// <summary>
    /// A 1D line segment, defined by two endpoints.
    /// </summary>
    public record struct Segment(Vector2 A, Vector2 B)
    {
        /// <summary>
        /// Parameterize the segment as a map from [0, 1] to the plane and get
        /// the point corresponding to a parameter t in that range.
        /// </summary>
        public readonly Vector2 At(float t)
        {
            return A + t * (B - A);
        }

        public readonly Vector2 Closest(Vector2 point, out float t)
        {
            // Calculate the projection of the AP segment onto AB.
            var ab = B - A;
            var ap = point - A;
            t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
            // Clamp to the segment bounds.
            t = Math.Clamp(t, 0f, 1f);
            return At(t);
        }

        public readonly float Length()
        {
            return Vector2.Distance(A, B);
        }

        public Segment Restrict(float t, float s)
        {
            return new Segment(At(t), At(s));
        }

        /// <summary>
        /// Check if this segment intersects with an axis-aligned box, and
        /// compute the distance along the segment to the intersection points.
        /// </summary>
        public readonly bool Intersects(in Box box, out float t0, out float t1)
        {
            // Liang-Barsky algorithm for line-box intersection. We keep track
            // of how far along the segment the first and second intersection
            // (starting from A) are, which is to say the subsegment of the
            // line that lies inside the box.
            t0 = 0f;
            t1 = 1f;

            var d = B - A;

            var min = box.Min;
            var max = box.Max;

            // Go through each edge, considered as an oriented halfplane, and
            // "clip off" the part of the segment that's on the wrong side of
            // the halfplane.
            for (var edge = 0; edge < 4; edge++)
            {
                var (p, q) = edge switch
                {
                    0 => (-d.X, -(min.X - A.X)),
                    1 => (d.X, (max.X - A.X)),
                    2 => (-d.Y, -(min.Y - A.Y)),
                    3 => (d.Y, (max.Y - A.Y)),
                    _ => throw new NotImplementedException(),
                };

                var r = q / p;

                if (MathF.Abs(p) < Epsilon && q < 0)
                {
                    // Segment is parallel to the edge and outside the box.
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
        public readonly Vector2 At(float angle)
        {
            return C + R * UnitVector(angle);
        }

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
    /// A 1D arc, defined by its center, radius, and start/end angles.
    /// </summary>
    public record struct Arc(Vector2 C, float R, float StartAngle, float EndAngle)
    {
        public readonly Vector2 Closest(Vector2 point, out float angle)
        {
            // Calculate the angle of the point relative to the center
            var direction = point - C;

            var start = NormalizeAngle(StartAngle);
            var end = NormalizeAngle(EndAngle);

            if (direction.Length() < Epsilon)
            {
                // If the point is at the center, return a point on the arc.
                angle = (start + end) / 2;
            }
            else
            {
                (start, end) = start < end ? (start, end) : (end, start);
                angle = Math.Clamp(direction.Atan2(), start, end);
            }

            return C + R * UnitVector(angle);
        }
    };

    /// <summary>
    /// A 2D disk (filled circle), defined by its center and radius.
    /// </summary>
    public record struct Disk(Vector2 C, float R)
    {
        public readonly Vector2 Closest(Vector2 point, out bool inside)
        {
            var direction = point - C;
            var distance = direction.Length();

            inside = distance <= R;
            return inside ? point : C + direction * (R / distance);
        }
    };

    /// <summary>
    /// A 2D sector (pie slice), defined by its center, radius, and start/end angles.
    /// </summary>
    public record struct Sector(Vector2 C, float r, float StartAngle, float EndAngle) { };

    /// <summary>
    /// A 2D axis-aligned rectangle, defined by two opposite corners.
    /// </summary>
    public record struct Box(Vector2 Min, Vector2 Max)
    {
        public readonly bool Contains(Vector2 point)
        {
            return point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;
        }

        public readonly Vector2 Closest(Vector2 point, out bool inside)
        {
            inside = Contains(point);
            return inside ? point : Vector2.Clamp(point, Min, Max);
        }
    }
}
