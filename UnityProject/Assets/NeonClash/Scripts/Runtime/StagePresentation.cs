using System.Collections.Generic;
using UnityEngine;

namespace NeonClash
{
    /// <summary>Builds ten distinct, original vector arenas from the migrated stage motif data.</summary>
    public static class StagePresentation
    {
        public const string RootName = "Stage - Production Vector Arena";

        public static GameObject Build(StageDefinition stage)
        {
            Color primary = stage != null ? stage.PrimaryColor : new Color(0.15f, 0.96f, 1f);
            Color accent = stage != null ? stage.AccentColor : new Color(1f, 0.18f, 0.73f);
            string motif = stage != null ? stage.Motif : "skyline";
            GameObject rootObject = new GameObject(RootName);
            Transform root = rootObject.transform;
            StageAmbience ambience = rootObject.AddComponent<StageAmbience>();

            ArenaBackdrop.Create(root, primary, accent);
            BuildSky(root, primary, accent);
            switch (motif)
            {
                case "rail": BuildRail(root, primary, accent, ambience); break;
                case "market": BuildMarket(root, primary, accent, ambience); break;
                case "forge": BuildForge(root, primary, accent, ambience); break;
                case "club": BuildClub(root, primary, accent, ambience); break;
                case "court": BuildCourt(root, primary, accent, ambience); break;
                case "plaza": BuildPlaza(root, primary, accent, ambience); break;
                case "steppe": BuildSteppe(root, primary, accent, ambience); break;
                case "metro": BuildMetro(root, primary, accent, ambience); break;
                case "harbour": BuildHarbour(root, primary, accent, ambience); break;
                default: BuildSkyline(root, primary, accent, ambience); break;
            }
            BuildFloor(root, primary, accent);
            return rootObject;
        }

        private static void BuildSky(Transform root, Color primary, Color accent)
        {
            Color top = new Color(primary.r, primary.g, primary.b, 0.025f);
            Color bottom = new Color(accent.r, accent.g, accent.b, 0.035f);
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                Flat("Sky Band " + i, root, NeonShape.Block, Color.Lerp(bottom, top, t), -50 + i, new Vector3(0f, -0.8f + i * 1.75f, 2f), new Vector2(19f, 1.82f));
            }
            Flat("Atmosphere Disc", root, NeonShape.Disc, new Color(primary.r, primary.g, primary.b, 0.12f), -42, new Vector3(5.8f, 3.4f, 1.8f), new Vector2(3.8f, 3.8f));
            for (int i = 0; i < 18; i++)
            {
                float x = -8.4f + (i * 47 % 168) * 0.1f;
                float y = -0.1f + (i * 31 % 60) * 0.1f;
                Flat("Sky Spark " + i, root, NeonShape.Diamond, i % 3 == 0 ? accent : primary, -40, new Vector3(x, y, 1.7f), new Vector2(0.035f, 0.035f));
            }
        }

        private static void BuildSkyline(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            for (int i = 0; i < 13; i++)
            {
                float x = -8.5f + i * 1.42f;
                float height = 1.5f + (i * 7 % 6) * 0.42f;
                Color body = Color.Lerp(NeonArtFactory.Ink, i % 2 == 0 ? primary : accent, 0.18f);
                Flat("Crossing Tower " + i, root, NeonShape.Tapered, body, -28, new Vector3(x, -2.88f + height * 0.5f, 1.2f), new Vector2(1.12f, height));
                for (int w = 0; w < 3; w++)
                {
                    SpriteRenderer light = Flat("Tower Light " + i + "-" + w, root, NeonShape.Block, w % 2 == 0 ? primary : accent, -26,
                        new Vector3(x - 0.30f + w * 0.30f, -2.45f + (w + i) % 4 * 0.48f, 1.1f), new Vector2(0.11f, 0.20f));
                    ambience.AddPulse(light, i * 0.31f + w);
                }
            }
            for (int i = 0; i < 7; i++)
                Flat("Crosswalk Light " + i, root, NeonShape.Tapered, new Color(primary.r, primary.g, primary.b, 0.34f), -3, new Vector3(-5.8f + i * 1.9f, -3.48f, 0.4f), new Vector2(0.72f, 0.16f));
        }

