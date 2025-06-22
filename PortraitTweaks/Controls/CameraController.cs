using System;
using System.Numerics;
using PortraitTweaks.Data;
using static PortraitTweaks.Controls.CameraConsts;

namespace PortraitTweaks.Controls;

// Camera system notes:

// SetCameraYawAndPitch
// #3 = 5, "angle increment" for yaw and pitch (divided by 1000; radians)
// #10 = -89, "min degrees" of pitch
// #11 = 30, "max degrees" of pitch

// SetCameraDistance
// #4 = 50, "distance increment", effect depends on normalized camera zoom
// #8 = 50, "min distance" (divided by 100)
// #9 = 200, "max distance" (divided by 100)

// SetCameraXAndY (here be dragons)
// #2 = 1, "orbit increment", effect depends on normalized camera zoom
// #12 = -160, #15 = 160, "x pivot bounds"
// #13 = -100, #16 = 50, "y pivot bounds"
// #14 = -50, #17 = 50, "z pivot bounds"
// These all bound the camera _pivot_, and then the camera position is
// recomputed in terms of the pivot, yaw, pitch, and distance.

// SetCameraZoom
// Hardcoded to be 0 < Z < 200, as displayed in base UI.
// CameraZoomNormalized computed from this.

// The built-in portrait camera operates based on two points, the "camera"
// position and the "pivot" position. The camera always faces the pivot,
// always orbits around the pivot when turning left/right/up/down, and moves
// closer or farther away from the pivot when you use the mousewheel to
// traverse in and out.
//
// The constraints imposed on this setup are:
// - A min and max distance between the camera and the pivot;
// - A bounding box within which the pivot must stay;
// - A min and max pitch angle of the camera relative to the pivot.
//
// Thus, limitations on the position of the camera are only _indirectly_ about
// the camera. A given camera position and angle is reachable as long as there
// exists an in-bounds pivot position at a legal distance from the camera.

/// <summary>
/// Allows manipulation of the camera in the portrait editor, while keeping
/// everything legal and in bounds.
/// </summary>
public class CameraController
{
    // The camera position. Actually not saved in portraits (!) but it's
    // one of the values we want to think in terms of for the UX.
    public Vector3 Camera { get; private set; } = new();

    // The focus position. Ordinarily on the character, though can be moved.
    // This concept doesn't exist at all in the original portrait editor.
    public Vector3 Subject { get; private set; } = new();

    // This pivot position that controls the default UI. Always directly in
    // front of the camera. Dragging on the portrait pivots around it (left
    // click) or moves it left/right/up/down (right click).
    //
    // We have to keep track of it in part to keep the original UI working, in
    // part because it's what's saved in the portrait, and in part because it
    // has a constrained legal range so we can only allow camera positions that
    // correspond to some allowed pivot point.
    public Vector3 Pivot { get; private set; } = new();

    // The direction of the camera, relative to the pivot point. This is what
    // left-click drag in the default UI modifies. It's saved in portraits, and
    // can't be tilted too far up or down but can rotate anywhere horizontally.
    public SphereLL Direction { get; private set; } = new();

    // The distance from the camera to the pivot point, which you change with
    // the mousewheel in the default UI. Saved in portraits, and constrained to
    // a legal range.
    public float Distance { get; private set; } = 0.0f;

    // Distance from the camera to the pivot point in the XZ plane. Useful for
    // bird's eye view controls.
    public float DistanceXZ => MathF.Sqrt(Camera.X * Camera.X + Camera.Z * Camera.Z);

    // The "zoom factor" from 0 to 200 controlled by the zoom slider in the UI.
    public byte Zoom { get; private set; } = 0;

    // The "normalized zoom", this is the angle in radians from the center to
    // the far edge of the image (so, half the field of view).
    public float ZoomRadians => 1.28f - (float)Zoom / 200f;

    public unsafe void Load(Editor e)
    {
        var portrait = e.Portrait;
        var pos = ((Vector4)portrait->CameraPosition).AsVector3();
        var tar = ((Vector4)portrait->CameraTarget).AsVector3();

        Camera = pos * Scale;
        Pivot = tar * Scale;
        Direction = SphereLL.FromRadians(portrait->CameraPitch, portrait->CameraYaw);
        Distance = portrait->CameraDistance * Scale;
        Zoom = portrait->CameraZoom;
    }

    public unsafe void Save(Editor e)
    {
        var portrait = e.Portrait;

        portrait->CameraTarget.X = Pivot.X / Scale;
        portrait->CameraTarget.Y = Pivot.Y / Scale;
        portrait->CameraTarget.Z = Pivot.Z / Scale;

        portrait->CameraDistance = Distance / Scale;
        portrait->SetCameraZoom(Zoom);

        portrait->CameraPitch = Direction.LatRadians;
        portrait->CameraYaw = Direction.LonRadians;

        portrait->CameraPosition.X = Camera.X / Scale;
        portrait->CameraPosition.Y = Camera.Y / Scale;
        portrait->CameraPosition.Z = Camera.Z / Scale;

        portrait->ApplyCameraPositions();
    }

    public void SetZoom(byte zoom)
    {
        Zoom = Math.Clamp(zoom, (byte)0, (byte)200);
    }

    public void SetCameraDistance(float distance)
    {
        Distance = Math.Clamp(distance, DistanceMin, DistanceMax);
        RecalculateCameraFromPivot();
    }

