using System;
using System.Collections.Generic;

namespace NeonClash
{
    /// <summary>Maps validated protocol messages to rollback actions without owning any socket or scene object.</summary>
    public sealed class RollbackNetworkCoordinator
    {
        private readonly RollbackSession rollback;
        private readonly string sessionId;
        private readonly NetworkRole role;
        private readonly int localPlayerIndex;
        private int outboundSequence;
        private int lastInboundSequence = -1;

        public RollbackSession Rollback { get { return rollback; } }
        public int LastInboundSequence { get { return lastInboundSequence; } }

        public RollbackNetworkCoordinator(string id, NetworkRole networkRole, int playerIndex, RollbackSession session)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Session id is required.");
            if (networkRole == NetworkRole.Spectator) throw new ArgumentException("Spectators use SpectatorPlayback.");
            if (playerIndex < 0 || playerIndex > 1) throw new ArgumentOutOfRangeException("playerIndex");
            sessionId = id;
            role = networkRole;
            localPlayerIndex = playerIndex;
            rollback = session ?? throw new ArgumentNullException("session");
        }

        public ProtocolEnvelopeV2 Advance(FighterCommand localCommand)
        {
            NetworkInputFrame frame = rollback.Advance(localCommand);
            return Envelope("input", input: ProtocolInputV2.FromFrame(frame, localPlayerIndex));
        }

        public ProtocolEnvelopeV2 TryBuildChecksum()
        {
            NetworkChecksumFrame checksum;
            if (!rollback.TryBuildChecksum(out checksum)) return null;
            return Envelope("checksum", checksum: new ProtocolChecksumV2 { tick = checksum.Tick, checksum = RealtimeProtocolV2.ChecksumText(checksum.Checksum) });
        }

        public ProtocolEnvelopeV2 Handle(ProtocolEnvelopeV2 envelope)
        {
            if (envelope == null) throw new ArgumentNullException("envelope");
            if (envelope.sessionId != sessionId) throw new ArgumentException("Protocol message targets a different session.");
            if (envelope.sequence <= lastInboundSequence && envelope.kind != "input") return null;
            if (envelope.sequence > lastInboundSequence) lastInboundSequence = envelope.sequence;
            switch (envelope.kind)
            {
                case "input":
                    if (envelope.input.playerIndex == localPlayerIndex) return null;
                    RollbackResult result = rollback.SubmitRemoteInput(envelope.input.ToFrame());
                    if (!result.RequiresResync) return null;
                    if (role == NetworkRole.Host) return BuildKeyframe();
                    string guestHash = RealtimeProtocolV2.ChecksumText(DeterministicMatchSimulation.ComputeChecksum(rollback.Simulation.CaptureState()));
                    return Envelope("desync", desync: new ProtocolDesyncV2
                    {
                        tick = rollback.Simulation.State.Tick, hostChecksum = "00000000", guestChecksum = guestHash, requestKeyframe = true
                    });
                case "checksum":
                    NetworkChecksumFrame remote = new NetworkChecksumFrame { Tick = envelope.checksum.tick, Checksum = RealtimeProtocolV2.ParseChecksum(envelope.checksum.checksum) };
                    if (rollback.MatchesChecksum(remote)) return null;
                    return role == NetworkRole.Host ? BuildKeyframe() : null;
                case "desync":
                    return role == NetworkRole.Host && envelope.desync.requestKeyframe ? BuildKeyframe() : null;
                case "keyframe":
                    if (role == NetworkRole.Guest)
                        rollback.ApplyAuthoritativeKeyframe(envelope.keyframe.state, RealtimeProtocolV2.ParseChecksum(envelope.keyframe.checksum));
                    return null;
                case "ping": return Envelope("pong");
                case "peer-joined": case "peer-left": case "pong": return null;
                case "error": throw new InvalidOperationException(envelope.error ?? "Relay reported an error.");
                default: throw new ArgumentException("Unexpected match protocol message " + envelope.kind + ".");
            }
        }

        public ProtocolEnvelopeV2 BuildKeyframe()
        {
            DeterministicMatchState state = rollback.Simulation.CaptureState();
            uint checksum = DeterministicMatchSimulation.ComputeChecksum(state);
            return Envelope("keyframe", keyframe: new ProtocolKeyframeV2 { tick = state.Tick, state = state, checksum = RealtimeProtocolV2.ChecksumText(checksum) });
        }

        private ProtocolEnvelopeV2 Envelope(string kind, ProtocolInputV2 input = null, ProtocolChecksumV2 checksum = null,
            ProtocolKeyframeV2 keyframe = null, ProtocolDesyncV2 desync = null)
        {
            return new ProtocolEnvelopeV2
            {
                kind = kind,
                sessionId = sessionId,
                sequence = ++outboundSequence,
                input = input,
                checksum = checksum,
                keyframe = keyframe,
                desync = desync
            };
        }
    }

    /// <summary>Delayed, non-predictive spectator playback from a keyframe plus both players' input frames.</summary>
    public sealed class SpectatorPlayback
    {
        public const int DefaultDelayTicks = 120;
        private readonly DeterministicMatchSimulation simulation;
        private readonly int delayTicks;
        private readonly SortedDictionary<int, FighterCommand> firstInputs = new SortedDictionary<int, FighterCommand>();
        private readonly SortedDictionary<int, FighterCommand> secondInputs = new SortedDictionary<int, FighterCommand>();
        private int latestNetworkTick;

        public DeterministicMatchSimulation Simulation { get { return simulation; } }
        public int BufferedTicks { get { return latestNetworkTick - simulation.State.Tick; } }

        public SpectatorPlayback(DeterministicMatchSimulation match, int delay = DefaultDelayTicks)
        {
            simulation = match ?? throw new ArgumentNullException("match");
            if (delay < 1 || delay > RollbackSession.HistoryCapacity) throw new ArgumentOutOfRangeException("delay");
            delayTicks = delay;
        }

        public void ApplyKeyframe(ProtocolKeyframeV2 keyframe)
        {
            uint checksum = RealtimeProtocolV2.ParseChecksum(keyframe.checksum);
            if (DeterministicMatchSimulation.ComputeChecksum(keyframe.state) != checksum) throw new ArgumentException("Spectator keyframe checksum is invalid.");
            simulation.RestoreState(keyframe.state);
            latestNetworkTick = keyframe.tick;
            firstInputs.Clear(); secondInputs.Clear();
        }

        public void SubmitInput(ProtocolInputV2 input)
        {
            if (input.playerIndex < 0 || input.playerIndex > 1 || input.tick <= simulation.State.Tick) return;
            FighterCommand command = input.ToFrame().Command;
            if (input.playerIndex == 0) firstInputs[input.tick] = command; else secondInputs[input.tick] = command;
            if (input.tick > latestNetworkTick) latestNetworkTick = input.tick;
        }

        public int AdvanceAvailable(int maximumSteps = 8)
        {
            int advanced = 0;
            while (advanced < maximumSteps && simulation.State.Tick < latestNetworkTick - delayTicks)
            {
                int next = simulation.State.Tick + 1;
                FighterCommand first, second;
                if (!firstInputs.TryGetValue(next, out first) || !secondInputs.TryGetValue(next, out second)) break;
                simulation.Step(first, second);
                firstInputs.Remove(next); secondInputs.Remove(next);
                advanced++;
            }
            return advanced;
        }
    }
}
