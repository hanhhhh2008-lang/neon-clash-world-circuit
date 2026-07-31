namespace NeonClash
{
    /// <summary>
    /// Authoritative combat foundation. All positions are integer millimetres and all
    /// state advances exactly once per 60 Hz tick. Unity transforms only present it.
    /// </summary>
    public static class FighterSimulation
    {
        public const int TickRate = 60;
        public const int UnitsPerMetre = 1000;
        public const int MaxHealth = 100000;
        public const int MaxDrive = 100000;
        public const int ArenaHalfWidth = 7600;
        public const int MinimumFighterGap = 900;

        private const int GravityPerTick = 300;
        private const int GroundAccelerationPerTick = 850;
        private const int AirAccelerationPerTick = 260;
        private const int JumpVelocity = 7600;

        public static FighterState CreateInitialState(int x, int facing)
        {
            FighterState state = new FighterState();
            state.PositionX = x;
            state.PositionY = 0;
            state.Facing = facing;
            state.Health = MaxHealth;
            state.Drive = 65000;
            state.Grounded = true;
            return state;
        }

        public static void Step(ref FighterState state, FighterCommand command, int speedRating)
        {
            command.Move = Clamp(command.Move, -1, 1);
            if (state.HurtTicks > 0) state.HurtTicks--;
            if (state.StunTicks > 0) state.StunTicks--;
            if (state.ComboWindowTicks > 0) state.ComboWindowTicks--;
            else state.ComboCount = 0;

            int regeneration = state.Guarding ? 25 : 92;
            state.Drive = Clamp(state.Drive + regeneration, 0, MaxDrive);

            bool canAct = state.HurtTicks == 0 && state.StunTicks == 0 && state.Action == CombatAction.None;
            state.Crouching = canAct && state.Grounded && command.Has(FighterButtons.Crouch);
            state.Guarding = canAct && state.Grounded && command.Has(FighterButtons.Guard);

            if (canAct)
            {
                int desiredSpeed = (3500 + Clamp(speedRating, 1, 10) * 220) * command.Move;
                if (state.Crouching || state.Guarding) desiredSpeed /= 5;
                state.VelocityX = MoveTowards(
                    state.VelocityX,
                    desiredSpeed,
                    state.Grounded ? GroundAccelerationPerTick : AirAccelerationPerTick);

                if (command.Has(FighterButtons.Jump) && state.Grounded && !state.Crouching && !state.Guarding)
                {
                    state.VelocityY = JumpVelocity;
                    state.Grounded = false;
                }

                CombatAction requested = GetRequestedAction(command);
                if (requested != CombatAction.None)
                {
                    AttackSpec spec = GetAttackSpec(requested);
                    if (state.Drive >= spec.DriveCost)
                    {
                        state.Drive -= spec.DriveCost;
                        state.Action = requested;
                        state.ActionTick = 0;
                        state.ActionConnected = false;
                        state.ActionEffectTriggered = false;
                        state.Guarding = false;
                    }
                }
            }
            else if (state.HurtTicks > 0 || state.StunTicks > 0)
            {
                state.Guarding = false;
                state.Crouching = false;
            }

            if (state.Action != CombatAction.None)
            {
                state.ActionTick++;
                AttackSpec activeSpec = GetAttackSpec(state.Action);
                if (state.Action == CombatAction.Special && state.ActionTick < 20)
                    state.VelocityX += state.Facing * 24;
                if (state.ActionTick >= activeSpec.DurationTicks)
                {
                    state.Action = CombatAction.None;
                    state.ActionTick = 0;
                    state.ActionConnected = false;
                    state.ActionEffectTriggered = false;
                }
            }

            if (!state.Grounded) state.VelocityY -= GravityPerTick;
            state.PositionX += state.VelocityX / TickRate;
            state.PositionY += state.VelocityY / TickRate;

            if (state.PositionY <= 0)
            {
                state.PositionY = 0;
                state.VelocityY = 0;
                state.Grounded = true;
            }

            state.PositionX = Clamp(state.PositionX, -ArenaHalfWidth, ArenaHalfWidth);
            state.VelocityX = state.VelocityX * (state.Grounded ? 49 : 59) / 60;
        }

        public static bool TryResolveHit(ref FighterState attacker, ref FighterState defender, int power, int reach, int bonusDamage = 0)
        {
            if (attacker.Action == CombatAction.None || attacker.ActionConnected) return false;
            AttackSpec spec = GetAttackSpec(attacker.Action);
            if (!spec.IsActive(attacker.ActionTick)) return false;

            CombatBox hitbox = GetAttackBox(attacker, spec, reach);
            CombatBox hurtbox = GetHurtBox(defender);
            if (!hitbox.Overlaps(hurtbox)) return false;

            attacker.ActionConnected = true;
            bool blocked = defender.Guarding && defender.Grounded && defender.Facing == -attacker.Facing;
            int damage = spec.Damage + Clamp(power, 1, 10) * 420 + bonusDamage;
            if (blocked) damage = damage * 28 / 100;
            defender.Health = Clamp(defender.Health - damage, 0, MaxHealth);
            defender.Drive = Clamp(defender.Drive - (blocked ? 7000 : 3000), 0, MaxDrive);
            defender.VelocityX = attacker.Facing * spec.Knockback * (blocked ? 35 : 100) / 100;
            defender.HurtTicks = blocked ? 7 : attacker.Action == CombatAction.Impact ? 28 : 14;
            if (!blocked && attacker.Action == CombatAction.Impact) defender.StunTicks = 21;
            attacker.Drive = Clamp(attacker.Drive + (blocked ? 3000 : 8000), 0, MaxDrive);
            attacker.ComboCount = attacker.ComboWindowTicks > 0 ? attacker.ComboCount + 1 : 1;
            attacker.ComboWindowTicks = 48;
            return true;
        }

        public static bool ApplyProjectileHit(ref FighterState attacker, ref FighterState defender, ProjectileState projectile)
        {
            CombatBox projectileBox = new CombatBox
            {
                MinimumX = projectile.PositionX - 350,
                MaximumX = projectile.PositionX + 350,
                MinimumY = projectile.PositionY - 350,
                MaximumY = projectile.PositionY + 350
            };
            if (!projectileBox.Overlaps(GetHurtBox(defender)) || defender.HurtTicks > 1) return false;
            bool blocked = defender.Guarding && defender.Grounded && defender.Facing == -attacker.Facing;
            int damage = blocked ? projectile.Damage * 28 / 100 : projectile.Damage;
            defender.Health = Clamp(defender.Health - damage, 0, MaxHealth);
            defender.VelocityX = attacker.Facing * (blocked ? 1500 : 4200);
            defender.HurtTicks = blocked ? 7 : 15;
            defender.Drive = Clamp(defender.Drive - (blocked ? 7000 : 3000), 0, MaxDrive);
            attacker.Drive = Clamp(attacker.Drive + (blocked ? 3000 : 8000), 0, MaxDrive);
            attacker.ComboCount = attacker.ComboWindowTicks > 0 ? attacker.ComboCount + 1 : 1;
            attacker.ComboWindowTicks = 48;
            return true;
        }

        public static CombatBox GetHurtBox(FighterState state)
        {
            int height = state.Crouching ? 1050 : 2050;
            return new CombatBox
            {
                MinimumX = state.PositionX - 420,
                MaximumX = state.PositionX + 420,
                MinimumY = state.PositionY,
                MaximumY = state.PositionY + height
            };
        }

        public static CombatBox GetAttackBox(FighterState state, AttackSpec spec, int reach)
        {
            int offset = spec.HitboxForwardOffset + Clamp(reach, 1, 10) * 20;
            int centre = state.PositionX + state.Facing * offset;
            return new CombatBox
            {
                MinimumX = centre - spec.HitboxHalfWidth,
                MaximumX = centre + spec.HitboxHalfWidth,
                MinimumY = state.PositionY + spec.HitboxBottom,
                MaximumY = state.PositionY + spec.HitboxTop
            };
        }

        public static void FaceEachOther(ref FighterState first, ref FighterState second)
        {
            first.Facing = first.PositionX <= second.PositionX ? 1 : -1;
            second.Facing = -first.Facing;
        }

        public static void Separate(ref FighterState first, ref FighterState second)
        {
            if (Abs(first.PositionY - second.PositionY) >= 1200) return;
            int gap = Abs(first.PositionX - second.PositionX);
            if (gap >= MinimumFighterGap) return;
            int correction = (MinimumFighterGap - gap + 1) / 2;
            int direction = first.PositionX <= second.PositionX ? 1 : -1;
            first.PositionX = Clamp(first.PositionX - direction * correction, -ArenaHalfWidth, ArenaHalfWidth);
            second.PositionX = Clamp(second.PositionX + direction * correction, -ArenaHalfWidth, ArenaHalfWidth);
        }

        public static AttackSpec GetAttackSpec(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.LightPunch: return Spec(3, 7, 13, 1200, 5200, 2300, 0, 700, 540, 900, 1700);
                case CombatAction.HeavyPunch: return Spec(8, 15, 26, 1700, 10800, 4100, 0, 940, 760, 820, 1850);
                case CombatAction.LightKick: return Spec(5, 10, 17, 1450, 6400, 2900, 0, 800, 650, 350, 1200);
                case CombatAction.HeavyKick: return Spec(10, 17, 29, 1900, 12200, 4800, 0, 1040, 820, 450, 1450);
                case CombatAction.Special: return Spec(10, 19, 35, 2150, 14000, 5200, 25000, 1120, 940, 600, 1800);
                case CombatAction.Impact: return Spec(14, 23, 37, 2000, 18000, 6800, 32000, 1050, 880, 300, 1950);
                default: return Spec(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
        }

        private static AttackSpec Spec(int activeFrom, int activeTo, int duration, int range, int damage, int knockback, int driveCost, int offset, int halfWidth, int bottom, int top)
        {
            AttackSpec value = new AttackSpec();
            value.ActiveFromTick = activeFrom;
            value.ActiveToTick = activeTo;
            value.DurationTicks = duration;
            value.Range = range;
            value.Damage = damage;
            value.Knockback = knockback;
            value.DriveCost = driveCost;
            value.HitboxForwardOffset = offset;
            value.HitboxHalfWidth = halfWidth;
            value.HitboxBottom = bottom;
            value.HitboxTop = top;
            return value;
        }

        private static CombatAction GetRequestedAction(FighterCommand command)
        {
            if (command.Has(FighterButtons.LightPunch)) return CombatAction.LightPunch;
            if (command.Has(FighterButtons.HeavyPunch)) return CombatAction.HeavyPunch;
            if (command.Has(FighterButtons.LightKick)) return CombatAction.LightKick;
            if (command.Has(FighterButtons.HeavyKick)) return CombatAction.HeavyKick;
            if (command.Has(FighterButtons.Special)) return CombatAction.Special;
            if (command.Has(FighterButtons.Impact)) return CombatAction.Impact;
            return CombatAction.None;
        }

        private static int MoveTowards(int current, int target, int maximumDelta)
        {
            if (current < target) return current + maximumDelta > target ? target : current + maximumDelta;
            if (current > target) return current - maximumDelta < target ? target : current - maximumDelta;
            return current;
        }

        private static int Abs(int value) { return value < 0 ? -value : value; }
        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum) return minimum;
            return value > maximum ? maximum : value;
        }
    }
}
