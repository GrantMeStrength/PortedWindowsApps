// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PhotoEditor.Models;

/// <summary>
/// Represents a photo with editable effect properties.
/// Migrated from C++/WinRT runtime class (Photo.idl) to a standard C# ObservableObject.
/// Key migration note: No .idl files needed in C# — INotifyPropertyChanged is handled by
/// CommunityToolkit.Mvvm's [ObservableProperty] source generator.
/// </summary>
public partial class Photo : ObservableObject
{
    public Photo(string filePath, string name, string fileType, uint width, uint height)
    {
        FilePath = filePath;
        ImageName = name;
        ImageFileType = fileType;
        ImageWidth = width;
        ImageHeight = height;
        Saturation = 1.0f;
        SepiaIntensity = 0.5f;
    }

    // File properties (read-only after construction)
    public string FilePath { get; }
    public string ImageName { get; }
    public string ImageFileType { get; }
    public uint ImageWidth { get; }
    public uint ImageHeight { get; }

    public string ImageTitle
    {
        get => string.IsNullOrEmpty(_imageTitle) ? ImageName : _imageTitle;
        set => SetProperty(ref _imageTitle, value);
    }
    private string _imageTitle = string.Empty;

    public string ImageDimensions => $"{ImageWidth} x {ImageHeight}";

    // Thumbnail for gallery view (loaded lazily)
    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; }

    // Full image source for detail view
    [ObservableProperty]
    public partial BitmapImage? ImageSource { get; set; }

    // --- Effect properties ---
    // These map 1:1 to Win2D effect parameters.
    // In the original C++/WinRT, each had a manual getter/setter with PropertyChanged.
    // CommunityToolkit.Mvvm generates all that boilerplate.

    [ObservableProperty]
    public partial float Exposure { get; set; }

    [ObservableProperty]
    public partial float Temperature { get; set; }

    [ObservableProperty]
    public partial float Tint { get; set; }

    [ObservableProperty]
    public partial float Contrast { get; set; }

    [ObservableProperty]
    public partial float Saturation { get; set; }

    [ObservableProperty]
    public partial float BlurAmount { get; set; }

    [ObservableProperty]
    public partial float SepiaIntensity { get; set; }

    /// <summary>
    /// Resets all effects to default values.
    /// </summary>
    public void ResetAllEffects()
    {
        Exposure = 0;
        Temperature = 0;
        Tint = 0;
        Contrast = 0;
        Saturation = 1.0f;
        BlurAmount = 0;
        SepiaIntensity = 0.5f;
    }
}
