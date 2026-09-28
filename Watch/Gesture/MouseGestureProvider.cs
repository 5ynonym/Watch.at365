using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using at365.Common365;
using at365.Native365;
using static at365.Native365.NativeMethods;

namespace at365.Gesture365
{
    public class MouseGestureProvider : IDisposable
    {
        public static readonly MouseGestureProvider Instance = new();
        public static class Actions
        {
            public static void ToggleProcessBlackList()
            {
                Instance.ToggleBlackList(WindowInfo.GetCurrentWindow().ExeName);
            }
        }

        private readonly MouseHookCallback _callback;
        private readonly ThrottleDispatcher _actionThrottle = new(TimeSpan.FromMilliseconds(20));
        private readonly GestureMoveTracker _moveTracker;
        private readonly Action<GestureButton> _replayClick;

        private nint _hHook = 0;
        private GestureButton _ready;
        private string _process = string.Empty;
        private bool _handled;
        private bool _disposed;
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private HashSet<string> _processBlackList = [];

        private MouseGestureProvider() : this(ReplayClick) { }

        internal MouseGestureProvider(Action<GestureButton> replayClick)
        {
            _replayClick = replayClick;
            _callback = (int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam) =>
            {
                try
                {
                    return CallbackHook(nCode, wParam, lParam);
                }
                catch (Exception error)
                {
                    Diagnostics.Report("Mouse hook", error);
                    return CallNextHookEx(_hHook, nCode, wParam, lParam);
                }
            };

            _moveTracker = new(this);
        }

        public void Initialize()
        {
            LoadConfig();
            SetHook();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Unhook(); } catch { }
            try { _actionThrottle.Dispose(); } catch { }
            _moveTracker.Close();
            _ready = GestureButton.None;
        }

        public bool IsReady(GestureButton button = GestureButton.All) => (_ready & button) > 0;
        public bool IsHandled() => _handled;

        public bool ExecuteAction(MouseTrigger mouseTrigger)
        {
            return ExecuteAction(MouseGestureManager.CreateTrigger(mouseTrigger));
        }

        public bool ExecuteAction(IEnumerable<MoveTrigger> moveTriggers)
        {
            return ExecuteAction(MouseGestureManager.CreateTrigger(moveTriggers)) || moveTriggers.Any();
        }

        public bool ExecuteAction(ModifierKeys modifierKeys, Key key)
        {
            return ExecuteAction(MouseGestureManager.CreateTrigger(modifierKeys, key));
        }

