using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Input.Inking;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ColoringBook.Models
{
    /// <summary>
    /// Handles exporting coloring artwork to image files.
    /// 
    /// MIGRATION NOTES:
    /// - FileSavePicker requires InitializeWithWindow in WinUI 3
    /// - Win2D CanvasDevice/CanvasBitmap APIs are identical
    /// - InkStroke serialization via InkStrokeContainer.SaveAsync is unchanged
    /// </summary>
    public static class ColoringExporter
    {
        /// <summary>
        /// Exports the current coloring to a PNG file.
        /// </summary>
        public static async Task<bool> ExportToPngAsync(
            string backgroundImagePath,
            IReadOnlyList<InkStroke> strokes,
            int width,
            int height)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();

            // CRITICAL WinUI 3 MIGRATION: Must initialize picker with window handle
            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });
            picker.SuggestedFileName = "MyColoring";

            var file = await picker.PickSaveFileAsync();
            if (file == null) return false;

            var device = CanvasDevice.GetSharedDevice();

            using var renderTarget = new CanvasRenderTarget(device, width, height, 96);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                // Draw background image
                if (File.Exists(backgroundImagePath))
                {
                    using var stream = File.OpenRead(backgroundImagePath);
                    using var raStream = stream.AsRandomAccessStream();
                    var bgBitmap = await CanvasBitmap.LoadAsync(device, raStream);
                    ds.DrawImage(bgBitmap, new Windows.Foundation.Rect(0, 0, width, height));
                }

                // Draw ink strokes
                if (strokes.Count > 0)
                {
                    ds.DrawInk(strokes);
                }
            }

            // Save to file
            using var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
            await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png);

            return true;
        }
    }
}
