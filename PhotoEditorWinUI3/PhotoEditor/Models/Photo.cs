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
    private BitmapImage? _thumbnail;

    // Full image source for detail view
    [ObservableProperty]
    private BitmapImage? _imageSource;

    // --- Effect properties ---
    // These map 1:1 to Win2D effect parameters.
    // In the original C++/WinRT, each had a manual getter/setter with PropertyChanged.
    // CommunityToolkit.Mvvm generates all that boilerplate.

    [ObservableProperty]
    private float _exposure;

    [ObservableProperty]
    private float _temperature;

    [ObservableProperty]
    private float _tint;

    [ObservableProperty]
    private float _contrast;

    [ObservableProperty]
    private float _saturation = 1.0f;

    [ObservableProperty]
    private float _blurAmount;

    [ObservableProperty]
    private float _sepiaIntensity = 0.5f;

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
