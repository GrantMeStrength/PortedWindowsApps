# Coloring Book: UWP → WinUI 3 Migration Guide

## Overview
The Coloring Book is the most complex sample in the collection. It exercises nearly every
UWP subsystem that changes in WinUI 3: InkCanvas, Win2D, printing, Pivot, custom title bar,
back navigation, undo/redo, file I/O, and localization.

**Original**: C#/UWP, 60+ source files, Win2D.uwp, Windows Ink, custom InkToolbar (32KB XAML)
**Migrated**: C#/WinUI 3 (.NET 8), Win2D (Microsoft.Graphics.Win2D), CommunityToolkit.Mvvm

---

## Migration Challenges (Ranked by Difficulty)

### 1. Pivot Control → TabView / RadioButtons (BREAKING)

**Impact**: HIGH — Pivot is NOT available in WinUI 3. No direct replacement.

```xml
<!-- UWP (Home.xaml was 38KB of Pivot content) -->
<Pivot>
    <PivotItem Header="Library">
        <!-- GridView of library images -->
    </PivotItem>
    <PivotItem Header="My Colorings">
        <!-- GridView of saved colorings -->
    </PivotItem>
</Pivot>
```

**WinUI 3 options** (we chose RadioButtons for simplicity):

| Option | Pros | Cons |
|--------|------|------|
| **TabView** | Closest to Pivot visually | Overkill for 2 fixed tabs, adds close/add buttons |
| **RadioButtons + visibility** | Simple, lightweight | Manual visibility toggling |
| **NavigationView (Top)** | System-standard pattern | Different mental model |
| **Segmented (CommunityToolkit)** | Clean segmented UI | Extra dependency |

```xml
<!-- WinUI 3: RadioButtons approach -->
<StackPanel Orientation="Horizontal">
    <RadioButton Content="Library" IsChecked="True" Checked="OnTabChanged" />
    <RadioButton Content="My Colorings" Checked="OnTabChanged" />
</StackPanel>
<!-- Then toggle GridView visibility in code-behind -->
```

**Key lesson for developers/LLMs**: When encountering `Pivot` in UWP XAML, don't search
for a "Pivot" equivalent — analyze how many items, whether they're fixed or dynamic, and
choose the appropriate WinUI 3 control.

---

### 2. Custom InkToolbar (32KB XAML → CommandBar)

**Impact**: HIGH — The UWP sample had a massively customized `InkToolbar`.

The UWP `ColoringBookInkToolbar` was 32KB of XAML with a 22KB code-behind:
- Custom pen/pencil/calligraphy buttons
- Custom color palette with 24+ colors
- Custom stroke size selector
- Integration with radial controller (Surface Dial)

**WinUI 3 approach**: Rebuild as a `CommandBar` with `AppBarToggleButton` items.

```csharp
// UWP: InkToolbar with custom pens
<InkToolbar TargetInkCanvas="{x:Bind DrawingCanvas}">
    <InkToolbarCustomPenButton ... />
    <!-- 800+ lines of custom XAML -->
</InkToolbar>

// WinUI 3: Standard CommandBar with tool buttons
<CommandBar>
    <AppBarToggleButton Icon="Edit" Label="Pen" Tag="Pen" Click="OnToolSelected" />
    <AppBarToggleButton Label="Pencil" Tag="Pencil" Click="OnToolSelected" />
    <!-- Much simpler, more maintainable -->
</CommandBar>
```

**Rationale**: The built-in `InkToolbar` exists in WinUI 3 but the UWP custom subclassing
pattern doesn't translate cleanly. A CommandBar gives us full control with less code.

---

### 3. Win2D Package Migration

**Impact**: MEDIUM — Package name changed, API surface is identical.

```xml
<!-- UWP .csproj -->
<PackageReference Include="Win2D.uwp" Version="1.25.0" />

<!-- WinUI 3 .csproj -->
<PackageReference Include="Microsoft.Graphics.Win2D" Version="1.2.1" />
```

All `Microsoft.Graphics.Canvas.*` APIs remain the same. The only code change is
removing `using Windows.UI.Composition` references if they were used for effects
(replaced by direct Win2D rendering).

