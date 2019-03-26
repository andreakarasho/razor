namespace Assistant.Filters
{
    public class WeatherFilter : Filter
    {
        private WeatherFilter()
        {
        }

        public override byte[] PacketIDs => new byte[] {0x65};

        public override LocString Name => LocString.Weather;

        public static void Initialize()
        {
            Register(new WeatherFilter());
        }

        public override void OnFilter(PacketReader p, PacketHandlerEventArgs args)
        {
            if (Windows.AllowBit(FeatureBit.WeatherFilter))
                args.Block = true;
        }
    }
}