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
    }
}
