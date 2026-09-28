using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using at365.Native365;
using Forms = System.Windows.Forms;

namespace at365.Watch365
{
    public partial class Watch : Window
    {
        private readonly DispatcherTimer _timer;

        public Watch()
        {
            InitializeComponent();
            _timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (sender, e) =>
                {
                    try
                    {
                        var now = DateTime.Now;
                        _timer.Interval = TimeSpan.FromMilliseconds(1000 - now.Millisecond);
                        RefreshTime(now);
                    }
                    catch (Exception error) { Common365.Diagnostics.Report("Clock update", error); }
                };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeHelper.SetupOverlayWindowStyle(this);
            Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            try { _timer.Stop(); } catch { }

            base.OnClosed(e);
        }

        public void SetVisible(bool isVisible)
        {
            if (isVisible)
            {
                RefreshTime(DateTime.Now);
                Show();
                RefreshBounds();
                _timer.Start();
            }
            else
            {
                _timer.Stop();
                Hide();
            }
        }

        public void Refresh()
        {
            RefreshBounds();
        }

        private DateTime _refreshPreviewTime;
        internal void RefreshTime(DateTime now)
        {
            if (now.Ticks / TimeSpan.TicksPerSecond == _refreshPreviewTime.Ticks / TimeSpan.TicksPerSecond) return;

            textBlockSecond.Text = now.ToString(":ss", CultureInfo.InvariantCulture);
            if (now.Date != _refreshPreviewTime.Date)
                textBlockRight.Text = now.ToString("M/d ddd", CultureInfo.InvariantCulture);

            if (now.Ticks / TimeSpan.TicksPerMinute != _refreshPreviewTime.Ticks / TimeSpan.TicksPerMinute)
            {
                textBlockLeft.Text = now.ToString("HH:mm", CultureInfo.InvariantCulture);
                RefreshBounds();
            }

            _refreshPreviewTime = now;
        }

        private void RefreshBounds()
        {
            NativeHelper.SetupOverlayWindowStyle(this);

            var settings = Shell.Properties.Settings.Default;
            var alignment = settings.Alignment == (int)VerticalAlignment.Bottom
                ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            textBlockLeft.VerticalAlignment = alignment;
            textBlockSecond.VerticalAlignment = alignment;
            textBlockRight.VerticalAlignment = alignment;

            var screens = Forms.Screen.AllScreens
                .OrderBy(each => each.Primary ? 0 : 1)
                .ThenBy(each => (each.WorkingArea.Left, each.WorkingArea.Top))
                .ToArray();
            var monitor = settings.Monitor;
            if (screens.Length == 0) return;
            var screen = screens[monitor >= 0 && monitor < screens.Length ? monitor : 0];
            var bounds = screen.Bounds;
            var dpiScale = VisualTreeHelper.GetDpi(this);
            Left = Math.Round(bounds.Left / dpiScale.DpiScaleX);
            Width = Math.Round(bounds.Width / dpiScale.DpiScaleX);
            Top = alignment == VerticalAlignment.Top
                ? Math.Round(bounds.Top / dpiScale.DpiScaleY)
                : Math.Round(bounds.Bottom / dpiScale.DpiScaleY - ActualHeight);
        }
    }
}
