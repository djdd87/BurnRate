using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClaudeMon.Services;
using ClaudeMon.ViewModels;

namespace ClaudeMon.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            UpdateThemeButtons(newVm.ThemeMode);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ThemeMode)
            && DataContext is MainViewModel vm)
        {
            UpdateThemeButtons(vm.ThemeMode);
        }
    }

    private void UpdateThemeButtons(AppThemeMode mode)
    {
        var activeStyle = (Style)FindResource("ThemeToggleButtonActive");
        var normalStyle = (Style)FindResource("ThemeToggleButton");

        BtnDark.Style   = mode == AppThemeMode.Dark   ? activeStyle : normalStyle;
        BtnLight.Style  = mode == AppThemeMode.Light  ? activeStyle : normalStyle;
        BtnSystem.Style = mode == AppThemeMode.System ? activeStyle : normalStyle;
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.ThemeMode = AppThemeMode.Dark;
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.ThemeMode = AppThemeMode.Light;
    }

    private void ThemeSystem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.ThemeMode = AppThemeMode.System;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void ProfileTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ProfileViewModel profile }
            && DataContext is MainViewModel vm)
        {
            vm.SelectedProfile = profile;
        }
    }
}
