using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonClash.Editor
{
    public static class FoundationValidator
    {
        private const string ScenePath = "Assets/NeonClash/Scenes/VerticalSlice.unity";
        private const string KaelPath = "Assets/NeonClash/Data/Fighters/Kael.asset";
        private const string ZaraPath = "Assets/NeonClash/Data/Fighters/Zara.asset";
        private const string CatalogPath = "Assets/NeonClash/Data/GameContentCatalog.asset";

        [MenuItem("Neon Clash/Validate Foundation")]
        public static void ValidateFromMenu()
        {
            Validate();
            EditorUtility.DisplayDialog("Neon Clash", "Foundation validation passed.", "OK");
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
            Debug.Log("NEON_CLASH_VALIDATION_PASSED");
        }

        private static void Validate()
        {
            FighterDefinition kael = RequireFighter(KaelPath, "kael");
            FighterDefinition zara = RequireFighter(ZaraPath, "zara");
            GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            ValidateCatalog(catalog);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Vertical slice scene is missing: " + ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneWiring(kael, zara, catalog);

            ValidateMovement(kael);
            ValidateCombat(kael, zara);
            ValidateRepeatability(kael, zara);
            ValidateAdvancedCombat();
            ValidateOnlineBoundary(catalog, kael, zara);
            ArtMilestoneValidator.ValidateFromCommandLine();
        }

        private static void ValidateCatalog(GameContentCatalog catalog)
        {
            if (catalog == null) throw new InvalidOperationException("Migrated content catalog is missing.");
            string reason;
            if (!catalog.IsComplete(out reason)) throw new InvalidOperationException(reason);
            System.Collections.Generic.HashSet<string> ids = new System.Collections.Generic.HashSet<string>();
            foreach (FighterDefinition fighter in catalog.Fighters)
            {
                if (fighter == null || !fighter.IsValid(out reason)) throw new InvalidOperationException("Invalid fighter in catalog: " + reason);
                if (!ids.Add(fighter.FighterId)) throw new InvalidOperationException("Duplicate fighter id: " + fighter.FighterId);
            }
            ids.Clear();
            foreach (StageDefinition stage in catalog.Stages)
            {
                if (stage == null || !stage.IsValid(out reason)) throw new InvalidOperationException("Invalid stage in catalog: " + reason);
                if (!ids.Add(stage.StageId)) throw new InvalidOperationException("Duplicate stage id: " + stage.StageId);
            }
            ids.Clear();
            foreach (CostumeDefinition costume in catalog.Costumes)
                if (costume == null || !ids.Add(costume.CostumeId)) throw new InvalidOperationException("Invalid or duplicate costume.");
        }

        private static FighterDefinition RequireFighter(string path, string expectedId)
        {
            FighterDefinition fighter = AssetDatabase.LoadAssetAtPath<FighterDefinition>(path);
            if (fighter == null) throw new InvalidOperationException("Fighter asset is missing: " + path);
            string reason;
            if (!fighter.IsValid(out reason)) throw new InvalidOperationException(path + ": " + reason);
            if (fighter.FighterId != expectedId) throw new InvalidOperationException(path + " has unexpected id " + fighter.FighterId);
            return fighter;
        }

        private static void ValidateSceneWiring(FighterDefinition kael, FighterDefinition zara, GameContentCatalog catalog)
        {
            GameObject root = GameObject.Find("Neon Clash Bootstrap");
            if (root == null) throw new InvalidOperationException("The scene has no Neon Clash Bootstrap object.");
            NeonClashBootstrap bootstrap = root.GetComponent<NeonClashBootstrap>();
            if (bootstrap == null) throw new InvalidOperationException("The scene bootstrap component is missing.");

            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            FighterDefinition sceneKael = serializedBootstrap.FindProperty("kael").objectReferenceValue as FighterDefinition;
            FighterDefinition sceneZara = serializedBootstrap.FindProperty("zara").objectReferenceValue as FighterDefinition;
            GameContentCatalog sceneCatalog = serializedBootstrap.FindProperty("catalog").objectReferenceValue as GameContentCatalog;
            if (sceneKael != kael || sceneZara != zara || sceneCatalog != catalog)
                throw new InvalidOperationException("The scene is not wired to the validated Kael and Zara assets.");
        }

        private static void ValidateAdvancedCombat()
        {
            ComboSequenceTracker tracker = new ComboSequenceTracker();
            FighterButtons[] sequence = { FighterButtons.LightPunch, FighterButtons.LightPunch, FighterButtons.LightKick, FighterButtons.Special };
            bool matched = false;
            for (int i = 0; i < sequence.Length; i++) matched = tracker.RecordAndMatch(new FighterCommand { Buttons = sequence[i] }, i * 8, "T,T,U,L");
            if (!matched) throw new InvalidOperationException("Signature combo recognition assertion failed.");

            FighterState attacker = FighterSimulation.CreateInitialState(-900, 1);
            FighterState defender = FighterSimulation.CreateInitialState(900, -1);
            ProjectileState projectile = new ProjectileState { Active = true, Owner = 0, PositionX = 700, PositionY = 1100, Damage = 15000 };
            if (!FighterSimulation.ApplyProjectileHit(ref attacker, ref defender, projectile) || defender.Health != FighterSimulation.MaxHealth - 15000)
                throw new InvalidOperationException("Projectile collision assertion failed.");
        }

        private static void ValidateOnlineBoundary(GameContentCatalog catalog, FighterDefinition kael, FighterDefinition zara)
        {
            if (CloudflareRoomService.NormalizeBaseUrl("https://rooms.example.test/") != "https://rooms.example.test")
                throw new InvalidOperationException("Room-service URL normalization assertion failed.");
            if (CloudflareRoomService.NormalizeBaseUrl("http://127.0.0.1:8787/") != "http://127.0.0.1:8787")
                throw new InvalidOperationException("Local room-service URL assertion failed.");

            bool rejectedInsecureRemote = false;
            try { CloudflareRoomService.NormalizeBaseUrl("http://rooms.example.test"); }
            catch (ArgumentException) { rejectedInsecureRemote = true; }
            if (!rejectedInsecureRemote) throw new InvalidOperationException("Insecure remote room-service URL was accepted.");

            string hash = RealtimeProtocolV2.ComputeContentHash(catalog);
            if (hash.Length != 64 || hash != RealtimeProtocolV2.ComputeContentHash(catalog))
                throw new InvalidOperationException("Protocol-2 content hash is not stable SHA-256 text.");
            if (RealtimeProtocolV2.NormalizeSocketUri("wss://relay.example.test/v2/socket").Scheme != "wss" ||
                RealtimeProtocolV2.NormalizeSocketUri("ws://127.0.0.1:8787/v2/socket").Scheme != "ws")
                throw new InvalidOperationException("Protocol-2 socket URL normalization failed.");
            bool rejectedInsecureSocket = false;
            try { RealtimeProtocolV2.NormalizeSocketUri("ws://relay.example.test/v2/socket"); }
            catch (ArgumentException) { rejectedInsecureSocket = true; }
            if (!rejectedInsecureSocket) throw new InvalidOperationException("Insecure remote WebSocket URL was accepted.");

            DeterministicMatchSimulation completeMatch = new DeterministicMatchSimulation(kael.ToTuning(), zara.ToTuning());
            RollbackSession rollback = new RollbackSession(completeMatch, 0);
            NetworkInputFrame inputFrame = rollback.Advance(new FighterCommand { Move = 1, Buttons = FighterButtons.LightPunch });
            ProtocolEnvelopeV2 inputEnvelope = new ProtocolEnvelopeV2
            {
                kind = "input", sessionId = "validation-session", sequence = 1, input = ProtocolInputV2.FromFrame(inputFrame, 0)
            };
            ProtocolEnvelopeV2 roundTrip = RealtimeProtocolV2.Decode(RealtimeProtocolV2.Encode(inputEnvelope));
            if (roundTrip.input.tick != inputFrame.Tick || roundTrip.input.playerIndex != 0 || roundTrip.input.move != 1)
                throw new InvalidOperationException("Protocol-2 input JSON round trip failed.");

            DeterministicMatchState keyframeState = completeMatch.CaptureState();
            uint keyframeHash = DeterministicMatchSimulation.ComputeChecksum(keyframeState);
            ProtocolEnvelopeV2 keyframe = new ProtocolEnvelopeV2
            {
                kind = "keyframe", sessionId = "validation-session", sequence = 2,
                keyframe = new ProtocolKeyframeV2 { tick = keyframeState.Tick, checksum = RealtimeProtocolV2.ChecksumText(keyframeHash), state = keyframeState }
            };
            ProtocolEnvelopeV2 decodedKeyframe = RealtimeProtocolV2.Decode(RealtimeProtocolV2.Encode(keyframe));
            if (DeterministicMatchSimulation.ComputeChecksum(decodedKeyframe.keyframe.state) != keyframeHash)
                throw new InvalidOperationException("Protocol-2 complete-state keyframe round trip failed.");

            bool rejectedShortChecksum = false;
            try { RealtimeProtocolV2.ParseChecksum("1"); }
            catch (ArgumentException) { rejectedShortChecksum = true; }
            if (!rejectedShortChecksum) throw new InvalidOperationException("Protocol-2 accepted a noncanonical checksum.");

            FighterState original = FighterSimulation.CreateInitialState(1234, -1);
            original.PositionY = 987;
            original.VelocityX = -321;
            original.VelocityY = 654;
            original.Health = 73125;
            original.Drive = 44321;
            LegacyCombatantSnapshotDto legacy = LegacyWebSnapshotAdapter.ToLegacy(original, 1);
            FighterState restored = LegacyWebSnapshotAdapter.FromLegacy(legacy);
            if (Mathf.Abs(original.PositionX - restored.PositionX) > 1 ||
                Mathf.Abs(original.PositionY - restored.PositionY) > 1 ||
                Mathf.Abs(original.VelocityX - restored.VelocityX) > 1 ||
                Mathf.Abs(original.VelocityY - restored.VelocityY) > 1 ||
                original.Health != restored.Health || original.Drive != restored.Drive ||
                original.Facing != restored.Facing)
                throw new InvalidOperationException("Legacy snapshot conversion exceeded its rounding tolerance.");
        }

        private static void ValidateMovement(FighterDefinition fighter)
        {
            FighterState state = FighterSimulation.CreateInitialState(0, 1);
            FighterCommand move = new FighterCommand { Move = 1 };
            for (int i = 0; i < 60; i++) FighterSimulation.Step(ref state, move, fighter.Speed);
            if (state.PositionX <= 1000 || state.PositionX > FighterSimulation.ArenaHalfWidth)
                throw new InvalidOperationException("Fixed-tick movement assertion failed.");

            FighterCommand jump = new FighterCommand { Buttons = FighterButtons.Jump };
            FighterSimulation.Step(ref state, jump, fighter.Speed);
            if (state.Grounded || state.PositionY <= 0) throw new InvalidOperationException("Jump assertion failed.");
            for (int i = 0; i < 180; i++) FighterSimulation.Step(ref state, new FighterCommand(), fighter.Speed);
            if (!state.Grounded || state.PositionY != 0) throw new InvalidOperationException("Landing assertion failed.");
        }

        private static void ValidateCombat(FighterDefinition kael, FighterDefinition zara)
        {
            FighterState attacker = FighterSimulation.CreateInitialState(-600, 1);
            FighterState defender = FighterSimulation.CreateInitialState(600, -1);
            FighterCommand punch = new FighterCommand { Buttons = FighterButtons.LightPunch };
            bool connected = false;
            for (int i = 0; i < 8; i++)
            {
                FighterSimulation.Step(ref attacker, i == 0 ? punch : new FighterCommand(), kael.Speed);
                FighterSimulation.Step(ref defender, new FighterCommand(), zara.Speed);
                connected |= FighterSimulation.TryResolveHit(ref attacker, ref defender, kael.Power, kael.Reach);
            }
            if (!connected || defender.Health >= FighterSimulation.MaxHealth)
                throw new InvalidOperationException("Attack/hit assertion failed.");

            FighterState blockedAttacker = FighterSimulation.CreateInitialState(-600, 1);
            FighterState blockedDefender = FighterSimulation.CreateInitialState(600, -1);
            int unblockedDamage = FighterSimulation.MaxHealth - defender.Health;
            FighterCommand guard = new FighterCommand { Buttons = FighterButtons.Guard };
            for (int i = 0; i < 8; i++)
            {
                FighterSimulation.Step(ref blockedAttacker, i == 0 ? punch : new FighterCommand(), kael.Speed);
                FighterSimulation.Step(ref blockedDefender, guard, zara.Speed);
                FighterSimulation.TryResolveHit(ref blockedAttacker, ref blockedDefender, kael.Power, kael.Reach);
            }
            int blockedDamage = FighterSimulation.MaxHealth - blockedDefender.Health;
            if (blockedDamage <= 0 || blockedDamage >= unblockedDamage)
                throw new InvalidOperationException("Block damage assertion failed.");
        }

        private static void ValidateRepeatability(FighterDefinition kael, FighterDefinition zara)
        {
            int firstHash = SimulateAndHash(kael, zara);
            int secondHash = SimulateAndHash(kael, zara);
            if (firstHash != secondHash) throw new InvalidOperationException("Deterministic replay assertion failed.");
        }

        private static int SimulateAndHash(FighterDefinition kael, FighterDefinition zara)
        {
            FighterState first = FighterSimulation.CreateInitialState(-2500, 1);
            FighterState second = FighterSimulation.CreateInitialState(2500, -1);
            for (int tick = 1; tick <= 600; tick++)
            {
                FighterCommand firstCommand = new FighterCommand { Move = tick < 100 ? 1 : 0 };
                if (tick % 87 == 0) firstCommand.Buttons = FighterButtons.LightKick;
                FighterCommand secondCommand = DeterministicCpu.Decide(tick, second, first);
                FighterSimulation.FaceEachOther(ref first, ref second);
                FighterSimulation.Step(ref first, firstCommand, kael.Speed);
                FighterSimulation.Step(ref second, secondCommand, zara.Speed);
                FighterSimulation.FaceEachOther(ref first, ref second);
                FighterSimulation.TryResolveHit(ref first, ref second, kael.Power, kael.Reach);
                FighterSimulation.TryResolveHit(ref second, ref first, zara.Power, zara.Reach);
                FighterSimulation.Separate(ref first, ref second);
            }

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + first.PositionX;
                hash = hash * 31 + first.PositionY;
                hash = hash * 31 + first.Health;
                hash = hash * 31 + first.Drive;
                hash = hash * 31 + second.PositionX;
                hash = hash * 31 + second.PositionY;
                hash = hash * 31 + second.Health;
                hash = hash * 31 + second.Drive;
                return hash;
            }
        }
    }
}
