import {
  PROTOCOL_VERSION, BUILD_REVISION, MAX_PACKET_BYTES, SESSION_LIFETIME_MS, ProtocolError,
  bearerToken, byteLength, errorResponse, normalizeCode, originAllowed, validateEnvelope,
  validateHello, validateJsonRequest, validateMatchConfig
} from "./protocol.js";

const CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
const MAX_SPECTATORS = 64;
const MAX_MESSAGES_PER_SECOND = 180;

export default {
  async fetch(request, env) {
    const cors = corsHeaders(request, env);
    try {
      if (!originAllowed(request, env.ALLOWED_ORIGINS)) throw new ProtocolError(403, "origin_denied", "Request origin is not allowed.");
      if (request.method === "OPTIONS") return new Response(null, { status: 204, headers: cors });
      const url = new URL(request.url);
      if (request.method === "GET" && url.pathname === "/v2/health")
        return Response.json({ ok: true, protocol: PROTOCOL_VERSION, build: BUILD_REVISION }, { headers: cors });
      if (request.method === "POST" && url.pathname === "/v2/sessions") return createSession(request, env, cors);
      const route = /^\/v2\/sessions\/([A-Z2-9]{6})\/(join|spectate|socket)$/i.exec(url.pathname);
      if (!route) throw new ProtocolError(404, "not_found", "Relay route was not found.");
      const code = normalizeCode(route[1]);
      const action = route[2].toLowerCase();
      if (action === "socket" && request.method !== "GET") throw new ProtocolError(405, "method_not_allowed", "Socket route requires GET.");
      if (action !== "socket" && request.method !== "POST") throw new ProtocolError(405, "method_not_allowed", "Session route requires POST.");
      const stub = env.MATCH_SESSIONS.get(env.MATCH_SESSIONS.idFromName(code));
      const response = await stub.fetch(new Request(new URL("/" + action, request.url), request));
      return action === "socket" ? response : withCors(response, cors);
    } catch (error) { return errorResponse(error, cors); }
  }
};

async function createSession(request, env, cors) {
  validateJsonRequest(request);
  const body = await boundedJson(request);
  const hello = validateHello(body.hello);
  for (let attempt = 0; attempt < 8; attempt++) {
    const code = randomCode();
    const stub = env.MATCH_SESSIONS.get(env.MATCH_SESSIONS.idFromName(code));
    const response = await stub.fetch(new Request(new URL("/create", request.url), {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ code, hello, config: body.config })
    }));
    if (response.status === 409) continue;
    return withCors(response, cors);
  }
  throw new ProtocolError(503, "code_exhausted", "Could not allocate a session code.");
}

export class MatchSession {
  constructor(state) {
    this.state = state;
    this.session = null;
    this.relaySequence = 0;
    this.sequenceClockMs = -1;
    this.sequenceWithinMs = 0;
  }

  async fetch(request) {
    try {
      const path = new URL(request.url).pathname;
      if (path === "/create") return await this.create(request);
      await this.loadSession();
      this.assertLive();
      if (path === "/join") return await this.join(request, "guest");
      if (path === "/spectate") return await this.join(request, "spectator");
      if (path === "/socket") return await this.openSocket(request);
      throw new ProtocolError(404, "not_found", "Session route was not found.");
    } catch (error) { return errorResponse(error); }
  }

  async create(request) {
    if (await this.state.storage.get("session")) throw new ProtocolError(409, "code_collision", "Session code is already allocated.");
    const body = await boundedJson(request);
    const hello = validateHello(body.hello);
    const config = validateMatchConfig(body.config);
    const token = randomToken();
    const now = Date.now();
    this.session = {
      sessionId: crypto.randomUUID(), code: normalizeCode(body.code), buildHash: hello.buildHash,
      contentHash: hello.contentHash, config, createdAt: now, expiresAt: now + SESSION_LIFETIME_MS,
      hostTokenHash: await hashToken(token), guestTokenHash: null, spectatorTokenHashes: [],
      latestTicks: [-1, -1], checksums: {}, latestKeyframe: null, relaySequence: 0
    };
    await this.state.storage.put("session", this.session);
    await this.state.storage.setAlarm(this.session.expiresAt);
    return Response.json({ assignment: assignment(this.session, token, "host", 0) }, { status: 201 });
  }

