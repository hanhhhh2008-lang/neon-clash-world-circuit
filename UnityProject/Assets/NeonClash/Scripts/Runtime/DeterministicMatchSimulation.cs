using System;

namespace NeonClash
{
    [Serializable]
    public struct FighterTuning
    {
        public int Speed, Power, Reach;
        public bool UsesProjectileSpecial;
        public string ComboSequence;
    }

    [Serializable]
    public struct DeterministicMatchState
    {
        public int Tick;
        public int TimerTicks;
        public int PhaseTicks;
        public int Round;
        public int FirstWins;
        public int SecondWins;
        public int RoundWinner;
        public int LastHitTick;
        public int FirstComboBonus;
        public int SecondComboBonus;
        public MatchPhase Phase;
        public FighterState First;
        public FighterState Second;
        public ProjectileState[] Projectiles;
        public ComboTrackerSnapshot FirstComboHistory;
        public ComboTrackerSnapshot SecondComboHistory;
    }

    /// <summary>
    /// Complete authoritative match state and rules with no Unity scene dependency.
    /// This is the shared local, replay, rollback, referee-checksum, and spectator
    /// simulation boundary.
    /// </summary>
    public sealed class DeterministicMatchSimulation
    {
        public const int ProjectileCapacity = 8;
        public const int RoundSeconds = 75;
        public const int IntroTicks = 70;
        public const int KnockoutTicks = 135;

        private readonly FighterTuning firstTuning;
        private readonly FighterTuning secondTuning;
        private readonly ComboSequenceTracker firstCombo = new ComboSequenceTracker();
        private readonly ComboSequenceTracker secondCombo = new ComboSequenceTracker();

        public DeterministicMatchState State;

        public DeterministicMatchSimulation(FighterTuning playerOne, FighterTuning playerTwo)
        {
            firstTuning = playerOne;
            secondTuning = playerTwo;
            State = new DeterministicMatchState
            {
                Projectiles = new ProjectileState[ProjectileCapacity],
                Round = 1,
                RoundWinner = -1,
                LastHitTick = -100
            };
            BeginRound();
        }

        public void Step(FighterCommand firstCommand, FighterCommand secondCommand)
        {
            State.Tick++;
            if (State.Phase == MatchPhase.Intro)
            {
                if (--State.PhaseTicks <= 0) State.Phase = MatchPhase.Fight;
                StoreTrackerState();
                return;
            }
            if (State.Phase == MatchPhase.Knockout)
            {
                if (--State.PhaseTicks <= 0)
                {
                    if (State.FirstWins >= 2 || State.SecondWins >= 2) State.Phase = MatchPhase.MatchOver;
                    else { State.Round++; BeginRound(); }
                }
                StoreTrackerState();
                return;
            }
            if (State.Phase == MatchPhase.MatchOver)
            {
                StoreTrackerState();
                return;
            }

            FighterSimulation.FaceEachOther(ref State.First, ref State.Second);
            if (firstCombo.RecordAndMatch(firstCommand, State.Tick, firstTuning.ComboSequence))
            {
                State.First.Drive = Minimum(FighterSimulation.MaxDrive, State.First.Drive + 18000);
                State.FirstComboBonus = 12000;
            }
            if (secondCombo.RecordAndMatch(secondCommand, State.Tick, secondTuning.ComboSequence))
            {
                State.Second.Drive = Minimum(FighterSimulation.MaxDrive, State.Second.Drive + 18000);
                State.SecondComboBonus = 12000;
            }

            FighterSimulation.Step(ref State.First, firstCommand, firstTuning.Speed);
            FighterSimulation.Step(ref State.Second, secondCommand, secondTuning.Speed);
            FighterSimulation.FaceEachOther(ref State.First, ref State.Second);
            TrySpawnProjectile(ref State.First, firstTuning, 0, State.FirstComboBonus);
            TrySpawnProjectile(ref State.Second, secondTuning, 1, State.SecondComboBonus);

            if (!firstTuning.UsesProjectileSpecial || State.First.Action != CombatAction.Special)
            {
                if (FighterSimulation.TryResolveHit(ref State.First, ref State.Second, firstTuning.Power, firstTuning.Reach,
                    State.First.Action == CombatAction.Special ? State.FirstComboBonus : 0))
                {
                    State.LastHitTick = State.Tick;
                    if (State.First.Action == CombatAction.Special) State.FirstComboBonus = 0;
                }
            }
            if (!secondTuning.UsesProjectileSpecial || State.Second.Action != CombatAction.Special)
            {
                if (FighterSimulation.TryResolveHit(ref State.Second, ref State.First, secondTuning.Power, secondTuning.Reach,
                    State.Second.Action == CombatAction.Special ? State.SecondComboBonus : 0))
                {
                    State.LastHitTick = State.Tick;
                    if (State.Second.Action == CombatAction.Special) State.SecondComboBonus = 0;
                }
            }
            FighterSimulation.Separate(ref State.First, ref State.Second);
            UpdateProjectiles();
            State.TimerTicks--;
            if (State.First.Health <= 0 || State.Second.Health <= 0 || State.TimerTicks <= 0) EndRound();
            StoreTrackerState();
        }

