using UnityEngine;

namespace NeonClash
{
    [CreateAssetMenu(menuName = "Neon Clash/Stage Definition", fileName = "Stage")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string stageId;
        [SerializeField] private string displayName;
        [SerializeField] private string city;
        [SerializeField] private string descriptor;
        [SerializeField] private string motif;
        [SerializeField] private Color primaryColor = Color.cyan;
        [SerializeField] private Color accentColor = Color.magenta;

        public string StageId { get { return stageId; } }
        public string DisplayName { get { return displayName; } }
        public string City { get { return city; } }
        public string Descriptor { get { return descriptor; } }
        public string Motif { get { return motif; } }
        public Color PrimaryColor { get { return primaryColor; } }
        public Color AccentColor { get { return accentColor; } }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stageId) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(motif))
            {
                reason = "Stage id, display name, and motif are required.";
                return false;
            }
            reason = string.Empty;
            return true;
        }
    }
}
