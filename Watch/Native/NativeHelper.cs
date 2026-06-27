using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using static at365.Native365.NativeMethods;

using Forms = System.Windows.Forms;
using Point = System.Drawing.Point;
using Screen = System.Windows.Forms.Screen;

namespace at365.Native365
{
    public static class NativeHelper
    {
        public static void OpenCurrentProcessFolder()
        {
            var path = WindowInfo.GetCurrentWindow().FileName;
            if (!string.IsNullOrEmpty(path))
            {
                Process.Start("explorer", Path.GetDirectoryName(path));
            }
        }

        public static void CopyPointedWindowProcessName()
        {
            try
            {
                System.Windows.Clipboard.SetText(WindowInfo.GetPointedWindow().ExeName);
            }
            catch { }
        }

        public static void SetupOverlayWindowStyle(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            // WS_EX_TRANSPARENTでクリックを透過
            // WS_EX_NOACTIVATEでタスクスイッチャーから除去
            SetWindowLong(handle, GWL_EXSTYLE, GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_NOOWNERZORDER);
        }

        public static void TurnOffDisplay()
        {
            PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, SC_MONITORPOWER_OFF);
        }

        public static void Sleep()
        {
            Forms.Application.SetSuspendState(Forms.PowerState.Suspend, false, false);
        }

