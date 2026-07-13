// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoEditor.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoEditor.ViewModels;

/// <summary>
/// ViewModel for the main gallery page.
/// Migration notes:
/// - KnownFolders.PicturesLibrary requires broadFileSystemAccess in packaged apps,
///   or picturesLibrary capability. In unpackaged WinUI 3 we use Environment.GetFolderPath
///   or FileOpenPicker instead (no capability system).
/// - co_await/co_return (C++ coroutines) → async/await (C# tasks)
/// - IObservableVector (C++/WinRT) → ObservableCollection (C#)
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<Photo> Photos { get; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasNoPhotos { get; set; }

    [RelayCommand]
    private async Task LoadPhotosAsync()
    {
        if (Photos.Count > 0) return;

        IsLoading = true;
        HasNoPhotos = false;

        try
        {
            // Migration: In UWP this used KnownFolders.PicturesLibrary with file query.
            // In WinUI 3 (unpackaged), use the .NET API directly.
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            if (!Directory.Exists(picturesPath))
            {
                HasNoPhotos = true;
                return;
            }

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var files = Directory.EnumerateFiles(picturesPath, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Take(100); // Limit for performance

            foreach (var filePath in files)
            {
                try
                {
                    var photo = await CreatePhotoFromFileAsync(filePath);
                    if (photo != null)
                    {
                        Photos.Add(photo);
                    }
                }
                catch
                {
                    // Skip files we can't read (corrupt, locked, etc.)
                }
            }

            HasNoPhotos = Photos.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task<Photo?> CreatePhotoFromFileAsync(string filePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var props = await file.Properties.GetImagePropertiesAsync();

        if (props.Width == 0 || props.Height == 0) return null;

        var photo = new Photo(
            filePath,
            Path.GetFileNameWithoutExtension(filePath),
            Path.GetExtension(filePath).ToUpperInvariant(),
            props.Width,
            props.Height);

        // Load thumbnail directly from file stream.
        // Migration: GetThumbnailAsync relies on Shell COM providers (REGDB_E_CLASSNOTREG)
        // which are unreliable in unpackaged WinUI 3 apps. Loading from the file stream
        // with DecodePixelWidth avoids the Shell COM layer entirely.
        using var stream = File.OpenRead(filePath);
        var bitmapImage = new BitmapImage { DecodePixelWidth = 300 };
        await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
        photo.Thumbnail = bitmapImage;

        return photo;
    }
}
