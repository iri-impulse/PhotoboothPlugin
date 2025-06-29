using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Photobooth.Controls;
using Photobooth.UI.Panels;
using static Photobooth.Configuration;

namespace Photobooth.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;

    private readonly PortraitController _portrait = new(new());
    private readonly CameraController _camera = new();

    // Panels
    private readonly AnimationPanel _animationPanel;
    private readonly CameraPanel _cameraPanel;
    private readonly FacingPanel _facingPanel;
    private readonly LightingPanel _lightingPanel;

    // The temporary/stateful attachment side of the window, so "auto" doesn't
    // flip sides when moving the banner editor unless there's no more space.
    private WindowAttachment _attachment = WindowAttachment.Right;

    public MainWindow(Plugin plugin)
        : base($"{Plugin.PluginName}##{Plugin.PluginName}_mainwindow")
    {
        _plugin = plugin;

        Flags =
            ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(375, 200),
            MaximumSize = new(600, float.MaxValue),
        };

        var settingsButton = new TitleBarButton()
        {
            Icon = FontAwesomeIcon.Cog,
            Click = (button) =>
            {
                _plugin.ToggleConfigUI();
            },
        };
        TitleBarButtons = [settingsButton];

        _animationPanel = new AnimationPanel(_portrait);
        _cameraPanel = new CameraPanel(_portrait, _camera, _plugin.Configuration);
        _facingPanel = new FacingPanel(_portrait, _plugin.Configuration);
        _lightingPanel = new LightingPanel(_portrait);
    }

    public void Dispose()
    {
        _portrait.Dispose();
    }

    public override void OnOpen()
    {
        _camera.Reset();

        _animationPanel.Reset();
        _cameraPanel.Reset();
        _facingPanel.Reset();
        _lightingPanel.Reset();
    }

    private unsafe Vector2? SnapToBannerEditor()
    {
        if (_plugin.Configuration.AttachWindow is not WindowAttachment setting)
        {
            return null;
        }

        var editor = Editor.GetAddon();
        if (editor == null || !Editor.IsAddonOpen())
            return null;
        var col = editor->RootNode;
        if (col == null)
            return null;
        var device = Device.Instance();
        if (device == null)
            return null;

        var top = col->GetYFloat();
        var left = col->GetXFloat();
        var editorWidth = col->GetWidth() * col->GetScaleX();
        var screenWidth = device->Width;
        var windowWidth = ImGui.GetWindowSize().X;

        var rightSpace = screenWidth - (left + editorWidth);
        var fitsOnLeft = left > windowWidth + 10;
        var fitsOnRight = rightSpace > windowWidth + 10;

        // The hysteresis approach here is:
        // - use the setting if it's definitive
        // - if only one side fits, use that side
        // - otherwise stay on the side we were.

        _attachment = setting switch
        {
            WindowAttachment.Auto => (fitsOnLeft, fitsOnRight) switch
            {
                (true, false) => WindowAttachment.Left,
                (false, true) => WindowAttachment.Right,
                _ => _attachment,
            },
            _ => setting,
        };

        var x = _attachment switch
        {
            WindowAttachment.Left => left - windowWidth - 10,
            _ => left + editorWidth + 10,
        };

        return new Vector2(x, top);
    }

    public override bool DrawConditions()
    {
        return Editor.IsAddonReady();
    }

    public override void Draw()
    {
        var e = Editor.Current();

        if (!Editor.IsAddonOpen())
        {
            Toggle();
            return;
        }

        if (e.IsValid)
        {
            Opened(e);
        }
        else
        {
            Unopened();
        }

        var height = ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y;
        ImGui.SetWindowSize(new(ImGui.GetWindowSize().X, height));
    }

    private void Opened(Editor e)
    {
        // If the banner editor is open and we're in automatic mode, we hide
        // ourselves when it closes and want ESC to close it, not us.
        RespectCloseHotkey = !_plugin.Configuration.AutoOpenWhenEditingPortrait;
        Position = SnapToBannerEditor();

        // Load manual portrait changes.
        _portrait.CopyData(e);
        // Load manual camera changes and perhaps track moving character.
        _camera.Load(e);

        _lightingPanel.Draw();
        _animationPanel.Draw();
        _cameraPanel.Draw();
        _facingPanel.Draw();
    }

    private unsafe void Unopened()
    {
        RespectCloseHotkey = true;
        Position = null;

        ImGui.TextUnformatted("Create or edit a portrait to get started.");
        ImGui.Spacing();

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Portrait, "Portrait List"))
        {
            UIModule.Instance()->ExecuteMainCommand(92);
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.AddressCard, "Adventurer Plate"))
        {
            UIModule.Instance()->ExecuteMainCommand(93);
        }
    }
}
