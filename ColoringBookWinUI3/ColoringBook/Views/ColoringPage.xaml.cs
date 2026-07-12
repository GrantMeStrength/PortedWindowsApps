using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColoringBook.Components;
using ColoringBook.FileIO;
using ColoringBook.Models;
using ColoringBook.UndoRedoOperations;
using ColoringBook.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace ColoringBook.Views
{
    /// <summary>
    /// The main coloring experience page.
    /// 
    /// MIGRATION NOTES (this page had the most UWP→WinUI 3 changes):
    /// 
    /// 1. SystemNavigationManager back button:
    ///    UWP: SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility
    ///    WinUI 3: No SystemNavigationManager. Use a back button in your own UI
    ///    (CommandBar or NavigationView). The back button is now in the toolbar.
    /// 
    /// 2. Window.Current.CoreWindow:
    ///    UWP used CoreWindow for pointer events in custom ink processing.
    ///    WinUI 3: Use InputPointerSource or standard XAML pointer events.
    /// 
    /// 3. InkCanvas behavior is nearly identical but namespace changed:
    ///    Windows.UI.Input.Inking → Microsoft.UI.Input.Inking
    /// 
    /// 4. Printing: See PrintHelper.cs for the interop approach.
    /// </summary>
    public sealed partial class ColoringPage : Page
    {
        public ColoringViewModel ViewModel { get; } = new();

        private CanvasInputController? _canvasController;
        private PrintHelper? _printHelper;
        private string _sessionPath = string.Empty;

        public ColoringPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string sessionPath)
            {
                _sessionPath = sessionPath;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize the ink canvas controller
            _canvasController = new CanvasInputController(DrawingCanvas, ViewModel);

            // Initialize printing
            _printHelper = new PrintHelper(PrintCanvas);
            _printHelper.RegisterForPrinting();

            // Load the session
            await LoadSessionAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _printHelper?.UnregisterForPrinting();
        }

        private async Task LoadSessionAsync()
        {
            if (string.IsNullOrEmpty(_sessionPath)) return;

            // Load background template image
            var templatePath = Path.Combine(_sessionPath, "template.png");
            if (File.Exists(templatePath))
            {
                var bitmap = new BitmapImage();
                bitmap.UriSource = new Uri(templatePath);
                BackgroundImage.Source = bitmap;
            }

            // Load saved ink strokes
            if (_canvasController != null)
            {
                await ColoringFileIO.LoadInkStrokesAsync(
                    _sessionPath, _canvasController.StrokeContainer);
            }

            // Load color/tool settings
            var settings = await ColoringFileIO.LoadSettingsAsync(_sessionPath);
            ViewModel.CurrentColor = settings.CurrentColor;
            ViewModel.CurrentTool = settings.CurrentTool;
            ViewModel.StrokeSize = settings.StrokeSize;

            _canvasController?.UpdateDrawingAttributes();
            ViewModel.HasUnsavedChanges = false;
        }

        // --- Toolbar event handlers ---

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.HasUnsavedChanges)
            {
                // Auto-save before navigating back
                _ = SaveAsync();
            }

            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void OnToolSelected(object sender, RoutedEventArgs e)
        {
            if (sender is not AppBarToggleButton button) return;

            // Uncheck all tool buttons, then check the clicked one
            PenButton.IsChecked = false;
            PencilButton.IsChecked = false;
            CalligraphyButton.IsChecked = false;
            EraserButton.IsChecked = false;
            button.IsChecked = true;

            ViewModel.SelectToolCommand.Execute(button.Tag?.ToString());
            _canvasController?.UpdateDrawingAttributes();
        }

        private void OnStrokeSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StrokeSizeCombo.SelectedItem is ComboBoxItem item
                && double.TryParse(item.Tag?.ToString(), out var size))
            {
                ViewModel.StrokeSize = size;
                _canvasController?.UpdateDrawingAttributes();
            }
        }

        private void OnColorSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is Windows.UI.Color color)
            {
                ViewModel.SelectColorCommand.Execute(color);
                _canvasController?.UpdateDrawingAttributes();
            }
        }

        private void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            var op = ViewModel.Undo();
            _canvasController?.ApplyUndoRedo(op, isUndo: true);
        }

        private void OnRedoClicked(object sender, RoutedEventArgs e)
        {
            var op = ViewModel.Redo();
            _canvasController?.ApplyUndoRedo(op, isUndo: false);
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrEmpty(_sessionPath) || _canvasController == null) return;

            // Save ink strokes
            await ColoringFileIO.SaveInkStrokesAsync(
                _sessionPath, _canvasController.StrokeContainer);

            // Save settings
            var settings = new ColoringSettings
            {
                CurrentColor = ViewModel.CurrentColor,
                CurrentTool = ViewModel.CurrentTool,
                StrokeSize = ViewModel.StrokeSize,
                Opacity = ViewModel.Opacity
            };
            await ColoringFileIO.SaveSettingsAsync(_sessionPath, settings);

            ViewModel.HasUnsavedChanges = false;
        }

        private async void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if (_canvasController == null) return;

            var templatePath = Path.Combine(_sessionPath, "template.png");
            await ColoringExporter.ExportToPngAsync(
                templatePath,
                _canvasController.GetAllStrokes(),
                (int)DrawingCanvas.ActualWidth,
                (int)DrawingCanvas.ActualHeight);
        }

        private async void OnPrintClicked(object sender, RoutedEventArgs e)
        {
            if (_printHelper == null || _canvasController == null) return;

            _printHelper.PreparePrintContent(
                BackgroundImage.Source as BitmapImage ?? new BitmapImage(),
                _canvasController.GetAllStrokes(),
                DrawingCanvas.ActualWidth,
                DrawingCanvas.ActualHeight);

            await _printHelper.PrintAsync();
        }

        private async void OnClearAllClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Clear All",
                Content = "Are you sure you want to clear all your coloring?",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot // CRITICAL: WinUI 3 requires XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                _canvasController?.ClearAll();
                ViewModel.HasUnsavedChanges = true;
            }
        }
    }
}
