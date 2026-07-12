# Ported Windows Apps

UWP sample applications migrated to WinUI 3 (Windows App SDK).

These are work-in-progress migrations intended to:
- Demonstrate modern WinUI 3 patterns (MVVM with CommunityToolkit, SDK-style projects, .NET 8)
- Document migration pitfalls for developers and AI tools
- Serve as reference implementations for the UWP → WinUI 3 migration guides on Microsoft Learn

## Samples

| Sample | Original (UWP) | Status | Key Technologies |
|--------|----------------|--------|-----------------|
| [RSS Reader](./RssReaderWinUI3/) | [Windows-appsample-rssreader](https://github.com/Microsoft/Windows-appsample-rssreader) | ✅ Complete | NavigationView, MVVM, HttpClient, WebView2 |
| [Photo Editor](./PhotoEditorWinUI3/) | [Windows-appsample-photo-editor](https://github.com/microsoft/Windows-appsample-photo-editor) | ✅ Complete | Win2D, image effects, file pickers, connected animations |
| [Coloring Book](./ColoringBookWinUI3/) | [Windows-appsample-coloringbook](https://github.com/microsoft/Windows-appsample-coloringbook) | ✅ Complete | InkCanvas, Win2D export, printing, undo/redo |
| [Customers Orders Database](./CustomersOrdersWinUI3/) | [Windows-appsample-customers-orders-database](https://github.com/Microsoft/Windows-appsample-customers-orders-database) | 📋 Assessment | Already migrated upstream; guide documents patterns |

## Migration Guides

Each sample includes a `MIGRATION-GUIDE.md` documenting:
- API substitutions performed
- Controls that required rework (no direct equivalent)
- Pitfalls for developers and LLMs
- Architecture decisions and rationale

## Common Patterns

All migrated samples follow these conventions:
- **.NET 8** with `net8.0-windows10.0.22621.0` target
- **Windows App SDK 1.6**
- **CommunityToolkit.Mvvm 8.4.0** (source generators, `ObservableObject`, `RelayCommand`)
- **SDK-style .csproj** (auto-include, no explicit file listings)
- **Static `App.MainWindow`** pattern for window handle access
- **System.IO** for file operations (works packaged and unpackaged)

## Status

⚠️ These samples are work-in-progress. They compile and demonstrate the architecture, but may need refinement before serving as polished public samples.

## License

MIT
