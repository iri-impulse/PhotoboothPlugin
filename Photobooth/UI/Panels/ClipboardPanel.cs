using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Photobooth.Controls;

namespace Photobooth.UI.Panels;

internal class ClipboardPanel(PortraitController portrait, CameraController camera)
    : Panel(FontAwesomeIcon.Clipboard, "Clipboard")
{
    private readonly PortraitController _portrait = portrait;
    private readonly CameraController _camera = camera;

    private string _status = string.Empty;

    protected override void DrawBody()
    {
        var e = Editor.Current();
        if (!e.IsValid)
        {
            return;
        }

        var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

        if (ImGui.Button("Copy Settings", new(buttonWidth, 0)))
        {
            ImGui.SetClipboardText(PortraitClipboard.Export(_portrait, _camera));
            _status = "Portrait settings copied.";
        }

        ImGui.SameLine();

        if (ImGui.Button("Paste Settings", new(buttonWidth, 0)))
        {
            if (PortraitClipboard.TryImport(ImGui.GetClipboardText(), _portrait, _camera, out var message))
            {
                _portrait.ApplyChanges(e);
                _camera.Save(e);
                e.SetHasChanged(true);
                e.UpdateUI(in _portrait.Data);
            }

            _status = message;
        }

        if (!string.IsNullOrEmpty(_status))
        {
            ImGui.TextWrapped(_status);
        }
    }
}