    public void AdjustCameraDistance(float delta)
    {
        // As with Client::UI::Misc::CharaViewPortrait.SetCameraDistance, but
        // we want to do it without necessarily changing the real camera yet.
        var increment = DistanceIncrement / 1000f;
        var zoomFactor = MathF.Tan(ZoomRadians * 0.5f);
        var normalizedDistance = zoomFactor * Distance;
        var update = 2 * normalizedDistance * increment * delta;
        Distance = Math.Clamp(Distance + update, DistanceMin, DistanceMax);
        RecalculateCameraFromPivot();
    }

    public void AdjustPivotPosition(Vector3 delta)
    {
        Pivot = Vector3.Clamp(Pivot + delta, PivotMin, PivotMax);
        RecalculateCameraFromPivot();
    }

    public void SetCameraPosition(Vector3 newCamera, bool preserveCameraDistance = false)
    {
        var newDistance = preserveCameraDistance ? Distance : (newCamera - Pivot).Length();
        newDistance = Math.Clamp(newDistance, DistanceMin, DistanceMax);

        var newDirection = SphereLL.FromDirection(newCamera - Pivot);
        var latitude = Math.Clamp(newDirection.LatRadians, PitchMin, PitchMax);
        newDirection = SphereLL.FromRadians(latitude, newDirection.LonRadians);

        Direction = newDirection;
        Distance = newDistance;
        RecalculateCameraFromPivot();
    }

    // Moves the camera to a place with a given projection in the XZ plane,
    // by setting the camera yaw and distance.
    public void SetCameraPositionXZ(Vector2 cameraXZ)
    {
        var dXZ = cameraXZ - new Vector2(Pivot.X, Pivot.Z);
        var newYaw = MathF.Atan2(dXZ.X, dXZ.Y);
        SetCameraYawRadians(newYaw);
        var newDist = dXZ.Length() / MathF.Abs(MathF.Cos(Direction.LatRadians));
        SetCameraDistance(newDist);
    }

    public void SetSubjectPosition(Vector3 newSubject, bool preserveCharacterAngle = false)
    {
        if (preserveCharacterAngle)
        {
            var oldDirection = SphereLL.FromDirection(Pivot - Subject);
            var newDirection = SphereLL.FromDirection(Pivot - newSubject);

            Direction = SphereLL.FromRadians(
                Direction.LatRadians + newDirection.LatRadians - oldDirection.LatRadians,
                Direction.LonRadians + newDirection.LonRadians - oldDirection.LonRadians
            );
        }

        Subject = newSubject;
        RecalculateCameraFromPivot();
    }

    // Move the pivot onto the subject (or as close as possible), and face the
    // camera at the subject.
    public void FaceSubject()
    {
        // New pivot point, corrected to be in bounds.
        var newPivot = new Vector3(Subject.X, Pivot.Y, Subject.Z);

        SphereLL newDirection;
        if (
            newPivot.X < PivotMin.X
            || newPivot.X > PivotMax.X
            || newPivot.Z < PivotMin.Z
            || newPivot.Z > PivotMax.Z
        )
        {
            // If the subject is outside the bounds, we want to move the camera
            // and face the direction between hte pivot and the subject.
            Pivot = Vector3.Clamp(newPivot, PivotMin, PivotMax);
            newDirection = SphereLL.FromDirection(Pivot - Subject);
        }
        else
        {
            // Otherwise, we face the current camera onto the pivot.
            Pivot = newPivot;
            newDirection = SphereLL.FromDirection(Camera - Pivot);
        }

        Direction = SphereLL.FromRadians(
            Math.Clamp(newDirection.LatRadians, PitchMin, PitchMax),
            newDirection.LonRadians
        );

        // Final camera position.
        RecalculateCameraFromPivot();
    }

    public void SetPivotPositionXZ(Vector2 pivotXZ, bool preserveCharacterAngle = false)
    {
        var newPivot = new Vector3(pivotXZ.X, Pivot.Y, pivotXZ.Y);
        newPivot = Vector3.Clamp(newPivot, PivotMin, PivotMax);

        if (preserveCharacterAngle)
        {
            var oldAngle = MathF.Atan2(Pivot.X - Subject.X, Pivot.Z - Subject.Z);
            var newAngle = MathF.Atan2(newPivot.X - Subject.X, newPivot.Z - Subject.Z);

            Direction = SphereLL.FromRadians(
                Direction.LatRadians,
                Direction.LonRadians + newAngle - oldAngle
            );
        }

        Pivot = newPivot;
        RecalculateCameraFromPivot();
    }

    public void SetPivotPositionY(float pivotY, bool preserveCharacterAngle = false)
    {
        var newPivot = new Vector3(Pivot.X, pivotY, Pivot.Z);

        Pivot = newPivot;
        RecalculateCameraFromPivot();
    }

    public void SetCameraYawRadians(float newYaw)
    {
        Direction = SphereLL.FromRadians(Direction.LatRadians, newYaw);
        RecalculateCameraFromPivot();
    }

    public void SetCameraPitchRadians(float newPitch)
    {
        Direction = SphereLL.FromRadians(newPitch, Direction.LonRadians);
        RecalculateCameraFromPivot();
    }

    private void RecalculateCameraFromPivot()
    {
        var displacement = Direction.Direction() * Distance;
        Camera = Pivot + displacement;
    }
}
