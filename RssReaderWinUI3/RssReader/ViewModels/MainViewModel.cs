// MIGRATION NOTE:
// The original BindableBase.cs (~50 lines of boilerplate INotifyPropertyChanged) is ENTIRELY
// replaced by CommunityToolkit.Mvvm's ObservableObject. Just inherit from ObservableObject.
// 
// Key differences:
// - SetProperty() method signature is identical (drop-in replacement)
// - OnPropertyChanged() works the same way
// - Source generators ([ObservableProperty], [RelayCommand]) further reduce boilerplate
// - No more manual backing field patterns needed for simple properties
//
// For this migration, we keep the explicit property pattern to minimize behavioral changes,
// but new code should prefer [ObservableProperty] attributes.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RssReader.Common;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RssReader.ViewModels;

/// <summary>
/// Represents a collection of RSS feeds and user interactions with the feeds.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Feeds = new ObservableCollection<FeedViewModel>();
        Feeds.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(FeedsWithFavorites));
            OnPropertyChanged(nameof(HasNoFeeds));

            if (_suppressSave || e.Action != NotifyCollectionChangedAction.Add) return;
            _ = SaveFeedsAsync();
        };
    }

    public FeedViewModel FavoritesFeed { get; private set; } = null!;
    public ObservableCollection<FeedViewModel> Feeds { get; }
    public IEnumerable<FeedViewModel> FeedsWithFavorites => new[] { FavoritesFeed }.Concat(Feeds);
    public bool HasNoFeeds => Feeds.Count == 0;
    public bool IsCurrentFeedFavoritesFeed => CurrentFeed == FavoritesFeed;

    public event EventHandler? Initialized;
    public event EventHandler? BadFeedRemoved;

    public async Task InitializeFeedsAsync()
    {
        FavoritesFeed = await FeedDataSource.GetFavoritesAsync();

        _suppressSave = true;
        Feeds.Clear();
        (await FeedDataSource.GetFeedsAsync()).ForEach(feed => Feeds.Add(feed));
        _suppressSave = false;

        CurrentFeed = Feeds.Count == 0 ? FavoritesFeed : Feeds[0];

        if (FavoritesFeed.Articles.Count == 0)
            FavoritesFeed.ErrorMessage = NO_ARTICLES_MESSAGE;

        FavoritesFeed.Articles.CollectionChanged += async (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                await SaveFavoritesAsync();
            FavoritesFeed.ErrorMessage = FavoritesFeed.Articles.Count > 0
                ? null : NO_ARTICLES_MESSAGE;
        };

        Initialized?.Invoke(this, EventArgs.Empty);
    }

    // MIGRATION NOTE: CoreWindow.Dispatcher → DispatcherQueue
    // The original used CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync() to
    // marshal UI updates. In WinUI 3, use DispatcherQueue.GetForCurrentThread().TryEnqueue().
    private FeedViewModel _currentFeed = null!;
    public FeedViewModel CurrentFeed
    {
        get => _currentFeed;
        set
        {
            if (SetProperty(ref _currentFeed, value))
            {
                OnPropertyChanged(nameof(IsCurrentFeedFavoritesFeed));
                if (_currentFeed.Articles.Count > 0)
                {
                    CurrentArticle = _currentFeed.Articles.First();
                }
                else
                {
                    CurrentArticle = null;
                    NotifyCollectionChangedEventHandler? handler = null;
                    handler = (s, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Add)
                        {
                            _currentFeed.Articles.CollectionChanged -= handler;

                            // WinUI 3: Use DispatcherQueue instead of CoreWindow.Dispatcher
                            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                                CurrentArticle = _currentFeed.Articles.First());
                        }
                    };
                    _currentFeed.Articles.CollectionChanged += handler;
                }
            }
        }
    }

    private ArticleViewModel? _currentArticle;
    public ArticleViewModel? CurrentArticle
    {
        get => _currentArticle;
        set
        {
            _currentArticle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentArticleAsObject));
        }
    }

    public object? CurrentArticleAsObject => CurrentArticle;

    private bool _isInDetailsMode;
    public bool IsInDetailsMode
    {
        get => _isInDetailsMode;
        set => SetProperty(ref _isInDetailsMode, value);
    }

    private bool _isFeedAddedMessageShowing;
    public bool IsFeedAddedMessageShowing
    {
        get => _isFeedAddedMessageShowing;
        set => SetProperty(ref _isFeedAddedMessageShowing, value);
    }

    private string? _nameOfFeedJustAdded;
    public string? NameOfFeedJustAdded
    {
        get => _nameOfFeedJustAdded;
        set => SetProperty(ref _nameOfFeedJustAdded, value);
    }

    public void SyncFavoritesFeed(ArticleViewModel article)
    {
        if (article.IsStarred == true) FavoritesFeed.Articles.Insert(0, article);
        else
        {
            FavoritesFeed.Articles.Remove(article);
            _ = SaveFavoritesAsync();
        }
    }

    [RelayCommand]
    private void RefreshCurrentFeed() => _ = CurrentFeed.RefreshAsync();

    public void AddCurrentFeed()
    {
        Feeds.Add(CurrentFeed);
        NameOfFeedJustAdded = CurrentFeed.Name;
        IsFeedAddedMessageShowing = true;
        _ = HideFeedAddedMessageAsync();
    }

    private async Task HideFeedAddedMessageAsync()
    {
        await Task.Delay(5000);
        IsFeedAddedMessageShowing = false;
    }

    public bool TryAddCurrentFeed()
    {
        if (Feeds.Contains(CurrentFeed))
        {
            CurrentFeed.IsInError = true;
            CurrentFeed.ErrorMessage = ALREADY_ADDED_MESSAGE;
            return false;
        }
        AddCurrentFeed();
        return true;
    }

    public void RemoveFeeds(IEnumerable<FeedViewModel> feeds)
    {
        feeds.ToList().ForEach(feed => Feeds.Remove(feed));
        _ = SaveFeedsAsync();
    }

    public void RemoveBadFeed()
    {
        var index = Feeds.IndexOf(CurrentFeed);
        Feeds.Remove(CurrentFeed);
        CurrentFeed = Feeds[Feeds.Count > index ? index : index - 1];
        _ = SaveFeedsAsync();
        BadFeedRemoved?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveFeedsAsync() => await Feeds.SaveAsync();
    public async Task SaveFavoritesAsync() => await FavoritesFeed.SaveFavoritesAsync();

    private bool _suppressSave = true;
    private const string NO_ARTICLES_MESSAGE = "There are no starred articles.";
    private const string ALREADY_ADDED_MESSAGE = "This feed has already been added.";
}