        public void ResetMatch()
        {
            State.Tick = 0;
            State.Round = 1;
            State.FirstWins = 0;
            State.SecondWins = 0;
            State.FirstComboBonus = 0;
            State.SecondComboBonus = 0;
            firstCombo.Reset();
            secondCombo.Reset();
            BeginRound();
        }

        public DeterministicMatchState CaptureState()
        {
            StoreTrackerState();
            return CloneState(State);
        }

        public void RestoreState(DeterministicMatchState state)
        {
            State = CloneState(state);
            if (State.Projectiles == null || State.Projectiles.Length != ProjectileCapacity)
                throw new ArgumentException("Rollback state has an invalid projectile buffer.");
            firstCombo.Restore(State.FirstComboHistory);
            secondCombo.Restore(State.SecondComboHistory);
        }

        public static DeterministicMatchState CloneState(DeterministicMatchState source)
        {
            DeterministicMatchState copy = source;
            copy.Projectiles = source.Projectiles == null ? new ProjectileState[ProjectileCapacity] : (ProjectileState[])source.Projectiles.Clone();
            return copy;
        }

        public static uint ComputeChecksum(DeterministicMatchState state)
        {
            uint hash = 2166136261u;
            Mix(ref hash, state.Tick); Mix(ref hash, state.TimerTicks); Mix(ref hash, state.PhaseTicks);
            Mix(ref hash, state.Round); Mix(ref hash, state.FirstWins); Mix(ref hash, state.SecondWins);
            Mix(ref hash, state.RoundWinner); Mix(ref hash, state.LastHitTick); Mix(ref hash, state.FirstComboBonus); Mix(ref hash, state.SecondComboBonus);
            Mix(ref hash, (int)state.Phase);
            MixFighter(ref hash, state.First); MixFighter(ref hash, state.Second);
            if (state.Projectiles != null)
            {
                Mix(ref hash, state.Projectiles.Length);
                for (int i = 0; i < state.Projectiles.Length; i++) MixProjectile(ref hash, state.Projectiles[i]);
            }
            MixCombo(ref hash, state.FirstComboHistory); MixCombo(ref hash, state.SecondComboHistory);
            return hash;
        }

        private void BeginRound()
        {
            State.First = FighterSimulation.CreateInitialState(-2500, 1);
            State.Second = FighterSimulation.CreateInitialState(2500, -1);
            State.TimerTicks = RoundSeconds * FighterSimulation.TickRate;
            State.PhaseTicks = IntroTicks;
            State.Phase = MatchPhase.Intro;
            State.RoundWinner = -1;
            State.LastHitTick = -100;
            firstCombo.Reset(); secondCombo.Reset();
            if (State.Projectiles == null || State.Projectiles.Length != ProjectileCapacity) State.Projectiles = new ProjectileState[ProjectileCapacity];
            for (int i = 0; i < State.Projectiles.Length; i++) State.Projectiles[i].Active = false;
            StoreTrackerState();
        }

        private void EndRound()
        {
            if (State.Phase != MatchPhase.Fight) return;
            bool firstWon = State.First.Health == State.Second.Health ? (State.Round % 2 == 1) : State.First.Health > State.Second.Health;
            State.RoundWinner = firstWon ? 0 : 1;
            if (firstWon) State.FirstWins++; else State.SecondWins++;
            State.Phase = MatchPhase.Knockout;
            State.PhaseTicks = KnockoutTicks;
        }

