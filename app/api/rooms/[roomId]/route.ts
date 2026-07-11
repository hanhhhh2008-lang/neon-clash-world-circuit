import { cleanInput, db, ensureRoomSchema, json, token } from "../lib";

type Context = { params: Promise<{ roomId: string }> };

export async function GET(request: Request, context: Context) {
  try {
    await ensureRoomSchema();
    const { roomId } = await context.params; const id = roomId.toUpperCase(); const url = new URL(request.url); const clientToken = url.searchParams.get("token") ?? ""; const now = Date.now(); const database = db();
    const room = await database.prepare("SELECT * FROM rooms WHERE id = ?").bind(id).first<Record<string, unknown>>();
    if (!room) return Response.json({ error: "Room not found" }, { status: 404 });
    if (clientToken === room.host_token) await database.prepare("UPDATE rooms SET host_seen = ?, updated_at = ? WHERE id = ?").bind(now, now, id).run();
    else if (clientToken === room.guest_token) await database.prepare("UPDATE rooms SET guest_seen = ? WHERE id = ?").bind(now, id).run();
    else if (clientToken) await database.prepare("UPDATE spectators SET last_seen = ? WHERE token = ? AND room_id = ?").bind(now, clientToken, id).run();
    await database.prepare("DELETE FROM spectators WHERE last_seen < ?").bind(now - 20_000).run();
    const spectators = await database.prepare("SELECT COUNT(*) AS count FROM spectators WHERE room_id = ? AND last_seen >= ?").bind(id, now - 20_000).first<{ count: number }>();
    return Response.json({ room: { id, config: json(String(room.config)), state: json(String(room.state)), guestInput: cleanInput(json(String(room.guest_input))), status: room.status, players: room.guest_token ? 2 : 1, spectators: Number(spectators?.count ?? 0) } });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to read room" }, { status: 500 }); }
}

export async function POST(request: Request, context: Context) {
  try {
    await ensureRoomSchema();
    const { roomId } = await context.params; const id = roomId.toUpperCase(); const payload = await request.json() as { action?: string }; const database = db(); const now = Date.now();
    const room = await database.prepare("SELECT * FROM rooms WHERE id = ?").bind(id).first<Record<string, unknown>>();
    if (!room) return Response.json({ error: "Room not found" }, { status: 404 });
    if (payload.action === "join" && !room.guest_token) {
      const guestToken = token();
      const result = await database.prepare("UPDATE rooms SET guest_token = ?, guest_seen = ?, status = 'ready', updated_at = ? WHERE id = ? AND guest_token IS NULL").bind(guestToken, now, now, id).run();
      if (result.meta.changes === 1) return Response.json({ room: { id, token: guestToken, role: "guest", players: 2, spectators: 0, status: "ready", config: json(String(room.config)) } });
    }
    const spectatorToken = token();
    await database.prepare("INSERT INTO spectators (token, room_id, last_seen) VALUES (?, ?, ?)").bind(spectatorToken, id, now).run();
    const spectators = await database.prepare("SELECT COUNT(*) AS count FROM spectators WHERE room_id = ? AND last_seen >= ?").bind(id, now - 20_000).first<{ count: number }>();
    return Response.json({ room: { id, token: spectatorToken, role: "spectator", players: room.guest_token ? 2 : 1, spectators: Number(spectators?.count ?? 1), status: room.status, config: json(String(room.config)) } });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to join room" }, { status: 500 }); }
}
