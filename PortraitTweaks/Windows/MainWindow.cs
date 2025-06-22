using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using PortraitTweaks.Controls;
using PortraitTweaks.Data;
using PortraitTweaks.UI;
using static PortraitTweaks.Configuration;

namespace PortraitTweaks.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private readonly PortraitController _portrait = new(new());
    private readonly CameraController _camera = new();

    private bool _followCharacter = false;

    // The temporary/stateful attachment side of the window, so "auto" doesn't
    // flip sides when moving the banner editor unless there's no more space.
    private WindowAttachment _attachment = WindowAttachment.Right;

    // For debouncing, since durations take a moment to stabilize.
    private float _lastDuration = 1f;
    private int _lastPose = -1;

    // The part of the body to face when facing/tracking the character.
    private static readonly ushort _Part = 6;

    public MainWindow(Plugin plugin)
        : base($"{Plugin.PluginName}##{Plugin.PluginName}_mainwindow")
    {
        _plugin = plugin;

        Flags =
            ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(375, 200),
            MaximumSize = new(600, float.MaxValue),
        };
    }

    public void Dispose()
    {
        _portrait.Dispose();
    }

    public override void OnOpen()
    {
        _lastPose = -1;
        _lastDuration = -1f;
        _followCharacter = false;
    }

    private unsafe void SnapToBannerEditor()
    {
        if (_plugin.Configuration.AttachWindow is not WindowAttachment setting)
            return;

        var editor = (AddonBannerEditor*)Plugin.GameGui.GetAddonByName("BannerEditor");
        if (editor == null)
            return;
        var col = editor->RootNode;
        if (col == null)
            return;
        var device = Device.Instance();
        if (device == null)
            return;

        var top = col->GetYFloat();
        var left = col->GetXFloat();
        var editorWidth = col->GetWidth();
        var screenWidth = device->Width;
        var windowWidth = ImGui.GetWindowSize().X;

        var rightSpace = screenWidth - (left + editorWidth);
        var fitsOnLeft = left > windowWidth + 10;
        var fitsOnRight = rightSpace > windowWidth + 10;

        // The hysteresis approach here is:
        // - use the setting if it's definitive
        // - if only one side fits, use that side
        // - otherwise stay on the side we were.

        _attachment = setting switch
        {
            WindowAttachment.Auto => (fitsOnLeft, fitsOnRight) switch
            {
                (true, false) => WindowAttachment.Left,
                (false, true) => WindowAttachment.Right,
                _ => _attachment,
            },
            _ => setting,
        };

        var x = _attachment switch
        {
            WindowAttachment.Left => left - windowWidth - 10,
            _ => left + editorWidth + 10,
        };

        Position = new Vector2(x, top);
    }

    public override void Draw()
    {
        SnapToBannerEditor();

        var e = Editor.Current();

        if (e.IsValid)
        {
            _portrait.CopyData(e);

            SectionLights();
            SectionAnimation(e);
            SectionCamera(e);
            SectionFacing(e);
        }
        else
        {
            SectionUnopened();
        }

        var height = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
        ImGui.SetWindowSize(new(ImGui.GetWindowSize().X, height));
    }

    private unsafe void SectionUnopened()
    {
        ImGui.TextUnformatted("Create or edit a portrait to get started.");
        ImGui.Spacing();

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Portrait, "Portrait List"))
        {
            UIModule.Instance()->ExecuteMainCommand(92);
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.AddressCard, "Adventurer Plate"))
        {
            UIModule.Instance()->ExecuteMainCommand(93);
        }
    }

    private void SectionLights()
    {
        ImPT.IconHeader("Lighting", FontAwesomeIcon.Lightbulb);

        var colorFlags =
            ImGuiColorEditFlags.AlphaBar
            | ImGuiColorEditFlags.NoTooltip
            | ImGuiColorEditFlags.NoOptions
            | ImGuiColorEditFlags.NoInputs;

        var startX = ImGui.GetCursorPosX();
        var entireWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            var ambientVec4 = _portrait.GetAmbientLightColor().ToVector4();
            if (ImGui.ColorEdit4("Ambient##_picker", ref ambientVec4, colorFlags))
            {
                _portrait.SetAmbientLightColor(new RGBA(ambientVec4));
            }

            var directional = _portrait.GetDirectionalLightColor().ToVector4();
            if (ImGui.ColorEdit4("Directional##_picker", ref directional, colorFlags))
            {
                _portrait.SetDirectionalLightColor(new RGBA(directional));
            }
        }

        ImGui.SameLine();
        var leftWidth = Math.Max(ImGui.GetCursorPosX(), startX + 0.3f * entireWidth);
        ImGui.SameLine(leftWidth);
        using (ImRaii.Group())
        {
            using var width = ImRaii.ItemWidth(-float.Epsilon);

            var lightDir = _portrait.GetDirectionalLightDirection();
            if (ImPT.Direction("##lightDirection", ref lightDir, -180, 180, -90, 90))
            {
                _portrait.SetDirectionalLightDirection(lightDir);
            }
        }
    }

    private void SectionCamera(Editor e)
    {
        ImPT.IconHeader(
            "Camera",
            FontAwesomeIcon.Camera,
            "Left click: drag a handle to rotate the camera (red) or pivot point (white).\nRight click: drag anywhere to slide camera and pivot together.\nMousewheel: adjust camera distance."
        );

        var changed = false;

        // TODO move this out of draw
        // Load manual camera changes and perhaps track moving character.
        _camera.Load(e);

        // Track moving character.
        var newSubject = e.CharacterPosition(_Part);
        if (_portrait.IsAnimationStable)
        {
            _camera.SetSubjectPosition(newSubject, _followCharacter);
            changed |= _followCharacter;
        }

        // Camera canvas area.
        changed |= CameraViewport(e);

        changed |= CameraWidgets();

        if (changed)
        {
            _camera.Save(e);
            e.SetHasChanged(true);
        }
    }

    private bool CameraViewport(Editor e)
    {
        var changed = false;

        // Tweakable stuff.
        var boundaryColor = 0xFFA0A0A0u;
        var wedgeColor = 0x606060FFu;
        var characterColor = 0xFF00EEEEu;
        var cameraColor = 0xFF8080FFu;
        var pivotColor = 0xFFE0E0E0u;
        var sunColor = 0xFF00EEEEu;
        var sunActiveColor = 0xFF44FFFFu;

        var handleSize = 10f;
        var playerSize = 16f;
        var sunSize = 8f;
        var wedgeLen = 300f;
        var lightRadius = 230f;

        var maxD = CameraConsts.DistanceMax;
        var min = CameraConsts.PivotMin;
        var max = CameraConsts.PivotMax;
        var minPitch = CameraConsts.PitchMin;
        var maxPitch = CameraConsts.PitchMax;

        // Dimensions of the camera box, which other elements are relative to.
        var xSpan = max.X - min.X + 2 * maxD;
        var zSpan = max.Z - min.Z + 2 * maxD;
        var aspectRatio = zSpan / xSpan;

        // Draw and respond to controls.
        ImGui.SetNextItemWidth(-float.Epsilon);
        var w = ImGui.CalcItemWidth();
        var h = w * zSpan / xSpan;
        ImGeo.BeginViewport(
            "##CameraViewport",
            new Vector2(min.X - maxD, min.Z - maxD),
            new Vector2(max.X + maxD, max.Z + maxD),
            new Vector2(w, w * zSpan / xSpan)
        );
        var pixels = ImGeo.ScaleToView(Vector2.One).X;

        var subject = _camera.Subject;
        var cameraXZ = new Vector2(_camera.Camera.X, _camera.Camera.Z);
        var pivotXZ = new Vector2(_camera.Pivot.X, _camera.Pivot.Z);
        var subjectXZ = new Vector2(subject.X, subject.Z);

        // Boundary area for camera pivot.
        ImGeo.AddRect(new Vector2(min.X, min.Z), new Vector2(max.X, max.Z), boundaryColor);

        // Camera wedge.
        var theta = -MathF.PI / 2 - _camera.Direction.LonRadians;
        var phi = _camera.ZoomRadians / 2;

        ImGeo.PathLineTo(cameraXZ);
        ImGeo.PathClear();
        ImGeo.PathArcTo(cameraXZ, wedgeLen, theta - phi, theta + phi);
        ImGeo.PathLineTo(cameraXZ);
        ImGeo.PathFillConvex(wedgeColor);

        // Light indicator.
        var lightDirection = _portrait.GetDirectionalLightDirection();
        var lightAngle = -lightDirection.LonRadians + MathF.PI / 2;
        var lightPos = new Vector2(
            MathF.Cos(lightAngle) * lightRadius,
            MathF.Sin(lightAngle) * lightRadius
        );
        var newLightPos = lightPos;
        if (ImGeo.DragHandleCircle("##LightPosition", ref newLightPos, handleSize * pixels, 0))
        {
            var newAngle = -(MathF.Atan2(newLightPos.Y, newLightPos.X) - MathF.PI / 2);
            var newDirection = SphereLL.FromRadians(lightDirection.LatRadians, newAngle);
            _portrait.SetDirectionalLightDirection(newDirection);
            changed = true;
        }
        var color = ImGeo.IsHandleHovered() || ImGeo.IsHandleActive() ? sunActiveColor : sunColor;
        AddLightMarker(lightPos, sunSize * pixels, color);

        // Character center indicator.
        AddPlayerMarker(subjectXZ, e.CharacterDirection(), playerSize * pixels, characterColor);

        // Camera pivot handle.
        var handlePixels = handleSize * pixels;
        if (ImGeo.DragHandleCircle("##CameraPivot", ref pivotXZ, handlePixels, pivotColor))
        {
            _camera.SetPivotPositionXZ(pivotXZ, true);
            changed = true;
        }

        // Camera position handle.
        if (ImGeo.DragHandleCircle("##CameraPosition", ref cameraXZ, handlePixels, cameraColor))
        {
            _camera.SetCameraPositionXZ(cameraXZ);
            changed = true;
        }

        // Right click pan.
        var panning =
            !changed
            && ImGui.IsItemHovered()
            && ImGui.IsMouseDown(ImGuiMouseButton.Right)
            && !ImGui.IsAnyItemActive();
        if (panning)
        {
            var delta = ImGeo.ScaleToView(ImGui.GetIO().MouseDelta);
            if (delta.LengthSquared() > 1e-6f)
            {
                var pivotDelta = new Vector3(delta.X, 0, delta.Y);
                _camera.AdjustPivotPosition(pivotDelta);
                changed = true;
            }
        }

        // Mousewheel zoom.
        if (ImGui.IsItemHovered())
        {
            var delta = ImGui.GetIO().MouseWheel;
            if (delta != 0)
            {
                _camera.AdjustCameraDistance(-delta);
                changed = true;
            }
        }

        ImGeo.EndViewport();

        return changed;
    }

    private bool CameraWidgets()
    {
        var changed = false;

        var startX = ImGui.GetCursorPosX();
        var entireWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.Group())
        {
            var buttonWidth = 0.3f * entireWidth;
            if (ImGui.Button("Face Character", new Vector2(buttonWidth, 0)))
            {
                _camera.FaceSubject();
                changed = true;
            }

            ImGui.Checkbox("Track Motion", ref _followCharacter);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    "Continuously adjust the camera as the character moves\nUseful for PVP limit breaks and other poses with extreme movement."
                );
            }
        }

        ImGui.SameLine();
        var leftWidth = Math.Max(ImGui.GetCursorPosX(), startX + 0.3f * entireWidth);
        ImGui.SameLine(leftWidth);
        using (ImRaii.Group())
        {
            using var _ = ImRaii.ItemWidth(-float.Epsilon);

            // Height slider.
            var hIcon = FontAwesomeIcon.RulerVertical;
            var pivotY = _camera.Pivot.Y;
            if (
                ImPT.IconSliderFloat(
                    "##height",
                    hIcon,
                    ref pivotY,
                    CameraConsts.PivotYMin,
                    CameraConsts.PivotYMax,
                    "%.1f",
                    "Height"
                )
            )
            {
                _camera.SetPivotPositionY(pivotY, false);
                changed = true;
            }

            // Pitch slider.
            var pIcon = FontAwesomeIcon.ArrowsUpDown;
            var pitch = -_camera.Direction.LatDegrees;
            if (
                ImPT.IconSliderFloat(
                    "##pitch",
                    pIcon,
                    ref pitch,
                    -CameraConsts.PitchMax,
                    -CameraConsts.PitchMin,
                    "%.1f",
                    "Pitch up/down"
                )
            )
            {
                _camera.SetCameraPitchRadians(-MathF.Tau * pitch / 360);
                changed = true;
            }
        }

        return changed;
    }

    private static void AddPlayerMarker(Vector2 position, float angle, float size, uint col)
    {
        var (s, c) = MathF.SinCos(angle);
        var front = new Vector2(c, s) * size / 2;
        var left = new Vector2(-s, c) * 0.4f * size - front;
        var right = new Vector2(s, -c) * 0.4f * size - front;

        ImGeo.AddTriangleFilled(position + front, position + left, position + right, col);
    }

    private static void AddLightMarker(Vector2 position, float size, uint col)
    {
        var spikiness = 1.5f;
        ImGeo.AddCircleFilled(position, size, col);
        for (var i = 0; i < 8; i++)
        {
            // Draw a triangle pointing outward.
            var angle = MathF.Tau * i / 8;
            var (s, c) = MathF.SinCos(angle);
            var front = new Vector2(c, s) * size * spikiness;
            var left = new Vector2(-s, c) * size;
            var right = new Vector2(s, -c) * size;
            ImGeo.AddTriangleFilled(position + front, position + left, position + right, col);
        }
    }

    private void SectionAnimation(Editor e)
    {
        ImPT.IconHeader(
            "Animation",
            FontAwesomeIcon.Running,
            "Ctrl+Click the slider to type an exact timestamp, or\nhold Shift for a smaller increment with the +/- buttons."
        );

        var paused = e.IsAnimationPaused();
        var icon = paused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
        if (ImGuiComponents.IconButton(icon, new(ImGui.GetContentRegionAvail().X * 0.15f, 0)))
        {
            e.ToggleAnimation(paused);
            e.BindUI();
        }

        var time = _portrait.GetAnimationProgress();
        var duration = DebounceDuration(e);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var timeChanged = ImPT.NudgeFloat("##animation", ref time, 0, duration, 1.0f);

        // In theory there are a few animations (Mesotes mostly) that wouldn't
        // want to pin for held-down nudge buttons, but on net it's worth it.
        _portrait.SetPinned(ImGui.IsItemActive());

        if (timeChanged)
        {
            _portrait.SetAnimationProgress(time);
        }
    }

    private void SectionFacing(Editor e)
    {
        ImPT.IconHeader(
            "Head & Gaze",
            FontAwesomeIcon.Eye,
            "Drag or double click to change head or gaze direction.\nNot available with certain poses."
        );

        using var width = ImRaii.ItemWidth(ImGui.GetContentRegionAvail().X * 0.5f - 5);

        var r = 0.8f;
        var head = _portrait.GetHeadDirection();
        var noHead = FaceControlMessage(e.HeadControlType());
        if (ImPT.HalfDirection("Head Direction", ref head, new(68, 79), new(-68, -79), r, noHead))
        {
            _portrait.SetHeadDirection(head);
        }
        ImGui.SameLine();

        var eye = _portrait.GetEyeDirection();
        var noEyes = FaceControlMessage(e.GazeControlType());
        if (ImPT.HalfDirection("Eye Direction", ref eye, new(20, 5), new(-20, -12), r, noEyes))
        {
            _portrait.SetEyeDirection(eye);
        }
    }

    private static string? FaceControlMessage(Editor.ControlType ty)
    {
        return ty switch
        {
            Editor.ControlType.Locked => "controlled by pose",
            Editor.ControlType.Free => null,
            Editor.ControlType.Camera => "facing camera",
            _ => "confused, try again",
        };
    }

    /// <summary>
    /// Setting the pose or progress causes reloading, which temporarily keeps
    /// us from directly observing the animation duration. Keep the old one, so
    /// we don't change the slider maximum while dragging.
    /// </summary>
    private unsafe float DebounceDuration(Editor e)
    {
        var pose = e.State->BannerEntry.BannerTimeline;

        if (pose != _lastPose)
        {
            _lastPose = pose;
            _lastDuration = 1f;
        }

        var dur = e.GetAnimationDuration();
        if (dur > 0)
        {
            _lastDuration = dur;
        }

        return _lastDuration;
    }
}
