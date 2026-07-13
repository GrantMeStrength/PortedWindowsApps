using System;
using ColoringBook.FileIO;
using ColoringBook.Models;
using ColoringBook.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ColoringBook.Views
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel { get; } = new();

        public HomePage()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadLibraryCommand.ExecuteAsync(null);
            await ViewModel.LoadSavedColoringsCommand.ExecuteAsync(null);
        }

        private void OnTabChanged(object sender, RoutedEventArgs e)
        {
            if (LibraryTab.IsChecked == true)
            {
                LibraryGrid.Visibility = Visibility.Visible;
                ColoringsGrid.Visibility = Visibility.Collapsed;
                EmptyStateText.Visibility = Visibility.Collapsed;
            }
            else
            {
                LibraryGrid.Visibility = Visibility.Collapsed;
                ColoringsGrid.Visibility = Visibility.Visible;
                EmptyStateText.Visibility = ViewModel.SavedColorings.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async void OnLibraryImageClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not LibraryImage image) return;

            // Create a new coloring session from this library image
            var coloringId = $"{image.Id}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var sessionPath = await ColoringFileIO.CreateColoringSessionAsync(
                image.ImagePath, coloringId);

            // Navigate to the coloring page
            Frame.Navigate(typeof(ColoringPage), sessionPath);
        }

        private void OnColoringClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not Coloring coloring) return;

            // Navigate to existing coloring session
            Frame.Navigate(typeof(ColoringPage), coloring.FolderPath);
        }
    }
}
