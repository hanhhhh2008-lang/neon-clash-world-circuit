export const PROTOCOL_VERSION = 2;
export const BUILD_REVISION = "neon-clash-unity-sim-r2";
export const MAX_PACKET_BYTES = 32768;
export const VALID_BUTTON_MASK = 511;
export const SESSION_LIFETIME_MS = 2 * 60 * 60 * 1000;

const roles = new Set(["host", "guest", "spectator"]);
const kinds = new Set(["input", "checksum", "keyframe", "desync", "ping", "pong"]);

export function byteLength(value) {
  return new TextEncoder().encode(value).byteLength;
}

export function normalizeCode(value) {
  const code = String(value || "").trim().toUpperCase();
  if (!/^[A-Z2-9]{6}$/.test(code)) throw new ProtocolError(400, "invalid_code", "Session code must contain six unambiguous characters.");
  return code;
}

export function validateHello(hello) {
  if (!plainObject(hello)) throw new ProtocolError(400, "invalid_hello", "A protocol hello payload is required.");
  if (hello.protocol !== PROTOCOL_VERSION) throw new ProtocolError(409, "protocol_mismatch", "Protocol version does not match.");
  if (hello.buildHash !== BUILD_REVISION) throw new ProtocolError(409, "build_mismatch", "Client build is incompatible.");
  if (!/^[a-f0-9]{64}$/.test(String(hello.contentHash || ""))) throw new ProtocolError(400, "invalid_content_hash", "Content hash must be SHA-256 hexadecimal text.");
  if (hello.requestedRole && !roles.has(hello.requestedRole)) throw new ProtocolError(400, "invalid_role", "Requested role is invalid.");
  if (hello.region && !/^[a-z0-9-]{2,20}$/i.test(hello.region)) throw new ProtocolError(400, "invalid_region", "Region is invalid.");
  return hello;
}

export function validateMatchConfig(config) {
  if (!plainObject(config) || !contentId(config.firstFighterId) || !contentId(config.secondFighterId) ||
      !contentId(config.firstCostumeId) || !contentId(config.secondCostumeId) || !contentId(config.stageId) ||
      !integerBetween(config.randomSeed, 0, 2147483647))
    throw new ProtocolError(400, "invalid_match_config", "Match content selection is invalid.");
  return config;
}

export function validateEnvelope(message, expectedSessionId) {
  if (!plainObject(message)) throw new ProtocolError(400, "invalid_packet", "Packet must be a JSON object.");
  if (message.protocol !== PROTOCOL_VERSION || !kinds.has(message.kind)) throw new ProtocolError(400, "invalid_packet", "Packet version or kind is invalid.");
  if (message.sessionId !== expectedSessionId) throw new ProtocolError(403, "wrong_session", "Packet targets another session.");
  if (!Number.isSafeInteger(message.sequence) || message.sequence < 0) throw new ProtocolError(400, "invalid_sequence", "Packet sequence is invalid.");
  if (message.kind === "input") validateInput(message.input);
  if (message.kind === "checksum") validateChecksum(message.checksum);
  if (message.kind === "keyframe") validateKeyframe(message.keyframe);
  if (message.kind === "desync") validateDesync(message.desync);
  return message;
}

export function validateInput(input) {
  if (!plainObject(input) || !integerBetween(input.playerIndex, 0, 1) || !integerBetween(input.tick, 0, 1000000000) ||
      !integerBetween(input.move, -1, 1) || !integerBetween(input.buttons, 0, VALID_BUTTON_MASK) ||
      !integerBetween(input.ackRemoteTick, -1, 1000000000))
    throw new ProtocolError(400, "invalid_input", "Input frame is outside protocol bounds.");
}

export function validateChecksum(value) {
  if (!plainObject(value) || !integerBetween(value.tick, 0, 1000000000) || !checksumText(value.checksum))
    throw new ProtocolError(400, "invalid_checksum", "Checksum frame is invalid.");
}

export function validateKeyframe(value) {
  if (!plainObject(value) || !integerBetween(value.tick, 0, 1000000000) || !checksumText(value.checksum) ||
      !plainObject(value.state) || value.state.Tick !== value.tick || !Array.isArray(value.state.Projectiles) || value.state.Projectiles.length !== 8)
    throw new ProtocolError(400, "invalid_keyframe", "Keyframe shape is invalid.");
}

export function validateDesync(value) {
  if (!plainObject(value) || !integerBetween(value.tick, 0, 1000000000) || !checksumText(value.hostChecksum) ||
      !checksumText(value.guestChecksum) || typeof value.requestKeyframe !== "boolean")
    throw new ProtocolError(400, "invalid_desync", "Desync frame is invalid.");
}

export function validateJsonRequest(request) {
  const declared = Number(request.headers.get("content-length") || 0);
  if (declared > MAX_PACKET_BYTES) throw new ProtocolError(413, "request_too_large", "Request exceeds the packet limit.");
  const type = request.headers.get("content-type") || "";
  if (!type.toLowerCase().includes("application/json")) throw new ProtocolError(415, "json_required", "Content-Type must be application/json.");
}

export function originAllowed(request, configuredOrigins) {
  const origin = request.headers.get("origin");
  if (!origin) return true; // Native Unity clients do not send browser Origin headers.
  const allowed = String(configuredOrigins || "").split(",").map(value => value.trim()).filter(Boolean);
  return allowed.includes(origin);
}

export function bearerToken(request) {
  const match = /^Bearer ([A-Za-z0-9_-]{32,128})$/.exec(request.headers.get("authorization") || "");
  if (!match) throw new ProtocolError(401, "unauthorized", "A valid bearer credential is required.");
  return match[1];
}

export function errorResponse(error, headers = {}) {
  const known = error instanceof ProtocolError;
  return Response.json({ error: known ? error.code : "internal_error", message: known ? error.message : "Unexpected relay error." },
    { status: known ? error.status : 500, headers });
}

export class ProtocolError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

function checksumText(value) { return /^[a-f0-9]{8}$/i.test(String(value || "")); }
function contentId(value) { return /^[a-z0-9-]{1,40}$/.test(String(value || "")); }
function integerBetween(value, minimum, maximum) { return Number.isSafeInteger(value) && value >= minimum && value <= maximum; }
function plainObject(value) { return value !== null && typeof value === "object" && !Array.isArray(value); }
