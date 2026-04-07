using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.Json;

namespace testSoulChat;

public sealed class AutoSocialAssistantService : IDisposable
{
    private const double StartMatchX = 0.15;
    private const double StartMatchY = 0.355;
    private const double ProfileEntryX = 0.13;
    private const double ProfileEntryY = 0.22;
    private const double ChatInputX = 0.50;
    private const double ChatInputY = 0.88;
    private const double HomeTabX = 0.07;
    private const double HomeTabY = 0.95;
    private const double MatchCardX = 0.18;
    private const double MatchCardY = 0.28;
    private const double AvatarX = 0.22;
    private const double AvatarY = 0.18;
    private const double ViewProfileX = 0.92;
    private const double ViewProfileY = 0.20;
    private const double ScrollFocusX = 0.52;
    private const double ScrollFocusY = 0.70;

    private readonly WindowCaptureService _captureService;
    private readonly GeminiClient _geminiClient;
    private readonly RandomDelayScheduler _scheduler;
    private readonly Action<string> _log;

    private AssistantState _state = AssistantState.Home;
    private bool _isRunning;
    private bool _isStarting;

    public AutoSocialAssistantService(WindowCaptureService captureService, GeminiClient geminiClient, Action<string> log)
    {
        _captureService = captureService;
        _geminiClient = geminiClient;
        _log = log;
        _scheduler = new RandomDelayScheduler(60_000, 300_000, OnTick);
    }

    public void Start()
    {
        if (_isRunning || _isStarting)
        {
            return;
        }

        _ = InitializeAsync();
    }

    public void Stop()
    {
        _isStarting = false;
        _isRunning = false;
        _scheduler.Stop();
        _log("自动社交助手已停止。");
    }

    private async Task InitializeAsync()
    {
        _isStarting = true;

        try
        {
            AppLauncher.EnsureMuMuRunning(_log);
            AppLauncher.PrepareChatGpt(_log);
            await EnsureChatGptModelSelectedAsync();

            if (!_geminiClient.HasApiKey)
            {
                await RunSoulMatchAndDraftReplyWithChatGptAsync();
                _log("未配置 GEMINI_API_KEY，已完成固定流程：匹配后采集主页并提交给 ChatGPT 生成回复建议。");
                return;
            }

            _isRunning = true;
            _log("自动社交助手已启动。");
            var next = _scheduler.ScheduleNext();
            _log($"下次执行将在 {next / 1000} 秒后。");
        }
        catch (Exception ex)
        {
            _log($"启动异常：{ex.Message}");
        }
        finally
        {
            _isStarting = false;
        }
    }

