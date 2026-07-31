import test from "node:test";
import assert from "node:assert/strict";
import { MatchSession } from "../src/index.js";
import { BUILD_REVISION } from "../src/protocol.js";

const contentHash = "b".repeat(64);
const config = { firstFighterId: "kael", secondFighterId: "zara", firstCostumeId: "circuit",
  secondCostumeId: "afterdark", stageId: "hyperrail", randomSeed: 42 };

test("Durable Object creates a hashed-token session and enforces compatible guest join", async () => {
  const state = fakeState();
  const session = new MatchSession(state);
  const hostResponse = await session.fetch(jsonRequest("/create", { code: "AB2CD3", hello: hello("host"), config }));
  assert.equal(hostResponse.status, 201);
  const host = await hostResponse.json();
  assert.equal(host.assignment.role, "host");
  assert.equal(host.assignment.playerIndex, 0);
  assert.deepEqual(host.assignment.config, config);
  const stored = state.values.get("session");
  assert.notEqual(stored.hostTokenHash, host.assignment.token);
  assert.match(stored.hostTokenHash, /^[a-f0-9]{64}$/);

  const mismatchResponse = await session.fetch(jsonRequest("/join", { hello: { ...hello("guest"), contentHash: "c".repeat(64) } }));
  assert.equal(mismatchResponse.status, 409);
  assert.equal((await mismatchResponse.json()).error, "content_mismatch");

  const guestResponse = await session.fetch(jsonRequest("/join", { hello: hello("guest") }));
  assert.equal(guestResponse.status, 200);
  const guest = await guestResponse.json();
  assert.equal(guest.assignment.playerIndex, 1);
  assert.deepEqual(guest.assignment.config, config);
  assert.match(state.values.get("session").guestTokenHash, /^[a-f0-9]{64}$/);

  const duplicate = await session.fetch(jsonRequest("/join", { hello: hello("guest") }));
  assert.equal(duplicate.status, 409);
  assert.equal((await duplicate.json()).error, "player_slot_full");
});

function hello(role) {
  return { protocol: 2, buildHash: BUILD_REVISION, contentHash, requestedRole: role, region: "syd" };
}

function jsonRequest(path, body) {
  return new Request("https://durable.test" + path, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(body) });
}

function fakeState() {
  const values = new Map();
  return {
    values,
    storage: {
      get: async key => values.get(key),
      put: async (key, value) => values.set(key, structuredClone(value)),
      setAlarm: async () => {},
      deleteAll: async () => values.clear()
    },
    acceptWebSocket: () => {},
    getWebSockets: () => []
  };
}
