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

        protected override void InitializeCore()
        {
            _cleaningTimer = new DispatcherTimer(
                TimeSpan.FromMinutes(5),
                DispatcherPriority.Normal,
                (_, _) => { try { CleanupClipboard(); } catch { } },
                Dispatcher.CurrentDispatcher);
        }

        protected override void DisposeCore(bool disposing)
        {
            _cleaningTimer?.Stop();
            _cleaningTimer = null;
        }

        private string? _clipboardPreviewText;
        private void CleanupClipboard()
        {
            var dataObject = Clipboard.GetDataObject();
            var text = dataObject.GetDataPresent(DataFormats.Text) ? (string)dataObject.GetData(DataFormats.Text) : null;
            if (text == _clipboardPreviewText)
            {
                Clipboard.Clear();
                _clipboardPreviewText = null;
                GC.Collect();
            }
            else
            {
                _clipboardPreviewText = text;
            }
        }
    }
}
