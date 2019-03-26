using System;

namespace Assistant
{
    public class SkillTimer
    {
        private static readonly Timer m_Timer;

        static SkillTimer()
        {
            m_Timer = new InternalTimer();
        }

        public static int Count { get; private set; }

        public static bool Running => m_Timer.Running;

        public static void Start()
        {
            Count = 0;

            if (m_Timer.Running) m_Timer.Stop();

            m_Timer.Start();
            Windows.RequestTitleBarUpdate();
        }

        public static void Stop()
        {
            m_Timer.Stop();
            Windows.RequestTitleBarUpdate();
        }

        private class InternalTimer : Timer
        {
            public InternalTimer() : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            {
            }

            protected override void OnTick()
            {
                Count++;
                if (Count > 10) Stop();

                Windows.RequestTitleBarUpdate();
            }
        }
    }
}