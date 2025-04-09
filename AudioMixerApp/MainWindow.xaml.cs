﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AudioMixerApp.ViewModels; // Added for ViewModel
using System.ComponentModel; // Added for Closing event args

namespace AudioMixerApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        Closing += MainWindow_Closing; // Hook up closing event
    }

    // Cleanup ViewModel resources when the window closes
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _viewModel?.Cleanup();
    }
}
