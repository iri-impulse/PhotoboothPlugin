using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using Photobooth.GameExt;

namespace Photobooth.Controls;

/// <summary>
/// Utilities for interacting with the open portrait editor.
/// </summary>
public unsafe ref struct Editor
{
    public readonly bool IsValid =>
        Agent != null && State != null && Portrait != null && Character != null;

    public AgentBannerEditor* Agent;
    public AgentBannerEditorState* State;
    public CharaViewPortrait* Portrait;
    public Character* Character;

    internal static Editor Current()
    {
        var agent = AgentBannerEditor.Instance();
        if (
            agent == null
            || agent->EditorState == null
            || agent->EditorState->CharaView == null
            || agent->EditorState->CharaView->GetCharacter() == null
        )
        {
            return new Editor { };
        }

        return new Editor
        {
            Agent = agent,
            State = agent->EditorState,
            Portrait = agent->EditorState->CharaView,
            Character = agent->EditorState->CharaView->GetCharacter(),
        };
    }

    public static unsafe AddonBannerEditor* GetAddon()
    {
        return (AddonBannerEditor*)Plugin.GameGui.GetAddonByName("BannerEditor");
    }

    public static bool IsAddonOpen()
    {
        var a = GetAddon();
        return a != null && a->IsVisible;
    }

    public static bool IsAddonReady()
    {
        var a = GetAddon();
        var e = Current();

        return e.IsValid & a != null
            && a->IsVisible
            && a->IsReady
            && a->IsFullyLoaded()
            && e.Portrait->CharaViewPortraitCharacterLoaded;
    }

    private readonly void AssertValid()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Editor not open/loaded");
        }
    }

    public bool IsAnimationPaused()
    {
        AssertValid();

        return Portrait->IsAnimationPaused();
    }

    public void ToggleAnimationPlayback(bool paused)
    {
        AssertValid();

        if (IsAnimationPaused() == paused)
        {
            return;
        }

        Portrait->PendingAnimationPauseState = paused;
        Portrait->IsAnimationPauseStatePending = true;

        var addon = (AddonBannerEditor*)Plugin.GameGui.GetAddonByName("BannerEditor");
        if (addon == null)
            return;

        addon->PlayAnimationCheckbox->SetChecked(!paused);
    }

    public float GetAnimationDuration()
    {
        AssertValid();

        var timeline = &Character->Timeline;
        if (timeline == null)
            return -1;

        var sched = timeline->TimelineSequencer.GetSchedulerTimeline(0);
        if (sched == null)
            return -1;

        return sched->TimelineController.GetAnimationDuration();
    }

    /// <summary>
    /// Update the portrait's current animation by setting its progress.
    /// </summary>
    /// <remarks>
    /// Unlike SetPoseTimed, this does not hide the model for a frame or two.
    /// Instead it is slightly cursed in other regards:
    /// <list type="bullet">
    /// <item>
    ///   When playing animations with extra held items (sweep, fan dance,
    ///   watering can, etc.) it must be called every frame or they reset.
    /// </item>
    /// <item>
    ///   When playing Action-type ActionTimelines such as PVP LBs, it only
    ///   works if the character's motion is unpaused.
    /// </item>
    /// </list>
    /// On the whole though this is enough to get the slider working, which
    /// would not be usable if the character disappeared while dragging.
    /// </remarks>
    public unsafe void SetAnimationProgress(float progress)
    {
        AssertValid();

        var timeline = &Character->Timeline;

        var row_id = timeline->GetActionTimelineRowId();
        var row = Plugin.DataManager.GetExcelSheet<ActionTimeline>().GetRowAt(row_id);

        var gameObjectId = ClientObjectManager
            .Instance()
            ->GetObjectByIndex(0xf8)
            ->GetGameObjectId();

        var req = new AnimationRequest()
        {
            GameObjectId = gameObjectId,
            Speed = 1.0f,
            StartTime = -1,
            PrevTime = MathF.Max(0.001f, progress),
            Onset = 1,
            Type = row.Type,
        };

        timeline->TimelineSequencer.PlayTimeline(row_id, &req);
    }

    public unsafe void SetHasChanged(bool changed)
    {
        AssertValid();
        State->SetHasChanged(changed);
    }

    public unsafe Vector3 CharacterPosition(uint partId = 6)
    {
        AssertValid();
        Character->GetPartPosition(out var pos, partId);
        return pos * CameraConsts.Scale;
    }

    public unsafe float CharacterDirection()
    {
        AssertValid();

        Character->GetPartPosition(out var head, 26);
        Character->GetPartPosition(out var face, 27);
        var facing = face - head;

        return MathF.Atan2(facing.Z, facing.X);
    }

    public unsafe int ActionTimelineRowId()
    {
        AssertValid();
        return Character->Timeline.TimelineSequencer.GetSlotTimeline(0);
    }

    public unsafe ControlType GazeControlType()
    {
        AssertValid();

        var id = ActionTimelineRowId();
        var timeline = Plugin.DataManager.GetExcelSheet<ActionTimeline>().GetRowAt(id);

        var flag = LookAtContainer.BannerCameraFollowFlags.Eyes;
        var camera = Character->LookAt.BannerCameraFollowFlag.HasFlag(flag);

        return timeline.Slot switch
        {
            1 => ControlType.Locked,
            _ => camera ? ControlType.Camera : ControlType.Free,
        };
    }

    public unsafe ControlType HeadControlType()
    {
        AssertValid();

        var id = ActionTimelineRowId();
        var timeline = Plugin.DataManager.GetExcelSheet<ActionTimeline>().GetRowAt(id);

        var flag = LookAtContainer.BannerCameraFollowFlags.Head;
        var camera = Character->LookAt.BannerCameraFollowFlag.HasFlag(flag);

        return timeline.Slot switch
        {
            1 => ControlType.Locked,
            3 => ControlType.Locked,
            _ => camera ? ControlType.Camera : ControlType.Free,
        };
    }

    public enum ControlType
    {
        Locked = 0,
        Free = 1,
        Camera = 2,
    }

    public readonly void UpdateUI(in ExportedPortraitData data)
    {
        var addon = (AddonBannerEditor*)Plugin.GameGui.GetAddonByName("BannerEditor");
        if (addon == null)
        {
            return;
        }

        addon->ImageRotation->SetValue(data.ImageRotation);
        addon->CameraZoomSlider->SetValue(data.CameraZoom);

        addon->AmbientLightingBrightnessSlider->SetValue(data.AmbientLightingBrightness);
        addon->AmbientLightingColorRedSlider->SetValue(data.AmbientLightingColorRed);
        addon->AmbientLightingColorGreenSlider->SetValue(data.AmbientLightingColorGreen);
        addon->AmbientLightingColorBlueSlider->SetValue(data.AmbientLightingColorBlue);

        addon->DirectionalLightingBrightnessSlider->SetValue(data.DirectionalLightingBrightness);
        addon->DirectionalLightingColorRedSlider->SetValue(data.DirectionalLightingColorRed);
        addon->DirectionalLightingColorGreenSlider->SetValue(data.DirectionalLightingColorGreen);
        addon->DirectionalLightingColorBlueSlider->SetValue(data.DirectionalLightingColorBlue);

        addon->DirectionalLightingHorizontalAngleSlider->SetValue(
            data.DirectionalLightingHorizontalAngle
        );
        addon->DirectionalLightingVerticalAngleSlider->SetValue(
            data.DirectionalLightingVerticalAngle
        );
    }
}
