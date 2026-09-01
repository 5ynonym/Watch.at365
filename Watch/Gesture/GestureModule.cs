using System.Windows.Input;
using at365.Common365;
using at365.Native365;

namespace at365.Gesture365
{
    public sealed class GestureModule : ModuleBase<GestureModule>
    {
        public static void Start() { var _ = Instance; }

        protected override void InitializeCore()
        {
            InitializeKeyGesture();
            InitializeButtonGesture();
            InitializeMoveGesture();
            MouseGestureProvider.Instance.Initialize();
        }

        protected override void DisposeCore(bool disposing)
        {
            SafeDispose(MouseGestureProvider.Instance);
        }

        private void InitializeKeyGesture()
        {
            var NONE = ModifierKeys.None;
            var ALT_WIN = ModifierKeys.Alt | ModifierKeys.Windows;
            var ALL_MOD = ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt;

            var whenver = HotKeyManager.When();
            whenver(ALL_MOD, Key.M, NativeHelper.MoveCursor, null);

            whenver(NONE, Key.F13, SendKeyActions.OpenExplorer, NativeHelper.OpenCurrentProcessFolder); // F1
            //whenver(NONE, Key.F14, MouseGestureProvider.Actions.ToggleProcessBlackList, null);
            //whenver(NONE , Key.F14, NativeHelper.CopyPointedWindowProcessName, null);

            // キー割り当てだとRDP側にフォーカスがある場合にRDP元で動かないのでマウスマクロで実行する必要がある
            //whenver(NONE, Key.F15, NativeHelper.SwitchRDPMinimize, null); // F3
            whenver(NONE, Key.F16, NativeHelper.SwitchRDPToConsole, null); // F4

            // XButton1 (マウス側で最小化を割り当てるとリモートデスクトップを貫通しない)
            whenver(NONE, Key.F17, NativeHelper.MinimizeWindow, null); // F5
            // XButton2 マウス側でAlt+F4を割り当てる (Alt+F4はリモートデスクトップを貫通する)
            whenver(NONE, Key.F18, SendKeyActions.Alt_F4, null); // F6

            whenver(NONE, Key.F20, SendKeyActions.OpenExplorer, NativeHelper.AdjustFullHeight); // F8

            // F21は変なWindows機能に割り当てられるので使用不可
            // F22はゲーミングコパイロットに割り当てられるので使用不可
            whenver(ALT_WIN, Key.F10, NativeHelper.AdjustFullHeight, null);

            // 左チルト
            whenver(ALT_WIN, Key.F11, NativeHelper.MoveWindowToLeft, SendKeyActions.WindowSnap.Left);
            whenver(NONE, Key.F23, NativeHelper.MoveWindowToLeft, SendKeyActions.WindowSnap.Left); // F11

            // 右チルト
            whenver(ALT_WIN, Key.F12, NativeHelper.MoveWindowToRight, SendKeyActions.WindowSnap.Right);
            whenver(NONE, Key.F24, NativeHelper.MoveWindowToRight, SendKeyActions.WindowSnap.Right); // F12
        }

        private void InitializeButtonGesture()
        {
            var whenever = MouseGestureManager.WhenMouse();
            whenever(MouseTrigger.LeftButtonDown, SendKeyActions.WebBrowser.CloseTab);
            whenever(MouseTrigger.RightButtonDown, SendKeyActions.Ctrl_N);
            whenever(MouseTrigger.MiddleButtonDown, SendKeyActions.Ctrl_N);
            whenever(MouseTrigger.WheelDown, SendKeyActions.WebBrowser.NextTab);
            whenever(MouseTrigger.WheelUp, SendKeyActions.WebBrowser.PrevTab);

            var explorer = MouseGestureManager.WhenMouse("explorer.exe");
            explorer(MouseTrigger.RightButtonDown, SendKeyActions.Explorer.NewTab);
            explorer(MouseTrigger.MiddleButtonDown, SendKeyActions.Explorer.NewTab);

            var browser = MouseGestureManager.WhenMouse("msedge.exe", "chrome.exe");
            browser(MouseTrigger.RightButtonDown, SendKeyActions.WebBrowser.NewTab);
            browser(MouseTrigger.MiddleButtonDown, SendKeyActions.WebBrowser.NewTab);
        }

        private void InitializeMoveGesture()
        {
            var browser = MouseGestureManager.WhenMove("msedge.exe", "chrome.exe");
            //browser("新しいタブ", [MoveTrigger.MoveDown, MoveTrigger.MoveRight], SendKeyActions.WebBrowser.NewTab);
            //browser("タブを閉じる", [MoveTrigger.MoveDown, MoveTrigger.MoveLeft], SendKeyActions.WebBrowser.CloseTab);
            browser("タブを復元", [MoveTrigger.MoveUp], SendKeyActions.WebBrowser.RestoreTab);
            browser("全画面", [MoveTrigger.MoveUp, MoveTrigger.MoveDown], SendKeyActions.F11);
            browser("リロード", [MoveTrigger.MoveDown], SendKeyActions.WebBrowser.Reload);
            browser("進む", [MoveTrigger.MoveRight], SendKeyActions.WebBrowser.Forward);
            browser("戻る", [MoveTrigger.MoveLeft], SendKeyActions.WebBrowser.Back);
        }
    }
}
