// MIGRATION NOTES:
// 1. Window.Current is GONE in WinUI 3. Use a static MainWindow property instead.
// 2. No more OnSuspending — desktop WinUI 3 apps don't have a suspend lifecycle.
// 3. ApplicationView.GetForCurrentView().TitleBar → AppWindow.TitleBar or ExtendsContentIntoTitleBar.
// 4. LaunchActivatedEventArgs simplified — no PreviousExecutionState in WinUI 3.
// 5. AppShell (custom nav) is replaced by NavigationView built into MainWindow.

using Microsoft.UI.Xaml;

namespace RssReader;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    /// <summary>
    /// The main application window. In WinUI 3, there is no Window.Current —
    /// you must track your own window reference.
    /// </summary>
    public static Window MainWindow { get; private set; } = null!;
}
