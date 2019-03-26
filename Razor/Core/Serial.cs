using System;
using System.Collections.Generic;

namespace Assistant
{
    public struct Serial : IComparable
    {
        public static List<Serial> Serials { get; set; }

        public static readonly Serial MinusOne = new Serial(0xFFFFFFFF);
        public static readonly Serial Zero = new Serial(0);

        private Serial(uint serial)
        {
            Value = serial;
        }

        public uint Value { get; }

        public bool IsMobile => Value > 0 && Value < 0x40000000;

        public bool IsItem => Value >= 0x40000000 && Value <= 0x7FFFFF00;

        public bool IsValid => Value > 0 && Value <= 0x7FFFFF00;

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public int CompareTo(object o)
        {
            if (o == null) return 1;

            if (!(o is Serial)) throw new ArgumentException();

            uint ser = ((Serial) o).Value;

            if (Value > ser) return 1;

            if (Value < ser) return -1;

            return 0;
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Serial)) return false;

            return ((Serial) o).Value == Value;
        }

        public static bool operator ==(Serial l, Serial r)
        {
            return l.Value == r.Value;
        }

        public static bool operator !=(Serial l, Serial r)
        {
            return l.Value != r.Value;
        }

        public static bool operator >(Serial l, Serial r)
        {
            return l.Value > r.Value;
        }

        public static bool operator <(Serial l, Serial r)
        {
            return l.Value < r.Value;
        }

        public static bool operator >=(Serial l, Serial r)
        {
            return l.Value >= r.Value;
        }

        public static bool operator <=(Serial l, Serial r)
        {
            return l.Value <= r.Value;
        }

        public override string ToString()
        {
            return string.Format("0x{0:X}", Value);
        }

        public static Serial Parse(string s)
        {
            if (s.StartsWith("0x"))
                return Convert.ToUInt32(s.Substring(2), 16);

            return Convert.ToUInt32(s);
        }

        public static implicit operator uint(Serial a)
        {
            return a.Value;
        }

        public static implicit operator int(Serial a)
        {
            return (int) a.Value;
        }

        public static implicit operator Serial(uint a)
        {
            return new Serial(a);
        }
    }
}