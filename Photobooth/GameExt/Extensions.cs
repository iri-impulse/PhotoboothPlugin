using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Photobooth.GameExt;

public static unsafe class Extensions
{
    private static Functions F => Functions.Instance();

    public static unsafe float GetAnimationDuration(this ref TimelineController tc)
    {
        fixed (TimelineController* ptr = &tc)
        {
            var bits32 = Marshal.ReadInt32((nint)ptr, 0x48);
            return BitConverter.ToSingle(BitConverter.GetBytes(bits32));
        }
    }

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