**Key Win2D usage in this sample:**
- `CanvasDevice.GetSharedDevice()` — unchanged
- `CanvasRenderTarget` for compositing image + ink — unchanged
- `CanvasBitmap.LoadAsync()` — unchanged
- `ds.DrawInk(strokes)` — unchanged

---

### 4. Printing Interop (Window Handle Required)

**Impact**: HIGH — Complete API change in how you obtain PrintManager.

```csharp
// UWP
var printManager = PrintManager.GetForCurrentView();
await PrintManager.ShowPrintUIAsync();

// WinUI 3 — requires window handle interop
var hWnd = App.WindowHandle;
var printManager = PrintManagerInterop.GetForWindow(hWnd);
await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
```

**Why this is tricky**: `PrintManagerInterop` is a WinRT interop class, not a standard
.NET API. If you forget the window handle initialization, printing silently fails or
throws `COMException`. The `PrintDocument` page rendering APIs (`Paginate`, `GetPreviewPage`,
`AddPages`) are unchanged.

**LLM pitfall**: An LLM that learned UWP printing patterns will generate
`PrintManager.GetForCurrentView()` which compiles but throws at runtime.

---

### 5. SystemNavigationManager Back Button → Custom UI

**Impact**: MEDIUM — `SystemNavigationManager` doesn't exist in WinUI 3.

```csharp
// UWP
var navManager = SystemNavigationManager.GetForCurrentView();
navManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
navManager.BackRequested += OnBackRequested;

// WinUI 3 — use your own back button in the toolbar
<CommandBar.Content>
    <Button Click="OnBackClicked"
            Style="{StaticResource NavigationBackButtonNormalStyle}" />
</CommandBar.Content>
```

Or use `NavigationView` which has a built-in back button.

---

### 6. Window.Current → Static App.Window

**Impact**: MEDIUM — Used throughout the UWP codebase.

The Coloring Book used `Window.Current` in:
- `App.xaml.cs` (setting content)
- `NavigationController.cs` (getting the root frame)
- `CoreApplication.GetCurrentView().TitleBar` (custom title bar)

```csharp
// UWP
Window.Current.Content = rootFrame;
CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

// WinUI 3
App.MainAppWindow = new MainWindow();
App.MainAppWindow.ExtendsContentIntoTitleBar = true;
App.MainAppWindow.SetTitleBar(AppTitleBar);
```

**Pattern**: Expose `Window` as a static property on `App` and reference `App.MainAppWindow`
throughout the codebase. This is the standard WinUI 3 pattern.

---

### 7. Windows Ink Namespace Changes

**Impact**: LOW — API surface is identical, only namespace changed.

```csharp
// UWP
using Windows.UI.Input.Inking;

// WinUI 3
using Microsoft.UI.Input.Inking;
```

All ink APIs work the same:
- `InkCanvas.InkPresenter` ✅
- `InkPresenter.StrokesCollected` ✅
- `InkStrokeContainer.SaveAsync()` / `LoadAsync()` ✅
- `InkDrawingAttributes` ✅
- `ds.DrawInk(strokes)` (Win2D) ✅

**The one gotcha**: `CoreInputDeviceTypes` moved from `Windows.UI.Core` to
`Microsoft.UI.Core`. If you grep-replace `Windows.UI.Xaml` → `Microsoft.UI.Xaml`,
you might miss this one because it's not in the Xaml namespace.

---

### 8. File I/O: StorageFile → System.IO

**Impact**: MEDIUM — The UWP version used Windows.Storage APIs throughout.

```csharp
// UWP (8 FileIO classes using Windows.Storage)
var folder = ApplicationData.Current.LocalFolder;
var file = await folder.CreateFileAsync("settings.json");
await FileIO.WriteTextAsync(file, json);

// WinUI 3 (using System.IO — works unpackaged)
var path = Path.Combine(App.LocalDataPath, "settings.json");
await File.WriteAllTextAsync(path, json);
```

**Key decision**: We used `System.IO` instead of `Windows.Storage` because:
1. Works in both packaged and unpackaged apps
2. Simpler API (no async factory methods for basic file ops)
3. Better interop with .NET ecosystem (JSON serialization, etc.)

**Exception**: InkStroke serialization still requires `IRandomAccessStream`, so we use
`MemoryStream.AsRandomAccessStream()` as a bridge.

