using System.Runtime.InteropServices;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Photobooth.Controls;
using Photobooth.UI.Stateless;

namespace Photobooth.Windows;

internal class DebugWindow : Window
{
    public DebugWindow()
        : base($"{Plugin.PluginName} Debug") { }

    [StructLayout(LayoutKind.Sequential)]
    private record MemDebug
    {
        internal nint SchedulerTimeline;
        internal nint TimelineContainer;
        internal nint CharaViewPortrait;
    }

    // By making the struct live as long as the debug window, we can have the
    // (addresses of) the things we're exposing remain stable as the portrait
    // window opens and closes.
    private static readonly MemDebug _Mem = new();

    private static GCHandle _MemGCHandle = GCHandle.Alloc(_Mem, GCHandleType.Pinned);

    public override void Draw()
    {
        var manual = PortraitController.UseManualCorrectionFactor;
        if (ImGui.Checkbox("##manual_correction", ref manual))
        {
            PortraitController.UseManualCorrectionFactor = manual;
        }
        ImGui.SameLine();

        ImGui.SetNextItemWidth(-float.Epsilon);
        var correction = PortraitController.ManualCorrectionFactor;
        if (
            ImGui.SliderFloat(
                "##drag_correction",
                ref correction,
                0,
                4f,
                "Correction Factor: %.3f",
                ImGuiSliderFlags.NoRoundToFormat
            )
        )
        {
            PortraitController.ManualCorrectionFactor = correction;
        }

        var setPoseTimedOnly = !PortraitController.UseNew;
        if (ImGui.Checkbox("Simple Progress Setter", ref setPoseTimedOnly))
        {
            PortraitController.UseNew = !setPoseTimedOnly;
        }

        var e = Editor.Current();

        if (e.IsValid)
        {
            DumpTimeline(e);
        }
    }

    private unsafe void DumpTimeline(Editor e)
    {
        var editor = e.Agent;
        var state = e.State;
        var portrait = e.Portrait;
        var chara = e.Character;

        var timeline = &chara->Timeline;
        if (timeline == null)
            return;

        var seq = &timeline->TimelineSequencer;
        if (seq == null)
            return;

        for (var slot = 0u; slot < 14; slot++)
        {
            var row_id = seq->GetSlotTimeline(slot);
            if (row_id == 0)
            {
                continue;
            }
            var row = Plugin.DataManager.GetExcelSheet<ActionTimeline>().GetRowAt(row_id);
            ImGui.Text($"ActionTimeline({slot}): #{row_id} {row.Key}");
        }

        var row0_id = seq->GetSlotTimeline(0);
        var row0 = Plugin.DataManager.GetExcelSheet<ActionTimeline>().GetRowAt(row0_id);
        var stance = row0.Stance;

        ImGui.Text($"Main timeline: id {row0.RowId}, stance {stance}, slot {row0.Slot}");

        var prog1 = state->BannerEntry.AnimationProgress;
        var prog2 = portrait->GetAnimationTime();
        ImGui.Text($"Animation Progress: {prog1} / {prog2}");

        var sched = seq->GetSchedulerTimeline(stance);

        _Mem.SchedulerTimeline = (nint)sched;
        _Mem.TimelineContainer = (nint)timeline;
        _Mem.CharaViewPortrait = (nint)portrait;

        var pSched = _MemGCHandle.AddrOfPinnedObject() + 0;
        ImPT.CopyAddress("SchedulerTimeline (p)", pSched);

        var pCont = _MemGCHandle.AddrOfPinnedObject() + 8;
        ImPT.CopyAddress("TimelineContainer (p)", pCont);

        var pPortrait = _MemGCHandle.AddrOfPinnedObject() + 16;
        ImPT.CopyAddress("CharaViewPortrait (p)", pPortrait);
    }
}
