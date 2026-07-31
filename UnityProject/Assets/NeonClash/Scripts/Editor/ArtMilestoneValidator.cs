using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonClash.Editor
{
    public static class ArtMilestoneValidator
    {
        private const string CatalogPath = "Assets/NeonClash/Data/GameContentCatalog.asset";

        [MenuItem("Neon Clash/Validate Art Milestone")]
        public static void ValidateFromMenu()
        {
            Validate();
            EditorUtility.DisplayDialog("Neon Clash", "Art and animation validation passed.", "OK");
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
            Debug.Log("NEON_CLASH_ART_VALIDATION_PASSED");
        }

        public static void CaptureVisualQaFromCommandLine()
        {
            GameContentCatalog catalog = RequireCatalog();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Documentation", "VisualQA");
            Directory.CreateDirectory(output);
            string[] stageIds = new string[catalog.Stages.Length];
            for (int i = 0; i < stageIds.Length; i++) stageIds[i] = catalog.Stages[i].StageId;
            CaptureRoster(catalog, Path.Combine(output, "roster-production-rigs.png"));
            CapturePoseSheet(catalog, Path.Combine(output, "kael-animation-state-sheet.png"));
            for (int i = 0; i < stageIds.Length; i++)
                CaptureStage(i, Path.Combine(output, (i + 1).ToString("00") + "-" + stageIds[i] + ".png"));
            Debug.Log("NEON_CLASH_VISUAL_QA_CAPTURE_PASSED output=" + output);
        }

        private static void Validate()
        {
            GameContentCatalog catalog = RequireCatalog();
            if (Enum.GetValues(typeof(FighterVisualState)).Length != FighterPresentation.AnimationStateCount)
                throw new InvalidOperationException("Animation state count is out of sync with FighterVisualState.");

            HashSet<FighterHairStyle> hairStyles = new HashSet<FighterHairStyle>();
            HashSet<FighterOutfitStyle> outfitStyles = new HashSet<FighterOutfitStyle>();
            HashSet<FighterEnergyStyle> energyStyles = new HashSet<FighterEnergyStyle>();
            foreach (FighterDefinition fighter in catalog.Fighters)
            {
                hairStyles.Add(fighter.HairStyle);
                outfitStyles.Add(fighter.OutfitStyle);
                energyStyles.Add(fighter.EnergyStyle);
                foreach (CostumeDefinition costume in catalog.Costumes)
                {
                    FighterPresentation presentation = FighterPresentation.Create(fighter, costume, "Art QA " + fighter.FighterId + " " + costume.CostumeId, 0);
                    int renderers = presentation.Root.GetComponentsInChildren<SpriteRenderer>(true).Length;
                    if (renderers < 40) throw new InvalidOperationException(fighter.FighterId + "/" + costume.CostumeId + " rig is incomplete: " + renderers + " renderers.");
                    if (!presentation.UsesPaintedArt) throw new InvalidOperationException(fighter.FighterId + "/" + costume.CostumeId + " is missing its painted fighter layer.");
                    if (ContainsPlaceholderName(presentation.Root)) throw new InvalidOperationException("Placeholder object remains in fighter art pipeline.");
                    ExerciseAllStates(presentation);
                    UnityEngine.Object.DestroyImmediate(presentation.Root.gameObject);
                }
            }
            if (hairStyles.Count != catalog.Fighters.Length || outfitStyles.Count != catalog.Fighters.Length || energyStyles.Count != catalog.Fighters.Length)
                throw new InvalidOperationException("Every source fighter must have a distinct hair, garment, and energy art profile.");

            foreach (StageDefinition stage in catalog.Stages)
            {
                GameObject arena = StagePresentation.Build(stage);
                int renderers = arena.GetComponentsInChildren<SpriteRenderer>(true).Length;
                StageAmbience ambience = arena.GetComponent<StageAmbience>();
                if (renderers < 30) throw new InvalidOperationException(stage.StageId + " arena is incomplete: " + renderers + " renderers.");
                if (ambience == null || ambience.AnimatedElementCount < 4) throw new InvalidOperationException(stage.StageId + " lacks production ambience animation.");
                Transform paintedBackdrop = arena.transform.Find("Painted Arena Background");
                if (paintedBackdrop == null || paintedBackdrop.GetComponent<SpriteRenderer>() == null)
                    throw new InvalidOperationException(stage.StageId + " is missing the painted arena background.");
                if (arena.transform.Find(ExpectedMarker(stage.Motif)) == null) throw new InvalidOperationException(stage.StageId + " did not build its expected motif marker " + ExpectedMarker(stage.Motif) + ".");
                if (ContainsPlaceholderName(arena.transform)) throw new InvalidOperationException("Placeholder object remains in stage art pipeline.");
                UnityEngine.Object.DestroyImmediate(arena);
            }
            ValidateStateSelection();
        }

        private static GameContentCatalog RequireCatalog()
        {
            GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            string reason = "Catalog asset is missing.";
            if (catalog == null || !catalog.IsComplete(out reason)) throw new InvalidOperationException("Art catalog is unavailable: " + reason);
            return catalog;
        }

        private static void ExerciseAllStates(FighterPresentation presentation)
        {
            for (int i = 0; i < FighterPresentation.AnimationStateCount; i++)
            {
                FighterVisualState expected = (FighterVisualState)i;
                FighterState state = StateFor(expected);
                MatchPhase phase = expected == FighterVisualState.Finish ? MatchPhase.MatchOver : expected == FighterVisualState.Knockdown ? MatchPhase.Knockout : MatchPhase.Fight;
                FighterVisualState actual = presentation.Present(state, 33 + i * 7, phase, expected == FighterVisualState.Finish);
                if (actual != expected) throw new InvalidOperationException("Expected pose " + expected + " but resolved " + actual + ".");
            }
        }

        private static void ValidateStateSelection()
        {
            for (int i = 0; i < FighterPresentation.AnimationStateCount; i++)
            {
                FighterVisualState expected = (FighterVisualState)i;
                FighterState state = StateFor(expected);
                MatchPhase phase = expected == FighterVisualState.Finish ? MatchPhase.MatchOver : expected == FighterVisualState.Knockdown ? MatchPhase.Knockout : MatchPhase.Fight;
                FighterVisualState actual = FighterPresentation.ResolveVisualState(state, phase, expected == FighterVisualState.Finish);
                if (actual != expected) throw new InvalidOperationException("Visual state selection failed for " + expected + ".");
            }
        }

        private static FighterState StateFor(FighterVisualState state)
        {
            FighterState value = FighterSimulation.CreateInitialState(0, 1);
            switch (state)
            {
                case FighterVisualState.Advance: value.VelocityX = 1500; break;
                case FighterVisualState.Retreat: value.VelocityX = -1500; break;
                case FighterVisualState.JumpRise: value.Grounded = false; value.PositionY = 1200; value.VelocityY = 2400; break;
                case FighterVisualState.JumpFall: value.Grounded = false; value.PositionY = 1200; value.VelocityY = -2400; break;
                case FighterVisualState.Crouch: value.Crouching = true; break;
                case FighterVisualState.Guard: value.Guarding = true; break;
                case FighterVisualState.LightPunch: SetAction(ref value, CombatAction.LightPunch); break;
                case FighterVisualState.HeavyPunch: SetAction(ref value, CombatAction.HeavyPunch); break;
                case FighterVisualState.LightKick: SetAction(ref value, CombatAction.LightKick); break;
                case FighterVisualState.HeavyKick: SetAction(ref value, CombatAction.HeavyKick); break;
                case FighterVisualState.Special: SetAction(ref value, CombatAction.Special); break;
                case FighterVisualState.Impact: SetAction(ref value, CombatAction.Impact); break;
                case FighterVisualState.Hit: value.HurtTicks = 8; break;
                case FighterVisualState.Stun: value.StunTicks = 8; break;
                case FighterVisualState.Knockdown: value.Health = 0; break;
            }
            return value;
        }

        private static void SetAction(ref FighterState state, CombatAction action)
        {
            AttackSpec spec = FighterSimulation.GetAttackSpec(action);
            state.Action = action;
            state.ActionTick = Mathf.Max(spec.ActiveFromTick, (spec.ActiveFromTick + spec.ActiveToTick) / 2);
        }

        private static bool ContainsPlaceholderName(Transform root)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string ExpectedMarker(string motif)
        {
            switch (motif)
            {
                case "rail": return "Hyperrail Body";
                case "market": return "Market Stall 0";
                case "forge": return "Foundry Furnace 0";
                case "club": return "Void Portal";
                case "court": return "Sky Court Sun";
                case "plaza": return "Solar Plaza Halo";
                case "steppe": return "Shrine Crossbeam";
                case "metro": return "Metro Tunnel";
                case "harbour": return "Opera Sail A";
                default: return "Crossing Tower 0";
            }
        }

        private static void CaptureRoster(GameContentCatalog catalog, string path)
        {
            NewCaptureScene(out Camera camera);
            catalog = RequireCatalog();
            StagePresentation.Build(catalog.Stages[0]);
            List<FighterPresentation> fighters = new List<FighterPresentation>();
            for (int i = 0; i < catalog.Fighters.Length; i++)
            {
                FighterPresentation fighter = FighterPresentation.Create(catalog.Fighters[i], catalog.Costumes[i % catalog.Costumes.Length], "Roster " + catalog.Fighters[i].DisplayName, 60 + i * 45);
                FighterState state = FighterSimulation.CreateInitialState(0, i % 2 == 0 ? 1 : -1);
                fighter.Present(state, 23 + i * 9, MatchPhase.Fight, false);
                fighter.Root.localScale = Vector3.one * 0.58f;
                fighter.Root.position = new Vector3(-6.4f + (i % 5) * 3.2f, i < 5 ? 0.30f : -2.70f, 0f);
                fighters.Add(fighter);
            }
            camera.transform.position = new Vector3(0f, 0.30f, -10f);
            camera.orthographicSize = 5.0f;
            Render(camera, path, 1920, 1080);
        }

        private static void CapturePoseSheet(GameContentCatalog catalog, string path)
        {
            NewCaptureScene(out Camera camera);
            catalog = RequireCatalog();
            FlatBackdrop();
            for (int i = 0; i < FighterPresentation.AnimationStateCount; i++)
            {
                FighterVisualState visualState = (FighterVisualState)i;
                FighterPresentation fighter = FighterPresentation.Create(catalog.Fighters[0], catalog.Costumes[i % catalog.Costumes.Length], "Pose " + visualState, 30 + i * 45);
                FighterState state = StateFor(visualState);
                MatchPhase phase = visualState == FighterVisualState.Finish ? MatchPhase.MatchOver : visualState == FighterVisualState.Knockdown ? MatchPhase.Knockout : MatchPhase.Fight;
                fighter.Present(state, 39 + i * 5, phase, visualState == FighterVisualState.Finish);
                fighter.Root.localScale = Vector3.one * 0.43f;
                fighter.Root.position = new Vector3(-7.2f + (i % 6) * 2.88f, 2.1f - (i / 6) * 2.65f, 0f);
            }
            camera.transform.position = new Vector3(0f, 0.2f, -10f);
            camera.orthographicSize = 5.1f;
            Render(camera, path, 1920, 1080);
        }

        private static void CaptureStage(int index, string path)
        {
            NewCaptureScene(out Camera camera);
            GameContentCatalog catalog = RequireCatalog();
            GameObject arena = StagePresentation.Build(catalog.Stages[index]);
            if (UnityEngine.Object.FindObjectsByType<StageAmbience>().Length != 1)
                throw new InvalidOperationException("Visual capture scene retained more than one arena.");
            if (arena.transform.Find(ExpectedMarker(catalog.Stages[index].Motif)) == null)
                throw new InvalidOperationException("Visual capture built the wrong motif for " + catalog.Stages[index].StageId + ".");
            FighterPresentation left = FighterPresentation.Create(catalog.Fighters[index], catalog.Costumes[index % 3], "Stage P1", 60);
            FighterPresentation right = FighterPresentation.Create(catalog.Fighters[(index + 1) % catalog.Fighters.Length], catalog.Costumes[(index + 1) % 3], "Stage P2", 110);
            FighterState attack = FighterSimulation.CreateInitialState(-2300, 1); SetAction(ref attack, index % 2 == 0 ? CombatAction.HeavyKick : CombatAction.Special);
            FighterState guard = FighterSimulation.CreateInitialState(2300, -1); guard.Guarding = true;
            left.Present(attack, 47, MatchPhase.Fight, false);
            right.Present(guard, 47, MatchPhase.Fight, false);
            Render(camera, path, 1600, 900);
        }

        private static void NewCaptureScene(out Camera camera)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Art QA Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.25f;
            camera.transform.position = new Vector3(0f, 1.2f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = NeonArtFactory.DeepNavy;
        }

        private static void FlatBackdrop()
        {
            Transform root = new GameObject("Pose Sheet Backdrop").transform;
            NeonArtFactory.CreateFlat("Background", root, NeonShape.Block, NeonArtFactory.DeepNavy, -20, new Vector3(0f, 0f, 2f), new Vector2(19f, 11f));
            for (int i = 0; i < 6; i++) NeonArtFactory.CreateFlat("Grid " + i, root, NeonShape.Block, new Color(0.2f, 0.75f, 0.95f, 0.08f), -18, new Vector3(-7.2f + i * 2.88f, 0f, 1f), new Vector2(0.025f, 10f));
        }

        private static void Render(Camera camera, string path, int width, int height)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