        private void TrySpawnProjectile(ref FighterState owner, FighterTuning tuning, int ownerIndex, int comboBonus)
        {
            if (!tuning.UsesProjectileSpecial || owner.Action != CombatAction.Special || owner.ActionTick < 10 || owner.ActionEffectTriggered) return;
            owner.ActionEffectTriggered = true;
            owner.ActionConnected = true;
            for (int i = 0; i < State.Projectiles.Length; i++)
            {
                if (State.Projectiles[i].Active) continue;
                State.Projectiles[i] = new ProjectileState
                {
                    Active = true,
                    Owner = ownerIndex,
                    PositionX = owner.PositionX + owner.Facing * 800,
                    PositionY = owner.PositionY + 1100,
                    VelocityX = owner.Facing * (9000 + tuning.Reach * 180),
                    RemainingTicks = 108,
                    Damage = 13000 + tuning.Power * 450 + comboBonus,
                    ColorIndex = ownerIndex
                };
                if (ownerIndex == 0) State.FirstComboBonus = 0; else State.SecondComboBonus = 0;
                return;
            }
        }

        private void UpdateProjectiles()
        {
            for (int i = 0; i < State.Projectiles.Length; i++)
            {
                ProjectileState projectile = State.Projectiles[i];
                if (!projectile.Active) continue;
                projectile.PositionX += projectile.VelocityX / FighterSimulation.TickRate;
                projectile.RemainingTicks--;
                bool hit = projectile.Owner == 0
                    ? FighterSimulation.ApplyProjectileHit(ref State.First, ref State.Second, projectile)
                    : FighterSimulation.ApplyProjectileHit(ref State.Second, ref State.First, projectile);
                if (hit) State.LastHitTick = State.Tick;
                if (hit || projectile.RemainingTicks <= 0 || Absolute(projectile.PositionX) > FighterSimulation.ArenaHalfWidth + 1000) projectile.Active = false;
                State.Projectiles[i] = projectile;
            }
        }

        private void StoreTrackerState()
        {
            State.FirstComboHistory = firstCombo.Capture();
            State.SecondComboHistory = secondCombo.Capture();
        }

        private static void MixFighter(ref uint hash, FighterState state)
        {
            Mix(ref hash, state.PositionX); Mix(ref hash, state.PositionY); Mix(ref hash, state.VelocityX); Mix(ref hash, state.VelocityY);
            Mix(ref hash, state.Facing); Mix(ref hash, state.Health); Mix(ref hash, state.Drive);
            Mix(ref hash, state.Grounded ? 1 : 0); Mix(ref hash, state.Crouching ? 1 : 0); Mix(ref hash, state.Guarding ? 1 : 0);
            Mix(ref hash, (int)state.Action); Mix(ref hash, state.ActionTick); Mix(ref hash, state.ActionConnected ? 1 : 0); Mix(ref hash, state.ActionEffectTriggered ? 1 : 0);
            Mix(ref hash, state.HurtTicks); Mix(ref hash, state.StunTicks); Mix(ref hash, state.ComboCount); Mix(ref hash, state.ComboWindowTicks);
        }

        private static void MixProjectile(ref uint hash, ProjectileState value)
        {
            Mix(ref hash, value.Active ? 1 : 0); Mix(ref hash, value.Owner); Mix(ref hash, value.PositionX); Mix(ref hash, value.PositionY);
            Mix(ref hash, value.VelocityX); Mix(ref hash, value.RemainingTicks); Mix(ref hash, value.Damage); Mix(ref hash, value.ColorIndex);
        }

        private static void MixCombo(ref uint hash, ComboTrackerSnapshot value)
        {
            Mix(ref hash, (int)value.B0); Mix(ref hash, (int)value.B1); Mix(ref hash, (int)value.B2); Mix(ref hash, (int)value.B3);
            Mix(ref hash, (int)value.B4); Mix(ref hash, (int)value.B5); Mix(ref hash, (int)value.B6); Mix(ref hash, (int)value.B7);
            Mix(ref hash, value.T0); Mix(ref hash, value.T1); Mix(ref hash, value.T2); Mix(ref hash, value.T3);
            Mix(ref hash, value.T4); Mix(ref hash, value.T5); Mix(ref hash, value.T6); Mix(ref hash, value.T7); Mix(ref hash, value.Count);
        }

        private static void Mix(ref uint hash, int value)
        {
            unchecked
            {
                hash ^= (byte)value; hash *= 16777619u;
                hash ^= (byte)(value >> 8); hash *= 16777619u;
                hash ^= (byte)(value >> 16); hash *= 16777619u;
                hash ^= (byte)(value >> 24); hash *= 16777619u;
            }
        }

        private static int Minimum(int a, int b) { return a < b ? a : b; }
        private static int Absolute(int value) { return value < 0 ? -value : value; }
    }
}
