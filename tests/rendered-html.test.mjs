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
    'KeyU: "lightPunch"',
    'KeyI: "heavyPunch"',
    'KeyO: "lightKick"',
    'Semicolon: "heavyKick"',
  ]) {
    assert.match(source, new RegExp(declaration.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")));
  }
  assert.match(source, /SOLAR CHAIN/);
  assert.match(source, /TIDAL FORM/);
  assert.match(source, /AXIOM-7/);
  assert.match(source, /MOSS COLOSSUS/);
  assert.match(source, /12-year-old junior inventor/);
  assert.match(source, /13-year-old skating champion/);
  assert.match(source, /14-year-old academy champion/);
  assert.match(source, /TIPSY SAGE/);
  assert.match(source, /CINEMATIC FINISH/);
  assert.match(source, /FIREBALL \/ LONG RANGE/);
  assert.match(source, /RUSH COMBO/);
  assert.match(source, /actionSeq/);
  assert.match(source, /guardMeter/);
  assert.match(source, /queuedAttack/);
  assert.match(source, /comboAction \?\? initialAction/);
  assert.doesNotMatch(source, /const artSheets/);
  assert.match(source, /buildLiveCombatPortrait/);
  assert.match(source, /livePortraits/);
  assert.doesNotMatch(source, /illustratedSprites/);
  assert.doesNotMatch(source, /else drawArticulatedFighter/);
  const advancedComboBlock = source.match(/const ADVANCED_COMBOS:[\s\S]*?= \[([\s\S]*?)\n\];/);
  assert.ok(advancedComboBlock);
  assert.equal([...advancedComboBlock[1].matchAll(/name: "/g)].length, 12);
  assert.match(source, /12 ADVANCED COMBOS/);
  assert.match(source, /WORLD CIRCUIT FINALE/);
  assert.match(source, /RUSH COMBO/);
});

test("renders one clear fighter per selection tile", async () => {
  const [game, styles] = await Promise.all([
    readFile(new URL("../app/neon-clash.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/globals.css", import.meta.url), "utf8"),
  ]);
  assert.match(game, /backgroundSize: `\$\{total \* 100\}% auto`/);
  assert.match(game, /ONE ACTIVE SELECTION/);
  assert.match(styles, /versus-preview\.solo-preview/);
  assert.doesNotMatch(game, /rival=\{fighter\.id === cpuId\}/);
  assert.doesNotMatch(styles, /fighter-card\.is-rival/);
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
  assert.match(room, /guestOnline/);
  assert.match(room, /staleGuest/);
  assert.match(input, /cleanInput/);
  assert.match(input, /stale-input/);
  assert.match(state, /UPDATE rooms SET state/);
  assert.match(schema, /spectators/);
  assert.match(room, /staleGuest/);
  const game = await readFile(new URL("../app/neon-clash.tsx", import.meta.url), "utf8");
  assert.match(game, /autoJoinAttemptedRef/);
  assert.match(game, /PLAYER 2 AUTO-JOIN LINK/);
  assert.match(game, /applyRemoteSnapshot\(true\)/);
  assert.match(game, /INPUT PREDICTION/);
  assert.match(game, /updateCombatant\(p2, p1, predictedMove, predicted, dt\)/);
});

test("adds responsive fighting-game feedback", async () => {
  const game = await readFile(new URL("../app/neon-clash.tsx", import.meta.url), "utf8");
  assert.match(game, /hitStop/);
  assert.match(game, /COUNTER HIT/);
  assert.match(game, /HEAVY IMPACT/);
  assert.match(game, /COMMAND MEMORY/);
});

test("implements the advanced GDD combat engine", async () => {
  const game = await readFile(new URL("../app/neon-clash.tsx", import.meta.url), "utf8");
  for (const state of ["idle", "walking", "crouching", "jumping", "attacking", "blocking", "blockstun", "hitstun", "hardKnockdown", "juggle", "dashing", "maxMode"]) assert.match(game, new RegExp(`"${state}"`));
  for (const box of ["pushbox", "hurtboxes", "hitboxes", "throwbox"]) assert.match(game, new RegExp(box));
  assert.match(game, /startup: 3/);
  assert.match(game, /startup: 8/);
  assert.match(game, /inputRing\.length > 30/);
  assert.match(game, /activateMaxMode/);
  assert.match(game, /lastMoveTap/);
  assert.match(game, /difficulty === "ROOKIE" \? 1\.15/);
  for (const stage of ["NEON CYBERPUNK ALLEY", "ABANDONED SUBWAY", "ANCIENT SHAOLIN DOJO", "DESERTED OIL RIG", "ROOFTOP SUNSET", "UNDERGROUND FIGHT CLUB", "HIMALAYAN PEAK", "VOLCANIC CAVERN", "COLOSSEUM RUINS", "HIGH-TECH LAB"]) assert.match(game, new RegExp(stage));
});
