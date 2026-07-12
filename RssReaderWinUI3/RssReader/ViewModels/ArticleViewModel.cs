// MIGRATION NOTE:
// Simple data model — no UWP-specific APIs. Just needs namespace change for BindableBase → ObservableObject.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace RssReader.ViewModels;

public partial class ArticleViewModel : ObservableObject
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Author { get; set; }
    public Uri? Link { get; set; }
    public DateTime PublishedDate { get; set; }

    [JsonIgnore]
    private bool? _isStarred;
    [JsonIgnore]
    public bool? IsStarred
    {
        get => _isStarred;
        set => SetProperty(ref _isStarred, value);
    }

    /// <summary>
    /// Gets a human-friendly relative date string (e.g., "2 hours ago").
    /// </summary>
    [JsonIgnore]
    public string PublishedDateFormatted
    {
        get
        {
            var elapsed = DateTime.Now - PublishedDate;
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            return PublishedDate.ToString("MMM d, yyyy");
        }
    }

    public override bool Equals(object? obj) =>
        obj is ArticleViewModel other && other.GetHashCode() == GetHashCode();

    public override int GetHashCode() =>
        Link?.GetComponents(UriComponents.Host | UriComponents.Path, UriFormat.Unescaped)
            .GetHashCode() ?? 0;
}
