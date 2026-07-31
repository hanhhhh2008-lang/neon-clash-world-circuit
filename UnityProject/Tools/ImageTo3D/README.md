# Optional image-to-3D research CLI

The requested 2D-to-3D CLI is installed in an isolated temporary environment at
`/private/tmp/neon-clash-triposr`:

- source: [VAST-AI-Research/TripoSR](https://github.com/VAST-AI-Research/TripoSR)
- license: MIT (verify upstream before redistribution)
- environment: `/private/tmp/neon-clash-triposr/.venv` (Python 3.11)
- smoke test: `run.py --help` succeeds
- native build: `xatlas 0.0.11` and `torchmcubes` were built in the isolated venv;
  PyTorch's CMake prefix was supplied for the latter on Apple Silicon

Example (only with project-owned source art and an explicitly reviewed output):

```bash
/private/tmp/neon-clash-triposr/.venv/bin/python \
  /private/tmp/neon-clash-triposr/run.py path/to/project-owned-image.png \
  --device cpu --mc-resolution 128 --model-save-format glb \
  --output-dir /private/tmp/neon-clash-triposr/output
```

The model weights are downloaded by Hugging Face on first real conversion and are
not part of this repository or the Unity build. Generated geometry still requires
topology, materials, rigging, animation, and provenance review, so the shipped game
uses the authored Unity primitive-mesh fighter rig instead of silently importing a
generated character. No paid or third-party Asset Store content is used.
