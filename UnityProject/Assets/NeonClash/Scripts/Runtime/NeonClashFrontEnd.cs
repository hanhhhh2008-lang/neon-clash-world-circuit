using System;
using System.Threading;
using UnityEngine;

namespace NeonClash
{
    public sealed class NeonClashFrontEnd : MonoBehaviour
    {
        private GameContentCatalog catalog;
        private int playerIndex;
        private int rivalIndex = 1;
        private int stageIndex;
        private int playerCostumeIndex;
        private int rivalCostumeIndex = 1;
        private CpuDifficulty difficulty = CpuDifficulty.Pro;
        private bool cpuEnabled = true;
        private bool touchControls = true;
        private float masterVolume = 0.8f;
        private bool selecting = true;
        private NeonClashMatch match;
        private GUIStyle title;
        private GUIStyle centred;
        private Vector2 scroll;
        private string relayUrl = "http://localhost:8787";
        private string joinCode = string.Empty;
        private string onlineStatus = "Protocol 2 relay is optional and separately deployed.";
        private bool onlineBusy;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Return)))
                Screen.fullScreen = !Screen.fullScreen;
            if (selecting && Input.GetKeyDown(KeyCode.Escape)) QuitToDesktop();
        }

        public void Initialize(GameContentCatalog content)
        {
            catalog = content;
            difficulty = (CpuDifficulty)Mathf.Clamp(PlayerPrefs.GetInt("difficulty", 1), 0, 2);
            touchControls = PlayerPrefs.GetInt("touch-controls", 1) != 0;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("master-volume", 0.8f));
            AudioListener.volume = masterVolume;
            playerIndex = Mathf.Clamp(PlayerPrefs.GetInt("player-fighter", 0), 0, catalog.Fighters.Length - 1);
            rivalIndex = Mathf.Clamp(PlayerPrefs.GetInt("rival-fighter", 1), 0, catalog.Fighters.Length - 1);
            stageIndex = Mathf.Clamp(PlayerPrefs.GetInt("stage", 0), 0, catalog.Stages.Length - 1);
            relayUrl = PlayerPrefs.GetString("relay-url", "http://localhost:8787");
            NeonClashBootstrap.CreateStage(catalog.Stages[stageIndex]);
        }

        public void ReturnToSelection()
        {
            if (match != null)
            {
                match.Shutdown();
                Destroy(match);
                match = null;
            }
            NeonClashBootstrap.CreateStage(catalog.Stages[stageIndex]);
            selecting = true;
        }

        private void StartFight()
        {
            if (playerIndex == rivalIndex) rivalIndex = (rivalIndex + 1) % catalog.Fighters.Length;
            PlayerPrefs.SetInt("difficulty", (int)difficulty);
            PlayerPrefs.SetInt("touch-controls", touchControls ? 1 : 0);
            PlayerPrefs.SetInt("player-fighter", playerIndex);
            PlayerPrefs.SetInt("rival-fighter", rivalIndex);
            PlayerPrefs.SetInt("stage", stageIndex);
            PlayerPrefs.SetFloat("master-volume", masterVolume);
            PlayerPrefs.Save();
            NeonClashBootstrap.CreateStage(catalog.Stages[stageIndex]);
            match = gameObject.AddComponent<NeonClashMatch>();
            match.Initialize(this, catalog.Fighters[playerIndex], catalog.Fighters[rivalIndex], catalog.Stages[stageIndex],
                catalog.Costumes[playerCostumeIndex], catalog.Costumes[rivalCostumeIndex], cpuEnabled, difficulty, touchControls);
            selecting = false;
        }

        private void CreateOnlineSession()
        {
            if (onlineBusy) return;
            try
            {
                SaveRelayUrl();
                RelaySessionServiceV2 service = new RelaySessionServiceV2(relayUrl, catalog, "auto");
                ProtocolMatchConfigV2 config = new ProtocolMatchConfigV2
                {
                    firstFighterId = catalog.Fighters[playerIndex].FighterId,
                    secondFighterId = catalog.Fighters[rivalIndex].FighterId,
                    firstCostumeId = catalog.Costumes[playerCostumeIndex].CostumeId,
                    secondCostumeId = catalog.Costumes[rivalCostumeIndex].CostumeId,
                    stageId = catalog.Stages[stageIndex].StageId,
                    randomSeed = (int)(DateTime.UtcNow.Ticks & int.MaxValue)
                };
                onlineBusy = true;
                onlineStatus = "Creating compatible session…";
                StartCoroutine(service.Create(config, assignment => ConnectOnline(service, assignment), OnlineFailed));
            }
            catch (Exception error) { OnlineFailed(error.Message); }
        }

        private void JoinOnlineSession(bool spectate)
        {
            if (onlineBusy) return;
            try
            {
                SaveRelayUrl();
                RelaySessionServiceV2 service = new RelaySessionServiceV2(relayUrl, catalog, "auto");
                onlineBusy = true;
                onlineStatus = spectate ? "Joining delayed spectator stream…" : "Joining rollback match…";
                StartCoroutine(service.Join(joinCode, spectate, assignment => ConnectOnline(service, assignment), OnlineFailed));
            }
            catch (Exception error) { OnlineFailed(error.Message); }
        }

        private async void ConnectOnline(RelaySessionServiceV2 service, ProtocolAssignmentV2 assignment)
        {
            WebSocketRollbackTransport transport = new WebSocketRollbackTransport();
            try
            {
                onlineStatus = "Opening secure realtime channel…";
                using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                    await transport.ConnectAsync(service.SocketUrl(assignment), assignment.token, timeout.Token);
                StartOnlineFight(assignment, transport);
            }
            catch (Exception error)
            {
                transport.Dispose();
                OnlineFailed(error.Message);
            }
        }

        private void StartOnlineFight(ProtocolAssignmentV2 assignment, WebSocketRollbackTransport transport)
        {
            ProtocolMatchConfigV2 config = assignment.config;
            if (config == null) throw new InvalidOperationException("Relay assignment omitted the authoritative match configuration.");
            FighterDefinition first = FindFighter(config.firstFighterId);
            FighterDefinition second = FindFighter(config.secondFighterId);
            CostumeDefinition firstCostume = FindCostume(config.firstCostumeId);
            CostumeDefinition secondCostume = FindCostume(config.secondCostumeId);
            StageDefinition stage = FindStage(config.stageId);
            NeonClashBootstrap.CreateStage(stage);
            match = gameObject.AddComponent<NeonClashMatch>();
            if (assignment.role == "spectator") match.InitializeSpectator(this, first, second, stage, firstCostume, secondCostume, assignment, transport);
            else match.InitializeOnline(this, first, second, stage, firstCostume, secondCostume, assignment, transport, touchControls);
            joinCode = assignment.joinCode;
            onlineStatus = assignment.role.ToUpperInvariant() + " // " + assignment.joinCode;
            onlineBusy = false;
            selecting = false;
        }

        private void OnlineFailed(string reason)
        {
            onlineBusy = false;
            onlineStatus = "ONLINE ERROR // " + (string.IsNullOrWhiteSpace(reason) ? "Unknown relay failure." : reason);
        }

        private void OnGUI()
        {
            if (!selecting || catalog == null) return;
            BuildStyles();
            GUI.skin.button.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 42f, 16f, 24f));
            GUI.skin.textField.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 48f, 15f, 22f));
            float scale = Mathf.Clamp(Screen.height / 900f, 0.78f, 1.25f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float viewportWidth = Screen.width / scale;
            float viewportHeight = Screen.height / scale;
            Color previousGuiColor = GUI.color;
            GUI.color = new Color(0.02f, 0.035f, 0.10f, 0.30f);
            GUI.DrawTexture(new Rect(0f, 0f, viewportWidth, 84f), Texture2D.whiteTexture);
            GUI.color = previousGuiColor;
            GUI.Label(new Rect(0f, 14f, viewportWidth, 42f), "NEON CLASH // WORLD CIRCUIT", title);
            GUI.Label(new Rect(0f, 55f, viewportWidth, 24f), "SELECT FIGHTERS  •  F11 FULLSCREEN", centred);
            if (GUI.Button(new Rect(viewportWidth - 190f, 16f, 174f, 38f), "EXIT TO DESKTOP")) QuitToDesktop();

            float width = Mathf.Min(1020f, viewportWidth - 36f);
            Rect panel = new Rect((viewportWidth - width) * 0.5f, 88f, width, Mathf.Max(440f, viewportHeight - 112f));
            GUI.color = new Color(0.08f, 0.10f, 0.17f, 0.88f);
            GUI.Box(panel, string.Empty);
            GUI.color = previousGuiColor;
            GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 36f, panel.height - 28f));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("PLAYER ONE", centred);
            DrawRoster(ref playerIndex);
            GUILayout.Space(10f);
            GUILayout.Label(cpuEnabled ? "CPU RIVAL" : "LOCAL PLAYER TWO", centred);
            DrawRoster(ref rivalIndex);
            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("STAGE ◀", GUILayout.Height(34f))) ChangeStage(-1);
            StageDefinition stage = catalog.Stages[stageIndex];
            GUILayout.Label(stage.DisplayName + " // " + stage.City + "\n" + stage.Descriptor, centred, GUILayout.Height(42f), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("STAGE ▶", GUILayout.Height(34f))) ChangeStage(1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawCostume("P1", ref playerCostumeIndex);
            DrawCostume("P2", ref rivalCostumeIndex);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            cpuEnabled = GUILayout.Toggle(cpuEnabled, "CPU OPPONENT");
            touchControls = GUILayout.Toggle(touchControls, "TOUCH OVERLAY");
            GUILayout.Label("DIFFICULTY", GUILayout.Width(82f));
            if (GUILayout.Button(difficulty.ToString().ToUpperInvariant(), GUILayout.Width(90f))) difficulty = (CpuDifficulty)(((int)difficulty + 1) % 3);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("MASTER VOLUME", GUILayout.Width(118f));
            float nextVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f, GUILayout.ExpandWidth(true));
            if (!Mathf.Approximately(nextVolume, masterVolume))
            {
                masterVolume = nextVolume;
                AudioListener.volume = masterVolume;
                PlayerPrefs.SetFloat("master-volume", masterVolume);
            }
            GUILayout.Label(Mathf.RoundToInt(masterVolume * 100f) + "%", GUILayout.Width(48f));
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
            if (GUILayout.Button("ENTER LOCAL CIRCUIT", GUILayout.Height(44f))) StartFight();
            GUILayout.Space(12f);
            GUILayout.Label("PROTOCOL 2 ONLINE", centred);
            GUILayout.BeginHorizontal();
            GUILayout.Label("RELAY", GUILayout.Width(55f));
            relayUrl = GUILayout.TextField(relayUrl, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(onlineBusy ? "PLEASE WAIT…" : "HOST", GUILayout.Height(36f))) CreateOnlineSession();
            joinCode = GUILayout.TextField(joinCode.ToUpperInvariant(), 6, GUILayout.Width(92f), GUILayout.Height(36f));
            if (GUILayout.Button("JOIN", GUILayout.Height(36f))) JoinOnlineSession(false);
            if (GUILayout.Button("WATCH", GUILayout.Height(36f))) JoinOnlineSession(true);
            GUILayout.EndHorizontal();
            GUILayout.Label(onlineStatus, centred);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawRoster(ref int selected)
        {
            const int columns = 5;
            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    FighterDefinition fighter = catalog.Fighters[index];
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = selected == index ? fighter.PrimaryColor : Color.white;
                    if (GUILayout.Button(fighter.Mark + "  " + fighter.DisplayName + "\n" + fighter.Style, GUILayout.Height(48f))) selected = index;
                    GUI.backgroundColor = old;
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawCostume(string prefix, ref int index)
        {
            if (GUILayout.Button(prefix + " COSTUME: " + catalog.Costumes[index].DisplayName, GUILayout.Height(32f))) index = Wrap(index + 1, catalog.Costumes.Length);
        }

        private void BuildStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 42, fontStyle = FontStyle.Bold };
            title.normal.textColor = Color.white;
            centred = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            centred.normal.textColor = new Color(0.82f, 0.93f, 1f);
        }

        private void QuitToDesktop()
        {
            PlayerPrefs.Save();
            Application.Quit();
        }

        private static int Wrap(int value, int count) { return (value % count + count) % count; }

        private void ChangeStage(int direction)
        {
            stageIndex = Wrap(stageIndex + direction, catalog.Stages.Length);
            NeonClashBootstrap.CreateStage(catalog.Stages[stageIndex]);
        }

        private void SaveRelayUrl()
        {
            PlayerPrefs.SetString("relay-url", relayUrl.Trim());
            PlayerPrefs.Save();
        }

        private FighterDefinition FindFighter(string id)
        {
            for (int i = 0; i < catalog.Fighters.Length; i++) if (catalog.Fighters[i].FighterId == id) return catalog.Fighters[i];
            throw new InvalidOperationException("Relay selected unknown fighter " + id + ".");
        }

        private StageDefinition FindStage(string id)
        {
            for (int i = 0; i < catalog.Stages.Length; i++) if (catalog.Stages[i].StageId == id) return catalog.Stages[i];
            throw new InvalidOperationException("Relay selected unknown stage " + id + ".");
        }

        private CostumeDefinition FindCostume(string id)
        {
            for (int i = 0; i < catalog.Costumes.Length; i++) if (catalog.Costumes[i].CostumeId == id) return catalog.Costumes[i];
            throw new InvalidOperationException("Relay selected unknown costume " + id + ".");
        }
    }
}
