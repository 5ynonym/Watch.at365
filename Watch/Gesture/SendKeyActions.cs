using at365.Native365;

namespace at365.Gesture365
{
    public static class SendKeyActions
    {
        public static readonly Action Alt_F4 = Create(NativeMethods.VK_MENU, NativeMethods.VK_F4);
        public static readonly Action Ctrl_N = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_N);
        public static readonly Action F11 = Create(NativeMethods.VK_NONE, NativeMethods.VK_F11);
        public static readonly Action OpenExplorer = Create(NativeMethods.VK_LWIN, NativeMethods.VK_E);

        public static class WindowSnap
        {
            public static readonly Action Left = Create(NativeMethods.VK_LWIN, NativeMethods.VK_LEFT);
            public static readonly Action Right = Create(NativeMethods.VK_LWIN, NativeMethods.VK_RIGHT);
        }

        public static class WebBrowser
        {
            public static readonly Action PrevTab = Create([NativeMethods.VK_CONTROL, NativeMethods.VK_SHIFT], NativeMethods.VK_TAB);
            public static readonly Action NextTab = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_TAB);
            public static readonly Action NewTab = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_T);
            public static readonly Action CloseTab = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_F4);
            public static readonly Action RestoreTab = Create([NativeMethods.VK_CONTROL, NativeMethods.VK_SHIFT], NativeMethods.VK_T);

            public static readonly Action Reload = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_F5);
            public static readonly Action Back = Create(NativeMethods.VK_MENU, NativeMethods.VK_LEFT);
            public static readonly Action Forward = Create(NativeMethods.VK_MENU, NativeMethods.VK_RIGHT);
        }

        public static class Explorer
        {
            public static readonly Action PrevTab = Create([NativeMethods.VK_CONTROL, NativeMethods.VK_SHIFT], NativeMethods.VK_TAB);
            public static readonly Action NextTab = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_TAB);
            public static readonly Action NewTab = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_T);
            public static readonly Action CloseTab = Create(NativeMethods.VK_CONTROL, NativeMethods.VK_F4);

            public static readonly Action Back = Create(NativeMethods.VK_MENU, NativeMethods.VK_LEFT);
            public static readonly Action Forward = Create(NativeMethods.VK_MENU, NativeMethods.VK_RIGHT);
            public static readonly Action Up = Create(NativeMethods.VK_MENU, NativeMethods.VK_UP);
        }

        private static Action Create(uint modifierKeyCode, uint keyCode)
        {
            return () => InputSimulator.Instance.Keyboard.ModifiedKeyStroke(modifierKeyCode, keyCode);
        }

        private static Action Create(IEnumerable<uint> modifierKeyCodes, uint keyCode)
        {
            return () => InputSimulator.Instance.Keyboard.ModifiedKeyStroke(modifierKeyCodes, keyCode);
        }
    }
}
