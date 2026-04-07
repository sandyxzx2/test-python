using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace testSoulChat;

public sealed class WindowCaptureService
{
    private const int SwShow = 5;
    private const int SwRestore = 9;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private static readonly string[] DefaultProcessAliases =
    {
        "MuMuNxDevice",
        "MuMuNxMain",
        "MuMuPlayer",
        "NemuPlayer",
        "NemuLauncher",
        "MuMuVMMHeadless",
        "MuMuVMMSVC",
        "MuMuPlayerGlobal",
        "NemuPlayerShell",
        "HD-Player"
    };

    private static readonly string[] DefaultTitleKeywords =
    {
        "MuMu",
        "模拟器",
        "Nemu",
        "MuMu Android Device",
        "Android Device"
    };

    public bool TryGetWindowRect(out Rectangle rect)
    {
        rect = Rectangle.Empty;
        if (!TryResolveTargetWindow(out var hwnd, out _))
        {
            return false;
        }

        if (!GetWindowRect(hwnd, out var nativeRect))
        {
            return false;
        }

        rect = new Rectangle(
            nativeRect.Left,
            nativeRect.Top,
            nativeRect.Right - nativeRect.Left,
            nativeRect.Bottom - nativeRect.Top);

        return rect.Width > 0 && rect.Height > 0;
    }

    public Bitmap? CaptureWindow()
    {
        if (!TryResolveTargetWindow(out var hwnd, out _))
        {
            return null;
        }

        return CaptureWindow(hwnd);
    }

    public bool TryBringTargetWindowToFront(out string matchedLabel)
    {
        matchedLabel = string.Empty;
        if (!TryResolveTargetWindow(out var hwnd, out matchedLabel))
        {
            return false;
        }

        return TryBringWindowToFront(hwnd);
    }

    public bool TryResolveTargetWindow(out IntPtr hwnd, out string matchedLabel)
    {
        var windows = Process.GetProcesses()
            .Where(p => p.MainWindowHandle != IntPtr.Zero)
            .Select(p => new
            {
                Process = p,
                IsExactNameMatch = DefaultProcessAliases.Any(alias =>
                    p.ProcessName.Equals(alias, StringComparison.OrdinalIgnoreCase)),
                IsAliasMatch = DefaultProcessAliases.Any(alias =>
                    p.ProcessName.Contains(alias, StringComparison.OrdinalIgnoreCase)),
                IsTitleMatch = !string.IsNullOrWhiteSpace(p.MainWindowTitle) &&
                               DefaultTitleKeywords.Any(keyword =>
                                   p.MainWindowTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.IsExactNameMatch || x.IsAliasMatch || x.IsTitleMatch)
            .Select(x => new
            {
                x.Process,
                Priority = x.IsExactNameMatch ? 0 : x.IsAliasMatch ? 1 : 2,
                Left = TryGetWindowLeft(x.Process.MainWindowHandle, out var left) ? left : int.MinValue
            })
            .Where(x => x.Left != int.MinValue)
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.Left)
            .FirstOrDefault();

        if (windows is not null)
        {
            hwnd = windows.Process.MainWindowHandle;
            matchedLabel = $"{windows.Process.ProcessName} ({windows.Process.MainWindowTitle})";
            return true;
        }

        hwnd = IntPtr.Zero;
        matchedLabel = string.Empty;
        return false;
    }

    private static bool TryGetWindowLeft(IntPtr hwnd, out int left)
    {
        left = 0;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        left = rect.Left;
        return true;
    }

    private static bool TryBringWindowToFront(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
        {
            return false;
        }

        if (GetForegroundWindow() == hwnd)
        {
            return true;
        }

        if (IsIconic(hwnd))
        {
            ShowWindow(hwnd, SwRestore);
        }
        else
        {
            ShowWindow(hwnd, SwShow);
        }

        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);

        return GetForegroundWindow() == hwnd;
    }

    public Bitmap? CaptureWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd) || !GetWindowRect(hwnd, out var rect))
        {
            return null;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        var hdc = g.GetHdc();

        var success = PrintWindow(hwnd, hdc, 0);

        g.ReleaseHdc(hdc);
        if (success)
        {
            return bitmap;
        }

        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    public static string ToBase64Png(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
