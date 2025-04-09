﻿﻿﻿﻿﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AudioMixerApp.ViewModels;
using System.ComponentModel;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification; // Added for TaskbarIcon

namespace AudioMixerApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TaskbarIcon? _notifyIcon; // Field to hold the notify icon instance

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        // Find the TaskbarIcon resource and assign it to the field
        _notifyIcon = (TaskbarIcon?)FindResource("NotifyIconResource");
        Closing += MainWindow_Closing; // Hook up closing event
    }

    // Cleanup ViewModel resources when the window closes
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Ensure the icon is disposed before the application exits
        _notifyIcon?.Dispose(); // Use the field here
        _viewModel?.Cleanup();
    }

    // --- System Tray Icon Event Handlers --- (Task 38)

    // Handle window state changes (minimize to tray)
    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Hide the window and rely on the tray icon
            Hide();
            // Optionally show a balloon tip
            // NotifyIcon.ShowBalloonTip("Minimized", "Audio Mixer is running in the background.", BalloonIcon.Info);
        }
    }

    // Restore window on tray icon double-click
    private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate(); // Bring window to front
    }

    // Restore window from context menu
    private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    // Exit application from context menu
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
