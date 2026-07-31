using System;
using System.Collections.Generic;
using NeonClash;

internal static class Program
{
    private const int KaelSpeed = 9;
    private const int KaelPower = 6;
    private const int KaelReach = 6;
    private const int ZaraSpeed = 8;
    private const int ZaraPower = 7;
    private const int ZaraReach = 5;

    private static int Main()
    {
        try
        {
            ValidateMovement();
            ValidateCombat();
            ValidateCombatGeometry();
            ValidateSignatureCombo();
            ValidateProjectile();
            ValidateCompleteMatchSnapshot();
            ValidateRollbackConvergence();
            int hashA = SimulateAndHash();
            int hashB = SimulateAndHash();
            Require(hashA == hashB, "The same command stream must produce the same state hash.");
            Console.WriteLine("NEON_CLASH_SIMULATION_VALIDATION_PASSED hash=" + hashA);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("NEON_CLASH_SIMULATION_VALIDATION_FAILED: " + exception.Message);
            return 1;
        }
    }

    private static void ValidateMovement()
    {
        FighterState state = FighterSimulation.CreateInitialState(0, 1);
        FighterCommand move = new FighterCommand { Move = 1 };
        for (int tick = 0; tick < 60; tick++) FighterSimulation.Step(ref state, move, KaelSpeed);
        Require(state.PositionX > 1000, "Kael should move right during one second of input.");
        Require(state.PositionX <= FighterSimulation.ArenaHalfWidth, "Movement must respect the stage boundary.");

        FighterSimulation.Step(ref state, new FighterCommand { Buttons = FighterButtons.Jump }, KaelSpeed);
        Require(!state.Grounded && state.PositionY > 0, "Jump should leave the ground.");
        for (int tick = 0; tick < 180; tick++) FighterSimulation.Step(ref state, new FighterCommand(), KaelSpeed);
        Require(state.Grounded && state.PositionY == 0, "Gravity should return Kael to the ground.");
    }

    private static void ValidateCombat()
    {
        int fullDamage = ResolvePunch(false);
        int blockedDamage = ResolvePunch(true);
        Require(fullDamage > 0, "An in-range light punch should deal damage.");
        Require(blockedDamage > 0 && blockedDamage < fullDamage, "Guard should reduce but not erase chip damage.");

        FighterState impactUser = FighterSimulation.CreateInitialState(0, 1);
        impactUser.Drive = 31000;
        FighterSimulation.Step(ref impactUser, new FighterCommand { Buttons = FighterButtons.Impact }, KaelSpeed);
        Require(impactUser.Action == CombatAction.None, "Drive impact must not start without its 32,000 drive cost.");
    }

    private static void ValidateCombatGeometry()
    {
        FighterState attacker = FighterSimulation.CreateInitialState(-3000, 1);
        FighterState defender = FighterSimulation.CreateInitialState(3000, -1);
        FighterCommand punch = new FighterCommand { Buttons = FighterButtons.LightPunch };
        for (int tick = 0; tick < 8; tick++)
        {
            FighterSimulation.Step(ref attacker, tick == 0 ? punch : new FighterCommand(), KaelSpeed);
            FighterSimulation.Step(ref defender, new FighterCommand(), ZaraSpeed);
            Require(!FighterSimulation.TryResolveHit(ref attacker, ref defender, KaelPower, KaelReach), "Distant hitboxes must not connect.");
        }
        CombatBox standing = FighterSimulation.GetHurtBox(defender);
        defender.Crouching = true;
        CombatBox crouching = FighterSimulation.GetHurtBox(defender);
        Require(crouching.MaximumY < standing.MaximumY, "Crouching must reduce the deterministic hurtbox height.");
    }

    private static void ValidateSignatureCombo()
    {
        ComboSequenceTracker tracker = new ComboSequenceTracker();
        FighterButtons[] sequence = { FighterButtons.LightPunch, FighterButtons.LightPunch, FighterButtons.LightKick, FighterButtons.Special };
        bool matched = false;
        for (int i = 0; i < sequence.Length; i++)
            matched = tracker.RecordAndMatch(new FighterCommand { Buttons = sequence[i] }, 10 + i * 8, "T,T,U,L");
        Require(matched, "Kael's migrated signature sequence must be recognized inside the input window.");
    }