        private static void BuildRail(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            Flat("Hyperrail Body", root, NeonShape.Capsule, Color.Lerp(NeonArtFactory.Ink, primary, 0.28f), -26, new Vector3(0f, -1.20f, 1f), new Vector2(17.5f, 2.2f));
            Flat("Hyperrail Stripe", root, NeonShape.Block, accent, -23, new Vector3(0f, -1.43f, 0.9f), new Vector2(17.0f, 0.12f));
            for (int i = 0; i < 12; i++)
            {
                SpriteRenderer window = Flat("Car Window " + i, root, NeonShape.Tapered, Color.Lerp(primary, Color.white, 0.38f), -22,
                    new Vector3(-7.6f + i * 1.38f, -0.92f, 0.8f), new Vector2(0.88f, 0.72f));
                ambience.AddPulse(window, i * 0.42f);
            }
            Flat("Upper Rail", root, NeonShape.Block, primary, -18, new Vector3(0f, 2.28f, 1.2f), new Vector2(18f, 0.16f));
            for (int i = 0; i < 7; i++) Flat("Rail Pylon " + i, root, NeonShape.Tapered, Color.Lerp(NeonArtFactory.Ink, accent, 0.24f), -19, new Vector3(-7.5f + i * 2.5f, 0.45f, 1.1f), new Vector2(0.25f, 3.7f));
        }

        private static void BuildMarket(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            for (int i = 0; i < 7; i++)
            {
                float x = -7.4f + i * 2.45f;
                Flat("Market Stall " + i, root, NeonShape.Block, Color.Lerp(NeonArtFactory.Ink, i % 2 == 0 ? primary : accent, 0.25f), -27, new Vector3(x, -1.95f, 1.1f), new Vector2(2.05f, 1.65f));
                Flat("Market Awning " + i, root, NeonShape.Triangle, i % 2 == 0 ? primary : accent, -23, new Vector3(x, -0.76f, 1f), new Vector2(2.20f, 0.75f));
                SpriteRenderer lantern = Flat("Market Lantern " + i, root, NeonShape.Disc, i % 2 == 0 ? accent : primary, -20, new Vector3(x + 0.75f, 0.34f + (i % 2) * 0.35f, 0.8f), new Vector2(0.27f, 0.34f));
                ambience.AddPulse(lantern, i * 0.7f);
            }
            for (int i = 0; i < 15; i++) Flat("Rain " + i, root, NeonShape.Capsule, new Color(0.65f, 0.85f, 1f, 0.24f), -18, new Vector3(-8f + i * 1.13f, -0.2f + (i * 19 % 50) * 0.1f, 0.7f), new Vector2(0.05f, 0.60f));
        }

