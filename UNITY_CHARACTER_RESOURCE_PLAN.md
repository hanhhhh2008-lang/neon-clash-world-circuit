# Unity character upgrade plan

Research refreshed July 2026. The browser game uses original project-owned concept art and procedural combat sprites, so no third-party art has been redistributed.

## Best-fit resources

### 1. Character Creator 2D — best clothing and personality foundation

- Unity-oriented modular humanoid creator by Simpleton.
- Colorable equipment, body sliders, facial expressions, basic animations, runtime customization, prefab/JSON saving, and PNG export.
- Best use in Neon Clash: build the ten adult humanoids and the elder as layered character prefabs, then add original coats, masks, gauntlets, boots, and cultural design details as custom parts.
- Limitation: its base content is fantasy themed, so Neon Clash still needs original modern-tech clothing. A valid Asset Store license is required before importing or shipping its content.
- Source: https://simpleton.rocks/cc2d/

### 2. Unity 2D Animation + 2D PSD Importer — best official art pipeline

- Import layered `.psb`/`.psd` character art, generate sprites, rig bones, skin meshes, and animate a reusable character prefab.
- Best use in Neon Clash: separate each fighter into head, torso, coat tails, upper/lower arms, hands, upper/lower legs, feet, hair, and accessories. Keep special-effect layers separate.
- These are Unity packages rather than borrowed character identities, so they are the safest core workflow for original fighters.
- Sources: https://docs.unity3d.com/kr/Packages/com.unity.2d.psdimporter%4012.0/manual/index.html and https://docs.unity3d.com/ja/Packages/com.unity.2d.animation%4013.0/manual/ex-psd-importer.html

### 3. Universal Fighting Engine 2 — best optional combat framework

- Provides fighting-game editors, move authoring, AI, hitbox/frame control, 2D support, and rollback-ready features.
- Best use in a future Unity edition: replace only the low-level match engine while retaining Neon Clash's original roster, controls, lobby presentation, and art direction.
- It is a commercial framework; purchase and license approval are required. Do not copy its source or demo characters into this repository without a valid license.
- Source: https://www.ufe3d.com/

### 4. 2D Magic & Attack Effects — optional VFX reference

- A Unity Asset Store effect pack suitable for projectiles, impact bursts, and cinematic finishers.
- Use only after confirming the current package license and visual fit. The current game already has original code-driven particles, so this is optional.
- Source: https://marketplace.unity.com/packages/vfx/particles/2d-magic-attack-effects-97953

## Exact Unity hierarchy for each fighter

```text
Fighter_<Name>                          [Animator, Rigidbody2D, FighterController]
├── VisualRoot                          [Sprite Skin / generated PSB prefab]
│   ├── Body
│   ├── Clothing
│   ├── Hair
│   ├── Face
│   └── Accessories
├── Hitboxes
│   ├── BodyHurtbox                     [BoxCollider2D, Hurtbox]
│   ├── LightPunchHitbox                [BoxCollider2D, Hitbox; disabled by default]
│   ├── HeavyPunchHitbox                [BoxCollider2D, Hitbox; disabled by default]
│   ├── LightKickHitbox                 [BoxCollider2D, Hitbox; disabled by default]
│   └── HeavyKickHitbox                 [BoxCollider2D, Hitbox; disabled by default]
├── VFX_Spawn                           [empty transform]
├── Projectile_Spawn                    [empty transform]
└── GroundCheck                         [empty transform]
```

Attach the `Animator`, movement controller, and combat-state controller to the fighter root. Attach `Sprite Skin` to the generated visual prefab. Attach `Hitbox` only to attack collider children and `Hurtbox` only to the body collider. Animation events should enable and disable hitboxes on their active frames; special-move events spawn VFX from `VFX_Spawn`.

## Art and safety rules

- Keep all sixteen designs original; do not copy Street Fighter or King of Fighters costumes, faces, names, or signature moves.
- Youth fighters use age-appropriate protective clothing and supervised, non-graphic exhibition stories. Adult-oriented outfit variants must not apply to them.
- Robot and monster silhouettes should use their own rigs instead of stretching a humanoid costume system.
- Cinematic finishes end in arcade knockouts, not gore or death animations.
