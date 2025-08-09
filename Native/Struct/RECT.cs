using System.Runtime.InteropServices;

namespace at365.Native365
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
        public readonly int CenterX => Left + Width / 2;
        public readonly int CenterY => Top + Height / 2;
    }
}