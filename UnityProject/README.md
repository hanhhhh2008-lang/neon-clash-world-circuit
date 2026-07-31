# Neon Clash — Unity Foundation

This directory is a separate Unity 6 project. The React/Canvas game at the repository root remains the playable reference implementation and is not consumed or changed by this project.

## Open and play

1. Open this `UnityProject` directory with Unity `6000.4.7f1` (Unity 6.0).
2. Open `Assets/NeonClash/Scenes/VerticalSlice.unity` if it is not already open.
3. Press Play.

The selection screen exposes all 10 fighters, 10 stages, and 3 costumes defined by the web reference. Matches support a deterministic CPU, local player two, or the optional protocol-2 online flow. The visible roster uses an original painted full-body atlas over hidden deterministic pose scaffolding; every arena combines an original painted rooftop with motif-specific animated foregrounds. No third-party art dependency is required.

### Controls

| Input | Action |
| --- | --- |
| `A` / `D` | Move |
| `W` | Jump |
| `S` | Crouch |
| `Space` | Guard |
| `T` / `Y` | Light / heavy punch |
| `U` / `K` | Light / heavy kick |
| `L` | Special |
| `O` | Drive impact |
| `Escape` | Return to fighter selection in a match; quit from the selection screen |
| `F11` / `Alt+Enter` | Toggle fullscreen |
| On-screen `EXIT TO DESKTOP` | Quit the standalone game from either screen |
| On-screen `SELECT SCREEN` | Leave a match without closing the game |
| Controller | Left stick movement; face buttons attack; shoulder buttons guard/special |
| Touch | Optional movement, guard, and six-button overlay |

Local P2 uses arrow keys to move/jump/crouch, Right Shift to guard, and numpad `1`–`6` for attacks.

## Validation

From the repository root, when a valid local Unity license is available:

```bash
unity -batchmode -quit -projectPath "$PWD/UnityProject" \
  -executeMethod NeonClash.Editor.FoundationValidator.ValidateFromCommandLine \
  -logFile -
```

The validator checks the scene, complete 10-fighter/10-stage/3-costume catalog, all 30 rig/costume combinations, 17 animation states, all arena art builders, deterministic combat, combos, projectiles, both online boundaries, protocol-2 content hashing, JSON/keyframe round trips, and transport security rules. Unity also compiles all runtime/editor scripts during project import.

To regenerate the checked visual evidence:

```bash
unity -batchmode -quit -projectPath "$PWD/UnityProject" \
  -executeMethod NeonClash.Editor.ArtMilestoneValidator.CaptureVisualQaFromCommandLine \
  -logFile -
```

The Unity-independent integer simulation can be compiled and exercised using the compiler bundled with the configured Unity editor (no SDK/package download required):

```bash
bash UnityProject/Tools/SimulationValidation/validate-simulation.sh
```

`dotnet run --project UnityProject/Tools/SimulationValidation/SimulationValidation.csproj` is also supported when a .NET 8 SDK is installed.

See [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md) for system boundaries, asset policy, networking direction, and the remaining migration scope.

See [`ART_DIRECTION.md`](ART_DIRECTION.md) for the art pipeline, deterministic animation contract, and honest current quality ceiling. Rendered evidence is under [`Documentation/VisualQA`](Documentation/VisualQA).

See [`QUALITY_TARGET.md`](QUALITY_TARGET.md) for the KOF XIV-inspired quality bar, completed gates, and remaining release work.

The protocol-1 compatibility adapter and implemented protocol-2 rollback architecture are documented in [`NETWORKING.md`](NETWORKING.md). The isolated relay package and dependency-free tests are under [`Services/RelayWorker`](Services/RelayWorker). A production deployment URL is intentionally not embedded in the client.
