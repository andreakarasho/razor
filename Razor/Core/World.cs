using System.Collections.Generic;

namespace Assistant
{
    public class World
    {
        static World()
        {
            Servers = new Dictionary<ushort, string>();
            Items = new Dictionary<Serial, Item>();
            Mobiles = new Dictionary<Serial, Mobile>();
            ShardName = "[None]";
        }

        internal static Dictionary<ushort, string> Servers { get; }
        internal static Dictionary<Serial, Item> Items { get; }
        internal static Dictionary<Serial, Mobile> Mobiles { get; }

        internal static PlayerData Player { get; set; }

        internal static string ShardName { get; set; }

        internal static string OrigPlayerName { get; set; }

        internal static string AccountName { get; set; }

        internal static Item FindItem(Serial serial)
        {
            Item it;
            Items.TryGetValue(serial, out it);

            return it;
        }

        internal static Mobile FindMobile(Serial serial)
        {
            Mobile m;
            Mobiles.TryGetValue(serial, out m);

            return m;
        }

        internal static List<Mobile> MobilesInRange(int range)
        {
            List<Mobile> list = new List<Mobile>();

            if (Player == null)
                return list;

            foreach (Mobile m in Mobiles.Values)
            {
                if (Utility.InRange(Player.Position, m.Position, Player.VisRange))
                    list.Add(m);
            }

            return list;
        }

        internal static List<Mobile> MobilesInRange()
        {
            if (Player == null)
                return MobilesInRange(18);

            return MobilesInRange(Player.VisRange);
        }

        internal static void AddItem(Item item)
        {
            Items[item.Serial] = item;
        }

        internal static void AddMobile(Mobile mob)
        {
            Mobiles[mob.Serial] = mob;
        }

        internal static void RemoveMobile(Mobile mob)
        {
            Mobiles.Remove(mob.Serial);
        }

        internal static void RemoveItem(Item item)
        {
            Items.Remove(item.Serial);
        }
    }
}