        private void SetHook()
        {
            nint hInstance = GetModuleHandle(null);
            _hHook = SetWindowsHookEx(WH_MOUSE_LL, _callback, hInstance, 0);
            if (_hHook == nint.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private void Unhook()
        {
            if (_hHook != nint.Zero)
            {
                if (!UnhookWindowsHookEx(_hHook))
                {
                    Diagnostics.Report("Unhook gestures", new Win32Exception(Marshal.GetLastWin32Error()));
                    return;
                }
                _hHook = nint.Zero;
            }
        }

        private nint CallbackHook(int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam)
        {
            if (nCode < 0 || _disposed || lParam.dwExtraInfo == (nuint)InputSimulator.InputMarker)
                return CallNextHookEx(_hHook, nCode, wParam, lParam);

            if (!IsReady())
            {
                switch (wParam)
                {
                    case WM_RBUTTONDOWN when StartGesture(GestureButton.Right):
                        return LRESULTCancel;
                    case WM_MBUTTONDOWN when StartGesture(GestureButton.Middle):
                        return LRESULTCancel;
                }
            }
            else
            {
                var delta = lParam.mouseData >> 16;
                switch (wParam)
                {
                    case WM_RBUTTONUP when EndGesture(GestureButton.Right):
                        return LRESULTCancel;
                    case WM_MBUTTONUP when EndGesture(GestureButton.Middle):
                        return LRESULTCancel;

                    case WM_LBUTTONDOWN when HandleActionButtonDown(MouseTrigger.LeftButtonDown):
                        return LRESULTCancel;
                    case WM_LBUTTONUP when HandleActionButtonUp(MouseTrigger.LeftButtonDown):
                        return LRESULTCancel;
                    case WM_MBUTTONDOWN when HandleActionButtonDown(MouseTrigger.MiddleButtonDown):
                        return LRESULTCancel;
                    case WM_MBUTTONUP when HandleActionButtonUp(MouseTrigger.MiddleButtonDown):
                        return LRESULTCancel;
                    case WM_RBUTTONDOWN when HandleActionButtonDown(MouseTrigger.RightButtonDown):
                        return LRESULTCancel;
                    case WM_RBUTTONUP when HandleActionButtonUp(MouseTrigger.RightButtonDown):
                        return LRESULTCancel;

                    case WM_MOUSE_WHEEL when delta <= -120 && HandleActionButtonDown(MouseTrigger.WheelDown):
                        return LRESULTCancel;
                    case WM_MOUSE_WHEEL when delta >= 120 && HandleActionButtonDown(MouseTrigger.WheelUp):
                        return LRESULTCancel;
                }
            }

            return CallNextHookEx(_hHook, nCode, wParam, lParam);
        }

        private bool StartGesture(GestureButton button)
        {
            if (IsReady()) return false;
            if (IsIgnoreGesture(button)) return false;

            return BeginGesture(button, WindowInfo.GetCurrentWindow().ExeName);
        }

        internal bool BeginGesture(GestureButton button, string process)
        {
            _dispatcher.VerifyAccess();
            if (_disposed || IsReady()) return false;
            // Hook callbacks, hotkeys and tracker ticks share the UI dispatcher.
            // Publish state before returning so button-up cannot overtake button-down.
            _process = process;
            _handled = false;
            _ready = button;
            if (button == GestureButton.Right && MouseGestureManager.Instance.HasMoveAction(_process))
                _moveTracker.Start(button, _process);

            return true;
        }

        internal bool EndGesture(GestureButton button)
        {
            _dispatcher.VerifyAccess();
            if (!IsReady(button)) return false;

            try
            {
                var moves = _moveTracker.End();
                if (!_handled && !ExecuteAction(moves))
                {
                    // Replay after returning from the hook; the marker prevents recapture.
                    _dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_disposed) return;
                        try
                        {
                            _replayClick(button);
                        }
                        catch (Exception error) { Diagnostics.Report("Replay mouse click", error); }
                    }));
                }
            }
            finally
            {
                _ready = GestureButton.None;
                _handled = false;
            }

            return true;
        }

        private static void ReplayClick(GestureButton button)
        {
            if (button == GestureButton.Right) InputSimulator.RightButtonClick();
            else if (button == GestureButton.Middle) InputSimulator.MiddleButtonClick();
        }

        private (Action? action, string? caption) GetAction(string trigger)
        {
            return MouseGestureManager.Instance.GetAction(_ready, trigger, _process);
        }

        private bool ExecuteAction(string trigger)
        {
            if (!IsReady()) return false;

            var (action, _) = GetAction(trigger);
            if (action == null) return false;

            _handled = true;
            _actionThrottle.Throttle(action);

            return true;
        }

        private bool HandleActionButtonDown(MouseTrigger trigger)
        {
            ExecuteAction(trigger);
            return true;
        }

        private bool HandleActionButtonUp(MouseTrigger trigger)
        {
            return true;
        }

        private bool IsIgnoreGesture(GestureButton button)
        {
            if (KeyHelper.HasAlt()) return false;
            if (KeyHelper.HasControl()) return true;

            var process = WindowInfo.GetPointedWindow().ExeName;
            return string.IsNullOrWhiteSpace(process)
                || _processBlackList.Contains(process)
                || _ignoreProcess[GestureButton.All].Contains(process)
                || _ignoreProcess[button].Contains(process);
        }

        private static readonly Dictionary<GestureButton, HashSet<string>> _ignoreProcess = new(2)
        {
            [GestureButton.All] =
            [
                "mstsc.exe",        // Remote Desktop
                "vmconnect.exe",    // Hyper-V
                "taskmgr.exe",      // Task Manager
                "mmc.exe",          // Microsoft Management Console
            ],
            [GestureButton.Right] =
            [
                AppDomain.CurrentDomain.FriendlyName.ToLowerInvariant(),
                "explorer.exe",
            ],
            [GestureButton.Middle] =
            [
                AppDomain.CurrentDomain.FriendlyName.ToLowerInvariant(),
                "msedge.exe", "chrome.exe", // Web Browser
            ],
        };

        private void LoadConfig()
        {
            _processBlackList = new HashSet<string>(Settings.Load<string[]>("blacklist.json", [])
                .Where(name => !string.IsNullOrWhiteSpace(name)), StringComparer.OrdinalIgnoreCase);
        }

        private void ToggleBlackList(string processName)
        {
            if (_processBlackList.Contains(processName))
            {
                _processBlackList.Remove(processName);
            }
            else
            {
                _processBlackList.Add(processName);
            }

            Settings.Save("blacklist.json", _processBlackList.ToArray());
        }
    }
}
