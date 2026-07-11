import { db, ensureRoomSchema, safeState } from "../../lib";

type Context = { params: Promise<{ roomId: string }> };

export async function POST(request: Request, context: Context) {
  try {
    await ensureRoomSchema();
    const { roomId } = await context.params; const payload = await request.json() as { token?: string; state?: unknown; status?: string }; const now = Date.now();
    const status = ["ready", "fighting", "complete"].includes(payload.status ?? "") ? payload.status : "fighting";
    const result = await db().prepare("UPDATE rooms SET state = ?, status = ?, host_seen = ?, updated_at = ? WHERE id = ? AND host_token = ?").bind(safeState(payload.state), status, now, now, roomId.toUpperCase(), payload.token ?? "").run();
    if (result.meta.changes !== 1) return Response.json({ error: "Not the room host" }, { status: 403 });
    return Response.json({ ok: true });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to publish state" }, { status: 500 }); }
}
