using System.Diagnostics;
using System.IO;
using WindowsForms = System.Windows.Forms;

namespace at365.Native365
{
    public class WindowInfo
    {
        public IntPtr Hwnd { get; private set; }
        public string FileName { get; private set; }
        public string ModuleName { get; private set; }
        public string ExeName => Path.GetFileName(FileName).ToLower();

        public static WindowInfo GetPointedWindow()
        {
            return CreateWindowInfo(NativeMethods.WindowFromPoint(new POINT(WindowsForms.Control.MousePosition)));
        }

        public static WindowInfo GetCurrentWindow()
        {
            return CreateWindowInfo(NativeMethods.GetForegroundWindow());
        }

        private static WindowInfo CreateWindowInfo(IntPtr hwnd)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
                using var process = Process.GetProcessById(pid);
                var mainModule = process.MainModule;
                return new WindowInfo()
                {
                    Hwnd = hwnd,
                    FileName = mainModule?.FileName ?? string.Empty,
                    ModuleName = mainModule?.ModuleName ?? string.Empty,
                };
            }
            catch
            {
                return new WindowInfo()
                {
                    Hwnd = hwnd,
                    FileName = string.Empty,
                    ModuleName = string.Empty,
                };
            }
        }
    }
}