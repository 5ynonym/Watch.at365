using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using at365.Common365;
using Clipboard = System.Windows.Clipboard;

namespace at365.Clipboard365;

internal sealed class ClipboardHistoryService : IDisposable
{
    private const int HotKeyId = 1;
    private readonly ClipboardHistory _history;
    private readonly HwndSource _source;
    private readonly DispatcherTimer _retryTimer;
    private readonly DispatcherTimer _captureDelayTimer;
    private readonly CancellationTokenSource _shutdown = new();
    private ClipboardHistoryWindow? _window;
    private bool _listening;
    private bool _hotKeyRegistered;
    private bool _disposed;
    private bool _writing;
    private bool _capturing;
    private uint? _lastSequence;
    private int _readAttempts;

    internal ClipboardHistoryService(int limit)
    {
        _history = new(limit);
        _retryTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(60) };
        _retryTimer.Tick += OnRetry;
        _captureDelayTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
        _captureDelayTimer.Tick += OnCaptureDelay;
        _source = new HwndSource(new HwndSourceParameters("Watch Clipboard History")
        {
            ParentWindow = new nint(-3), // HWND_MESSAGE: independent of clock visibility.
            WindowStyle = 0
        });
        try
        {
            _source.AddHook(WndProc);
            if (limit > 0)
            {
                _listening = ClipboardNative.AddClipboardFormatListener(_source.Handle);
                if (!_listening) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            // MOD_ALT | MOD_NOREPEAT avoids repeated windows while holding Alt+C.
            _hotKeyRegistered = ClipboardNative.RegisterHotKey(_source.Handle, HotKeyId, 0x4001, 0x43);
            if (!_hotKeyRegistered)
            {
                Diagnostics.Report("Register clipboard Alt+C", new Win32Exception(Marshal.GetLastWin32Error()));
                System.Windows.MessageBox.Show("Alt+C を登録できませんでした。他のアプリのホットキー設定を確認してください。\n履歴はトレイメニューからも開けます。",
                    "Watch クリップボード履歴");
            }
        }
        catch { Dispose(); throw; }
    }

    internal void ShowHistory()
    {
        if (_disposed) return;
        try { ShowHistoryCore(); }
        catch (Exception error)
        {
            Diagnostics.Report("Show clipboard history", error);
            var failed = _window;
            _window = null;
            if (failed is not null)
            {
                try { failed.Close(); }
                catch (Exception closeError) { Diagnostics.Report("Close failed clipboard view", closeError); }
            }
        }
    }

    private void ShowHistoryCore()
    {
        if (_window is not null) { _window.Activate(); return; }
        if (_history.Entries.Count == 0) return;
        // A snapshot keeps keyboard selection stable while new copies arrive.
        var snapshot = new ClipboardHistory(_history.Entries.Count);
        foreach (var entry in _history.Entries.Reverse()) snapshot.Add(entry);
        var window = new ClipboardHistoryWindow(snapshot, RestoreAsync, entry => _history.Remove(entry));
        _window = window;
        window.Closed += (_, _) => { if (ReferenceEquals(_window, window)) _window = null; };
        window.ShowAtCursor();
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (_disposed) return 0;
        if (message == 0x031D && !_writing) // WM_CLIPBOARDUPDATE
        {
            ScheduleCapture();
            handled = true;
        }
        else if (message == 0x0312 && wParam == HotKeyId)
        {
            try { ShowHistory(); }
            catch (Exception error) { Diagnostics.Report("Show clipboard history", error); }
            handled = true;
        }
        return 0;
    }

    private void OnRetry(object? sender, EventArgs e) => Capture();

    private void ScheduleCapture()
    {
        // Let the copying application finish before requesting delayed-rendered data.
        // Every new notification starts a fresh quiet period, including during retries.
        _retryTimer.Stop();
        _readAttempts = 0;
        _captureDelayTimer.Stop();
        _captureDelayTimer.Start();
    }

    private void OnCaptureDelay(object? sender, EventArgs e)
    {
        _captureDelayTimer.Stop();
        Capture();
    }

    private void Capture()
    {
        _retryTimer.Stop();
        if (_disposed || _writing || _captureDelayTimer.IsEnabled) return;
        // OLE delayed rendering can pump messages while reading the clipboard.
        // Defer reentrant notifications instead of recursively reading the same data.
        if (_capturing) { ScheduleCapture(); return; }
        var sequence = ClipboardNative.GetClipboardSequenceNumber();
        if (_lastSequence == sequence) return;
        try
        {
            _capturing = true;
            var entry = ClipboardHistoryEntry.Read(Clipboard.GetDataObject());
            if (_disposed) return;
            // Do not save a mixture if another application copied during conversion.
            if (sequence != ClipboardNative.GetClipboardSequenceNumber())
            {
                ScheduleCapture();
                return;
            }
            _lastSequence = sequence;
            _history.Add(entry);
        }
        catch (ExternalException error)
        {
            if (!RetryRead()) Diagnostics.Report("Read clipboard history", error);
        }
        catch (Exception error) { Diagnostics.Report("Read clipboard history", error); }
        finally { _capturing = false; }
    }

    private bool RetryRead()
    {
        // A notification received through an OLE message pump takes precedence.
        if (_disposed) return false;
        if (_captureDelayTimer.IsEnabled) return true;
        if (++_readAttempts >= 5) return false;
        _retryTimer.Start();
        return true;
    }

    private async Task<bool> RestoreAsync(ClipboardHistoryEntry entry, CancellationToken cancelled)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelled, _shutdown.Token);
        var sequence = ClipboardNative.GetClipboardSequenceNumber();
        for (int attempt = 0; attempt < 5 && !linked.IsCancellationRequested; attempt++)
        {
            // A new external copy during a retry supersedes the user's earlier choice.
            if (ClipboardNative.GetClipboardSequenceNumber() != sequence) return false;
            try
            {
                _writing = true;
                Clipboard.SetDataObject(entry.ToDataObject(), copy: true);
                // OLE can pump shutdown messages while publishing clipboard data.
                if (_disposed) return false;
                _lastSequence = ClipboardNative.GetClipboardSequenceNumber();
                _retryTimer.Stop();
                _captureDelayTimer.Stop();
                _history.MoveToLatest(entry);
                return true;
            }
            catch (ExternalException error)
            {
                if (attempt == 4) Diagnostics.Report("Restore clipboard history", error);
            }
            catch (Exception error)
            {
                Diagnostics.Report("Restore clipboard history", error);
                return false;
            }
            finally { _writing = false; }
            try { await Task.Delay(60, linked.Token); }
            catch (OperationCanceledException) { return false; }
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shutdown.Cancel();
        _retryTimer.Stop();
        _retryTimer.Tick -= OnRetry;
        _captureDelayTimer.Stop();
        _captureDelayTimer.Tick -= OnCaptureDelay;
        try { _window?.Close(); }
        catch (Exception error) { Diagnostics.Report("Close clipboard view on shutdown", error); }
        if (_hotKeyRegistered) ClipboardNative.UnregisterHotKey(_source.Handle, HotKeyId);
        if (_listening) ClipboardNative.RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _history.Clear();
        _shutdown.Dispose();
    }
}
