namespace testSoulChat
{
    public partial class Form1 : Form
    {
        private readonly AutoSocialAssistantService _assistantService;

        public Form1()
        {
            InitializeComponent();

            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
            var geminiClient = new GeminiClient(new HttpClient(), apiKey);
            _assistantService = new AutoSocialAssistantService(new WindowCaptureService(), geminiClient, AppendLog);
            AppendLog("程序已启动，点击“启动助手”执行自动社交流程。");
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            _assistantService.Start();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            _assistantService.Stop();
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLog(message));
                return;
            }

            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _assistantService.Dispose();
            base.OnFormClosed(e);
        }
    }
}
