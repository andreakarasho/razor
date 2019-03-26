using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Assistant
{
    public class MemoryHelperThinggie : Timer
    {
        private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(2.5);

        public static readonly MemoryHelperThinggie Instance = new MemoryHelperThinggie();

        private MemoryHelperThinggie() : base(TimeSpan.Zero, Frequency)
        {
        }

        public static void Initialize()
        {
            Instance.Start();
        }

        [DllImport("Kernel32")]
        private static extern uint SetProcessWorkingSetSize(IntPtr hProc, int minSize, int maxSize);

        protected override void OnTick()
        {
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
        }
    }
}