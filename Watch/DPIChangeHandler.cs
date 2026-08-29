using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

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
                RestartApplication();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private static void RestartApplication()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(exePath);
                    System.Windows.Application.Current?.Shutdown();
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            _hwndSource = null;
            _disposed = true;
        }
    }
}
