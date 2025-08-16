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
            GestureProvider.Instance.Initialize();
        }

        protected override void DisposeCore(bool disposing)
        {
            SafeDispose(GestureProvider.Instance);
        }

        private void InitializeKeyGesture()
        {
            var whenver = HotKeyManager.When();
            //whenver(ModifierKeys.None, Key.F13, GestureProvider.Actions.ToggleProcessBlackList, null);
            whenver(ModifierKeys.None, Key.F13, NativeHelper.OpenCurrentProcessFolder, null);
            whenver(ModifierKeys.None, Key.F14, NativeHelper.CopyPointedWindowProcessName, null);

            // XButton1 F17を割り当てる。(マウス側で最小化を割り当てるとリモートデスクトップを貫通しない)
            whenver(ModifierKeys.None, Key.F17, NativeHelper.MinimizeWindow, null);
            // XButton2 F18は使わずにマウス側でAlt+F4を割り当てる (Alt+F4はリモートデスクトップを貫通する)
            whenver(ModifierKeys.None, Key.F18, SendKeyActions.Alt_F4, null);
            // XButton3
            whenver(ModifierKeys.None, Key.F19, NativeHelper.MoveCursor, null);
            // ホイールクリック
            whenver(ModifierKeys.None, Key.F20, SendKeyActions.OpenExplorer, NativeHelper.AdjustFullHeight);
            // 左チルト
            whenver(ModifierKeys.None, Key.F21, NativeHelper.MoveWindowToLeft, SendKeyActions.WindowSnap.Left);
            // 右チルト
            whenver(ModifierKeys.None, Key.F22, NativeHelper.MoveWindowToRight, SendKeyActions.WindowSnap.Right);

            whenver(ModifierKeys.None, Key.F23, SendKeyActions.WindowSnap.Left, null);
            whenver(ModifierKeys.None, Key.F24, SendKeyActions.WindowSnap.Right, null);
        }

        private void InitializeButtonGesture()
        {
            //var vs = When("devenv.exe");
            //var vscode = When("code.exe");

            var whenever = GestureManager.WhenMouse();
            whenever(MouseTrigger.LeftButtonDown, SendKeyActions.WebBrowser.CloseTab);
            whenever(MouseTrigger.RightButtonDown, SendKeyActions.Ctrl_N);
            whenever(MouseTrigger.MiddleButtonDown, SendKeyActions.Ctrl_N);
            whenever(MouseTrigger.WheelDown, SendKeyActions.WebBrowser.NextTab);
            whenever(MouseTrigger.WheelUp, SendKeyActions.WebBrowser.PrevTab);

            var explorer = GestureManager.WhenMouse("explorer.exe");
            explorer(MouseTrigger.RightButtonDown, SendKeyActions.Explorer.NewTab);
            explorer(MouseTrigger.MiddleButtonDown, SendKeyActions.Explorer.NewTab);

            var browser = GestureManager.WhenMouse("msedge.exe", "chrome.exe");
            browser(MouseTrigger.RightButtonDown, SendKeyActions.WebBrowser.NewTab);
            browser(MouseTrigger.MiddleButtonDown, SendKeyActions.WebBrowser.NewTab);
        }

        private void InitializeMoveGesture()
        {
            var explorer = GestureManager.WhenMove("explorer.exe");
            //explorer("新しいタブ", [MoveTrigger.MoveDown, MoveTrigger.MoveRight], SendKeyActions.Explorer.NewTab);
            //explorer("タブを閉じる", [MoveTrigger.MoveDown, MoveTrigger.MoveLeft], SendKeyActions.Explorer.CloseTab);
            explorer("進む", [MoveTrigger.MoveRight], SendKeyActions.Explorer.Forward);
            explorer("戻る", [MoveTrigger.MoveLeft], SendKeyActions.Explorer.Back);
            explorer("上へ", [MoveTrigger.MoveUp], SendKeyActions.Explorer.Up);

            var browser = GestureManager.WhenMove("msedge.exe", "chrome.exe");
            browser("新しいタブ", [MoveTrigger.MoveDown, MoveTrigger.MoveRight], SendKeyActions.WebBrowser.NewTab);
            browser("タブを閉じる", [MoveTrigger.MoveDown, MoveTrigger.MoveLeft], SendKeyActions.WebBrowser.CloseTab);
            browser("タブを復元", [MoveTrigger.MoveUp], SendKeyActions.WebBrowser.RestoreTab);
            browser("全画面", [MoveTrigger.MoveUp, MoveTrigger.MoveDown], SendKeyActions.F11);
            browser("タブを復元", [MoveTrigger.MoveLeft, MoveTrigger.MoveDown, MoveTrigger.MoveRight], SendKeyActions.WebBrowser.RestoreTab);
            browser("リロード", [MoveTrigger.MoveDown], SendKeyActions.WebBrowser.Reload);
            browser("進む", [MoveTrigger.MoveRight], SendKeyActions.WebBrowser.Forward);
            browser("戻る", [MoveTrigger.MoveLeft], SendKeyActions.WebBrowser.Back);
        }
    }
}
