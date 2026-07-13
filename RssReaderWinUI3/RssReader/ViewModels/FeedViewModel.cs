// MIGRATION NOTE:
// - Symbol enum exists in Microsoft.UI.Xaml.Controls (same values, different namespace)
// - [IgnoreDataMember] still works fine (System.Runtime.Serialization)
// - No functional changes needed in this ViewModel

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace RssReader.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    public FeedViewModel()
    {
        Articles = new ObservableCollection<ArticleViewModel>();
        Articles.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsNotEmpty));
            OnPropertyChanged(nameof(IsInErrorAndEmpty));
            OnPropertyChanged(nameof(IsInErrorAndNotEmpty));
            OnPropertyChanged(nameof(IsLoadingAndNotEmpty));
        };
    }

    private Uri? _link;
    public Uri? Link
    {
        get => _link;
        set { if (SetProperty(ref _link, value)) OnPropertyChanged(nameof(LinkAsString)); }
    }

    [JsonIgnore]
    public string LinkAsString
    {
        get => Link?.OriginalString ?? string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var trimmed = value.Trim();
            if (!trimmed.StartsWith("http://") && !trimmed.StartsWith("https://"))
            {
                IsInError = true;
                ErrorMessage = NOT_HTTP_MESSAGE;
            }
            else if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                Link = uri;
            }
            else
            {
                IsInError = true;
                ErrorMessage = INVALID_URL_MESSAGE;
            }
        }
    }

    private string? _name;
    public string? Name { get => _name; set => SetProperty(ref _name, value); }

    private string? _description;
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    private Symbol _symbol = Symbol.PostUpdate;
    public Symbol Symbol
    {
        get => _symbol;
        set { if (SetProperty(ref _symbol, value)) OnPropertyChanged(nameof(SymbolAsChar)); }
    }
    public char SymbolAsChar => (char)Symbol;

    public ObservableCollection<ArticleViewModel> Articles { get; }
    public object ArticlesAsObject => Articles;
    public bool IsEmpty => Articles.Count == 0;
    public bool IsNotEmpty => !IsEmpty;
    public bool IsFavoritesFeed { get; set; }
    public bool IsNotFavoritesOrInError => !IsFavoritesFeed && !IsInError;
    public bool IsLoadingAndNotEmpty => IsLoading && !IsEmpty;
    public bool IsInErrorAndEmpty => IsInError && IsEmpty;
    public bool IsInErrorAndNotEmpty => IsInError && !IsEmpty;

    private DateTime _lastSyncDateTime;
    public DateTime LastSyncDateTime
    {
        get => _lastSyncDateTime;
        set => SetProperty(ref _lastSyncDateTime, value);
    }

    public string FeedDownMessage
    {
        get
        {
            var lastSync = LastSyncDateTime.ToString(
                LastSyncDateTime.Date == DateTime.Today ? "t" : "g");
            return $"It looks like this feed is down. Last synced {lastSync}. Tap here to refresh.";
        }
    }

    [JsonIgnore]
    private bool _isSelectedInNavList;
    [JsonIgnore]
    public bool IsSelectedInNavList
    {
        get => _isSelectedInNavList;
        set => SetProperty(ref _isSelectedInNavList, value);
    }

    [JsonIgnore]
    private bool _isLoading;
    [JsonIgnore]
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsInError));
                OnPropertyChanged(nameof(IsLoadingAndNotEmpty));
                OnPropertyChanged(nameof(IsNotFavoritesOrInError));
                OnPropertyChanged(nameof(IsInErrorAndEmpty));
                OnPropertyChanged(nameof(IsInErrorAndNotEmpty));
            }
        }
    }

    [JsonIgnore]
    private bool _isInEdit;
    [JsonIgnore]
    public bool IsInEdit { get => _isInEdit; set => SetProperty(ref _isInEdit, value); }

    [JsonIgnore]
    private bool _isInError;
    [JsonIgnore]
    public bool IsInError
    {
        get => _isInError && !IsLoading;
        set
        {
            if (SetProperty(ref _isInError, value))
            {
                OnPropertyChanged(nameof(IsNotFavoritesOrInError));
                OnPropertyChanged(nameof(IsInErrorAndEmpty));
                OnPropertyChanged(nameof(IsInErrorAndNotEmpty));
            }
        }
    }

    [JsonIgnore]
    private string? _errorMessage;
    [JsonIgnore]
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public override bool Equals(object? obj) =>
        obj is FeedViewModel other && other.GetHashCode() == GetHashCode();

    public override int GetHashCode() =>
        Link?.GetComponents(UriComponents.Host | UriComponents.Path, UriFormat.Unescaped)
            .GetHashCode() ?? 0;

    private const string NOT_HTTP_MESSAGE = "Sorry. The URL must begin with http:// or https://";
    private const string INVALID_URL_MESSAGE = "Sorry. That is not a valid URL.";
}
