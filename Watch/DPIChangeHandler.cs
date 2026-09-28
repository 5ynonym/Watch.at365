using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using at365.Common365;

namespace at365.Shell
{
    public sealed class DpiChangeHandler : IDisposable
    {
        private const int WM_DPICHANGED = 0x02E0;
        private readonly Window _window;
        private HwndSource? _hwndSource;
        private bool _disposed;

        public DpiChangeHandler(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            Initialize();
        }

        private void Initialize()
        {
            _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
            _hwndSource?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DPICHANGED)
            {
                ApplicationLifetime.RequestRestart();
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _hwndSource?.RemoveHook(WndProc);
            // The Window owns this HwndSource; only remove our hook.
            _hwndSource = null;
            _disposed = true;
        }
    }
}
