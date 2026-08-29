namespace at365.Native365
{
    public static partial class NativeMethods
    {
        public static int GET_WHEEL_DELTA_WPARAM(int wParam)
        {
            return wParam >> 16;
        }
    }
}
