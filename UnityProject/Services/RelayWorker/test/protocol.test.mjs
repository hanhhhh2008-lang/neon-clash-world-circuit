import test from "node:test";
import assert from "node:assert/strict";
import { BUILD_REVISION, ProtocolError, normalizeCode, originAllowed, validateEnvelope, validateHello, validateKeyframe, validateMatchConfig } from "../src/protocol.js";

const contentHash = "a".repeat(64);

test("hello negotiates exact protocol, build, and content hash", () => {
  assert.equal(validateHello({ protocol: 2, buildHash: BUILD_REVISION, contentHash, requestedRole: "host", region: "syd" }).contentHash, contentHash);
  assert.throws(() => validateHello({ protocol: 1, buildHash: BUILD_REVISION, contentHash }), error => error instanceof ProtocolError && error.code === "protocol_mismatch");
  assert.throws(() => validateHello({ protocol: 2, buildHash: "old", contentHash }), error => error.code === "build_mismatch");
});

test("input packets enforce ownership-safe numeric bounds", () => {
  const message = { protocol: 2, kind: "input", sessionId: "session", sequence: 9,
    input: { playerIndex: 1, tick: 120, move: -1, buttons: 511, ackRemoteTick: 117 } };
  assert.equal(validateEnvelope(message, "session"), message);
  assert.throws(() => validateEnvelope({ ...message, input: { ...message.input, buttons: 512 } }, "session"), error => error.code === "invalid_input");
  assert.throws(() => validateEnvelope(message, "another"), error => error.code === "wrong_session");
});

test("keyframes require the complete fixed projectile buffer and matching tick", () => {
  const keyframe = { tick: 60, checksum: "1234abcd", state: { Tick: 60, Projectiles: Array(8).fill({ Active: false }) } };
  validateKeyframe(keyframe);
  assert.throws(() => validateKeyframe({ ...keyframe, state: { Tick: 59, Projectiles: keyframe.state.Projectiles } }), error => error.code === "invalid_keyframe");
  assert.throws(() => validateKeyframe({ ...keyframe, state: { Tick: 60, Projectiles: [] } }), error => error.code === "invalid_keyframe");
});

test("codes reject ambiguous characters and browser origins are allowlisted", () => {
  assert.equal(normalizeCode("ab2cd3"), "AB2CD3");
  assert.throws(() => normalizeCode("AB0CD1"));
  const allowed = new Request("https://relay.example/v2/health", { headers: { origin: "https://game.example" } });
  const denied = new Request("https://relay.example/v2/health", { headers: { origin: "https://evil.example" } });
  const native = new Request("https://relay.example/v2/health");
  assert.equal(originAllowed(allowed, "https://game.example,https://staging.example"), true);
  assert.equal(originAllowed(denied, "https://game.example"), false);
  assert.equal(originAllowed(native, "https://game.example"), true);
});

test("host match selection is authoritative and content-id bounded", () => {
  const config = { firstFighterId: "kael", secondFighterId: "zara", firstCostumeId: "circuit",
    secondCostumeId: "afterdark", stageId: "hyperrail", randomSeed: 41322 };
  assert.equal(validateMatchConfig(config), config);
  assert.throws(() => validateMatchConfig({ ...config, stageId: "../legacy" }), error => error.code === "invalid_match_config");
});
