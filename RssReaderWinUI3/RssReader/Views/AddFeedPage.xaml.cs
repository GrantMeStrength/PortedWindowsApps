using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RssReader.Common;
using RssReader.ViewModels;

namespace RssReader.Views;

public sealed partial class AddFeedPage : Page
{
    private MainViewModel _viewModel = null!;
    private FeedViewModel _newFeed = new();

    public AddFeedPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is MainViewModel vm)
        {
            _viewModel = vm;
        }
    }

    private async void FeedUrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var url = FeedUrlBox.Text.Trim();
        ErrorInfoBar.IsOpen = false;
        FeedPreview.Visibility = Visibility.Collapsed;
        AddButton.IsEnabled = false;

        if (string.IsNullOrWhiteSpace(url)) return;

        _newFeed = new FeedViewModel();
        _newFeed.LinkAsString = url;

        if (_newFeed.IsInError)
        {
            ErrorInfoBar.Message = _newFeed.ErrorMessage ?? "Invalid URL";
            ErrorInfoBar.IsOpen = true;
            return;
        }

        // Try to load the feed to validate and preview
        await _newFeed.RefreshAsync();

        if (_newFeed.IsInError)
        {
            ErrorInfoBar.Message = "Could not load feed. Please check the URL.";
            ErrorInfoBar.IsOpen = true;
        }
        else
        {
            FeedNameText.Text = _newFeed.Name ?? "Untitled Feed";
            FeedDescriptionText.Text = _newFeed.Description ?? "";
            FeedPreview.Visibility = Visibility.Visible;
            AddButton.IsEnabled = true;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentFeed = _newFeed;
        if (_viewModel.TryAddCurrentFeed())
        {
            // Navigate back to the feed view
            _viewModel.CurrentFeed = _newFeed;
            FeedUrlBox.Text = "";
            FeedPreview.Visibility = Visibility.Collapsed;
            AddButton.IsEnabled = false;
        }
        else
        {
            ErrorInfoBar.Message = "This feed has already been added.";
            ErrorInfoBar.IsOpen = true;
        }
    }
}
