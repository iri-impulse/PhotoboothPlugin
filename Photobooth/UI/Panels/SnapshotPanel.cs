using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Photobooth.Controls;
using System;
using System.Collections.Generic;
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
            if (ImGui.Button("Restore snapshot"))
            {
                var guid = Guid.NewGuid(); //temporary
                _portrait.RestoreSnapshot(guid);
            }
        }
    }
}
