# UWP → WinUI 3 Migration Guide: RSS Reader Sample

## Why This Sample Was Chosen

The RSS Reader is the ideal first migration because it demonstrates **every common UWP → WinUI 3 pain point** in a manageable codebase (~15 files). It covers:
- Custom navigation → Built-in NavigationView
- UWP app lifecycle → WinAppSDK lifecycle  
- Windows.Web.Syndication → System.ServiceModel.Syndication
- Custom MVVM → CommunityToolkit.Mvvm
- Dispatcher marshaling changes
- Local storage API changes
- Theme resource name changes

---

## Migration Pitfalls: What Developers & LLMs Must Watch For

### 1. 🚨 `Window.Current` is GONE (Critical)

**UWP:**
```csharp
Window.Current.Content = shell;
Window.Current.Activate();
```

**WinUI 3:**
```csharp
// You must track your own window reference
public static Window MainWindow { get; private set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    MainWindow = new MainWindow();
    MainWindow.Activate();
}
```

**Why LLMs get this wrong:** Training data mixes UWP and WinUI 3 patterns. Any code using `Window.Current` is UWP-only and will compile but crash at runtime in WinUI 3.

---

### 2. 🚨 Dispatcher Changes (Critical)

**UWP:**
```csharp
await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
    CoreDispatcherPriority.Normal, () => { /* UI work */ });
```

**WinUI 3:**
```csharp
App.MainWindow.DispatcherQueue.TryEnqueue(() => { /* UI work */ });
// or
DispatcherQueue.GetForCurrentThread().TryEnqueue(() => { /* UI work */ });
```

**Key differences:**
- `CoreDispatcher` → `DispatcherQueue` (from `Microsoft.UI.Dispatching`)
- `RunAsync` (awaitable) → `TryEnqueue` (fire-and-forget, returns bool)
- No `CoreWindow` in WinUI 3 at all

---

### 3. 🚨 No `ApplicationView.GetForCurrentView()` (Critical)

**UWP:**
```csharp
ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = color;
ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 200));
```

**WinUI 3:**
```csharp
// Title bar
ExtendsContentIntoTitleBar = true; // or false
// For custom title bar colors, use AppWindow:
var appWindow = this.AppWindow;
appWindow.TitleBar.BackgroundColor = color;

// Min size
this.AppWindow.Resize(new Windows.Graphics.SizeInt32(320, 200));
```

---

### 4. ⚠️ Navigation Architecture Overhaul (Major)

**UWP (pre-2018 samples):** Custom `AppShell.xaml` with `SplitView` + hand-built `NavMenuListView` (hundreds of lines of selection tracking, keyboard handling, visual state management).

**WinUI 3:** Built-in `NavigationView` handles all of this:
```xml
<NavigationView SelectionChanged="NavView_SelectionChanged">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" Tag="home" />
    </NavigationView.MenuItems>
    <Frame x:Name="ContentFrame" />
</NavigationView>
```

**This single control replaces ~500 lines of custom code.** The entire `NavMenuListView.cs` (11.7KB) and most of `AppShell.xaml.cs` (12.4KB) become unnecessary.

---

### 5. ⚠️ `Windows.Web.Syndication` May Not Work (Major)

**UWP:**
```csharp
var client = new Windows.Web.Syndication.SyndicationClient();
var feed = await client.RetrieveFeedAsync(new Uri(url));
```

**WinUI 3 (Recommended):**
```csharp
using System.ServiceModel.Syndication;
using System.Xml;

using var stream = await httpClient.GetStreamAsync(url);
using var reader = XmlReader.Create(stream);
var feed = SyndicationFeed.Load(reader);
```

**Why:** `SyndicationClient` is a WinRT API that requires specific threading contexts and may fail in unpackaged desktop apps. `System.ServiceModel.Syndication` is pure .NET and works everywhere.

---

### 6. ⚠️ Namespace Mass-Rename (Major but Mechanical)

| UWP Namespace | WinUI 3 Namespace |
|---|---|
| `Windows.UI.Xaml` | `Microsoft.UI.Xaml` |
| `Windows.UI.Xaml.Controls` | `Microsoft.UI.Xaml.Controls` |
| `Windows.UI.Xaml.Data` | `Microsoft.UI.Xaml.Data` |
| `Windows.UI.Xaml.Media` | `Microsoft.UI.Xaml.Media` |
| `Windows.UI.Xaml.Navigation` | `Microsoft.UI.Xaml.Navigation` |
| `Windows.UI.Colors` | `Microsoft.UI.Colors` |
| `Windows.UI.Text` | `Microsoft.UI.Text` |

**XAML namespace change:**
```xml
<!-- UWP default namespace (implicit) points to Windows.UI.Xaml -->
<!-- WinUI 3 default namespace (implicit) points to Microsoft.UI.Xaml -->
<!-- No change needed in XAML files! The SDK handles the mapping. -->
```

**Trap for LLMs:** The XAML default namespace `http://schemas.microsoft.com/winfx/2006/xaml/presentation` maps to DIFFERENT CLR namespaces depending on the project type. In code-behind, you MUST change `using` statements.

---

### 7. ⚠️ App Lifecycle Simplification (Major)

**UWP:**
```csharp
public App()
{
    this.Suspending += OnSuspending; // Desktop apps don't suspend!
}

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
    {
        // Restore state
    }
}
```

**WinUI 3:**
```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // No PreviousExecutionState — desktop apps don't have it
    // No Suspending event — desktop apps don't suspend
    MainWindow = new MainWindow();
    MainWindow.Activate();
}
```

**Important:** `LaunchActivatedEventArgs` in WinUI 3 is `Microsoft.UI.Xaml.LaunchActivatedEventArgs`, NOT `Windows.ApplicationModel.Activation.LaunchActivatedEventArgs`. They're different types!

