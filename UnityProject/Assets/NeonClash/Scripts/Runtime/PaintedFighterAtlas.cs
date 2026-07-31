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
            GameObject item = new GameObject("Painted Fighter - " + fighterId);
            item.transform.SetParent(parent, false);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;
            return renderer;
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
}
