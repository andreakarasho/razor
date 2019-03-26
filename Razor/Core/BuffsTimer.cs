using System;

namespace Assistant
{
    public class BuffsTimer
    {
        //private static int m_Count;
        private static readonly Timer m_Timer;


        static BuffsTimer()
        {
            m_Timer = new InternalTimer();
        }

        /*public static int Count
        {
            get
            {
                return m_Count;
            }
        }*/

        public static bool Running => m_Timer.Running;

        public static void Start()
        {
            //m_Count = 0;

            if (m_Timer.Running)
                m_Timer.Stop();
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
                /*m_Count++;
                if ( m_Count > 30 )
                    Stop();*/

                Windows.RequestTitleBarUpdate();
            }
        }
    }
}