using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Pointeur_Laser_INSA
{
    class Win32
    {
        [DllImport("User32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }
    }
}
