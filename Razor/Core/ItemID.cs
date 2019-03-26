using Ultima;

namespace Assistant
{
    public struct ItemID
    {
        public ItemID(ushort id)
        {
            Value = id;
        }

        public ushort Value { get; }

        public static implicit operator ushort(ItemID a)
        {
            return a.Value;
        }

        public static implicit operator ItemID(ushort a)
        {
            return new ItemID(a);
        }

        public override string ToString()
        {
            try
            {
                return string.Format("{0} ({1:X4})", TileData.ItemTable[Value].Name, Value);
            }
            catch
            {
                return string.Format(" ({0:X4})", Value);
            }
        }

        public ItemData ItemData
        {
            get
            {
                try
                {
                    return TileData.ItemTable[Value];
                }
                catch
                {
                    return new ItemData("", TileFlag.None, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                }
            }
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is ItemID)) return false;

            return ((ItemID) o).Value == Value;
        }

        public static bool operator ==(ItemID l, ItemID r)
        {
            return l.Value == r.Value;
        }

        public static bool operator !=(ItemID l, ItemID r)
        {
            return l.Value != r.Value;
        }

        public static bool operator >(ItemID l, ItemID r)
        {
            return l.Value > r.Value;
        }

        public static bool operator >=(ItemID l, ItemID r)
        {
            return l.Value >= r.Value;
        }

        public static bool operator <(ItemID l, ItemID r)
        {
            return l.Value < r.Value;
        }

        public static bool operator <=(ItemID l, ItemID r)
        {
            return l.Value <= r.Value;
        }
    }
}