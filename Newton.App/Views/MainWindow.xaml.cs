using System.ComponentModel;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Newton.App.ViewModels;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Newton.Windows.WindowManagement;

namespace Newton.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainNewtonndViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly NewtonndWindowHelper _windowHelper;

    private readonly Visual _containerVisual;
    private readonly Compositor _compositor;
    private readonly DispatcherTimer _morphTimer;
    private readonly DispatcherTimer _hoverExitTimer;
    private readonly DispatcherTimer _dodgeTimer;
    private bool _isPointerInside;
    private bool _isDodgingBrowser;
    private bool _isGhostClickThrough;

    private double _curW = 216, _curH = 44, _curR = 22;
    private double _startW = 216, _startH = 44, _startR = 22;
    private double _targetW = 216, _targetH = 44, _targetR = 22;
    private DateTime _morphStart;
    private TimeSpan _morphDuration = TimeSpan.FromMilliseconds(240);

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<MainNewtonndViewModel>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _viewModel.AttachDispatcher(DispatcherQueue);

        RootShell.DataContext = _viewModel;

        _windowHelper = new NewtonndWindowHelper(this);
        _windowHelper.ConfigureAsFloatingNewtonnd();

        _containerVisual = ElementCompositionPreview.GetElementVisual(NewtonndContainer);
        _compositor = _containerVisual.Compositor;
        NewtonndContainer.SizeChanged += (_, args) =>
        {
            _containerVisual.CenterPoint = new Vector3((float)(args.NewSize.Width / 2.0), 0f, 0f);
        };

        _morphTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _morphTimer.Tick += OnMorphTick;

        _hoverExitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
        _hoverExitTimer.Tick += (_, _) =>
        {
            _hoverExitTimer.Stop();
            if (!_isPointerInside && _viewModel.VisualState == NewtonndVisualState.HoverPeek)
            {
                _viewModel.VisualState = NewtonndVisualState.Compact;
            }
        };

        _dodgeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _dodgeTimer.Tick += OnDodgeTick;
        _dodgeTimer.Start();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _settingsService.SettingsChanged += (_, _) => DispatcherQueue.TryEnqueue(() => ApplyWindowBounds(animate: true));
        Activated += OnWindowActivated;

        ApplyWindowBounds(animate: false);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainNewtonndViewModel.VisualState) or nameof(MainNewtonndViewModel.PrimaryActivity))
        {
            ApplyWindowBounds(animate: true);
            if (e.PropertyName == nameof(MainNewtonndViewModel.VisualState))
            {
                PlaySpringContentTransition();
            }
        }
    }

    private void ApplyWindowBounds(bool animate)
    {
        var s = _settingsService.Current;
        if (!s.IsNewtonndEnabled)
        {
            _windowHelper.AppWindow.Hide();
            return;
        }

        _windowHelper.AppWindow.Show(false);
        RootShell.Opacity = Math.Clamp(s.NewtonndOpacity, 0.4, 1.0);

        var (w, h, r) = _viewModel.VisualState switch
        {
            NewtonndVisualState.Compact => (s.CompactWidth, s.CompactHeight, s.CompactHeight / 2.0),
            NewtonndVisualState.HoverPeek => (Math.Max(384.0, s.HoverPeekWidth), s.CompactHeight, s.CompactHeight / 2.0),
            NewtonndVisualState.Expanded => GetExpandedDimensions(),
            NewtonndVisualState.Dashboard => (780.0, 560.0, 20.0),
            _ => (s.CompactWidth, s.CompactHeight, s.CompactHeight / 2.0)
        };

        if (!animate)
        {
            _morphTimer.Stop();
            _curW = _targetW = w;
            _curH = _targetH = h;
            _curR = _targetR = r;
            NewtonndContainer.CornerRadius = new CornerRadius(r);
            _windowHelper.UpdateBounds(w, h, r, s, _isDodgingBrowser);
            return;
        }

        _startW = _curW;
        _startH = _curH;
        _startR = _curR;
        _targetW = w;
        _targetH = h;
        _targetR = r;

        var speed = Math.Clamp(s.AnimationSpeed, 0.4, 2.0);
        _morphDuration = TimeSpan.FromMilliseconds(240 / speed);
        _morphStart = DateTime.UtcNow;
        if (!_morphTimer.IsEnabled)
        {
            _morphTimer.Start();
        }
    }

    private void OnMorphTick(object? sender, object e)
    {
        var elapsed = (DateTime.UtcNow - _morphStart).TotalMilliseconds;
        var total = Math.Max(1.0, _morphDuration.TotalMilliseconds);
        var t = Math.Clamp(elapsed / total, 0.0, 1.0);

        // Dynamic Island style ease-out quintic curve
        var ease = 1.0 - Math.Pow(1.0 - t, 4.0);

        _curW = _startW + (_targetW - _startW) * ease;
        _curH = _startH + (_targetH - _startH) * ease;
        _curR = _startR + (_targetR - _startR) * ease;

        NewtonndContainer.CornerRadius = new CornerRadius(_curR);
        _windowHelper.UpdateBounds(_curW, _curH, _curR, _settingsService.Current, _isDodgingBrowser);

        if (t >= 1.0)
        {
            _morphTimer.Stop();
        }
    }

    private void OnDodgeTick(object? sender, object e)
    {
        var s = _settingsService.Current;

        // 1. Alt-key click-through ghost mode
        bool altDown = NewtonndWindowHelper.IsAltKeyDown();
        if (altDown != _isGhostClickThrough)
        {
            _isGhostClickThrough = altDown;
            _windowHelper.SetClickThrough(altDown);
            RootShell.Opacity = altDown ? 0.22 : Math.Clamp(s.NewtonndOpacity, 0.4, 1.0);
        }

        // 2. Smart auto-dodge when a browser is at the top of the screen
        bool shouldDodge = s.AutoDodgeBrowserTabs &&
                           _viewModel.VisualState == NewtonndVisualState.Compact &&
                           NewtonndWindowHelper.IsBrowserAtTop();
        if (shouldDodge != _isDodgingBrowser)
        {
            _isDodgingBrowser = shouldDodge;
            _windowHelper.UpdateBounds(_curW, _curH, _curR, s, _isDodgingBrowser);
        }
    }

    private (double Width, double Height, double Radius) GetExpandedDimensions()
    {
        return _viewModel.PrimaryActivity?.Type switch
        {
            ActivityType.Music => (368, 152, 22),
            ActivityType.LowBattery => (284, 52, 26),
            ActivityType.Timer or ActivityType.Pomodoro => (360, 128, 22),
            ActivityType.ScreenRecording => (318, 60, 30),
            ActivityType.FocusAssist => (310, 60, 30),
            ActivityType.BluetoothAccessory => (296, 60, 30),
            ActivityType.IncomingCall => (342, 64, 32),
            ActivityType.Download or ActivityType.FileOperation or ActivityType.AppInstall or ActivityType.CustomDeveloper => (340, 64, 32),
            _ => (356, 72, 22)
        };
    }

    private void AnimateSpringScale(Vector3 targetScale, float damping = 0.68f, int periodMs = 48)
    {
        var spring = _compositor.CreateSpringVector3Animation();
        spring.Target = "Scale";
        spring.FinalValue = targetScale;
        spring.DampingRatio = damping;
        spring.Period = TimeSpan.FromMilliseconds(periodMs);
        _containerVisual.StartAnimation("Scale", spring);
    }

    private void PlaySpringContentTransition()
    {
        _containerVisual.Scale = new Vector3(0.95f, 0.95f, 1f);
        AnimateSpringScale(Vector3.One, damping: 0.65f, periodMs: 52);

        var speed = Math.Clamp(_settingsService.Current.AnimationSpeed, 0.4, 2.0);
        var anim = new DoubleAnimation
        {
            From = 0.78,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(180 / speed)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(anim, NewtonndContainer);
        Storyboard.SetTargetProperty(anim, "Opacity");

        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            if (_settingsService.Current.ClickOutsideToCollapse &&
                _viewModel.VisualState != NewtonndVisualState.Compact)
            {
                _viewModel.CollapseCommand.Execute(null);
            }
            _windowHelper.EnsureTopMost();
        }
    }

    private void OnRootPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInside = true;
        _hoverExitTimer.Stop();
        if (_viewModel.VisualState == NewtonndVisualState.Compact)
        {
            if (_settingsService.Current.HoverToPeek)
            {
                _viewModel.VisualState = NewtonndVisualState.HoverPeek;
            }
            else
            {
                AnimateSpringScale(new Vector3(1.035f, 1.035f, 1f), damping: 0.62f, periodMs: 42);
            }
        }
    }

    private void OnRootPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInside = false;
        AnimateSpringScale(Vector3.One, damping: 0.7f, periodMs: 45);
        if (_viewModel.VisualState == NewtonndVisualState.HoverPeek)
        {
            _hoverExitTimer.Start();
        }
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        AnimateSpringScale(new Vector3(0.96f, 0.96f, 1f), damping: 0.75f, periodMs: 36);

        var props = e.GetCurrentPoint(RootShell).Properties;
        if (props.IsLeftButtonPressed)
        {
            if (_settingsService.Current.DragToReposition)
            {
                _windowHelper.BeginDrag();
                return;
            }

            if (_viewModel.VisualState is NewtonndVisualState.Compact or NewtonndVisualState.HoverPeek && _settingsService.Current.ClickToExpand)
            {
                _viewModel.VisualState = NewtonndVisualState.Expanded;
            }
        }
    }

    private void OnRootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        AnimateSpringScale(Vector3.One, damping: 0.65f, periodMs: 45);
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
