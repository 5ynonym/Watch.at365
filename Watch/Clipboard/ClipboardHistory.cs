using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using IDataObject = System.Windows.IDataObject;

namespace at365.Clipboard365;

internal sealed class ClipboardHistoryEntry
{
    public string? Text { get; }
    public BitmapSource? Image { get; }
    public string PreviewText => Text is null ? "" : Text.Length > 500 ? Text[..500] + "…" : Text;
    public string Label => Image is null ? "テキスト" : $"画像 {Image.PixelWidth} × {Image.PixelHeight}";
    public string ToolTipText
    {
        get
        {
            var details = new List<string>();
            details.Add("送信形式: " + string.Join(" + ",
                new[] { Text is not null ? "UNICODE_TEXT" : null, Image is not null ? "BITMAP" : null }.OfType<string>()));
            if (Text is not null) details.Add($"テキスト長: {Text.Length:N0}（UTF-16コード単位）");
            if (Image is not null) details.Add($"画像サイズ: {Image.PixelWidth} × {Image.PixelHeight} px\n画素形式: {Image.Format}");
            return string.Join(Environment.NewLine, details);
        }
    }

    private ClipboardHistoryEntry(string? text, BitmapSource? image) => (Text, Image) = (text, image);

    internal static ClipboardHistoryEntry? Read(IDataObject? data)
    {
        if (data is null) return null;
        string? text = null;
        // Explorer file copies may also advertise text; paths take precedence.
        if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] paths)
            text = string.Join(Environment.NewLine, paths);
        else if (data.GetDataPresent(DataFormats.UnicodeText))
            text = data.GetData(DataFormats.UnicodeText) as string;
        else if (data.GetDataPresent(DataFormats.Text))
            text = data.GetData(DataFormats.Text) as string;

        BitmapSource? image = null;
        if (data.GetDataPresent(DataFormats.Bitmap) && data.GetData(DataFormats.Bitmap) is BitmapSource source)
        {
            // CF_BITMAP has no reliable alpha channel. Copy pixels now so history does
            // not retain clipboard-owned native handles or delayed rendering objects.
            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            int stride = checked(converted.PixelWidth * 4);
            var pixels = new byte[checked(stride * converted.PixelHeight)];
            converted.CopyPixels(pixels, stride, 0);
            image = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, 96, 96,
                PixelFormats.Bgr32, null, pixels, stride);
            image.Freeze();
        }
        return text is null && image is null ? null : new(text, image);
    }

    internal DataObject ToDataObject()
    {
        var data = new DataObject();
        if (Text is not null) data.SetData(DataFormats.UnicodeText, Text);
        if (Image is not null) data.SetData(DataFormats.Bitmap, Image);
        return data;
    }
}

internal sealed class ClipboardHistory
{
    private readonly int _limit;
    private readonly ObservableCollection<ClipboardHistoryEntry> _entries = [];
    public ReadOnlyObservableCollection<ClipboardHistoryEntry> Entries { get; }

    public ClipboardHistory(int limit)
    {
        _limit = Math.Clamp(limit, 0, 1000);
        Entries = new(_entries);
    }

    public void Add(ClipboardHistoryEntry? entry)
    {
        if (entry is null || _limit <= 0) return;
        _entries.Insert(0, entry);
        while (_entries.Count > _limit) _entries.RemoveAt(_entries.Count - 1);
    }

    public void Clear() => _entries.Clear();
    public bool Remove(ClipboardHistoryEntry entry) => _entries.Remove(entry);

    public void MoveToLatest(ClipboardHistoryEntry entry)
    {
        var index = _entries.IndexOf(entry);
        if (index > 0) _entries.Move(index, 0);
        // A displayed snapshot may outlive an entry evicted by newer copies.
        else if (index < 0) Add(entry);
    }
}
