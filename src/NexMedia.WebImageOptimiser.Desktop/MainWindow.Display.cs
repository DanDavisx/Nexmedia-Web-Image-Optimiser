using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow
{
    private bool fittingDisplay;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        FitCurrentDisplay();
        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(DisplayWindowMessage);
        Loaded += (_, _) => FitCurrentDisplay();
        SizeChanged += (_, _) => UpdateCompactLayout();
        UpdateCompactLayout();
    }

    private nint DisplayWindowMessage(nint handle, int message, nint wParam, nint lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (message == WmGetMinMaxInfo)
        {
            var monitor = new MonitorInformation { Size = Marshal.SizeOf<MonitorInformation>() };
            if (GetMonitorInfo(MonitorFromWindow(handle, 2), ref monitor))
            {
                // Borderless windows need explicit maximized bounds. Windows supplies
                // the work area in physical pixels, excluding the taskbar on any edge.
                // MaxPosition is relative to this monitor, not the virtual desktop.
                MinMaxInformation bounds = Marshal.PtrToStructure<MinMaxInformation>(lParam);
                bounds.MaxPosition.X = monitor.Work.Left - monitor.Monitor.Left;
                bounds.MaxPosition.Y = monitor.Work.Top - monitor.Monitor.Top;
                bounds.MaxSize.X = monitor.Work.Right - monitor.Work.Left;
                bounds.MaxSize.Y = monitor.Work.Bottom - monitor.Work.Top;
                Marshal.StructureToPtr(bounds, lParam, false);
                handled = true;
            }
            return 0;
        }

        // Recheck after dragging/resizing or a display configuration change.
        // Do not clamp during a drag: that would prevent crossing monitor edges.
        if (message is 0x0232 or 0x007E)
            Dispatcher.BeginInvoke(new Action(FitCurrentDisplay));
        return 0;
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        // Let WPF finish applying the new monitor's scale and window bounds first.
        Dispatcher.BeginInvoke(new Action(FitCurrentDisplay));
    }

    private void FitCurrentDisplay()
    {
        if (fittingDisplay) return;
        nint handle = new WindowInteropHelper(this).Handle;
        if (handle == 0) return;

        var monitor = new MonitorInformation
        {
            Size = Marshal.SizeOf<MonitorInformation>()
        };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref monitor)) return;

        fittingDisplay = true;
        try
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            double availableWidth = (monitor.Work.Right - monitor.Work.Left) / dpi.DpiScaleX;
            double availableHeight = (monitor.Work.Bottom - monitor.Work.Top) / dpi.DpiScaleY;

          
            double widthLimit = Math.Max(1, availableWidth - 24);
            double heightLimit = Math.Max(1, availableHeight - 24);
            MinWidth = Math.Min(1040, widthLimit);
            MinHeight = Math.Min(560, heightLimit);

            if (WindowState == WindowState.Normal)
            {
                Width = Math.Min(Width, widthLimit);
                Height = Math.Min(Height, heightLimit);

                if (GetWindowRect(handle, out NativeRectangle bounds))
                {
                    int width = (int)Math.Ceiling(Width * dpi.DpiScaleX);
                    int height = (int)Math.Ceiling(Height * dpi.DpiScaleY);
                    int x = Math.Clamp(bounds.Left, monitor.Work.Left,
                        Math.Max(monitor.Work.Left, monitor.Work.Right - width));
                    int y = Math.Clamp(bounds.Top, monitor.Work.Top,
                        Math.Max(monitor.Work.Top, monitor.Work.Bottom - height));
                    if (x != bounds.Left || y != bounds.Top)
                        SetWindowPos(handle, 0, x, y, 0, 0, 0x0015);
                }
            }
        }
        finally
        {
            fittingDisplay = false;
        }
    }

    private void UpdateCompactLayout()
    {
        bool compact = ActualHeight < 800;
        MainContent.Margin = compact ? new Thickness(16, 10, 16, 10) : new Thickness(26, 20, 26, 20);
        HeaderArea.Margin = compact ? new Thickness(2, 0, 2, 8) : new Thickness(2, 0, 2, 18);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInformation
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInformation
    {
        public int Size;
        public NativeRectangle Monitor;
        public NativeRectangle Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInformation information);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y,
        int width, int height, uint flags);
}
