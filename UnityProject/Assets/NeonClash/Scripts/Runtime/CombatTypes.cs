using System;

namespace NeonClash
{
    public enum CpuDifficulty { Rookie, Pro, Ace }

    public enum CombatAction
    {
        None,
        LightPunch,
        HeavyPunch,
        LightKick,
        HeavyKick,
        Special,
        Impact
    }

    public enum MatchPhase
    {
        Intro,
        Fight,
        Knockout,
        MatchOver
    }

    [Flags]
    public enum FighterButtons
    {
        None = 0,
        Jump = 1 << 0,
        Crouch = 1 << 1,
        Guard = 1 << 2,
        LightPunch = 1 << 3,
        HeavyPunch = 1 << 4,
        LightKick = 1 << 5,
        HeavyKick = 1 << 6,
        Special = 1 << 7,
        Impact = 1 << 8
    }

    [Serializable]
    public struct FighterCommand
    {
        public int Move;
        public FighterButtons Buttons;

        public bool Has(FighterButtons button)
        {
            return (Buttons & button) != 0;
        }
    }

    [Serializable]
    public struct FighterState
    {
        public int PositionX;
        public int PositionY;
        public int VelocityX;
        public int VelocityY;
        public int Facing;
        public int Health;
        public int Drive;
        public bool Grounded;
        public bool Crouching;
        public bool Guarding;
        public CombatAction Action;
        public int ActionTick;
        public bool ActionConnected;
        public bool ActionEffectTriggered;
        public int HurtTicks;
        public int StunTicks;
        public int ComboCount;
        public int ComboWindowTicks;
    }

    public struct AttackSpec
    {
        public int ActiveFromTick;
        public int ActiveToTick;
        public int DurationTicks;
        public int Range;
        public int Damage;
        public int Knockback;
        public int DriveCost;
        public int HitboxForwardOffset;
        public int HitboxHalfWidth;
        public int HitboxBottom;
        public int HitboxTop;

        public bool IsActive(int tick)
        {
            return tick >= ActiveFromTick && tick <= ActiveToTick;
        }
    }


    [Serializable]
    public struct CombatBox
    {
        public int MinimumX, MaximumX, MinimumY, MaximumY;

        public bool Overlaps(CombatBox other)
        {
            return MinimumX <= other.MaximumX && MaximumX >= other.MinimumX &&
                   MinimumY <= other.MaximumY && MaximumY >= other.MinimumY;
        }
    }

    [Serializable]
    public struct ProjectileState
    {
        public bool Active;
        public int Owner;
        public int PositionX;
        public int PositionY;
        public int VelocityX;
        public int RemainingTicks;
        public int Damage;
        public int ColorIndex;
    }
}
