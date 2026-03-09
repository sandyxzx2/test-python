using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace testSoulChat;

public sealed class WindowCaptureService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    private static readonly string[] DefaultProcessAliases =
    {
        "MuMuPlayer",
        "MuMuPlayerGlobal",
        "NemuPlayer",
        "NemuPlayerShell",
        "HD-Player"
    };

    private static readonly string[] DefaultTitleKeywords =
    {
        "MuMu",
        "模拟器",
        "Nemu"
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

    public bool TryResolveTargetWindow(out IntPtr hwnd, out string matchedLabel)
    {
        // 1) 先按进程名别名匹配
        foreach (var alias in DefaultProcessAliases)
        {
            var process = Process.GetProcessesByName(alias).FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
            if (process is not null)
            {
                hwnd = process.MainWindowHandle;
                matchedLabel = $"{process.ProcessName} ({process.MainWindowTitle})";
                return true;
            }
        }

        // 2) 回退：按窗口标题关键字匹配
        var candidate = Process.GetProcesses()
            .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(p.MainWindowTitle))
            .FirstOrDefault(p => DefaultTitleKeywords.Any(k => p.MainWindowTitle.Contains(k, StringComparison.OrdinalIgnoreCase)));

        if (candidate is not null)
        {
            hwnd = candidate.MainWindowHandle;
            matchedLabel = $"{candidate.ProcessName} ({candidate.MainWindowTitle})";
            return true;
        }

        hwnd = IntPtr.Zero;
        matchedLabel = string.Empty;
        return false;
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
