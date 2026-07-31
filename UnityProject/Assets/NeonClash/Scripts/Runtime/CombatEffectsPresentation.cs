using UnityEngine;

namespace NeonClash
{
    /// <summary>
    /// Deterministic combat readability layer. It derives every slash, hit spark,
    /// projectile trail, and combo flash from simulation state; it never feeds back
    /// into the authoritative rules.
    /// </summary>
    public sealed class CombatEffectsPresentation
    {
        private const float GroundWorldY = -3.05f;
        private readonly Transform root;
        private readonly EffectLayer firstAttack;
        private readonly EffectLayer secondAttack;
        private readonly EffectLayer hitBurst;
        private readonly EffectLayer blockBurst;
        private readonly EffectLayer[] projectileTrails = new EffectLayer[DeterministicMatchSimulation.ProjectileCapacity];

        public CombatEffectsPresentation(Transform parent)
        {
            root = new GameObject("Combat Effects - Deterministic Readability").transform;
            root.SetParent(parent, false);
            firstAttack = CreateAttackLayer("P1 Attack", 190);
            secondAttack = CreateAttackLayer("P2 Attack", 191);
            hitBurst = CreateImpactLayer("Hit Spark", 230, new Color(1f, 0.88f, 0.36f, 1f));
            blockBurst = CreateImpactLayer("Guard Spark", 231, new Color(0.32f, 0.91f, 1f, 1f));
            for (int i = 0; i < projectileTrails.Length; i++)
                projectileTrails[i] = CreateTrailLayer("Projectile Trail " + i, 154);
        }

        public void Remove()
        {
            if (root != null) Object.Destroy(root.gameObject);
        }

        public void Present(DeterministicMatchState state, FighterDefinition firstDefinition, FighterDefinition secondDefinition)
        {
            PresentAttack(firstAttack, state.First, firstDefinition);
            PresentAttack(secondAttack, state.Second, secondDefinition);

            int hitAge = state.Tick - state.LastHitTick;
            if (hitAge >= 0 && hitAge <= 16)
            {
                float firstX = state.First.PositionX / 1000f;
                float secondX = state.Second.PositionX / 1000f;
                float x = Mathf.Lerp(firstX, secondX, 0.5f);
                float y = GroundWorldY + Mathf.Max(state.First.PositionY, state.Second.PositionY) / 1000f + 1.15f;
                float pulse = 1f - Mathf.Clamp01(hitAge / 16f);
                bool blocked = state.First.HurtTicks <= 8 || state.Second.HurtTicks <= 8;
                (blocked ? blockBurst : hitBurst).ShowBurst(new Vector3(x, y, 0f), pulse, state.Tick);
            }
            else
            {
                hitBurst.Hide();
                blockBurst.Hide();
            }

            for (int i = 0; i < state.Projectiles.Length && i < projectileTrails.Length; i++)
            {
                ProjectileState projectile = state.Projectiles[i];
                if (!projectile.Active)
                {
                    projectileTrails[i].Hide();
                    continue;
                }
                float x = projectile.PositionX / 1000f;
                float y = GroundWorldY + projectile.PositionY / 1000f;
                float direction = projectile.VelocityX >= 0 ? 1f : -1f;
                projectileTrails[i].ShowTrail(new Vector3(x, y, 0f), direction, state.Tick);
            }
        }

        private void PresentAttack(EffectLayer layer, FighterState fighter, FighterDefinition definition)
        {
            if (fighter.Action == CombatAction.None)
            {
                layer.Hide();
                return;
            }
            AttackSpec spec = FighterSimulation.GetAttackSpec(fighter.Action);
            if (!spec.IsActive(fighter.ActionTick))
            {
                layer.Hide();
                return;
            }
            float progress = Mathf.InverseLerp(spec.ActiveFromTick, spec.ActiveToTick, fighter.ActionTick);
            float reach = (spec.HitboxForwardOffset + spec.HitboxHalfWidth) / 1000f;
            float x = fighter.PositionX / 1000f + fighter.Facing * reach;
            float y = GroundWorldY + (fighter.PositionY + (spec.HitboxBottom + spec.HitboxTop) / 2) / 1000f;
            float scale = fighter.Action == CombatAction.Impact ? 1.35f : fighter.Action == CombatAction.Special ? 1.18f : 0.86f;
            float direction = fighter.Facing;
            Color color = definition != null ? definition.SecondaryColor : new Color(0.3f, 0.95f, 1f);
            layer.Show(new Vector3(x, y, 0f), scale, direction, color, progress);
        }

        private EffectLayer CreateAttackLayer(string name, int order)
        {
            EffectLayer layer = new EffectLayer();
            layer.primary = NeonArtFactory.CreateFlat(name + " Arc", root, NeonShape.Crescent, Color.white, order, Vector3.zero, Vector2.one);
            layer.secondary = NeonArtFactory.CreateFlat(name + " Edge", root, NeonShape.Chevron, Color.white, order + 1, Vector3.zero, Vector2.one);
            layer.spark = NeonArtFactory.CreateFlat(name + " Core", root, NeonShape.Disc, Color.white, order + 2, Vector3.zero, Vector2.one);
            layer.Hide();
            return layer;
        }