        private static void BuildForge(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = -6.6f + i * 4.4f;
                Flat("Foundry Furnace " + i, root, NeonShape.Hexagon, Color.Lerp(NeonArtFactory.Ink, primary, 0.23f), -28, new Vector3(x, -1.12f, 1.1f), new Vector2(2.6f, 3.25f));
                SpriteRenderer core = Flat("Furnace Core " + i, root, NeonShape.Ring, accent, -23, new Vector3(x, -1.05f, 0.9f), new Vector2(1.22f, 1.22f));
                ambience.AddPulse(core, i * 0.6f);
                Flat("Furnace Stack " + i, root, NeonShape.Block, Color.Lerp(NeonArtFactory.Ink, accent, 0.16f), -29, new Vector3(x, 1.32f, 1.2f), new Vector2(0.62f, 2.0f));
            }
            for (int i = 0; i < 5; i++) Flat("Molten Channel " + i, root, NeonShape.Chevron, primary, -5, new Vector3(-6f + i * 3f, -3.26f, 0.5f), new Vector2(1.1f, 0.28f));
        }

        private static void BuildClub(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            Flat("Void Portal", root, NeonShape.Ring, new Color(primary.r, primary.g, primary.b, 0.48f), -31, new Vector3(0f, 0.35f, 1.2f), new Vector2(6.4f, 6.4f));
            for (int i = 0; i < 21; i++)
            {
                float h = 0.55f + (i * 13 % 8) * 0.32f;
                SpriteRenderer equalizer = Flat("Equalizer " + i, root, NeonShape.Block, i % 3 == 0 ? accent : primary, -22,
                    new Vector3(-8f + i * 0.8f, -2.85f + h * 0.5f, 0.8f), new Vector2(0.42f, h));
                ambience.AddPulse(equalizer, i * 0.24f);
            }
            for (int i = 0; i < 4; i++) Flat("Laser " + i, root, NeonShape.Capsule, new Color(accent.r, accent.g, accent.b, 0.26f), -18, new Vector3(-5f + i * 3.4f, 1.0f, 0.5f), new Vector2(8f, 0.06f));
        }

        private static void BuildCourt(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            SpriteRenderer sun = Flat("Sky Court Sun", root, NeonShape.Disc, new Color(primary.r, primary.g, primary.b, 0.62f), -34, new Vector3(5.6f, 2.6f, 1.3f), new Vector2(3.2f, 3.2f));
            ambience.AddPulse(sun, 0f);
            for (int i = 0; i < 6; i++)
            {
                Flat("Rooftop Tower " + i, root, NeonShape.Tapered, Color.Lerp(NeonArtFactory.Ink, accent, 0.18f), -27, new Vector3(-7f + i * 2.8f, -1.4f, 1f), new Vector2(1.4f, 3.2f + (i % 3) * 0.7f));
                SpriteRenderer beacon = Flat("Rooftop Beacon " + i, root, NeonShape.Diamond, i % 2 == 0 ? primary : accent, -22, new Vector3(-7f + i * 2.8f, 0.42f + (i % 3) * 0.35f, 0.8f), new Vector2(0.18f, 0.18f));
                ambience.AddPulse(beacon, i * 0.7f);
            }
            for (int i = 0; i < 9; i++) Flat("Court Fence Vertical " + i, root, NeonShape.Block, new Color(0.75f, 0.94f, 1f, 0.22f), -17, new Vector3(-8f + i * 2f, -1.6f, 0.6f), new Vector2(0.05f, 2.6f));
            Flat("Court Fence", root, NeonShape.Block, new Color(primary.r, primary.g, primary.b, 0.32f), -16, new Vector3(0f, -1.55f, 0.5f), new Vector2(18f, 0.08f));
        }

        private static void BuildPlaza(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            Flat("Solar Plaza Halo", root, NeonShape.Ring, new Color(accent.r, accent.g, accent.b, 0.36f), -33, new Vector3(0f, 1.15f, 1.1f), new Vector2(5.2f, 5.2f));
            for (int i = 0; i < 5; i++)
            {
                float x = -7.2f + i * 3.6f;
                Flat("Plaza Arch " + i, root, NeonShape.Crescent, Color.Lerp(NeonArtFactory.Ink, primary, 0.25f), -25, new Vector3(x, -1.18f, 0.8f), new Vector2(2.2f, 3.3f));
            }
            for (int i = 0; i < 12; i++)
            {
                SpriteRenderer flag = Flat("Festival Pennant " + i, root, NeonShape.Triangle, i % 2 == 0 ? primary : accent, -18,
                    new Vector3(-7.8f + i * 1.42f, 2.45f + Mathf.Sin(i * 0.7f) * 0.28f, 0.5f), new Vector2(0.46f, 0.56f));
                ambience.AddSway(flag.transform, i * 0.35f);
            }
        }

        private static void BuildSteppe(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            for (int i = 0; i < 7; i++) Flat("Dawn Mountain " + i, root, NeonShape.Triangle, Color.Lerp(NeonArtFactory.Ink, i % 2 == 0 ? primary : accent, 0.18f), -31 + i % 2, new Vector3(-8f + i * 2.7f, -0.9f, 1.1f), new Vector2(4.5f, 4.1f - (i % 3) * 0.55f));
            Flat("Shrine Crossbeam", root, NeonShape.Block, primary, -19, new Vector3(0f, 1.35f, 0.6f), new Vector2(5.6f, 0.28f));
            Flat("Shrine Beam Accent", root, NeonShape.Tapered, accent, -18, new Vector3(0f, 1.67f, 0.5f), new Vector2(4.8f, 0.30f));
            Flat("Shrine Left Pillar", root, NeonShape.Tapered, Color.Lerp(NeonArtFactory.Ink, primary, 0.4f), -20, new Vector3(-2.0f, -0.55f, 0.7f), new Vector2(0.42f, 4.0f));
            Flat("Shrine Right Pillar", root, NeonShape.Tapered, Color.Lerp(NeonArtFactory.Ink, primary, 0.4f), -20, new Vector3(2.0f, -0.55f, 0.7f), new Vector2(0.42f, 4.0f));
            for (int i = 0; i < 8; i++)
            {
                SpriteRenderer ribbon = Flat("Wind Ribbon " + i, root, NeonShape.Capsule, new Color(accent.r, accent.g, accent.b, 0.48f), -16, new Vector3(-7.2f + i * 2f, 2.6f - (i % 3) * 0.4f, 0.4f), new Vector2(1.1f, 0.06f));
                ambience.AddSway(ribbon.transform, i * 0.5f);
            }
        }

        private static void BuildMetro(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            Flat("Metro Tunnel", root, NeonShape.Ring, Color.Lerp(NeonArtFactory.Ink, primary, 0.28f), -31, new Vector3(0f, -0.15f, 1.1f), new Vector2(15.8f, 9.8f));
            Flat("Platform Fascia", root, NeonShape.Block, Color.Lerp(NeonArtFactory.Ink, accent, 0.22f), -24, new Vector3(0f, -2.30f, 0.9f), new Vector2(18f, 1.15f));
            for (int i = 0; i < 9; i++)
            {
                SpriteRenderer prism = Flat("Metro Prism " + i, root, NeonShape.Diamond, i % 2 == 0 ? primary : accent, -19,
                    new Vector3(-7.6f + i * 1.9f, -0.42f + (i % 2) * 0.45f, 0.6f), new Vector2(0.55f, 0.55f));
                ambience.AddPulse(prism, i * 0.45f);
            }
            Flat("Platform Safety Line", root, NeonShape.Block, primary, -4, new Vector3(0f, -3.17f, 0.4f), new Vector2(18f, 0.10f));
        }

        private static void BuildHarbour(Transform root, Color primary, Color accent, StageAmbience ambience)
        {
            Flat("Harbour Moon", root, NeonShape.Crescent, new Color(primary.r, primary.g, primary.b, 0.55f), -35, new Vector3(5.4f, 3.1f, 1.4f), new Vector2(2.6f, 2.6f));
            Flat("Opera Sail A", root, NeonShape.Triangle, Color.Lerp(NeonArtFactory.OffWhite, primary, 0.30f), -27, new Vector3(-2.4f, -0.45f, 1f), new Vector2(4.4f, 4.2f));
            Flat("Opera Sail B", root, NeonShape.Triangle, Color.Lerp(NeonArtFactory.OffWhite, accent, 0.28f), -26, new Vector3(0.6f, -0.72f, 0.9f), new Vector2(4.8f, 3.7f));
            Flat("Harbour Bridge", root, NeonShape.Crescent, Color.Lerp(NeonArtFactory.Ink, primary, 0.42f), -23, new Vector3(4.9f, -0.78f, 0.8f), new Vector2(7.2f, 3.4f));
            for (int i = 0; i < 12; i++)
            {
                SpriteRenderer reflection = Flat("Water Reflection " + i, root, NeonShape.Capsule, i % 2 == 0 ? primary : accent, -12,
                    new Vector3(-7.5f + i * 1.35f, -2.82f - (i % 3) * 0.12f, 0.5f), new Vector2(0.72f + (i % 4) * 0.22f, 0.045f));
                ambience.AddPulse(reflection, i * 0.28f);
            }
        }

        private static void BuildFloor(Transform root, Color primary, Color accent)
        {
            Flat("Arena Floor", root, NeonShape.Block, new Color(0.035f, 0.055f, 0.105f), -2, new Vector3(0f, -3.62f, 0.2f), new Vector2(19f, 1.02f));
            Flat("Arena Edge Light", root, NeonShape.Block, primary, -1, new Vector3(0f, -3.10f, 0.1f), new Vector2(19f, 0.075f));
            for (int i = 0; i < 8; i++) Flat("Floor Circuit " + i, root, NeonShape.Chevron, new Color(accent.r, accent.g, accent.b, 0.22f), 0, new Vector3(-7f + i * 2f, -3.50f, 0f), new Vector2(0.72f, 0.22f));
        }

        private static SpriteRenderer Flat(string name, Transform parent, NeonShape shape, Color color, int order, Vector3 position, Vector2 size)
        {
            return NeonArtFactory.CreateFlat(name, parent, shape, color, order, position, size);
        }
    }

    public sealed class StageAmbience : MonoBehaviour
    {
        private readonly List<SpriteRenderer> pulseRenderers = new List<SpriteRenderer>();
        private readonly List<float> pulseOffsets = new List<float>();
        private readonly List<Transform> swayTransforms = new List<Transform>();
        private readonly List<float> swayOffsets = new List<float>();
        private readonly List<Color> baseColors = new List<Color>();

        public int AnimatedElementCount { get { return pulseRenderers.Count + swayTransforms.Count; } }

        public void AddPulse(SpriteRenderer renderer, float offset)
        {
            pulseRenderers.Add(renderer); pulseOffsets.Add(offset); baseColors.Add(renderer.color);
        }

        public void AddSway(Transform item, float offset)
        {
            swayTransforms.Add(item); swayOffsets.Add(offset);
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            for (int i = 0; i < pulseRenderers.Count; i++)
            {
                Color color = baseColors[i];
                color.a *= 0.66f + Mathf.Sin(time * 2.6f + pulseOffsets[i]) * 0.22f;
                pulseRenderers[i].color = color;
            }
            for (int i = 0; i < swayTransforms.Count; i++)
                swayTransforms[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.8f + swayOffsets[i]) * 8f);
        }
    }
}
