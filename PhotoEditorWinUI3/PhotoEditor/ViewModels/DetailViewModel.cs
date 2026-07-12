// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml.Media.Imaging;
using PhotoEditor.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace PhotoEditor.ViewModels;

/// <summary>
/// ViewModel for the detail/editing page.
/// Migration notes:
/// - Win2D effects in UWP used Composition layer (CompositionEffectBrush).
///   In WinUI 3 C#, we render effects using CanvasDevice + CanvasBitmap directly,
///   which is simpler and more portable.
/// - FileSavePicker requires InitializeWithWindow (WinRT interop) in WinUI 3.
/// - The C++/WinRT version used std::variant to hold heterogeneous effects.
///   C# can use polymorphism or just apply effects in sequence via ICanvasImage.
/// </summary>
public partial class DetailViewModel : ObservableObject
{
    [ObservableProperty]
    private Photo? _currentPhoto;

    [ObservableProperty]
    private BitmapImage? _displayImage;

    [ObservableProperty]
    private bool _isEditMode = true;

    private CanvasBitmap? _originalBitmap;
    private CanvasDevice? _device;

    public async Task LoadImageAsync()
    {
        if (CurrentPhoto == null) return;

        _device = CanvasDevice.GetSharedDevice();
        var file = await StorageFile.GetFileFromPathAsync(CurrentPhoto.FilePath);

        using var stream = await file.OpenReadAsync();
        _originalBitmap = await CanvasBitmap.LoadAsync(_device, stream);

        await RenderWithEffectsAsync();
    }

    /// <summary>
    /// Applies the current effect settings and renders the result to a BitmapImage.
    /// This replaces the Composition-layer approach from UWP which used:
    ///   CompositionEffectBrush + CompositionSurfaceBrush + SpriteVisual
    /// In WinUI 3 C# we use Win2D's software rendering pipeline directly.
    /// </summary>
    public async Task RenderWithEffectsAsync()
    {
        if (_originalBitmap == null || _device == null || CurrentPhoto == null) return;

        // Build the effects chain — equivalent to CreateEffectsGraph() in C++/WinRT
        ICanvasImage currentEffect = _originalBitmap;

        // Exposure
        if (CurrentPhoto.Exposure != 0)
        {
            currentEffect = new ExposureEffect
            {
                Source = currentEffect,
                Exposure = CurrentPhoto.Exposure
            };
        }

        // Contrast
        if (CurrentPhoto.Contrast != 0)
        {
            currentEffect = new ContrastEffect
            {
                Source = currentEffect,
                Contrast = CurrentPhoto.Contrast
            };
        }

        // Temperature and Tint
        if (CurrentPhoto.Temperature != 0 || CurrentPhoto.Tint != 0)
        {
            currentEffect = new TemperatureAndTintEffect
            {
                Source = currentEffect,
                Temperature = CurrentPhoto.Temperature,
                Tint = CurrentPhoto.Tint
            };
        }

        // Saturation (default is 1.0, only apply if changed)
        if (Math.Abs(CurrentPhoto.Saturation - 1.0f) > 0.01f)
        {
            currentEffect = new SaturationEffect
            {
                Source = currentEffect,
                Saturation = CurrentPhoto.Saturation
            };
        }

        // Gaussian Blur
        if (CurrentPhoto.BlurAmount > 0)
        {
            currentEffect = new GaussianBlurEffect
            {
                Source = currentEffect,
                BlurAmount = CurrentPhoto.BlurAmount,
                BorderMode = EffectBorderMode.Hard
            };
        }

        // Sepia (default intensity 0.5 means half-applied)
        if (CurrentPhoto.SepiaIntensity > 0 && CurrentPhoto.SepiaIntensity < 1.0f)
        {
            currentEffect = new SepiaEffect
            {
                Source = currentEffect,
                Intensity = CurrentPhoto.SepiaIntensity
            };
        }

        // Render to a bitmap
        using var renderTarget = new CanvasRenderTarget(
            _device,
            (float)_originalBitmap.SizeInPixels.Width,
            (float)_originalBitmap.SizeInPixels.Height,
            _originalBitmap.Dpi);

        using (var session = renderTarget.CreateDrawingSession())
        {
            session.DrawImage(currentEffect);
        }

        // Convert to BitmapImage for display
        using var memStream = new InMemoryRandomAccessStream();
        await renderTarget.SaveAsync(memStream, CanvasBitmapFileFormat.Png);
        memStream.Seek(0);

        var bitmapImage = new BitmapImage();
        await bitmapImage.SetSourceAsync(memStream);
        DisplayImage = bitmapImage;
    }

    [RelayCommand]
    private void ResetEffects()
    {
        CurrentPhoto?.ResetAllEffects();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_originalBitmap == null || _device == null || CurrentPhoto == null) return;

        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.SuggestedFileName = $"{CurrentPhoto.ImageName}_edited";
        picker.FileTypeChoices.Add("JPEG Image", new List<string> { ".jpg" });
        picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });

        // WinUI 3 migration: FileSavePicker requires window handle initialization
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file == null) return;

        // Re-render at full quality and save
        ICanvasImage finalEffect = BuildEffectsChain();

        using var renderTarget = new CanvasRenderTarget(
            _device,
            (float)_originalBitmap.SizeInPixels.Width,
            (float)_originalBitmap.SizeInPixels.Height,
            _originalBitmap.Dpi);

        using (var session = renderTarget.CreateDrawingSession())
        {
            session.DrawImage(finalEffect);
        }

        using var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
        var format = file.FileType.ToLowerInvariant() == ".png"
            ? CanvasBitmapFileFormat.Png
            : CanvasBitmapFileFormat.Jpeg;
        await renderTarget.SaveAsync(fileStream, format);
    }

    private ICanvasImage BuildEffectsChain()
    {
        if (_originalBitmap == null) return _originalBitmap!;

        ICanvasImage current = _originalBitmap;

        if (CurrentPhoto!.Exposure != 0)
            current = new ExposureEffect { Source = current, Exposure = CurrentPhoto.Exposure };
        if (CurrentPhoto.Contrast != 0)
            current = new ContrastEffect { Source = current, Contrast = CurrentPhoto.Contrast };
        if (CurrentPhoto.Temperature != 0 || CurrentPhoto.Tint != 0)
            current = new TemperatureAndTintEffect { Source = current, Temperature = CurrentPhoto.Temperature, Tint = CurrentPhoto.Tint };
        if (Math.Abs(CurrentPhoto.Saturation - 1.0f) > 0.01f)
            current = new SaturationEffect { Source = current, Saturation = CurrentPhoto.Saturation };
        if (CurrentPhoto.BlurAmount > 0)
            current = new GaussianBlurEffect { Source = current, BlurAmount = CurrentPhoto.BlurAmount, BorderMode = EffectBorderMode.Hard };
        if (CurrentPhoto.SepiaIntensity > 0 && CurrentPhoto.SepiaIntensity < 1.0f)
            current = new SepiaEffect { Source = current, Intensity = CurrentPhoto.SepiaIntensity };

        return current;
    }
}
