using ImGuiNET;

namespace Photobooth.UI.Stateless;

public static partial class ImPB
{
    /// <summary>
    /// For debugging, a widget that makes a copyable textbox for a pointer.
    /// </summary>
    public static void CopyAddress(string label, nint address)
    {
        var addressString = $"0x{address:X}";
        ImGui.InputText(label, ref addressString, 32, ImGuiInputTextFlags.ReadOnly);
    }
}
