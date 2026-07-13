// MIGRATION NOTES:
// 1. The entire AppShell.xaml.cs + NavMenuListView.cs (~25KB of custom navigation code)
//    is replaced by this ~100-line file using WinUI 3's built-in NavigationView.
// 2. CoreWindow.Dispatcher → DispatcherQueue (the biggest behavioral change).
// 3. ApplicationView.GetForCurrentView() → use AppWindow or ExtendsContentIntoTitleBar.
// 4. Window.Current.Content → direct window content management.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RssReader.ViewModels;
using RssReader.Views;

namespace RssReader;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new MainViewModel();

    public MainWindow()
    {
        this.InitializeComponent();

        // Set title bar color (WinUI 3 approach)
        ExtendsContentIntoTitleBar = false;
        Title = "RSS Reader";
    }

    private async void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize feed data
        await ViewModel.InitializeFeedsAsync();

        // Dynamically add feed items to nav
        RebuildFeedNavItems();

        // Subscribe to feeds collection changes to keep nav in sync
        ViewModel.Feeds.CollectionChanged += (s, args) => RebuildFeedNavItems();

        // Select the first feed (or Favorites if empty)
        NavView.SelectedItem = NavView.MenuItems.First();
        ContentFrame.Navigate(typeof(FeedPage), ViewModel);
    }

    private void RebuildFeedNavItems()
    {
        // Remove all dynamic feed items (keep Favorites at index 0)
        while (NavView.MenuItems.Count > 1)
        {
            NavView.MenuItems.RemoveAt(1);
        }

        // Add current feeds
        foreach (var feed in ViewModel.Feeds)
        {
            var item = new NavigationViewItem
            {
                Content = feed.Name,
                Tag = feed,
                Icon = new SymbolIcon(Symbol.PostUpdate)
            };
            NavView.MenuItems.Add(item);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        var tag = item.Tag;
        switch (tag)
        {
            case "Favorites":
                ViewModel.CurrentFeed = ViewModel.FavoritesFeed;
                ContentFrame.Navigate(typeof(FeedPage), ViewModel);
                break;

            case "AddFeed":
                ContentFrame.Navigate(typeof(AddFeedPage), ViewModel);
                break;

            case "EditFeeds":
                ContentFrame.Navigate(typeof(EditFeedsPage), ViewModel);
                break;

            case FeedViewModel feed:
                ViewModel.CurrentFeed = feed;
                ContentFrame.Navigate(typeof(FeedPage), ViewModel);
                break;
        }
    }
}
