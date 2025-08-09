using System.Runtime.InteropServices;
using System.Text;

namespace at365.Native365
{
    public static partial class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool IsZoomed(nint hWnd);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern nint GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern nint FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(nint hWnd, int index);

        [DllImport("user32.dll")]
        public static extern uint SetWindowLong(nint hWnd, int index, uint newLong);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern nint SendMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(nint hwnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        public static extern long DwmGetWindowAttribute(nint hWnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT rect, int cbAttribute);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out int ProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [PreserveSig]
        public static extern uint GetModuleFileName([In] nint hModule, [Out] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern nint GetModuleHandle(string name);

        [DllImport("user32.dll")]
        public static extern nint WindowFromPoint(POINT point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(nint hook);

        [DllImport("user32.dll")]
        public static extern nint SetWindowsHookEx(nint hHook, MouseHookCallback lpfn, nint hInstance, int threadId);

        [DllImport("user32.dll")]
        public static extern nint CallNextHookEx(nint hHook, int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam);

        public delegate nint MouseHookCallback(int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(nint dpiContext);
    }
}
