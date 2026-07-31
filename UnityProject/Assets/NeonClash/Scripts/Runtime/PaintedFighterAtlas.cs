using System.Collections.Generic;
using UnityEngine;

namespace NeonClash
{
    /// <summary>Project-owned painted roster atlas. Cropping is presentation-only and deterministic.</summary>
    public static class PaintedFighterAtlas
    {
        public const string ResourcePath = "NeonClash/Fighters/roster-atlas-v2";
        private static readonly string[] FighterOrder =
        {
            "kael", "zara", "atlas", "nyx", "oren",
            "sable", "rio", "batu", "lux", "mara"
        };
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static SpriteRenderer Create(string fighterId, Transform parent, int sortingOrder, Color tint)
        {
            Sprite sprite = SpriteFor(fighterId);
            if (sprite == null) return null;
            GameObject item = new GameObject("Painted Fighter Volume - " + fighterId);
            item.transform.SetParent(parent, false);
            PaintedFighterVolume volume = item.AddComponent<PaintedFighterVolume>();
            return volume.Configure(sprite, sortingOrder, tint);
        }

        public static Sprite SpriteFor(string fighterId)
        {
            Sprite cached;
            if (Sprites.TryGetValue(fighterId, out cached) && cached != null) return cached;
            int index = System.Array.IndexOf(FighterOrder, fighterId);
            if (index < 0) return null;
            Texture2D atlas = Resources.Load<Texture2D>(ResourcePath);
            if (atlas == null)
            {
                Debug.LogError("Painted fighter atlas is missing at Resources/" + ResourcePath + ".");
                return null;
            }
            float cellWidth = atlas.width / 5f;
            float cellHeight = atlas.height / 2f;
            int column = index % 5;
            int rowFromTop = index / 5;
            float y = rowFromTop == 0 ? cellHeight : 0f;
            Sprite sprite = Sprite.Create(atlas, new Rect(column * cellWidth, y, cellWidth, cellHeight), new Vector2(0.5f, 0.035f), 170f);
            sprite.name = "Painted Fighter " + fighterId;
            Sprites[fighterId] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// Adds a small, deterministic extrusion and rim-light to the owned painted sprite.
    /// This is a presentation-only 2.5D volume: it keeps the original atlas readable
    /// while giving the fighter a grounded edge, depth, and a stronger silhouette.
    /// </summary>
    public sealed class PaintedFighterVolume : MonoBehaviour
    {
        private SpriteRenderer main;
        private SpriteRenderer[] depthSlices;
        private SpriteRenderer rim;

        public SpriteRenderer Configure(Sprite sprite, int sortingOrder, Color initialTint)
        {
            main = gameObject.AddComponent<SpriteRenderer>();
            main.sprite = sprite;
            main.sortingOrder = sortingOrder;
            depthSlices = new SpriteRenderer[5];
            for (int i = 0; i < depthSlices.Length; i++)
            {
                GameObject slice = new GameObject("Depth Slice " + i);
                slice.transform.SetParent(transform, false);
                float offset = (i + 1) * 0.018f;
                slice.transform.localPosition = new Vector3(-offset, -offset * 0.72f, 0f);
                slice.transform.localScale = Vector3.one * (1f + i * 0.004f);
                SpriteRenderer renderer = slice.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder - depthSlices.Length + i;
                depthSlices[i] = renderer;
            }
            GameObject highlight = new GameObject("Painted Rim Light");
            highlight.transform.SetParent(transform, false);
            highlight.transform.localPosition = new Vector3(0.018f, 0.026f, 0f);
            highlight.transform.localScale = new Vector3(0.992f, 0.992f, 1f);
            rim = highlight.AddComponent<SpriteRenderer>();
            rim.sprite = sprite;
            rim.sortingOrder = sortingOrder + 1;
            SetTint(initialTint);
            return main;
        }

        public void SetTint(Color value)
        {
            if (main != null) main.color = value;
            if (depthSlices != null)
            {
                for (int i = 0; i < depthSlices.Length; i++)
                {
                    Color depth = Color.Lerp(Color.black, value, 0.24f + i * 0.035f);
                    depth.a = value.a * 0.82f;
                    depthSlices[i].color = depth;
                }
            }
            if (rim != null)
            {
                Color edge = Color.Lerp(value, Color.white, 0.52f);
                edge.a = value.a * 0.16f;
                rim.color = edge;
            }
        }

        public void SetVisible(bool visible)
        {
            if (main != null) main.enabled = visible;
            if (depthSlices != null)
                for (int i = 0; i < depthSlices.Length; i++) depthSlices[i].enabled = visible;
            if (rim != null) rim.enabled = visible;
        }
    }
}
