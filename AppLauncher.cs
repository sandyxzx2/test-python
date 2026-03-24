using System.Diagnostics;
using System.Drawing;

namespace testSoulChat;

public static class AppLauncher
{
    private static readonly Point DefaultChatGptTaskbarIcon = new(122, 1056);
    private static readonly Point DefaultAndroidEmulatorTaskbarIcon = new(1561, 1056);
    private static readonly Point DefaultNewChatButton = new(72, 198);
    private static readonly Point DefaultModelSelectorButton = new(395, 145);
    private static readonly Point DefaultModelOptionButton = new(350, 315);
    private static readonly Point DefaultChatGptInputPoint = new(1055, 533);

    private static readonly string[] MuMuDefaultPaths =
    {
        @"D:\\Program Files\\Netease\\MuMuPlayer\\nx_main\\MuMuNxMain.exe",
        @"C:\\Program Files\\Netease\\MuMuPlayer\\nx_main\\MuMuNxMain.exe",
        @"C:\\Program Files\\Netease\\MuMuPlayer-12.0\\shell\\MuMuPlayer.exe",
        @"C:\\Program Files\\Netease\\MuMuPlayerGlobal-12.0\\shell\\MuMuPlayer.exe",
        @"D:\\Program Files\\Netease\\MuMuPlayer-12.0\\shell\\MuMuPlayer.exe"
    };

    public static bool EnsureMuMuRunning(Action<string> log)
    {
        if (Process.GetProcesses().Any(p =>
                p.MainWindowHandle != IntPtr.Zero &&
                (p.ProcessName.Contains("MuMu", StringComparison.OrdinalIgnoreCase)
                 || p.ProcessName.Contains("Nemu", StringComparison.OrdinalIgnoreCase)
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
            ClickNewChat(log);
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
        ClickChatGptTaskbar(log);
        Thread.Sleep(400);

        var inputPoint = ReadPoint("CHATGPT_INPUT_X", "CHATGPT_INPUT_Y", DefaultChatGptInputPoint);
        InputSimulator.LeftClick(inputPoint.X, inputPoint.Y);
        log($"已聚焦 ChatGPT 输入框：({inputPoint.X},{inputPoint.Y})");
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

    private static int ReadIntFromEnvironment(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var value) ? value : fallback;
    }
}
