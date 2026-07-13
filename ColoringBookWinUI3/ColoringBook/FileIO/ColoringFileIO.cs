using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ColoringBook.Models;
using Windows.UI.Input.Inking;

namespace ColoringBook.FileIO
{
    /// <summary>
    /// Handles reading/writing coloring session data to local storage.
    /// 
    /// MIGRATION NOTES:
    /// - UWP version used StorageFile/StorageFolder APIs throughout
    /// - WinUI 3 version uses System.IO for file operations (works unpackaged)
    /// - InkStroke serialization still uses InkStrokeContainer.SaveAsync/LoadAsync
    ///   with IRandomAccessStream, requiring Windows.Storage.Streams interop
    /// - JSON serialization migrated from DataContractJsonSerializer → System.Text.Json
    /// </summary>
    public static class ColoringFileIO
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Saves coloring settings (tool, color, stroke size) to a JSON file.
        /// </summary>
        public static async Task SaveSettingsAsync(string folderPath, ColoringSettings settings)
        {
            Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, "settings.json");
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Loads coloring settings from a JSON file.
        /// </summary>
        public static async Task<ColoringSettings> LoadSettingsAsync(string folderPath)
        {
            var filePath = Path.Combine(folderPath, "settings.json");
            if (!File.Exists(filePath))
                return new ColoringSettings();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ColoringSettings>(json, _jsonOptions)
                ?? new ColoringSettings();
        }

        /// <summary>
        /// Saves ink strokes to a GIF file (ISF format) using InkStrokeContainer.
        /// 
        /// MIGRATION NOTE: InkStrokeContainer.SaveAsync requires a
        /// Windows.Storage.Streams.IRandomAccessStream. The cleanest approach in
        /// WinUI 3 is to create a MemoryStream, save to it, then write bytes to disk.
        /// Alternatively, use StorageFile.OpenAsync for IRandomAccessStream.
        /// </summary>
        public static async Task SaveInkStrokesAsync(
            string folderPath,
            InkStrokeContainer container)
        {
            Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, "strokes.gif");

            using var memoryStream = new MemoryStream();
            using var raStream = memoryStream.AsRandomAccessStream();

            await container.SaveAsync(raStream);
            await raStream.FlushAsync();

            var bytes = memoryStream.ToArray();
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        /// <summary>
        /// Loads ink strokes from a GIF file (ISF format).
        /// </summary>
        public static async Task LoadInkStrokesAsync(
            string folderPath,
            InkStrokeContainer container)
        {
            var filePath = Path.Combine(folderPath, "strokes.gif");
            if (!File.Exists(filePath)) return;

            var bytes = await File.ReadAllBytesAsync(filePath);
            using var memoryStream = new MemoryStream(bytes);
            using var raStream = memoryStream.AsRandomAccessStream();

            await container.LoadAsync(raStream);
        }

        /// <summary>
        /// Creates a new coloring session folder with the library image copied in.
        /// </summary>
        public static async Task<string> CreateColoringSessionAsync(
            string libraryImagePath,
            string coloringId)
        {
            var sessionPath = Path.Combine(App.LocalDataPath, "Colorings", coloringId);
            Directory.CreateDirectory(sessionPath);

            // Copy the library image as the background template
            var destPath = Path.Combine(sessionPath, "template.png");
            File.Copy(libraryImagePath, destPath, overwrite: true);

            // Create default settings
            await SaveSettingsAsync(sessionPath, new ColoringSettings());

            return sessionPath;
        }
    }
}
