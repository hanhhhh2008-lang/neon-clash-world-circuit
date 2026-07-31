using System;
using System.Collections.Generic;

namespace NeonClash
{
    [Serializable]
    public struct NetworkInputFrame
    {
        public int Tick;
        public FighterCommand Command;
        public int AckRemoteTick;
    }

    [Serializable]
    public struct NetworkChecksumFrame
    {
        public int Tick;
        public uint Checksum;
    }

    public struct RollbackResult
    {
        public bool Replayed;
        public bool HardResync;
        public bool RequiresResync;
        public int ReplayedTicks;
    }

    /// <summary>
    /// GGPO-style input prediction and bounded rollback over the shared deterministic
    /// match simulation. Transport concerns are deliberately outside this class.
    /// </summary>
    public sealed class RollbackSession
    {
        public const int DefaultInputDelay = 2;
        public const int DefaultMaxRollbackTicks = 12;
        public const int HistoryCapacity = 240;
        public const int ChecksumInterval = 30;

        private static readonly FighterButtons PredictableHeldButtons = FighterButtons.Crouch | FighterButtons.Guard;
        private readonly DeterministicMatchSimulation simulation;
        private readonly int localPlayerIndex;
        private readonly int inputDelay;
        private readonly int maxRollbackTicks;
        private readonly SortedDictionary<int, FighterCommand> localInputs = new SortedDictionary<int, FighterCommand>();
        private readonly SortedDictionary<int, FighterCommand> remoteInputs = new SortedDictionary<int, FighterCommand>();
        private readonly SortedDictionary<int, FighterCommand> usedLocalInputs = new SortedDictionary<int, FighterCommand>();
        private readonly SortedDictionary<int, FighterCommand> usedRemoteInputs = new SortedDictionary<int, FighterCommand>();
        private readonly SortedDictionary<int, DeterministicMatchState> statesBeforeTick = new SortedDictionary<int, DeterministicMatchState>();
        private readonly SortedDictionary<int, DeterministicMatchState> statesAfterTick = new SortedDictionary<int, DeterministicMatchState>();
        private FighterCommand predictedRemote;
        private int latestRemoteTick = -1;

        public DeterministicMatchSimulation Simulation { get { return simulation; } }
        public int LatestRemoteTick { get { return latestRemoteTick; } }
        public int InputDelay { get { return inputDelay; } }
        public int MaxRollbackTicks { get { return maxRollbackTicks; } }
        public int RollbackCount { get; private set; }
        public int TotalReplayedTicks { get; private set; }
        public int HardResyncCount { get; private set; }

        public RollbackSession(DeterministicMatchSimulation match, int localIndex, int delay = DefaultInputDelay, int rollbackWindow = DefaultMaxRollbackTicks)
        {
            if (match == null) throw new ArgumentNullException("match");
            if (localIndex != 0 && localIndex != 1) throw new ArgumentOutOfRangeException("localIndex");
            if (delay < 0 || delay > 8) throw new ArgumentOutOfRangeException("delay");
            if (rollbackWindow < 2 || rollbackWindow >= HistoryCapacity) throw new ArgumentOutOfRangeException("rollbackWindow");
            simulation = match;
            localPlayerIndex = localIndex;
            inputDelay = delay;
            maxRollbackTicks = rollbackWindow;
            statesAfterTick[simulation.State.Tick] = simulation.CaptureState();
        }

        public NetworkInputFrame Advance(FighterCommand capturedLocalCommand)
        {
            int scheduledTick = simulation.State.Tick + inputDelay + 1;
            capturedLocalCommand = Sanitize(capturedLocalCommand);
            localInputs[scheduledTick] = capturedLocalCommand;
            int nextTick = simulation.State.Tick + 1;
            FighterCommand local = GetOrNeutral(localInputs, nextTick);
            FighterCommand remote;
            if (remoteInputs.TryGetValue(nextTick, out remote)) predictedRemote = PredictionContinuation(remote);
            else remote = predictedRemote;
            StepAt(nextTick, local, remote);
            PurgeHistory(simulation.State.Tick - HistoryCapacity);
            return new NetworkInputFrame { Tick = scheduledTick, Command = capturedLocalCommand, AckRemoteTick = latestRemoteTick };
        }

        public RollbackResult SubmitRemoteInput(NetworkInputFrame frame)
        {
            frame.Command = Sanitize(frame.Command);
            if (frame.Tick < 0 || frame.Tick > simulation.State.Tick + HistoryCapacity)
                throw new ArgumentOutOfRangeException("frame", "Remote input tick is outside the accepted window.");
            FighterCommand existing;
            bool changed = !remoteInputs.TryGetValue(frame.Tick, out existing) || !CommandsEqual(existing, frame.Command);
            remoteInputs[frame.Tick] = frame.Command;
            if (frame.Tick > latestRemoteTick) latestRemoteTick = frame.Tick;
            if (!changed || frame.Tick > simulation.State.Tick) return new RollbackResult();

            FighterCommand used;
            if (usedRemoteInputs.TryGetValue(frame.Tick, out used) && !CommandsEqual(used, frame.Command))
                return RollbackFrom(frame.Tick);
            if (!usedRemoteInputs.ContainsKey(frame.Tick)) return new RollbackResult { RequiresResync = true };
            return new RollbackResult();
        }

