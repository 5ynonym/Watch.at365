namespace at365.Gesture365
{
    public static partial class KeyHelper
    {
        public static bool HasAlt()
        {
            return Control.ModifierKeys.HasFlag(Keys.Alt);
        }

        public static bool HasControl()
        {
            return Control.ModifierKeys.HasFlag(Keys.Control);
        }

        public static bool HasShift()
        {
            return Control.ModifierKeys.HasFlag(Keys.Shift);
        }
    }
}
