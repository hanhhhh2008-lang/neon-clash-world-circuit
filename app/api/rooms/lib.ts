import { env } from "cloudflare:workers";

export type RoomConfig = {
  playerId: string;
  cpuId: string;
  stageId: string;
  difficulty: "ROOKIE" | "PRO" | "ACE";
  outfitId: string;
};

const FIGHTERS = new Set(["kael", "zara", "atlas", "nyx", "rio", "sable", "mara", "batu", "lux", "oren"]);
const STAGES = new Set(["shibuya", "hyperrail", "stormmarket", "aegis", "voidclub", "skycourt", "solarplaza", "steppe", "prismmetro", "tidal"]);
const OUTFITS = new Set(["circuit", "afterdark", "heatwave"]);

export function db() {
  if (!env.DB) throw new Error("Room database is unavailable");
  return env.DB;
}

export async function ensureRoomSchema() {
  const database = db();
  await database.batch([
    database.prepare(`CREATE TABLE IF NOT EXISTS rooms (
      id TEXT PRIMARY KEY,
      host_token TEXT NOT NULL,
      guest_token TEXT,
      config TEXT NOT NULL,
      guest_input TEXT NOT NULL DEFAULT '{}',
      state TEXT NOT NULL DEFAULT '{}',
      status TEXT NOT NULL DEFAULT 'waiting',
      created_at INTEGER NOT NULL,
      updated_at INTEGER NOT NULL,
      host_seen INTEGER NOT NULL,
      guest_seen INTEGER
    )`),
    database.prepare(`CREATE TABLE IF NOT EXISTS spectators (
      token TEXT PRIMARY KEY,
      room_id TEXT NOT NULL,
      last_seen INTEGER NOT NULL
    )`),
    database.prepare("CREATE INDEX IF NOT EXISTS spectators_room_idx ON spectators(room_id, last_seen)"),
  ]);
}

export function validConfig(value: unknown): value is RoomConfig {
  if (!value || typeof value !== "object") return false;
  const item = value as Record<string, unknown>;
  return FIGHTERS.has(String(item.playerId)) && FIGHTERS.has(String(item.cpuId)) && STAGES.has(String(item.stageId)) && OUTFITS.has(String(item.outfitId)) && ["ROOKIE", "PRO", "ACE"].includes(String(item.difficulty));
}

export function roomCode() {
  const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
  const bytes = new Uint8Array(6); crypto.getRandomValues(bytes);
  return Array.from(bytes, (byte) => alphabet[byte % alphabet.length]).join("");
}

export function token() { return crypto.randomUUID(); }

export function json(value: string | null, fallback: unknown = {}) {
  try { return JSON.parse(value || ""); } catch { return fallback; }
}

export function cleanInput(value: unknown) {
  const source = value && typeof value === "object" ? value as Record<string, unknown> : {};
  const safe: Record<string, boolean> = {};
  for (const key of ["left", "right", "jump", "crouch", "guard", "lightPunch", "heavyPunch", "lightKick", "heavyKick", "special", "impact"]) safe[key] = source[key] === true;
  return safe;
}

export function safeState(value: unknown) {
  const encoded = JSON.stringify(value ?? {});
  if (encoded.length > 24_000) throw new Error("Match state is too large");
  return encoded;
}
