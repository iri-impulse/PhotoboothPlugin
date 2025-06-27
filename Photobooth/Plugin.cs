using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Photobooth.GameExt;
using Photobooth.Windows;

namespace Photobooth;

public sealed class Plugin : IDalamudPlugin
{
    public const string CommandName = "/pb";
    public const string PluginName = "Photobooth";

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    [PluginService]
    internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    [PluginService]
    internal static IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new($"{PluginName}Plugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private DebugWindow DebugWindow { get; init; }

    // Used to observe the portrait editor closing sooner than PreFinalize.
    private Hook<AtkUnitBase.Delegates.Hide2>? _hideHook;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        DebugWindow = new DebugWindow();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DebugWindow);

        CommandManager.AddHandler(
            CommandName,
            new CommandInfo(OnCommand)
            {
                HelpMessage = $"Open the {PluginName} portrait editor window.",
            }
        );

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Set up listeners so we can automatically open when editing a portrait.
        AddonLifecycle.RegisterListener(
            AddonEvent.PostRequestedUpdate,
            "BannerEditor",
            OnBannerEditorOpen
        );

        InstallHooks();
    }

    public void Dispose()
    {
        _hideHook?.Dispose();

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        AddonLifecycle.UnregisterListener([OnBannerEditorOpen]);
    }

    private void OnBannerEditorOpen(AddonEvent type, AddonArgs args)
    {
        if (Configuration.AutoOpenWhenEditingPortrait && !MainWindow.IsOpen)
        {
            MainWindow.Toggle();
        }
    }

    private unsafe void InstallHooks()
    {
        var address = Funcs.Instance().AgentBannerEditor_Hide2;
        if (address is null)
        {
            Log.Error("Failed to hook AgentBannerEditor.Hide2. Auto-closing disabled.");
            return;
        }
        _hideHook = GameInteropProvider.HookFromAddress<AtkUnitBase.Delegates.Hide2>(
            (nint)address,
            AgentBannerEditorHide2Detour
        );
        _hideHook.Enable();
    }

    private unsafe void AgentBannerEditorHide2Detour(AtkUnitBase* self)
    {
        try
        {
            if (MainWindow.IsOpen)
            {
                MainWindow.Toggle();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while closing the banner editor.");
        }

        _hideHook?.Original(self);
    }

    private void OnCommand(string command, string args)
    {
        if (args == "config" || args == "settings" || args == "options")
        {
            ToggleConfigUI();
        }
        else if (args == "debug")
        {
            ToggleDebugUI();
        }
        else
        {
            ToggleMainUI();
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    public void ToggleMainUI() => MainWindow.Toggle();

    public void ToggleDebugUI() => DebugWindow.Toggle();
}
