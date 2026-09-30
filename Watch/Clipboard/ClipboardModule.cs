using System.Windows;
using System.Windows.Threading;
using at365.Common365;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;

namespace at365.Clipboard365
{
    public sealed class ClipboardModule : ModuleBase<ClipboardModule>
    {
        public static void Start() { var _ = Instance; }

        private DispatcherTimer? _cleaningTimer;
        private ClipboardHistoryService? _historyService;

        public void ShowHistory() => _historyService?.ShowHistory();

        protected override void InitializeCore()
        {
            _historyService = new ClipboardHistoryService(ApplicationSettings.Current.ClipboardHistoryLimit);
            _cleaningTimer = new DispatcherTimer(
                TimeSpan.FromMinutes(5),
                DispatcherPriority.Normal,
                (_, _) => { try { CleanupClipboard(); } catch (Exception error) { Diagnostics.Report("Clipboard cleanup", error); } },
                Dispatcher.CurrentDispatcher);
        }

        protected override void DisposeCore(bool disposing)
        {
            _cleaningTimer?.Stop();
            _cleaningTimer = null;
            _clipboardPreviewText = null;
            _historyService?.Dispose();
            _historyService = null;
        }

        private string? _clipboardPreviewText;
        private void CleanupClipboard()
        {
            CleanupClipboard(Clipboard.GetDataObject(), Clipboard.Clear);
        }

        internal void CleanupClipboard(System.Windows.IDataObject? dataObject, Action clear)
        {
            // Preserve the existing no-data behavior (previously skipped by the catch).
            if (dataObject == null) return;
            var text = dataObject.GetDataPresent(DataFormats.Text) ? (string)dataObject.GetData(DataFormats.Text) : null;
            if (text == _clipboardPreviewText)
            {
                clear();
                _clipboardPreviewText = null;
            }
            else
            {
                _clipboardPreviewText = text;
            }
        }
    }
}
