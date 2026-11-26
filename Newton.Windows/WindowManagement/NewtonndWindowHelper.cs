using System.Runtime.InteropServices;
using Newton.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Newton.Windows.WindowManagement;

public sealed class NewtonndWindowHelper
{
    private readonly Window _window;
    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;

    public NewtonndWindowHelper(Window window)
    {
        _window = window;
        _hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
    }

    public AppWindow AppWindow => _appWindow;
    public IntPtr Handle => _hwnd;

    public void ConfigureAsFloatingNewtonnd()
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(false, false);
        }

        try { _appWindow.IsShownInSwitchers = false; } catch { /* Not implemented in unpackaged mode; WS_EX_TOOLWINDOW handles this */ }

        // Remove standard window border styles for crisp floating pill rendering
        var style = GetWindowLongPtr(_hwnd, GWL_STYLE).ToInt64();
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(style));

        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle));

        EnsureTopMost();
    }

    public void EnsureTopMost()
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
        }
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    public double GetDpiScale()
    {
        var dpi = GetDpiForWindow(_hwnd);
        return dpi > 0 ? dpi / 96.0 : 1.0;
    }

    public IReadOnlyList<string> GetMonitorNames()
    {
        var displays = DisplayArea.FindAll();
        var list = new List<string>(displays.Count);
        for (int i = 0; i < displays.Count; i++)
        {
            var d = displays[i];
            list.Add($"Display {i + 1} ({d.OuterBounds.Width}×{d.OuterBounds.Height}){(d.IsPrimary ? " • Primary" : "")}");
        }
        return list;
    }

    public void UpdateBounds(double dipWidth, double dipHeight, double cornerRadiusDip, NewtonSettings settings)
    {
        var scale = GetDpiScale();
        int pixelWidth = Math.Max(100, (int)Math.Round(dipWidth * scale));
        int pixelHeight = Math.Max(36, (int)Math.Round(dipHeight * scale));

        var displays = DisplayArea.FindAll();
        DisplayArea targetDisplay = DisplayArea.Primary;
        if (settings.SelectedMonitorIndex >= 0 && settings.SelectedMonitorIndex < displays.Count)
        {
            targetDisplay = displays[settings.SelectedMonitorIndex];
        }

        var bounds = targetDisplay.WorkArea;
        int offsetX = (int)Math.Round(settings.HorizontalOffset * scale);
        int offsetY = (int)Math.Round(settings.VerticalOffset * scale);

        int x = settings.PositionMode switch
        {
            NewtonPositionMode.TopLeft => bounds.X + (int)(20 * scale) + offsetX,
            NewtonPositionMode.TopRight => bounds.X + bounds.Width - pixelWidth - (int)(20 * scale) + offsetX,
            NewtonPositionMode.Custom => bounds.X + (bounds.Width - pixelWidth) / 2 + offsetX,
            _ => bounds.X + (bounds.Width - pixelWidth) / 2 + offsetX
        };

        int y = targetDisplay.OuterBounds.Y + offsetY;

        _appWindow.MoveAndResize(new RectInt32(x, y, pixelWidth, pixelHeight));

        int cornerPx = (int)Math.Round(cornerRadiusDip * scale * 2);
        var rgn = CreateRoundRectRgn(0, 0, pixelWidth + 1, pixelHeight + 1, cornerPx, cornerPx);
        SetWindowRgn(_hwnd, rgn, true);
        EnsureTopMost();
    }

    public void BeginDrag()
    {
        ReleaseCapture();
        SendMessage(_hwnd, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const long WS_CAPTION = 0x00C00000L;
    private const long WS_THICKFRAME = 0x00040000L;
    private const long WS_MINIMIZEBOX = 0x00020000L;
    private const long WS_MAXIMIZEBOX = 0x00010000L;
    private const long WS_SYSMENU = 0x00080000L;
    private const long WS_EX_TOOLWINDOW = 0x00000080L;
    private const long WS_EX_TOPMOST = 0x00000008L;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 2;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
}
