import assert from "node:assert/strict";
import { access, readFile } from "node:fs/promises";
import test from "node:test";

test("builds the Neon Clash game shell", async () => {
  const [layout, game] = await Promise.all([
    readFile(new URL("../app/layout.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/neon-clash.tsx", import.meta.url), "utf8"),
    access(new URL("../dist/server/index.js", import.meta.url)),
  ]);
  assert.match(layout, /Neon Clash — World Circuit/);
  assert.match(game, /NEON/);
  assert.match(game, /WORLD CIRCUIT/);
  assert.doesNotMatch(`${layout}\n${game}`, /codex-preview/i);
});

test("declares the requested keyboard controls and signature combos", async () => {
  const source = await readFile(new URL("../app/neon-clash.tsx", import.meta.url), "utf8");
  for (const declaration of [
    'KeyA: "left"',
    'KeyD: "right"',
    'KeyW: "jump"',
    'KeyS: "crouch"',
    'KeyT: "lightPunch"',
    'KeyY: "heavyPunch"',
    'KeyU: "lightKick"',
    'KeyK: "heavyKick"',
  ]) {
    assert.match(source, new RegExp(declaration.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
  }
  assert.match(source, /SOLAR CHAIN/);
  assert.match(source, /TIDAL FORM/);
});

test("includes persistent two-player rooms and spectator APIs", async () => {
  const [rooms, room, input, state, schema] = await Promise.all([
    readFile(new URL("../app/api/rooms/route.ts", import.meta.url), "utf8"),
    readFile(new URL("../app/api/rooms/[roomId]/route.ts", import.meta.url), "utf8"),
    readFile(new URL("../app/api/rooms/[roomId]/input/route.ts", import.meta.url), "utf8"),
    readFile(new URL("../app/api/rooms/[roomId]/state/route.ts", import.meta.url), "utf8"),
    readFile(new URL("../db/schema.ts", import.meta.url), "utf8"),
  ]);

  assert.match(rooms, /export async function POST/);
  assert.match(room, /role: "guest"/);
  assert.match(room, /role: "spectator"/);
  assert.match(input, /cleanInput/);
  assert.match(state, /UPDATE rooms SET state/);
  assert.match(schema, /spectators/);
});
