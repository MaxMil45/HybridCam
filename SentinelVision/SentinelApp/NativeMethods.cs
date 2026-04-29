using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SentinelApp
{

    internal static class NativeMethods
    {
        [DllImport("SentinelVision.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitCamera();

        [DllImport("SentinelVision.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetNextFrame(IntPtr buffer, int width, int height);

        [DllImport("SentinelVision.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseCamera();
    }
}
