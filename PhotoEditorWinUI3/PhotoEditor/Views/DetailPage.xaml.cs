// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using PhotoEditor.Models;
using PhotoEditor.ViewModels;

namespace PhotoEditor.Views;

/// <summary>
/// Detail page with Win2D effects editor.
/// Migration notes:
/// - In C++/WinRT UWP, effects were applied live using CompositionEffectBrush.
///   The WinUI 3 C# approach renders to a bitmap (simpler, avoids raw composition).
/// - ConnectedAnimation "forwardAnimation" plays when arriving from MainPage.
/// - FileSavePicker needs WinRT interop initialization for the window handle.
/// - ContentDialog.XamlRoot must be set before ShowAsync() in WinUI 3.
/// </summary>
public sealed partial class DetailPage : Page
{
    public DetailViewModel ViewModel { get; } = new();

    // Throttle effect re-rendering to avoid lag during slider drags
    private DispatcherTimer? _renderTimer;
    private bool _renderPending;

    public DetailPage()
    {
        InitializeComponent();

        // Debounce timer — renders effect 200ms after last slider change
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _renderTimer.Tick += async (s, e) =>
        {
            _renderTimer.Stop();
            if (_renderPending)
            {
                _renderPending = false;
                await ViewModel.RenderWithEffectsAsync();
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Photo photo)
        {
            ViewModel.CurrentPhoto = photo;
            await ViewModel.LoadImageAsync();

            // Play connected animation from gallery
            var animation = ConnectedAnimationService.GetForCurrentView()
                .GetAnimation("forwardAnimation");
            animation?.TryStart(EditImage);
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);

        // Prepare back animation
        if (e.NavigationMode == NavigationMode.Back)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("backAnimation", EditImage);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        var window = App.MainAppWindow as MainWindow;
        if (window?.NavigationFrame.CanGoBack == true)
        {
            window.NavigationFrame.GoBack();
        }
    }

    private void Effect_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        // Debounce: restart timer on each slider change
        _renderPending = true;
        _renderTimer?.Stop();
        _renderTimer?.Start();
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetEffectsCommand.Execute(null);
        await ViewModel.RenderWithEffectsAsync();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveCommand.ExecuteAsync(null);
    }
}
