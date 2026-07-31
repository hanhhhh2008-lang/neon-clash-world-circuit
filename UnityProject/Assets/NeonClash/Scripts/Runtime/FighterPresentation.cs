using System.Collections.Generic;
using UnityEngine;

namespace NeonClash
{
    public enum FighterVisualState
    {
        Idle, Advance, Retreat, JumpRise, JumpFall, Crouch, Guard,
        LightPunch, HeavyPunch, LightKick, HeavyKick, Special, Impact,
        Hit, Stun, Knockdown, Finish
    }

    /// <summary>
    /// Project-owned modular cutout fighter rig. Its pose is derived only from the
    /// authoritative fighter state and simulation tick, so presentation can never
    /// change move timing or introduce nondeterministic gameplay state.
    /// </summary>
    public sealed class FighterPresentation
    {
        public const int AnimationStateCount = 17;
        private const float GroundWorldY = -3.05f;

        private readonly Transform root;
        private readonly Transform rig;
        private readonly SpriteRenderer shadow;
        private readonly SpriteRenderer paintedFighter;
        private readonly FighterDefinition definition;
        private readonly Color primary;
        private readonly Color secondary;
        private readonly Color neutral;
        private readonly Color fabric;
        private readonly Color skin;
        private readonly Color hairColor;
        private readonly Color paintedTint;
        private readonly float widthScale;
        private readonly float heightScale;
        private readonly float upperArmLength;
        private readonly float forearmLength;
        private readonly float thighLength;
        private readonly float calfLength;

        private readonly ArtPiece torso;
        private readonly ArtPiece shoulderMass;
        private readonly ArtPiece waistMass;
        private readonly ArtPiece neck;
        private readonly ArtPiece torsoPanel;
        private readonly ArtPiece chestCircuit;
        private readonly ArtPiece pelvis;
        private readonly ArtPiece head;
        private readonly ArtPiece jawShade;
        private readonly ArtPiece faceLight;
        private readonly ArtPiece visor;
        private readonly ArtPiece rearUpperArm;
        private readonly ArtPiece rearForearm;
        private readonly ArtPiece rearHand;
        private readonly ArtPiece frontUpperArm;
        private readonly ArtPiece frontForearm;
        private readonly ArtPiece frontHand;
        private readonly ArtPiece rearThigh;
        private readonly ArtPiece rearCalf;
        private readonly ArtPiece rearFoot;
        private readonly ArtPiece frontThigh;
        private readonly ArtPiece frontCalf;
        private readonly ArtPiece frontFoot;
        private readonly ArtPiece energyCore;
        private readonly ArtPiece attackArc;
        private readonly List<ArtPiece> hair = new List<ArtPiece>();
        private readonly List<ArtPiece> accessories = new List<ArtPiece>();

        public FighterVisualState CurrentState { get; private set; }
        public Transform Root { get { return root; } }
        public bool UsesPaintedArt { get { return paintedFighter != null && paintedFighter.enabled && paintedFighter.sprite != null; } }