  async join(request, role) {
    const body = await boundedJson(request);
    const hello = validateHello(body.hello);
    if (hello.buildHash !== this.session.buildHash || hello.contentHash !== this.session.contentHash)
      throw new ProtocolError(409, "content_mismatch", "All peers must use identical build and content hashes.");
    const token = randomToken();
    const hash = await hashToken(token);
    if (role === "guest") {
      if (this.session.guestTokenHash) throw new ProtocolError(409, "player_slot_full", "The guest player slot is occupied.");
      this.session.guestTokenHash = hash;
    } else {
      if (this.session.spectatorTokenHashes.length >= MAX_SPECTATORS) throw new ProtocolError(429, "spectator_full", "Spectator capacity is full.");
      this.session.spectatorTokenHashes.push(hash);
    }
    await this.saveSession();
    return Response.json({ assignment: assignment(this.session, token, role, role === "guest" ? 1 : -1) });
  }

  async openSocket(request) {
    if ((request.headers.get("upgrade") || "").toLowerCase() !== "websocket") throw new ProtocolError(426, "upgrade_required", "WebSocket upgrade is required.");
    const identity = await this.authorize(bearerToken(request));
    const pair = new WebSocketPair();
    const client = pair[0];
    const server = pair[1];
    server.serializeAttachment({ ...identity, windowStarted: Date.now(), messageCount: 0 });
    this.state.acceptWebSocket(server);
    this.broadcast("peer-joined", null, server);
    if (identity.role === "spectator" && this.session.latestKeyframe)
      server.send(JSON.stringify(this.envelope("keyframe", { keyframe: this.session.latestKeyframe })));
    return new Response(null, { status: 101, webSocket: client });
  }

  async webSocketMessage(socket, message) {
    try {
      await this.loadSession();
      this.assertLive();
      const identity = socket.deserializeAttachment();
      this.enforceRate(identity, socket);
      if (typeof message !== "string" || byteLength(message) > MAX_PACKET_BYTES) throw new ProtocolError(413, "packet_too_large", "Packet exceeds the size limit.");
      const packet = validateEnvelope(JSON.parse(message), this.session.sessionId);
      if (packet.kind === "ping") { socket.send(JSON.stringify(this.envelope("pong"))); return; }
      if (identity.role === "spectator") throw new ProtocolError(403, "spectator_read_only", "Spectators cannot publish match packets.");
      if (packet.kind === "input") await this.handleInput(socket, identity, packet);
      else if (packet.kind === "checksum") await this.handleChecksum(socket, identity, packet);
      else if (packet.kind === "keyframe") await this.handleKeyframe(socket, identity, packet);
      else if (packet.kind === "desync") this.broadcast("desync", { desync: packet.desync }, socket);
      else throw new ProtocolError(400, "invalid_kind", "Client cannot publish this packet kind.");
    } catch (error) { this.failSocket(socket, error); }
  }

  async webSocketClose(socket) {
    try { await this.loadSession(); this.broadcast("peer-left", null, socket); } catch { /* session already expired */ }
  }
  webSocketError(socket) { try { socket.close(1011, "socket error"); } catch { /* already closed */ } }

  async alarm() {
    for (const socket of this.state.getWebSockets()) try { socket.close(1001, "session expired"); } catch { /* closed */ }
    await this.state.storage.deleteAll();
    this.session = null;
  }

  async handleInput(socket, identity, packet) {
    if (packet.input.playerIndex !== identity.playerIndex) throw new ProtocolError(403, "wrong_player", "Input player index does not match the credential.");
    const peerTick = this.session.latestTicks[identity.playerIndex];
    const otherTick = this.session.latestTicks[1 - identity.playerIndex];
    if (packet.input.tick < peerTick - 12 || packet.input.tick > Math.max(peerTick, otherTick, 0) + 360)
      throw new ProtocolError(400, "tick_out_of_window", "Input tick is outside the relay window.");
    if (packet.input.tick > peerTick) this.session.latestTicks[identity.playerIndex] = packet.input.tick;
    this.broadcast("input", { input: packet.input }, socket);
  }

  async handleChecksum(socket, identity, packet) {
    const tick = String(packet.checksum.tick);
    const values = this.session.checksums[tick] || {};
    values[identity.role] = packet.checksum.checksum.toLowerCase();
    this.session.checksums[tick] = values;
    if (values.host && values.guest && values.host !== values.guest)
      this.broadcast("desync", { desync: { tick: packet.checksum.tick, hostChecksum: values.host, guestChecksum: values.guest, requestKeyframe: true } });
    else this.broadcast("checksum", { checksum: packet.checksum }, socket);
    for (const key of Object.keys(this.session.checksums)) if (Number(key) < packet.checksum.tick - 240) delete this.session.checksums[key];
    await this.saveSession();
  }

  async handleKeyframe(socket, identity, packet) {
    if (identity.role !== "host") throw new ProtocolError(403, "host_only", "Only the host may publish authoritative keyframes.");
    this.session.latestKeyframe = packet.keyframe;
    this.broadcast("keyframe", { keyframe: packet.keyframe }, socket);
    await this.saveSession();
  }

