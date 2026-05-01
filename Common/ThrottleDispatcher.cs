using System.Diagnostics;

namespace at365.Common365
{
    /// <summary>
    /// アクション実行をスロットル制御するディスパッチャー
    /// 指定時間間隔以上の間隔でアクション実行を行う
    /// </summary>
    public class ThrottleDispatcher : IDisposable
    {
        private readonly TimeSpan _throttleInterval;
        private readonly object _syncLock = new();
        private Stopwatch? _lastExecuteTime;
        private Action? _pendingAction;
        private System.Threading.Timer? _timer;
        private bool _disposed;

        public ThrottleDispatcher(TimeSpan throttleInterval)
        {
            _throttleInterval = throttleInterval;
            _lastExecuteTime = Stopwatch.StartNew();
        }

        /// <summary>
        /// アクションをスロットル実行キューに追加
        /// </summary>
        public void Throttle(Action action)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThrottleDispatcher));

            lock (_syncLock)
            {
                _pendingAction = action;

                var elapsed = _lastExecuteTime?.Elapsed ?? TimeSpan.Zero;
                if (elapsed >= _throttleInterval)
                {
                    // スロットル間隔が経過していれば即座に実行
                    ExecuteAction();
                }
                else if (_timer == null)
                {
                    // スロットル間隔内なら遅延実行をスケジュール
                    var delayMs = (int)(_throttleInterval - elapsed).TotalMilliseconds;
                    _timer = new System.Threading.Timer(
                        _ => ExecuteAction(),
                        null,
                        delayMs,
                        Timeout.Infinite);
                }
            }
        }

        private void ExecuteAction()
        {
            lock (_syncLock)
            {
                if (_pendingAction != null)
                {
                    try
                    {
                        _pendingAction.Invoke();
                    }
                    finally
                    {
                        _lastExecuteTime = Stopwatch.StartNew();
                        _pendingAction = null;
                        _timer?.Dispose();
                        _timer = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                _timer?.Dispose();
                _lastExecuteTime?.Stop();
                _disposed = true;
            }
        }
    }
}
