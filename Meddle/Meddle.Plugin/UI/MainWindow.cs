using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Meddle.Plugin.UI;

public class MainWindow : Window, IDisposable
{
    private readonly ITab[] _tabs;

    public MainWindow(ITab[] tabs) : base("Meddle")
    {
        _tabs = tabs;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2( 375, 350 ),
            MaximumSize = new Vector2( 1200, 1000 ),
        };

        IsOpen          = Plugin.Configuration.AutoOpen;
    }

    private Dictionary<string, DateTime> _errorLog = new();
    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("##meddle_tabs");
        foreach (var tab in _tabs)
        {
            using var tabItem = ImRaii.TabItem(tab.Name);
            try
            {
                if (tabItem) {
                    tab.Draw();
                }
            }
            catch (Exception e)
            {
                var errorString = e.ToString();
                // compare error string to last error
                if (_errorLog.TryGetValue(errorString, out var lastError) && (DateTime.Now - lastError < TimeSpan.FromSeconds(5)))
                {
                    return; // Don't spam the log
                }

                Service.Log.Error(e, $"Error in {tab.Name}");
                _errorLog[errorString] = DateTime.Now;
            }
        }
    }

    public void Dispose()
    {
        foreach (var tab in _tabs)
        {
            tab.Dispose();
        }
    }
}