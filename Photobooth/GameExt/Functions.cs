using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Photobooth.GameExt;

public unsafe class Functions
{
    private static Functions? _Instance;

    public static Functions Instance() => _Instance ??= new Functions();

    private Functions()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
    }

    /// <summary>
    /// The currently-playing main ActionTimeline of a TimelineContainer. This
    /// is not quite always the ActionTimeline of the first slot, because for a
    /// frame or two after the timeline is reset this will have a value already
    /// even if the animation hasn't started.
    /// </summary>
    [Signature("E8 ?? ?? ?? ?? 0F B7 C8 85 C9 0F 84")]
    public readonly delegate* unmanaged<
        TimelineContainer*,
        ushort> TimelineContainer_GetActionTimelineRowId;

    // 1 = eyes?
    // 6 = torso maybe? less stable
    // 7 = torso maybe? most stable
    // 8 = right hand
    // 9 = left hand
    // 10 = right foot
    // 11 = left foot
    // 25, 26 = head parts
    // 27 = some face part
    [Signature("E8 ?? ?? ?? ?? F3 41 0F 5C F7")]
    public readonly delegate* unmanaged<
        Character*,
        Vector3*,
        uint,
        Vector3*> Character_GetPartPosition;
}
