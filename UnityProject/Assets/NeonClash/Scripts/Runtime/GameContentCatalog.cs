using UnityEngine;

namespace NeonClash
{
    [CreateAssetMenu(menuName = "Neon Clash/Game Content Catalog", fileName = "GameContentCatalog")]
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private FighterDefinition[] fighters = new FighterDefinition[0];
        [SerializeField] private StageDefinition[] stages = new StageDefinition[0];
        [SerializeField] private CostumeDefinition[] costumes = new CostumeDefinition[0];
        [SerializeField] private string sourceRevision = "web-reference-v1";

        public FighterDefinition[] Fighters { get { return fighters; } }
        public StageDefinition[] Stages { get { return stages; } }
        public CostumeDefinition[] Costumes { get { return costumes; } }
        public string SourceRevision { get { return sourceRevision; } }

        public bool IsComplete(out string reason)
        {
            if (fighters == null || fighters.Length != 10)
            {
                reason = "The authoritative web roster contains exactly 10 fighters.";
                return false;
            }
            if (stages == null || stages.Length != 10)
            {
                reason = "The catalog must contain all 10 source stages.";
                return false;
            }
            if (costumes == null || costumes.Length != 3)
            {
                reason = "The catalog must contain all 3 source costumes.";
                return false;
            }
            reason = string.Empty;
            return true;
        }
    }
}
