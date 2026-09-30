using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.ComponentModel;
using at365.Common365;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace at365.Clipboard365;

internal partial class ClipboardHistoryWindow : Window
{
    private readonly Func<ClipboardHistoryEntry, CancellationToken, Task<bool>> _restore;
    private readonly CancellationTokenSource _closedToken = new();
    private readonly ClipboardHistory _history;
    private readonly Action<ClipboardHistoryEntry>? _remove;
    private bool _restoring;
    private bool _closed;
    private bool _closing;
    private bool _closeRequested;

    internal ClipboardHistoryWindow(ClipboardHistory history, Func<ClipboardHistoryEntry, CancellationToken, Task<bool>> restore,
        Action<ClipboardHistoryEntry>? remove = null)
    {
        InitializeComponent();
        Width = Math.Clamp(ApplicationSettings.Current.ClipboardHistoryWidth, 280, 1600);
        MaxHeight = Math.Clamp(ApplicationSettings.Current.ClipboardHistoryHeight, 200, 1600);
        _restore = restore;
        _history = history;
        _remove = remove;
        HistoryList.ItemsSource = history.Entries;
        Deactivated += (_, _) => RequestClose();
        Closed += (_, _) => { _closed = true; _closedToken.Cancel(); _closedToken.Dispose(); };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closed is too late: native teardown can raise Deactivated first.
        _closing = true;
        base.OnClosing(e);
        if (e.Cancel) _closing = false;
    }

    private void RequestClose()
    {
        if (_closed || _closing || _closeRequested) return;
        _closeRequested = true;
        _closedToken.Cancel();
        // Leave the native input/activation callback before destroying its HWND.
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
            _closeRequested = false;
            if (!_closed && !_closing) Close();
        }));
    }

    internal void ShowAtCursor()
    {
        // Keep layout available for measuring the first card without painting
        // the window at its intermediate positions.
        Opacity = 0;
        ShowActivated = false;
        var cursor = System.Windows.Forms.Cursor.Position;
        // Position in physical pixels first; WPF processes the target monitor DPI.
        var handle = new WindowInteropHelper(this).EnsureHandle();
        ClipboardNative.SetWindowPos(handle, 0, cursor.X, cursor.Y, 0, 0, 0x0015);
        var area = System.Windows.Forms.Screen.FromPoint(cursor).WorkingArea;
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        MaxHeight = Math.Min(MaxHeight, area.Height / dpi.DpiScaleY);
        MinHeight = Math.Min(MinHeight, MaxHeight);
        Width = Math.Min(Width, area.Width / dpi.DpiScaleX);
        Show();
        UpdateLayout();
        int width = Math.Min(area.Width, (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX));
        int height = Math.Min(area.Height, (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY));
        ClipboardNative.SetWindowPos(handle, 0,
            Math.Clamp(cursor.X, area.Left, area.Right - width),
            Math.Clamp(cursor.Y, area.Top, area.Bottom - height), 0, 0, 0x0015);
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => CompletePresentation(() =>
        {
            HistoryList.SelectedIndex = HistoryList.Items.Count > 0 ? 0 : -1;
            HistoryList.UpdateLayout();
            if (HistoryList.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem item)
            {
                var center = item.PointToScreen(new System.Windows.Point(item.ActualWidth / 2, item.ActualHeight / 2));
                var origin = PointToScreen(new System.Windows.Point(0, 0));
                // Place the first card under the cursor, then clamp to the work area.
                int left = Math.Clamp((int)Math.Round(origin.X + cursor.X - center.X), area.Left, area.Right - width);
                int top = Math.Clamp((int)Math.Round(origin.Y + cursor.Y - center.Y), area.Top, area.Bottom - height);
                ClipboardNative.SetWindowPos(handle, 0, left, top, 0, 0, 0x0015);
            }
        })));
    }

    internal void CompletePresentation(Action position, Action<Exception>? report = null)
    {
        if (_closed || _closing || _closeRequested) return;
        try
        {
            position();
            if (_closed || _closing || _closeRequested) return;
            Opacity = 1;
            Activate();
            HistoryList.Focus();
            if (HistoryList.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem first) first.Focus();
        }
        catch (Exception error)
        {
            // Dispatcher callbacks run after ShowHistory's try/catch has returned.
            if (report is not null) report(error);
            else Diagnostics.Report("Position clipboard history", error);
            RequestClose();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { e.Handled = true; RequestClose(); }
        else if (e.Key == Key.Enter) { e.Handled = true; RestoreSelection(); }
    }

    private void OnItemClick(object sender, MouseButtonEventArgs e)
    {
        // The list sees preview events before the delete button does. Let the
        // button handle its own click without restoring clipboard content.
        for (var node = e.OriginalSource as DependencyObject; node is not null && node != HistoryList;
             node = System.Windows.Media.VisualTreeHelper.GetParent(node))
            if (node is System.Windows.Controls.Primitives.ButtonBase) return;
        if (ItemsControl.ContainerFromElement(HistoryList, e.OriginalSource as DependencyObject) is ListBoxItem item)
        {
            HistoryList.SelectedItem = item.DataContext;
            e.Handled = true;
            RestoreSelection();
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (_restoring || _closed || _closing || _closeRequested ||
            sender is not System.Windows.Controls.Button { DataContext: ClipboardHistoryEntry entry }) return;
        var index = HistoryList.SelectedIndex;
        var selected = HistoryList.SelectedItem;
        _remove?.Invoke(entry);
        _history.Remove(entry);
        if (HistoryList.Items.Count == 0)
        {
            RequestClose();
            return;
        }
        if (selected is ClipboardHistoryEntry remaining && _history.Entries.Contains(remaining))
            HistoryList.SelectedItem = remaining;
        else HistoryList.SelectedIndex = Math.Min(Math.Max(index, 0), HistoryList.Items.Count - 1);
        HistoryList.Focus();
    }

    private async void RestoreSelection()
    {
        if (_restoring || _closed || _closing || _closeRequested || HistoryList.SelectedItem is not ClipboardHistoryEntry entry) return;
        _restoring = true;
        HistoryList.IsEnabled = false;
        try
        {
            if (await _restore(entry, _closedToken.Token)) RequestClose();
        }
        catch (OperationCanceledException) when (_closed || _closing || _closeRequested) { }
        catch (Exception error)
        {
            // async event handlers must not propagate into Dispatcher/native callbacks.
            Diagnostics.Report("Select clipboard history", error);
        }
        finally
        {
            _restoring = false;
            if (!_closed) HistoryList.IsEnabled = true;
        }
    }
}
