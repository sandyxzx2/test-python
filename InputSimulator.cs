using System.Runtime.InteropServices;

namespace testSoulChat;

public static class InputSimulator
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint MouseeventfLeftdown = 0x0002;
    private const uint MouseeventfLeftup = 0x0004;
    private const uint MouseeventfWheel = 0x0800;
    private const uint KeyeventfKeyup = 0x0002;

    public static void LeftClick(int x, int y)
    {
        SetCursorPos(x, y);
        var inputs = new[]
        {
            new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT { dwFlags = MouseeventfLeftdown }
                }
            },
            new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT { dwFlags = MouseeventfLeftup }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void ScrollVertical(int delta)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MouseeventfWheel,
                        mouseData = unchecked((uint)delta)
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void PressEnter() => PressKey(0x0D);

    public static void PasteClipboard() => PressHotkey(0x11, 0x56);

    public static void PressHotkey(params ushort[] keys)
    {
        if (keys.Length == 0)
        {
            return;
        }

        var inputs = new List<INPUT>(keys.Length * 2);
        foreach (var key in keys)
        {
            inputs.Add(KeyDown(key));
        }

        for (var i = keys.Length - 1; i >= 0; i--)
        {
            inputs.Add(KeyUp(keys[i]));
        }

        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    public static void TypeText(string text)
    {
        foreach (var ch in text)
        {
            var key = VkKeyScan(ch);
            if (key == -1)
            {
                continue;
            }

            var vk = (ushort)(key & 0xFF);
            var shiftState = (key >> 8) & 0xFF;

            var inputs = new List<INPUT>();
            if ((shiftState & 1) != 0)
            {
                inputs.Add(KeyDown(0x10));
            }

            inputs.Add(KeyDown(vk));
            inputs.Add(KeyUp(vk));

            if ((shiftState & 1) != 0)
            {
                inputs.Add(KeyUp(0x10));
            }

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        }
    }

    private static void PressKey(ushort vk)
    {
        var inputs = new[]
        {
            KeyDown(vk),
            KeyUp(vk)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyDown(ushort vk) => new()
    {
        type = InputKeyboard,
        U = new InputUnion
        {
            ki = new KEYBDINPUT { wVk = vk, dwFlags = 0 }
        }
    };

    private static INPUT KeyUp(ushort vk) => new()
    {
        type = InputKeyboard,
        U = new InputUnion
        {
            ki = new KEYBDINPUT { wVk = vk, dwFlags = KeyeventfKeyup }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }
}
