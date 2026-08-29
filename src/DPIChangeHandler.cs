using System;
using System.Windows;
using System.Windows.Interop;

namespace at365.Watch
{
    public class DpiChangeHandler : IDisposable
    {
        private const int WM_DPICHANGED = 0x02E0;
        private readonly Window _window;
        private HwndSource _hwndSource;
        private bool _disposed;

        public DpiChangeHandler(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            Initialize();
        }

        private void Initialize()
        {
            _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
            _hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DPICHANGED)
            {
                OnDpiChanged();
            }
            return IntPtr.Zero;
        }

        private static void OnDpiChanged()
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(exePath);
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_hwndSource != null)
                {
                    _hwndSource.RemoveHook(WndProc);
                    _hwndSource.Dispose();
                    _hwndSource = null;
                }
                _disposed = true;
            }
        }
    }
}

