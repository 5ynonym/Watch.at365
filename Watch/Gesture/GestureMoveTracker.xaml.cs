using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using at365.Native365;
using Point = System.Drawing.Point;

namespace at365.Gesture365
{
    public partial class GestureMoveTracker : Window
    {
        private readonly string[] _triggerMark = ["⬆️", "⬇️", "⬅️", "➡️"];
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly MouseGestureProvider _gestureProvider;
        private readonly List<MoveTrigger> _triggers = new List<MoveTrigger>(100);
        private GestureButton _gestureButton;
        private string _process = string.Empty;
        private Point _checkPoint;
        private nint _hwnd;

        public GestureMoveTracker(MouseGestureProvider core)
        {
            InitializeComponent();
            _gestureProvider = core;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeHelper.SetupOverlayWindowStyle(this);
            Hide();

            _hwnd = new WindowInteropHelper(this).Handle;
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            _timer.Tick += (sender, e) => Check();
        }

        public void Start(GestureButton gestureButton, string process)
        {
            Dispatcher.VerifyAccess();
            _gestureButton = gestureButton;
            _process = process;
            _checkPoint = Control.MousePosition;
            _triggers.Clear();

            ReadyIndicator(_checkPoint, process);
            _timer.Start();
        }

        public IEnumerable<MoveTrigger> End()
        {
            Dispatcher.VerifyAccess();
            if (_timer.IsEnabled && !_gestureProvider.IsHandled()) Check(Control.MousePosition);
            _timer.Stop();
            HideIndicator();

            var result = _triggers.ToArray();
            _triggers.Clear();

            return result;
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            _triggers.Clear();
            base.OnClosed(e);
        }

        private void Check() => Check(Control.MousePosition);

        internal void Check(Point point)
        {
            if (_gestureProvider.IsHandled())
            {
                End();
                _triggers.Clear();
                return;
            }

            var triggerCount = _triggers.Count;
            var first = triggerCount == 0;
            var trigger = GetMoveTrigger(_checkPoint, point, first);
            if (trigger == null) return;

            _checkPoint = point;

            if (!first && _triggers.Last() == trigger.Value) return;

            _triggers.Add(trigger.Value);

            if (triggerCount >= 3)
            {
                // Keep directions until button-up so unknown gestures do not replay a click.
                _timer.Stop();
                HideIndicator();
            }
            else
            {
                var (action, caption) = MouseGestureManager.Instance.GetAction(_gestureButton, _triggers, _process);
                UpdateIndicator(
                    string.Concat(_triggers.Select(each => _triggerMark[(int)each])),
                    action != null ? caption ?? string.Empty : "アクション無し");
            }
        }

        private static MoveTrigger? GetMoveTrigger(Point checkPoint, Point point, bool first)
        {
            int THRESHOLD = 50;

            if (point.Y < checkPoint.Y - THRESHOLD) return MoveTrigger.MoveUp;
            if (point.Y > checkPoint.Y + THRESHOLD) return MoveTrigger.MoveDown;
            if (point.X < checkPoint.X - THRESHOLD) return MoveTrigger.MoveLeft;
            if (point.X > checkPoint.X + THRESHOLD) return MoveTrigger.MoveRight;

            return null;
        }

        private void ReadyIndicator(Point checkPoint, string process)
        {
            _textProcessName.Text = process;
            _textBlockGesture.Text = string.Empty;
            _textBlockCaption.Text = string.Empty;
            SetVisibility(Visibility.Hidden);
            Show();
            UpdateLayout();
            NativeHelper.MoveWindowCentering(_hwnd, checkPoint.X, checkPoint.Y);
        }

        private void UpdateIndicator(string gesture, string caption)
        {
            _textBlockGesture.Text = gesture;
            _textBlockCaption.Text = caption;
            SetVisibility(Visibility.Visible);
        }

        private void HideIndicator()
        {
            _textProcessName.Text = string.Empty;
            _textBlockGesture.Text = string.Empty;
            _textBlockCaption.Text = string.Empty;
            SetVisibility(Visibility.Hidden);
        }

        private void SetVisibility(Visibility visibility)
        {
            _rectangle.Visibility = visibility;
            _textProcessName.Visibility = visibility;
            _textBlockGesture.Visibility = visibility;
            _textBlockCaption.Visibility = visibility;
        }
    }
}
