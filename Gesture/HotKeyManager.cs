using System.Windows.Input;
using at365.Native365;
using NHotkey.Wpf;

namespace at365.Gesture365
{
    public class HotKeyManager
    {
        public static readonly HotKeyManager Instance = new();

        public void RegisterHotKey(string[] process, ModifierKeys modifierKeys, Key key, Action? action, Action? gestureAction = null)
        {
            if (gestureAction != null)
            {
                GestureManager.Instance.RegisterKeyAction(modifierKeys, key, gestureAction, process);
            }

            var name = KeyHelper.ToString(modifierKeys, key);
            HotkeyManager.Current.AddOrReplace(name, key, modifierKeys, true, (sender, e) =>
            {
                if (GestureProvider.Instance.ExecuteAction(modifierKeys, key))
                {
                    e.Handled = true;
                }
                else if (action != null)
                {
                    if (process.Length == 0 || process.Contains(WindowInfo.GetCurrentWindow().ExeName))
                    {
                        action();
                        e.Handled = true;
                    }
                }
            });
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