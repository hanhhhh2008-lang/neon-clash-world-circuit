# 2D-to-3D research and selection

The visual review found that the game needed dimensional depth, not a risky dependency
on downloaded character assets. I reviewed these open-source GitHub projects:

- [Boner2D](https://github.com/playemgames/Boner2D) — MIT-licensed Unity 2D bone, IK,
  mesh-deformation, and sprite animation tooling. Useful for a future authored rig pass,
  but it is an editor dependency and does not automatically turn the current atlas into
  production-ready 3D fighters.
- [TripoSR-for-Unity](https://github.com/mapluisch/TripoSR-for-Unity) — MIT-licensed
  Unity integration for generating meshes from images. It requires Python/PyTorch,
  downloads model weights, and is tested against older Unity versions; generated meshes
  would still need retopology, rigging, materials, animation, and provenance review.
- [SBS-2DTo3D](https://github.com/yushan777/SBS-2DTo3D) — MIT-licensed Depth Anything
  image-depth workflow for parallax/stereo output, not a character rig or fighter asset
  pipeline.

## CLI installation and decision for this increment

The open-source TripoSR CLI was installed and smoke-tested in an isolated temporary
Python 3.11 environment. Its native `xatlas` and `torchmcubes` dependencies were built
there; model weights are deliberately not copied into this repository. The repeatable
details and provenance boundary are in [`Tools/ImageTo3D/README.md`](Tools/ImageTo3D/README.md).

No external package, model weight, or character asset is imported into the Unity build.
The shipped path now combines the project-owned painted atlas (kept as a low-opacity
provenance layer) with an articulated Unity primitive-mesh rig. The rig has independent
torso, head, upper/lower arm, thigh, and calf transforms driven by the deterministic
simulation tick, while the existing attack/guard/impact layers remain presentation-only.
This gives a controlled 2.5D/3D read without pretending that an unrigged image-to-3D
mesh is production animation. The reviewed GitHub projects remain optional research
references until an authored mesh/skin pass is approved.
