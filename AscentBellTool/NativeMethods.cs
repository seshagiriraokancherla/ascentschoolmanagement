using System;
using System.Runtime.InteropServices;

namespace AscentBellTool
{
    internal static class NativeMethods
    {
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        /// Keep the PC awake and the screen on (true), or let Windows sleep normally again (false).
        /// Must be called from the same (UI) thread each time.
        public static void KeepAwake(bool on)
        {
            try
            {
                SetThreadExecutionState(on ? ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED : ES_CONTINUOUS);
            }
            catch { /* not fatal */ }
        }
    }
}
