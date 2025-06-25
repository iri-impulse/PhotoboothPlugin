using System;
using System.Numerics;
using PortraitTweaks.Maths;
using PortraitTweaks.Maths.Collision;
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
internal class CameraController
{
    // The focus position. Ordinarily on the character, though can be moved.
    // This concept doesn't exist at all in the original portrait editor.
    public Vector3 Subject { get; private set; } = new();

    public BuiltinCamera Builtin { get; } = new();

    public CustomCamera Custom { get; } = new();

    // The camera position. Actually not saved in portraits (!) but it's
    // one of the values we want to think in terms of for the UX.
    public Vector3 Camera => Builtin.Camera;

    // This pivot position that controls the default UI. Always directly in
    // front of the camera. Dragging on the portrait pivots around it (left
    // click) or moves it left/right/up/down (right click).
    //
    // We have to keep track of it in part to keep the original UI working, in
    // part because it's what's saved in the portrait, and in part because it
    // has a constrained legal range so we can only allow camera positions that
    // correspond to some allowed pivot point.
    public Vector3 Pivot => Builtin.Pivot;

    public Vector2 TargetXZ => Custom.TargetXZ;

    // The direction of the camera, relative to the pivot point. This is what
    // left-click drag in the default UI modifies. It's saved in portraits, and
    // can't be tilted too far up or down but can rotate anywhere horizontally.
    public SphereLL Direction => Builtin.Direction;

    // The distance from the camera to the pivot point, which you change with
    // the mousewheel in the default UI. Saved in portraits, and constrained to
    // a legal range.
    public float Distance => Builtin.Distance;

    // The "zoom factor" from 0 to 200 controlled by the zoom slider in the UI.
    public byte Zoom => Builtin.Zoom;

    // The "normalized zoom", this is the angle in radians from the center to
    // the far edge of the image (so, half the field of view).
    public float ZoomRadians => Builtin.FoV;

    public static Collision.Box PivotBoxXZ = new(
        new(PivotMin.X, PivotMin.Z),
        new(PivotMax.X, PivotMax.Z)
    );

