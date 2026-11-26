using System.ComponentModel;
using Isla.App.ViewModels;
using Isla.Core.Interfaces;
using Isla.Core.Models;
using Isla.Windows.WindowManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;

namespace Isla.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainIslandViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly IslandWindowHelper _windowHelper;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<MainIslandViewModel>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _viewModel.AttachDispatcher(DispatcherQueue);

        RootShell.DataContext = _viewModel;

        _windowHelper = new IslandWindowHelper(this);
        _windowHelper.ConfigureAsFloatingIsland();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _settingsService.SettingsChanged += (_, _) => DispatcherQueue.TryEnqueue(ApplyWindowBounds);
        Activated += OnWindowActivated;

        ApplyWindowBounds();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainIslandViewModel.VisualState) or nameof(MainIslandViewModel.PrimaryActivity))
        {
            ApplyWindowBounds();
            if (e.PropertyName == nameof(MainIslandViewModel.VisualState))
            {
                PlayStateTransitionAnimation();
            }
        }
    }

    private void ApplyWindowBounds()
    {
        var s = _settingsService.Current;
        if (!s.IsIslandEnabled)
        {
            _windowHelper.AppWindow.Hide();
            return;
        }

        _windowHelper.AppWindow.Show(false);
        RootShell.Opacity = Math.Clamp(s.IslandOpacity, 0.4, 1.0);

        switch (_viewModel.VisualState)
        {
            case IslandVisualState.Compact:
                IslandContainer.CornerRadius = new CornerRadius(s.CompactHeight / 2.0);
                _windowHelper.UpdateBounds(s.CompactWidth, s.CompactHeight, s.CompactHeight / 2.0, s);
                break;

            case IslandVisualState.Expanded:
                var (expW, expH, expR) = GetExpandedDimensions();
                IslandContainer.CornerRadius = new CornerRadius(expR);
                _windowHelper.UpdateBounds(expW, expH, expR, s);
                break;

            case IslandVisualState.Dashboard:
                double dashRadius = 24;
                IslandContainer.CornerRadius = new CornerRadius(dashRadius);
                _windowHelper.UpdateBounds(780, 560, dashRadius, s);
                break;
        }
    }

    private (double Width, double Height, double Radius) GetExpandedDimensions()
    {
        return _viewModel.PrimaryActivity?.Type switch
        {
            ActivityType.Music => (348, 166, 32),
            ActivityType.LowBattery => (276, 48, 24),
            ActivityType.Timer or ActivityType.Pomodoro => (328, 66, 33),
            ActivityType.ScreenRecording => (310, 62, 31),
            ActivityType.FocusAssist => (304, 62, 31),
            ActivityType.BluetoothAccessory => (290, 62, 31),
            ActivityType.IncomingCall => (336, 64, 32),
            ActivityType.Download or ActivityType.FileOperation or ActivityType.AppInstall or ActivityType.CustomDeveloper => (334, 66, 33),
            _ => (340, 66, 33)
        };
    }

    private void PlayStateTransitionAnimation()
    {
        var speed = Math.Clamp(_settingsService.Current.AnimationSpeed, 0.4, 2.0);
        var duration = TimeSpan.FromMilliseconds(220 / speed);

        var anim = new DoubleAnimation
        {
            From = 0.72,
            To = 1.0,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(anim, IslandContainer);
        Storyboard.SetTargetProperty(anim, "Opacity");

        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated &&
            _settingsService.Current.ClickOutsideToCollapse &&
            _viewModel.VisualState != IslandVisualState.Compact)
        {
            _viewModel.CollapseCommand.Execute(null);
        }
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(RootShell).Properties;
        if (props.IsLeftButtonPressed)
        {
            if (_settingsService.Current.DragToReposition)
            {
                _windowHelper.BeginDrag();
                return;
            }

            if (_viewModel.VisualState == IslandVisualState.Compact && _settingsService.Current.ClickToExpand)
            {
                _viewModel.ToggleExpandCommand.Execute(null);
            }
        }
    }

    private void OnRootDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (_settingsService.Current.DoubleClickForDashboard)
        {
            _viewModel.OpenDashboardCommand.Execute(null);
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }
}