        private FighterPresentation(FighterDefinition fighter, CostumeDefinition costume, string name, int orderOffset)
        {
            definition = fighter;
            primary = costume != null ? costume.Apply(fighter.PrimaryColor) : fighter.PrimaryColor;
            secondary = costume != null ? costume.Apply(fighter.SecondaryColor) : fighter.SecondaryColor;
            string cut = costume != null ? costume.Cut : "classic";
            neutral = cut == "heatwave" ? Color.white : cut == "sleek" ? new Color(0.16f, 0.18f, 0.25f) : NeonArtFactory.OffWhite;
            fabric = cut == "afterdark" || cut == "sleek" ? Color.Lerp(NeonArtFactory.Ink, primary, 0.18f) : Color.Lerp(NeonArtFactory.Ink, primary, 0.32f);
            skin = NeonArtFactory.SkinTone(fighter.FighterId);
            hairColor = Color.Lerp(NeonArtFactory.Ink, secondary, fighter.HairStyle == FighterHairStyle.FoxTails ? 0.62f : 0.20f);
            paintedTint = cut == "afterdark" || cut == "sleek" ? new Color(0.66f, 0.72f, 0.84f, 1f) :
                cut == "heatwave" ? new Color(1f, 0.93f, 0.84f, 1f) : Color.white;

            widthScale = fighter.BodyBuild == FighterBodyBuild.Power ? 1.18f : fighter.BodyBuild == FighterBodyBuild.Armored ? 1.24f : fighter.BodyBuild == FighterBodyBuild.Agile ? 0.88f : 1f;
            heightScale = fighter.BodyBuild == FighterBodyBuild.Power ? 1.05f : fighter.BodyBuild == FighterBodyBuild.Armored ? 1.08f : fighter.BodyBuild == FighterBodyBuild.Agile ? 1.02f : 1f;
            upperArmLength = 0.62f * widthScale;
            forearmLength = 0.58f * widthScale;
            thighLength = 0.72f * heightScale;
            calfLength = 0.68f * heightScale;

            root = new GameObject(name).transform;
            rig = new GameObject("Deterministic Cutout Rig").transform;
            rig.SetParent(root, false);
            shadow = NeonArtFactory.CreateFlat("Soft Ink Shadow", root, NeonShape.Disc, new Color(0f, 0f, 0f, 0.42f), orderOffset, new Vector3(0f, 0.04f, 0f), new Vector2(1.55f * widthScale, 0.24f));

            rearThigh = Limb("Rear Thigh", rig, fabric, orderOffset + 2, thighLength, 0.44f * widthScale);
            rearCalf = Limb("Rear Calf", rearThigh.Pivot, primary, orderOffset + 3, calfLength, 0.37f * widthScale);
            rearFoot = NeonArtFactory.CreatePiece("Rear Foot", rearCalf.Pivot, NeonShape.Tapered, neutral, orderOffset + 4, new Vector2(0.64f, 0.34f), new Vector2(0.27f, 0f));
            rearUpperArm = Limb("Rear Upper Arm", rig, fabric, orderOffset + 5, upperArmLength, 0.37f * widthScale);
            rearForearm = Limb("Rear Forearm", rearUpperArm.Pivot, primary, orderOffset + 6, forearmLength, 0.32f * widthScale);
            rearHand = NeonArtFactory.CreatePiece("Rear Glove", rearForearm.Pivot, NeonShape.Hexagon, secondary, orderOffset + 7, new Vector2(0.40f, 0.38f), new Vector2(0.18f, 0f));

            AddGarmentBack(fighter.OutfitStyle, orderOffset + 7, cut);
            pelvis = NeonArtFactory.CreatePiece("Armored Hips", rig, NeonShape.Hexagon, fabric, orderOffset + 8, new Vector2(1.00f * widthScale, 0.62f), Vector2.zero);
            waistMass = NeonArtFactory.CreatePiece("Filled Waist", rig, NeonShape.Block, primary, orderOffset + 9, new Vector2(0.78f * widthScale, 0.54f), Vector2.zero);
            shoulderMass = NeonArtFactory.CreatePiece("Filled Shoulders", rig, NeonShape.Capsule, fabric, orderOffset + 9, new Vector2(1.36f * widthScale, 0.48f), Vector2.zero);
            neck = NeonArtFactory.CreatePiece("Neck", rig, NeonShape.Capsule, skin, orderOffset + 9, new Vector2(0.30f * widthScale, 0.42f), Vector2.zero);
            torso = NeonArtFactory.CreatePiece("Tailored Torso", rig, NeonShape.Tapered, primary, orderOffset + 10, new Vector2(1.22f * widthScale, 1.42f * heightScale), Vector2.zero);
            torsoPanel = NeonArtFactory.CreatePiece("Layered Jacket Panel", rig, NeonShape.Tapered, Color.Lerp(primary, neutral, 0.42f), orderOffset + 12, new Vector2(0.68f * widthScale, 0.92f * heightScale), Vector2.zero);
            chestCircuit = NeonArtFactory.CreatePiece("Circuit Emblem", rig, NeonShape.Diamond, secondary, orderOffset + 14, new Vector2(0.27f * widthScale, 0.27f), Vector2.zero);
            head = NeonArtFactory.CreatePiece("Head", rig, NeonShape.Disc, skin, orderOffset + 16, new Vector2(0.76f * widthScale, 0.84f), Vector2.zero);
            jawShade = NeonArtFactory.CreatePiece("Jaw Shade", rig, NeonShape.Tapered, Color.Lerp(skin, NeonArtFactory.Ink, 0.23f), orderOffset + 17, new Vector2(0.50f * widthScale, 0.34f), Vector2.zero);
            AddHair(fighter.HairStyle, orderOffset + 18);
            visor = NeonArtFactory.CreatePiece("Visor Mark", rig, NeonShape.Capsule, secondary, orderOffset + 23, new Vector2(0.36f, 0.07f), Vector2.zero);
            faceLight = NeonArtFactory.CreatePiece("Eye Light", rig, NeonShape.Disc, NeonArtFactory.OffWhite, orderOffset + 24, new Vector2(0.075f, 0.075f), Vector2.zero);

            frontThigh = Limb("Front Thigh", rig, fabric, orderOffset + 25, thighLength, 0.47f * widthScale);
            frontCalf = Limb("Front Calf", frontThigh.Pivot, primary, orderOffset + 26, calfLength, 0.39f * widthScale);
            frontFoot = NeonArtFactory.CreatePiece("Front Foot", frontCalf.Pivot, NeonShape.Tapered, neutral, orderOffset + 27, new Vector2(0.67f, 0.35f), new Vector2(0.28f, 0f));
            frontUpperArm = Limb("Front Upper Arm", rig, fabric, orderOffset + 28, upperArmLength, 0.40f * widthScale);
            frontForearm = Limb("Front Forearm", frontUpperArm.Pivot, primary, orderOffset + 29, forearmLength, 0.34f * widthScale);
            frontHand = NeonArtFactory.CreatePiece("Front Glove", frontForearm.Pivot, NeonShape.Hexagon, secondary, orderOffset + 30, new Vector2(0.42f, 0.40f), new Vector2(0.19f, 0f));
            AddGarmentFront(fighter.OutfitStyle, orderOffset + 31, cut);

            energyCore = NeonArtFactory.CreatePiece("Signature Energy - " + fighter.EnergyStyle, rig, EnergyShape(fighter.EnergyStyle), secondary, orderOffset + 38, new Vector2(0.92f, 0.92f), Vector2.zero);
            attackArc = NeonArtFactory.CreatePiece("Attack Arc", rig, NeonShape.Crescent, primary, orderOffset + 40, new Vector2(1.35f, 1.35f), Vector2.zero);
            energyCore.SetVisible(false);
            attackArc.SetVisible(false);
            HideCutoutArtwork();
            paintedFighter = PaintedFighterAtlas.Create(fighter.FighterId, rig, orderOffset + 34, paintedTint);
            ApplyNeutralPose();
        }

