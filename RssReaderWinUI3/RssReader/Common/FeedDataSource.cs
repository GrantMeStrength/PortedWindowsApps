// MIGRATION NOTE:
// The original used Windows.Web.Syndication.SyndicationClient (UWP-only API).
// In WinUI 3 desktop apps, SyndicationClient is NOT available because it's a
// Windows Runtime API that requires the app identity (packaged apps only) or
// has threading restrictions.
//
// REPLACEMENT: System.ServiceModel.Syndication (part of .NET, cross-platform)
// with HttpClient for fetching. This is actually BETTER because:
// - No dependency on WinRT threading model
// - Works in unpackaged apps
// - Standard .NET, testable outside of UI context
//
// Alternative: You COULD use Windows.Web.Syndication if your app is MSIX-packaged,
// but System.ServiceModel.Syndication is the recommended modern approach.

using RssReader.ViewModels;
using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Xml;

namespace RssReader.Common;

public static class FeedDataSource
{
    private static readonly HttpClient _httpClient = new();
    private static readonly string _feedsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RssReader", "feeds.json");
    private static readonly string _favoritesFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RssReader", "favorites.json");

    /// <summary>
    /// Loads saved feeds from local storage and refreshes their articles.
    /// </summary>
    public static async Task<List<FeedViewModel>> GetFeedsAsync()
    {
        var feeds = await LoadAsync<List<FeedViewModel>>(_feedsFile) ?? GetDefaultFeeds();
        foreach (var feed in feeds)
        {
            await feed.RefreshAsync();
        }
        return feeds;
    }

    /// <summary>
    /// Loads the favorites feed from local storage.
    /// </summary>
    public static async Task<FeedViewModel> GetFavoritesAsync()
    {
        var favorites = await LoadAsync<FeedViewModel>(_favoritesFile) ?? new FeedViewModel();
        favorites.Name = "Favorites";
        favorites.IsFavoritesFeed = true;
        return favorites;
    }

    /// <summary>
    /// Refreshes the articles for a feed by fetching from the RSS source.
    /// </summary>
    public static async Task RefreshAsync(this FeedViewModel feed)
    {
        if (feed.Link == null || feed.IsFavoritesFeed) return;

        feed.IsLoading = true;
        feed.IsInError = false;

        try
        {
            // MIGRATION NOTE: This replaces Windows.Web.Syndication.SyndicationClient
            using var stream = await _httpClient.GetStreamAsync(feed.Link);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            var syndicationFeed = SyndicationFeed.Load(reader);

            feed.Name ??= syndicationFeed.Title?.Text ?? "Untitled";
            feed.Description ??= syndicationFeed.Description?.Text;
            feed.LastSyncDateTime = DateTime.Now;

            var existingLinks = feed.Articles.Select(a => a.Link).ToHashSet();

            foreach (var item in syndicationFeed.Items)
            {
                var link = item.Links.FirstOrDefault()?.Uri;
                if (link != null && !existingLinks.Contains(link))
                {
                    feed.Articles.Add(new ArticleViewModel
                    {
                        Title = item.Title?.Text,
                        Summary = StripHtml(item.Summary?.Text ?? ""),
                        Author = item.Authors.FirstOrDefault()?.Name,
                        Link = link,
                        PublishedDate = item.PublishDate.LocalDateTime
                    });
                }
            }
        }
        catch (Exception)
        {
            feed.IsInError = true;
            feed.ErrorMessage = feed.FeedDownMessage;
        }
        finally
        {
            feed.IsLoading = false;
        }
    }

    /// <summary>
    /// Saves the feeds collection to local storage.
    /// </summary>
    public static async Task SaveAsync(this ObservableCollection<FeedViewModel> feeds)
    {
        await SaveAsync(feeds.ToList(), _feedsFile);
    }

    /// <summary>
    /// Saves the favorites feed to local storage.
    /// </summary>
    public static async Task SaveFavoritesAsync(this FeedViewModel favorites)
    {
        await SaveAsync(favorites, _favoritesFile);
    }

    private static List<FeedViewModel> GetDefaultFeeds() =>
    [
        new FeedViewModel { Link = new Uri("https://devblogs.microsoft.com/dotnet/feed/") },
        new FeedViewModel { Link = new Uri("https://devblogs.microsoft.com/visualstudio/feed/") },
        new FeedViewModel { Link = new Uri("https://feeds.hanselman.com/ScottHanselman") },
    ];

    // MIGRATION NOTE: 
    // Original used Windows.Storage.ApplicationData.Current.LocalFolder + DataContractSerializer.
    // Replaced with System.Text.Json + Environment.SpecialFolder.LocalApplicationData.
    // This works for both packaged AND unpackaged WinUI 3 apps.
    private static async Task<T?> LoadAsync<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private static async Task SaveAsync<T>(T data, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;
        // Simple HTML stripping — for production, use a proper sanitizer
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }
}
