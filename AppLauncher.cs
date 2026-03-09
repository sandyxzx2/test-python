using System.Diagnostics;

namespace testSoulChat;

public static class AppLauncher
{
    private static readonly string[] MuMuDefaultPaths =
    {
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
            log("检测到 MuMu 模拟器已运行。");
            return true;
        }

        var envPath = Environment.GetEnvironmentVariable("MUMU_EXE_PATH");
        var candidates = string.IsNullOrWhiteSpace(envPath)
            ? MuMuDefaultPaths
            : new[] { envPath }.Concat(MuMuDefaultPaths).ToArray();

        var exe = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(exe))
        {
            log("未找到 MuMu 可执行文件。请设置环境变量 MUMU_EXE_PATH。\n例如：MUMU_EXE_PATH=C:\\Program Files\\Netease\\MuMuPlayer-12.0\\shell\\MuMuPlayer.exe");
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

    public static void OpenChatGpt(Action<string> log)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://chatgpt.com/",
                UseShellExecute = true
            });

            log("已打开 ChatGPT 页面。");
        }
        catch (Exception ex)
        {
            log($"打开 ChatGPT 失败：{ex.Message}");
        }
    }
}