        public static FighterPresentation Create(FighterDefinition fighter, CostumeDefinition costume, string name, int orderOffset)
        {
            return new FighterPresentation(fighter, costume, name, orderOffset);
        }

        public void Remove()
        {
            if (root != null) Object.Destroy(root.gameObject);
        }

        public FighterVisualState Present(FighterState state, int simulationTick, MatchPhase phase, bool winner)
        {
            CurrentState = ResolveVisualState(state, phase, winner);
            float x = state.PositionX / (float)FighterSimulation.UnitsPerMetre;
            float y = GroundWorldY + state.PositionY / (float)FighterSimulation.UnitsPerMetre;
            root.position = new Vector3(x, y, 0f);
            rig.localScale = new Vector3(state.Facing, 1f, 1f);
            rig.localPosition = Vector3.zero;
            rig.localRotation = Quaternion.identity;
            shadow.transform.localPosition = new Vector3(0f, -state.PositionY / (float)FighterSimulation.UnitsPerMetre + 0.04f, 0f);
            shadow.transform.localScale = new Vector3(1.55f * widthScale * Mathf.Lerp(1f, 0.62f, Mathf.Clamp01(state.PositionY / 4500f)), 0.24f, 1f);
            ApplyNeutralPose();
            ApplyStatePose(state, simulationTick, winner);
            ApplyFlash(state.HurtTicks > 0 && (simulationTick & 1) == 0);
            return CurrentState;
        }

        public static FighterVisualState ResolveVisualState(FighterState state, MatchPhase phase, bool winner)
        {
            if (phase == MatchPhase.MatchOver && winner) return FighterVisualState.Finish;
            if (state.Health <= 0 || (phase == MatchPhase.Knockout && !winner)) return FighterVisualState.Knockdown;
            if (state.StunTicks > 0) return FighterVisualState.Stun;
            if (state.HurtTicks > 0) return FighterVisualState.Hit;
            switch (state.Action)
            {
                case CombatAction.LightPunch: return FighterVisualState.LightPunch;
                case CombatAction.HeavyPunch: return FighterVisualState.HeavyPunch;
                case CombatAction.LightKick: return FighterVisualState.LightKick;
                case CombatAction.HeavyKick: return FighterVisualState.HeavyKick;
                case CombatAction.Special: return FighterVisualState.Special;
                case CombatAction.Impact: return FighterVisualState.Impact;
            }
            if (state.Guarding) return FighterVisualState.Guard;
            if (state.Crouching) return FighterVisualState.Crouch;
            if (!state.Grounded) return state.VelocityY >= 0 ? FighterVisualState.JumpRise : FighterVisualState.JumpFall;
            if (Mathf.Abs(state.VelocityX) > 250) return state.VelocityX * state.Facing > 0 ? FighterVisualState.Advance : FighterVisualState.Retreat;
            return FighterVisualState.Idle;
        }

        private void ApplyNeutralPose()
        {
            torso.Pivot.localPosition = new Vector3(0f, 1.57f * heightScale, 0f);
            torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -4f);
            torsoPanel.Pivot.localPosition = torso.Pivot.localPosition;
            torsoPanel.Pivot.localRotation = torso.Pivot.localRotation;
            chestCircuit.Pivot.localPosition = new Vector3(0.10f, 1.78f * heightScale, 0f);
            pelvis.Pivot.localPosition = new Vector3(0f, 0.97f * heightScale, 0f);
            waistMass.Pivot.localPosition = new Vector3(0f, 1.17f * heightScale, 0f);
            shoulderMass.Pivot.localPosition = new Vector3(0f, 1.94f * heightScale, 0f);
            neck.Pivot.localPosition = new Vector3(0.01f, 2.19f * heightScale, 0f);
            neck.Pivot.localRotation = Quaternion.Euler(0f, 0f, 90f);
            head.Pivot.localPosition = new Vector3(0.02f, 2.45f * heightScale, 0f);
            head.Pivot.localRotation = Quaternion.identity;
            jawShade.Pivot.localPosition = new Vector3(0.10f, 2.30f * heightScale, 0f);
            visor.Pivot.localPosition = new Vector3(0.21f, 2.49f * heightScale, 0f);
            faceLight.Pivot.localPosition = new Vector3(0.31f, 2.50f * heightScale, 0f);
            visor.Pivot.localRotation = Quaternion.identity;
            PlaceHair();

