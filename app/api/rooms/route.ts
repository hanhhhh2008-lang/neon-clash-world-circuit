import { db, ensureRoomSchema, json, roomCode, token, validConfig } from "./lib";

export async function GET() {
  try {
    await ensureRoomSchema();
    const now = Date.now();
    const result = await db().prepare("SELECT id, config, status, guest_token, guest_seen, updated_at FROM rooms WHERE updated_at > ? ORDER BY created_at DESC LIMIT 12").bind(now - 6 * 60 * 60 * 1000).all();
    return Response.json({ rooms: result.results.map((row: Record<string, unknown>) => { const guestOnline = Boolean(row.guest_token) && now - Number(row.guest_seen ?? 0) < 20_000; return { id: row.id, config: json(String(row.config)), status: guestOnline ? row.status : "waiting", players: guestOnline ? 2 : 1, updatedAt: row.updated_at }; }) });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to list rooms" }, { status: 500 }); }
}

export async function POST(request: Request) {
  try {
    await ensureRoomSchema();
    const payload = await request.json() as { config?: unknown };
    if (!validConfig(payload.config)) return Response.json({ error: "Invalid room configuration" }, { status: 400 });
    const database = db(); const now = Date.now(); const hostToken = token();
    let id = roomCode();
    for (let attempt = 0; attempt < 4; attempt++) {
      const existing = await database.prepare("SELECT id FROM rooms WHERE id = ?").bind(id).first();
      if (!existing) break; id = roomCode();
    }
    await database.prepare("INSERT INTO rooms (id, host_token, config, created_at, updated_at, host_seen) VALUES (?, ?, ?, ?, ?, ?)").bind(id, hostToken, JSON.stringify(payload.config), now, now, now).run();
    return Response.json({ room: { id, token: hostToken, role: "host", players: 1, spectators: 0, status: "waiting", config: payload.config } }, { status: 201 });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to create room" }, { status: 500 }); }
}
