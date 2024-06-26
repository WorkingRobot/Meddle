using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Lumina.Data;
using Lumina.Data.Files;
using Meddle.Plugin.UI;
//using Meddle.Plugin.UI.Shared;
using Meddle.Plugin.Utility;
using Meddle.Xande;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xande;
using Xande.Havok;

namespace Meddle.Plugin;

public unsafe sealed class Plugin : IDalamudPlugin
{
    private static readonly WindowSystem WindowSystem = new("Meddle");
    public static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "Meddle.Export");
    private readonly MainWindow _mainWindow;
    private readonly ICommandManager _commandManager;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly IGameInteropProvider _gameInteropProvider;

    [Signature("40 53 48 83 EC 20 FF 81 ?? ?? ?? ?? 48 8B D9 48 8D 4C 24", DetourName = nameof(PostTickDetour))]
    private Hook<PostTickDelegate> postTickHook = null!;
    private delegate bool PostTickDelegate(nint a1);

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.Initialize(pluginInterface);
        config.Save();

        var services = new ServiceCollection()
            .AddDalamud(pluginInterface)
            .AddUi()
            .AddSingleton(pluginInterface)
            .AddSingleton(config)
            .AddSingleton<HavokConverter>()
            .AddSingleton<ModelConverter>()
            .AddSingleton<LuminaManager>()
            //.AddSingleton<ResourceTreeRenderer>()
            .BuildServiceProvider();
        
        _mainWindow = services.GetRequiredService<MainWindow>();
        WindowSystem.AddWindow(_mainWindow);
        
        _commandManager = services.GetRequiredService<ICommandManager>();
        _commandManager.AddHandler("/meddle", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the menu"
        });
        
        _pluginInterface = services.GetRequiredService<DalamudPluginInterface>();
        _pluginInterface.UiBuilder.Draw += DrawUi;
        _pluginInterface.UiBuilder.OpenConfigUi += OpenUi;
        _pluginInterface.UiBuilder.DisableGposeUiHide = true;

        _gameInteropProvider = services.GetRequiredService<IGameInteropProvider>();
        
        // https://github.com/Caraxi/SimpleTweaksPlugin/blob/2b7c105d1671fd6a344edb5c621632b8825a81c5/SimpleTweaksPlugin.cs#L101C13-L103C75
        Task.Run(() =>
        {
            FFXIVClientStructs.Interop.Resolver.GetInstance.SetupSearchSpace(services.GetRequiredService<ISigScanner>().SearchBase);
            FFXIVClientStructs.Interop.Resolver.GetInstance.Resolve();
            //_gameInteropProvider.InitializeFromAttributes(this);
            //postTickHook.Enable();
        });
    }

    private bool PostTickDetour(nint a1)
    {
        var ret = postTickHook.Original(a1);
        
        //NewTree.ProcessQueue();
        return ret;
    }

    private void OnCommand(string command, string args)
    {
        OpenUi();
    }
    
    private void OpenUi()
    {
        _mainWindow.IsOpen = true;
        _mainWindow.BringToFront();
    }
    
    private void DrawUi()
    {
        WindowSystem.Draw();
    }

    public void Dispose()
    {
        postTickHook?.Dispose();

        _mainWindow.Dispose();
        WindowSystem.RemoveAllWindows();
        _commandManager.RemoveHandler("/meddle");

        _pluginInterface.UiBuilder.Draw -= DrawUi;
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenUi;
    }
}