---

### 8. ⚠️ Local Storage API Changes (Major)

**UWP:**
```csharp
var folder = ApplicationData.Current.LocalFolder;
var file = await folder.CreateFileAsync("data.json", CreationCollisionOption.ReplaceExisting);
await FileIO.WriteTextAsync(file, json);
```

**WinUI 3 (unpackaged):**
```csharp
var path = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MyApp", "data.json");
Directory.CreateDirectory(Path.GetDirectoryName(path));
await File.WriteAllTextAsync(path, json);
```

**Why:** `ApplicationData.Current` requires package identity. Unpackaged WinUI 3 apps should use standard .NET file APIs. If your app IS packaged (MSIX), `ApplicationData.Current` still works.

---

### 9. 📝 Theme Resource Names Changed (Minor but Pervasive)

**UWP:**
```xml
Foreground="{ThemeResource SystemControlForegroundBaseMediumBrush}"
Background="{ThemeResource SystemControlBackgroundChromeMediumBrush}"
```

**WinUI 3:**
```xml
Foreground="{ThemeResource TextFillColorSecondaryBrush}"
Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
```

**Mapping reference:** https://learn.microsoft.com/windows/apps/design/style/xaml-theme-resources

---

### 10. 📝 x:Bind in WinUI 3 (Minor)

`x:Bind` works the same but has one key behavioral note:
- In UWP, `x:Bind` defaults to `Mode=OneTime`
- In WinUI 3, `x:Bind` ALSO defaults to `Mode=OneTime` (this is unchanged)
- Always specify `Mode=OneWay` or `Mode=TwoWay` for live data

---

### 11. 📝 BindableBase → CommunityToolkit.Mvvm (Recommended Modernization)

**UWP custom BindableBase:**
```csharp
public class BindableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string prop = null) => ...
    protected bool SetProperty<T>(ref T storage, T value, ...) => ...
}
```

**WinUI 3 with CommunityToolkit:**
```csharp
// Option A: Drop-in replacement (minimal changes)
public class MyViewModel : ObservableObject { }

// Option B: Source generators (modern, less boilerplate)
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name; // Auto-generates public Name property with change notification
    
    [RelayCommand]
    private void Save() { } // Auto-generates SaveCommand
}
```

---

## Architecture Decisions in This Migration

| Decision | Rationale |
|----------|-----------|
| Unpackaged app (`WindowsPackageType=None`) | Simpler deployment, no MSIX needed for a sample |
| `System.ServiceModel.Syndication` over `SyndicationClient` | Works in all deployment modes, standard .NET |
| `System.Text.Json` over `DataContractSerializer` | Modern, faster, better supported |
| Keep explicit properties (not `[ObservableProperty]`) | Easier to see the 1:1 mapping from UWP code |
| `NavigationView` with Frame navigation | Matches current WinUI 3 Gallery patterns |
| `InfoBar` instead of custom toast | Built-in WinUI 3 control for transient messages |

---

## UI/UX Design Inspiration Sources

### Where I Draw UI/UX Ideas From

1. **WinUI 3 Gallery App** (Primary source)
   - https://github.com/microsoft/WinUI-Gallery
   - Install from Microsoft Store — every WinUI 3 control with live demos
   - Shows proper spacing, theming, adaptive layout patterns

2. **Fluent Design System**
   - https://fluent2.microsoft.design/
   - Design tokens, spacing scales, typography ramp, color semantics
   - The "why" behind specific padding values and hierarchy

3. **Windows App SDK Design Guidance**
   - https://learn.microsoft.com/windows/apps/design/
   - Navigation patterns, input guidance, responsive layouts
   - Master-detail, command bar, and content layout blueprints

4. **Windows 11 Signature Experiences**
   - Settings app, Files app, Windows Terminal
   - Study how they handle: NavigationView, card-based layouts, InfoBar usage
   - Consistent 24px outer padding, 12-16px inner spacing

5. **Community Toolkit Sample App**
   - https://github.com/CommunityToolkit/Windows
   - Shows real-world usage of toolkit controls alongside core WinUI

6. **Design Principles Applied Here:**
   - **Progressive disclosure** — Feed list shows title+summary, detail pane shows full content
   - **Spatial hierarchy** — Larger type for titles, muted secondary text, tertiary timestamps
   - **Semantic colors** — Using `TextFillColorPrimary/Secondary/Tertiary` for text hierarchy
   - **Consistent density** — 12-16px item padding, 24px page padding (Windows 11 standard)
   - **Empty states** — Never show a blank screen; always communicate "no items" meaningfully
   - **Adaptive layout** — Master-detail collapses gracefully at narrow widths

---

## File Structure Comparison

```
ORIGINAL UWP (~50KB of navigation code)     →   WinUI 3 (~5KB total nav)
├── AppShell.xaml (12.8KB)                       ├── MainWindow.xaml (2.5KB)
├── AppShell.xaml.cs (12.4KB)                    ├── MainWindow.xaml.cs (2.9KB)
├── Controls/NavMenuListView.cs (11.7KB)         │   (DELETED - built into NavigationView)
├── Common/BindableBase.cs (2.8KB)               │   (DELETED - CommunityToolkit.Mvvm)
└── Views/MasterDetailPage.xaml                  └── Views/FeedPage.xaml (same pattern, modern)
```

**Net result: ~40KB of custom boilerplate code eliminated.**

---

## Quick Start

```bash
# Create from template (if starting fresh)
dotnet new winui3 -n RssReader

# Build
dotnet build

# Run
dotnet run
```

**Required NuGet packages:**
- `Microsoft.WindowsAppSDK` (1.6+)
- `CommunityToolkit.Mvvm` (8.x)
- `System.ServiceModel.Syndication` (included in .NET 8+)
