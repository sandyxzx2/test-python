using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace changeWeChat
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static readonly string[] TargetWindowTitles =
        {
            "MuMu Android Device",
            "Android Device"
        };

        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x8;
        private const int LWA_ALPHA = 0x2;

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_W = 0x57;

        private const uint SWP_NOACTIVATE = 0x0010;
        private const byte OverlayAlpha = 204; // 80%
        private const byte OpaqueAlpha = 255;

        private readonly System.Windows.Forms.Timer timer = new();
        private IntPtr hTargetWindow = IntPtr.Zero;
        private bool isTransparent = true;

        public Form1()
        {
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;

            timer.Interval = 500;
            timer.Tick += (_, _) => SyncWithTargetWindow();
            timer.Start();

            FindTargetWindow();

            RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_W);

            if (hTargetWindow == IntPtr.Zero)
            {
                MessageBox.Show(
                    $"Target window was not found.\nTried titles: {string.Join(" / ", TargetWindowTitles)}\nPlease launch MuMu first.\n\nThe app will keep searching automatically.\nPress Ctrl+Shift+W to toggle transparent mode.",
                    "Notice",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Rectangle? area = Screen.PrimaryScreen?.WorkingArea;
                if (area.HasValue)
                {
                    SetBounds((area.Value.Width - 800) / 2, (area.Value.Height - 450) / 2, 800, 450);
                }
            }

            SetTransparentMode(isTransparent);
        }

        private void FindTargetWindow()
        {
            hTargetWindow = IntPtr.Zero;

            foreach (string title in TargetWindowTitles)
            {
                hTargetWindow = FindWindow(null, title);
                if (IsValidTargetWindow(hTargetWindow))
                {
                    return;
                }
            }

            hTargetWindow = FindWindowByTitleContains(TargetWindowTitles);
            if (IsValidTargetWindow(hTargetWindow))
            {
                return;
            }

            hTargetWindow = FindTargetByProcess();
            if (!IsValidTargetWindow(hTargetWindow))
            {
                hTargetWindow = IntPtr.Zero;
            }
        }

        private static bool IsValidTargetWindow(IntPtr hWnd)
        {
            return hWnd != IntPtr.Zero && IsWindow(hWnd);
        }

        private static IntPtr FindWindowByTitleContains(string[] keywords)
        {
            IntPtr found = IntPtr.Zero;

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                StringBuilder titleBuilder = new(512);
                int titleLength = GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                if (titleLength <= 0)
                {
                    return true;
                }

                string title = titleBuilder.ToString();
                bool match = false;
                foreach (string keyword in keywords)
                {
                    if (title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                {
                    found = hWnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return found;
        }

        private static IntPtr FindTargetByProcess()
        {
            string[] processNames =
            {
                "MuMuNxDevice",
                "MuMuNxMain",
                "MuMuPlayer",
                "NemuPlayer",
                "NemuLauncher",
                "MuMuVMMHeadless",
                "MuMuVMMSVC"
            };

            foreach (string processName in processNames)
            {
                Process[] processes;
                try
                {
                    processes = Process.GetProcessesByName(processName);
                }
                catch
                {
                    continue;
                }

                foreach (Process process in processes)
                {
                    try
                    {
                        IntPtr hwnd = process.MainWindowHandle;
                        if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd))
                        {
                            continue;
                        }

                        if (!GetWindowRect(hwnd, out RECT rect))
                        {
                            continue;
                        }

                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        if (width > 300 && height > 200)
                        {
                            return hwnd;
                        }
                    }
                    catch
                    {
                        // Ignore inaccessible process windows.
                    }
                }
            }

            return IntPtr.Zero;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                if (isTransparent)
                {
                    cp.ExStyle |= WS_EX_TRANSPARENT;
                }

                return cp;
            }
        }

        private void SetTransparentMode(bool transparent)
        {
            isTransparent = transparent;

            RecreateHandle();
            SetLayeredWindowAttributes(Handle, 0, transparent ? OverlayAlpha : OpaqueAlpha, LWA_ALPHA);
            Invalidate();
        }

        private void SyncWithTargetWindow()
        {
            if (hTargetWindow == IntPtr.Zero || !IsWindow(hTargetWindow))
            {
                FindTargetWindow();
            }

            if (hTargetWindow != IntPtr.Zero && IsWindow(hTargetWindow) && IsWindowVisible(hTargetWindow))
            {
                Show();

                if (!GetWindowRect(hTargetWindow, out RECT rect))
                {
                    return;
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0)
                {
                    return;
                }

                SetWindowPos(Handle, new IntPtr(-1), rect.Left, rect.Top, width, height, SWP_NOACTIVATE);

                if (Width != width || Height != height)
                {
                    Invalidate();
                }
            }
            else
            {
                Hide();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(245, 245, 245));
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                SetTransparentMode(!isTransparent);
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer.Stop();
            UnregisterHotKey(Handle, HOTKEY_ID);
            base.OnFormClosed(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (!isTransparent && e.Button == MouseButtons.Right)
            {
                ContextMenuStrip menu = new();
                menu.Items.Add("Find MuMu Window Again", null, (_, _) => FindTargetWindow());
                menu.Items.Add($"{(isTransparent ? "Disable" : "Enable")} Transparent Mode", null, (_, _) => SetTransparentMode(!isTransparent));
                menu.Items.Add("Exit", null, (_, _) => Close());
                menu.Show(this, e.Location);
            }

            base.OnMouseClick(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BringToFront();
            Focus();
        }
    }
}
