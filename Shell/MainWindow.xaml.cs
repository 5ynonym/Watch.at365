using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using at365.Clipboard365;
using at365.Common365;
using at365.Gesture365;
using at365.Native365;
using Key = System.Windows.Input.Key;

namespace at365.Shell
{
    public partial class MainWindow : Window
    {
        private readonly BitmapFrame _iconOn = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/On.ico", UriKind.RelativeOrAbsolute));
        private readonly BitmapFrame _iconOff = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/Off.ico", UriKind.RelativeOrAbsolute));
        private readonly Watch365.Watch _watch = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            try
            {
                PreventMultipleInstances();

                base.OnSourceInitialized(e);
                NativeHelper.SetupOverlayWindowStyle(this);
                Hide();

                InitializeDisplayChangeNotification();
                InitializeWatch();
                InitializeHotkeys();
                InitializeModules();
            }
            catch
            {
                // 重複起動で例外になるので終了しておく
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try { _watch.Close(); } catch { }
            try { notifyIcon.Dispose(); } catch { }
            try { ModuleBase.DisposeAll(); } catch { }

            base.OnClosed(e);
        }

        private static void InitializeModules()
        {
            GestureModule.Start();
            ClipboardModule.Start();
        }

        private void InitializeDisplayChangeNotification()
        {
            var source = HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM_DISPLAYCHANGE)
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
                var currentProcess = Process.GetCurrentProcess();
                var exePath = currentProcess.MainModule?.FileName;

                if (exePath != null)
                {
                    Process.Start(exePath);
                    System.Windows.Application.Current?.Shutdown();
                }
            }
            catch
            {
            }
        }

        private void InitializeHotkeys()
        {
            var whenever = HotKeyManager.When();
            whenever(ModifierKeys.None, Key.Pause, ToggleVisible, null);
            whenever(ModifierKeys.Shift, Key.Pause, void () => Task.Run(() =>
            {
                Thread.Sleep(1000);
                NativeHelper.TurnOffDisplay();
            }), null);
            whenever(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key.M, NativeHelper.MoveCursor, null);
            //whenever(ModifierKeys.Windows | ModifierKeys.Control, Key.Down, NativeHelper.MinimizeWindow, null);
        }

        private void InitializeWatch()
        {
            _watch.IsVisibleChanged += (sender, e) =>
            {
                if (e.NewValue is bool isVisible && isVisible)
                {
                    notifyIcon.IconSource = _iconOn;
                }
                else
                {
                    notifyIcon.IconSource = _iconOff;
                }
            };

            _watch.Show();
            _watch.SetVisible(Properties.Settings.Default.Visible);
        }

        private void UpdateMenuState()
        {
            var settings = Properties.Settings.Default;
            var monitor = settings.Monitor;
            monitor0.IsChecked = monitor == 0;
            monitor1.IsChecked = monitor == 1;
            monitor2.IsChecked = monitor == 2;
            monitor3.IsChecked = monitor == 3;

            watchVisible.IsChecked = _watch.IsVisible;

            var alignment = settings.Alignment;
            alignmentTop.IsChecked = alignment == (int)VerticalAlignment.Top;
            alignmentBottom.IsChecked = alignment == (int)VerticalAlignment.Bottom;
        }

        private void SetMonitor(int monitor)
        {
            var settings = Properties.Settings.Default;
            settings.Monitor = monitor;
            settings.Save();

            _watch.Refresh();
        }

        private void SetAlignment(VerticalAlignment alignment)
        {
            var settings = Properties.Settings.Default;
            settings.Alignment = (int)alignment;
            settings.Save();

            _watch.Refresh();
        }

        private void ToggleVisible()
        {
            var newVisible = !_watch.IsVisible;
            var settings = Properties.Settings.Default;
            settings.Visible = newVisible;
            settings.Save();

            _watch.SetVisible(newVisible);
        }

        private static void PreventMultipleInstances()
        {
            using var currentProcess = Process.GetCurrentProcess();
            var existingProcesses = Process.GetProcessesByName(currentProcess.ProcessName)
                .Where(p => p.Id != currentProcess.Id);
            foreach (var process in existingProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();

                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private void HandleMenuDisplayOffClick(object sender, RoutedEventArgs e) => NativeHelper.TurnOffDisplay();
        private void HandleMenuExitClick(object sender, RoutedEventArgs e) => System.Windows.Application.Current?.Shutdown();
        private void HandleTrayMouseDown(object sender, RoutedEventArgs e) => ToggleVisible();
        private void HandleMenuOpened(object sender, RoutedEventArgs e) => UpdateMenuState();
        private void HandleMenuMonitor0(object sender, RoutedEventArgs e) => SetMonitor(0);
        private void HandleMenuMonitor1(object sender, RoutedEventArgs e) => SetMonitor(1);
        private void HandleMenuMonitor2(object sender, RoutedEventArgs e) => SetMonitor(2);
        private void HandleMenuMonitor3(object sender, RoutedEventArgs e) => SetMonitor(3);
        private void HandleAlignmentTop(object sender, RoutedEventArgs e) => SetAlignment(VerticalAlignment.Top);
        private void HandleAlignmentBottom(object sender, RoutedEventArgs e) => SetAlignment(VerticalAlignment.Bottom);
        private void HandleMenuToggleVisibleClick(object sender, RoutedEventArgs e) => ToggleVisible();
    }
}
