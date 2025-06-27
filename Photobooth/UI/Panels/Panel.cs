using Dalamud.Interface;
using Photobooth.UI.Stateless;

namespace Photobooth.UI.Panels;

internal abstract class Panel(FontAwesomeIcon icon, string title)
{
    public string Title { get; } = title;
    public FontAwesomeIcon Icon { get; } = icon;
    public virtual string? Help { get; }

    protected abstract void DrawBody();

    public virtual void Reset() { }

    public void Draw()
    {
        ImPB.IconHeader(Title, Icon, Help);
        DrawBody();
    }
}
