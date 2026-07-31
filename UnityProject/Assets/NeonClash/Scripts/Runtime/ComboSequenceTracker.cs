namespace NeonClash
{
    [System.Serializable]
    public struct ComboTrackerSnapshot
    {
        public FighterButtons B0, B1, B2, B3, B4, B5, B6, B7;
        public int T0, T1, T2, T3, T4, T5, T6, T7;
        public int Count;
    }

    public sealed class ComboSequenceTracker
    {
        private readonly FighterButtons[] buttons = new FighterButtons[8];
        private readonly int[] ticks = new int[8];
        private int count;

        public ComboTrackerSnapshot Capture()
        {
            ComboTrackerSnapshot value = new ComboTrackerSnapshot { Count = count };
            value.B0 = buttons[0]; value.B1 = buttons[1]; value.B2 = buttons[2]; value.B3 = buttons[3];
            value.B4 = buttons[4]; value.B5 = buttons[5]; value.B6 = buttons[6]; value.B7 = buttons[7];
            value.T0 = ticks[0]; value.T1 = ticks[1]; value.T2 = ticks[2]; value.T3 = ticks[3];
            value.T4 = ticks[4]; value.T5 = ticks[5]; value.T6 = ticks[6]; value.T7 = ticks[7];
            return value;
        }

        public void Restore(ComboTrackerSnapshot value)
        {
            count = value.Count < 0 ? 0 : value.Count > buttons.Length ? buttons.Length : value.Count;
            buttons[0] = value.B0; buttons[1] = value.B1; buttons[2] = value.B2; buttons[3] = value.B3;
            buttons[4] = value.B4; buttons[5] = value.B5; buttons[6] = value.B6; buttons[7] = value.B7;
            ticks[0] = value.T0; ticks[1] = value.T1; ticks[2] = value.T2; ticks[3] = value.T3;
            ticks[4] = value.T4; ticks[5] = value.T5; ticks[6] = value.T6; ticks[7] = value.T7;
        }

        public void Reset()
        {
            count = 0;
            for (int i = 0; i < buttons.Length; i++) { buttons[i] = FighterButtons.None; ticks[i] = 0; }
        }

        public bool RecordAndMatch(FighterCommand command, int tick, string sequence)
        {
            FighterButtons action = FirstAction(command.Buttons);
            if (action == FighterButtons.None) return false;
            if (count == buttons.Length)
            {
                for (int i = 1; i < count; i++) { buttons[i - 1] = buttons[i]; ticks[i - 1] = ticks[i]; }
                count--;
            }
            buttons[count] = action;
            ticks[count] = tick;
            count++;

            string[] tokens = sequence.Split(',');
            if (tokens.Length > count || tokens.Length == 0) return false;
            int start = count - tokens.Length;
            if (ticks[count - 1] - ticks[start] > 66) return false;
            for (int i = 0; i < tokens.Length; i++)
                if (buttons[start + i] != Parse(tokens[i])) return false;
            count = 0;
            return true;
        }

        private static FighterButtons FirstAction(FighterButtons value)
        {
            FighterButtons[] order = { FighterButtons.LightPunch, FighterButtons.HeavyPunch, FighterButtons.LightKick, FighterButtons.HeavyKick, FighterButtons.Special, FighterButtons.Impact };
            for (int i = 0; i < order.Length; i++) if ((value & order[i]) != 0) return order[i];
            return FighterButtons.None;
        }

        private static FighterButtons Parse(string token)
        {
            switch (token.Trim())
            {
                case "T": return FighterButtons.LightPunch;
                case "Y": return FighterButtons.HeavyPunch;
                case "U": return FighterButtons.LightKick;
                case "K": return FighterButtons.HeavyKick;
                case "L": return FighterButtons.Special;
                case "O": return FighterButtons.Impact;
                default: return FighterButtons.None;
            }
        }
    }
}
