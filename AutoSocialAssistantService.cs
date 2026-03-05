using System.Text.Json;

namespace testSoulChat;

public sealed class AutoSocialAssistantService : IDisposable
{
    private readonly WindowCaptureService _captureService;
    private readonly GeminiClient _geminiClient;
    private readonly RandomDelayScheduler _scheduler;
    private readonly Action<string> _log;

    private AssistantState _state = AssistantState.Home;
    private bool _isRunning;

    public AutoSocialAssistantService(WindowCaptureService captureService, GeminiClient geminiClient, Action<string> log)
    {
        _captureService = captureService;
        _geminiClient = geminiClient;
        _log = log;
        _scheduler = new RandomDelayScheduler(60_000, 300_000, OnTick);
    }

    public void Start()
    {
        AppLauncher.EnsureMuMuRunning(_log);
        AppLauncher.OpenGemini(_log);

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _log("自动社交助手已启动。");
        var next = _scheduler.ScheduleNext();
        _log($"下次执行将在 {next / 1000} 秒后。");
    }

    public void Stop()
    {
        _isRunning = false;
        _scheduler.Stop();
        _log("自动社交助手已停止。");
    }

    private async void OnTick()
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            await ExecuteCurrentStateAsync();
            _state = NextState(_state);
        }
        catch (Exception ex)
        {
            _log($"执行异常：{ex.Message}");
        }
        finally
        {
            if (_isRunning)
            {
                var next = _scheduler.ScheduleNext();
                _log($"已调度下一轮任务，{next / 1000} 秒后执行。");
            }
        }
    }

    private async Task ExecuteCurrentStateAsync()
    {
        using var screenshot = _captureService.CaptureWindow();
        if (screenshot is null)
        {
            _log("未找到 MuMu 窗口或截图失败（已启用进程别名+标题关键字匹配）。");
            return;
        }

        var base64 = WindowCaptureService.ToBase64Png(screenshot);
        var prompt = BuildPrompt(_state);
        var response = await _geminiClient.AnalyzeScreenshotAsync(base64, prompt, CancellationToken.None);
        _log($"[{_state}] AI回复：{response}");

        var cmd = ParseCommand(response);
        await ExecuteCommandAsync(cmd);
    }

    private async Task ExecuteCommandAsync(AiActionCommand cmd)
    {
        if (!_captureService.TryGetWindowRect(out var rect))
        {
            _log("无法获取模拟器窗口坐标。操作跳过。");
            return;
        }

        if (cmd.Action.Equals("click", StringComparison.OrdinalIgnoreCase))
        {
            var absolute = CoordinateMapper.RelativeToAbsolute(cmd.X, cmd.Y, rect);
            var jittered = CoordinateMapper.ApplyJitter(absolute, 10);
            InputSimulator.LeftClick(jittered.X, jittered.Y);
            _log($"执行点击：({jittered.X},{jittered.Y})");
        }

        if (!string.IsNullOrWhiteSpace(cmd.Text))
        {
            await Task.Delay(Random.Shared.Next(1200, 4500));
            InputSimulator.TypeText(cmd.Text);
            _log($"执行输入：{cmd.Text}");
        }
    }

    private static AssistantState NextState(AssistantState state) => state switch
    {
        AssistantState.Home => AssistantState.ProfileAnalysis,
        AssistantState.ProfileAnalysis => AssistantState.Chat,
        AssistantState.Chat => AssistantState.AntiDetection,
        _ => AssistantState.Home
    };

    private static string BuildPrompt(AssistantState state)
    {
        var stateInstruction = state switch
        {
            AssistantState.Home => "识别灵魂匹配剩余次数。若有次数则返回点击开始匹配，否则返回随机浏览建议。",
            AssistantState.ProfileAnalysis => "识别对方标签、瞬间、MBTI并生成15字以上个性化打招呼语。",
            AssistantState.Chat => "识别最新聊天内容并输出符合语境的自然回复。",
            AssistantState.AntiDetection => "执行随机主页/广场轻度浏览，避免固定轨迹。",
            _ => "执行下一步"
        };

        return $$"""
你是自动社交助手的视觉决策核心。
请仅返回JSON，格式如下：
{"action":"click|type|noop","x":0.5,"y":0.5,"text":"可选文本"}
要求：
1) x/y 是相对坐标（0-1）
2) 回复语气自然，不要模板化
3) 当前状态任务：{{stateInstruction}}
""";
    }

    private static AiActionCommand ParseCommand(string raw)
    {
        try
        {
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                return AiActionCommand.Noop();
            }

            var json = raw[jsonStart..(jsonEnd + 1)];
            return JsonSerializer.Deserialize<AiActionCommand>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? AiActionCommand.Noop();
        }
        catch
        {
            return AiActionCommand.Noop();
        }
    }

    public void Dispose() => _scheduler.Dispose();

    private enum AssistantState
    {
        Home,
        ProfileAnalysis,
        Chat,
        AntiDetection
    }

    private sealed record AiActionCommand(string Action, double X, double Y, string? Text)
    {
        public static AiActionCommand Noop() => new("noop", 0.5, 0.5, null);
    }
}
