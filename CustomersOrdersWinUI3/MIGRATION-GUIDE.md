# Customers-Orders Database: WinUI 3 Migration Assessment

## Status: ALREADY MIGRATED ✅

The Customers-Orders Database sample has **already been migrated to WinUI 3 / Windows App SDK**.
This document serves as a reference for what was done and what pitfalls developers should watch for
when performing a similar multi-project enterprise migration.

## Current State (as of repo HEAD)

| Aspect | Value |
|--------|-------|
| Target Framework | `net6.0-windows10.0.22000.0` |
| Windows App SDK | `1.0.0` |
| CommunityToolkit | `CommunityToolkit.WinUI` 7.1.2 |
| EF Core | 6.0.4 |
| Microsoft.Graph | 4.25.0 |
| MSAL | `Microsoft.Identity.Client.Extensions.Msal` 2.20.1 |

## Recommended Updates (Modernization Pass)

While functional, packages are outdated. A modernization pass should upgrade:

| Package | Current | Recommended |
|---------|---------|-------------|
| `Microsoft.WindowsAppSDK` | 1.0.0 | 1.6.x |
| `CommunityToolkit.WinUI` | 7.1.2 | 8.x (CommunityToolkit.WinUI3) |
| `Microsoft.EntityFrameworkCore.Sqlite` | 6.0.4 | 8.x or 9.x |
| `Microsoft.Graph` | 4.25.0 | 5.x |
| Target Framework | net6.0 | net8.0 or net9.0 |

## What Was Already Migrated (Lessons for Developers)

### 1. Multi-Project Solution Structure Preserved
The original architecture (ContosoApp → ContosoModels → ContosoRepository → ContosoService) was
preserved. The key insight: **only the UI project needed WinUI 3 changes**. The Models and
Repository projects are plain .NET class libraries with no UI dependencies.

**Pitfall**: If your models or data layer reference `Windows.UI.Xaml` types (e.g., for
`INotifyPropertyChanged` using UWP's `DispatcherHelper`), you'll need to decouple those.

### 2. Window.Current → Static App.Window Pattern
```csharp
// UWP
Window.Current.Content = rootFrame;
Window.Current.Activate();

// WinUI 3 (what they did)
public static Window Window { get { return m_window; } }
private static Window m_window;

// In OnLaunched:
m_window = new MainWindow();
m_window.Content = shell;
m_window.Activate();
```

### 3. Custom Title Bar Migration
```csharp
// UWP
CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

// WinUI 3
App.Window.ExtendsContentIntoTitleBar = true;
App.Window.SetTitleBar(AppTitleBar); // XAML element
```

### 4. NavigationView Shell Pattern
The AppShell uses `NavigationView` with string-based item matching. This pattern translates
cleanly to WinUI 3 — the control API is nearly identical. The main difference is the namespace
change from `muxc = Microsoft.UI.Xaml.Controls` (which was already the MUXC namespace in UWP).

### 5. MSAL Authentication
The sample uses `MsalHelper.cs` with `Microsoft.Identity.Client`. In UWP, you'd use
`WebAccountProvider`. The current implementation already uses MSAL.NET directly, which is the
recommended approach for WinUI 3.

**Key pitfall for other apps**: UWP apps using `WebAccountProvider` or `WebAuthenticationBroker`
must migrate to MSAL.NET. WinUI 3 doesn't support `WebAuthenticationBroker` — you need
`WithWindowHandle(hwnd)` on the MSAL builder.

### 6. DataGrid via CommunityToolkit
The sample uses `CommunityToolkit.WinUI.UI.Controls` for DataGrid. This works in WinUI 3 but
the package name changed:
- UWP: `Microsoft.Toolkit.Uwp.UI.Controls`
- WinUI 3: `CommunityToolkit.WinUI.UI.Controls`

### 7. SQLite / EF Core
Database access via EF Core + SQLite works identically. The `ApplicationData.Current.LocalFolder`
API is available in packaged WinUI 3 apps.

**Pitfall**: Unpackaged WinUI 3 apps don't have `ApplicationData.Current`. Use
`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` instead.

### 8. ApplicationData.Current.LocalSettings
Used for storing the data source preference ("Rest" vs SQLite). This API works in packaged
WinUI 3 apps but **not in unpackaged apps**. For unpackaged, use `Microsoft.Extensions.Configuration`
or a JSON settings file.

## Migration Complexity: LOW
This was one of the cleanest UWP→WinUI 3 migrations because:
1. Models/data layers had no UI dependencies
2. Already used MSAL.NET (not WebAccountProvider)
3. NavigationView translates 1:1
4. No Composition layer, Win2D, or advanced inking
5. No BackgroundTask or LiveTile dependencies

## Key Takeaway for LLM-Assisted Migration
When an LLM encounters a multi-project UWP solution:
1. **Scan all .csproj files first** — only UI projects need WinUI 3 changes
2. **Check for existing WinAppSDK references** — the migration may already be done
3. **Focus on the namespace swap** in XAML (`Windows.UI.Xaml` → `Microsoft.UI.Xaml`)
4. **Check authentication patterns** — MSAL vs WebAccountProvider is a breaking change
5. **Verify storage APIs** — packaged vs unpackaged determines which APIs work
