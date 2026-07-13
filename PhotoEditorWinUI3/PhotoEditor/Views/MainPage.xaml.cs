// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using PhotoEditor.Models;
using PhotoEditor.ViewModels;

namespace PhotoEditor.Views;

/// <summary>
/// Gallery page showing photo thumbnails.
/// Migration notes:
/// - ConnectedAnimationService.GetForCurrentView() still works in WinUI 3.
/// - Window.Current.Compositor → not needed; we don't use Composition implicit
///   animations in the C# version (use XAML animations instead).
/// - OnContainerContentChanging phased loading → simplified with x:Bind thumbnail.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();
    private Photo? _persistedItem;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadPhotosCommand.ExecuteAsync(null);
    }

    private void ImageGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Photo photo)
        {
            _persistedItem = photo;

            // Prepare connected animation for transition to detail page
            ImageGridView.PrepareConnectedAnimation("forwardAnimation", photo, "ItemImage");

            var window = App.MainAppWindow as MainWindow;
            window?.NavigationFrame.Navigate(typeof(DetailPage), photo,
                new SuppressNavigationTransitionInfo());
        }
    }

    private async void ImageGridView_Loaded(object sender, RoutedEventArgs e)
    {
        // Play connected animation on back navigation
        if (_persistedItem != null)
        {
            ImageGridView.ScrollIntoView(_persistedItem);
            var animation = ConnectedAnimationService.GetForCurrentView()
                .GetAnimation("backAnimation");

            if (animation != null)
            {
                await ImageGridView.TryStartConnectedAnimationAsync(
                    animation, _persistedItem, "ItemImage");
            }
        }
    }
}