    public unsafe void Load(Editor e)
    {
        var portrait = e.Portrait;
        var pos = ((Vector4)portrait->CameraPosition).AsVector3();
        var tar = ((Vector4)portrait->CameraTarget).AsVector3();

        var pivot = tar * Scale;
        var pitch = portrait->CameraPitch;
        var yaw = portrait->CameraYaw;
        var direction = SphereLL.FromRadians(pitch, yaw);

        Builtin.SetDirection(direction);
        Builtin.SetPivot(pivot);
        Builtin.SetZoom(portrait->CameraZoom);
        Builtin.SetDistance(portrait->CameraDistance * Scale);

        RecomputeCustom();
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

    public void Reset()
    {
        Subject = Vector3.Zero;
    }

    public void SetZoom(byte zoom)
    {
        Builtin.SetZoom(zoom);
    }

    public void AdjustCameraDistance(float delta)
    {
        // As with Client::UI::Misc::CharaViewPortrait.SetCameraDistance, but
        // we want to do it without necessarily changing the real camera yet.
        var increment = DistanceIncrement / 1000f;
        var zoomFactor = MathF.Tan(ZoomRadians * 0.5f);
        var normalizedDistance = zoomFactor * Distance;
        var update = 2 * normalizedDistance * increment * delta;
        Builtin.SetDistance(Builtin.Distance + update);
        RecomputeCustom();
    }

    public void Translate(Vector3 delta)
    {
        var displacement = Builtin.TryTranslate(delta);
        Custom.Translate(displacement);
    }

    /// <summary>
    /// Moves the camera towards a specified position, reaching it exactly if
    /// preserveDistance is false or coming as close as possible if true.
    /// </summary>
    public void SetCameraPositionXZ(Vector2 cameraXZ, bool preserveDistance = false)
    {
        if (preserveDistance)
        {
            var distance = Vector2.Distance(TargetXZ, Custom.CameraXZ);
            cameraXZ = new Collision.Circle(TargetXZ, distance).Closest(cameraXZ);
        }

        var camera = new Vector3(cameraXZ.X, Camera.Y, cameraXZ.Y);
        Custom.SetCamera(camera);
        RecomputeBuiltin(true);
        RecomputeCustom();
    }

    /// <summary>
    /// Moves the target towards a specified position, reaching it exactly if
    /// preserveDistance is false or coming as close as possible if true.
    /// </summary>
    public void SetTargetPositionXZ(Vector2 targetXZ, bool preserveDistance = false)
    {
        if (preserveDistance)
        {
            var distance = Vector2.Distance(TargetXZ, Custom.CameraXZ);
            targetXZ = new Collision.Circle(Custom.CameraXZ, distance).Closest(targetXZ);
        }

        Custom.SetTargetXZ(targetXZ);
        RecomputeBuiltin();
        RecomputeCustom();
    }

    public void SetSubjectPosition(Vector3 newSubject)
    {
        Subject = newSubject;
    }

    /// <summary>
    /// Move the target onto the subject (or as close as possible), move the
    /// camera away from the target if it's too close, and recompute the pivot.
    /// </summary>
    public void FaceSubject()
    {
        // Right now we're not adjusting the camera vertical angle. This is
        // alright because you can adjust it manually, even while camera
        // following is active, but it's not completely ideal.
        Custom.SetTargetXZ(Subject.XZ());
        RecomputeBuiltin(true);
        RecomputeCustom();
    }

    public void SetPivotPositionXZ(Vector2 pivotXZ)
    {
        var newPivot = new Vector3(pivotXZ.X, Pivot.Y, pivotXZ.Y);
        newPivot = Vector3.Clamp(newPivot, PivotMin, PivotMax);
        Builtin.SetPivot(newPivot);
    }

    public void RotatePivotPositionXZ(Vector2 pivotXZ)
    {
        var oldAngle = (Subject.XZ() - Pivot.XZ()).Atan2();
        var newAngle = (Subject.XZ() - pivotXZ).Atan2();
        var theta = newAngle - oldAngle;
        var rotation = Matrix3x2.CreateRotation(theta, Subject.XZ());

        var newPivotXZ = Vector2.Transform(Pivot.XZ(), rotation);
        if (!PivotBoxXZ.Contains(newPivotXZ))
        {
            return;
        }

        var newCameraXZ = Vector2.Transform(Custom.CameraXZ, rotation);
        var newTargetXZ = Vector2.Transform(TargetXZ, rotation);

        var newCamera = new Vector3(newCameraXZ.X, Camera.Y, newCameraXZ.Y);
        Custom.SetCamera(newCamera);
        Custom.SetTargetXZ(newTargetXZ);

        RecomputeBuiltin();
        RecomputeCustom();
    }

    public void SetPivotPositionY(float pivotY)
    {
        var newPivot = new Vector3(Pivot.X, pivotY, Pivot.Z);
        Builtin.SetPivot(newPivot);
    }

    public void SetCameraPitchRadians(float newPitch)
    {
        var newDirection = SphereLL.FromRadians(newPitch, Direction.LonRadians);
        Builtin.SetDirection(newDirection);
        Custom.SetPitch(newPitch);
    }

    /// <summary>
    /// Recompute the custom camera position to match the state of the built-in
    /// camera. The distance between the camera and the target is preserved,
    /// but the camera position and the direction to the target are updated.
    /// </summary>
    private void RecomputeCustom()
    {
        Custom.SetCamera(Builtin.Camera);
        Custom.SetPitch(Builtin.Direction.LatRadians);
        Custom.SetTargetViaYaw(Builtin.Direction.LonRadians);
    }

    /// <summary>
    /// Recompute the pivot position based on our custom coordinate system.
    /// </summary>
    /// <remarks>
    /// This is where the magic happens. Our UI works in terms of a camera and
    /// target position, ignoring the existence of the pivot. To use it as a
    /// source of truth we want a unidirectional data flow where we come up
    /// with a pivot position without consulting the built-in camera at all.
    ///
    /// The result is a legal built-in camera position that's as close as we
    /// can manage to the custom camera, but not necessarily identical to it.
    /// </remarks>
    private void RecomputeBuiltin(bool allowCameraMovement = false)
    {
        // Move the pivot to the spot in the line of sight closest to the
        // subject, respecting limits of camera distance and position.
        var cameraXZ = Custom.Camera.XZ();
        var pivotXZ = Pivot.XZ();
        var targetXZ = Custom.TargetXZ;
        var subjectXZ = Subject.XZ();

        var line = new Collision.Line(cameraXZ, targetXZ);
        var lineLength = Vector2.Distance(cameraXZ, targetXZ);

        if (lineLength < 0.001f)
        {
            return;
        }

        if (!line.Intersects(PivotBoxXZ, out var near, out var far))
        {
            return;
        }

        line.Closest(subjectXZ, out var t);

        var distanceScale = MathF.Cos(Custom.Pitch);
        var minDistance = DistanceMin * distanceScale / lineLength;
        var maxDistance = DistanceMax * distanceScale / lineLength;

        // We now have an interval of legal values based on the camera distance
        // and an interval of legal values based on the bounding box. But these
        // might not overlap, in which case there's no feasible pivot to give
        // rise to the original camera position.
        //
        // In these cases we send the pivot to the edge of the box, which ends
        // up pushing or pulling the camera to be back within distance.

        float min;
        float max;

        if (near > maxDistance)
        {
            if (!allowCameraMovement)
                return;
            min = max = near;
        }
        else if (far < minDistance)
        {
            if (!allowCameraMovement)
                return;
            min = max = far;
        }
        else
        {
            min = Math.Max(near, minDistance);
            max = Math.Min(far, maxDistance);
        }

        var newPivotXZ = line.At(Math.Clamp(t, min, max));
        var newPivot = new Vector3(newPivotXZ.X, Pivot.Y, newPivotXZ.Y);
        var newDistance = Vector2.Distance(newPivotXZ, cameraXZ) / distanceScale;

        Builtin.SetPivot(newPivot);
        Builtin.SetDirection(Custom.Direction);
        Builtin.SetDistance(newDistance);
    }
}
