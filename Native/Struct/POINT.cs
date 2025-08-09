using System.Runtime.InteropServices;

namespace at365.Native365
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(System.Drawing.Point p)
        {
            X = p.X;
            Y = p.Y;
        }

        public POINT(System.Windows.Point p)
        {
            X = (int)p.X;
            Y = (int)p.Y;
        }
    }
}