        public bool TryBuildChecksum(out NetworkChecksumFrame frame)
        {
            if (simulation.State.Tick <= 0 || simulation.State.Tick % ChecksumInterval != 0)
            {
                frame = new NetworkChecksumFrame();
                return false;
            }
            frame = new NetworkChecksumFrame { Tick = simulation.State.Tick, Checksum = DeterministicMatchSimulation.ComputeChecksum(simulation.CaptureState()) };
            return true;
        }

        public bool MatchesChecksum(NetworkChecksumFrame remote)
        {
            DeterministicMatchState state;
            if (!statesAfterTick.TryGetValue(remote.Tick, out state)) return false;
            return DeterministicMatchSimulation.ComputeChecksum(state) == remote.Checksum;
        }

        public RollbackResult ApplyAuthoritativeKeyframe(DeterministicMatchState state, uint checksum)
        {
            DeterministicMatchState copy = DeterministicMatchSimulation.CloneState(state);
            if (DeterministicMatchSimulation.ComputeChecksum(copy) != checksum)
                throw new ArgumentException("Authoritative keyframe checksum is invalid.");
            if (copy.Tick < simulation.State.Tick - HistoryCapacity || copy.Tick > simulation.State.Tick + 1)
                throw new ArgumentOutOfRangeException("state", "Keyframe tick is outside the accepted window.");
            simulation.RestoreState(copy);
            statesBeforeTick.Clear(); statesAfterTick.Clear(); usedLocalInputs.Clear(); usedRemoteInputs.Clear();
            statesAfterTick[copy.Tick] = simulation.CaptureState();
            predictedRemote = PredictionBefore(copy.Tick + 1);
            HardResyncCount++;
            return new RollbackResult { HardResync = true };
        }

        private RollbackResult RollbackFrom(int tick)
        {
            int currentTick = simulation.State.Tick;
            if (currentTick - tick + 1 > maxRollbackTicks)
                return new RollbackResult { RequiresResync = true };
            DeterministicMatchState before;
            if (!statesBeforeTick.TryGetValue(tick, out before)) return new RollbackResult { RequiresResync = true };
            simulation.RestoreState(before);
            predictedRemote = PredictionBefore(tick);
            int replayed = 0;
            for (int replayTick = tick; replayTick <= currentTick; replayTick++)
            {
                FighterCommand local = GetOrNeutral(localInputs, replayTick);
                FighterCommand remote;
                if (remoteInputs.TryGetValue(replayTick, out remote)) predictedRemote = PredictionContinuation(remote);
                else remote = predictedRemote;
                StepAt(replayTick, local, remote);
                replayed++;
            }
            RollbackCount++;
            TotalReplayedTicks += replayed;
            return new RollbackResult { Replayed = true, ReplayedTicks = replayed };
        }

        private void StepAt(int tick, FighterCommand local, FighterCommand remote)
        {
            statesBeforeTick[tick] = simulation.CaptureState();
            usedLocalInputs[tick] = local;
            usedRemoteInputs[tick] = remote;
            if (localPlayerIndex == 0) simulation.Step(local, remote);
            else simulation.Step(remote, local);
            if (simulation.State.Tick != tick) throw new InvalidOperationException("Deterministic simulation tick drifted during rollback.");
            statesAfterTick[tick] = simulation.CaptureState();
        }

        private FighterCommand PredictionBefore(int tick)
        {
            FighterCommand value = new FighterCommand();
            foreach (KeyValuePair<int, FighterCommand> item in remoteInputs)
            {
                if (item.Key >= tick) break;
                value = PredictionContinuation(item.Value);
            }
            return value;
        }

        private static FighterCommand PredictionContinuation(FighterCommand source)
        {
            return new FighterCommand { Move = source.Move, Buttons = source.Buttons & PredictableHeldButtons };
        }

        private static FighterCommand GetOrNeutral(SortedDictionary<int, FighterCommand> source, int tick)
        {
            FighterCommand value;
            return source.TryGetValue(tick, out value) ? value : new FighterCommand();
        }

        private static FighterCommand Sanitize(FighterCommand command)
        {
            if (command.Move < -1) command.Move = -1;
            if (command.Move > 1) command.Move = 1;
            command.Buttons &= FighterButtons.Jump | FighterButtons.Crouch | FighterButtons.Guard |
                FighterButtons.LightPunch | FighterButtons.HeavyPunch | FighterButtons.LightKick |
                FighterButtons.HeavyKick | FighterButtons.Special | FighterButtons.Impact;
            return command;
        }

        private static bool CommandsEqual(FighterCommand first, FighterCommand second)
        {
            return first.Move == second.Move && first.Buttons == second.Buttons;
        }

        private void PurgeHistory(int minimumTick)
        {
            Purge(localInputs, minimumTick); Purge(remoteInputs, minimumTick);
            Purge(usedLocalInputs, minimumTick); Purge(usedRemoteInputs, minimumTick);
            Purge(statesBeforeTick, minimumTick); Purge(statesAfterTick, minimumTick);
        }

        private static void Purge<T>(SortedDictionary<int, T> values, int minimumTick)
        {
            List<int> remove = new List<int>();
            foreach (int tick in values.Keys) { if (tick >= minimumTick) break; remove.Add(tick); }
            for (int i = 0; i < remove.Count; i++) values.Remove(remove[i]);
        }
    }
}
