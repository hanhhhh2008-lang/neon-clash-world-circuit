# Neon Clash migration plan

## Boundary and current increment

The existing React 19 / Canvas 2D / Cloudflare implementation remains intact at the repository root and is the behavioral reference. Unity work is isolated under `UnityProject/`. This is a substantial playable Unity migration, not a claim that release production, service deployment, or final polish is complete.

Implemented now:

- Unity 6 project layout and one boot scene.
- All 10 source fighters, all 10 stages, and all 3 costumes as generated, validated ScriptableObject content with a catalog and repeatable migration generator.
- Fixed 60 Hz integer-state movement with jump, crouch, guard, six attack states, explicit hit/hurt boxes, signature sequence recognition, drive costs/regeneration, melee/projectiles, hit/block, hitstun, knockback, push separation, best-of-three rounds, timer, KO, and restart.
- Three deterministic CPU levels and a local two-player option.
- Fighter/stage/costume selection, persisted settings, HUD, keyboard, legacy-controller, and touch-overlay input.
- Original data-driven inked vector rigs for all fighters/costumes, 17 deterministic presentation states, and ten motif-specific animated vector arenas.
- Existing Cloudflare room-route transport, DTOs, and browser snapshot conversion behind an explicit compatibility boundary.
- Complete deterministic match snapshots, bounded rollback/prediction, checksum and host-keyframe recovery, delayed spectators, secure WebSocket transport, and a host/join/watch lobby path.
- An isolated protocol-2 Cloudflare Durable Object relay package with build/content negotiation, role credentials, validation, rate/tick/packet limits, checksum disagreement detection, recovery keyframes, and spectator fan-out.
- Editor/CLI validation entry points for catalog, scene wiring, art, protocol serialization/security, geometry, combos, projectiles, deterministic simulation, rollback convergence, and relay validation.

Not implemented yet:

- Hand-illustrated portrait/marketing art, facial detail, cloth deformation, secondary skeletal dynamics, frame-by-frame smear animation, audio, cinematic VFX, and final typography.
- Per-character frame data, cancel trees, throws, air attacks, hitstop/trades, training mode, accessibility review, and broad device/platform QA.
- Deployment configuration/account ownership, production authentication identity, reconnect UX/token rotation, structured telemetry/moderation/retention, adversarial testing, multi-region routing, relay load tests, and real-device latency/loss soak testing.

## Web-to-Unity map

| Web reference | Unity destination | Migration note |
| --- | --- | --- |
| `FIGHTERS` constants | `FighterDefinition` assets | Migrate data first; keep visuals and move tuning versioned separately. |
| `STAGES` constants and Canvas backgrounds | Stage definition assets + prefabs/scenes | Preserve original location concepts; replace procedural placeholder art with owned/licensed production art. |
| `OUTFITS` | Cosmetic definition assets + rig/skin variants | Cosmetic-only; never alter deterministic combat state. |
| `Combatant`, `attackData`, fixed Canvas update | Pure fixed-tick simulation assembly | Integer state is the authoritative layer; Unity objects present it and do not own rules. |
| Keyboard/touch input objects | Input adapters producing `FighterCommand` | Current legacy-key adapter is dependency-free; add Unity Input System action assets when platform/controller work begins. |
| Canvas sprite builder/drawing | Owned modular vector rigs, deterministic poses, arena builders, VFX and camera | Production-style first-party art is implemented; high-end authored polish remains. |
| Adaptive browser CPU | Deterministic command producer / later behavior layer | CPU must emit the same commands as local or network players. |
| React fighter/stage select and HUD | uGUI or UI Toolkit screens | Keep menu state outside the match simulation. |
| Host snapshot simulation | Protocol-2 bounded peer rollback through a relay/referee | Complete integer state snapshots/checksums and host recovery keyframes replace render snapshots. |
| Worker room API | Isolated Durable Object session/relay service | Expiring opaque secrets stay in memory; production identity, region, and operations remain deployment work. |
| Cloudflare D1 `rooms` / `spectators` | Server-side room metadata and durable match records | Transient tick state belongs in memory/relay, not a relational write on every tick. D1 may remain for discovery/history through a versioned HTTPS API. |

## Remaining sequence

1. Finish the competitive combat specification: cancel trees, hitstop/trades, throws, air attacks, input buffering, training/replay diagnostics, and per-character balance data.
2. Add authored audio/VFX, typography, portraits, accessibility settings, controller rebinding, and platform-specific touch polish.
3. Deploy protocol 2 into an owned non-production Cloudflare account, add identity/rotation/telemetry/retention, and execute real multi-device latency, loss, reconnect, host-loss, security, and load tests.
4. Resolve defects from soak tests, add platform builds/profiling, complete accessibility and compliance reviews, and establish release operations.

## Asset pipeline and policy

- No Asset Store, paid, downloaded, or third-party character resource is part of this foundation.
- Keep source art in an ownership-reviewed external source-art location; commit only approved game-ready exports and attribution/license records where applicable.
- Use one Unity pixels-per-unit convention, consistent pivots, named animation clips, sprite atlases, and platform import presets.
- Separate fighter identity/data from the presentation prefab so placeholder art can be swapped without rewriting combat.
- Add an `ASSET_PROVENANCE.md` entry before importing any non-original resource. Unknown provenance means do not import.
- Existing repository imagery may only be reused after its provenance and intended Unity redistribution rights are confirmed.
