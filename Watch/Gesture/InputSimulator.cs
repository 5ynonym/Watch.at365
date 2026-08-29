using System.Runtime.InteropServices;
using at365.Native365;

namespace at365.Gesture365
{
    public class InputSimulator
    {
        public static readonly InputSimulator Instance = new();

        private readonly KeyboardSimulator _keyboard;
        private readonly MouseSimulator _mouse;

        public InputSimulator()
        {
            _keyboard = new KeyboardSimulator();
            _mouse = new MouseSimulator();
        }

        public KeyboardSimulator Keyboard => _keyboard;
        public MouseSimulator Mouse => _mouse;

        public static void LeftButtonClick() => Instance.Mouse.LeftButtonClick();
        public static void RightButtonClick() => Instance.Mouse.RightButtonClick();
        public static void MiddleButtonClick() => Instance.Mouse.MiddleButtonClick();
    }

    public class MouseSimulator
    {
        public void LeftButtonClick()
        {
            LeftButtonDown();
            LeftButtonUp();
        }

        public void RightButtonClick()
        {
            RightButtonDown();
            RightButtonUp();
        }

        public void MiddleButtonClick()
        {
            MiddleButtonDown();
            MiddleButtonUp();
        }

        private static void LeftButtonDown()
        {
            SendMouseInput(NativeMethods.MOUSEEVENTF_LEFTDOWN);
        }

        private static void LeftButtonUp()
        {
            SendMouseInput(NativeMethods.MOUSEEVENTF_LEFTUP);
        }

        private static void RightButtonDown()
        {
            SendMouseInput(NativeMethods.MOUSEEVENTF_RIGHTDOWN);
        }

        private static void RightButtonUp()
        {
            SendMouseInput(NativeMethods.MOUSEEVENTF_RIGHTUP);
        }

        private static void MiddleButtonDown()
        {
            SendMouseInput(NativeMethods.MOUSEEVENTF_MIDDLEDOWN);
        }

        private static void MiddleButtonUp()
        {
            SendMouseInput(NativeMethods.MOUSEEVENTF_MIDDLEUP);
        }

        private static void SendMouseInput(uint flags)
        {
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = nint.Zero
                    }
                }
            };

            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }

    public class KeyboardSimulator
    {
        public void ModifiedKeyStroke(uint modifierKeyCode, uint keyCode)
        {
            if (modifierKeyCode == 0)  // VK_NONE
            {
                PressKey(keyCode);
                ReleaseKey(keyCode);
            }
            else
            {
                PressKey(modifierKeyCode);
                PressKey(keyCode);
                ReleaseKey(keyCode);
                ReleaseKey(modifierKeyCode);
            }
        }

        public void ModifiedKeyStroke(IEnumerable<uint> modifierKeyCodes, uint keyCode)
        {
            foreach (var modifierKeyCode in modifierKeyCodes)
            {
                PressKey(modifierKeyCode);
            }

            PressKey(keyCode);
            ReleaseKey(keyCode);

            foreach (var modifierKeyCode in modifierKeyCodes.Reverse())
            {
                ReleaseKey(modifierKeyCode);
            }
        }

        private static void PressKey(uint keyCode)
        {
            SendKeyInput(keyCode, 0);
        }

        private static void ReleaseKey(uint keyCode)
        {
            SendKeyInput(keyCode, NativeMethods.KEYEVENTF_KEYUP);
        }

        private static void SendKeyInput(uint keyCode, uint flags)
        {
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.INPUTUNION
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = (ushort)keyCode,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = nint.Zero
                    }
                }
            };

            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}