        private EffectLayer CreateImpactLayer(string name, int order, Color color)
        {
            EffectLayer layer = new EffectLayer();
            layer.primary = NeonArtFactory.CreateFlat(name + " Ring", root, NeonShape.Ring, color, order, Vector3.zero, Vector2.one);
            layer.secondary = NeonArtFactory.CreateFlat(name + " Cross", root, NeonShape.Chevron, color, order + 1, Vector3.zero, Vector2.one);
            layer.spark = NeonArtFactory.CreateFlat(name + " Core", root, NeonShape.Disc, Color.white, order + 2, Vector3.zero, Vector2.one);
            layer.Hide();
            return layer;
        }

        private EffectLayer CreateTrailLayer(string name, int order)
        {
            EffectLayer layer = new EffectLayer();
            layer.primary = NeonArtFactory.CreateFlat(name + " Body", root, NeonShape.Disc, Color.white, order, Vector3.zero, Vector2.one);
            layer.secondary = NeonArtFactory.CreateFlat(name + " Echo", root, NeonShape.Disc, Color.white, order - 1, Vector3.zero, Vector2.one);
            layer.spark = NeonArtFactory.CreateFlat(name + " Tail", root, NeonShape.Disc, Color.white, order - 2, Vector3.zero, Vector2.one);
            layer.Hide();
            return layer;
        }

        private sealed class EffectLayer
        {
            public SpriteRenderer primary;
            public SpriteRenderer secondary;
            public SpriteRenderer spark;

            public void Hide()
            {
                primary.enabled = false;
                secondary.enabled = false;
                spark.enabled = false;
            }

            public void Show(Vector3 position, float scale, float direction, Color color, float progress)
            {
                primary.enabled = secondary.enabled = spark.enabled = true;
                primary.transform.position = position;
                primary.transform.localScale = new Vector3(scale * 1.30f, scale * 1.30f, 1f);
                primary.transform.localRotation = Quaternion.Euler(0f, 0f, direction < 0 ? 180f : 0f);
                secondary.transform.position = position + new Vector3(direction * 0.12f, 0.04f, 0f);
                secondary.transform.localScale = new Vector3(scale * (0.72f + progress * 0.52f), scale * 0.58f, 1f);
                secondary.transform.localRotation = Quaternion.Euler(0f, 0f, direction < 0 ? -22f : 22f);
                spark.transform.position = position + new Vector3(direction * 0.06f, 0.02f, 0f);
                spark.transform.localScale = Vector3.one * (scale * (0.24f + progress * 0.40f));
                Color bright = color;
                bright.a = Mathf.Clamp01(0.78f + progress * 0.22f);
                primary.color = bright;
                Color edge = Color.Lerp(color, Color.white, 0.42f);
                edge.a = bright.a * 0.86f;
                secondary.color = edge;
                spark.color = new Color(1f, 0.96f, 0.74f, bright.a * 0.95f);
            }

            public void ShowBurst(Vector3 position, float pulse, int tick)
            {
                primary.enabled = secondary.enabled = spark.enabled = true;
                float spin = tick * 11f;
                primary.transform.position = position;
                primary.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
                primary.transform.localScale = Vector3.one * (0.62f + pulse * 1.55f);
                secondary.transform.position = position;
                secondary.transform.localRotation = Quaternion.Euler(0f, 0f, -spin * 0.75f);
                secondary.transform.localScale = new Vector3(1.45f, 0.42f, 1f) * (0.58f + pulse * 0.82f);
                spark.transform.position = position;
                spark.transform.localScale = Vector3.one * (0.20f + pulse * 0.46f);
                Color main = primary.color; main.a = 0.62f + pulse * 0.38f; primary.color = main;
                Color secondaryColor = this.secondary.color; secondaryColor.a = main.a * 0.90f; this.secondary.color = secondaryColor;
                Color core = spark.color; core.a = main.a; spark.color = core;
            }

            public void ShowTrail(Vector3 position, float direction, int tick)
            {
                primary.enabled = secondary.enabled = spark.enabled = true;
                primary.transform.position = position;
                secondary.transform.position = position - new Vector3(direction * 0.22f, 0f, 0f);
                spark.transform.position = position - new Vector3(direction * 0.43f, 0f, 0f);
                primary.transform.localScale = Vector3.one * 0.46f;
                secondary.transform.localScale = Vector3.one * 0.31f;
                spark.transform.localScale = Vector3.one * 0.20f;
                Color color = tick % 2 == 0 ? new Color(0.25f, 0.96f, 1f, 0.72f) : new Color(1f, 0.26f, 0.75f, 0.72f);
                primary.color = color;
                color.a *= 0.62f; secondary.color = color;
                color.a *= 0.58f; spark.color = color;
            }
        }
    }
}
