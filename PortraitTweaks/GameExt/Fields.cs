using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;

namespace PortraitTweaks.GameExt;

public static unsafe class FieldExtensions
{
    public static unsafe float GetAnimationDuration(this ref TimelineController tc)
    {
        fixed (TimelineController* ptr = &tc)
        {
            var bits32 = Marshal.ReadInt32((nint)ptr, 0x48);
            return BitConverter.ToSingle(BitConverter.GetBytes(bits32));
        }
    }
}
