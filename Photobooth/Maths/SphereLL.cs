using System;
using System.Numerics;

namespace Photobooth.Maths;

/// <summary>
/// Latitude/longitude sphere representation.
/// </summary>
public record struct SphereLL
{
    private float _latitude;
    private float _longitude;

    public readonly float LatRadians => _latitude;
    public readonly float LonRadians => _longitude;
    public readonly Vector2 Radians => new(LatRadians, LonRadians);

    public readonly float LatDegrees => 360f * LatRadians / MathF.Tau;
    public readonly float LonDegrees => 360f * LonRadians / MathF.Tau;
    public readonly Vector2 Degrees => new(LatDegrees, LonDegrees);

    public readonly SphereLL Normalized()
    {
        // This isn't fully general but the portrait system only uses values
        // from -180 to +180 degrees (for both lat and lon).

        var antipodalLongitude = (LonDegrees + 360) % 360 - 180;

        if (LatDegrees < -90)
        {
            return FromDegrees(-180 - LatDegrees, antipodalLongitude);
        }
        else if (LatDegrees > 90)
        {
            return FromDegrees(180 - LatDegrees, antipodalLongitude);
        }
        else
        {
            return this;
        }
    }

    public void SetDegrees(float lat, float lon)
    {
        _latitude = MathF.Tau * lat / 360f;
        _longitude = MathF.Tau * lon / 360f;
    }

    public void SetRadians(float lat, float lon)
    {
        _latitude = lat;
        _longitude = lon;
    }

    public SphereLL()
    {
        _latitude = 0f;
        _longitude = 0f;
    }

    private SphereLL(float lat, float lon)
    {
        _latitude = lat;
        _longitude = lon;
        //Normalize();
    }

    public static SphereLL FromDegrees(float lat, float lon)
    {
        return new SphereLL(MathF.Tau * lat / 360f, MathF.Tau * lon / 360f);
    }

    public static SphereLL FromRadians(float lat, float lon)
    {
        return new SphereLL(lat, lon);
    }

    public static SphereLL FromDirection(Vector3 direction)
    {
        direction = Vector3.Normalize(direction);
        var lat = -MathF.Asin(direction.Y);
        var lon = MathF.Atan2(direction.X, direction.Z);

        return new SphereLL(lat, lon);
    }

    public readonly Vector3 Direction()
    {
        return new Vector3(
            MathF.Cos(LatRadians) * MathF.Sin(LonRadians),
            -MathF.Sin(LatRadians),
            MathF.Cos(LatRadians) * MathF.Cos(LonRadians)
        );
    }

    public override readonly string ToString()
    {
        return $"SphereLL(Lat: {LatDegrees}°, Lon: {LonDegrees}°)";
    }
}
