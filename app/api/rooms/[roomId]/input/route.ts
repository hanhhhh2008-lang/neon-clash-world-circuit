import { cleanInput, db, ensureRoomSchema } from "../../lib";

type Context = { params: Promise<{ roomId: string }> };

export async function POST(request: Request, context: Context) {
  try {
    await ensureRoomSchema();
    const { roomId } = await context.params; const payload = await request.json() as { token?: string; input?: unknown }; const now = Date.now();
    const result = await db().prepare("UPDATE rooms SET guest_input = ?, guest_seen = ? WHERE id = ? AND guest_token = ?").bind(JSON.stringify(cleanInput(payload.input)), now, roomId.toUpperCase(), payload.token ?? "").run();
    if (result.meta.changes !== 1) return Response.json({ error: "Not a player in this room" }, { status: 403 });
    return Response.json({ ok: true });
  } catch (error) { return Response.json({ error: error instanceof Error ? error.message : "Unable to send input" }, { status: 500 }); }
}
