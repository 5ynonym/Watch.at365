namespace at365.Native365
{
    public static partial class NativeMethods
    {
        public const int SW_HIDE = 0; //ウィンドウを非表示にし、他のウィンドウをアクティブにします。
        public const int SW_MAXIMIZE = 3; // ウィンドウを最大化します。
        public const int SW_MINIMIZE = 6; // ウィンドウを最小化し、Z 順位が次のトップレベルウィンドウをアクティブにします。
        public const int SW_RESTORE = 9; // ウィンドウをアクティブにし、表示します。ウィンドウが最小化されていたり最大化されていたりすると、元の位置とサイズに戻ります。
        public const int SW_SHOW = 5; //ウィンドウをアクティブにして、現在の位置とサイズで表示します。
        public const int SW_SHOWDEFAULT = 10; // アプリケーションを起動させたプログラムが CreateProcess 関数に渡すSTARTUPINFO 構造体の wShowWindow メンバで指定された SW_ フラグを基にして、表示状態を設定します。
        public const int SW_SHOWMAXIMIZED = 3; // ウィンドウをアクティブにして、最大化します。
        public const int SW_SHOWMINIMIZED = 2; // ウィンドウをアクティブにして、最小化します。
        public const int SW_SHOWMINNOACTIVE = 7; // ウィンドウを最小化します。アクティブなウィンドウは、アクティブな状態を維持します。非アクティブなウィンドウは、非アクティブなままです。
        public const int SW_SHOWNA = 8; // ウィンドウを現在の状態で表示します。アクティブなウィンドウはアクティブな状態を維持します。
        public const int SW_SHOWNOACTIVATE = 4; // ウィンドウを直前の位置とサイズで表示します。アクティブなウィンドウはアクティブな状態を維持します。
        public const int SW_SHOWNORMAL = 1; // ウィンドウをアクティブにして、表示します。ウィンドウが最小化または最大化されているときは、位置とサイズを元に戻します。
    }
}
