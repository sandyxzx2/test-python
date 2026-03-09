# testwinform-0309 / testSoulChat 代码速览

## 1. 项目定位
这是一个 **.NET 8 WinForms 自动化助手**：
- 目标窗口：MuMu/Nemu/BlueStacks 等模拟器窗口。
- 核心能力：截图 → 发送给 Gemini 识别 → 解析成操作指令（点击/输入/不操作）→ 在窗口中执行。
- 运行方式：桌面窗体按钮启动/停止，后台按随机间隔循环执行。

## 2. 启动入口与 UI
- `Program.cs`：标准 WinForms 入口，`Application.Run(new Form1())`。
- `Form1.cs`：
  - 启动时从环境变量读取 `GEMINI_API_KEY`，构造 `GeminiClient` 与 `AutoSocialAssistantService`。
  - “启动助手”按钮触发 `Start()`；“停止助手”按钮触发 `Stop()`。
  - `AppendLog` 通过 `InvokeRequired` 处理跨线程日志写入。
- `Form1.Designer.cs`：三个控件（启动按钮、停止按钮、日志文本框）。

## 3. 主流程（AutoSocialAssistantService）
`AutoSocialAssistantService` 是调度中枢：
1. `Start()`：确保 MuMu 运行 + 打开 ChatGPT 页面，再开启随机调度。
2. 调度器到点后触发 `OnTick()`：
   - `ExecuteCurrentStateAsync()` 截图并调用 Gemini。
   - 将 AI 返回文本解析为 JSON 命令。
   - 执行鼠标点击或键盘输入。
3. 任务执行后切换状态机：
   - `Home` → `ProfileAnalysis` → `Chat` → `AntiDetection` → `Home`。

提示词由 `BuildPrompt()` 根据状态拼接，约束 AI 只返回 JSON：
`{"action":"click|type|noop","x":0.5,"y":0.5,"text":"可选文本"}`。

## 4. 截图与窗口识别
`WindowCaptureService` 负责识别目标窗口并截图：
- 先按进程别名匹配（`MuMuPlayer` / `NemuPlayer` / `HD-Player` 等）。
- 找不到则按窗口标题关键词匹配（`MuMu`、`模拟器`、`Nemu`）。
- 截图优先使用 `PrintWindow`，失败后回退 `CopyFromScreen`。
- 最终将位图转为 Base64 PNG，供 Gemini API 使用。

## 5. AI 调用
`GeminiClient`：
- 调用 `v1beta/models/{model}:generateContent`。
- 请求体包含 `system_instruction` + `inline_data(image/png)`。
- 若 `GEMINI_API_KEY` 为空会直接抛异常。
- 从 `candidates[0].content.parts[0].text` 读取模型输出文本。

## 6. 输入与坐标转换
- `CoordinateMapper`：把相对坐标（0~1）映射到窗口绝对坐标，并加入随机抖动（默认 ±10px）。
- `InputSimulator`：通过 Win32 API 执行鼠标左键点击与逐字符键盘输入（`VkKeyScan` + `SendInput`）。

## 7. 调度与外部应用拉起
- `RandomDelayScheduler`：WinForms `Timer`，随机区间 `60~300s`。
- `AppLauncher`：
  - `EnsureMuMuRunning`：检测 MuMu/Nemu 进程，不在运行则尝试从默认路径或 `MUMU_EXE_PATH` 启动。
  - `OpenChatGpt`：用默认浏览器打开 `https://chatgpt.com/`。

## 8. 关键环境变量
- `GEMINI_API_KEY`：Gemini API 访问密钥（必需）。
- `MUMU_EXE_PATH`：MuMu 可执行文件路径（可选，默认路径找不到时建议配置，例如 `D:\\Program Files\\Netease\\MuMuPlayer\\nx_main\\MuMuNxMain.exe`）。

## 9. 当前代码可改进点（建议）
1. **容错**：Gemini 返回非 JSON 时目前直接 `noop`，可增加重试/回退策略。
2. **安全**：日志中会输出 AI 回复与输入文本，可考虑脱敏。
3. **可测试性**：把状态机、命令解析、提示词构建拆分成可单测的纯函数模块。
4. **可观测性**：增加最近截图落盘与每轮 trace-id，便于排查误点击。
5. **UI 细节**：`startButton` 文本为“启动助手1”，可能是临时文案。
