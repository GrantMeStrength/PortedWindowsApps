// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoEditor.Views;

namespace PhotoEditor;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Photo Editor";
        RootFrame.Navigate(typeof(MainPage));
    }

    public Frame NavigationFrame => RootFrame;
}
