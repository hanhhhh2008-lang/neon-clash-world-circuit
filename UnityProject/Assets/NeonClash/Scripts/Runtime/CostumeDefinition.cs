using UnityEngine;

namespace NeonClash
{
    [CreateAssetMenu(menuName = "Neon Clash/Costume Definition", fileName = "Costume")]
    public sealed class CostumeDefinition : ScriptableObject
    {
        [SerializeField] private string costumeId;
        [SerializeField] private string displayName;
        [SerializeField] private string note;
        [SerializeField] private string cut;
        [SerializeField] private float saturation = 1f;
        [SerializeField] private float brightness = 1f;

        public string CostumeId { get { return costumeId; } }
        public string DisplayName { get { return displayName; } }
        public string Note { get { return note; } }
        public string Cut { get { return cut; } }
        public float Saturation { get { return saturation; } }
        public float Brightness { get { return brightness; } }

        public Color Apply(Color source)
        {
            Color.RGBToHSV(source, out float hue, out float sourceSaturation, out float value);
            return Color.HSVToRGB(hue, Mathf.Clamp01(sourceSaturation * saturation), Mathf.Clamp01(value * brightness));
        }
    }
}
