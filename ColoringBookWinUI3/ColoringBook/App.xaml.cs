using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace ColoringBook
{
    public partial class App : Application
    {
        public static Window MainAppWindow { get; private set; } = null!;
        public static IntPtr WindowHandle { get; private set; }

        /// <summary>
        /// Local folder for storing coloring data (replaces ApplicationData.Current.LocalFolder
        /// for unpackaged apps).
        /// </summary>
        public static string LocalDataPath { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Set up local data directory
            LocalDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ColoringBook");
            Directory.CreateDirectory(LocalDataPath);
            Directory.CreateDirectory(Path.Combine(LocalDataPath, "Colorings"));

            MainAppWindow = new MainWindow();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(MainAppWindow);

            MainAppWindow.ExtendsContentIntoTitleBar = true;
            MainAppWindow.Activate();
        }
    }
}