---

### 9. Serialization: DataContractJsonSerializer → System.Text.Json

**Impact**: LOW — Modernization improvement, not strictly required.

```csharp
// UWP
var serializer = new DataContractJsonSerializer(typeof(ColoringSettings));
serializer.WriteObject(stream, settings);

// WinUI 3
var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
await File.WriteAllTextAsync(filePath, json);
```

`System.Text.Json` is the modern .NET standard. `DataContractJsonSerializer` still works
but is not recommended for new code.

---

### 10. Project File Modernization (34KB → SDK-style)

**Impact**: LOW — Mechanical but important.

The UWP .csproj was 34KB with hundreds of explicit file includes. The SDK-style .csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250205002" />
    <PackageReference Include="Microsoft.Graphics.Win2D" Version="1.2.1" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>
</Project>
```

SDK-style projects auto-include all `.cs`, `.xaml` files — no manual listing needed.

---

### 11. RadialController (Surface Dial) — DROPPED

**Impact**: LOW (niche hardware) — The UWP `RadialController` API (`Windows.UI.Input.RadialController`)
requires interop in WinUI 3.

For this migration, we dropped radial controller support because:
1. Very few users have Surface Dial hardware
2. The interop is complex and not well-documented
3. The same functionality is available via keyboard shortcuts and the toolbar

If needed, WinUI 3 can access `RadialController` through WinRT interop:
```csharp
var controller = RadialController.CreateForCurrentView(); // UWP
// WinUI 3: Use RadialControllerInterop with window handle (undocumented)
```

---

### 12. ContentDialog.XamlRoot (Subtle but Critical)

**Impact**: MEDIUM — ContentDialog silently fails without XamlRoot in WinUI 3.

```csharp
// UWP — just works
var dialog = new ContentDialog { Title = "Clear All", ... };
await dialog.ShowAsync();

// WinUI 3 — MUST set XamlRoot
var dialog = new ContentDialog
{
    Title = "Clear All",
    XamlRoot = Content.XamlRoot  // ← Required!
};
await dialog.ShowAsync();
```

Without `XamlRoot`, you get: `WinRT originate error - The Content property must be set`.

---

## Architecture Decisions

### What Ported Cleanly (No Changes)
- **Undo/Redo system**: Pure C# command pattern — worked as-is after namespace change
- **Color palette model**: Plain data — no platform dependencies
- **Image loading**: `BitmapImage` works identically
- **Localization**: `.resw` files and `x:Uid` pattern carry forward

### What Required Significant Rework
- **Home page layout**: Pivot → RadioButtons + visibility switching
- **Custom InkToolbar**: 32KB XAML custom control → simple CommandBar
- **Print support**: Entire initialization flow changed (window handle interop)
- **File I/O**: 8 reader/writer classes → consolidated `ColoringFileIO` using System.IO
- **Navigation controller**: Static `Frame` + `Window.Current` → `MainWindow.AppFrame`

### What Was Dropped
- **RadialController**: Niche hardware, complex interop, not well-documented
- **Suspending event handler**: WinUI 3 doesn't use `Application.Suspending` the same way;
  auto-save on navigation instead

---

## New Documentation Gaps Found

These topics are missing or insufficient in the current UWP→WinUI 3 migration docs:

1. **Pivot control removal** — No guidance on which WinUI 3 control to use as a replacement
2. **Printing interop** — PrintManagerInterop pattern not documented in migration context
3. **Custom InkToolbar migration** — InkToolbar subclassing patterns don't translate
4. **RadialController interop** — No documentation for WinUI 3 radial controller access
5. **SystemNavigationManager removal** — Back button alternatives not documented in migration guide
6. **IRandomAccessStream bridging** — How to bridge System.IO streams with WinRT stream APIs
7. **`InkCanvas` XAML namespace in WinUI 3** — `InkCanvas` is not in the default XAML namespace and requires an explicit `xmlns:controls="using:Microsoft.UI.Xaml.Controls"` declaration; this isn't called out in migration docs.
8. **Win2D version staleness** — Old patch versions (e.g. 1.2.1) are periodically yanked from NuGet; docs don't warn about this or recommend a safe version pinning strategy.
