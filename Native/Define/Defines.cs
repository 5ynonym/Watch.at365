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

        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;
        public const int VK_MBUTTON = 0x04;

        public const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
    }
}
