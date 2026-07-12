using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace ColoringBook.Models
{
    /// <summary>
    /// Represents a library image template that users can select to color.
    /// 
    /// MIGRATION NOTE: BitmapImage works the same in WinUI 3, but loading from
    /// app package assets uses ms-appx:/// for packaged apps. For unpackaged apps,
    /// load from the file system with a full path or embedded resources.
    /// </summary>
    public partial class LibraryImage : ObservableObject
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;

        [ObservableProperty]
        private BitmapImage? _thumbnail;

        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Number of rows this item spans in a VariableSizedWrapGrid.
        /// </summary>
        public int RowSpan { get; set; } = 1;

        /// <summary>
        /// Number of columns this item spans in a VariableSizedWrapGrid.
        /// </summary>
        public int ColSpan { get; set; } = 1;
    }

    /// <summary>
    /// Represents a saved coloring (user's work in progress or completed artwork).
    /// </summary>
    public partial class Coloring : ObservableObject
    {
        public string Id { get; set; } = string.Empty;
        public string LibraryImageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;

        [ObservableProperty]
        private BitmapImage? _thumbnail;

        [ObservableProperty]
        private bool _isNew = true;

        [ObservableProperty]
        private bool _hasChanges;
    }

    /// <summary>
    /// Settings for a coloring session (selected color, tool, etc.).
    /// </summary>
    public class ColoringSettings
    {
        public Color CurrentColor { get; set; } = Colors.Red;
        public DrawingTool CurrentTool { get; set; } = DrawingTool.Pen;
        public double StrokeSize { get; set; } = 4.0;
        public double Opacity { get; set; } = 1.0;
    }
}
