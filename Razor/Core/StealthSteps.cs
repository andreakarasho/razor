namespace Assistant
{
    public class StealthSteps
    {
        public static int Count { get; private set; }

        public static bool Counting { get; private set; }

        public static bool Hidden => Counting;

        public static void OnMove()
        {
            if (Counting && Config.GetBool("CountStealthSteps") && World.Player != null)
            {
                Count++;

                if (Config.GetBool("StealthOverhead"))
                    World.Player.OverheadMessage(LocString.StealthSteps, Count);
                else
                    World.Player.SendMessage(MsgLevel.Error, LocString.StealthSteps, Count);

                if (Count > 30)
                    Unhide();
            }
        }

        public static void Hide()
        {
            Counting = true;
            Count = 0;

            if (Config.GetBool("CountStealthSteps") && World.Player != null)
            {
                if (Config.GetBool("StealthOverhead"))
                    World.Player.OverheadMessage(LocString.StealthStart);
                else
                    World.Player.SendMessage(MsgLevel.Error, LocString.StealthStart);
            }
        }

        public static void Unhide()
        {
            Counting = false;
            Count = 0;
        }
    }
}