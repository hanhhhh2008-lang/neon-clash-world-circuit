# Neon Clash — World Circuit

Neon Clash is an original, browser-playable 2D fighting game inspired by the pace and spectacle of modern arcade fighters. It includes ten fighters, ten stages, character-specific combo skills, adaptive CPU play, public two-player rooms, and live spectators.

## Controls

| Key | Action |
| --- | --- |
| `A` / `D` | Move left / right |
| `W` | Jump |
| `S` | Crouch |
| `T` | Light punch |
| `Y` | Heavy punch |
| `U` | Light kick |
| `K` | Heavy kick |
| `L` | Special skill |
| `Space` | Guard |
| `O` | Drive impact |

Each fighter has a four-input signature combo shown on the fighter-select screen. Enter it quickly to trigger an enhanced special.

## Online rooms

- Create a public room or join with its six-character room code.
- Each room has exactly two active player slots.
- Additional visitors enter as live spectators.
- The host runs the authoritative match simulation and publishes snapshots; player two sends input to the room.
- Room, player, spectator, and heartbeat data are persisted in Cloudflare D1.
- Invite links can be copied or shared using the built-in email button.

## Run locally

Requires Node.js 22.13 or newer.

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Verify

```bash
npm test
npm run lint
```

`npm test` creates the production build and verifies both the rendered game shell and core multiplayer/control declarations.

## Stack

- React 19 and Next.js-compatible vinext runtime
- Canvas-rendered 2D combat
- Cloudflare D1 with Drizzle migrations
- Server API routes for room discovery, joining, input relay, and authoritative snapshots

All fighters, names, stages, visual designs, and techniques in Neon Clash are original to this project.
