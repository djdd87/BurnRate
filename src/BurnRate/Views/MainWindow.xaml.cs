using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BurnRate.Models;
using BurnRate.Services;
using BurnRate.ViewModels;

namespace BurnRate.Views;

public partial class MainWindow : Window
{
    private readonly List<Button> _customThemeButtons = [];

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
            RebuildCustomThemeButtons(newVm.AvailableCustomThemes);
            UpdateThemeButtons(newVm.ThemeMode, newVm.ActiveCustomTheme);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.PropertyName == nameof(MainViewModel.ThemeMode)
            || e.PropertyName == nameof(MainViewModel.ActiveCustomTheme))
        {
            UpdateThemeButtons(vm.ThemeMode, vm.ActiveCustomTheme);
        }
    }

    private void RebuildCustomThemeButtons(IEnumerable<CustomTheme> themes)
    {
        // Remove old custom buttons
        foreach (var btn in _customThemeButtons)
            ThemeButtonPanel.Children.Remove(btn);
        _customThemeButtons.Clear();

        // Find insertion index: after BtnLight, before BtnSystem
        var systemIndex = ThemeButtonPanel.Children.IndexOf(BtnSystem);

        foreach (var theme in themes)
        {
            var btn = new Button
            {
                Content = theme.DisplayName,
                Style = (Style)FindResource("ThemeToggleButton"),
                Margin = new Thickness(0, 0, 2, 0),
                Tag = theme
            };
            btn.Click += CustomThemeButton_Click;
            ThemeButtonPanel.Children.Insert(systemIndex, btn);
            _customThemeButtons.Add(btn);
            systemIndex++;
        }
    }

    private void CustomThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CustomTheme theme } && DataContext is MainViewModel vm)
            vm.SelectCustomThemeCommand.Execute(theme);
    }

    private void UpdateThemeButtons(AppThemeMode mode, CustomTheme? activeCustom)
    {
        var activeStyle = (Style)FindResource("ThemeToggleButtonActive");
        var normalStyle = (Style)FindResource("ThemeToggleButton");

        BtnDark.Style   = mode == AppThemeMode.Dark   ? activeStyle : normalStyle;
        BtnLight.Style  = mode == AppThemeMode.Light  ? activeStyle : normalStyle;
        BtnSystem.Style = mode == AppThemeMode.System ? activeStyle : normalStyle;

        foreach (var btn in _customThemeButtons)
        {
            btn.Style = mode == AppThemeMode.Custom
                && btn.Tag is CustomTheme ct
                && ct.Id == activeCustom?.Id
                ? activeStyle
                : normalStyle;
        }
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