    private static void ValidateProjectile()
    {
        FighterState attacker = FighterSimulation.CreateInitialState(-900, 1);
        FighterState defender = FighterSimulation.CreateInitialState(900, -1);
        ProjectileState projectile = new ProjectileState { Active = true, Owner = 0, PositionX = 700, PositionY = 1100, Damage = 15000 };
        Require(FighterSimulation.ApplyProjectileHit(ref attacker, ref defender, projectile), "A projectile box overlapping the defender must connect.");
        Require(defender.Health == FighterSimulation.MaxHealth - 15000, "Projectile damage must be deterministic.");
    }

    private static int ResolvePunch(bool guard)
    {
        FighterState attacker = FighterSimulation.CreateInitialState(-600, 1);
        FighterState defender = FighterSimulation.CreateInitialState(600, -1);
        for (int tick = 0; tick < 8; tick++)
        {
            FighterCommand attack = new FighterCommand();
            if (tick == 0) attack.Buttons = FighterButtons.LightPunch;
            FighterCommand defence = new FighterCommand();
            if (guard) defence.Buttons = FighterButtons.Guard;
            FighterSimulation.Step(ref attacker, attack, KaelSpeed);
            FighterSimulation.Step(ref defender, defence, ZaraSpeed);
            FighterSimulation.TryResolveHit(ref attacker, ref defender, KaelPower, KaelReach);
        }
        return FighterSimulation.MaxHealth - defender.Health;
    }

