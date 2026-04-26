using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Photobooth.Controls;
using Photobooth.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Photobooth.UI.Panels
{
    internal class SnapshotPanel(PortraitController portrait)
        : Panel(FontAwesomeIcon.PhotoVideo, "Saved snapshots for this job")
    {
        private readonly PortraitController _portrait = portrait;

        protected override void DrawBody()
        {

            if (ImGui.Button("Take snapshot"))
            {
                _portrait.TakeOrUpdateSnapshot(Guid.NewGuid());
            }
            var snapshots = Plugin.SnapshotDataController.Snapshots;
            var currentClassJobId = _portrait.CurrentClassJobId();
            if (snapshots.ContainsKey(currentClassJobId))
            {
                DrawSnapshopTable(snapshots[currentClassJobId]);
            }

            if (ImGui.Button("Restore snapshot"))
            {
                var guid = Guid.NewGuid(); //temporary
                _portrait.RestoreSnapshot(guid);
            }
        }

        private void DrawSnapshopTable(List<PortraitSnapshot> snaps)
        {
            if (ImGui.BeginTable("Saved snapshots", 4, ImGuiTableFlags.Resizable))
            {
                
                ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthStretch, 0.2f);
                ImGui.TableSetupColumn("Clan", ImGuiTableColumnFlags.WidthStretch, 0.7f);
                ImGui.TableSetupColumn("Gender", ImGuiTableColumnFlags.WidthStretch, 0.1f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch, 0.3f);
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var currentClassJobIdd = _portrait.CurrentClassJobId();
                foreach (var snapshot in snaps)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{snapshot.TakenAt.ToShortDateString()} {snapshot.TakenAt.ToShortTimeString()}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(snapshot.TribeName);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(snapshot.Gender);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted("buttons");
                }

                ImGui.EndTable();

            }
        }
    }
}
