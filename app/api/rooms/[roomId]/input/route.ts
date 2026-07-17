import { cleanInput, db, ensureRoomSchema, json } from "../../lib";

type Context = { params: Promise<{ roomId: string }> };

export async function POST(request: Request, context: Context) {
  try {
    await ensureRoomSchema();
    const { roomId } = await context.params; const payload = await request.json() as { token?: string; input?: unknown }; const now = Date.now(); const database = db(); const id = roomId.toUpperCase();
    const room = await database.prepare("SELECT guest_token, guest_input FROM rooms WHERE id = ?").bind(id).first<Record<string, unknown>>();
    if (!room || payload.token !== room.guest_token) return Response.json({ error: "Not a player in this room" }, { status: 403 });
    const next = cleanInput(payload.input), current = cleanInput(json(String(room.guest_input)));
    if (Number(next.actionSeq ?? 0) < Number(current.actionSeq ?? 0)) return Response.json({ ok: true, ignored: "stale-input" });
    const result = await database.prepare("UPDATE rooms SET guest_input = ?, guest_seen = ? WHERE id = ? AND guest_token = ?").bind(JSON.stringify(next), now, id, payload.token ?? "").run();
    if (result.meta.changes !== 1) return Response.json({ error: "Not a player in this room" }, { status: 403 });
    return Response.json({ ok: true });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to send input" }, { status: 500 }); }
}
