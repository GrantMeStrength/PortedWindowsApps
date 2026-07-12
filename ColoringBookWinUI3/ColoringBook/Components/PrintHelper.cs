using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Input.Inking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using Windows.Graphics.Printing;

namespace ColoringBook.Components
{
    /// <summary>
    /// Handles printing the current coloring.
    /// 
    /// MIGRATION NOTES (CRITICAL — printing is one of the most changed areas):
    /// 
    /// UWP approach:
    ///   var printMan = PrintManager.GetForCurrentView();
    ///   printMan.PrintTaskRequested += OnPrintTaskRequested;
    ///   await PrintManager.ShowPrintUIAsync();
    /// 
    /// WinUI 3 approach:
    ///   // Must get PrintManager via interop with the window handle
    ///   var hWnd = App.WindowHandle;
    ///   var printMan = PrintManagerInterop.GetForWindow(hWnd);
    ///   printMan.PrintTaskRequested += OnPrintTaskRequested;
    ///   await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
    /// 
    /// The PrintDocument and page rendering APIs are unchanged.
    /// The key difference is how you obtain the PrintManager instance —
    /// WinUI 3 requires the window handle via COM interop.
    /// </summary>
    public class PrintHelper
    {
        private readonly Canvas _printCanvas;
        private PrintDocument? _printDocument;
        private IPrintDocumentSource? _printDocumentSource;
        private List<UIElement>? _printPages;

        // COM interop for PrintManager in WinUI 3
        [ComImport, Guid("45b44032-fd76-4b0e-a768-4bb1457f3ab0")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPrintManagerInterop
        {
            IntPtr GetForWindow(IntPtr hwnd, [In] ref Guid riid);
            IntPtr ShowPrintUIForWindowAsync(IntPtr hwnd, [In] ref Guid riid);
        }

        public PrintHelper(Canvas printCanvas)
        {
            _printCanvas = printCanvas;
        }

        /// <summary>
        /// Registers the app for printing.
        /// </summary>
        public void RegisterForPrinting()
        {
            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;

            _printDocument.Paginate += OnPaginate;
            _printDocument.GetPreviewPage += OnGetPreviewPage;
            _printDocument.AddPages += OnAddPages;

            // Get PrintManager via window handle interop
            var printManager = PrintManagerInterop.GetForWindow(App.WindowHandle);
            printManager.PrintTaskRequested += OnPrintTaskRequested;
        }

        /// <summary>
        /// Shows the print UI.
        /// </summary>
        public async Task PrintAsync()
        {
            await PrintManagerInterop.ShowPrintUIForWindowAsync(App.WindowHandle);
        }

        /// <summary>
        /// Unregisters from printing.
        /// </summary>
        public void UnregisterForPrinting()
        {
            if (_printDocument != null)
            {
                _printDocument.Paginate -= OnPaginate;
                _printDocument.GetPreviewPage -= OnGetPreviewPage;
                _printDocument.AddPages -= OnAddPages;
            }

            try
            {
                var printManager = PrintManagerInterop.GetForWindow(App.WindowHandle);
                printManager.PrintTaskRequested -= OnPrintTaskRequested;
            }
            catch
            {
                // PrintManager may not be available
            }
        }

        /// <summary>
        /// Creates a print page from the coloring image and ink strokes.
        /// </summary>
        public void PreparePrintContent(
            BitmapImage backgroundImage,
            IReadOnlyList<InkStroke> strokes,
            double width,
            double height)
        {
            _printPages = new List<UIElement>();

            // Create a page with the coloring
            var pageGrid = new Grid
            {
                Width = width,
                Height = height
            };

            // Background image
            var bgImage = new Image
            {
                Source = backgroundImage,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform
            };
            pageGrid.Children.Add(bgImage);

            // Ink strokes rendered via InkCanvas
            var inkCanvas = new InkCanvas
            {
                Width = width,
                Height = height
            };

            foreach (var stroke in strokes)
            {
                inkCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
            }

            pageGrid.Children.Add(inkCanvas);

            _printPages.Add(pageGrid);
        }

        private void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            var printTask = args.Request.CreatePrintTask("Coloring Book", sourceRequestedArgs =>
            {
                sourceRequestedArgs.SetSource(_printDocumentSource);
            });
        }

        private void OnPaginate(object sender, PaginateEventArgs args)
        {
            _printDocument?.SetPreviewPageCount(_printPages?.Count ?? 0,
                PreviewPageCountType.Final);
        }

        private void OnGetPreviewPage(object sender, GetPreviewPageEventArgs args)
        {
            if (_printPages != null && args.PageNumber <= _printPages.Count)
            {
                _printDocument?.SetPreviewPage(args.PageNumber,
                    _printPages[args.PageNumber - 1]);
            }
        }

        private void OnAddPages(object sender, AddPagesEventArgs args)
        {
            if (_printPages != null)
            {
                foreach (var page in _printPages)
                {
                    _printDocument?.AddPage(page);
                }
            }
            _printDocument?.AddPagesComplete();
        }
    }
}
