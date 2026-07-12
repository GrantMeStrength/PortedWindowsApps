using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RssReader.ViewModels;

namespace RssReader.Views;

public sealed partial class FeedPage : Page
{
    public MainViewModel ViewModel { get; private set; } = null!;

    public FeedPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is MainViewModel vm)
        {
            ViewModel = vm;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.CurrentFeed.RefreshAsync();
    }

    private void ArticleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ArticleList.SelectedItem is ArticleViewModel article)
        {
            ViewModel.CurrentArticle = article;
        }
    }
}