    private static int SimulateAndHash()
    {
        FighterState first = FighterSimulation.CreateInitialState(-2500, 1);
        FighterState second = FighterSimulation.CreateInitialState(2500, -1);
        for (int tick = 1; tick <= 600; tick++)
        {
            FighterCommand firstCommand = new FighterCommand { Move = tick < 100 ? 1 : 0 };
            if (tick % 87 == 0) firstCommand.Buttons = FighterButtons.LightKick;
            FighterCommand secondCommand = DeterministicCpu.Decide(tick, second, first);
            FighterSimulation.FaceEachOther(ref first, ref second);
            FighterSimulation.Step(ref first, firstCommand, KaelSpeed);
            FighterSimulation.Step(ref second, secondCommand, ZaraSpeed);
            FighterSimulation.FaceEachOther(ref first, ref second);
            FighterSimulation.TryResolveHit(ref first, ref second, KaelPower, KaelReach);
            FighterSimulation.TryResolveHit(ref second, ref first, ZaraPower, ZaraReach);
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

    private static void ValidateCompleteMatchSnapshot()
    {
        DeterministicMatchSimulation match = CreateMatch();
        for (int tick = 0; tick < 420; tick++)
            match.Step(CommandForPlayer(0, tick), CommandForPlayer(1, tick));

        DeterministicMatchState snapshot = match.CaptureState();
        uint before = DeterministicMatchSimulation.ComputeChecksum(snapshot);
        for (int tick = 420; tick < 510; tick++)
            match.Step(CommandForPlayer(0, tick), CommandForPlayer(1, tick));
        match.RestoreState(snapshot);
        uint restored = DeterministicMatchSimulation.ComputeChecksum(match.CaptureState());
        Require(before == restored, "A complete match snapshot must restore every checksum field exactly.");

        DeterministicMatchSimulation repeat = CreateMatch();
        for (int tick = 0; tick < 420; tick++)
            repeat.Step(CommandForPlayer(0, tick), CommandForPlayer(1, tick));
        Require(before == DeterministicMatchSimulation.ComputeChecksum(repeat.CaptureState()),
            "The complete match simulation must repeat bit-for-bit for the same commands.");
    }

    private static void ValidateRollbackConvergence()
    {
        RollbackSession firstPeer = new RollbackSession(CreateMatch(), 0);
        RollbackSession secondPeer = new RollbackSession(CreateMatch(), 1);
        List<PendingInput> toFirst = new List<PendingInput>();
        List<PendingInput> toSecond = new List<PendingInput>();

        for (int captureTick = 0; captureTick < 720; captureTick++)
        {
            FighterCommand first = captureTick < 620 ? CommandForPlayer(0, captureTick) : new FighterCommand();
            FighterCommand second = captureTick < 620 ? CommandForPlayer(1, captureTick) : new FighterCommand();
            NetworkInputFrame firstFrame = firstPeer.Advance(first);
            NetworkInputFrame secondFrame = secondPeer.Advance(second);

            // Deterministic jitter deliberately causes prediction and out-of-order delivery.
            toSecond.Add(new PendingInput(firstFrame, captureTick + 1 + ((captureTick * 7) % 6)));
            toFirst.Add(new PendingInput(secondFrame, captureTick + 1 + ((captureTick * 11) % 6)));
            DeliverDue(toFirst, captureTick, firstPeer);
            DeliverDue(toSecond, captureTick, secondPeer);
        }

        DeliverAll(toFirst, firstPeer);
        DeliverAll(toSecond, secondPeer);
        uint firstChecksum = DeterministicMatchSimulation.ComputeChecksum(firstPeer.Simulation.CaptureState());
        uint secondChecksum = DeterministicMatchSimulation.ComputeChecksum(secondPeer.Simulation.CaptureState());
        Require(firstPeer.RollbackCount > 0 && secondPeer.RollbackCount > 0,
            "Both peers must exercise rollback under delayed input.");
        Require(firstChecksum == secondChecksum,
            "Peers must converge after all delayed and reordered inputs arrive.");

        NetworkChecksumFrame frame;
        Require(firstPeer.TryBuildChecksum(out frame) && secondPeer.MatchesChecksum(frame),
            "Converged peers must accept the periodic checksum frame.");

        RollbackResult tooLate = firstPeer.SubmitRemoteInput(new NetworkInputFrame
        {
            Tick = firstPeer.Simulation.State.Tick - firstPeer.MaxRollbackTicks - 1,
            Command = new FighterCommand { Buttons = FighterButtons.HeavyPunch }
        });
        Require(tooLate.RequiresResync, "An input beyond the bounded rollback window must request authoritative recovery.");
    }

    private static DeterministicMatchSimulation CreateMatch()
    {
        return new DeterministicMatchSimulation(
            new FighterTuning { Speed = KaelSpeed, Power = KaelPower, Reach = KaelReach, UsesProjectileSpecial = false, ComboSequence = "T,T,U,L" },
            new FighterTuning { Speed = ZaraSpeed, Power = ZaraPower, Reach = ZaraReach, UsesProjectileSpecial = true, ComboSequence = "Y,Y,I,R" });
    }

    private static FighterCommand CommandForPlayer(int player, int tick)
    {
        FighterCommand command = new FighterCommand();
        int cycle = tick % (player == 0 ? 113 : 127);
        command.Move = cycle < 42 ? (player == 0 ? 1 : -1) : cycle < 58 ? 0 : (player == 0 ? -1 : 1);
        if (cycle == 10) command.Buttons = FighterButtons.Jump;
        else if (cycle == 50) command.Buttons = FighterButtons.LightPunch;
        else if (cycle == 66) command.Buttons = FighterButtons.LightKick;
        else if (cycle == 82) command.Buttons = FighterButtons.Special;
        else if (cycle >= 95 && cycle < 101) command.Buttons = FighterButtons.Guard;
        return command;
    }

    private static void DeliverDue(List<PendingInput> pending, int now, RollbackSession receiver)
    {
        // Reverse traversal makes messages sharing a delivery tick arrive out of order.
        for (int index = pending.Count - 1; index >= 0; index--)
        {
            if (pending[index].DeliveryTick > now) continue;
            receiver.SubmitRemoteInput(pending[index].Frame);
            pending.RemoveAt(index);
        }
    }

    private static void DeliverAll(List<PendingInput> pending, RollbackSession receiver)
    {
        pending.Sort((left, right) => right.Frame.Tick.CompareTo(left.Frame.Tick));
        for (int index = 0; index < pending.Count; index++) receiver.SubmitRemoteInput(pending[index].Frame);
        pending.Clear();
    }

    private struct PendingInput
    {
        public readonly NetworkInputFrame Frame;
        public readonly int DeliveryTick;

        public PendingInput(NetworkInputFrame frame, int deliveryTick)
        {
            Frame = frame;
            DeliveryTick = deliveryTick;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
