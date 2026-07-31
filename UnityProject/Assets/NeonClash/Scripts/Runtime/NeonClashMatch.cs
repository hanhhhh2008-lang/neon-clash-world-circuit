using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonClash
{
    public sealed class NeonClashMatch : MonoBehaviour
    {
        private const float GroundWorldY = -3.05f;
        private FighterDefinition firstDefinition;
        private FighterDefinition secondDefinition;
        private NeonClashFrontEnd frontEnd;
        private DeterministicMatchSimulation simulation;
        private KeyboardInputBuffer firstInput;
        private KeyboardInputBuffer secondInput;
        private FighterPresentation firstView;
        private FighterPresentation secondView;
        private readonly SpriteRenderer[] projectileViews = new SpriteRenderer[8];
        private bool initialized;
        private bool cpuEnabled = true;
        private bool touchControls;
        private RollbackNetworkCoordinator onlineCoordinator;
        private WebSocketRollbackTransport onlineTransport;
        private ProtocolAssignmentV2 onlineAssignment;
        private SpectatorPlayback spectatorPlayback;
        private CancellationTokenSource onlineLifetime;
        private string networkError;
        private CpuDifficulty difficulty = CpuDifficulty.Pro;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle centredStyle;

        public void Initialize(FighterDefinition playerOne, FighterDefinition playerTwo)
        {
            Initialize(null, playerOne, playerTwo, null, null, null, true, CpuDifficulty.Pro, false);
        }

        public void Initialize(NeonClashFrontEnd owner, FighterDefinition playerOne, FighterDefinition playerTwo, StageDefinition stage,
            CostumeDefinition playerOneCostume, CostumeDefinition playerTwoCostume, bool useCpu, CpuDifficulty cpuDifficulty, bool showTouchControls)
        {
            frontEnd = owner;
            firstDefinition = playerOne;
            secondDefinition = playerTwo;
            cpuEnabled = useCpu;
            difficulty = cpuDifficulty;
            touchControls = showTouchControls;
            firstInput = new KeyboardInputBuffer(false, showTouchControls);
            secondInput = new KeyboardInputBuffer(true);
            simulation = new DeterministicMatchSimulation(playerOne.ToTuning(), playerTwo.ToTuning());
            firstView = FighterPresentation.Create(playerOne, playerOneCostume, "P1 - " + playerOne.DisplayName, 50);
            secondView = FighterPresentation.Create(playerTwo, playerTwoCostume, "P2 - " + playerTwo.DisplayName, 100);
            for (int i = 0; i < projectileViews.Length; i++)
            {
                projectileViews[i] = NeonArtFactory.CreateFlat("Projectile " + i, transform, NeonShape.Disc, Color.white, 160 + i, Vector3.zero, new Vector2(0.55f, 0.55f));
                projectileViews[i].enabled = false;
            }
            initialized = true;
            PresentFighters();
        }

        public void InitializeOnline(NeonClashFrontEnd owner, FighterDefinition playerOne, FighterDefinition playerTwo, StageDefinition stage,
            CostumeDefinition playerOneCostume, CostumeDefinition playerTwoCostume, ProtocolAssignmentV2 assignment, WebSocketRollbackTransport transport,
            bool showTouchControls)
        {
            if (assignment == null || assignment.playerIndex < 0 || assignment.playerIndex > 1) throw new ArgumentException("Online player assignment is invalid.");
            if (transport == null || !transport.IsConnected) throw new ArgumentException("Online transport must be connected before the match starts.");
            Initialize(owner, playerOne, playerTwo, stage, playerOneCostume, playerTwoCostume, false, CpuDifficulty.Pro, showTouchControls);
            onlineAssignment = assignment;
            onlineTransport = transport;
            onlineLifetime = new CancellationTokenSource();
            RollbackSession rollback = new RollbackSession(simulation, assignment.playerIndex, assignment.inputDelay, assignment.maxRollbackTicks);
            onlineCoordinator = new RollbackNetworkCoordinator(assignment.sessionId,
                assignment.role == "host" ? NetworkRole.Host : NetworkRole.Guest, assignment.playerIndex, rollback);
        }

        public void InitializeSpectator(NeonClashFrontEnd owner, FighterDefinition playerOne, FighterDefinition playerTwo, StageDefinition stage,
            CostumeDefinition playerOneCostume, CostumeDefinition playerTwoCostume, ProtocolAssignmentV2 assignment, WebSocketRollbackTransport transport)
        {
            if (assignment == null || assignment.role != "spectator" || assignment.playerIndex != -1) throw new ArgumentException("Spectator assignment is invalid.");
            if (transport == null || !transport.IsConnected) throw new ArgumentException("Spectator transport must be connected before playback starts.");
            Initialize(owner, playerOne, playerTwo, stage, playerOneCostume, playerTwoCostume, false, CpuDifficulty.Pro, false);
            onlineAssignment = assignment;
            onlineTransport = transport;
            onlineLifetime = new CancellationTokenSource();
            spectatorPlayback = new SpectatorPlayback(simulation);
        }

        public void Shutdown()
        {
            CloseNetwork();
            if (firstView != null) firstView.Remove();
            if (secondView != null) secondView.Remove();
            for (int i = 0; i < projectileViews.Length; i++) if (projectileViews[i] != null) Destroy(projectileViews[i].gameObject);
            initialized = false;
        }

        private void Update()
        {
            if (!initialized) return;
            firstInput.CaptureFrame();
            secondInput.CaptureFrame();
            if (Input.GetKeyDown(KeyCode.C)) cpuEnabled = !cpuEnabled;
            if (Input.GetKeyDown(KeyCode.R) && simulation.State.Phase == MatchPhase.MatchOver) simulation.ResetMatch();
            if (Input.GetKeyDown(KeyCode.Escape) && frontEnd != null) frontEnd.ReturnToSelection();
            PumpNetworkMessages();
        }

        private void FixedUpdate()
        {
            if (!initialized) return;
            FighterCommand firstCommand = new FighterCommand();
            FighterCommand secondCommand = new FighterCommand();
            if (simulation.State.Phase == MatchPhase.Fight)
            {
                firstCommand = firstInput.ConsumeTick();
                secondCommand = cpuEnabled
                    ? DeterministicCpu.Decide(simulation.State.Tick + 1, simulation.State.Second, simulation.State.First, difficulty)
                    : secondInput.ConsumeTick();
            }
            if (spectatorPlayback != null)
            {
                spectatorPlayback.AdvanceAvailable();
            }
            else if (onlineCoordinator != null)
            {
                SendNetwork(onlineCoordinator.Advance(firstCommand));
                ProtocolEnvelopeV2 checksum = onlineCoordinator.TryBuildChecksum();
                if (checksum != null) SendNetwork(checksum);
                if (onlineAssignment.role == "host" && simulation.State.Tick > 0 && simulation.State.Tick % 300 == 0)
                    SendNetwork(onlineCoordinator.BuildKeyframe());
            }
            else simulation.Step(firstCommand, secondCommand);
            UpdateProjectileViews();
            PresentFighters();
        }

        private void PresentFighters()
        {
            DeterministicMatchState state = simulation.State;
            bool firstWinner = state.Phase == MatchPhase.MatchOver ? state.FirstWins > state.SecondWins : state.Phase == MatchPhase.Knockout && state.RoundWinner == 0;
            bool secondWinner = state.Phase == MatchPhase.MatchOver ? state.SecondWins > state.FirstWins : state.Phase == MatchPhase.Knockout && state.RoundWinner == 1;
            if (firstView != null) firstView.Present(state.First, state.Tick, state.Phase, firstWinner);
            if (secondView != null) secondView.Present(state.Second, state.Tick, state.Phase, secondWinner);
        }

        private void UpdateProjectileViews()
        {
            for (int i = 0; i < simulation.State.Projectiles.Length; i++)
            {
                ProjectileState projectile = simulation.State.Projectiles[i];
                SpriteRenderer view = projectileViews[i];
                view.enabled = projectile.Active;
                if (projectile.Active)
                {
                    view.color = projectile.Owner == 0 ? firstDefinition.PrimaryColor : secondDefinition.PrimaryColor;
                    view.transform.position = new Vector3(projectile.PositionX / 1000f, GroundWorldY + projectile.PositionY / 1000f, 0f);
                }
            }
        }

        private void OnGUI()
        {
            if (!initialized) return;
            BuildStyles();
            DeterministicMatchState state = simulation.State;
            float margin = 28f;
            float barWidth = Mathf.Min(430f, Screen.width * 0.36f);
            DrawFighterHud(new Rect(margin, 24f, barWidth, 75f), firstDefinition, state.First, false);
            DrawFighterHud(new Rect(Screen.width - margin - barWidth, 24f, barWidth, 75f), secondDefinition, state.Second, true);

            GUI.Label(new Rect(0f, 20f, Screen.width, 36f), "NEON CLASH // UNITY FOUNDATION", centredStyle);
            GUI.Label(new Rect(0f, 52f, Screen.width, 28f), "ROUND " + state.Round + "   " + Mathf.CeilToInt(state.TimerTicks / (float)FighterSimulation.TickRate).ToString("00") + "   //   " + state.FirstWins + " - " + state.SecondWins, centredStyle);
            if (GUI.Button(new Rect(Screen.width - 178f, 18f, 158f, 38f), "EXIT TO DESKTOP"))
            {
                PlayerPrefs.Save();
                Application.Quit();
            }
            if (GUI.Button(new Rect(Screen.width - 178f, 62f, 158f, 30f), "SELECT SCREEN"))
            {
                if (frontEnd != null) frontEnd.ReturnToSelection();
            }
            GUI.Label(new Rect(0f, Screen.height - 62f, Screen.width, 28f), "A/D MOVE   W JUMP   S CROUCH   SPACE GUARD   T/Y PUNCH   U/K KICK   L SPECIAL   O IMPACT", centredStyle);
            string mode = onlineAssignment != null
                ? (spectatorPlayback != null ? "ONLINE SPECTATOR   BUFFER " + spectatorPlayback.BufferedTicks
                    : "ONLINE " + onlineAssignment.role.ToUpperInvariant() + "   RB " + onlineCoordinator.Rollback.RollbackCount + " / " + onlineCoordinator.Rollback.TotalReplayedTicks)
                : (cpuEnabled ? difficulty.ToString().ToUpperInvariant() + " CPU" : "LOCAL P2");
            GUI.Label(new Rect(0f, Screen.height - 35f, Screen.width - 190f, 24f), "ESC: SELECT   F11: FULLSCREEN   •   " + mode, centredStyle);
            if (!string.IsNullOrWhiteSpace(networkError))
                GUI.Label(new Rect(0f, Screen.height * 0.72f, Screen.width, 26f), "NETWORK: " + networkError, centredStyle);

            if (touchControls) DrawTouchOverlay();

            if (state.Tick - state.LastHitTick < 14)
                GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, 48f), "IMPACT", titleStyle);
            if (state.Phase == MatchPhase.Intro)
                GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 48f), state.PhaseTicks > 35 ? "ROUND " + state.Round : "FIGHT", titleStyle);
            if (state.Phase == MatchPhase.Knockout)
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 48f), "K.O.", titleStyle);
            if (state.Phase == MatchPhase.MatchOver)
            {
                string winner = state.FirstWins > state.SecondWins ? firstDefinition.DisplayName : secondDefinition.DisplayName;
                GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.45f - 55f, 360f, 110f), string.Empty);
                GUI.Label(new Rect(0f, Screen.height * 0.45f - 34f, Screen.width, 42f), winner + " WINS", titleStyle);
                GUI.Label(new Rect(0f, Screen.height * 0.45f + 12f, Screen.width, 24f), "PRESS R TO RESET", centredStyle);
            }
        }

        private void DrawTouchOverlay()
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.Box(new Rect(10f, Screen.height * 0.45f, Screen.width * 0.14f, Screen.height * 0.25f), "LEFT");
            GUI.Box(new Rect(Screen.width * 0.16f, Screen.height * 0.45f, Screen.width * 0.14f, Screen.height * 0.25f), "RIGHT");
            string[] labels = { "LP", "HP", "SP", "LK", "HK", "DI" };
            for (int i = 0; i < labels.Length; i++)
            {
                int row = i / 3; int column = i % 3;
                GUI.Box(new Rect(Screen.width * (0.52f + column * 0.16f), Screen.height * (0.43f + row * 0.18f), Screen.width * 0.14f, Screen.height * 0.15f), labels[i]);
            }
            GUI.color = previous;
        }

        private void DrawFighterHud(Rect area, FighterDefinition definition, FighterState state, bool alignRight)
        {
            GUI.Label(new Rect(area.x, area.y, area.width, 24f), definition.DisplayName + " // " + definition.Alias, alignRight ? labelStyle : GUI.skin.label);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);
            GUI.Box(new Rect(area.x, area.y + 28f, area.width, 18f), string.Empty);
            GUI.color = definition.PrimaryColor;
            float healthWidth = area.width * state.Health / FighterSimulation.MaxHealth;
            float healthX = alignRight ? area.x + area.width - healthWidth : area.x;
            GUI.DrawTexture(new Rect(healthX, area.y + 29f, healthWidth, 16f), Texture2D.whiteTexture);
            GUI.color = definition.SecondaryColor;
            float driveWidth = area.width * state.Drive / FighterSimulation.MaxDrive;
            float driveX = alignRight ? area.x + area.width - driveWidth : area.x;
            GUI.DrawTexture(new Rect(driveX, area.y + 51f, driveWidth, 7f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(area.x, area.y + 59f, area.width, 18f), definition.SpecialName + " // " + definition.ComboSequence, alignRight ? labelStyle : GUI.skin.label);
        }

        private void BuildStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 18f, 38f, 64f));
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleRight;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;
            centredStyle = new GUIStyle(GUI.skin.label);
            centredStyle.alignment = TextAnchor.MiddleCenter;
            centredStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 55f, 15f, 24f));
            centredStyle.fontStyle = FontStyle.Bold;
            centredStyle.normal.textColor = new Color(0.84f, 0.94f, 1f);
        }

        private void PumpNetworkMessages()
        {
            if (onlineTransport == null) return;
            ProtocolEnvelopeV2 envelope;
            try
            {
                while (onlineTransport.TryDequeue(out envelope))
                {
                    if (spectatorPlayback != null)
                    {
                        if (envelope.sessionId != onlineAssignment.sessionId) throw new ArgumentException("Spectator packet targets another session.");
                        if (envelope.kind == "keyframe") spectatorPlayback.ApplyKeyframe(envelope.keyframe);
                        else if (envelope.kind == "input") spectatorPlayback.SubmitInput(envelope.input);
                        else if (envelope.kind == "error") throw new InvalidOperationException(envelope.error ?? "Relay reported an error.");
                        continue;
                    }
                    ProtocolEnvelopeV2 response = onlineCoordinator.Handle(envelope);
                    if (response != null) SendNetwork(response);
                }
                if (!string.IsNullOrWhiteSpace(onlineTransport.LastError)) networkError = onlineTransport.LastError;
            }
            catch (Exception error) { networkError = error.Message; }
        }

        private async void SendNetwork(ProtocolEnvelopeV2 envelope)
        {
            if (onlineTransport == null || envelope == null || onlineLifetime == null) return;
            try { await onlineTransport.SendAsync(envelope, onlineLifetime.Token); }
            catch (OperationCanceledException) { }
            catch (Exception error) { networkError = error.Message; }
        }

        private async void CloseNetwork()
        {
            if (onlineLifetime == null) return;
            CancellationTokenSource lifetime = onlineLifetime;
            WebSocketRollbackTransport transport = onlineTransport;
            onlineLifetime = null;
            onlineTransport = null;
            lifetime.Cancel();
            if (transport != null)
            {
                try { await transport.CloseAsync(CancellationToken.None); } catch { }
                transport.Dispose();
            }
            lifetime.Dispose();
        }

    }
}
