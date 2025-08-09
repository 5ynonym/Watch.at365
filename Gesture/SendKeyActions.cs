using WindowsInput;

namespace at365.Gesture365
{
    public static class SendKeyActions
    {
        public static readonly Action Alt_F4 = Create(VirtualKeyCode.MENU, VirtualKeyCode.F4);
        public static readonly Action Ctrl_N = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_N);
        public static readonly Action F11 = Create(VirtualKeyCode.None, VirtualKeyCode.F11);
        public static readonly Action OpenExplorer = Create(VirtualKeyCode.LWIN, VirtualKeyCode.VK_E);

        public static class WindowSnap
        {
            public static readonly Action Left = Create(VirtualKeyCode.LWIN, VirtualKeyCode.LEFT);
            public static readonly Action Right = Create(VirtualKeyCode.LWIN, VirtualKeyCode.RIGHT);
        }

        public static class WebBrowser
        {
            public static readonly Action PrevTab = Create([VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT], VirtualKeyCode.TAB);
            public static readonly Action NextTab = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.TAB);
            public static readonly Action NewTab = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_T);
            public static readonly Action CloseTab = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.F4);
            public static readonly Action RestoreTab = Create([VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT], VirtualKeyCode.VK_T);

            public static readonly Action Reload = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.F5);
            public static readonly Action Back = Create(VirtualKeyCode.MENU, VirtualKeyCode.LEFT);
            public static readonly Action Forward = Create(VirtualKeyCode.MENU, VirtualKeyCode.RIGHT);
        }

        public static class Explorer
        {
            public static readonly Action PrevTab = Create([VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT], VirtualKeyCode.TAB);
            public static readonly Action NextTab = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.TAB);
            public static readonly Action NewTab = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_T);
            public static readonly Action CloseTab = Create(VirtualKeyCode.CONTROL, VirtualKeyCode.F4);

            public static readonly Action Back = Create(VirtualKeyCode.MENU, VirtualKeyCode.LEFT);
            public static readonly Action Forward = Create(VirtualKeyCode.MENU, VirtualKeyCode.RIGHT);
            public static readonly Action Up = Create(VirtualKeyCode.MENU, VirtualKeyCode.UP);
        }

        private static Action Create(VirtualKeyCode modifierKeyCode, VirtualKeyCode keyCode)
        {
            return () => InputSimulator.Instance.Keyboard.ModifiedKeyStroke(modifierKeyCode, keyCode);
        }

        private static Action Create(IEnumerable<VirtualKeyCode> modifierKeyCodes, VirtualKeyCode keyCode)
        {
            return () => InputSimulator.Instance.Keyboard.ModifiedKeyStroke(modifierKeyCodes, keyCode);
        }
    }
}
