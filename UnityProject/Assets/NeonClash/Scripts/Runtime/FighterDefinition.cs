using UnityEngine;

namespace NeonClash
{
    public enum FighterBodyBuild { Agile, Athletic, Power, Armored }
    public enum FighterHairStyle { Spikes, Braids, Wild, Bob, Crest, BladeBob, Ponytail, Topknot, FoxTails, Flow }
    public enum FighterOutfitStyle { StreetJacket, CroppedStreet, GrapplerWrap, LongCoat, FlightVest, CounterRobe, Bomber, HeavyArmor, PrismJacket, TideRobe }
    public enum FighterEnergyStyle { Rift, Lightning, Ring, Orb, Comet, Slash, Meteor, Quake, Prism, Wave }

    [CreateAssetMenu(menuName = "Neon Clash/Fighter Definition", fileName = "Fighter")]
    public sealed class FighterDefinition : ScriptableObject
    {
        [SerializeField] private string fighterId = "fighter";
        [SerializeField] private string displayName = "FIGHTER";
        [SerializeField] private string alias = "CHALLENGER";
        [SerializeField] private string city = "WORLD CIRCUIT";
        [SerializeField] private string style = "Balanced";
        [SerializeField] private string specialName = "Special";
        [SerializeField] private string quote = "Ready.";
        [SerializeField] private string mark = "?";
        [SerializeField] private Color primaryColor = Color.cyan;
        [SerializeField] private Color secondaryColor = Color.blue;
        [SerializeField, Range(1, 10)] private int speed = 5;
        [SerializeField, Range(1, 10)] private int power = 5;
        [SerializeField, Range(1, 10)] private int reach = 5;
        [SerializeField] private string comboName = "SIGNATURE";
        [SerializeField] private string comboSequence = "T,T,U,L";
        [Header("Original modular art direction")]
        [SerializeField] private FighterBodyBuild bodyBuild = FighterBodyBuild.Athletic;
        [SerializeField] private FighterHairStyle hairStyle = FighterHairStyle.Spikes;
        [SerializeField] private FighterOutfitStyle outfitStyle = FighterOutfitStyle.StreetJacket;
        [SerializeField] private FighterEnergyStyle energyStyle = FighterEnergyStyle.Rift;

        public string FighterId { get { return fighterId; } }
        public string DisplayName { get { return displayName; } }
        public string Alias { get { return alias; } }
        public string City { get { return city; } }
        public string Style { get { return style; } }
        public string SpecialName { get { return specialName; } }
        public string Quote { get { return quote; } }
        public string Mark { get { return mark; } }
        public Color PrimaryColor { get { return primaryColor; } }
        public Color SecondaryColor { get { return secondaryColor; } }
        public int Speed { get { return speed; } }
        public int Power { get { return power; } }
        public int Reach { get { return reach; } }
        public string ComboName { get { return comboName; } }
        public string ComboSequence { get { return comboSequence; } }
        public FighterBodyBuild BodyBuild { get { return bodyBuild; } }
        public FighterHairStyle HairStyle { get { return hairStyle; } }
        public FighterOutfitStyle OutfitStyle { get { return outfitStyle; } }
        public FighterEnergyStyle EnergyStyle { get { return energyStyle; } }
        public bool UsesProjectileSpecial { get { return style == "Zoner" || style == "Control"; } }

        public FighterTuning ToTuning()
        {
            return new FighterTuning
            {
                Speed = speed,
                Power = power,
                Reach = reach,
                UsesProjectileSpecial = UsesProjectileSpecial,
                ComboSequence = comboSequence
            };
        }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(fighterId) || string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Fighter id and display name are required.";
                return false;
            }

            if (speed < 1 || speed > 10 || power < 1 || power > 10 || reach < 1 || reach > 10)
            {
                reason = "Speed, power, and reach must be between 1 and 10.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(comboName) || string.IsNullOrWhiteSpace(comboSequence))
            {
                reason = "Signature combo name and sequence are required.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
