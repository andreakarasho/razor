namespace Assistant.Filters
{
    public class LightFilter : Filter
    {
        private LightFilter()
        {
        }

        public override byte[] PacketIDs => new byte[] {0x4E, 0x4F};

        public override LocString Name => LocString.LightFilter;

        public static void Initialize()
        {
            Register(new LightFilter());
        }

        public override void OnFilter(PacketReader p, PacketHandlerEventArgs args)
        {
            if (Windows.AllowBit(FeatureBit.LightFilter))
            {
                args.Block = true;

                if (World.Player != null)
                {
                    World.Player.LocalLightLevel = 0;
                    World.Player.GlobalLightLevel = 0;
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (Windows.AllowBit(FeatureBit.LightFilter) && World.Player != null)
            {
                World.Player.LocalLightLevel = 0;
                World.Player.GlobalLightLevel = 0;

                ClientCommunication.SendToClient(new GlobalLightLevel(0));
                ClientCommunication.SendToClient(new PersonalLightLevel(World.Player));
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (Windows.AllowBit(FeatureBit.LightFilter) && World.Player != null)
            {
                World.Player.LocalLightLevel = 6;
                World.Player.GlobalLightLevel = 2;

                ClientCommunication.SendToClient(new GlobalLightLevel(26));
                ClientCommunication.SendToClient(new PersonalLightLevel(World.Player));
            }
        }
    }
}