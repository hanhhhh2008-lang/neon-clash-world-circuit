# Quality target: KOF XIV-inspired vertical slice

The target is a polished, original 2D fighter vertical slice that approaches the clarity, responsiveness, presentation discipline, and usability expected from a KOF XIV-era console fighter. This is a quality bar and product direction—not a claim of matching SNK's proprietary roster, art, animation budget, audio, or production scale.

## Done for this increment

- Readable full-body painted fighter art for all 10 original fighters, with three costume treatments and a persistent painted arena backdrop.
- Deterministic 60 Hz movement/combat, hit/block/knockback, projectiles, combos, drive actions, round flow, CPU, local two-player, keyboard/controller/touch input, rollback, checksum recovery, and delayed spectators.
- Responsive selection UI, larger entry controls, visible desktop exit/select controls, Escape routing, F11/Alt+Enter fullscreen toggling, and a fresh universal macOS build.
- Original asset provenance and visual-QA captures; no paid or third-party character/stage assets.

## Acceptance gates before calling the target complete

1. Playtest every fighter/stage/costume combination at 60 FPS on supported desktop and touch sizes.
2. Add authored multi-frame move animation, hitstop, cancel windows, throws, air attacks, richer VFX, audio, portraits, accessibility, and controller rebinding.
3. Run automated replays plus latency/loss/reconnect/host-loss/spectator soak tests across two real devices.
4. Deploy the protocol-2 relay into an owned non-production environment, set origin/identity/telemetry/retention policies, and perform security/load review.
5. Package release builds for agreed platforms, document known limits, and complete a human visual/gameplay sign-off.

The current Unity project is a playable and validated migration increment toward this target. The legacy React/Canvas/Cloudflare/D1 implementation remains the untouched reference.
