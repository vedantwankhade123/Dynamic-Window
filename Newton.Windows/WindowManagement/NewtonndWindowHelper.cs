using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Newton.Core.Models;
using Windows.Graphics;
using WinRT.Interop;

namespace Newton.Windows.WindowManagement;

public sealed class NewtonndWindowHelper : IDisposable
{
    private readonly Window _window;
    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;
    private SubclassProc? _subclassDelegate;

    public event Action? GlobalHotKeyPressed;

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

        try { _appWindow.IsShownInSwitchers = false; } catch { }

        var style = GetWindowLongPtr(_hwnd, GWL_STYLE).ToInt64();
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtr(_hwnd, GWL_STYLE, new IntPtr(style));

        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle));

        int noBorder = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE
        DwmSetWindowAttribute(_hwnd, DWMWA_BORDER_COLOR, ref noBorder, sizeof(int));
        int ncrpDisabled = 1; // DWMNCRP_DISABLED
        DwmSetWindowAttribute(_hwnd, DWMWA_NCRENDERING_POLICY, ref ncrpDisabled, sizeof(int));

        RegisterCtrlNHotKey();
        EnsureTopMost();
    }

    private void RegisterCtrlNHotKey()
    {
        _subclassDelegate = OnWindowSubclass;
        SetWindowSubclass(_hwnd, _subclassDelegate, new UIntPtr(1), IntPtr.Zero);
        // Ctrl + N (0x4E)
        RegisterHotKey(_hwnd, HOTKEY_TOGGLE_ID, MOD_CONTROL | MOD_NOREPEAT, 0x4E);
    }

    private IntPtr OnWindowSubclass(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_TOGGLE_ID)
        {
            GlobalHotKeyPressed?.Invoke();
            return IntPtr.Zero;
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
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
            _ => bounds.X + (bounds.Width - pixelWidth) / 2 + offsetX
        };

        int y = targetDisplay.OuterBounds.Y + offsetY;

        _appWindow.MoveAndResize(new RectInt32(x, y, pixelWidth, pixelHeight));

        int cornerPx = (int)Math.Round(cornerRadiusDip * scale * 2);
        var rgn = CreateRoundRectRgn(0, 0, pixelWidth + 1, pixelHeight + 1, cornerPx, cornerPx);
        SetWindowRgn(_hwnd, rgn, true);
        EnsureTopMost();
    }

    public bool IsCursorInsideWindow()
    {
        if (!GetCursorPos(out var pt) || !GetWindowRect(_hwnd, out var r)) return false;
        return pt.X >= r.Left && pt.X <= r.Right && pt.Y >= r.Top && pt.Y <= r.Bottom;
    }

    public bool IsCursorAtTopCenterEdge(NewtonSettings settings)
    {
        if (!GetCursorPos(out var pt)) return false;

        var displays = DisplayArea.FindAll();
        DisplayArea targetDisplay = DisplayArea.Primary;
        if (settings.SelectedMonitorIndex >= 0 && settings.SelectedMonitorIndex < displays.Count)
        {
            targetDisplay = displays[settings.SelectedMonitorIndex];
        }

        var scale = GetDpiScale();
        var outer = targetDisplay.OuterBounds;
        int centerX = outer.X + outer.Width / 2;
        int halfZone = (int)(160 * scale);
        int edgePx = (int)(4 * scale);

        return pt.Y <= outer.Y + edgePx &&
               pt.Y >= outer.Y &&
               pt.X >= centerX - halfZone &&
               pt.X <= centerX + halfZone;
    }

    public void BeginDrag()
    {
        ReleaseCapture();
        SendMessage(_hwnd, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
    }

    public void SetClickThrough(bool enable)
    {
        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
        if (enable) exStyle |= WS_EX_TRANSPARENT;
        else exStyle &= ~WS_EX_TRANSPARENT;
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
    }

    public static bool IsAltKeyDown() => (GetAsyncKeyState(0x12) & 0x8000) != 0;

    public void Dispose()
    {
        UnregisterHotKey(_hwnd, HOTKEY_TOGGLE_ID);
        if (_subclassDelegate is not null)
        {
            RemoveWindowSubclass(_hwnd, _subclassDelegate, new UIntPtr(1));
        }
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
    private const long WS_EX_TRANSPARENT = 0x00000020L;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_HOTKEY = 0x0312;
    private const int HTCAPTION = 2;
    private const int HOTKEY_TOGGLE_ID = 1001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int DWMWA_NCRENDERING_POLICY = 2;
    private const int DWMWA_BORDER_COLOR = 34;

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

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

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("comctl32.dll")]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll")]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
}
