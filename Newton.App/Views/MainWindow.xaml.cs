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
    private VolumePillWindow? _volumePillWindow;

    private readonly Visual _containerVisual;
    private readonly Compositor _compositor;
    private readonly DispatcherTimer _morphTimer;
    private readonly DispatcherTimer _edgeTimer;
    private DateTime _lastCursorInsideUtc = DateTime.MinValue;
    private bool _isPointerInside;
    private bool _isUserHidden;
    private bool _isEdgeRevealed;
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
        _windowHelper.GlobalHotKeyPressed += OnGlobalHotKeyPressed;

        _containerVisual = ElementCompositionPreview.GetElementVisual(NewtonndContainer);
        _compositor = _containerVisual.Compositor;
        NewtonndContainer.SizeChanged += (_, args) =>
        {
            _containerVisual.CenterPoint = new Vector3((float)(args.NewSize.Width / 2.0), 0f, 0f);
        };

        _morphTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _morphTimer.Tick += OnMorphTick;

        _edgeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        _edgeTimer.Tick += OnEdgeTimerTick;
        _edgeTimer.Start();

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
            if (_viewModel.VisualState != NewtonndVisualState.Expanded && _viewModel.IsVolumeBarOpen)
            {
                _viewModel.IsVolumeBarOpen = false;
            }
            if (e.PropertyName == nameof(MainNewtonndViewModel.VisualState))
            {
                PlaySpringContentTransition();
            }
        }
        else if (e.PropertyName == nameof(MainNewtonndViewModel.IsVolumeBarOpen))
        {
            UpdateVolumePillWindow();
        }
    }

    private void UpdateVolumePillWindow()
    {
        if (_viewModel.IsVolumeBarOpen && _viewModel.VisualState == NewtonndVisualState.Expanded)
        {
            _volumePillWindow ??= new VolumePillWindow();
            var (_, h, _) = GetExpandedDimensions();
            _volumePillWindow.PositionBelowMainPill(_settingsService.Current.VerticalOffset + h, _settingsService);
            _volumePillWindow.Activate();
        }
        else
        {
            _volumePillWindow?.Close();
            _volumePillWindow = null;
        }
    }

    private void ApplyWindowBounds(bool animate)
    {
        var s = _settingsService.Current;
        bool shouldShow = (s.IsNewtonndEnabled && !_isUserHidden) || _isEdgeRevealed;
        if (!shouldShow)
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
            _windowHelper.UpdateBounds(w, h, r, s);
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
        _windowHelper.UpdateBounds(_curW, _curH, _curR, _settingsService.Current);

        if (t >= 1.0)
        {
            _morphTimer.Stop();
        }
    }

    private void OnGlobalHotKeyPressed()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _isUserHidden = !_isUserHidden;
            _isEdgeRevealed = false;
            ApplyWindowBounds(animate: true);
        });
    }

    private void OnEdgeTimerTick(object? sender, object e)
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

        // 2. Reliable Win32 hover-expand across X axis (single source of truth)
        bool cursorInside = _windowHelper.IsCursorInsideWindow();
        _isPointerInside = cursorInside;
        if (cursorInside)
        {
            _lastCursorInsideUtc = DateTime.UtcNow;
            if (s.HoverToPeek && _viewModel.VisualState == NewtonndVisualState.Compact)
            {
                _viewModel.VisualState = NewtonndVisualState.HoverPeek;
            }
        }
        else if (s.HoverToPeek &&
                 _viewModel.VisualState == NewtonndVisualState.HoverPeek &&
                 (DateTime.UtcNow - _lastCursorInsideUtc).TotalMilliseconds > 180)
        {
            _viewModel.VisualState = NewtonndVisualState.Compact;
        }

        // 3. Top-edge mouse reveal when pill is hidden
        if (_isUserHidden || !s.IsNewtonndEnabled)
        {
            bool atTopEdge = _windowHelper.IsCursorAtTopCenterEdge(s);
            if (atTopEdge && !_isEdgeRevealed)
            {
                _isEdgeRevealed = true;
                ApplyWindowBounds(animate: true);
            }
            else if (!atTopEdge && !_isPointerInside && _isEdgeRevealed && _viewModel.VisualState == NewtonndVisualState.Compact)
            {
                _isEdgeRevealed = false;
                ApplyWindowBounds(animate: false);
            }
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
            ActivityType.Clipboard => (392, 232, 22),
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
            if (_volumePillWindow is not null && _viewModel.IsVolumeBarOpen)
            {
                _windowHelper.EnsureTopMost();
                return;
            }
            if (_settingsService.Current.ClickOutsideToCollapse &&
                _viewModel.VisualState != NewtonndVisualState.Compact)
            {
                _viewModel.CollapseCommand.Execute(null);
            }
            _windowHelper.EnsureTopMost();
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

    private void OnRootDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems) ||
            e.DataView.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
        {
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            _viewModel.IsDragOver = true;
            AnimateSpringScale(new Vector3(1.05f, 1.05f, 1f), damping: 0.6f, periodMs: 42);
        }
    }

    private void OnRootDragLeave(object sender, DragEventArgs e)
    {
        _viewModel.IsDragOver = false;
        AnimateSpringScale(Vector3.One, damping: 0.68f, periodMs: 45);
    }

    private async void OnRootDrop(object sender, DragEventArgs e)
    {
        _viewModel.IsDragOver = false;
        AnimateSpringScale(new Vector3(0.95f, 0.95f, 1f), damping: 0.55f, periodMs: 45);
        AnimateSpringScale(Vector3.One, damping: 0.6f, periodMs: 55);

        try
        {
            if (e.DataView.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var paths = items.Select(i => i.Path).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                _viewModel.HandleDropFiles(paths);
            }
            else if (e.DataView.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                var text = await e.DataView.GetTextAsync();
                _viewModel.HandleDropText(text);
            }
        }
        catch { }
    }
}