        public static void SwitchRDPToConsole()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c tscon rdp-tcp#0 /dest:console",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch { }
        }

        public static void MoveCursor()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            Cursor.Position = new Point(
                (bounds.Left + bounds.Right) / 2,
                (bounds.Top + bounds.Bottom) / 2);
        }

        public static void AdjustFullHeight()
        {
            FitWindowToMonitorEdge(false, false, true, true);
        }

        public static void MoveWindowToLeft()
        {
            if (!FitWindowToMonitorCenter(true, false))
            {
                if (!FitWindowToMonitorEdge(true, false, false, false))
                {
                    FitWindowToMonitorEdge(false, true, false, false, "left");
                }
            }
        }

        public static void MoveWindowToRight()
        {
            if (!FitWindowToMonitorCenter(false, true))
            {
                if (!FitWindowToMonitorEdge(false, true, false, false))
                {
                    FitWindowToMonitorEdge(true, false, false, false, "right");
                }
            }
        }

        private const int FIT_PADDING_TOP = 60;
        private const int FIT_PADDING_BOTTOM = 70;
        /// <summary>
        /// 画面の中央に移動
        /// 中央よりも移動方向の端に近ければ何もせずfalseを返却
        /// </summary>
        public static bool FitWindowToMonitorCenter(bool moveLeft, bool moveRight, bool fitTop = false, bool fitBottom = false)
        {
            if (!(moveLeft ^ moveRight)) return false;

            var hwnd = GetForegroundWindow();
            if (!IsTargetHWND(hwnd)) return false;

            GetWindowRect(hwnd, out var rect);

            var workingArea = GetWorkingArea(rect);
            if (rect.Width > workingArea.Width) return false;

            var diff = rect.CenterX - workingArea.CenterX;
            if (moveLeft && diff <= 0 || moveRight && diff >= 0) return false;

            // 端に近ければ強制的にfitさせる
            DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out var rect2, Marshal.SizeOf(typeof(RECT)));
            fitTop |= rect2.Height <= workingArea.Height && rect.Top < workingArea.Top;
            fitBottom |= rect2.Height <= workingArea.Height && rect.Bottom > workingArea.Bottom;
            fitTop |= (rect.Top > workingArea.Top - FIT_PADDING_TOP && rect.Top < workingArea.Top + FIT_PADDING_TOP);
            fitBottom |= (rect.Bottom > workingArea.Bottom - FIT_PADDING_BOTTOM && rect.Bottom < workingArea.Bottom + FIT_PADDING_BOTTOM);

            var adjustHeight = rect.Height - rect2.Height;
            var newLeft = rect.Left - diff;
            var newHeight = fitTop & fitBottom ? workingArea.Height + adjustHeight : rect.Height;
            var newTop = fitTop ? workingArea.Top
                : fitBottom ? workingArea.Bottom - newHeight + adjustHeight
                : rect.Top;

            // 全画面の場合にタスクバーがでてくるのを防ぐための2回SetWindowPos
            SetWindowPos(hwnd, nint.Zero, newLeft, newTop, rect.Width, newHeight, 0);
            SetWindowPos(hwnd, nint.Zero, newLeft, newTop, rect.Width, newHeight, 0);

            GetWindowRect(hwnd, out var rectAfter);
            return (rect.CenterX != rectAfter.CenterX || rect.CenterY != rectAfter.CenterY);
        }

        /// <summary>
        /// 画面の端に移動
        /// </summary>
        public static bool FitWindowToMonitorEdge(bool fitLeft, bool fitRight, bool fitTop, bool fitBottom, string monitor = "")
        {
            if (!(fitLeft | fitRight | fitTop | fitBottom)) return false;

            var hwnd = GetForegroundWindow();
            if (!IsTargetHWND(hwnd)) return false;

            GetWindowRect(hwnd, out var rect);
            var workingArea = GetWorkingArea(rect);
            if (rect.Width >= workingArea.Width * 2) return false;

            if (monitor == "left")
            {
                rect.Left -= workingArea.Width;
                rect.Right -= workingArea.Width;
                var workingArea2 = GetWorkingArea(rect);
                if ((workingArea2.Left == workingArea.Left && workingArea2.Top == workingArea.Top)
                    || (workingArea2.Top != workingArea.Top && workingArea2.Bottom != workingArea.Bottom))
                {
                    return false;
                }
                workingArea = workingArea2;
            }
            else if (monitor == "right")
            {
                rect.Left += workingArea.Width;
                rect.Right += workingArea.Width;
                var workingArea2 = GetWorkingArea(rect);
                if ((workingArea2.Left == workingArea.Left && workingArea2.Top == workingArea.Top)
                    || (workingArea2.Top != workingArea.Top && workingArea2.Bottom != workingArea.Bottom))
                {
                    return false;
                }
                workingArea = workingArea2;
            }

            var overflowWidth = rect.Width > workingArea.Width;
            fitLeft |= overflowWidth;
            fitRight |= overflowWidth;

            var overflowHeight = rect.Height > workingArea.Height;
            fitTop |= overflowHeight;
            fitBottom |= overflowHeight;

            DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out var rect2, Marshal.SizeOf(typeof(RECT)));
            fitTop |= rect2.Height <= workingArea.Height && rect.Top < workingArea.Top;
            fitBottom |= rect2.Height <= workingArea.Height && rect.Bottom > workingArea.Bottom;
            fitTop |= (rect.Top > workingArea.Top - FIT_PADDING_TOP && rect.Top < workingArea.Top + FIT_PADDING_TOP);
            fitBottom |= (rect.Bottom > workingArea.Bottom - FIT_PADDING_BOTTOM && rect.Bottom < workingArea.Bottom + FIT_PADDING_BOTTOM);

            var adjustWidth = (rect.Width - rect2.Width);
            var adjustHeight = rect.Height - rect2.Height;
            var newWidth = fitLeft & fitRight ? workingArea.Width + adjustWidth : rect.Width;
            var newHeight = fitTop & fitBottom ? workingArea.Height + adjustHeight : rect.Height;
            var newLeft = fitLeft & fitRight ? workingArea.Left + (workingArea.Width - newWidth) / 2
                : fitLeft ? workingArea.Left - adjustWidth / 2
                : fitRight ? workingArea.Right - newWidth + adjustWidth / 2
                : rect.Left;
            var newTop = fitTop ? workingArea.Top
                : fitBottom ? workingArea.Bottom - newHeight + adjustHeight
                : rect.Top;

            // 全画面の場合にタスクバーがでてくるのを防ぐための2回SetWindowPos
            SetWindowPos(hwnd, nint.Zero, newLeft, newTop, newWidth, newHeight, 0);
            SetWindowPos(hwnd, nint.Zero, newLeft, newTop, newWidth, newHeight, 0);

            GetWindowRect(hwnd, out var rectAfter);
            return (fitLeft && rect.Left != rectAfter.Left)
                || (fitRight && rect.Right != rectAfter.Right)
                || (fitTop && rect.Top != rectAfter.Top)
                || (fitBottom && rect.Bottom != rectAfter.Bottom);
        }

        public static void MoveWindowCentering(nint hwnd, int left, int top)
        {
            GetWindowRect(hwnd, out var rect);
            SetWindowPos(hwnd, nint.Zero, left - rect.Width / 2, top - rect.Height / 2, 0, 0, SWP_NOSIZE);
        }

        public static void MinimizeWindow()
        {
            var hwnd = GetForegroundWindow();
            if (!IsTargetHWND(hwnd)) return;

            SendMessage(hwnd, WM_SYSCOMMAND, SC_MINIMIZE, nint.Zero);
        }

        public static void MaximizeWindow()
        {
            var hwnd = GetForegroundWindow();
            if (!IsTargetHWND(hwnd)) return;

            SendMessage(hwnd, WM_SYSCOMMAND, SC_MAXIMIZE, nint.Zero);
        }

        private static RECT GetWorkingArea(RECT rect)
        {
            var workingArea = Screen.GetWorkingArea(new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
            return new RECT { Left = workingArea.Left, Right = workingArea.Right, Top = workingArea.Top, Bottom = workingArea.Bottom };
        }

        private static bool IsTargetHWND(nint hwnd, bool ignoreNoTitle = true)
        {
            if (hwnd != nint.Zero && hwnd != GetDesktopWindow())
            {
                // デスクトップアイコンのウィンドウを対象から外すためタイトルが無いものを除外
                return !ignoreNoTitle || GetWindowText(hwnd, new StringBuilder(2048), 2048) > 0;
            }

            return false;
        }
    }
}
