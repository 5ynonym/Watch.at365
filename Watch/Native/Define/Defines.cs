namespace at365.Native365
{
    public static partial class NativeMethods
    {
        public const nint LRESULTCancel = 1;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const uint WS_EX_TRANSPARENT = 0x00000020;
        public const uint WS_EX_NOACTIVATE = 0x08000000;
        public const uint WS_EX_TOPMOST = 0x00000008;

        public const nint HWND_TOPMOST = -1;
        public const nint HWND_TOP = 0;
        public const nint HWND_BROADCAST = 0xffff;

        public const uint WM_DISPLAYCHANGE = 0x007E;
        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint WM_HOTKEY = 0x0312;
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_RBUTTONUP = 0x0205;
        public const uint WM_MBUTTONDOWN = 0x0207;
        public const uint WM_MBUTTONUP = 0x0208;
        public const uint WM_MOUSE_WHEEL = 0x20A;

        public const nint SC_MINIMIZE = 0xF020;
        public const nint SC_MAXIMIZE = 0xF030;
        public const nint SC_CLOSE = 0xF06060;

        public const nint SC_MONITORPOWER = 0xF170;
        public const nint SC_MONITORPOWER_OFF = 2;
        public const nint SC_MONITORPOWER_ON = -1;

        public const int HC_ACTION = 0;
        public const int WH_MOUSE_LL = 14;

        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // Virtual Key Codes for SendInput
        public const uint VK_NONE = 0x00;
        public const uint VK_LBUTTON = 0x01;
        public const uint VK_RBUTTON = 0x02;
        public const uint VK_MBUTTON = 0x04;
        public const uint VK_SHIFT = 0x10;
        public const uint VK_CONTROL = 0x11;
        public const uint VK_MENU = 0x12;    // Alt
        public const uint VK_TAB = 0x09;
        public const uint VK_LEFT = 0x25;
        public const uint VK_UP = 0x26;
        public const uint VK_RIGHT = 0x27;
        public const uint VK_DOWN = 0x28;
        public const uint VK_F4 = 0x73;
        public const uint VK_F5 = 0x74;
        public const uint VK_F11 = 0x7A;
        public const uint VK_E = 0x45;
        public const uint VK_N = 0x4E;
        public const uint VK_T = 0x54;
        public const uint VK_LWIN = 0x5B;
        public const uint VK_RWIN = 0x5C;

        public const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
    }
}
