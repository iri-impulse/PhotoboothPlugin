using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Photobooth.GameExt;

public unsafe class Funcs
{
    private static Funcs? _Instance;

    public static Funcs Instance() => _Instance ??= new Funcs();

    private Funcs()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
    }

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

    [Signature(
        "40 53 48 83 EC ?? 80 B9 ?? ?? ?? ?? ?? 48 8B D9 74 ?? E8 ?? ?? ?? ?? 41 B8 ?? ?? ?? ?? 48 8D 54 24 ?? 48 8B 48 ?? 48 8B 01 FF 50 ?? 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 45 33 C0"
    )]
    public readonly delegate* unmanaged<AtkUnitBase*, void> AgentBannerEditor_Hide2;
}

public static unsafe class FuncExtensions
{
    private static Funcs F => Funcs.Instance();

    public static ushort GetActionTimelineRowId(this ref TimelineContainer tc)
    {
        fixed (TimelineContainer* ptr = &tc)
        {
            return F.TimelineContainer_GetActionTimelineRowId(ptr);
        }
    }

    public static void GetPartPosition(this ref Character chara, out Vector3 outPos, uint unk)
    {
        fixed (Character* ptr = &chara)
        {
            fixed (Vector3* pos = &outPos)
            {
                F.Character_GetPartPosition(ptr, pos, unk);
            }
        }
    }
}
