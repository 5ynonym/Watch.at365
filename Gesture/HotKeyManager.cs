using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using at365.Native365;
using static at365.Native365.NativeMethods;

namespace at365.Gesture365
{
    public class HotKeyManager
    {
        public static readonly HotKeyManager Instance = new();

        private nint _windowHandle = 0;
        private int _hotKeyId = 0;
        private readonly Dictionary<int, (ModifierKeys modifiers, Key key, Action? action)> _registeredHotKeys = [];

        public void Initialize(nint windowHandle)
        {
            _windowHandle = windowHandle;
        }

        public void RegisterHotKey(string[] process, ModifierKeys modifierKeys, Key key, Action? action, Action? gestureAction = null)
        {
            if (_windowHandle == 0)
            {
                throw new InvalidOperationException("HotKeyManager must be initialized with a window handle before registering hotkeys.");
            }

            if (gestureAction != null)
            {
                MouseGestureManager.Instance.RegisterKeyAction(modifierKeys, key, gestureAction, process);
            }

            int hotKeyId = ++_hotKeyId;
            uint vk = KeyHelper.KeyToVirtualKey(key);
            uint modifiers = KeyHelper.ModifierKeysToFlags(modifierKeys);

            if (!NativeMethods.RegisterHotKey(_windowHandle, hotKeyId, modifiers, vk))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to register hotkey. Error code: {error}");
            }

            _registeredHotKeys[hotKeyId] = (modifierKeys, key, action);
        }

        public bool ProcessHotKey(int hotKeyId)
        {
            if (!_registeredHotKeys.TryGetValue(hotKeyId, out var hotKeyData))
            {
                return false;
            }

            var (modifierKeys, key, action) = hotKeyData;

            if (MouseGestureProvider.Instance.ExecuteAction(modifierKeys, key))
            {
                return true;
            }

            if (action != null)
            {
                action();
                return true;
            }

            return false;
        }

        public void UnregisterAllHotKeys()
        {
            if (_windowHandle == 0)
            {
                return;
            }

            foreach (var hotKeyId in _registeredHotKeys.Keys.ToList())
            {
                try
                {
                    NativeMethods.UnregisterHotKey(_windowHandle, hotKeyId);
                }
                catch { }
            }

            _registeredHotKeys.Clear();
        }

        public static Action<ModifierKeys, Key, Action, Action?> When(params string[] process)
        {
            return (modifierKeys, key, action, gestureAction) =>
            {
                try
                {
                    Instance.RegisterHotKey(process, modifierKeys, key, action, gestureAction);
                }
                catch { }
            };
        }
    }
}