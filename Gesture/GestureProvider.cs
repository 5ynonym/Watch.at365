using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using at365.Common365;
using at365.Native365;
using static at365.Native365.NativeMethods;

namespace at365.Gesture365
{
    public class GestureProvider : IDisposable
    {
        public static readonly GestureProvider Instance = new();
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

        private nint _hHook = 0;
        private GestureButton _ready;
        private string _process = string.Empty;
        private bool _handled;
        private bool _throughMode;
        private HashSet<string> _processBlackList = [];

        private GestureProvider()
        {
            _callback = (int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam) =>
            {
                try
                {
                    return CallbackHook(nCode, wParam, lParam);
                }
                catch
                {
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
            try { Unhook(); } catch { }
            try { _actionThrottle.Dispose(); } catch { }
        }

        public bool IsReady(GestureButton button = GestureButton.All) => (_ready & button) > 0;
        public bool IsHandled() => _handled;

        public bool ExecuteAction(MouseTrigger mouseTrigger)
        {
            return ExecuteAction(GestureManager.CreateTrigger(mouseTrigger));
        }

        public bool ExecuteAction(IEnumerable<MoveTrigger> moveTriggers)
        {
            return ExecuteAction(GestureManager.CreateTrigger(moveTriggers)) || moveTriggers.Any();
        }

        public bool ExecuteAction(ModifierKeys modifierKeys, Key key)
        {
            return ExecuteAction(GestureManager.CreateTrigger(modifierKeys, key));
        }

        private void SetHook()
        {
            using var process = Process.GetCurrentProcess();
            nint hInstance = GetModuleHandle(process.MainModule!.ModuleName);
            _hHook = SetWindowsHookEx(WH_MOUSE_LL, _callback, hInstance, 0);
        }

        private void Unhook()
        {
            if (_hHook != nint.Zero)
            {
                UnhookWindowsHookEx(_hHook);
                _hHook = nint.Zero;
            }
        }

        private nint CallbackHook(int nCode, uint wParam, [In] MSLLHOOKSTRUCT lParam)
        {
            if (nCode < 0 || _throughMode) return CallNextHookEx(_hHook, nCode, wParam, lParam);

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

            ThreadPool.QueueUserWorkItem((_) =>
            {
                var targetWindow = WindowInfo.GetCurrentWindow();
                _process = targetWindow.ExeName;
                _handled = false;
                _ready = button;

                if (button == GestureButton.Right && GestureManager.Instance.HasMoveAction(_process))
                {
                    _moveTracker.Start(button, _process);
                }
            });

            return true;
        }

        private bool EndGesture(GestureButton button)
        {
            if (!IsReady(button)) return false;

            ThreadPool.QueueUserWorkItem((_) =>
            {
                if (!_handled)
                {
                    try
                    {
                        _throughMode = true;
                        if (!ExecuteAction(_moveTracker.End()))
                        {
                            if (button == GestureButton.Right)
                            {
                                InputSimulator.RightButtonClick();
                            }
                            else if (button == GestureButton.Middle)
                            {
                                InputSimulator.MiddleButtonClick();
                            }
                        }
                    }
                    finally
                    {
                        _throughMode = false;
                        _ready = GestureButton.None;
                        _handled = false;
                    }
                }
                else
                {
                    _moveTracker.End();
                    _ready = GestureButton.None;
                    _handled = false;
                }
            });

            return true;
        }

        private (Action? action, string? caption) GetAction(string trigger)
        {
            return GestureManager.Instance.GetAction(_ready, trigger, _process);
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
            ThreadPool.QueueUserWorkItem((_) => ExecuteAction(trigger));
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
            return _processBlackList.Contains(process)
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
                AppDomain.CurrentDomain.FriendlyName.ToLower(),
                "explorer.exe",
            ],
            [GestureButton.Middle] =
            [
                AppDomain.CurrentDomain.FriendlyName.ToLower(),
                "msedge.exe", "chrome.exe", // Web Browser
            ],
        };

        private void LoadConfig()
        {
            _processBlackList = [.. Settings.Load<string[]>("blacklist.json", [])];
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
