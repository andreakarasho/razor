using System;
using System.Collections.Generic;

namespace Assistant
{
    public class MinHeap
    {
        private List<IComparable> m_List;

        public MinHeap()
            : this(1)
        {
        }

        public MinHeap(int capacity)
        {
            m_List = new List<IComparable>(capacity + 1);
            Count = 0;
            m_List.Add(null); // 0th index is never used, always null
        }

        public MinHeap(ICollection<IComparable> c)
            : this(c.Count)
        {
            foreach (IComparable o in c)
                m_List.Add(o);
            Count = c.Count;
            Heapify();
        }

        public int Count { get; private set; }

        public bool IsEmpty => Count <= 0;

        public void Heapify()
        {
            for (int i = Count / 2; i > 0; i--)
                PercolateDown(i);
        }

        private void PercolateDown(int hole)
        {
            IComparable tmp = m_List[hole];
            int child;

            for (; hole * 2 <= Count; hole = child)
            {
                child = hole * 2;

                if (child != Count && m_List[child + 1].CompareTo(m_List[child]) < 0)
                    child++;

                if (tmp.CompareTo(m_List[child]) >= 0)
                    m_List[hole] = m_List[child];
                else
                    break;
            }

            m_List[hole] = tmp;
        }

        public IComparable Peek()
        {
            return m_List[1];
        }

        public IComparable Pop()
        {
            IComparable top = Peek();

            m_List[1] = m_List[Count--];
            PercolateDown(1);

            return top;
        }

        public void Remove(IComparable o)
        {
            for (int i = 1; i <= Count; i++)
            {
                if (m_List[i] == o)
                {
                    m_List[i] = m_List[Count--];
                    PercolateDown(i);

                    // TODO: Do we ever need to shrink?
                    return;
                }
            }
        }

        public void Clear()
        {
            int capacity = m_List.Count / 2;

            if (capacity < 2)
                capacity = 2;
            Count = 0;
            m_List = new List<IComparable>(capacity) {null};
        }

        public void Add(IComparable o)
        {
            // PercolateUp
            int hole = ++Count;

            // Grow the list if needed
            while (m_List.Count <= Count)
                m_List.Add(null);

            for (; hole > 1 && o.CompareTo(m_List[hole / 2]) < 0; hole /= 2)
                m_List[hole] = m_List[hole / 2];
            m_List[hole] = o;
        }

        public void AddMultiple(ICollection<IComparable> col)
        {
            if (col == null || col.Count <= 0)
                return;

            foreach (IComparable o in col)
            {
                int hole = ++Count;

                // Grow the list as needed
                while (m_List.Count <= Count)
                    m_List.Add(null);

                m_List[hole] = o;
            }

            Heapify();
        }

        public List<IComparable> GetRawList()
        {
            List<IComparable> copy = new List<IComparable>(Count);

            for (int i = 1; i <= Count; i++)
                copy.Add(m_List[i]);

            return copy;
        }
    }

    public delegate void TimerCallback();

    public delegate void TimerCallbackState(object state);

    public abstract class Timer : IComparable
    {
        private static readonly MinHeap m_Heap = new MinHeap();
        private static System.Timers.Timer m_SystemTimer;
        private int m_Index;
        private readonly int m_Count;
        private DateTime m_Next;

        public Timer(TimeSpan delay)
            : this(delay, TimeSpan.Zero, 1)
        {
        }

        public Timer(TimeSpan interval, int count)
            : this(interval, interval, count)
        {
        }

        public Timer(TimeSpan delay, TimeSpan interval)
            : this(delay, interval, 0)
        {
        }

        public Timer(TimeSpan delay, TimeSpan interval, int count)
        {
            Delay = delay;
            Interval = interval;
            m_Count = count;
        }

        public TimeSpan TimeUntilTick => Running ? m_Next - DateTime.Now : TimeSpan.MaxValue;

        public bool Running { get; private set; }

        public TimeSpan Delay { get; set; }

        public TimeSpan Interval { get; set; }

        public static System.Timers.Timer SystemTimer
        {
            get => m_SystemTimer;
            set
            {
                if (m_SystemTimer != value)
                {
                    if (m_SystemTimer != null)
                        m_SystemTimer.Stop();
                    m_SystemTimer = value;
                    ChangedNextTick();
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is Timer)
                return TimeUntilTick.CompareTo(((Timer) obj).TimeUntilTick);

            return -1;
        }

        protected abstract void OnTick();

        public void Start()
        {
            if (!Running)
            {
                m_Index = 0;
                m_Next = DateTime.Now + Delay;
                Running = true;
                m_Heap.Add(this);
                ChangedNextTick(true);
            }
        }

        public void Stop()
        {
            if (!Running)
                return;

            Running = false;
            m_Heap.Remove(this);
            //ChangedNextTick();
        }

        private static void ChangedNextTick()
        {
            ChangedNextTick(false);
        }

        private static void ChangedNextTick(bool allowImmediate)
        {
            if (m_SystemTimer == null)
                return;

            m_SystemTimer.Stop();

            if (!m_Heap.IsEmpty)
            {
                int interval = (int) Math.Round(((Timer) m_Heap.Peek()).TimeUntilTick.TotalMilliseconds);

                if (allowImmediate && interval <= 0)
                    Slice();
                else
                {
                    if (interval <= 0)
                        interval = 1;

                    m_SystemTimer.Interval = interval;
                    m_SystemTimer.Start();
                }
            }
        }

        public static void Slice()
        {
            int breakCount = 100;
            List<IComparable> readd = new List<IComparable>();

            while (!m_Heap.IsEmpty && ((Timer) m_Heap.Peek()).TimeUntilTick < TimeSpan.Zero)
            {
                if (breakCount-- <= 0)
                    break;

                Timer t = (Timer) m_Heap.Pop();

                if (t != null && t.Running)
                {
                    t.OnTick();

                    if (t.Running && (t.m_Count == 0 || ++t.m_Index < t.m_Count))
                    {
                        t.m_Next = DateTime.Now + t.Interval;
                        readd.Add(t);
                    }
                    else
                        t.Stop();
                }
            }

            m_Heap.AddMultiple(readd);

            ChangedNextTick();
        }

        public static Timer DelayedCallback(TimeSpan delay, TimerCallback call)
        {
            return new OneTimeTimer(delay, call);
        }

        public static Timer DelayedCallbackState(TimeSpan delay, TimerCallbackState call, object state)
        {
            return new OneTimeTimerState(delay, call, state);
        }

        private class OneTimeTimer : Timer
        {
            private readonly TimerCallback m_Call;

            public OneTimeTimer(TimeSpan d, TimerCallback call)
                : base(d)
            {
                m_Call = call;
            }

            protected override void OnTick()
            {
                m_Call();
            }
        }

        private class OneTimeTimerState : Timer
        {
            private readonly TimerCallbackState m_Call;
            private readonly object m_State;

            public OneTimeTimerState(TimeSpan d, TimerCallbackState call, object state)
                : base(d)
            {
                m_Call = call;
                m_State = state;
            }

            protected override void OnTick()
            {
                m_Call(m_State);
            }
        }
    }
}