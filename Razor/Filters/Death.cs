namespace Assistant.Filters
{
    public class DeathFilter : Filter
    {
        private DeathFilter()
        {
        }

        public override byte[] PacketIDs => new byte[] {0x2C};

        public override LocString Name => LocString.DeathStatus;

        public static void Initialize()
        {
            Register(new DeathFilter());
        }

        public override void OnFilter(PacketReader p, PacketHandlerEventArgs args)
        {
            args.Block = true;
        }
    }
}