# Neon Clash protocol-2 relay

This isolated Cloudflare Worker is the formal Unity online boundary. It does not replace or modify the legacy web game's Worker or D1 database.

It provides session creation/join/spectate endpoints and a Durable Object WebSocket relay with exact protocol/build/content negotiation, opaque two-hour bearer credentials, role ownership checks, input bounds, tick windows, rate/packet limits, periodic checksum comparison, host-authoritative recovery keyframes, and delayed spectator input fan-out.

## Local checks

```sh
npm run check
npm test
```

Deployment is intentionally not performed by this repository increment. Before deploying, set a real comma-separated `ALLOWED_ORIGINS`, install a pinned Wrangler development dependency, select Cloudflare account/region policy, configure observability and retention, and complete adversarial/load/multi-region testing. Tokens are returned only once and the service persists only token hashes.
