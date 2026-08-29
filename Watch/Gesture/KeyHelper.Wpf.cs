using System.Windows.Input;

namespace at365.Gesture365
{
    public static partial class KeyHelper
    {
        private static readonly Dictionary<(ModifierKeys, Key), string> _toStringCache = [];

        public static string ToString(ModifierKeys modifierKeys, Key key)
        {
            var cacheKey = (modifierKeys, key);
            if (!_toStringCache.TryGetValue(cacheKey, out var str))
            {
                var keyString = Enum.GetName(key) ?? "";
                if (modifierKeys == ModifierKeys.None) return keyString;

                str = string.Join("+", [.. modifierKeys.ToString().Split(", ").OrderBy(t => t), keyString]);
                _toStringCache[cacheKey] = str;
            }

            return str;
        }

        public static uint KeyToVirtualKey(Key key)
        {
            return (uint)KeyInterop.VirtualKeyFromKey(key);
        }

        public static uint ModifierKeysToFlags(ModifierKeys modifierKeys)
        {
            uint flags = 0;
            if ((modifierKeys & ModifierKeys.Alt) != 0) flags |= 0x0001;      // MOD_ALT
            if ((modifierKeys & ModifierKeys.Control) != 0) flags |= 0x0002;  // MOD_CONTROL
            if ((modifierKeys & ModifierKeys.Shift) != 0) flags |= 0x0004;    // MOD_SHIFT
            if ((modifierKeys & ModifierKeys.Windows) != 0) flags |= 0x0008;  // MOD_WIN
            return flags;
        }
    }
}
