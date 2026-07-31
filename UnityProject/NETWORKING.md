# Online architecture

Unity online play has two explicit, non-overlapping boundaries. The legacy web game and its Cloudflare Worker/D1 implementation remain untouched.

## Protocol 1 compatibility

`OnlineRoomTransport.cs` can speak to the existing `/api/rooms` polling routes and converts between Unity integer state and the browser's Canvas-shaped snapshots. It exists for staged compatibility checks only. Tokens remain in memory, non-local URLs require HTTPS, and none of these DTOs participate in protocol-2 rollback.

## Protocol 2 implementation

The production path is fixed at 60 Hz and uses the complete `DeterministicMatchState` as its replay/checksum/keyframe boundary:

1. `RelaySessionServiceV2` creates, joins, or spectates a session over HTTPS. It negotiates exact protocol, build revision, SHA-256 content hash, and the host-authoritative fighter/costume/stage selection.
2. The relay returns a two-hour opaque bearer credential. The Unity client keeps it in memory and sends it only in the WebSocket `Authorization` header—never in a URL or `PlayerPrefs`.
3. `RollbackSession` applies two ticks of input delay, predicts only held movement/crouch/guard state, never repeats action edges, and replays at most 12 ticks from complete snapshots.
4. Both players emit a checksum every 30 ticks. A late input outside the rollback window or a mismatch requests a host-authoritative keyframe; keyframes carry and validate the complete eight-slot projectile and combo-history state.
5. The host also emits a keyframe every 300 ticks. Spectators consume a keyframe plus both input streams with a 120-tick non-predictive delay.
6. `NeonClashMatch` drives the existing deterministic presentation and HUD from rollback or spectator state. Local/CPU play uses the same simulation without a transport.

The relay under `Services/RelayWorker` is deliberately a separate Cloudflare Worker package. Its Durable Object validates role ownership and numeric ranges, enforces 32 KiB packets, tick windows, 180 messages/second/socket, exact content/build compatibility, expiring token hashes, a 64-spectator cap, browser-origin allowlisting, checksum disagreement, host-only keyframes, and monotonic relay sequencing across hibernation. It does not write per-tick state to D1.

## Running the local gates

```sh
bash UnityProject/Tools/SimulationValidation/validate-simulation.sh
cd UnityProject/Services/RelayWorker
npm run check
npm test
```

Unity's foundation validator additionally exercises content-hash stability, WSS/HTTPS enforcement, canonical checksums, input JSON, and complete keyframe round trips.

## Compatibility and deployment boundaries

- Protocol 1 and 2 cannot share a live match. They may coexist as separately selected migration paths.
- Match content must be identical by SHA-256; incompatible builds fail before the socket opens.
- Native Unity WebGL does not support `ClientWebSocket`; a future WebGL target needs a browser transport implementing the same envelope boundary.
- No relay is deployed and no account, hostname, certificate, secret, analytics vendor, or paid service is created by this increment. The lobby defaults to `http://localhost:8787` and rejects insecure remote URLs.
- Before production deployment: set `ALLOWED_ORIGINS`, pin Wrangler, choose account/region policy, add identity/account linking and credential rotation, define audit/retention/deletion policy, add observability and moderation, and run adversarial, reconnect, background/resume, host-loss, multi-region, spectator-fan-out, and real-device latency/loss/load tests.

Because those operational decisions require external infrastructure and ownership, the codebase is implementation-ready but online service production is not being claimed.
