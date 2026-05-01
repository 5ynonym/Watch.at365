using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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
        private readonly Icon _iconOn;
        private readonly Icon _iconOff;
        private readonly Watch365.Watch _watch = new();
        private NotifyIcon? _notifyIcon;
        private ToolStripMenuItem? _monitor0;
        private ToolStripMenuItem? _monitor1;
        private ToolStripMenuItem? _monitor2;
        private ToolStripMenuItem? _monitor3;
        private ToolStripMenuItem? _alignmentTop;
        private ToolStripMenuItem? _alignmentBottom;
        private ToolStripMenuItem? _watchVisible;

        public MainWindow()
        {
            InitializeComponent();
            _iconOn = LoadIconFromResource("Resources/On.ico");
            _iconOff = LoadIconFromResource("Resources/Off.ico");
        }

        private static Icon LoadIconFromResource(string resourcePath)
        {
            try
            {
                var resourceUri = $"pack://application:,,,/{resourcePath}";
                var streamResourceInfo = System.Windows.Application.GetResourceStream(new Uri(resourceUri));
                if (streamResourceInfo?.Stream != null)
                {
                    return new Icon(streamResourceInfo.Stream);
                }
            }
            catch { }
            return SystemIcons.Application;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            try
            {
                PreventMultipleInstances();

                base.OnSourceInitialized(e);
                NativeHelper.SetupOverlayWindowStyle(this);
                Hide();

                InitializeNotifyIcon();
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
            try { _notifyIcon?.Dispose(); } catch { }
            try { ModuleBase.DisposeAll(); } catch { }
            try { _iconOn?.Dispose(); } catch { }
            try { _iconOff?.Dispose(); } catch { }

            base.OnClosed(e);
        }

        private static void InitializeModules()
        {
            GestureModule.Start();
            ClipboardModule.Start();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = _iconOn,
                Visible = true,
                Text = "Watch at365"
            };

            _notifyIcon.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleVisible();
                }
            };

            _notifyIcon.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ShowContextMenu();
                }
            };
        }

        private void ShowContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var monitorMenu = new ToolStripMenuItem("Monitor");
            _monitor0 = new ToolStripMenuItem("0", null, (s, e) => SetMonitor(0));
            _monitor1 = new ToolStripMenuItem("1", null, (s, e) => SetMonitor(1));
            _monitor2 = new ToolStripMenuItem("2", null, (s, e) => SetMonitor(2));
            _monitor3 = new ToolStripMenuItem("3", null, (s, e) => SetMonitor(3));
            monitorMenu.DropDownItems.Add(_monitor0);
            monitorMenu.DropDownItems.Add(_monitor1);
            monitorMenu.DropDownItems.Add(_monitor2);
            monitorMenu.DropDownItems.Add(_monitor3);
            contextMenu.Items.Add(monitorMenu);

            var alignmentMenu = new ToolStripMenuItem("Alignment");
            _alignmentTop = new ToolStripMenuItem("Top", null, (s, e) => SetAlignment(VerticalAlignment.Top));
            _alignmentBottom = new ToolStripMenuItem("Bottom", null, (s, e) => SetAlignment(VerticalAlignment.Bottom));
            alignmentMenu.DropDownItems.Add(_alignmentTop);
            alignmentMenu.DropDownItems.Add(_alignmentBottom);
            contextMenu.Items.Add(alignmentMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            _watchVisible = new ToolStripMenuItem("Toggle Watch", null, (s, e) => ToggleVisible());
            contextMenu.Items.Add(_watchVisible);

            var displayOffMenu = new ToolStripMenuItem("Turn off Display", null, (s, e) => HandleMenuDisplayOffClick(s, null));
            contextMenu.Items.Add(displayOffMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitMenu = new ToolStripMenuItem("Exit", null, (s, e) => HandleMenuExitClick(s, null));
            contextMenu.Items.Add(exitMenu);

            contextMenu.Opening += (s, e) => UpdateMenuState();

            var pt = Control.MousePosition;
            contextMenu.Show(new System.Drawing.Point(pt.X, pt.Y));
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
        }

        private void InitializeWatch()
        {
            _watch.IsVisibleChanged += (sender, e) =>
            {
                if (_notifyIcon != null)
                {
                    if (e.NewValue is bool isVisible && isVisible)
                    {
                        _notifyIcon.Icon = _iconOn;
                    }
                    else
                    {
                        _notifyIcon.Icon = _iconOff;
                    }
                }
            };

            _watch.Show();
            _watch.SetVisible(Properties.Settings.Default.Visible);
        }

        private void UpdateMenuState()
        {
            var settings = Properties.Settings.Default;
            var monitor = settings.Monitor;
            if (_monitor0 != null) _monitor0.Checked = monitor == 0;
            if (_monitor1 != null) _monitor1.Checked = monitor == 1;
            if (_monitor2 != null) _monitor2.Checked = monitor == 2;
            if (_monitor3 != null) _monitor3.Checked = monitor == 3;

            if (_watchVisible != null) _watchVisible.Checked = _watch.IsVisible;

            var alignment = settings.Alignment;
            if (_alignmentTop != null) _alignmentTop.Checked = alignment == (int)VerticalAlignment.Top;
            if (_alignmentBottom != null) _alignmentBottom.Checked = alignment == (int)VerticalAlignment.Bottom;
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
