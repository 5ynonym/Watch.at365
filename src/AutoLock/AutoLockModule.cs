using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using at365.Common365;
using at365.Native365;
using UserSettings = at365.Watch.Properties.Settings;
using static at365.Native365.NativeMethods;

namespace at365.AutoLock365
{
    /// <summary>
    /// マウスが一定時間操作されなかった場合に Windows をロックする。
    /// 計測対象はマウス入力のみ（キーボードは含まない）。
    /// </summary>
    public sealed class AutoLockModule : ModuleBase<AutoLockModule>
    {
        /// <summary>マウスが操作されないまま経過した場合にロックを発動する閾値。</summary>
        public static readonly TimeSpan IdleThreshold = TimeSpan.FromHours(6);

        /// <summary>アイドル時間のチェック間隔。閾値より十分細かくする。</summary>
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public static void Start() { var _ = Instance; }

        private readonly MouseHookCallback _callback;
        private nint _hHook;
        private System.Threading.Timer? _timer;
        private long _lastMouseInputTicks;
        private bool _enabled;

        public AutoLockModule()
        {
            _callback = (int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam) =>
            {
                if (nCode >= 0)
                {
                    // どのマウスイベント（移動・クリック・ホイール）でも操作とみなす
                    Volatile.Write(ref _lastMouseInputTicks, DateTime.UtcNow.Ticks);
                }
                return CallNextHookEx(_hHook, nCode, wParam, lParam);
            };
        }

        /// <summary>
        /// 自動ロック機能の有効・無効を切り替える。
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                UserSettings.Default.AutoLockEnabled = value;
                UserSettings.Default.Save();
                ResetLastInput();
            }
        }

        protected override void InitializeCore()
        {
            _enabled = UserSettings.Default.AutoLockEnabled;
            ResetLastInput();
            SetHook();
            _timer = new System.Threading.Timer(OnTick, null, CheckInterval, CheckInterval);
        }

        protected override void DisposeCore(bool disposing)
        {
            try { _timer?.Dispose(); } catch { }
            _timer = null;
            try { Unhook(); } catch { }
        }

        private void SetHook()
        {
            using var process = Process.GetCurrentProcess();
            nint hInstance = GetModuleHandle(process.MainModule!.ModuleName);
            _hHook = SetWindowsHookEx(WH_MOUSE_LL, _callback, hInstance, 0);
        }

        private void Unhook()
        {
            if (_hHook != nint.Zero)
            {
                UnhookWindowsHookEx(_hHook);
                _hHook = nint.Zero;
            }
        }

        private void ResetLastInput()
        {
            Volatile.Write(ref _lastMouseInputTicks, DateTime.UtcNow.Ticks);
        }

        private void OnTick(object? state)
        {
            try
            {
                if (!_enabled) return;

                var lastTicks = Volatile.Read(ref _lastMouseInputTicks);
                var elapsed = DateTime.UtcNow - new DateTime(lastTicks, DateTimeKind.Utc);
                if (elapsed < IdleThreshold) return;

                // ロック後はカウンタをリセットして連続ロックを避ける
                ResetLastInput();
                NativeHelper.LockWorkstation();
            }
            catch
            {
            }
        }
    }
}
