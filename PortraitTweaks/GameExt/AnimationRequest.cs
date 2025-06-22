using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace PortraitTweaks.GameExt;

// Reversed manually. This gets passed to ActionTimelineSequencer::PlayTimeline
// and appears as a substructure of various Timeline-related structs.
[StructLayout(LayoutKind.Explicit, Size = 0x60)]
public ref struct AnimationRequest
{
    // csharpier-ignore-start
    [FieldOffset(0x00)] public VTable VirtualTable;

    // Optional, only used for some animations
    [FieldOffset(0x10)] public float TargetX;
    [FieldOffset(0x14)] public float TargetY;
    [FieldOffset(0x18)] public float TargetZ;

    [FieldOffset(0x1C)] public float VfxDuration;

    // Angle in the XZ plane.
    [FieldOffset(0x20)] public float TargetAngle;

    // Usually 1.0.
    // TODO test this and make sure it's actually speed
    [FieldOffset(0x24)] public float Speed;

    // TODO test this
    // Notes:
    // - is 0 if StartTime is -1
    // - in real life seems to be epsilonically earlier than StartTime
    [FieldOffset(0x28)] public float PrevTime;

    // Notes:
    // - some special meaning for -1
    // - it seems to be set to +0.001 instead of 0
    // - Does this do anything? It seems like it's only PrevTime that matters.
    [FieldOffset(0x2C)] public float StartTime;

    // Optional, only used for some animations.
    [FieldOffset(0x30)] public uint SpellId;

    [FieldOffset(0x38)] public GameObjectId GameObjectId;

    // Used for... spell effects? And little else.
    [FieldOffset(0x40)] public uint AnimationVariation;

    // Same as the Type column in the ActionTimeline sheet.
    [FieldOffset(0x44)] public uint Type;

    // Usually 0 or -1. Zero makes it play instantly without any transition.
    [FieldOffset(0x48)] public int Onset;

    // Often set (to 0, 1, or 255) but no idea what it does.
    [FieldOffset(0x4C)] public byte Flags0;
    [FieldOffset(0x4D)] public byte Flags1;
    // If nonzero, plays the idle/breathing animation even if motion is paused.
    [FieldOffset(0x4E)] public byte Flags2;
    [FieldOffset(0x4F)] public byte Flags3;
    // If nonzero, the XYZ/Angle fields are consulted.
    [FieldOffset(0x50)] public byte Flags4;
    [FieldOffset(0x51)] public byte Flags5;
    // Often set to 0x01 but no idea what it does.
    [FieldOffset(0x52)] public byte Flags6;
    [FieldOffset(0x53)] public byte Flags7;
    // csharpier-ignore-end

    // Virtual destructor nonsense; this is not ever going to be called AFAIK
    // but better safe than sorry. We make a static vtable with one no-op item
    // in it. (The real VTable is too short to find a signature for it seems.)

    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public unsafe struct VTable
    {
        [FieldOffset(0x00)]
        public delegate* unmanaged<AnimationRequest*, bool, void> Dtor;

        [UnmanagedCallersOnly]
        private static void Dummy(AnimationRequest* self, bool free) { }

        public VTable()
        {
            Dtor = &Dummy;
        }
    }

    internal static VTable DummyVTable = new();
}
