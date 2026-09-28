using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace at365.Common365;

internal static class Diagnostics
{
    private static readonly object Sync = new();

    // Log only operation and error codes: exception messages can contain user data.
    public static void Report(string operation, Exception error)
    {
        var line = $"{DateTimeOffset.Now:O} {operation}: {error.GetType().Name} (0x{error.HResult:X8})";
        if (error is Win32Exception nativeError) line += $" Win32={nativeError.NativeErrorCode}";
        Trace.TraceError(line);
        try
        {
            lock (Sync)
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "at365", "Watch");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "errors.log");
                if (File.Exists(path) && new FileInfo(path).Length > 1_048_576)
                    File.Move(path, path + ".old", true);
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch { /* Diagnostics must never interrupt input or shutdown. */ }
    }
}