    private async void OnTick()
    {
        if (!_isRunning || !_geminiClient.HasApiKey)
        {
            return;
        }

        try
        {
            EnsureEmulatorForeground(useTaskbarFallback: true, logSuccess: false);
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

    private async Task EnsureChatGptModelSelectedAsync()
    {
        await Task.Delay(1200);

        if (!_geminiClient.HasApiKey)
        {
            _log("使用固定坐标切换 ChatGPT 5.4。");
            AppLauncher.OpenChatGptModelMenu(_log);
            await Task.Delay(800);
            AppLauncher.ClickChatGpt54Option(_log);
            await Task.Delay(600);
            return;
        }

        using var screenshot = AppLauncher.CapturePrimaryScreen();
        var base64 = WindowCaptureService.ToBase64Png(screenshot);
        var prompt = """
请检查这张 ChatGPT 页面截图顶部左侧的模型名称。
如果当前已经明确显示为“ChatGPT 5.4 Thinking”或“ChatGPT 5.4”，返回：
{"isSelected":true}

如果当前不是这个模型，或者你无法确认，返回：
{"isSelected":false}

只返回 JSON，不要返回别的内容。
""";

        var response = await _geminiClient.AnalyzeScreenshotAsync(base64, prompt, CancellationToken.None);
        var isSelected = ParseModelSelected(response);

        if (isSelected)
        {
            _log("当前已经是 ChatGPT 5.4，无需重复选择。");
            return;
        }

        _log("当前不是 ChatGPT 5.4，开始切换模型。");
        AppLauncher.OpenChatGptModelMenu(_log);
        await Task.Delay(900);
        await ClickChatGpt54OptionAsync();
    }

    private async Task GenerateDraftWithChatGptAsync()
    {
        _log("开始采集 Soul 主页资料。");
        EnsureEmulatorForeground(useTaskbarFallback: true, logSuccess: true);
        await Task.Delay(450);
        await OpenSoulProfileAsync();

        using var topScreenshot = CaptureSoulScreenOrThrow();
        await Task.Delay(900);

        ScrollSoulProfile(-650, "向下滚动主页");
        await Task.Delay(1400);
        using var middleScreenshot = CaptureSoulScreenOrThrow();

        ScrollSoulProfile(-650, "继续滚动主页");
        await Task.Delay(1400);
        using var lowerScreenshot = CaptureSoulScreenOrThrow();

        _log("开始提交资料给 ChatGPT。");
        await SubmitProfileToChatGptAsync(new[] { topScreenshot, middleScreenshot, lowerScreenshot });
    }

    private async Task OpenSoulProfileAsync()
    {
        ClickMuMuRelative(StartMatchX, StartMatchY, "开始匹配");
        await Task.Delay(6500);

        ClickMuMuRelative(AvatarX, AvatarY, "头像");
        await Task.Delay(2800);

        ClickMuMuRelative(ViewProfileX, ViewProfileY, "查看主页");
        await Task.Delay(4200);
    }

    private async Task SubmitProfileToChatGptAsync(IReadOnlyList<Bitmap> screenshots)
    {
        AppLauncher.FocusChatGptComposer(_log);
        await Task.Delay(260);

        foreach (var screenshot in screenshots)
        {
            ClipboardHelper.SetImage(screenshot);
            InputSimulator.PasteClipboard();
            await Task.Delay(1500);
        }

        var prompt = """
根据我刚刚粘贴的 Soul 用户资料截图，生成一条“有趣一点”的首条私聊开场白。
要求：
1. 结合对方资料里的具体细节，不要空泛。
2. 语气自然、轻松、有趣，不油腻。
3. 长度控制在 25 到 45 个中文字符。
4. 像真人聊天，不要像模板，不要分点。
5. 只输出最终一句可直接发送的话，不要解释。
""";
        await PastePromptAndSendToChatGptAsync(prompt);
        _log("已提交给 ChatGPT 生成有趣版待发送内容。");
    }

    private async Task SubmitProfileToChatGptForReplyAsync(IReadOnlyList<Bitmap> screenshots)
    {
        AppLauncher.FocusChatGptComposer(_log);
        await Task.Delay(260);

        foreach (var screenshot in screenshots)
        {
            ClipboardHelper.SetImage(screenshot);
            InputSimulator.PasteClipboard();
            await Task.Delay(1500);
        }

        var prompt = """
根据我刚刚粘贴的 Soul 用户主页连续截图，给我一条现在就能发的回复建议。
要求：
1. 结合主页里的具体细节，不要空泛。
2. 语气自然礼貌，不油腻，不说教。
3. 长度控制在 20 到 40 个中文字符。
4. 只输出最终这一句，不要解释。
""";
        await PastePromptAndSendToChatGptAsync(prompt);
    }

    private async Task PastePromptAndSendToChatGptAsync(string prompt)
    {
        ClipboardHelper.SetText(prompt);
        AppLauncher.FocusChatGptComposer(_log);
        await Task.Delay(260);

        InputSimulator.PasteClipboard();
        var uploadWaitMs = ReadIntFromEnvironment("CHATGPT_UPLOAD_WAIT_MS", 15_000);
        _log($"等待 ChatGPT 上传图片完成（{uploadWaitMs / 1000.0:F1} 秒）...");
        await Task.Delay(uploadWaitMs);
        InputSimulator.PressEnter();
        await Task.Delay(300);
    }

    private async Task RunSoulMatchAndDraftReplyWithChatGptAsync()
    {
        _log("开始执行 Soul 固定流程。");
        EnsureEmulatorForeground(useTaskbarFallback: true, logSuccess: true);
        await Task.Delay(450);

        ClickMuMuRelative(
            ReadDoubleFromEnvironment("SOUL_HOME_TAB_X", HomeTabX),
            ReadDoubleFromEnvironment("SOUL_HOME_TAB_Y", HomeTabY),
            "星球首页");
        await Task.Delay(350);

        ClickMuMuRelative(
            ReadDoubleFromEnvironment("SOUL_MATCH_CARD_X", MatchCardX),
            ReadDoubleFromEnvironment("SOUL_MATCH_CARD_Y", MatchCardY),
            "灵魂匹配卡片");
        await Task.Delay(300);

        ClickStartMatchWithFallbacks();
        var waitMs = ReadIntFromEnvironment("SOUL_MATCH_WAIT_MS", 18_000);
        _log($"正在等待匹配结果，预计等待 {waitMs / 1000.0:F1} 秒。");
        await Task.Delay(waitMs);

        ClickProfileEntryAndWaitForLoad();

        var profileScreenshots = await CaptureProfileTimelineAsync();
        try
        {
            _log("开始提交主页截图给 ChatGPT。");
            await SubmitProfileToChatGptForReplyAsync(profileScreenshots);
            _log("已提交给 ChatGPT 生成回复建议。");
        }
        finally
        {
            foreach (var screenshot in profileScreenshots)
            {
                screenshot.Dispose();
            }
        }
    }

    private Bitmap CaptureSoulScreenOrThrow()
    {
        using var screenshot = _captureService.CaptureWindow();
        if (screenshot is null)
        {
            throw new InvalidOperationException("未找到 MuMu 窗口，无法截图 Soul 页面。");
        }

        return ScaleBitmap(screenshot, 2.0);
    }

    private void ClickMuMuRelative(double x, double y, string actionLabel)
    {
        EnsureEmulatorForeground(useTaskbarFallback: false, logSuccess: false);

        if (!_captureService.TryGetWindowRect(out var rect))
        {
            throw new InvalidOperationException("无法获取 MuMu 窗口坐标。");
        }

        var absolute = CoordinateMapper.RelativeToAbsolute(x, y, rect);
        InputSimulator.LeftClick(absolute.X, absolute.Y);
        _log($"已点击{actionLabel}：({absolute.X},{absolute.Y})");
    }

    private void ScrollSoulProfile(int delta, string actionLabel)
    {
        EnsureEmulatorForeground(useTaskbarFallback: false, logSuccess: false);

        if (!_captureService.TryGetWindowRect(out var rect))
        {
            throw new InvalidOperationException("无法获取 MuMu 窗口坐标。");
        }

        var focus = CoordinateMapper.RelativeToAbsolute(ScrollFocusX, ScrollFocusY, rect);
        InputSimulator.LeftClick(focus.X, focus.Y);
        Thread.Sleep(150);
        InputSimulator.ScrollVertical(delta);
        _log($"已执行{actionLabel}。");
    }

    private void ClickProfileEntryAndWaitForLoad()
    {
        var x = ReadDoubleFromEnvironment("SOUL_PROFILE_ENTRY_X", ProfileEntryX);
        var y = ReadDoubleFromEnvironment("SOUL_PROFILE_ENTRY_Y", ProfileEntryY);
        ClickMuMuRelative(x, y, "主页入口(头像)");

        var waitMs = ReadIntFromEnvironment("SOUL_PROFILE_LOAD_WAIT_MS", 3_500);
        _log($"已点击主页入口，等待加载 {waitMs / 1000.0:F1} 秒。");
        Thread.Sleep(waitMs);
    }

    private async Task<List<Bitmap>> CaptureProfileTimelineAsync()
    {
        var maxShots = Math.Clamp(ReadIntFromEnvironment("SOUL_PROFILE_SHOT_COUNT", 5), 2, 8);
        var scrollDelta = -Math.Abs(ReadIntFromEnvironment("SOUL_PROFILE_SCROLL_DELTA", 720));
        var scrollDelayMs = ReadIntFromEnvironment("SOUL_PROFILE_SCROLL_DELAY_MS", 1_250);
        var duplicateThreshold = Math.Clamp(ReadIntFromEnvironment("SOUL_PROFILE_DUPLICATE_THRESHOLD", 6), 1, 20);

        var screenshots = new List<Bitmap>(maxShots);
        var hashes = new List<ulong>(maxShots);

        for (var i = 0; i < maxShots; i++)
        {
            var screenshot = CaptureSoulScreenOrThrow();
            var hash = ComputeAverageHash(screenshot);

            if (hashes.Count > 0 && HammingDistance(hash, hashes[^1]) <= duplicateThreshold)
            {
                screenshot.Dispose();
                _log("检测到滚动后画面重复，提前结束主页截图。");
                break;
            }

            screenshots.Add(screenshot);
            hashes.Add(hash);
            _log($"主页截图进度：{screenshots.Count}/{maxShots}");

            if (i == maxShots - 1)
            {
                break;
            }

            ScrollSoulProfile(scrollDelta, $"主页滚动({screenshots.Count})");
            await Task.Delay(scrollDelayMs);
        }

        return screenshots;
    }

    private static Bitmap ScaleBitmap(Bitmap source, double scale)
    {
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var bitmap = new Bitmap(width, height);

        using var g = Graphics.FromImage(bitmap);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.DrawImage(source, new Rectangle(0, 0, width, height));
        return bitmap;
    }

    private async Task ClickChatGpt54OptionAsync()
    {
        using var screenshot = AppLauncher.CapturePrimaryScreen();
        var base64 = WindowCaptureService.ToBase64Png(screenshot);
        var prompt = """
你正在查看 ChatGPT 的模型下拉菜单截图。
请找到“ChatGPT 5.4”或“ChatGPT 5.4 Thinking”这个可点击选项的中心位置。
如果找到了，返回：
{"action":"click","x":0.5,"y":0.5}

如果没找到，返回：
{"action":"noop","x":0.5,"y":0.5}

要求：
1) x 和 y 是相对整张截图的坐标，范围 0 到 1
2) 只返回 JSON
""";

        var response = await _geminiClient.AnalyzeScreenshotAsync(base64, prompt, CancellationToken.None);
        var cmd = ParseCommand(response);
        if (!cmd.Action.Equals("click", StringComparison.OrdinalIgnoreCase))
        {
            _log("没有识别到 ChatGPT 5.4 的下拉选项。");
            return;
        }

        var screenBounds = GetPrimaryScreenBounds();
        var absolute = CoordinateMapper.RelativeToAbsolute(cmd.X, cmd.Y, screenBounds);
        InputSimulator.LeftClick(absolute.X, absolute.Y);
        _log($"已点击 ChatGPT 5.4 下拉选项：({absolute.X},{absolute.Y})");
    }

    private async Task ExecuteCurrentStateAsync()
    {
        using var screenshot = _captureService.CaptureWindow();
        if (screenshot is null)
        {
            _log("未找到 MuMu 窗口或截图失败。");
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
            _log("无法获取模拟器窗口坐标，已跳过本轮。");
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

    private static Rectangle GetPrimaryScreenBounds()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;
        return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    private static int ReadIntFromEnvironment(string key, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    private void EnsureEmulatorForeground(bool useTaskbarFallback, bool logSuccess)
    {
        if (_captureService.TryBringTargetWindowToFront(out var matchedLabel))
        {
            if (logSuccess && !string.IsNullOrWhiteSpace(matchedLabel))
            {
                _log($"已通过句柄激活模拟器窗口：{matchedLabel}");
            }

            return;
        }

        if (!useTaskbarFallback)
        {
            return;
        }

        _log("句柄激活模拟器失败，尝试任务栏兜底激活。");
        AppLauncher.FocusAndroidEmulator(_log);
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

    private static ulong ComputeAverageHash(Bitmap source)
    {
        using var resized = new Bitmap(8, 8);
        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, 8, 8));
        }

        var luminance = new int[64];
        var sum = 0;
        var index = 0;

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var pixel = resized.GetPixel(x, y);
                var value = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                luminance[index++] = value;
                sum += value;
            }
        }

        var average = sum / 64;
        ulong hash = 0;
        for (var i = 0; i < luminance.Length; i++)
        {
            if (luminance[i] >= average)
            {
                hash |= 1UL << i;
            }
        }

        return hash;
    }

    private static int HammingDistance(ulong a, ulong b)
    {
        var value = a ^ b;
        var count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }

    private void ClickStartMatchWithFallbacks()
    {
        ClickStartMatchByWindowOriginAndResolution();
        Thread.Sleep(220);

        var primaryX = ReadDoubleFromEnvironment("SOUL_START_MATCH_X", StartMatchX);
        var primaryY = ReadDoubleFromEnvironment("SOUL_START_MATCH_Y", StartMatchY);

        var points = new (double X, double Y, string Label)[]
        {
            (primaryX, primaryY, "开始匹配(主点)"),
            (0.14, 0.355, "开始匹配(兜底1)"),
            (0.18, 0.355, "开始匹配(兜底2)"),
            (0.16, 0.335, "开始匹配(兜底3)")
        };

        foreach (var point in points)
        {
            ClickMuMuRelative(point.X, point.Y, point.Label);
            Thread.Sleep(200);
        }
    }

    private void ClickStartMatchByWindowOriginAndResolution()
    {
        if (!_captureService.TryGetWindowRect(out var rect))
        {
            throw new InvalidOperationException("无法获取 MuMu 窗口坐标。");
        }

        var screen = Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;
        var referenceWidth = ReadIntFromEnvironment("SOUL_REF_SCREEN_WIDTH", 2560);
        var referenceHeight = ReadIntFromEnvironment("SOUL_REF_SCREEN_HEIGHT", 1440);

        var baseOffsetX = ReadIntFromEnvironment("SOUL_START_MATCH_OFFSET_X", 88);
        var baseOffsetY = ReadIntFromEnvironment("SOUL_START_MATCH_OFFSET_Y", 322);

        var scaleX = screen.Width / (double)Math.Max(1, referenceWidth);
        var scaleY = screen.Height / (double)Math.Max(1, referenceHeight);

        var targetX = rect.Left + (int)Math.Round(baseOffsetX * scaleX);
        var targetY = rect.Top + (int)Math.Round(baseOffsetY * scaleY);

        InputSimulator.LeftClick(targetX, targetY);
        _log($"已点击开始匹配(句柄+分辨率)：({targetX},{targetY})");
    }

    private void ClickChatInputWithFallbacks()
    {
        var primaryX = ReadDoubleFromEnvironment("SOUL_CHAT_INPUT_X", ChatInputX);
        var primaryY = ReadDoubleFromEnvironment("SOUL_CHAT_INPUT_Y", ChatInputY);

        var points = new (double X, double Y, string Label)[]
        {
            (primaryX, primaryY, "聊天输入框(主点)"),
            (0.50, 0.86, "聊天输入框(兜底1)"),
            (0.50, 0.90, "聊天输入框(兜底2)")
        };

        foreach (var point in points)
        {
            ClickMuMuRelative(point.X, point.Y, point.Label);
            Thread.Sleep(180);
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
            AssistantState.Home => "识别 Soul 匹配剩余次数。若有次数则返回点击开始匹配，否则返回随机浏览建议。",
            AssistantState.ProfileAnalysis => "识别对方标签、瞬间、MBTI，并生成 5 字以上个性化打招呼语。",
            AssistantState.Chat => "识别最新聊天内容，并输出符合语境的自然回复。",
            AssistantState.AntiDetection => "执行随机主页或广场轻度浏览，避免固定轨迹。",
            _ => "执行下一步操作。"
        };

        return $$"""
你是自动社交助手的视觉决策核心。
请仅返回 JSON，格式如下：
{"action":"click|type|noop","x":0.5,"y":0.5,"text":"可选文本"}
要求：
1) x/y 是相对坐标（0-1）
2) 回复语气自然，不要模板化
3) 当前状态任务：{{stateInstruction}}
""";
    }

    private static bool ParseModelSelected(string raw)
    {
        try
        {
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                return false;
            }

            var json = raw[jsonStart..(jsonEnd + 1)];
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("isSelected", out var value) &&
                   value.ValueKind == JsonValueKind.True;
        }
        catch
        {
            return false;
        }
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
