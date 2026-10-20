namespace RoRCheatPanel.Channel
{
    internal static class LobbyRules
    {
        public static int Clamp(int count)
        {
            if (count < 2)
                return 2;
            if (count > 64)
                return 64;
            return count;
        }
    }
}
