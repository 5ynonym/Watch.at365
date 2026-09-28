using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using at365.Common365;
using at365.Native365;
using static at365.Native365.NativeMethods;

namespace at365.AutoLock365;

/// <summary>マウス入力のみを対象に、6 時間の無操作でロックする。</summary>
public sealed class AutoLockModule : ModuleBase<AutoLockModule>
{
    public static readonly TimeSpan IdleThreshold = TimeSpan.FromHours(6);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    public static void Start() { _ = Instance; }

    private readonly MouseHookCallback _callback;
    private readonly DispatcherTimer _timer;
    private nint _hHook;
    private long _lastMouseInputMilliseconds;
    private bool _enabled;

    public AutoLockModule()
    {
        _timer = new DispatcherTimer { Interval = CheckInterval };
        _timer.Tick += OnTick;
        _callback = (int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam) =>
        {
            if (nCode >= 0) ResetLastInput();
            return CallNextHookEx(_hHook, nCode, wParam, lParam);
        };
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _timer.Dispatcher.VerifyAccess();
            if (_enabled == value) return;
            try
            {
                SetMonitoring(value);
                ApplicationSettings.Current.AutoLockEnabled = value;
                ApplicationSettings.Save();
            }
            catch (Exception error) { Diagnostics.Report("Set auto lock", error); }
        }
    }

    protected override void InitializeCore() =>
        SetMonitoring(ApplicationSettings.Current.AutoLockEnabled);

    private void SetMonitoring(bool enabled)
    {
        if (enabled && _hHook == nint.Zero)
        {
            _hHook = SetWindowsHookEx(WH_MOUSE_LL, _callback, GetModuleHandle(null), 0);
            if (_hHook == nint.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        _enabled = enabled;
        ResetLastInput();
        if (enabled) _timer.Start();
        else
        {
            _timer.Stop();
            Unhook();
        }
    }

    protected override void DisposeCore(bool disposing)
    {
        _enabled = false;
        _timer.Stop();
        _timer.Tick -= OnTick;
        Unhook();
    }

    private void Unhook()
    {
        if (_hHook == nint.Zero) return;
        if (!UnhookWindowsHookEx(_hHook))
        {
            Diagnostics.Report("Unhook auto lock", new Win32Exception(Marshal.GetLastWin32Error()));
            return;
        }
        _hHook = nint.Zero;
    }

    private void ResetLastInput() => _lastMouseInputMilliseconds = Environment.TickCount64;

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_enabled || Environment.TickCount64 - _lastMouseInputMilliseconds < IdleThreshold.TotalMilliseconds) return;
        ResetLastInput();
        if (!LockWorkStation())
            Diagnostics.Report("Auto lock", new Win32Exception(Marshal.GetLastWin32Error()));
    }
}
