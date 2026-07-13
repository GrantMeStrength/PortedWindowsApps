using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RssReader.ViewModels;

namespace RssReader.Views;

public sealed partial class EditFeedsPage : Page
{
    private MainViewModel _viewModel = null!;

    public EditFeedsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is MainViewModel vm)
        {
            _viewModel = vm;
            FeedsList.ItemsSource = vm.Feeds;
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFeeds = FeedsList.SelectedItems.Cast<FeedViewModel>().ToList();
        if (selectedFeeds.Count > 0)
        {
            _viewModel.RemoveFeeds(selectedFeeds);
        }
    }
}
