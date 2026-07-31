using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonClash.Editor
{
    public static class ContentMigrationGenerator
    {
        private const string Root = "Assets/NeonClash/Data";
        private const string ScenePath = "Assets/NeonClash/Scenes/VerticalSlice.unity";

        private static readonly FighterSeed[] Fighters =
        {
            new FighterSeed("kael", "KAEL", "SUN BREAKER", "SEOUL", "Rushdown", "Solar Rift", "Speed is a decision.", "#27f4ff", "#1768ff", 9, 6, 6, "K", "SOLAR CHAIN", "T,T,U,L", FighterBodyBuild.Athletic, FighterHairStyle.Spikes, FighterOutfitStyle.StreetJacket, FighterEnergyStyle.Rift),
            new FighterSeed("zara", "ZARA", "VOLT QUEEN", "LAGOS", "Pressure", "Thunder Step", "Hear the storm arrive.", "#ff2dba", "#8a3bff", 8, 7, 5, "Z", "VOLTAGE RUSH", "T,U,Y,L", FighterBodyBuild.Athletic, FighterHairStyle.Braids, FighterOutfitStyle.CroppedStreet, FighterEnergyStyle.Lightning),
            new FighterSeed("atlas", "ATLAS", "IRON SAINT", "ATHENS", "Grappler", "Titan Break", "The ground remembers.", "#ff7648", "#ffb627", 4, 10, 6, "A", "TITAN LOCK", "Y,K,Y,L", FighterBodyBuild.Power, FighterHairStyle.Wild, FighterOutfitStyle.GrapplerWrap, FighterEnergyStyle.Ring),
            new FighterSeed("nyx", "NYX", "VOID SIGNAL", "BERLIN", "Zoner", "Black Pulse", "Distance is control.", "#9b6cff", "#ff2dba", 6, 7, 10, "N", "VOID CASCADE", "U,T,Y,L", FighterBodyBuild.Agile, FighterHairStyle.Bob, FighterOutfitStyle.LongCoat, FighterEnergyStyle.Orb),
            new FighterSeed("rio", "RIO", "SKYLINE KID", "SÃO PAULO", "Aerial", "Comet Kick", "Gravity is optional.", "#d8ff47", "#24df9b", 10, 5, 6, "R", "COMET LADDER", "U,U,K,L", FighterBodyBuild.Agile, FighterHairStyle.Crest, FighterOutfitStyle.FlightVest, FighterEnergyStyle.Comet),
            new FighterSeed("sable", "SABLE", "NIGHT BLADE", "TOKYO", "Counter", "Zero Cut", "Your move. My opening.", "#efefff", "#6676ff", 8, 8, 7, "S", "ZERO VERDICT", "T,K,Y,L", FighterBodyBuild.Athletic, FighterHairStyle.BladeBob, FighterOutfitStyle.CounterRobe, FighterEnergyStyle.Slash),
            new FighterSeed("mara", "MARA", "RED ORBIT", "MEXICO CITY", "Balanced", "Meteor Arc", "Burn bright. Hit hard.", "#ff405c", "#ff8b32", 7, 8, 7, "M", "ORBIT BREAK", "T,Y,U,L", FighterBodyBuild.Athletic, FighterHairStyle.Ponytail, FighterOutfitStyle.Bomber, FighterEnergyStyle.Meteor),
            new FighterSeed("batu", "BATU", "STEPPE WALL", "ULAANBAATAR", "Armor", "Stone Wake", "I do not move.", "#41d7bf", "#2587a6", 5, 9, 5, "B", "STEPPE QUAKE", "K,Y,K,L", FighterBodyBuild.Armored, FighterHairStyle.Topknot, FighterOutfitStyle.HeavyArmor, FighterEnergyStyle.Quake),
            new FighterSeed("lux", "LUX", "PRISM FOX", "PARIS", "Trickster", "Mirror Dash", "Catch the afterimage.", "#ffdc4a", "#ff4da9", 9, 6, 8, "L", "PRISM FEINT", "T,U,K,L", FighterBodyBuild.Agile, FighterHairStyle.FoxTails, FighterOutfitStyle.PrismJacket, FighterEnergyStyle.Prism),
            new FighterSeed("oren", "OREN", "TIDE MONK", "SYDNEY", "Control", "Breaker Wave", "Breathe between impacts.", "#48a8ff", "#42f5c5", 6, 7, 9, "O", "TIDAL FORM", "U,Y,K,L", FighterBodyBuild.Athletic, FighterHairStyle.Flow, FighterOutfitStyle.TideRobe, FighterEnergyStyle.Wave)
        };

        private static readonly StageSeed[] Stages =
        {
            new StageSeed("shibuya", "NEON CROSSING", "TOKYO", "Rain / Midnight", "skyline", "#27f4ff", "#ff2dba"),
            new StageSeed("hyperrail", "HYPERRAIL 88", "SEOUL", "Transit / 02:14", "rail", "#3e9dff", "#d8ff47"),
            new StageSeed("stormmarket", "STORM MARKET", "LAGOS", "Monsoon / Live", "market", "#ffbd31", "#ff2dba"),
            new StageSeed("aegis", "AEGIS FOUNDRY", "ATHENS", "Smelter / Shift 3", "forge", "#ff623e", "#ffd24a"),
            new StageSeed("voidclub", "VOID CLUB", "BERLIN", "Sublevel / 140 BPM", "club", "#a26cff", "#27f4ff"),
            new StageSeed("skycourt", "SKY COURT", "SÃO PAULO", "Rooftop / Sunset", "court", "#d8ff47", "#28e2a0"),
            new StageSeed("solarplaza", "SOLAR PLAZA", "MEXICO CITY", "Festival / Golden Hour", "plaza", "#ff405c", "#ffb12b"),
            new StageSeed("steppe", "STEPPE SHRINE", "ULAANBAATAR", "High Wind / Dawn", "steppe", "#41d7bf", "#c9f5ff"),
            new StageSeed("prismmetro", "PRISM METRO", "PARIS", "Platform / Last Train", "metro", "#ffdc4a", "#ff4da9"),
            new StageSeed("tidal", "TIDAL OPERA", "SYDNEY", "Harbour / Blue Hour", "harbour", "#48a8ff", "#42f5c5")
        };

        private static readonly CostumeSeed[] Costumes =
        {
            new CostumeSeed("circuit", "CIRCUIT", "Tournament kit", "classic", 1f, 1f),
            new CostumeSeed("afterdark", "AFTER DARK", "Sleek night look", "sleek", 0.72f, 0.7f),
            new CostumeSeed("heatwave", "HEATWAVE", "Bold summer look", "heatwave", 1.2f, 1.12f)
        };

        [MenuItem("Neon Clash/Regenerate Migrated Content")]
        public static void GenerateFromMenu() { Generate(); }

        public static void GenerateFromCommandLine()
        {
            Generate();
            Debug.Log("NEON_CLASH_CONTENT_GENERATION_PASSED");
        }

        private static void Generate()
        {
            EnsureFolder(Root, "Fighters");
            EnsureFolder(Root, "Stages");
            EnsureFolder(Root, "Costumes");
            List<FighterDefinition> fighters = new List<FighterDefinition>();
            List<StageDefinition> stages = new List<StageDefinition>();
            List<CostumeDefinition> costumes = new List<CostumeDefinition>();

            foreach (FighterSeed seed in Fighters)
            {
                string path = Root + "/Fighters/" + Title(seed.Id) + ".asset";
                FighterDefinition asset = LoadOrCreate<FighterDefinition>(path);
                Set(asset, "fighterId", seed.Id, "displayName", seed.Name, "alias", seed.Alias, "city", seed.City, "style", seed.Style,
                    "specialName", seed.Special, "quote", seed.Quote, "mark", seed.Mark, "speed", seed.Speed, "power", seed.Power,
                    "reach", seed.Reach, "comboName", seed.ComboName, "comboSequence", seed.ComboSequence,
                    "primaryColor", ParseColor(seed.Primary), "secondaryColor", ParseColor(seed.Secondary),
                    "bodyBuild", seed.BodyBuild, "hairStyle", seed.HairStyle, "outfitStyle", seed.OutfitStyle, "energyStyle", seed.EnergyStyle);
                fighters.Add(asset);
            }

            foreach (StageSeed seed in Stages)
            {
                string path = Root + "/Stages/" + Title(seed.Id) + ".asset";
                StageDefinition asset = LoadOrCreate<StageDefinition>(path);
                Set(asset, "stageId", seed.Id, "displayName", seed.Name, "city", seed.City, "descriptor", seed.Descriptor,
                    "motif", seed.Motif, "primaryColor", ParseColor(seed.Primary), "accentColor", ParseColor(seed.Accent));
                stages.Add(asset);
            }

            foreach (CostumeSeed seed in Costumes)
            {
                string path = Root + "/Costumes/" + Title(seed.Id) + ".asset";
                CostumeDefinition asset = LoadOrCreate<CostumeDefinition>(path);
                Set(asset, "costumeId", seed.Id, "displayName", seed.Name, "note", seed.Note, "cut", seed.Cut,
                    "saturation", seed.Saturation, "brightness", seed.Brightness);
                costumes.Add(asset);
            }

            GameContentCatalog catalog = LoadOrCreate<GameContentCatalog>(Root + "/GameContentCatalog.asset");
            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SetArray(serializedCatalog.FindProperty("fighters"), fighters.ToArray());
            SetArray(serializedCatalog.FindProperty("stages"), stages.ToArray());
            SetArray(serializedCatalog.FindProperty("costumes"), costumes.ToArray());
            serializedCatalog.FindProperty("sourceRevision").stringValue = "app/neon-clash.tsx:2026-07-24";
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("Neon Clash Bootstrap");
            SerializedObject bootstrap = new SerializedObject(root.GetComponent<NeonClashBootstrap>());
            bootstrap.FindProperty("catalog").objectReferenceValue = catalog;
            bootstrap.FindProperty("kael").objectReferenceValue = fighters[0];
            bootstrap.FindProperty("zara").objectReferenceValue = fighters[1];
            bootstrap.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void Set(UnityEngine.Object target, params object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            for (int i = 0; i < values.Length; i += 2)
            {
                SerializedProperty property = serialized.FindProperty((string)values[i]);
                object value = values[i + 1];
                if (value is string text) property.stringValue = text;
                else if (value is int integer) property.intValue = integer;
                else if (value is float number) property.floatValue = number;
                else if (value is Color color) property.colorValue = color;
                else if (value is Enum enumValue) property.enumValueIndex = Convert.ToInt32(enumValue);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Color ParseColor(string html)
        {
            if (!ColorUtility.TryParseHtmlString(html, out Color color)) throw new InvalidOperationException("Invalid color: " + html);
            return color;
        }

        private static string Title(string id) { return char.ToUpperInvariant(id[0]) + id.Substring(1); }

        private readonly struct FighterSeed
        {
            public readonly string Id, Name, Alias, City, Style, Special, Quote, Primary, Secondary, Mark, ComboName, ComboSequence;
            public readonly int Speed, Power, Reach;
            public readonly FighterBodyBuild BodyBuild; public readonly FighterHairStyle HairStyle; public readonly FighterOutfitStyle OutfitStyle; public readonly FighterEnergyStyle EnergyStyle;
            public FighterSeed(string id, string name, string alias, string city, string style, string special, string quote, string primary, string secondary, int speed, int power, int reach, string mark, string comboName, string comboSequence, FighterBodyBuild bodyBuild, FighterHairStyle hairStyle, FighterOutfitStyle outfitStyle, FighterEnergyStyle energyStyle)
            { Id = id; Name = name; Alias = alias; City = city; Style = style; Special = special; Quote = quote; Primary = primary; Secondary = secondary; Speed = speed; Power = power; Reach = reach; Mark = mark; ComboName = comboName; ComboSequence = comboSequence; BodyBuild = bodyBuild; HairStyle = hairStyle; OutfitStyle = outfitStyle; EnergyStyle = energyStyle; }
        }
        private readonly struct StageSeed
        {
            public readonly string Id, Name, City, Descriptor, Motif, Primary, Accent;
            public StageSeed(string id, string name, string city, string descriptor, string motif, string primary, string accent)
            { Id = id; Name = name; City = city; Descriptor = descriptor; Motif = motif; Primary = primary; Accent = accent; }
        }
        private readonly struct CostumeSeed
        {
            public readonly string Id, Name, Note, Cut; public readonly float Saturation, Brightness;
            public CostumeSeed(string id, string name, string note, string cut, float saturation, float brightness)
            { Id = id; Name = name; Note = note; Cut = cut; Saturation = saturation; Brightness = brightness; }
        }
    }
}
