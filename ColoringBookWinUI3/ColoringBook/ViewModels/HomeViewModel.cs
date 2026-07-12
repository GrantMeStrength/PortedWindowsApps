using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ColoringBook.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ColoringBook.ViewModels
{
    /// <summary>
    /// ViewModel for the home page showing library images and saved colorings.
    /// 
    /// MIGRATION NOTE (Pivot → TabView): The UWP version used Pivot control with PivotItems.
    /// Pivot is not available in WinUI 3. This ViewModel supports a TabView-based layout
    /// where "Library" and "My Colorings" are separate tabs. The data binding approach
    /// is identical — only the view changes.
    /// </summary>
    public partial class HomeViewModel : ObservableObject
    {
        public ObservableCollection<LibraryImage> LibraryImages { get; } = new();
        public ObservableCollection<Coloring> SavedColorings { get; } = new();

        [ObservableProperty]
        private LibraryImage? _selectedLibraryImage;

        [ObservableProperty]
        private Coloring? _selectedColoring;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _selectedTabIndex;

        [RelayCommand]
        private async Task LoadLibraryAsync()
        {
            IsLoading = true;

            try
            {
                LibraryImages.Clear();

                // Load built-in library images from Assets folder
                var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Library");
                if (Directory.Exists(assetsPath))
                {
                    var images = Directory.GetFiles(assetsPath, "*.png")
                        .Concat(Directory.GetFiles(assetsPath, "*.jpg"))
                        .OrderBy(f => f);

                    int index = 0;
                    foreach (var imagePath in images)
                    {
                        var name = Path.GetFileNameWithoutExtension(imagePath);
                        var libraryImage = new LibraryImage
                        {
                            Id = name,
                            Title = FormatTitle(name),
                            ImagePath = imagePath,
                            RowSpan = (index % 3 == 0) ? 2 : 1,
                            ColSpan = (index % 5 == 0) ? 2 : 1
                        };

                        // Load thumbnail
                        var bitmap = new BitmapImage();
                        bitmap.DecodePixelWidth = 300;
                        bitmap.UriSource = new Uri(imagePath);
                        libraryImage.Thumbnail = bitmap;

                        LibraryImages.Add(libraryImage);
                        index++;
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadSavedColoringsAsync()
        {
            IsLoading = true;

            try
            {
                SavedColorings.Clear();

                var coloringsPath = Path.Combine(App.LocalDataPath, "Colorings");
                if (!Directory.Exists(coloringsPath)) return;

                foreach (var dir in Directory.GetDirectories(coloringsPath))
                {
                    var thumbnailPath = Path.Combine(dir, "thumbnail.png");
                    var coloring = new Coloring
                    {
                        Id = Path.GetFileName(dir),
                        Title = FormatTitle(Path.GetFileName(dir)),
                        FolderPath = dir,
                        IsNew = false
                    };

                    if (File.Exists(thumbnailPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.DecodePixelWidth = 200;
                        bitmap.UriSource = new Uri(thumbnailPath);
                        coloring.Thumbnail = bitmap;
                    }

                    SavedColorings.Add(coloring);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void DeleteColoring(Coloring coloring)
        {
            if (coloring?.FolderPath != null && Directory.Exists(coloring.FolderPath))
            {
                Directory.Delete(coloring.FolderPath, recursive: true);
                SavedColorings.Remove(coloring);
            }
        }

        private static string FormatTitle(string fileName)
        {
            return fileName
                .Replace("-", " ")
                .Replace("_", " ");
        }
    }
}
