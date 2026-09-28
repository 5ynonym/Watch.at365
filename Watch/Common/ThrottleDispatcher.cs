using System.Diagnostics;
using System.Windows.Threading;

namespace at365.Common365;

/// <summary>Coalesces actions on the owning dispatcher, outside native hook callbacks.</summary>
public sealed class ThrottleDispatcher : IDisposable
{
    private readonly TimeSpan _interval;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private Action? _pending;
    private bool _disposed;

    public ThrottleDispatcher(TimeSpan throttleInterval)
    {
        if (throttleInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(throttleInterval));
        _interval = throttleInterval;
        _timer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher);
        _timer.Tick += OnTick;
    }

    public void Throttle(Action action)
    {
        _dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(action);
        _pending = action;
        if (_timer.IsEnabled) return;
        var remaining = _interval - _elapsed.Elapsed;
        _timer.Interval = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_disposed) return;
        var action = _pending;
        _pending = null;
        _elapsed.Restart();
        try { action?.Invoke(); }
        catch (Exception error) { Diagnostics.Report("Gesture action", error); }
    }

    public void Dispose()
    {
        _dispatcher.VerifyAccess();
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _pending = null;
        _elapsed.Stop();
    }
}
