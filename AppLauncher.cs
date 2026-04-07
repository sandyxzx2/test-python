using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace testSoulChat;

public static class AppLauncher
{
    private const int SwShow = 5;
    private const int SwRestore = 9;
    private static readonly object ChatGptWindowLock = new();
    private static IntPtr _cachedChatGptWindowHandle = IntPtr.Zero;

    private static readonly string[] EmulatorProcessNames =
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

    private static readonly Point DefaultChatGptTaskbarIcon = new(122, 1056);
    private static readonly Point DefaultAndroidEmulatorTaskbarIcon = new(1561, 1056);
    private static readonly Point DefaultNewChatButton = new(72, 198);
    private static readonly Point DefaultModelSelectorButton = new(395, 145);
    private static readonly Point DefaultModelOptionButton = new(350, 315);
    private static readonly Point DefaultChatGptInputPoint = new(1055, 533);
    private static readonly Point DefaultChatGptSendPoint = new(1148, 704);

    private static readonly string[] BrowserProcessAliases =
    {
        "chrome",
        "msedge",
        "firefox",
        "brave",
        "opera"
    };

    private static readonly string[] ChatGptTitleKeywords =
    {
        "ChatGPT",
        "chatgpt.com"
    };

    private static readonly string[] MuMuDefaultPaths =
    {
        @"D:\\Program Files\\Netease\\MuMuPlayer\\nx_main\\MuMuNxMain.exe",
        @"C:\\Program Files\\Netease\\MuMuPlayer\\nx_main\\MuMuNxMain.exe",
        @"C:\\Program Files\\Netease\\MuMuPlayer-12.0\\shell\\MuMuPlayer.exe",
        @"C:\\Program Files\\Netease\\MuMuPlayerGlobal-12.0\\shell\\MuMuPlayer.exe",
        @"D:\\Program Files\\Netease\\MuMuPlayer-12.0\\shell\\MuMuPlayer.exe"
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

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

    public static bool EnsureMuMuRunning(Action<string> log)
    {
        if (Process.GetProcesses().Any(p =>
                p.MainWindowHandle != IntPtr.Zero &&
                (EmulatorProcessNames.Any(name => p.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase))
                 || p.MainWindowTitle.Contains("MuMu", StringComparison.OrdinalIgnoreCase))))
        {
            log("检测到 MuMu 模拟器已经在运行。");
            return true;
        }

        var envPath = Environment.GetEnvironmentVariable("MUMU_EXE_PATH");
        var candidates = string.IsNullOrWhiteSpace(envPath)
            ? MuMuDefaultPaths
            : new[] { envPath }.Concat(MuMuDefaultPaths).ToArray();

        var exe = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(exe))
        {
            log("未找到 MuMu 可执行文件。请设置环境变量 MUMU_EXE_PATH。");
            return false;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true
        });

        log($"已尝试启动 MuMu：{exe}");
        return true;
    }

    public static void PrepareChatGpt(Action<string> log)
    {
        try
        {
            ClickChatGptTaskbar(log);
            Thread.Sleep(1200);
            CacheForegroundChatGptWindow(log);
            ClickNewChat(log);
            Thread.Sleep(350);
            CacheForegroundChatGptWindow(log);
        }
        catch (Exception ex)
        {
            log($"打开 ChatGPT 失败：{ex.Message}");
        }
    }

    public static void ClickChatGptTaskbar(Action<string> log)
    {
        var taskbarPoint = ReadPoint("CHATGPT_TASKBAR_ICON_X", "CHATGPT_TASKBAR_ICON_Y", DefaultChatGptTaskbarIcon);
        InputSimulator.LeftClick(taskbarPoint.X, taskbarPoint.Y);
        log($"已点击任务栏中的 ChatGPT Chrome：({taskbarPoint.X},{taskbarPoint.Y})");
    }

    public static void FocusAndroidEmulator(Action<string> log)
    {
        var taskbarPoint = ReadPoint("ANDROID_EMULATOR_TASKBAR_X", "ANDROID_EMULATOR_TASKBAR_Y", DefaultAndroidEmulatorTaskbarIcon);
        InputSimulator.LeftClick(taskbarPoint.X, taskbarPoint.Y);
        log($"已点击安卓模拟器任务栏图标：({taskbarPoint.X},{taskbarPoint.Y})");
    }

    public static void ClickNewChat(Action<string> log)
    {
        var newChatPoint = ReadPoint("CHATGPT_NEW_CHAT_X", "CHATGPT_NEW_CHAT_Y", DefaultNewChatButton);
        InputSimulator.LeftClick(newChatPoint.X, newChatPoint.Y);
        log($"已点击 ChatGPT 的新聊天按钮：({newChatPoint.X},{newChatPoint.Y})");
    }

    public static void OpenChatGptModelMenu(Action<string> log)
    {
        var modelSelectorPoint = ReadPoint("CHATGPT_MODEL_SELECTOR_X", "CHATGPT_MODEL_SELECTOR_Y", DefaultModelSelectorButton);
        InputSimulator.LeftClick(modelSelectorPoint.X, modelSelectorPoint.Y);
        log($"已点击 ChatGPT 模型选择器：({modelSelectorPoint.X},{modelSelectorPoint.Y})");
    }

    public static void ClickChatGpt54Option(Action<string> log)
    {
        var modelOptionPoint = ReadPoint("CHATGPT_MODEL_OPTION_X", "CHATGPT_MODEL_OPTION_Y", DefaultModelOptionButton);
        InputSimulator.LeftClick(modelOptionPoint.X, modelOptionPoint.Y);
        log($"已点击 ChatGPT 5.4 选项：({modelOptionPoint.X},{modelOptionPoint.Y})");
    }

    public static void FocusChatGptComposer(Action<string> log)
    {
        var hasWindowRect = TryBringChatGptWindowToFront(out var chatGptRect, out var matchedWindow);
        if (hasWindowRect)
        {
            log($"已通过句柄激活 ChatGPT 窗口：{matchedWindow}");
        }
        else
        {
            log("句柄激活 ChatGPT 失败，使用任务栏兜底。");
            ClickChatGptTaskbar(log);
            Thread.Sleep(420);
            CacheForegroundChatGptWindow(log);
            hasWindowRect = TryGetChatGptWindowRect(out chatGptRect, out _);
        }

        Thread.Sleep(220);

        var inputPoint = ResolveChatGptPoint(
            "CHATGPT_INPUT_X",
            "CHATGPT_INPUT_Y",
            "CHATGPT_INPUT_REL_X",
            "CHATGPT_INPUT_REL_Y",
            0.52,
            0.50,
            hasWindowRect ? chatGptRect : Rectangle.Empty,
            DefaultChatGptInputPoint);

        InputSimulator.LeftClick(inputPoint.X, inputPoint.Y);
        log($"已聚焦 ChatGPT 输入框：({inputPoint.X},{inputPoint.Y})");
    }

    public static void ClickChatGptSend(Action<string> log)
    {
        var hasWindowRect = TryBringChatGptWindowToFront(out var chatGptRect, out _);
        if (!hasWindowRect)
        {
            ClickChatGptTaskbar(log);
            Thread.Sleep(350);
            CacheForegroundChatGptWindow(log);
            hasWindowRect = TryGetChatGptWindowRect(out chatGptRect, out _);
        }

        var sendPoint = ResolveChatGptPoint(
            "CHATGPT_SEND_X",
            "CHATGPT_SEND_Y",
            "CHATGPT_SEND_REL_X",
            "CHATGPT_SEND_REL_Y",
            0.965,
            0.905,
            hasWindowRect ? chatGptRect : Rectangle.Empty,
            DefaultChatGptSendPoint);

        InputSimulator.LeftClick(sendPoint.X, sendPoint.Y);
        log($"已点击 ChatGPT 发送按钮：({sendPoint.X},{sendPoint.Y})");
    }

    public static Bitmap CapturePrimaryScreen()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return bitmap;
    }

    private static Point ReadPoint(string xKey, string yKey, Point fallback)
    {
        var x = ReadIntFromEnvironment(xKey, fallback.X);
        var y = ReadIntFromEnvironment(yKey, fallback.Y);
        return new Point(x, y);
    }

    private static Point ResolveChatGptPoint(
        string absXKey,
        string absYKey,
        string relXKey,
        string relYKey,
        double fallbackRelX,
        double fallbackRelY,
        Rectangle chatGptRect,
        Point fallbackAbsolute)
    {
        var absXRaw = Environment.GetEnvironmentVariable(absXKey);
        var absYRaw = Environment.GetEnvironmentVariable(absYKey);
        if (int.TryParse(absXRaw, out var absX) && int.TryParse(absYRaw, out var absY))
        {
            return new Point(absX, absY);
        }

        if (chatGptRect.Width > 0 && chatGptRect.Height > 0)
        {
            var relX = ReadDoubleFromEnvironment(relXKey, fallbackRelX);
            var relY = ReadDoubleFromEnvironment(relYKey, fallbackRelY);
            return CoordinateMapper.RelativeToAbsolute(relX, relY, chatGptRect);
        }

        return fallbackAbsolute;
    }

    private static int ReadIntFromEnvironment(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    private static double ReadDoubleFromEnvironment(string key, double fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return fallback;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static bool TryBringChatGptWindowToFront(out Rectangle rect, out string matchedWindow)
    {
        rect = Rectangle.Empty;
        matchedWindow = string.Empty;
        if (TryBringCachedChatGptWindowToFront(out rect, out matchedWindow))
        {
            return true;
        }

        if (!TryGetChatGptWindow(out var process, out rect))
        {
            return false;
        }

        matchedWindow = $"{process.ProcessName} ({process.MainWindowTitle})";
        var hwnd = process.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        CacheChatGptWindowHandle(hwnd);
        return TryBringWindowToFront(hwnd);
    }

    private static bool TryBringCachedChatGptWindowToFront(out Rectangle rect, out string matchedWindow)
    {
        rect = Rectangle.Empty;
        matchedWindow = string.Empty;

        var cachedHwnd = GetCachedChatGptWindowHandle();
        if (cachedHwnd == IntPtr.Zero || !IsWindow(cachedHwnd))
        {
            return false;
        }

        if (!TryGetWindowRect(cachedHwnd, out rect))
        {
            return false;
        }

        if (!TryGetProcessByWindowHandle(cachedHwnd, out var process))
        {
            return false;
        }

        matchedWindow = $"{process.ProcessName} ({process.MainWindowTitle})";
        return TryBringWindowToFront(cachedHwnd);
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

    private static bool TryGetChatGptWindowRect(out Rectangle rect, out string matchedWindow)
    {
        rect = Rectangle.Empty;
        matchedWindow = string.Empty;
        if (!TryGetChatGptWindow(out var process, out rect))
        {
            return false;
        }

        matchedWindow = $"{process.ProcessName} ({process.MainWindowTitle})";
        return true;
    }

    private static bool TryGetChatGptWindow(out Process process, out Rectangle rect)
    {
        process = null!;
        rect = Rectangle.Empty;
        var preferredBrowser = GetPreferredChatGptBrowserProcessName();

        var candidate = Process.GetProcesses()
            .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(p.MainWindowTitle))
            .Where(p => BrowserProcessAliases.Any(alias => p.ProcessName.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new
            {
                Process = p,
                IsPreferredBrowser = p.ProcessName.Equals(preferredBrowser, StringComparison.OrdinalIgnoreCase),
                IsChatGptTitle = ChatGptTitleKeywords.Any(keyword =>
                    p.MainWindowTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase)),
                Rect = TryGetWindowRect(p.MainWindowHandle, out var r) ? r : Rectangle.Empty
            })
            .Where(x => x.IsPreferredBrowser || x.IsChatGptTitle)
            .Where(x => x.Rect.Width > 0 && x.Rect.Height > 0)
            .OrderByDescending(x => x.IsPreferredBrowser)
            .ThenByDescending(x => x.IsChatGptTitle)
            .ThenByDescending(x => x.Rect.Width * x.Rect.Height)
            .FirstOrDefault();

        if (candidate is null)
        {
            return false;
        }

        process = candidate.Process;
        rect = candidate.Rect;
        CacheChatGptWindowHandle(process.MainWindowHandle);
        return true;
    }

    private static string GetPreferredChatGptBrowserProcessName()
    {
        var raw = Environment.GetEnvironmentVariable("CHATGPT_BROWSER_PROCESS");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "chrome";
        }

        return raw.Trim().Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static void CacheForegroundChatGptWindow(Action<string> log)
    {
        var hwnd = GetForegroundWindow();
        if (!TryCacheChatGptWindowHandle(hwnd))
        {
            return;
        }

        log("已记录 ChatGPT Chrome 句柄。");
    }

    private static bool TryCacheChatGptWindowHandle(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
        {
            return false;
        }

        if (!TryGetProcessByWindowHandle(hwnd, out var process))
        {
            return false;
        }

        var preferredBrowser = GetPreferredChatGptBrowserProcessName();
        var isPreferredBrowser = process.ProcessName.Equals(preferredBrowser, StringComparison.OrdinalIgnoreCase);
        var isChatGptTitle = ChatGptTitleKeywords.Any(keyword =>
            process.MainWindowTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (!isPreferredBrowser && !isChatGptTitle)
        {
            return false;
        }

        CacheChatGptWindowHandle(hwnd);
        return true;
    }

    private static void CacheChatGptWindowHandle(IntPtr hwnd)
    {
        lock (ChatGptWindowLock)
        {
            _cachedChatGptWindowHandle = hwnd;
        }
    }

    private static IntPtr GetCachedChatGptWindowHandle()
    {
        lock (ChatGptWindowLock)
        {
            return _cachedChatGptWindowHandle;
        }
    }

    private static bool TryGetProcessByWindowHandle(IntPtr hwnd, out Process process)
    {
        process = null!;
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
        {
            return false;
        }

        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            process = Process.GetProcessById((int)processId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetWindowRect(IntPtr hwnd, out Rectangle rect)
    {
        rect = Rectangle.Empty;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var nativeRect))
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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