            SetArm(rearUpperArm, rearForearm, rearHand, new Vector2(-0.38f * widthScale, 1.93f * heightScale), -48f, 76f);
            SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.42f * widthScale, 1.92f * heightScale), -20f, 68f);
            SetLeg(rearThigh, rearCalf, rearFoot, new Vector2(-0.24f * widthScale, 1.00f * heightScale), -102f, 15f, 4f);
            SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.26f * widthScale, 1.00f * heightScale), -72f, -23f, 7f);
            PlaceAccessories();
            if (paintedFighter != null)
            {
                paintedFighter.transform.localPosition = Vector3.zero;
                paintedFighter.transform.localRotation = Quaternion.identity;
                paintedFighter.transform.localScale = Vector3.one;
            }
            energyCore.SetVisible(false);
            attackArc.SetVisible(false);
        }

        private void ApplyStatePose(FighterState state, int tick, bool winner)
        {
            float cycle = Mathf.Sin((tick % 120) * Mathf.PI / 60f);
            float actionProgress = state.Action == CombatAction.None ? 0f : state.ActionTick / (float)Mathf.Max(1, FighterSimulation.GetAttackSpec(state.Action).DurationTicks);
            float strike = Mathf.Sin(Mathf.Clamp01(actionProgress) * Mathf.PI);
            switch (CurrentState)
            {
                case FighterVisualState.Idle:
                    rig.localPosition = new Vector3(0f, cycle * 0.025f, 0f);
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -4f + cycle * 1.7f);
                    head.Pivot.localRotation = Quaternion.Euler(0f, 0f, cycle * 1.3f);
                    break;
                case FighterVisualState.Advance:
                case FighterVisualState.Retreat:
                    float walk = Mathf.Sin((tick % 24) * Mathf.PI / 12f) * (CurrentState == FighterVisualState.Retreat ? -1f : 1f);
                    rig.localPosition = new Vector3(0f, Mathf.Abs(walk) * 0.035f, 0f);
                    SetLeg(rearThigh, rearCalf, rearFoot, new Vector2(-0.16f, 0.98f * heightScale), -96f + walk * 24f, 12f - walk * 18f, 4f);
                    SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.18f, 0.98f * heightScale), -78f - walk * 24f, -18f + walk * 18f, 6f);
                    rearUpperArm.Pivot.localRotation = Quaternion.Euler(0f, 0f, -48f - walk * 14f);
                    frontUpperArm.Pivot.localRotation = Quaternion.Euler(0f, 0f, -20f + walk * 14f);
                    break;
                case FighterVisualState.JumpRise:
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    SetArm(rearUpperArm, rearForearm, rearHand, new Vector2(-0.28f, 1.9f), 45f, 20f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.30f, 1.9f), 58f, -16f);
                    SetLeg(rearThigh, rearCalf, rearFoot, new Vector2(-0.15f, 0.98f), -135f, 68f, 4f);
                    SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.18f, 0.98f), -42f, -60f, 6f);
                    break;
                case FighterVisualState.JumpFall:
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -10f);
                    SetArm(rearUpperArm, rearForearm, rearHand, new Vector2(-0.28f, 1.9f), 145f, -30f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.30f, 1.9f), 28f, 24f);
                    SetLeg(rearThigh, rearCalf, rearFoot, new Vector2(-0.15f, 0.98f), -112f, 42f, 4f);
                    SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.18f, 0.98f), -64f, -34f, 6f);
                    break;
                case FighterVisualState.Crouch:
                    rig.localPosition = new Vector3(0f, -0.54f, 0f);
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -18f);
                    head.Pivot.localPosition += new Vector3(0.26f, -0.23f, 0f);
                    visor.Pivot.localPosition += new Vector3(0.26f, -0.23f, 0f);
                    SetLeg(rearThigh, rearCalf, rearFoot, new Vector2(-0.18f, 0.98f), -154f, 92f, 3f);
                    SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.20f, 0.98f), -28f, -96f, 5f);
                    break;
                case FighterVisualState.Guard:
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -12f);
                    SetArm(rearUpperArm, rearForearm, rearHand, new Vector2(-0.28f, 1.90f), 54f, 47f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.32f, 1.92f), 78f, -25f);
                    energyCore.Pivot.localPosition = new Vector3(0.64f, 1.67f, 0f);
                    energyCore.SetVisible(true);
                    energyCore.SetVisualScale(0.68f + cycle * 0.03f, 1.15f);
                    break;
                case FighterVisualState.LightPunch:
                    StrikePunch(strike, false);
                    break;
                case FighterVisualState.HeavyPunch:
                    StrikePunch(strike, true);
                    break;
                case FighterVisualState.LightKick:
                    StrikeKick(strike, false);
                    break;
                case FighterVisualState.HeavyKick:
                    StrikeKick(strike, true);
                    break;
                case FighterVisualState.Special:
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -18f + strike * 25f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.30f, 1.92f), -5f + strike * 12f, -6f);
                    SetArm(rearUpperArm, rearForearm, rearHand, new Vector2(-0.30f, 1.91f), 160f - strike * 28f, -10f);
                    energyCore.Pivot.localPosition = new Vector3(1.12f + strike * 0.35f, 1.72f, 0f);
                    energyCore.Pivot.localRotation = Quaternion.Euler(0f, 0f, tick * 12f);
                    energyCore.SetVisualScale(0.55f + strike * 0.75f, 0.55f + strike * 0.75f);
                    energyCore.SetVisible(true);
                    break;
                case FighterVisualState.Impact:
                    rig.localPosition = new Vector3(-0.10f + strike * 0.25f, -0.08f, 0f);
                    torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, -28f + strike * 34f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.32f, 1.90f), -32f + strike * 24f, 4f);
                    attackArc.Pivot.localPosition = new Vector3(0.85f, 1.18f, 0f);
                    attackArc.Pivot.localRotation = Quaternion.Euler(0f, 0f, -20f + actionProgress * 100f);
                    attackArc.SetVisible(strike > 0.25f);
                    break;
                case FighterVisualState.Hit:
                    rig.localPosition = new Vector3(-0.12f, 0.02f, 0f);
                    rig.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    head.Pivot.localRotation = Quaternion.Euler(0f, 0f, 18f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.30f, 1.9f), 122f, 18f);
                    break;
                case FighterVisualState.Stun:
                    rig.localRotation = Quaternion.Euler(0f, 0f, cycle * 7f);
                    head.Pivot.localRotation = Quaternion.Euler(0f, 0f, -cycle * 11f);
                    energyCore.Pivot.localPosition = new Vector3(cycle * 0.2f, 2.88f, 0f);
                    energyCore.SetVisualScale(0.28f, 0.28f);
                    energyCore.SetVisible(true);
                    break;
                case FighterVisualState.Knockdown:
                    rig.localPosition = new Vector3(-0.08f, 0.35f, 0f);
                    rig.localRotation = Quaternion.Euler(0f, 0f, 78f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.30f, 1.9f), 145f, 22f);
                    SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.18f, 0.98f), -48f, -40f, 5f);
                    break;
                case FighterVisualState.Finish:
                    rig.localPosition = new Vector3(0f, Mathf.Abs(cycle) * 0.04f, 0f);
                    SetArm(rearUpperArm, rearForearm, rearHand, new Vector2(-0.30f, 1.91f), 82f, 22f);
                    SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.30f, 1.91f), 98f, -22f);
                    energyCore.Pivot.localPosition = new Vector3(0f, 3.02f, 0f);
                    energyCore.Pivot.localRotation = Quaternion.Euler(0f, 0f, tick * 8f);
                    energyCore.SetVisualScale(0.72f + cycle * 0.06f, 0.72f + cycle * 0.06f);
                    energyCore.SetVisible(true);
                    break;
            }
            ApplyPaintedPose(tick, strike);
            SyncAttachedDetails();
        }

        private void StrikePunch(float strike, bool heavy)
        {
            torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, heavy ? -22f + strike * 29f : -7f + strike * 11f);
            SetArm(frontUpperArm, frontForearm, frontHand, new Vector2(0.32f, 1.91f), Mathf.Lerp(-20f, heavy ? 8f : 2f, strike), Mathf.Lerp(68f, 0f, strike));
            if (heavy) rearUpperArm.Pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-48f, 135f, strike));
            attackArc.Pivot.localPosition = new Vector3(1.05f, heavy ? 1.84f : 1.72f, 0f);
            attackArc.Pivot.localRotation = Quaternion.Euler(0f, 0f, heavy ? 25f : 0f);
            attackArc.SetVisualScale(heavy ? 1.0f : 0.62f, heavy ? 1.0f : 0.62f);
            attackArc.SetVisible(strike > 0.62f);
        }

        private void StrikeKick(float strike, bool heavy)
        {
            rig.localPosition = new Vector3(0f, heavy ? 0.18f * strike : 0f, 0f);
            torso.Pivot.localRotation = Quaternion.Euler(0f, 0f, heavy ? 14f : -10f);
            SetLeg(frontThigh, frontCalf, frontFoot, new Vector2(0.20f, 0.98f), Mathf.Lerp(-72f, heavy ? 28f : -8f, strike), Mathf.Lerp(-23f, heavy ? -18f : 4f, strike), 4f);
            attackArc.Pivot.localPosition = new Vector3(0.86f, heavy ? 1.22f : 0.72f, 0f);
            attackArc.Pivot.localRotation = Quaternion.Euler(0f, 0f, heavy ? 62f : 20f);
            attackArc.SetVisualScale(heavy ? 1.12f : 0.75f, heavy ? 1.12f : 0.75f);
            attackArc.SetVisible(strike > 0.58f);
        }

        private void ApplyFlash(bool flash)
        {
            torso.SetColor(flash ? Color.white : primary);
            torsoPanel.SetColor(flash ? Color.white : Color.Lerp(primary, neutral, 0.42f));
            shoulderMass.SetColor(flash ? Color.white : fabric);
            waistMass.SetColor(flash ? Color.white : primary);
            head.SetColor(flash ? Color.white : skin);
            jawShade.SetColor(flash ? Color.white : Color.Lerp(skin, NeonArtFactory.Ink, 0.23f));
            frontUpperArm.SetColor(flash ? Color.white : fabric);
            rearUpperArm.SetColor(flash ? Color.white : fabric);
            frontForearm.SetColor(flash ? Color.white : primary);
            rearForearm.SetColor(flash ? Color.white : primary);
            if (paintedFighter != null) paintedFighter.color = flash ? Color.white : paintedTint;
        }

        private void HideCutoutArtwork()
        {
            SpriteRenderer[] renderers = rig.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
        }

        private void ApplyPaintedPose(int tick, float strike)
        {
            if (paintedFighter == null) return;
            Transform visual = paintedFighter.transform;
            float cycle = Mathf.Sin((tick % 120) * Mathf.PI / 60f);
            switch (CurrentState)
            {
                case FighterVisualState.Idle:
                    visual.localScale = new Vector3(1f + cycle * 0.008f, 1f - cycle * 0.008f, 1f);
                    visual.localPosition = new Vector3(0f, Mathf.Abs(cycle) * 0.018f, 0f);
                    break;
                case FighterVisualState.Advance:
                    visual.localRotation = Quaternion.Euler(0f, 0f, -3.5f);
                    visual.localPosition = new Vector3(0.08f, Mathf.Abs(cycle) * 0.035f, 0f);
                    break;
                case FighterVisualState.Retreat:
                    visual.localRotation = Quaternion.Euler(0f, 0f, 4.5f);
                    visual.localPosition = new Vector3(-0.06f, Mathf.Abs(cycle) * 0.025f, 0f);
                    break;
                case FighterVisualState.JumpRise:
                    visual.localRotation = Quaternion.Euler(0f, 0f, -7f);
                    visual.localScale = new Vector3(0.96f, 1.04f, 1f);
                    break;
                case FighterVisualState.JumpFall:
                    visual.localRotation = Quaternion.Euler(0f, 0f, 6f);
                    visual.localScale = new Vector3(1.04f, 0.97f, 1f);
                    break;
                case FighterVisualState.Crouch:
                    visual.localPosition = new Vector3(0.10f, -0.12f, 0f);
                    visual.localScale = new Vector3(1.10f, 0.78f, 1f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, -8f);
                    break;
                case FighterVisualState.Guard:
                    visual.localPosition = new Vector3(-0.06f, 0f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, 5f);
                    visual.localScale = new Vector3(0.98f, 1.02f, 1f);
                    break;
                case FighterVisualState.LightPunch:
                case FighterVisualState.HeavyPunch:
                    visual.localPosition = new Vector3(strike * 0.20f, 0f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, -strike * 8f);
                    visual.localScale = new Vector3(1f + strike * 0.06f, 1f - strike * 0.025f, 1f);
                    break;
                case FighterVisualState.LightKick:
                case FighterVisualState.HeavyKick:
                    visual.localPosition = new Vector3(strike * 0.13f, strike * 0.07f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, strike * 10f);
                    visual.localScale = new Vector3(1.04f, 0.96f, 1f);
                    break;
                case FighterVisualState.Special:
                    visual.localPosition = new Vector3(-0.06f + strike * 0.18f, strike * 0.04f, 0f);
                    visual.localScale = Vector3.one * (1f + strike * 0.06f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, -6f + strike * 12f);
                    break;
                case FighterVisualState.Impact:
                    visual.localPosition = new Vector3(strike * 0.25f, -0.04f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, -14f + strike * 20f);
                    visual.localScale = new Vector3(1f + strike * 0.10f, 0.98f, 1f);
                    break;
                case FighterVisualState.Hit:
                    visual.localPosition = new Vector3(-0.16f, 0.02f, 0f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, 13f);
                    break;
                case FighterVisualState.Stun:
                    visual.localRotation = Quaternion.Euler(0f, 0f, cycle * 6f);
                    break;
                case FighterVisualState.Knockdown:
                    visual.localPosition = new Vector3(0f, 0.08f, 0f);
                    break;
                case FighterVisualState.Finish:
                    visual.localPosition = new Vector3(0f, Mathf.Abs(cycle) * 0.05f, 0f);
                    visual.localScale = Vector3.one * 1.05f;
                    break;
            }
        }

        private ArtPiece Limb(string name, Transform parent, Color color, int order, float length, float thickness)
        {
            return NeonArtFactory.CreatePiece(name, parent, NeonShape.Capsule, color, order, new Vector2(length, thickness), new Vector2(length * 0.5f, 0f));
        }

        private void SetArm(ArtPiece upper, ArtPiece lower, ArtPiece hand, Vector2 shoulder, float upperRotation, float elbowRotation)
        {
            upper.Pivot.localPosition = shoulder;
            upper.Pivot.localRotation = Quaternion.Euler(0f, 0f, upperRotation);
            lower.Pivot.localPosition = new Vector3(upperArmLength, 0f, 0f);
            lower.Pivot.localRotation = Quaternion.Euler(0f, 0f, elbowRotation);
            hand.Pivot.localPosition = new Vector3(forearmLength, 0f, 0f);
            hand.Pivot.localRotation = Quaternion.identity;
        }

        private void SetLeg(ArtPiece upper, ArtPiece lower, ArtPiece foot, Vector2 hip, float upperRotation, float kneeRotation, float footRotation)
        {
            upper.Pivot.localPosition = hip;
            upper.Pivot.localRotation = Quaternion.Euler(0f, 0f, upperRotation);
            lower.Pivot.localPosition = new Vector3(thighLength, 0f, 0f);
            lower.Pivot.localRotation = Quaternion.Euler(0f, 0f, kneeRotation);
            foot.Pivot.localPosition = new Vector3(calfLength, 0f, 0f);
            foot.Pivot.localRotation = Quaternion.Euler(0f, 0f, 90f - upperRotation - kneeRotation + footRotation);
        }

        private void AddHair(FighterHairStyle style, int order)
        {
            switch (style)
            {
                case FighterHairStyle.Braids:
                    Hair(NeonShape.Disc, new Vector2(0.66f, 0.66f), new Vector2(-0.18f, 2.64f), order);
                    Hair(NeonShape.Capsule, new Vector2(0.86f, 0.18f), new Vector2(-0.46f, 2.35f), order + 1, -72f);
                    Hair(NeonShape.Capsule, new Vector2(0.74f, 0.16f), new Vector2(-0.56f, 2.17f), order + 1, -82f);
                    break;
                case FighterHairStyle.Wild:
                    Hair(NeonShape.Triangle, new Vector2(0.54f, 0.64f), new Vector2(-0.20f, 2.78f), order, -26f);
                    Hair(NeonShape.Triangle, new Vector2(0.50f, 0.68f), new Vector2(0.08f, 2.82f), order + 1, 5f);
                    Hair(NeonShape.Triangle, new Vector2(0.46f, 0.61f), new Vector2(0.31f, 2.72f), order + 2, 24f);
                    break;
                case FighterHairStyle.Bob:
                case FighterHairStyle.BladeBob:
                    Hair(NeonShape.Hexagon, new Vector2(0.78f, 0.70f), new Vector2(-0.08f, 2.59f), order);
                    if (style == FighterHairStyle.BladeBob) Hair(NeonShape.Triangle, new Vector2(0.55f, 0.52f), new Vector2(-0.37f, 2.39f), order + 1, -28f);
                    break;
                case FighterHairStyle.Crest:
                    Hair(NeonShape.Triangle, new Vector2(0.52f, 0.82f), new Vector2(-0.05f, 2.88f), order, -8f);
                    break;
                case FighterHairStyle.Ponytail:
                    Hair(NeonShape.Disc, new Vector2(0.70f, 0.62f), new Vector2(-0.10f, 2.63f), order);
                    Hair(NeonShape.Capsule, new Vector2(0.95f, 0.20f), new Vector2(-0.48f, 2.58f), order + 1, -28f);
                    break;
                case FighterHairStyle.Topknot:
                    Hair(NeonShape.Crescent, new Vector2(0.72f, 0.72f), new Vector2(-0.05f, 2.62f), order);
                    Hair(NeonShape.Disc, new Vector2(0.28f, 0.28f), new Vector2(-0.03f, 2.98f), order + 1);
                    break;
                case FighterHairStyle.FoxTails:
                    Hair(NeonShape.Disc, new Vector2(0.68f, 0.60f), new Vector2(-0.06f, 2.62f), order);
                    Hair(NeonShape.Triangle, new Vector2(0.30f, 0.46f), new Vector2(-0.23f, 2.96f), order + 1, -16f);
                    Hair(NeonShape.Triangle, new Vector2(0.30f, 0.46f), new Vector2(0.20f, 2.96f), order + 1, 18f);
                    break;
                case FighterHairStyle.Flow:
                    Hair(NeonShape.Crescent, new Vector2(0.88f, 0.90f), new Vector2(-0.12f, 2.57f), order, 14f);
                    break;
                default:
                    Hair(NeonShape.Disc, new Vector2(0.64f, 0.48f), new Vector2(-0.08f, 2.68f), order);
                    Hair(NeonShape.Triangle, new Vector2(0.42f, 0.55f), new Vector2(0.18f, 2.83f), order + 1, 18f);
                    Hair(NeonShape.Triangle, new Vector2(0.36f, 0.50f), new Vector2(-0.16f, 2.84f), order + 1, -14f);
                    break;
            }
        }

        private void Hair(NeonShape shape, Vector2 size, Vector2 position, int order, float rotation = 0f)
        {
            ArtPiece piece = NeonArtFactory.CreatePiece("Hair " + hair.Count, rig, shape, hairColor, order, size, Vector2.zero);
            piece.Pivot.localPosition = position;
            piece.Pivot.localRotation = Quaternion.Euler(0f, 0f, rotation);
            hair.Add(piece);
        }

        private void PlaceHair()
        {
            // Hair pieces are authored in head-space coordinates and share the head's deterministic recoil.
            Vector3 recoil = head.Pivot.localPosition - new Vector3(0.02f, 2.45f * heightScale, 0f);
            for (int i = 0; i < hair.Count; i++) hair[i].Visual.localPosition = recoil;
        }

        private void AddGarmentBack(FighterOutfitStyle style, int order, string cut)
        {
            if (style == FighterOutfitStyle.LongCoat || style == FighterOutfitStyle.CounterRobe || style == FighterOutfitStyle.TideRobe)
            {
                Accessory("Rear Coat Tail", NeonShape.Tapered, fabric, order, new Vector2(0.52f, cut == "heatwave" ? 0.72f : 1.25f), new Vector2(-0.28f, 0.47f), 10f);
                Accessory("Front Coat Tail", NeonShape.Tapered, primary, order + 1, new Vector2(0.48f, cut == "heatwave" ? 0.68f : 1.18f), new Vector2(0.30f, 0.49f), -12f);
            }
            if (style == FighterOutfitStyle.HeavyArmor)
            {
                Accessory("Armor Back Plate", NeonShape.Hexagon, fabric, order, new Vector2(1.25f, 1.36f), new Vector2(-0.12f, 1.50f), -4f);
            }
        }

        private void AddGarmentFront(FighterOutfitStyle style, int order, string cut)
        {
            if (style == FighterOutfitStyle.GrapplerWrap)
                Accessory("Champion Ring", NeonShape.Ring, secondary, order, new Vector2(0.62f, 0.62f), new Vector2(0.08f, 1.72f), 0f);
            if (style == FighterOutfitStyle.FlightVest)
                Accessory("Flight Fin", NeonShape.Triangle, secondary, order, new Vector2(0.42f, 0.62f), new Vector2(-0.46f, 1.78f), -24f);
            if (style == FighterOutfitStyle.Bomber || style == FighterOutfitStyle.StreetJacket)
            {
                Accessory("Jacket Collar", NeonShape.Chevron, neutral, order, new Vector2(0.68f, 0.38f), new Vector2(0.02f, 2.00f), 180f);
            }
            if (style == FighterOutfitStyle.HeavyArmor)
            {
                Accessory("Front Shoulder Guard", NeonShape.Hexagon, secondary, order + 1, new Vector2(0.58f, 0.40f), new Vector2(0.48f, 1.94f), -8f);
                Accessory("Rear Shoulder Guard", NeonShape.Hexagon, primary, order, new Vector2(0.58f, 0.40f), new Vector2(-0.47f, 1.94f), 8f);
            }
            if (style == FighterOutfitStyle.PrismJacket)
            {
                Accessory("Prism Lapel A", NeonShape.Triangle, primary, order, new Vector2(0.40f, 0.60f), new Vector2(-0.26f, 1.57f), -16f);
                Accessory("Prism Lapel B", NeonShape.Triangle, secondary, order + 1, new Vector2(0.38f, 0.58f), new Vector2(0.28f, 1.58f), 18f);
            }
            if (cut == "heatwave")
                Accessory("Heatwave Waist Flash", NeonShape.Chevron, secondary, order + 2, new Vector2(0.82f, 0.30f), new Vector2(0f, 1.05f), 0f);
        }

        private void Accessory(string name, NeonShape shape, Color color, int order, Vector2 size, Vector2 position, float rotation)
        {
            ArtPiece piece = NeonArtFactory.CreatePiece(name, rig, shape, color, order, size, Vector2.zero);
            piece.Pivot.localPosition = position;
            piece.Pivot.localRotation = Quaternion.Euler(0f, 0f, rotation);
            accessories.Add(piece);
        }

        private void PlaceAccessories()
        {
            // Accessories are intentionally authored around the neutral pose. Their
            // parent rig follows hit/knockdown/finish states as one readable silhouette.
        }

        private void SyncAttachedDetails()
        {
            torsoPanel.Pivot.localPosition = torso.Pivot.localPosition;
            torsoPanel.Pivot.localRotation = torso.Pivot.localRotation;
            chestCircuit.Pivot.localRotation = torso.Pivot.localRotation;
            shoulderMass.Pivot.localRotation = torso.Pivot.localRotation;
            Vector3 headDelta = head.Pivot.localPosition - new Vector3(0.02f, 2.45f * heightScale, 0f);
            jawShade.Pivot.localPosition = new Vector3(0.10f, 2.30f * heightScale, 0f) + headDelta;
            jawShade.Pivot.localRotation = head.Pivot.localRotation;
            visor.Pivot.localPosition = new Vector3(0.21f, 2.49f * heightScale, 0f) + headDelta;
            visor.Pivot.localRotation = head.Pivot.localRotation;
            faceLight.Pivot.localPosition = new Vector3(0.31f, 2.50f * heightScale, 0f) + headDelta;
            faceLight.Pivot.localRotation = head.Pivot.localRotation;
            for (int i = 0; i < hair.Count; i++) hair[i].Visual.localPosition = headDelta;
        }

        private static NeonShape EnergyShape(FighterEnergyStyle style)
        {
            switch (style)
            {
                case FighterEnergyStyle.Lightning: return NeonShape.Chevron;
                case FighterEnergyStyle.Rift:
                case FighterEnergyStyle.Slash:
                case FighterEnergyStyle.Wave: return NeonShape.Crescent;
                case FighterEnergyStyle.Comet:
                case FighterEnergyStyle.Meteor: return NeonShape.Disc;
                case FighterEnergyStyle.Prism: return NeonShape.Diamond;
                case FighterEnergyStyle.Quake: return NeonShape.Hexagon;
                default: return NeonShape.Ring;
            }
        }
    }
}
