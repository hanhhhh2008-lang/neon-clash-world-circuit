# Neon Clash production art direction

## Visual identity

Neon Clash uses an original **Circuit Noir** direction: graphic-novel cutout fighters, hard ink silhouettes, vivid two-color identities, charcoal/off-white utility layers, and luminous circuit effects against deep midnight arenas. The approach is designed to stay legible during fast combat, scale cleanly across desktop and mobile, and remain entirely reproducible without third-party art packages.

The reference board at `ArtSource/Concepts/neon-clash-roster-direction-v1.png` established the direction. The runtime now uses the source-controlled painted roster atlas and rooftop arena rather than exposing the cutout scaffolding as final art.

## Runtime asset pipeline

1. `ContentMigrationGenerator` owns the complete first-party content map. Every fighter has body-build, hair, garment, and energy-motif fields in addition to the migrated gameplay identity.
2. `PaintedFighterAtlas` crops ten original filled, cel-shaded full-body fighters from one transparent source-controlled atlas. The generated chroma source is retained under `ArtSource/Fighters` for provenance.
3. `FighterPresentation` keeps its jointed cutout rig hidden as deterministic pose scaffolding and a no-asset fallback. The painted silhouette is the visible body; state transforms, hit flash, energy and attack effects remain driven by fixed simulation ticks.
4. The three costumes are presentation-only variants:
   - **Circuit** — bright tournament kit with off-white technical panels.
   - **After Dark** — low-value, reduced-saturation stealth fabric.
   - **Heatwave** — brighter high-saturation palette with a waist flash and shortened long layers.
5. `ArenaBackdrop` supplies the original painted neon rooftop continuously, including behind the selection UI. `StagePresentation` adds each migrated arena motif as animated foreground/parallax detail.

## Deterministic animation contract

`FighterPresentation` never writes to combat state. It selects one of 17 presentation states solely from `FighterState`, `MatchPhase`, and the authoritative simulation tick:

- idle, advance, retreat;
- jump rise, jump fall, crouch, guard;
- light/heavy punch, light/heavy kick, special, drive impact;
- hit, stun, knockdown, finish.

Attack anticipation/contact/recovery transforms and effects use `ActionTick` and `AttackSpec.DurationTicks`. Walk and idle motion use the integer simulation tick. Rendering frame rate therefore cannot change hit timing, movement, or online replay results.

## Adding or replacing art

- Add first-party profile data through `ContentMigrationGenerator`; do not hand-edit generated assets.
- New runtime shapes belong in `NeonArtFactory` and must remain deterministic and dependency-free.
- A future authored sprite or skeletal renderer should implement the same state-selection contract and remain presentation-only.
- Add provenance to `ASSET_PROVENANCE.md` before importing any external image, font, animation, audio, shader, or package. Unknown provenance means do not import.

## Automated and visual QA

`ArtMilestoneValidator` instantiates every one of the 30 fighter/costume combinations, exercises all 17 states, rejects placeholder object names, verifies unique roster profiles, and checks all ten stages for scene detail and ambience animation. Its capture command renders the roster, state sheet, and every arena into `Documentation/VisualQA/` for human inspection.

## Current quality ceiling

The painted fighters and arena are original, cohesive playable visuals and no longer resemble exposed skeleton rigs. Each fighter currently has one painted combat key pose animated with deterministic whole-silhouette transforms and effects. Bespoke multi-frame limb animation, per-move painted frames, cloth deformation, smear frames, portrait art, audio, and cinematic finishers remain future authored work.