  broadcast(kind, payload = null, except = null) {
    const encoded = JSON.stringify(this.envelope(kind, payload || {}));
    for (const socket of this.state.getWebSockets()) {
      if (socket === except) continue;
      try { socket.send(encoded); } catch { /* close event will clean up */ }
    }
  }

  envelope(kind, payload = {}) {
    // A two-hour session-relative wall clock keeps sequences monotonic across
    // Durable Object hibernation without writing storage for every input frame.
    const clock = Math.max(0, Math.min(8388607, Date.now() - this.session.createdAt));
    if (clock === this.sequenceClockMs) this.sequenceWithinMs++;
    else { this.sequenceClockMs = clock; this.sequenceWithinMs = 0; }
    if (this.sequenceWithinMs > 255) throw new ProtocolError(429, "sequence_rate", "Relay sequence capacity exceeded.");
    this.relaySequence = clock * 256 + this.sequenceWithinMs;
    return { protocol: PROTOCOL_VERSION, kind, sessionId: this.session.sessionId, sequence: this.relaySequence, ...payload };
  }

  enforceRate(identity, socket) {
    const now = Date.now();
    if (now - identity.windowStarted >= 1000) { identity.windowStarted = now; identity.messageCount = 0; }
    identity.messageCount++;
    socket.serializeAttachment(identity);
    if (identity.messageCount > MAX_MESSAGES_PER_SECOND) throw new ProtocolError(429, "rate_limited", "Socket message rate exceeded.");
  }

  failSocket(socket, error) {
    const known = error instanceof ProtocolError;
    try { socket.send(JSON.stringify(this.envelope("error", { error: known ? error.code : "internal_error" }))); } catch { /* ignore */ }
    try { socket.close(known ? 1008 : 1011, known ? error.code : "internal error"); } catch { /* ignore */ }
  }

  async authorize(token) {
    const hash = await hashToken(token);
    if (safeEqual(hash, this.session.hostTokenHash)) return { role: "host", playerIndex: 0 };
    if (safeEqual(hash, this.session.guestTokenHash)) return { role: "guest", playerIndex: 1 };
    if (this.session.spectatorTokenHashes.some(value => safeEqual(hash, value))) return { role: "spectator", playerIndex: -1 };
    throw new ProtocolError(401, "unauthorized", "Credential is invalid or expired.");
  }

  async loadSession() {
    if (!this.session) {
      this.session = await this.state.storage.get("session");
      if (!this.session) throw new ProtocolError(404, "session_not_found", "Session does not exist.");
    }
  }

  assertLive() { if (Date.now() >= this.session.expiresAt) throw new ProtocolError(410, "session_expired", "Session has expired."); }
  async saveSession() { await this.state.storage.put("session", this.session); }
}

async function boundedJson(request) {
  validateJsonRequest(request);
  const text = await request.text();
  if (byteLength(text) > MAX_PACKET_BYTES) throw new ProtocolError(413, "request_too_large", "Request exceeds the packet limit.");
  try { return JSON.parse(text); } catch { throw new ProtocolError(400, "invalid_json", "Request JSON is invalid."); }
}

function assignment(session, token, role, playerIndex) {
  return { sessionId: session.sessionId, joinCode: session.code, token, role, playerIndex, tickRate: 60,
    inputDelay: 2, maxRollbackTicks: 12, expiresAt: session.expiresAt, config: session.config };
}

function randomCode() {
  const bytes = crypto.getRandomValues(new Uint8Array(6));
  return Array.from(bytes, value => CODE_ALPHABET[value % CODE_ALPHABET.length]).join("");
}

function randomToken() {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return btoa(String.fromCharCode(...bytes)).replaceAll("+", "-").replaceAll("/", "_").replaceAll("=", "");
}

async function hashToken(value) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, "0")).join("");
}

function safeEqual(left, right) {
  if (!left || !right || left.length !== right.length) return false;
  let difference = 0;
  for (let i = 0; i < left.length; i++) difference |= left.charCodeAt(i) ^ right.charCodeAt(i);
  return difference === 0;
}

function corsHeaders(request, env) {
  const origin = request.headers.get("origin");
  const allowed = origin && originAllowed(request, env.ALLOWED_ORIGINS) ? origin : "";
  return { "access-control-allow-origin": allowed, "access-control-allow-methods": "GET,POST,OPTIONS",
    "access-control-allow-headers": "Authorization,Content-Type,X-Neon-Clash-Protocol", "vary": "Origin",
    "cache-control": "no-store", "x-content-type-options": "nosniff" };
}

function withCors(response, cors) {
  const copy = new Response(response.body, response);
  for (const [key, value] of Object.entries(cors)) if (value) copy.headers.set(key, value);
  return copy;
}
