using System.Collections.Generic;
using UnityEngine;

namespace NeonClash
{
    public enum NeonShape { Block, Disc, Capsule, Diamond, Triangle, Tapered, Hexagon, Chevron, Ring, Crescent }

    /// <summary>
    /// Creates the small set of antialiased, project-owned vector silhouettes used by
    /// fighters, effects, and stages. Textures are generated deterministically at
    /// runtime, cached, and never sourced from a third-party package.
    /// </summary>
    public static class NeonArtFactory
    {
        public static readonly Color Ink = new Color(0.012f, 0.018f, 0.055f, 1f);
        public static readonly Color DeepNavy = new Color(0.025f, 0.035f, 0.09f, 1f);
        public static readonly Color OffWhite = new Color(0.88f, 0.92f, 0.96f, 1f);

        private const int TextureSize = 96;
        private static readonly Dictionary<NeonShape, Sprite> Sprites = new Dictionary<NeonShape, Sprite>();

        public static Sprite SpriteFor(NeonShape shape)
        {
            Sprite sprite;
            if (Sprites.TryGetValue(shape, out sprite) && sprite != null) return sprite;
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true);
            texture.name = "NC Vector " + shape;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int covered = 0;
                    for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float px = (x + (sx + 0.5f) * 0.5f) / TextureSize;
                        float py = (y + (sy + 0.5f) * 0.5f) / TextureSize;
                        if (Contains(shape, px, py)) covered++;
                    }
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(covered * 255 / 4));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            sprite.name = "NC Vector " + shape;
            Sprites[shape] = sprite;
            return sprite;
        }

        public static ArtPiece CreatePiece(string name, Transform parent, NeonShape shape, Color color, int order, Vector2 size, Vector2 visualOffset)
        {
            Transform pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(pivot, false);
            visual.localPosition = visualOffset;

            SpriteRenderer outline = CreateRenderer("Ink", visual, shape, Ink, order, size * 1.075f);
            SpriteRenderer fill = CreateRenderer("Fill", visual, shape, color, order + 1, size);
            return new ArtPiece(pivot, visual, outline, fill, size);
        }

        public static SpriteRenderer CreateFlat(string name, Transform parent, NeonShape shape, Color color, int order, Vector3 position, Vector2 size)
        {
            SpriteRenderer renderer = CreateRenderer(name, parent, shape, color, order, size);
            renderer.transform.localPosition = position;
            return renderer;
        }

        public static Color SkinTone(string fighterId)
        {
            switch (fighterId)
            {
                case "zara": return new Color(0.30f, 0.13f, 0.09f);
                case "atlas": return new Color(0.55f, 0.30f, 0.18f);
                case "nyx": return new Color(0.78f, 0.60f, 0.51f);
                case "rio": return new Color(0.54f, 0.32f, 0.19f);
                case "sable": return new Color(0.76f, 0.58f, 0.48f);
                case "mara": return new Color(0.47f, 0.24f, 0.16f);
                case "batu": return new Color(0.51f, 0.34f, 0.23f);
                case "lux": return new Color(0.33f, 0.16f, 0.10f);
                case "oren": return new Color(0.82f, 0.66f, 0.55f);
                default: return new Color(0.72f, 0.52f, 0.39f);
            }
        }

        private static SpriteRenderer CreateRenderer(string name, Transform parent, NeonShape shape, Color color, int order, Vector2 size)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteFor(shape);
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static bool Contains(NeonShape shape, float x, float y)
        {
            float dx = x - 0.5f;
            float dy = y - 0.5f;
            switch (shape)
            {
                case NeonShape.Block: return x >= 0.025f && x <= 0.975f && y >= 0.025f && y <= 0.975f;
                case NeonShape.Disc: return dx * dx + dy * dy <= 0.235f;
                case NeonShape.Capsule:
                    float cx = Mathf.Clamp(x, 0.27f, 0.73f) - x;
                    return cx * cx + dy * dy <= 0.058f;
                case NeonShape.Diamond: return Mathf.Abs(dx) * 0.82f + Mathf.Abs(dy) <= 0.46f;
                case NeonShape.Triangle: return PointInTriangle(new Vector2(x, y), new Vector2(0.5f, 0.94f), new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.08f));
                case NeonShape.Tapered:
                    float half = Mathf.Lerp(0.23f, 0.43f, y);
                    return y >= 0.05f && y <= 0.95f && Mathf.Abs(dx) <= half;
                case NeonShape.Hexagon: return Mathf.Abs(dx) <= 0.44f && Mathf.Abs(dy) <= 0.48f && Mathf.Abs(dx) + Mathf.Abs(dy) * 0.58f <= 0.58f;
                case NeonShape.Chevron:
                    return y > 0.08f && y < 0.92f && Mathf.Abs(Mathf.Abs(dx) - (0.08f + Mathf.Abs(dy) * 0.68f)) < 0.105f;
                case NeonShape.Ring:
                    float r2 = dx * dx + dy * dy;
                    return r2 <= 0.235f && r2 >= 0.105f;
                case NeonShape.Crescent:
                    return dx * dx + dy * dy <= 0.235f && (dx - 0.16f) * (dx - 0.16f) + dy * dy >= 0.17f;
                default: return false;
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }

    public sealed class ArtPiece
    {
        public Transform Pivot { get; private set; }
        public Transform Visual { get; private set; }
        public SpriteRenderer Outline { get; private set; }
        public SpriteRenderer Fill { get; private set; }
        public Vector2 BaseSize { get; private set; }

        public ArtPiece(Transform pivot, Transform visual, SpriteRenderer outline, SpriteRenderer fill, Vector2 size)
        {
            Pivot = pivot; Visual = visual; Outline = outline; Fill = fill; BaseSize = size;
        }

        public void SetVisible(bool visible) { Outline.enabled = visible; Fill.enabled = visible; }
        public void SetColor(Color color) { Fill.color = color; }
        public void SetVisualScale(float x, float y)
        {
            Visual.localScale = new Vector3(x, y, 1f);
        }
    }
}
