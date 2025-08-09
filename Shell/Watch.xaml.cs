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
            _timer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(1000),
                DispatcherPriority.Normal,
                (sender, e) =>
                {
                    try
                    {
                        var now = DateTime.Now;
                        _timer.Interval = TimeSpan.FromMilliseconds(1000 - now.Millisecond);
                        RefreshTime(now);
                    }
                    catch { }
                },
                Dispatcher);
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
        private void RefreshTime(DateTime now)
        {
            if (now.Second == _refreshPreviewTime.Second) return;

            textBlockLeft.Text = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            textBlockSecond.Text = now.ToString(":ss", CultureInfo.InvariantCulture);
            textBlockRight.Text = now.ToString("M/d ddd", CultureInfo.InvariantCulture);

            if (now.Minute != _refreshPreviewTime.Minute)
            {
                RefreshBounds();
            }

            _refreshPreviewTime = now;
        }

        private void RefreshBounds()
        {
            NativeHelper.SetupOverlayWindowStyle(this);

            var settings = Shell.Properties.Settings.Default;
            var alignment = (VerticalAlignment)settings.Alignment;
            textBlockLeft.VerticalAlignment = alignment;
            textBlockSecond.VerticalAlignment = alignment;
            textBlockRight.VerticalAlignment = alignment;

            var screens = Forms.Screen.AllScreens
                .OrderBy(each => each.Primary ? 0 : 1)
                .ThenBy(each => (each.WorkingArea.Left, each.WorkingArea.Top))
                .ToArray();
            var monitor = settings.Monitor;
            var screen = screens[monitor < screens.Length ? monitor : 0];
            var bounds = screen.Bounds;
            var dpiScale = VisualTreeHelper.GetDpi(this);
            Left = Math.Round(bounds.Left / dpiScale.DpiScaleX);
            Width = Math.Round(bounds.Width / dpiScale.DpiScaleY);
            Top = alignment == VerticalAlignment.Top
                ? Math.Round(bounds.Top / dpiScale.DpiScaleY)
                : Math.Round(bounds.Bottom / dpiScale.DpiScaleY - ActualHeight);
        }
    }
}
