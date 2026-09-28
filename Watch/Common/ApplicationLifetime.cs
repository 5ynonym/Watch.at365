using System.Diagnostics;
using System.Windows;

namespace at365.Common365;

internal static class ApplicationLifetime
{
    private static bool _restartRequested;

    public static void RequestRestart()
    {
        var application = System.Windows.Application.Current;
        if (application == null || application.Dispatcher.HasShutdownStarted || _restartRequested) return;
        _restartRequested = true;
        application.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var path = Environment.ProcessPath;
                if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Executable path unavailable.");
                using var nextProcess = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                if (nextProcess == null) throw new InvalidOperationException("Restart failed.");
                application.Shutdown();
            }
            catch (Exception error)
            {
                _restartRequested = false;
                Diagnostics.Report("Restart", error);
            }
        }));
    }
}
