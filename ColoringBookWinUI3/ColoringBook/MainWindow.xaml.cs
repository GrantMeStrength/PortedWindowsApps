using Microsoft.UI.Xaml;
using ColoringBook.Views;

namespace ColoringBook
{
    public sealed partial class MainWindow : Window
    {
        public Frame AppFrame => RootFrame;

        public MainWindow()
        {
            InitializeComponent();
            SetTitleBar(AppTitleBar);
            RootFrame.Navigate(typeof(HomePage));
        }

        public void NavigateBack()
        {
            if (RootFrame.CanGoBack)
                RootFrame.GoBack();
        }
    }
}
