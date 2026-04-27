using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Photobooth.Controls;
using Photobooth.Model;
using System.Collections.Generic;

namespace Photobooth.UI.Panels
{
    internal class SnapshotPanel(PortraitController portrait)
        : Panel(FontAwesomeIcon.PhotoVideo, "Saved snapshots for this job")
    {
        private readonly PortraitController _portrait = portrait;

        protected override void DrawBody()
        {
            var snapshots = Plugin.SnapshotDataController.Snapshots;
            var currentClassJobId = _portrait.CurrentClassJobId();
            if (snapshots.ContainsKey(currentClassJobId))
            {
                DrawSnapshopTable(snapshots[currentClassJobId]);
            }

            if (ImGui.Button("Take snapshot"))
            {
                _portrait.TakeSnapshot();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Save the current look of the portrait. Border and accent are not saved.");
            }
        }

        private void DrawSnapshopTable(List<PortraitSnapshot> snaps)
        {
            if (ImGui.BeginTable("Saved snapshots", 4, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthStretch, 0.3f);
                ImGui.TableSetupColumn("Clan", ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn("Gender", ImGuiTableColumnFlags.WidthStretch, 0.2f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch, 0.1f);
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var currentClassJobIdd = _portrait.CurrentClassJobId();
                int index = 0;
                foreach (var snapshot in snaps)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{snapshot.TakenAt.ToShortDateString()} {snapshot.TakenAt.ToShortTimeString()}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(snapshot.TribeName);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(snapshot.Sex);
                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton($"Apply##{index}", FontAwesomeIcon.ArrowsSpin))
                    {
                        _portrait.RestoreSnapshot(snapshot);
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Apply saved snapshot to the portrait.");
                    }
                    index++;
                }

                ImGui.EndTable();
            }
        }
    }
}
