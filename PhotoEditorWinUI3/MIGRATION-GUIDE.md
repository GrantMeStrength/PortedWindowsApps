# Photo Editor: UWP C++/WinRT → WinUI 3 C# Migration Guide

## Overview

This migration converts the [Windows-appsample-photo-editor](https://github.com/microsoft/Windows-appsample-photo-editor) from a **C++/WinRT UWP** app to a **C# WinUI 3** app targeting .NET 8. This is a more complex migration than a simple 1:1 port — it also changes the implementation language to provide a modern C# reference sample.

> **Note:** Microsoft already documents the C++/WinRT→WinUI 3 path for this sample in [Case Study 2](https://learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-2). This C# rewrite offers an alternative for developers who prefer .NET.

---

## Key Migration Challenges

### 1. Win2D Package Change

| UWP | WinUI 3 |
|-----|---------|
| `Win2D.uwp` NuGet | `Microsoft.Graphics.Win2D` NuGet (1.2.x+) |
| `Microsoft.Graphics.Canvas` namespace | Same namespace (compatible) |
| Requires `CanvasDevice.GetSharedDevice()` works differently | Same API, different underlying plumbing |

**Pitfall:** The WinUI 3 Win2D package is `Microsoft.Graphics.Win2D`, NOT `Win2D.uwp`. If you reference the old package, you'll get runtime COM exceptions.

```xml
<!-- ✅ Correct for WinUI 3 -->
<PackageReference Include="Microsoft.Graphics.Win2D" Version="1.3.0" />

<!-- ❌ UWP only - will not work -->
<PackageReference Include="Win2D.uwp" Version="1.27.0" />
```

> **Pitfall:** Older patch versions of `Microsoft.Graphics.Win2D` (e.g. 1.2.1) are periodically removed from NuGet. Pin to the latest available or use `1.*` to avoid restore failures. If you see `WinRT.Runtime` assembly resolution errors, this is almost always the root cause — it's a downstream symptom of a Win2D version mismatch, not a missing package on its own.

---

### 2. Composition Effects → CanvasBitmap Rendering

The original uses Windows.UI.Composition layer:
```cpp
// UWP C++/WinRT — builds effects via Composition layer
auto effect = ref new GaussianBlurEffect();
effect->Source = CompositionEffectSourceParameter(L"backdrop");
auto brush = compositor.CreateEffectFactory(effect).CreateBrush();
auto visual = compositor.CreateSpriteVisual();
visual.Brush(brush);
```

The WinUI 3 C# version renders directly with Win2D:
```csharp
// WinUI 3 C# — renders effects chain to a bitmap
ICanvasImage current = _originalBitmap;
current = new GaussianBlurEffect { Source = current, BlurAmount = 5 };

using var renderTarget = new CanvasRenderTarget(device, width, height, dpi);
using (var session = renderTarget.CreateDrawingSession())
{
    session.DrawImage(current);
}
```

**Why the change:** The Composition approach requires obtaining a `Compositor` (harder in WinUI 3 where `Window.Current` doesn't exist) and is more verbose in C#. Direct Win2D rendering is simpler, more testable, and avoids COM interop complexities.

---

### 3. IDL Files → Standard C# Classes

C++/WinRT requires `.idl` files to define runtime classes:
```idl
// Photo.idl (UWP C++/WinRT)
runtimeclass Photo : Windows.UI.Xaml.Data.INotifyPropertyChanged
{
    String ImageName{ get; };
    Single Exposure;
    Single Contrast;
    // ... etc
}
```

In C# with CommunityToolkit.Mvvm, this becomes:
```csharp
// Photo.cs (WinUI 3 C#)
public partial class Photo : ObservableObject
{
    [ObservableProperty]
    private float _exposure;

    [ObservableProperty]
    private float _contrast;
}
```

**Key insight:** C++/WinRT developers must write IDL + .h + .cpp for each property. C# source generators eliminate all that boilerplate. This is a major productivity win.

---

### 4. FileSavePicker Requires Window Handle

```csharp
// UWP — just works
var picker = new FileSavePicker();
var file = await picker.PickSaveFileAsync();

// WinUI 3 — must initialize with window handle
var picker = new FileSavePicker();
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow!);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
var file = await picker.PickSaveFileAsync();
```

**Pitfall:** Forgetting `InitializeWithWindow` causes a silent failure or COM exception — the picker simply won't appear.

---

### 5. KnownFolders → Direct File System Access

```csharp
// UWP — required picturesLibrary capability in manifest
var folder = KnownFolders.PicturesLibrary;
var files = await folder.GetFilesAsync();

// WinUI 3 (unpackaged) — use .NET APIs directly
var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
var files = Directory.EnumerateFiles(picturesPath, "*.*");
```

For **packaged** WinUI 3 apps, you still need `broadFileSystemAccess` capability to use `KnownFolders`. The simpler approach for a photo editor is `FileOpenPicker`.

---

### 6. Connected Animations

Connected animations still work in WinUI 3, but the API access changes:

```csharp
// UWP
ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("anim", element);

// WinUI 3 — same API! (it's in Microsoft.UI.Xaml.Media.Animation)
ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("anim", element);
```

**Good news:** This is one of the rare APIs that migrated with zero code changes (just a namespace swap if using fully-qualified names).

---

### 7. Window.Current → Static Window Reference

```csharp
// UWP — global Window.Current
var compositor = Window.Current.Compositor;

// WinUI 3 — no global. Use static pattern or pass explicitly.
public partial class App : Application
{
    public static Window? MainAppWindow { get; private set; }
}
```

---

### 8. C++ Coroutines → C# async/await

```cpp
// C++/WinRT — coroutines with co_await and IAsyncOperation
IAsyncOperation<StorageFile> SaveAsync()
{
    auto picker = FileSavePicker();
    auto file = co_await picker.PickSaveFileAsync();
    co_return file;
}
```

```csharp
// C# — native async/await
async Task<StorageFile?> SaveAsync()
{
    var picker = new FileSavePicker();
    var file = await picker.PickSaveFileAsync();
    return file;
}
```

---

### 9. Effect Rendering Performance

**Problem:** Applying 6-7 effects and re-rendering a full-resolution image on every slider tick causes UI lag.

**Solution:** Debounce the rendering:
```csharp
private DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };

private void Effect_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
{
    _renderPending = true;
    _renderTimer.Stop();
    _renderTimer.Start(); // Restarts the 200ms countdown
}
```

---

### 10. ParallaxView

```xml
<!-- Works identically in UWP and WinUI 3 -->
<ParallaxView VerticalShift="50" Source="{x:Bind MyScrollViewer}">
    <Image Source="ms-appx:///Assets/bg1.png" Stretch="UniformToFill" />
</ParallaxView>
```

No changes needed — this control migrated cleanly.

---

## Architecture Comparison

| Aspect | UWP C++/WinRT | WinUI 3 C# |
|--------|---------------|-------------|
| Language | C++/WinRT | C# 12 / .NET 8 |
| MVVM framework | Manual INotifyPropertyChanged | CommunityToolkit.Mvvm |
| Effects rendering | Composition layer (CompositionEffectBrush) | Win2D CanvasRenderTarget |
| Property system | IDL + generated headers | [ObservableProperty] source gen |
| Async | co_await / co_return | async/await |
| Image loading | StorageItemThumbnail | Same (WinRT interop) |
| File picker | Direct use | InitializeWithWindow required |
| Target | UWP (Windows 10) | Windows App SDK 1.6+ |

### 11. Namespace Swap: `Windows.UI.Xaml` → `Microsoft.UI.Xaml`

Every UWP XAML type moves to the `Microsoft.UI.Xaml` namespace hierarchy in WinUI 3. This includes controls like `Frame`, `Page`, `NavigationView`, and their sub-namespaces:

| UWP namespace | WinUI 3 namespace |
|---------------|-------------------|
| `Windows.UI.Xaml` | `Microsoft.UI.Xaml` |
| `Windows.UI.Xaml.Controls` | `Microsoft.UI.Xaml.Controls` |
| `Windows.UI.Xaml.Media.Animation` | `Microsoft.UI.Xaml.Media.Animation` |
| `Windows.UI.Xaml.Navigation` | `Microsoft.UI.Xaml.Navigation` |

**Pitfall:** The error `The type or namespace name 'Frame' could not be found` (or any other XAML control) almost always means `using Microsoft.UI.Xaml.Controls;` is missing. Visual Studio's "add using" suggestion may offer the wrong `Windows.UI.Xaml.Controls` namespace — both exist in the build graph, but only `Microsoft.UI.Xaml.Controls` works at runtime in WinUI 3.

```csharp
// ❌ UWP — wrong namespace in WinUI 3
using Windows.UI.Xaml.Controls;

// ✅ WinUI 3
using Microsoft.UI.Xaml.Controls;
```

### 12. `[RelayCommand]` Generates a Command Property, Not a Public Method

`CommunityToolkit.Mvvm`'s `[RelayCommand]` attribute source-generates a **public command property**, not a public wrapper method. The decorated method stays `private`.

```csharp
// ViewModel
[RelayCommand]
private async Task LoadPhotosAsync() { ... }
// ↑ generates: public AsyncRelayCommand LoadPhotosCommand { get; }
```

```csharp
// ❌ CS0122 — 'LoadPhotosAsync' is inaccessible due to its protection level
await ViewModel.LoadPhotosAsync();

// ✅ Use the generated command
await ViewModel.LoadPhotosCommand.ExecuteAsync(null);

// ✅ Or bind it in XAML (the idiomatic approach)
// <Button Command="{x:Bind ViewModel.LoadPhotosCommand}" />
```

**Pitfall for LLMs:** When generating code-behind that calls ViewModel methods, always check whether the method carries `[RelayCommand]`. If it does, the correct call site is the generated `*Command` property, not the method itself.

---

## Documentation Gaps Found

During this migration, the following gaps were noted in official docs:

1. **Win2D WinUI 3 migration path** — No clear guidance on switching from `Win2D.uwp` to `Microsoft.Graphics.Win2D` or the Composition→CanvasBitmap rendering approach change.
2. **C++/WinRT → C# language migration** — Existing case study preserves C++. No guidance for teams wanting to rewrite in C# simultaneously.
3. **Effect performance patterns** — No docs on debouncing Win2D re-renders during slider input.
4. **Namespace swap confusion** — The `Windows.UI.Xaml.*` → `Microsoft.UI.Xaml.*` rename is mentioned in migration docs but the IDE tooling (IntelliSense/quick-fix) can silently suggest the wrong namespace, causing runtime failures that are hard to diagnose.
5. **`[RelayCommand]` access pattern** — CommunityToolkit docs don't prominently warn that the decorated method remains private; developers and LLMs frequently call the method directly and get CS0122.

---

## UI/UX Design Decisions

The migrated version preserves the original's gallery→detail navigation flow but modernizes:

- **WinUI 3 design tokens**: Uses `CardBackgroundFillColorDefaultBrush`, `BodyStrongTextBlockStyle` etc. from the WinUI 3 resource system
- **Mica/Acrylic backdrop**: Available via `SystemBackdrop` on Window (could be added)
- **Effect panel**: Right-side panel with slider controls (matches modern photo editing UX — Lightroom, Apple Photos)
- **Connected animations**: Preserved for fluid gallery↔detail transitions

**Design inspiration sources:**
- Windows 11 Photos app (gallery grid layout, effect sliders)
- Adobe Lightroom (right-panel adjustment controls)
- Fluent Design System principles (depth, motion, material)
