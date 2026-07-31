namespace NeonClash
{
    /// <summary>
    /// A deliberately simple, repeatable opponent. It has no random or wall-clock
    /// input, so a given tick/state sequence always produces the same command stream.
    /// </summary>
    public static class DeterministicCpu
    {
        public static FighterCommand Decide(int tick, FighterState self, FighterState opponent)
        {
            return Decide(tick, self, opponent, CpuDifficulty.Pro);
        }

        public static FighterCommand Decide(int tick, FighterState self, FighterState opponent, CpuDifficulty difficulty)
        {
            FighterCommand command = new FighterCommand();
            int distance = self.PositionX - opponent.PositionX;
            int absoluteDistance = distance < 0 ? -distance : distance;
            command.Move = absoluteDistance > 1450 ? (distance < 0 ? 1 : -1) : 0;

            int reaction = difficulty == CpuDifficulty.Rookie ? 150 : difficulty == CpuDifficulty.Pro ? 90 : 54;
            if (opponent.Action != CombatAction.None && absoluteDistance < 2300 && tick % (difficulty == CpuDifficulty.Ace ? 5 : 4) != 0)
                command.Buttons |= FighterButtons.Guard;
            else if (absoluteDistance < 1850 && tick % reaction == 12 % reaction)
                command.Buttons |= FighterButtons.LightPunch;
            else if (absoluteDistance < 2100 && tick % (reaction + 60) == 36)
                command.Buttons |= FighterButtons.HeavyKick;
            else if (self.Drive >= 25000 && absoluteDistance < 2500 && tick % (reaction + 150) == 60)
                command.Buttons |= FighterButtons.Special;
            else if (absoluteDistance > 3300 && tick % 210 == 80)
                command.Buttons |= FighterButtons.Jump;

            return command;
        }
    }
}
