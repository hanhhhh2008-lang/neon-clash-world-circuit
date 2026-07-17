"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";

type Fighter = {
  id: string;
  name: string;
  alias: string;
  city: string;
  style: string;
  special: string;
  quote: string;
  color: string;
  secondary: string;
  speed: number;
  power: number;
  reach: number;
  mark: string;
  combo: { name: string; sequence: string[] };
  ultimate: string;
  personality: string;
  bio: string;
  costume: string;
  kind: "human" | "youth" | "robot" | "monster" | "elder";
  portrait: { sheet: "main" | "bonus"; index: number };
};

type Stage = { id: string; name: string; city: string; descriptor: string; color: string; accent: string; motif: "skyline" | "rail" | "market" | "forge" | "club" | "court" | "plaza" | "steppe" | "metro" | "harbour" };
type Outfit = { id: string; name: string; note: string; cut: "classic" | "sleek" | "heatwave" };

type Combatant = {
  fighter: Fighter;
  x: number;
  y: number;
  vx: number;
  vy: number;
  facing: 1 | -1;
  health: number;
  drive: number;
  grounded: boolean;
  crouching: boolean;
  guarding: boolean;
  attack: "lightPunch" | "heavyPunch" | "lightKick" | "heavyKick" | "special" | "impact" | "super" | null;
  attackTime: number;
  attackHit: boolean;
  hurtTime: number;
  stunTime: number;
  flashTime: number;
  combo: number;
  comboWindow: number;
  guardMeter: number;
  evadeTime: number;
  queuedAttack: Combatant["attack"];
  queuedTime: number;
  wins: number;
};

type Projectile = { x: number; y: number; vx: number; life: number; owner: Combatant; color: string; damage: number };
type Particle = { x: number; y: number; vx: number; vy: number; life: number; maxLife: number; color: string; size: number };
type CombatantSnapshot = Omit<Combatant, "fighter" | "queuedAttack" | "queuedTime">;
type MatchSnapshot = { sequence?: number; p1: CombatantSnapshot; p2: CombatantSnapshot; timer: number; round: number; roundState: "intro" | "fight" | "ko" | "done"; projectiles: Array<Omit<Projectile, "owner"> & { owner: 1 | 2 }> };
type RoomRole = "host" | "guest" | "spectator";
type InputFrame = Record<string, boolean | number | string>;
type RoomSession = { id: string; token: string; role: RoomRole; players: number; spectators: number; status: string; hostOnline?: boolean; guestOnline?: boolean };
type RoomConfig = { playerId: string; cpuId: string; stageId: string; difficulty: "ROOKIE" | "PRO" | "ACE"; outfitId: string };
type OpenRoom = { id: string; players: number; status: string; config: RoomConfig };

const FIGHTERS: Fighter[] = [
  { id: "kael", name: "KAEL", alias: "SUN BREAKER", city: "SEOUL", style: "Rushdown", special: "Solar Rift", ultimate: "HELIOS OVERDRIVE", personality: "Driven · Protective · Impatient", bio: "A courier who weaponized an illegal solar prosthetic to protect his district.", costume: "Asymmetric techwear jacket · armored right sleeve · reactor sneakers", quote: "Speed is a decision.", color: "#ff7a28", secondary: "#1768ff", speed: 9, power: 6, reach: 6, mark: "K", kind: "human", portrait: { sheet: "main", index: 0 }, combo: { name: "SOLAR CHAIN", sequence: ["Y", "Y", "U", "L"] } },
  { id: "zara", name: "ZARA", alias: "VOLT QUEEN", city: "LAGOS", style: "Pressure", special: "Thunder Step", ultimate: "QUEEN'S TEMPEST", personality: "Magnetic · Fearless · Theatrical", bio: "A grid engineer who dances through voltage surges and never enters quietly.", costume: "Conductive captain coat · braided crown · insulated gauntlets", quote: "Hear the storm arrive.", color: "#347cff", secondary: "#ff2dba", speed: 8, power: 7, reach: 5, mark: "Z", kind: "human", portrait: { sheet: "main", index: 1 }, combo: { name: "VOLTAGE RUSH", sequence: ["Y", "U", "I", "L"] } },
  { id: "atlas", name: "ATLAS", alias: "IRON SAINT", city: "ATHENS", style: "Grappler", special: "Titan Break", ultimate: "OLYMPUS DESCENDS", personality: "Patient · Honorable · Immovable", bio: "A museum conservator who rebuilt ceremonial armor into a kinetic grappling rig.", costume: "Bronze muscle cuirass · white mantle · articulated greaves", quote: "The ground remembers.", color: "#ff9d3f", secondary: "#f4d6a0", speed: 4, power: 10, reach: 6, mark: "A", kind: "human", portrait: { sheet: "main", index: 2 }, combo: { name: "TITAN LOCK", sequence: ["Y", "L", "U", "L"] } },
  { id: "nyx", name: "NYX", alias: "VOID SIGNAL", city: "BERLIN", style: "Zoner", special: "Black Pulse", ultimate: "EVENT HORIZON", personality: "Private · Analytical · Dry-witted", bio: "A signal pirate who bends arena light with a coat woven from programmable mesh.", costume: "Hooded mesh trench · holographic half-mask · signal gloves", quote: "Distance is control.", color: "#9b6cff", secondary: "#32204f", speed: 6, power: 7, reach: 10, mark: "N", kind: "human", portrait: { sheet: "main", index: 3 }, combo: { name: "VOID CASCADE", sequence: ["U", "Y", "I", "L"] } },
  { id: "rio", name: "RIO", alias: "SKYLINE KID", city: "SÃO PAULO", style: "Aerial", special: "Comet Kick", ultimate: "ORBITAL SAMBA", personality: "Joyful · Restless · Daring", bio: "A rooftop courier and capoeira showstopper who treats every wall like a launchpad.", costume: "Street-athletic layers · reinforced knees · neon high-tops", quote: "Gravity is optional.", color: "#d8ff47", secondary: "#24df9b", speed: 10, power: 5, reach: 6, mark: "R", kind: "human", portrait: { sheet: "main", index: 4 }, combo: { name: "COMET LADDER", sequence: ["I", "I", "U", "L"] } },
  { id: "sable", name: "SABLE", alias: "NIGHT BLADE", city: "TOKYO", style: "Counter", special: "Zero Cut", ultimate: "MIDNIGHT VERDICT", personality: "Reserved · Precise · Compassionate", bio: "A forensic fencer who predicts attacks by reading breath, balance, and fabric movement.", costume: "Tailored urban shinobi coat · red scarf · plated half-mask", quote: "Your move. My opening.", color: "#efefff", secondary: "#d62646", speed: 8, power: 8, reach: 7, mark: "S", kind: "human", portrait: { sheet: "main", index: 5 }, combo: { name: "ZERO VERDICT", sequence: ["Y", "I", "U", "L"] } },
  { id: "mara", name: "MARA", alias: "RED ORBIT", city: "MEXICO CITY", style: "Balanced", special: "Meteor Arc", ultimate: "AZTEC SUPERNOVA", personality: "Warm · Competitive · Unbreakable", bio: "An aerospace mechanic who fused lucha pageantry with zero-gravity training.", costume: "Embroidered flight jacket · orbital belt · impact boots", quote: "Burn bright. Hit hard.", color: "#ff405c", secondary: "#ff8b32", speed: 7, power: 8, reach: 7, mark: "M", kind: "human", portrait: { sheet: "main", index: 6 }, combo: { name: "ORBIT BREAK", sequence: ["Y", "U", "I", "L"] } },
  { id: "batu", name: "BATU", alias: "STEPPE WALL", city: "ULAANBAATAR", style: "Armor", special: "Stone Wake", ultimate: "ETERNAL BLUE SKY", personality: "Stoic · Loyal · Surprisingly gentle", bio: "A rescue captain whose layered armor absorbs force and returns it through the earth.", costume: "Futuristic deel coat · lamellar shoulders · heavy riding boots", quote: "I do not move.", color: "#41d7bf", secondary: "#354b68", speed: 5, power: 9, reach: 5, mark: "B", kind: "human", portrait: { sheet: "main", index: 7 }, combo: { name: "STEPPE QUAKE", sequence: ["L", "Y", "U", "L"] } },
  { id: "lux", name: "LUX", alias: "PRISM FOX", city: "PARIS", style: "Trickster", special: "Mirror Dash", ultimate: "KALEIDOSCOPE HEIST", personality: "Playful · Elegant · Unreadable", bio: "A stage illusionist who turns refractive fashion into decoys and impossible angles.", costume: "Prismatic couture trench · fox visor · split-tail trousers", quote: "Catch the afterimage.", color: "#ffdc4a", secondary: "#a947ff", speed: 9, power: 6, reach: 8, mark: "L", kind: "human", portrait: { sheet: "main", index: 8 }, combo: { name: "PRISM FEINT", sequence: ["Y", "I", "I", "L"] } },
  { id: "oren", name: "OREN", alias: "TIDE MONK", city: "SYDNEY", style: "Control", special: "Breaker Wave", ultimate: "SOUTHERN DELUGE", personality: "Calm · Wry · Relentless", bio: "A coastal medic who learned to redirect momentum like water around stone.", costume: "Layered ocean robes · wrapped forearms · split training boots", quote: "Breathe between impacts.", color: "#48a8ff", secondary: "#42f5c5", speed: 6, power: 7, reach: 9, mark: "O", kind: "human", portrait: { sheet: "main", index: 9 }, combo: { name: "TIDAL FORM", sequence: ["I", "Y", "U", "L"] } },
  { id: "axiom", name: "AXIOM-7", alias: "BLUE STANDARD", city: "ORBITAL LAB", style: "Adaptive", special: "Vector Copy", ultimate: "PERFECT RECALL", personality: "Curious · Literal · Learning humor", bio: "A tournament training robot that entered the circuit to understand why humans fight for joy.", costume: "Cobalt segmented chassis · gyroscopic joints · expression-ring display", quote: "New pattern acquired.", color: "#3b7cff", secondary: "#9bdcff", speed: 7, power: 7, reach: 7, mark: "7", kind: "robot", portrait: { sheet: "bonus", index: 0 }, combo: { name: "MACHINE LEARNING", sequence: ["Y", "U", "I", "L"] } },
  { id: "cinder", name: "CINDER", alias: "MOSS COLOSSUS", city: "KRAKATOA", style: "Juggernaut", special: "Magma Bloom", ultimate: "MOUNTAIN AWAKES", personality: "Gentle · Ancient · Easily amused", bio: "A volcanic guardian who mistakes the World Circuit for an elaborate friendship ritual.", costume: "Basalt plates · moss mantle · glowing magma seams", quote: "Small friends hit loudly.", color: "#ff6b32", secondary: "#6fa85a", speed: 3, power: 10, reach: 8, mark: "C", kind: "monster", portrait: { sheet: "bonus", index: 1 }, combo: { name: "FAULT LINE", sequence: ["U", "L", "U", "L"] } },
  { id: "miko", name: "MIKO", alias: "SPARK MAKER", city: "OSAKA", style: "Gadget", special: "Drone Pop", ultimate: "BRIGHT IDEA BARRAGE", personality: "Inventive · Cheerful · Stubborn", bio: "A 12-year-old junior inventor competing in supervised exhibition matches with a safety drone.", costume: "Age-appropriate utility jacket · leggings · goggles · reinforced sneakers", quote: "I fixed it while you blinked!", color: "#ffd43b", secondary: "#42c7ff", speed: 8, power: 4, reach: 9, mark: "M", kind: "youth", portrait: { sheet: "bonus", index: 2 }, combo: { name: "TOOLBOX TANGO", sequence: ["Y", "I", "Y", "L"] } },
  { id: "teo", name: "TEO", alias: "RAIL RUNNER", city: "MADRID", style: "Skirmisher", special: "Kickflip Arc", ultimate: "CITYWIDE WALL RIDE", personality: "Brave · Social · Overconfident", bio: "A 13-year-old skating champion in padded exhibition gear who fights through speed challenges.", costume: "Age-appropriate teal hoodie · padded trousers · gloves · high-tops", quote: "Bet you can't keep up.", color: "#24d4c3", secondary: "#1768ff", speed: 10, power: 4, reach: 6, mark: "T", kind: "youth", portrait: { sheet: "bonus", index: 3 }, combo: { name: "RAIL COMBO", sequence: ["I", "I", "U", "L"] } },
  { id: "jun", name: "JUN", alias: "QUIET COMET", city: "SINGAPORE", style: "Technical", special: "Paper Crane", ultimate: "THOUSAND LESSONS", personality: "Thoughtful · Polite · Fiercely focused", bio: "A 14-year-old academy champion taking part in non-contact holographic circuit bouts.", costume: "Age-appropriate layered academy uniform · forearm pads · training shoes", quote: "Practice makes possibilities.", color: "#8ea8ff", secondary: "#f1f5ff", speed: 7, power: 5, reach: 8, mark: "J", kind: "youth", portrait: { sheet: "bonus", index: 4 }, combo: { name: "COMET LESSON", sequence: ["Y", "L", "I", "L"] } },
  { id: "raku", name: "RAKU", alias: "TIPSY SAGE", city: "CHENGDU", style: "Unorthodox", special: "Stagger Step", ultimate: "NINE-CUP MIRAGE", personality: "Mischievous · Wise · Generous", bio: "A 72-year-old tavern storyteller whose legendary drunken style is mostly theatre and perfect balance.", costume: "Weathered teal coat · loose training trousers · travel gourd · rope sash", quote: "I wobble. The world falls.", color: "#e4bb68", secondary: "#42a7a0", speed: 6, power: 7, reach: 7, mark: "R", kind: "elder", portrait: { sheet: "bonus", index: 5 }, combo: { name: "WANDERING CUP", sequence: ["U", "I", "Y", "L"] } },
];

const STAGES: Stage[] = [
  { id: "shibuya", name: "NEON CROSSING", city: "TOKYO", descriptor: "Rain / Midnight", color: "#27f4ff", accent: "#ff2dba", motif: "skyline" },
  { id: "hyperrail", name: "HYPERRAIL 88", city: "SEOUL", descriptor: "Transit / 02:14", color: "#3e9dff", accent: "#d8ff47", motif: "rail" },
  { id: "stormmarket", name: "STORM MARKET", city: "LAGOS", descriptor: "Monsoon / Live", color: "#ffbd31", accent: "#ff2dba", motif: "market" },
  { id: "aegis", name: "AEGIS FOUNDRY", city: "ATHENS", descriptor: "Smelter / Shift 3", color: "#ff623e", accent: "#ffd24a", motif: "forge" },
  { id: "voidclub", name: "VOID CLUB", city: "BERLIN", descriptor: "Sublevel / 140 BPM", color: "#a26cff", accent: "#27f4ff", motif: "club" },
  { id: "skycourt", name: "SKY COURT", city: "SÃO PAULO", descriptor: "Rooftop / Sunset", color: "#d8ff47", accent: "#28e2a0", motif: "court" },
  { id: "solarplaza", name: "SOLAR PLAZA", city: "MEXICO CITY", descriptor: "Festival / Golden Hour", color: "#ff405c", accent: "#ffb12b", motif: "plaza" },
  { id: "steppe", name: "STEPPE SHRINE", city: "ULAANBAATAR", descriptor: "High Wind / Dawn", color: "#41d7bf", accent: "#c9f5ff", motif: "steppe" },
  { id: "prismmetro", name: "PRISM METRO", city: "PARIS", descriptor: "Platform / Last Train", color: "#ffdc4a", accent: "#ff4da9", motif: "metro" },
  { id: "tidal", name: "TIDAL OPERA", city: "SYDNEY", descriptor: "Harbour / Blue Hour", color: "#48a8ff", accent: "#42f5c5", motif: "harbour" },
];

const OUTFITS: Outfit[] = [
  { id: "circuit", name: "CIRCUIT", note: "Tournament kit", cut: "classic" },
  { id: "afterdark", name: "AFTER DARK", note: "Sleek night look", cut: "sleek" },
  { id: "heatwave", name: "HEATWAVE", note: "Bold summer look", cut: "heatwave" },
];

const CONTROL_LABELS = [["A / D", "MOVE / BACK GUARD"], ["W / S", "JUMP / CROUCH"], ["Y", "LIGHT PUNCH"], ["U", "PUNCH"], ["I", "KICK"], ["L", "HEAVY KICK"], ["E", "EVASIVE ROLL"], ["SPACE", "GUARD"]];
const COMMAND_LABELS = [["↓ ↘ → + Y/U/I/L", "LONG-RANGE SPECIAL"], ["→ ↓ ↘ + Y/U", "RISING COUNTER"], ["↓ ↘ → ×2 + U/L", "CINEMATIC SUPER"], ["U + L", "BLOWBACK"], ["Y → U → I → L", "CANCEL COMBO"], ["O / P", "SPECIAL / SUPER SHORTCUTS"]];

const clamp = (n: number, min: number, max: number) => Math.max(min, Math.min(max, n));
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

function createCombatant(fighter: Fighter, x: number, facing: 1 | -1): Combatant {
  return { fighter, x, y: 566, vx: 0, vy: 0, facing, health: 100, drive: 65, grounded: true, crouching: false, guarding: false, attack: null, attackTime: 0, attackHit: false, hurtTime: 0, stunTime: 0, flashTime: 0, combo: 0, comboWindow: 0, guardMeter: 100, evadeTime: 0, queuedAttack: null, queuedTime: 0, wins: 0 };
}

function portraitStyle(fighter: Fighter) {
  return { "--fighter": fighter.color, "--fighter-2": fighter.secondary } as React.CSSProperties;
}

function portraitCropStyle(fighter: Fighter, compact: boolean) {
  const total = fighter.portrait.sheet === "main" ? 10 : 6;
  const position = fighter.portrait.index / (total - 1) * 100;
  const image = fighter.portrait.sheet === "main" ? "/characters/neon-clash-roster-concept.webp" : "/characters/neon-clash-bonus-roster-concept.webp";
  return compact
    ? { backgroundImage: `url(${image})`, backgroundSize: `${total * 100}% auto`, backgroundPosition: `${position}% top` } as React.CSSProperties
    : { backgroundImage: `url(${image})`, backgroundSize: "auto 100%", backgroundPosition: `${position}% center`, aspectRatio: fighter.portrait.sheet === "main" ? "153.6 / 757" : "256 / 768" } as React.CSSProperties;
}

function FighterPortrait({ fighter, compact = false }: { fighter: Fighter; compact?: boolean }) {
  return <span className={`fighter-portrait ${compact ? "compact" : "hero"} kind-${fighter.kind}`} style={portraitCropStyle(fighter, compact)} role="img" aria-label={`${fighter.name}, ${fighter.costume}`} />;
}

function FighterCard({ fighter, selected, onClick }: { fighter: Fighter; selected: boolean; onClick: () => void }) {
  return (
    <button className={`fighter-card ${selected ? "is-selected" : ""}`} onClick={onClick} style={portraitStyle(fighter)} aria-pressed={selected} aria-label={`Select ${fighter.name}, ${fighter.alias}`}>
      <span className="fighter-number">{String(FIGHTERS.indexOf(fighter) + 1).padStart(2, "0")}</span>
      <FighterPortrait fighter={fighter} compact />
      <span className="fighter-card-copy"><strong>{fighter.name}</strong><small>{fighter.style}</small></span>
    </button>
  );
}

export function NeonClash() {
  const [playerId, setPlayerId] = useState("kael");
  const [cpuId, setCpuId] = useState("zara");
  const [difficulty, setDifficulty] = useState<"ROOKIE" | "PRO" | "ACE">("PRO");
  const [stageId, setStageId] = useState("shibuya");
  const [outfitId, setOutfitId] = useState("circuit");
  const [mode, setMode] = useState<"CPU" | "ONLINE">("CPU");
  const [room, setRoom] = useState<RoomSession | null>(null);
  const [roomCode, setRoomCode] = useState("");
  const [roomBusy, setRoomBusy] = useState(false);
  const [roomError, setRoomError] = useState("");
  const [openRooms, setOpenRooms] = useState<OpenRoom[]>([]);
  const [lobbyOpen, setLobbyOpen] = useState(false);
  const [screen, setScreen] = useState<"select" | "fight">("select");
  const [outcome, setOutcome] = useState<string | null>(null);
  const [matchKey, setMatchKey] = useState(0);

  const player = useMemo(() => FIGHTERS.find((f) => f.id === playerId) ?? FIGHTERS[0], [playerId]);
  const cpu = useMemo(() => FIGHTERS.find((f) => f.id === cpuId) ?? FIGHTERS[1], [cpuId]);
  const stage = useMemo(() => STAGES.find((s) => s.id === stageId) ?? STAGES[0], [stageId]);
  const outfit = useMemo(() => OUTFITS.find((s) => s.id === outfitId) ?? OUTFITS[0], [outfitId]);
  const remoteInputRef = useRef<InputFrame>({});
  const remoteStateRef = useRef<MatchSnapshot | null>(null);
  const roomRef = useRef<RoomSession | null>(null);
  const lastStatePush = useRef(0);
  const statePushInFlight = useRef(false);
  const autoJoinAttemptedRef = useRef(false);

  const applyRoomConfig = useCallback((config: RoomConfig) => {
    setPlayerId(config.playerId); setCpuId(config.cpuId); setStageId(config.stageId); setDifficulty(config.difficulty); setOutfitId(config.outfitId);
  }, []);

  useEffect(() => { roomRef.current = room; }, [room]);
  useEffect(() => {
    const code = new URLSearchParams(window.location.search).get("room");
    if (!code) return;
    const linkedCode = code.toUpperCase().slice(0, 6);
    const timer = window.setTimeout(() => {
      setMode("ONLINE"); setRoomCode(linkedCode); setLobbyOpen(true);
      if (!autoJoinAttemptedRef.current && linkedCode.length === 6) { autoJoinAttemptedRef.current = true; void joinRoom(false, linkedCode); }
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  const refreshRooms = useCallback(async () => {
    try { const response = await fetch("/api/rooms", { cache: "no-store" }); const data = await response.json(); if (response.ok) setOpenRooms(data.rooms ?? []); } catch { /* lobby still works with a room code */ }
  }, []);

  useEffect(() => {
    if (!lobbyOpen) return;
    const timer = window.setTimeout(() => void refreshRooms(), 0);
    return () => window.clearTimeout(timer);
  }, [lobbyOpen, refreshRooms]);

  useEffect(() => {
    if (!room?.id || !room.token) return;
    let active = true;
    let pollInFlight = false;
    const poll = async () => {
      if (pollInFlight) return;
      pollInFlight = true;
      try {
        const response = await fetch(`/api/rooms/${room.id}?token=${encodeURIComponent(room.token)}`, { cache: "no-store" });
        const data = await response.json();
        if (!active || !response.ok) return;
        remoteInputRef.current = data.room.guestInput ?? {};
        if (room.role !== "host" && data.room.state && Object.keys(data.room.state).length) remoteStateRef.current = data.room.state;
        setRoom((current) => current ? { ...current, players: data.room.players, spectators: data.room.spectators, status: data.room.status, hostOnline: data.room.hostOnline, guestOnline: data.room.guestOnline } : current);
        if (room.role !== "host" && data.room.status === "fighting") { setLobbyOpen(false); setScreen("fight"); }
      } catch { if (active) setRoomError("ROOM CONNECTION INTERRUPTED — RETRYING"); }
      finally { pollInFlight = false; }
    };
    void poll(); const interval = window.setInterval(poll, room.role === "spectator" ? 180 : 90);
    return () => { active = false; window.clearInterval(interval); };
  }, [room?.id, room?.role, room?.token]);

  const createRoom = async () => {
    setRoomBusy(true); setRoomError("");
    try {
      const config: RoomConfig = { playerId, cpuId, stageId, difficulty, outfitId };
      const response = await fetch("/api/rooms", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ config }) });
      const data = await response.json(); if (!response.ok) throw new Error(data.error);
      setRoom(data.room); setRoomCode(data.room.id); setMode("ONLINE"); window.history.replaceState({}, "", `?room=${data.room.id}`);
    } catch (error) { setRoomError(error instanceof Error ? error.message : "ROOM CREATION FAILED"); }
    finally { setRoomBusy(false); }
  };

  async function joinRoom(watchOnly = false, selectedCode?: string) {
    const code = (selectedCode ?? roomCode).trim().toUpperCase(); if (!code) return;
    setRoomBusy(true); setRoomError("");
    try {
      const response = await fetch(`/api/rooms/${code}`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ action: watchOnly ? "spectate" : "join" }) });
      const data = await response.json(); if (!response.ok) throw new Error(data.error);
      applyRoomConfig(data.room.config); setRoom(data.room); setRoomCode(data.room.id); setMode("ONLINE"); window.history.replaceState({}, "", `?room=${data.room.id}`);
      if (data.room.role === "spectator" && data.room.status === "fighting") { setLobbyOpen(false); setScreen("fight"); }
    } catch (error) { setRoomError(error instanceof Error ? error.message : "ROOM JOIN FAILED"); }
    finally { setRoomBusy(false); }
  };

  const sendInput = useCallback((value: InputFrame) => {
    const session = roomRef.current; if (!session || session.role !== "guest") return;
    void fetch(`/api/rooms/${session.id}/input`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token: session.token, input: value }) });
  }, []);

  const publishSnapshot = useCallback((snapshot: MatchSnapshot) => {
    const session = roomRef.current; const now = performance.now();
    if (!session || session.role !== "host" || statePushInFlight.current || now - lastStatePush.current < 80) return;
    lastStatePush.current = now;
    statePushInFlight.current = true;
    void fetch(`/api/rooms/${session.id}/state`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token: session.token, state: snapshot, status: snapshot.roundState === "done" ? "complete" : "fighting" }) })
      .finally(() => { statePushInFlight.current = false; });
  }, []);

  const randomRival = useCallback((exclude: string) => {
    const pool = FIGHTERS.filter((f) => f.id !== exclude);
    setCpuId(pool[Math.floor(Math.random() * pool.length)].id);
  }, []);

  const startFight = () => {
    if (mode === "ONLINE" && (!room || (room.role === "host" && room.players < 2) || (room.role !== "host" && room.status !== "fighting"))) { setLobbyOpen(true); return; }
    if (playerId === cpuId) randomRival(playerId);
    setOutcome(null);
    setMatchKey((k) => k + 1);
    setScreen("fight");
  };

  return (
    <main className="game-shell">
      <div className="ambient-grid" aria-hidden="true" />
      <header className="topbar">
        <a className="brand" href="#top" aria-label="Neon Clash home"><span>NEON</span> CLASH <b>{"///"}</b></a>
        <div className="event-chip"><i /> WORLD CIRCUIT 01 · NIGHT STAGE</div>
        <div className="status-cluster"><button className="email-share" onClick={() => { const subject = encodeURIComponent("Play Neon Clash with me"); const body = encodeURIComponent(`Enter the World Circuit with me: ${window.location.href}`); window.location.href = `mailto:?subject=${subject}&body=${body}`; }}>EMAIL ↗</button><span>{mode === "CPU" ? "LOCAL // CPU" : room ? `${room.id} // ${room.role.toUpperCase()}` : "ONLINE // LOBBY"}</span><strong>60 FPS</strong></div>
      </header>

      {screen === "select" ? (
        <section className="select-screen" id="top">
          <div className="select-heading">
            <div><p className="eyebrow">FIGHTER SELECT / 16 CONTENDERS</p><h1>CHOOSE YOUR<br /><em>FREQUENCY</em></h1></div>
            <p className="intro">Humans, youth exhibition heroes, robots, monsters, and one famously tipsy master. Read every personality and finishing art before taking the circuit live.</p>
          </div>

          <div className="versus-preview solo-preview">
            <FighterPanel fighter={player} outfit={outfit} side="player" />
          </div>

          <div className="roster-wrap">
            <div className="roster-label"><span>ROSTER // 16 DISTINCT FIGHTERS // ONE ACTIVE SELECTION</span><small>RIVAL LOCKS WHEN THE MATCH STARTS</small></div>
            <div className="roster-grid">
              {FIGHTERS.map((fighter) => <FighterCard key={fighter.id} fighter={fighter} selected={fighter.id === playerId} onClick={() => { setPlayerId(fighter.id); if (fighter.id === cpuId) randomRival(fighter.id); }} />)}
            </div>
          </div>

          <div className="stage-select">
            <div className="roster-label"><span>ARENA SELECT // 10 LOCATIONS</span><button onClick={() => setStageId(STAGES[Math.floor(Math.random() * STAGES.length)].id)}>RANDOM STAGE ↻</button></div>
            <div className="stage-grid">
              {STAGES.map((item, index) => <button key={item.id} onClick={() => setStageId(item.id)} className={item.id === stageId ? "active" : ""} style={{ "--stage": item.color, "--stage-2": item.accent } as React.CSSProperties}><b>{String(index + 1).padStart(2, "0")}</b><span><strong>{item.name}</strong><small>{item.city} · {item.descriptor}</small></span></button>)}
            </div>
          </div>

          <div className="launch-row">
            <div className="launch-options">
              <div className="difficulty" role="group" aria-label="Battle mode"><span>MODE</span>{(["CPU", "ONLINE"] as const).map((item) => <button key={item} onClick={() => { setMode(item); if (item === "ONLINE") setLobbyOpen(true); }} className={mode === item ? "active" : ""}>{item === "CPU" ? "VS AI" : "ONLINE ROOMS"}</button>)}</div>
              <div className="difficulty" role="group" aria-label="CPU difficulty"><span>CPU LEVEL</span>{(["ROOKIE", "PRO", "ACE"] as const).map((level) => <button key={level} onClick={() => setDifficulty(level)} className={difficulty === level ? "active" : ""}>{level}</button>)}</div>
              <div className="difficulty outfit-picker" role="group" aria-label="Outfit"><span>OUTFIT</span>{OUTFITS.map((item) => <button key={item.id} title={item.note} onClick={() => setOutfitId(item.id)} className={outfitId === item.id ? "active" : ""}>{item.name}</button>)}</div>
            </div>
            <button className="fight-button" onClick={startFight}><span>START FIGHT</span><b>↗</b></button>
          </div>
        </section>
      ) : (
        <section className="fight-screen">
          <div className="broadcast-strip"><span>LIVE</span><p>{stage.name} {"//"} {stage.descriptor}</p><strong>{stage.city} · {player.name} ↔ {cpu.name}</strong></div>
          <GameCanvas key={matchKey} player={player} cpu={cpu} stage={stage} outfit={outfit} difficulty={difficulty} mode={mode} role={room?.role ?? "host"} remoteInputRef={remoteInputRef} remoteStateRef={remoteStateRef} sendInput={sendInput} onSnapshot={publishSnapshot} onMatchEnd={setOutcome} />
          <div className="control-deck">
            <div className="keyboard-map">
              {CONTROL_LABELS.map(([key, action]) => <div key={action}><kbd>{key}</kbd><span>{action}</span></div>)}
            </div>
            <div className="command-map" aria-label="Motion commands">
              {COMMAND_LABELS.map(([command, action]) => <div key={action}><kbd>{command}</kbd><span>{action}</span></div>)}
            </div>
            <button className="menu-button" onClick={() => setScreen("select")}>← FIGHTER SELECT</button>
          </div>
          {outcome && <div className="result-modal"><p>MATCH COMPLETE</p><h2>{outcome}</h2><div><button onClick={() => { setOutcome(null); setMatchKey((k) => k + 1); }}>REMATCH</button><button onClick={() => setScreen("select")}>CHANGE FIGHTER</button></div></div>}
        </section>
      )}

      {lobbyOpen && <RoomLobby room={room} code={roomCode} setCode={setRoomCode} busy={roomBusy} error={roomError} openRooms={openRooms} createRoom={createRoom} joinRoom={joinRoom} startFight={() => { setLobbyOpen(false); startFight(); }} close={() => setLobbyOpen(false)} />}

      <footer><span>NC // BUILD 01.24</span><p>ORIGINAL BROWSER FIGHTER · KEYBOARD + TOUCH</p><span>PERFORMANCE: AUTO</span></footer>
    </main>
  );
}

function RoomLobby({ room, code, setCode, busy, error, openRooms, createRoom, joinRoom, startFight, close }: { room: RoomSession | null; code: string; setCode: (value: string) => void; busy: boolean; error: string; openRooms: OpenRoom[]; createRoom: () => void; joinRoom: (watchOnly?: boolean, code?: string) => void; startFight: () => void; close: () => void }) {
  const [copied, setCopied] = useState("");
  const [inviteUrl, setInviteUrl] = useState("");
  const [networkReady, setNetworkReady] = useState(false);
  useEffect(() => {
    setInviteUrl(`${window.location.origin}${window.location.pathname}?room=${room?.id ?? code}`);
    setNetworkReady(!["localhost", "127.0.0.1"].includes(window.location.hostname));
  }, [code, room?.id]);
  const copyText = async (value: string, label: string) => {
    try {
      if (window.isSecureContext && navigator.clipboard) await navigator.clipboard.writeText(value);
      else {
        const field = document.createElement("textarea");
        field.value = value; field.style.position = "fixed"; field.style.opacity = "0";
        document.body.appendChild(field); field.select(); document.execCommand("copy"); field.remove();
      }
      setCopied(label); window.setTimeout(() => setCopied(""), 1600);
    } catch { setCopied("COPY FAILED — SELECT LINK ABOVE"); }
  };
  const email = () => { window.location.href = `mailto:?subject=${encodeURIComponent("Join my Neon Clash room")}&body=${encodeURIComponent(`Room ${room?.id ?? code}: ${inviteUrl}`)}`; };
  return <div className="online-backdrop"><section className="online-lobby"><button className="close-lobby" onClick={close}>×</button><p className="eyebrow">CROSS-COMPUTER MATCHMAKING // LIVE SPECTATORS</p><h2>{room ? `ROOM ${room.id}` : "ENTER THE LOBBY"}</h2><p className="lobby-copy">Create a room and open its Player 2 link on the other computer. Invite links now join automatically.</p>{room ? <div className="room-console"><div className="slot-row"><span className={room.hostOnline === false ? "waiting" : "filled"}>P1<br /><b>{room.hostOnline === false ? "OFFLINE" : "HOST"}</b></span><i>VS</i><span className={room.players === 2 ? "filled" : "waiting"}>P2<br /><b>{room.players === 2 ? "CONNECTED" : "WAITING"}</b></span></div><div className="room-metrics"><span>{room.players}/2 PLAYERS</span><span>{room.spectators} WATCHING</span><span>{room.status.toUpperCase()}</span></div><div className="invite-strip"><label>PLAYER 2 AUTO-JOIN LINK</label><input value={inviteUrl} readOnly aria-label="Player 2 invite link" /><small>{networkReady ? "Ready for another computer on this network." : "For another computer, open Neon Clash using this computer's network address before copying."}</small></div><div className="room-actions"><button onClick={() => void copyText(inviteUrl, "LINK COPIED")}>{copied || "COPY PLAYER 2 LINK"}</button><a href={inviteUrl} target="_blank" rel="noreferrer">OPEN PLAYER 2 LINK</a><button onClick={() => void copyText(room.id, "ROOM CODE COPIED")}>COPY ROOM CODE</button><button onClick={email}>EMAIL INVITE</button>{room.role === "host" && <button className="primary" disabled={room.players < 2} onClick={startFight}>{room.players < 2 ? "WAITING FOR P2" : "START MATCH"}</button>}{room.role !== "host" && <button className="primary" disabled>{room.role === "spectator" ? "WATCHING ROOM" : room.hostOnline === false ? "HOST DISCONNECTED" : "WAITING FOR HOST"}</button>}</div></div> : <div className="lobby-grid"><div><b>HOST ON THIS COMPUTER</b><span>Create a six-character room and share the auto-join link with Player 2.</span><button className="primary" disabled={busy} onClick={createRoom}>CREATE TWO-PLAYER ROOM</button></div><div><b>JOIN FROM ANOTHER COMPUTER</b><input value={code} onChange={(event) => setCode(event.target.value.toUpperCase().slice(0, 6))} aria-label="Room code" placeholder="6-DIGIT ROOM CODE" /><button disabled={busy || code.length !== 6} onClick={() => joinRoom(false)}>JOIN AS PLAYER 2</button><button disabled={busy || code.length !== 6} onClick={() => joinRoom(true)}>WATCH ONLY</button></div></div>}{error && <strong className="connection-state failed">{error}</strong>}{!room && openRooms.length > 0 && <div className="open-room-list"><b>OPEN ROOMS</b>{openRooms.map((item) => <div key={item.id}><span><strong>{item.id}</strong><small>{item.players}/2 · {item.status.toUpperCase()}</small></span><button onClick={() => joinRoom(item.players >= 2, item.id)}>{item.players < 2 ? "JOIN" : "WATCH"}</button></div>)}</div>}</section></div>;
}

function FighterPanel({ fighter, outfit, side }: { fighter: Fighter; outfit: Outfit; side: "player" | "cpu" }) {
  return (
    <article className={`fighter-panel ${side}`} style={portraitStyle(fighter)}>
      <div className="panel-no">{side === "player" ? "P1" : "CPU"}</div>
      <div className="hero-mark"><FighterPortrait fighter={fighter} /></div>
      <div className="fighter-copy">
        <p>{fighter.city} {"//"} {fighter.style} {"//"} {fighter.kind.toUpperCase()}</p><h2>{fighter.name}</h2><h3>{fighter.alias}</h3><blockquote>“{fighter.quote}”</blockquote>
        <div className="personality-card"><strong>{fighter.personality}</strong><span>{fighter.bio}</span><small>{outfit.name}: {fighter.costume}</small></div>
        <div className="stat-row"><Stat label="SPD" value={fighter.speed} /><Stat label="PWR" value={fighter.power} /><Stat label="RNG" value={fighter.reach} /></div>
        <div className="special-tag"><span>SPECIAL</span><strong>{fighter.special}</strong></div>
        <div className="ultimate-tag"><span>CINEMATIC FINISH</span><strong>{fighter.ultimate}</strong></div>
        <div className="combo-tag"><span>{fighter.combo.name}</span><strong>{fighter.combo.sequence.join(" › ")}</strong></div>
      </div>
    </article>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return <div><span>{label}</span><i><b style={{ width: `${value * 10}%` }} /></i><em>{value}</em></div>;
}

function GameCanvas({ player, cpu, stage, outfit, difficulty, mode, role, remoteInputRef, remoteStateRef, sendInput, onSnapshot, onMatchEnd }: { player: Fighter; cpu: Fighter; stage: Stage; outfit: Outfit; difficulty: "ROOKIE" | "PRO" | "ACE"; mode: "CPU" | "ONLINE"; role: RoomRole; remoteInputRef: React.RefObject<InputFrame>; remoteStateRef: React.RefObject<MatchSnapshot | null>; sendInput: (value: InputFrame) => void; onSnapshot: (value: MatchSnapshot) => void; onMatchEnd: (value: string) => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const inputRef = useRef<InputFrame>({ actionSeq: 0, action: "" });

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d", { alpha: false });
    if (!ctx) return;

    const W = 1280, H = 720, FLOOR = 584;
    const lowPower = (navigator as Navigator & { deviceMemory?: number }).deviceMemory !== undefined && ((navigator as Navigator & { deviceMemory?: number }).deviceMemory ?? 8) <= 4;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const dpr = Math.min(window.devicePixelRatio || 1, lowPower ? 1 : 1.25);
    canvas.width = Math.round(W * dpr); canvas.height = Math.round(H * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.imageSmoothingEnabled = true;

    const background = document.createElement("canvas");
    background.width = W; background.height = H;
    const bg = background.getContext("2d")!;
    drawBackground(bg, W, H, stage);

    const spriteSheets: Partial<Record<string, HTMLImageElement>> = {};
    for (const [id, src] of Object.entries({ kael: "/characters/kael-combat-sprites-v2.png", zara: "/characters/zara-combat-sprites-v2.png" })) {
      const image = new Image(); image.decoding = "async"; image.src = src; spriteSheets[id] = image;
    }
    const illustratedSprites = new Map<string, Record<SpritePose, HTMLCanvasElement>>();
    for (const fighter of [player, cpu]) {
      illustratedSprites.set(fighter.id, {
        idle: buildSprite(fighter, outfit, "idle"),
        punch: buildSprite(fighter, outfit, "punch"),
        kick: buildSprite(fighter, outfit, "kick"),
        special: buildSprite(fighter, outfit, "special"),
        guard: buildSprite(fighter, outfit, "guard"),
        hurt: buildSprite(fighter, outfit, "hurt"),
      });
    }
    const p1 = createCombatant(player, 350, 1);
    const p2 = createCombatant(cpu, 930, -1);
    const projectiles: Projectile[] = [];
    const particles: Particle[] = [];
    const inputs = inputRef.current;
    let timer = 75;
    let round = 1;
    let roundState: "intro" | "fight" | "ko" | "done" = "intro";
    let stateTimer = 1.25;
    let aiTimer = 0;
    let aiIntent = "idle";
    let shake = 0;
    let last = performance.now();
    let accumulator = 0;
    let raf = 0;
    let running = true;
    const fixed = 1 / 60;
    const particleCap = lowPower || reduceMotion ? 36 : 90;
    const aiRate = difficulty === "ROOKIE" ? 0.56 : difficulty === "PRO" ? 0.33 : 0.19;
    const inputHistory: Array<{ key: string; at: number }> = [];
    const directionHistory: Array<{ key: string; at: number }> = [];
    const previousWants = new Map<Combatant, Record<string, boolean>>([[p1, {}], [p2, {}]]);
    let lastRemoteActionSeq = -1;
    let snapshotSequence = 0;
    let lastAppliedRemoteSequence = -1;
    let remoteSnapshotReceivedAt = performance.now();
    let comboBonus = 0;
    let comboCallout = 0;
    let finisherTime = 0;
    let finisherName = "";
    let finisherColor = player.color;

    const recordAction = (key: string) => {
      const now = performance.now(); inputHistory.push({ key, at: now });
      while (inputHistory.length > 8 || (inputHistory[0] && now - inputHistory[0].at > 1200)) inputHistory.shift();
      const sequence = player.combo.sequence;
      const tail = inputHistory.slice(-sequence.length);
      if (tail.length === sequence.length && tail.every((item, index) => item.key === sequence[index]) && tail[tail.length - 1].at - tail[0].at <= 1100) {
        comboBonus = 12; comboCallout = 1.25; p1.drive = clamp(p1.drive + 18, 0, 100); inputHistory.length = 0;
        return true;
      }
      return false;
    };

    const applyCombatantSnapshot = (target: Combatant, value: CombatantSnapshot) => {
      target.x = value.x; target.y = value.y; target.vx = value.vx; target.vy = value.vy; target.facing = value.facing;
      target.health = value.health; target.drive = value.drive; target.grounded = value.grounded; target.crouching = value.crouching; target.guarding = value.guarding;
      target.attack = value.attack; target.attackTime = value.attackTime; target.attackHit = value.attackHit; target.hurtTime = value.hurtTime; target.stunTime = value.stunTime;
      target.flashTime = value.flashTime; target.combo = value.combo; target.comboWindow = value.comboWindow; target.wins = value.wins;
      target.guardMeter = value.guardMeter ?? 100; target.evadeTime = value.evadeTime ?? 0;
    };

    const applyRemoteSnapshot = () => {
      const snapshot = remoteStateRef.current; if (!snapshot) return;
      const nextSequence = Number(snapshot.sequence ?? 0);
      if (nextSequence !== lastAppliedRemoteSequence) { lastAppliedRemoteSequence = nextSequence; remoteSnapshotReceivedAt = performance.now(); }
      const prediction = Math.min(0.1, Math.max(0, (performance.now() - remoteSnapshotReceivedAt) / 1000));
      applyCombatantSnapshot(p1, snapshot.p1); applyCombatantSnapshot(p2, snapshot.p2); timer = snapshot.timer; round = snapshot.round; roundState = snapshot.roundState;
      for (const combatant of [p1, p2]) {
        combatant.x = clamp(combatant.x + combatant.vx * prediction, 92, W - 92);
        combatant.y += combatant.vy * prediction;
        combatant.attackTime += prediction;
        combatant.hurtTime = Math.max(0, combatant.hurtTime - prediction);
        combatant.stunTime = Math.max(0, combatant.stunTime - prediction);
      }
      projectiles.length = 0;
      for (const item of snapshot.projectiles ?? []) projectiles.push({ ...item, x: item.x + item.vx * prediction, life: Math.max(0, item.life - prediction), owner: item.owner === 1 ? p1 : p2 });
    };

    const combatantSnapshot = (value: Combatant): CombatantSnapshot => ({ x: value.x, y: value.y, vx: value.vx, vy: value.vy, facing: value.facing, health: value.health, drive: value.drive, grounded: value.grounded, crouching: value.crouching, guarding: value.guarding, attack: value.attack, attackTime: value.attackTime, attackHit: value.attackHit, hurtTime: value.hurtTime, stunTime: value.stunTime, flashTime: value.flashTime, combo: value.combo, comboWindow: value.comboWindow, guardMeter: value.guardMeter, evadeTime: value.evadeTime, wins: value.wins });

    const snapshot = (): MatchSnapshot => ({ sequence: ++snapshotSequence, p1: combatantSnapshot(p1), p2: combatantSnapshot(p2), timer, round, roundState, projectiles: projectiles.map((item) => ({ x: item.x, y: item.y, vx: item.vx, life: item.life, color: item.color, damage: item.damage, owner: item.owner === p1 ? 1 : 2 })) });

    const burst = (x: number, y: number, color: string, count: number) => {
      const room = particleCap - particles.length;
      for (let i = 0; i < Math.min(room, count); i++) {
        const angle = Math.random() * Math.PI * 2;
        const speed = 70 + Math.random() * 260;
        particles.push({ x, y, vx: Math.cos(angle) * speed, vy: Math.sin(angle) * speed, life: 0.25 + Math.random() * 0.35, maxLife: 0.6, color, size: 2 + Math.random() * 8 });
      }
    };

    const startAttack = (c: Combatant, type: Combatant["attack"], cancel = false) => {
      if (!type || (!cancel && c.attack) || c.hurtTime > 0 || c.stunTime > 0 || c.evadeTime > 0 || roundState !== "fight") return false;
      const costs = { lightPunch: 0, heavyPunch: 0, lightKick: 0, heavyKick: 0, special: 25, impact: 32, super: 65 };
      if (c.drive < costs[type]) return false;
      c.drive -= costs[type]; c.attack = type; c.attackTime = 0; c.attackHit = false; c.guarding = false;
      if (type === "special" || type === "super") {
        burst(c.x + c.facing * 58, c.y - 126, c.fighter.color, type === "super" ? 46 : c === p1 && comboBonus > 0 ? 34 : 14);
        if (type === "super" || (c === p1 && comboBonus > 0)) { finisherTime = type === "super" ? 1.55 : 1.15; finisherName = c.fighter.ultimate; finisherColor = c.fighter.color; shake = type === "super" ? 23 : 16; }
      }
      if (type === "special" || type === "super") {
        const rangedStyle = c.fighter.style === "Zoner" || c.fighter.style === "Control" || c.fighter.style === "Gadget";
        projectiles.push({ x: c.x + c.facing * 76, y: c.y - 122, vx: c.facing * (type === "super" ? 760 : (rangedStyle ? 610 : 525) + c.fighter.reach * 10), life: type === "super" ? 2.35 : 1.95, owner: c, color: c.fighter.color, damage: (type === "super" ? 27 : rangedStyle ? 14 : 11) + c.fighter.power * 0.45 + (c === p1 ? comboBonus : 0) });
      }
      return true;
    };

    const hit = (attacker: Combatant, defender: Combatant, damage: number, force: number, color: string, impact = false) => {
      if (defender.hurtTime > 0.02 || defender.evadeTime > 0.06 || roundState !== "fight") return;
      const blocked = defender.guarding && defender.grounded && defender.facing === -attacker.facing;
      const dealt = blocked ? damage * 0.28 : damage;
      defender.health = clamp(defender.health - dealt, 0, 100);
      defender.drive = clamp(defender.drive - (blocked ? 7 : 3), 0, 100);
      defender.vx = attacker.facing * force * (blocked ? 0.35 : 1);
      defender.hurtTime = blocked ? 0.12 : impact ? 0.46 : 0.24;
      defender.flashTime = 0.1;
      if (blocked) {
        defender.guardMeter = clamp(defender.guardMeter - damage * 2.6, 0, 100);
        if (defender.guardMeter <= 0) { defender.guarding = false; defender.stunTime = 1.05; defender.hurtTime = 0.42; defender.guardMeter = 42; }
      }
      if (!blocked && impact) defender.stunTime = 0.35;
      attacker.drive = clamp(attacker.drive + (blocked ? 3 : 8), 0, 100);
      attacker.combo = attacker.comboWindow > 0 ? attacker.combo + 1 : 1;
      attacker.comboWindow = 0.8;
      shake = Math.max(shake, impact ? 13 : blocked ? 2 : 6);
      burst(defender.x, defender.y - 112, blocked ? "#e8fbff" : color, blocked ? 6 : impact ? 22 : 12);
    };

    const attackData = (type: NonNullable<Combatant["attack"]>) => ({
      lightPunch: { activeA: 0.05, activeB: 0.125, end: 0.22, range: 72, damage: 5.2, force: 145 },
      heavyPunch: { activeA: 0.13, activeB: 0.25, end: 0.43, range: 104, damage: 10.8, force: 270 },
      lightKick: { activeA: 0.075, activeB: 0.165, end: 0.28, range: 88, damage: 6.4, force: 180 },
      heavyKick: { activeA: 0.16, activeB: 0.29, end: 0.49, range: 122, damage: 12.2, force: 315 },
      special: { activeA: 0.16, activeB: 0.32, end: 0.58, range: 138, damage: 14, force: 340 },
      impact: { activeA: 0.23, activeB: 0.39, end: 0.62, range: 126, damage: 18, force: 430 },
      super: { activeA: 0.19, activeB: 0.48, end: 0.82, range: 178, damage: 28, force: 510 },
    }[type]);

    const updateCombatant = (c: Combatant, foe: Combatant, move: number, wants: Record<string, boolean>, dt: number) => {
      const previous = previousWants.get(c) ?? {};
      const pressed = Object.fromEntries(Object.keys(wants).map((key) => [key, !!wants[key] && !previous[key]])) as Record<string, boolean>;
      const requestedAttack = (): Combatant["attack"] => pressed.super ? "super" : pressed.impact ? "impact" : pressed.special ? "special" : pressed.heavyKick ? "heavyKick" : pressed.heavyPunch ? "heavyPunch" : pressed.lightKick ? "lightKick" : pressed.lightPunch ? "lightPunch" : null;
      const freshAttack = requestedAttack();
      if (freshAttack) { c.queuedAttack = freshAttack; c.queuedTime = 0.24; }
      else { c.queuedTime = Math.max(0, c.queuedTime - dt); if (c.queuedTime === 0) c.queuedAttack = null; }
      c.facing = c.x < foe.x ? 1 : -1;
      c.hurtTime = Math.max(0, c.hurtTime - dt); c.stunTime = Math.max(0, c.stunTime - dt); c.flashTime = Math.max(0, c.flashTime - dt);
      c.evadeTime = Math.max(0, c.evadeTime - dt); c.guardMeter = clamp(c.guardMeter + dt * (c.guarding ? 1.2 : 8), 0, 100);
      c.comboWindow = Math.max(0, c.comboWindow - dt); if (c.comboWindow === 0) c.combo = 0;
      c.drive = clamp(c.drive + dt * (c.guarding ? 1.5 : 5.5), 0, 100);
      c.crouching = wants.crouch && c.grounded && !c.attack;
      c.guarding = (wants.guard || move === -c.facing) && c.grounded && !c.attack && c.hurtTime <= 0 && c.evadeTime <= 0;
      if (pressed.evade && c.grounded && !c.attack && c.hurtTime <= 0 && c.stunTime <= 0 && c.drive >= 10) { c.evadeTime = 0.36; c.drive -= 10; c.vx = c.facing * 520; c.guarding = false; burst(c.x, c.y - 55, c.fighter.color, 8); }
      if (c.hurtTime <= 0 && c.stunTime <= 0 && c.evadeTime <= 0 && !c.attack && roundState === "fight") {
        const speed = (190 + c.fighter.speed * 16) * (c.crouching || c.guarding ? 0.22 : 1);
        c.vx = lerp(c.vx, move * speed, 0.26);
        if (pressed.jump && c.grounded && !c.crouching && !c.guarding) { c.vy = -(550 + c.fighter.speed * 7); c.grounded = false; }
        if (c.queuedAttack && startAttack(c, c.queuedAttack)) { c.queuedAttack = null; c.queuedTime = 0; }
      } else if (c.hurtTime > 0 || c.stunTime > 0) c.guarding = false;

      if (c.attack) {
        c.attackTime += dt;
        const data = attackData(c.attack);
        const isProjectileSpecial = c.attack === "special" || c.attack === "super";
        if (!c.attackHit && !isProjectileSpecial && c.attackTime >= data.activeA && c.attackTime <= data.activeB && Math.abs(c.x - foe.x) < data.range + c.fighter.reach * 2 && Math.abs(c.y - foe.y) < 105) {
          c.attackHit = true; hit(c, foe, data.damage + c.fighter.power * 0.42 + (c === p1 && c.attack === "special" ? comboBonus : 0), data.force, c.fighter.color, c.attack === "impact" || c.attack === "super");
        }
        if ((c.attack === "special" || c.attack === "super") && !isProjectileSpecial && c.attackTime < 0.42) c.vx += c.facing * (c.attack === "super" ? 52 : 28);
        const next = c.queuedAttack;
        const rank = { lightPunch: 1, lightKick: 1, heavyPunch: 2, heavyKick: 2, impact: 3, special: 4, super: 5 };
        if (next && c.attackHit && c.comboWindow > 0 && rank[next] > rank[c.attack] && startAttack(c, next, true)) { c.queuedAttack = null; c.queuedTime = 0; }
        else if (c.attackTime >= data.end) { if (c === p1 && (c.attack === "special" || c.attack === "super")) comboBonus = 0; c.attack = null; c.attackTime = 0; }
      }

      c.vy += 1450 * dt; c.x += c.vx * dt; c.y += c.vy * dt; c.vx *= c.grounded ? 0.82 : 0.985;
      if (c.y >= FLOOR) { c.y = FLOOR; c.vy = 0; c.grounded = true; } else c.grounded = false;
      c.x = clamp(c.x, 82, W - 82);
      previousWants.set(c, { ...wants });
    };

    const cpuWants = (dt: number) => {
      aiTimer -= dt;
      const distance = Math.abs(p1.x - p2.x);
      if (aiTimer <= 0) {
        aiTimer = aiRate * (0.72 + Math.random() * 0.75);
        const r = Math.random();
        if (p2.health < 28 && r < 0.26) aiIntent = "guard";
        else if (distance > 300) aiIntent = p2.fighter.style === "Zoner" && p2.drive >= 25 && r < 0.52 ? "special" : "approach";
        else if (distance < 95) aiIntent = r < 0.18 ? "retreat" : r < 0.34 ? "guard" : r < 0.54 ? "lightPunch" : r < 0.72 ? "lightKick" : r < 0.88 ? "heavyKick" : p2.drive >= 32 ? "impact" : "jump";
        else aiIntent = r < 0.24 ? "approach" : r < 0.43 ? "jump" : r < 0.62 ? "heavyPunch" : r < 0.78 ? "heavyKick" : p2.drive >= 25 ? "special" : "lightPunch";
      }
      return { move: aiIntent === "approach" ? p2.facing : aiIntent === "retreat" ? -p2.facing : 0, jump: aiIntent === "jump", crouch: false, guard: aiIntent === "guard", evade: false, lightPunch: aiIntent === "lightPunch", heavyPunch: aiIntent === "heavyPunch", lightKick: aiIntent === "lightKick", heavyKick: aiIntent === "heavyKick", special: aiIntent === "special", impact: aiIntent === "impact", super: false };
    };

    const resetRound = () => {
      p1.x = 350; p1.y = FLOOR; p1.vx = p1.vy = 0; p1.health = 100; p1.drive = 65; p1.guardMeter = 100; p1.evadeTime = 0; p1.attack = null; p1.queuedAttack = null; p1.queuedTime = 0; p1.hurtTime = p1.stunTime = 0;
      p2.x = 930; p2.y = FLOOR; p2.vx = p2.vy = 0; p2.health = 100; p2.drive = 65; p2.guardMeter = 100; p2.evadeTime = 0; p2.attack = null; p2.queuedAttack = null; p2.queuedTime = 0; p2.hurtTime = p2.stunTime = 0;
      projectiles.length = 0; particles.length = 0; timer = 75; roundState = "intro"; stateTimer = 1.15;
    };

    const update = (dt: number) => {
      if (mode === "ONLINE" && role !== "host") { applyRemoteSnapshot(); return; }
      if (roundState === "intro") { stateTimer -= dt; if (stateTimer <= 0) roundState = "fight"; }
      else if (roundState === "fight") timer = Math.max(0, timer - dt);
      else if (roundState === "ko") {
        stateTimer -= dt;
        if (stateTimer <= 0) {
          if (p1.wins >= 2 || p2.wins >= 2) { roundState = "done"; onMatchEnd(`${p1.wins >= 2 ? p1.fighter.name : p2.fighter.name} WINS`); }
          else { round += 1; resetRound(); }
        }
      }

      comboCallout = Math.max(0, comboCallout - dt); finisherTime = Math.max(0, finisherTime - dt);
      const local = { jump: !!inputs.jump, crouch: !!inputs.crouch, guard: !!inputs.guard, evade: !!inputs.evade, lightPunch: !!inputs.lightPunch, heavyPunch: !!inputs.heavyPunch, lightKick: !!inputs.lightKick, heavyKick: !!inputs.heavyKick, special: !!inputs.special, impact: !!inputs.impact, super: !!inputs.super };
      const localMove = (inputs.left ? -1 : 0) + (inputs.right ? 1 : 0);
      const remote = remoteInputRef.current ?? {};
      const remoteMove = (remote.left ? -1 : 0) + (remote.right ? 1 : 0);
      const remoteWants: Record<string, boolean> = { jump: !!remote.jump, crouch: !!remote.crouch, guard: !!remote.guard, evade: !!remote.evade, lightPunch: false, heavyPunch: false, lightKick: false, heavyKick: false, special: false, impact: false, super: false };
      const remoteSeq = Number(remote.actionSeq ?? -1);
      const remoteAction = String(remote.action ?? "");
      if (remoteSeq !== lastRemoteActionSeq && ["lightPunch", "heavyPunch", "lightKick", "heavyKick", "special", "impact", "super"].includes(remoteAction)) {
        remoteWants[remoteAction] = true;
        lastRemoteActionSeq = remoteSeq;
      }
      const ai = cpuWants(dt);
      const { move: aiMove, ...aiWants } = ai;
      updateCombatant(p1, p2, localMove, local, dt);
      updateCombatant(p2, p1, mode === "CPU" ? aiMove : remoteMove, mode === "CPU" ? aiWants : remoteWants, dt);

      const gap = Math.abs(p1.x - p2.x);
      if (gap < 86 && Math.abs(p1.y - p2.y) < 110) { const push = (86 - gap) * 0.5; p1.x -= p1.facing * push; p2.x -= p2.facing * push; }

      for (let i = projectiles.length - 1; i >= 0; i--) {
        const q = projectiles[i]; q.x += q.vx * dt; q.life -= dt;
        const target = q.owner === p1 ? p2 : p1;
        if (Math.abs(q.x - target.x) < 48 && Math.abs(q.y - (target.y - 105)) < 75) { hit(q.owner, target, q.damage, 280, q.color); projectiles.splice(i, 1); }
        else if (q.life <= 0 || q.x < -60 || q.x > W + 60) projectiles.splice(i, 1);
      }
      for (let i = particles.length - 1; i >= 0; i--) { const q = particles[i]; q.life -= dt; q.x += q.vx * dt; q.y += q.vy * dt; q.vy += 760 * dt; q.vx *= 0.97; if (q.life <= 0) particles.splice(i, 1); }
      shake *= 0.84;

      if (roundState === "fight" && (p1.health <= 0 || p2.health <= 0 || timer <= 0)) {
        roundState = "ko"; stateTimer = 2.25;
        const winner = p1.health === p2.health ? (Math.random() > 0.5 ? p1 : p2) : p1.health > p2.health ? p1 : p2;
        winner.wins += 1; shake = 18; burst((p1.x + p2.x) / 2, 330, winner.fighter.color, particleCap);
      }
    };

    const drawCombatant = (c: Combatant) => {
      const attack = c.attack ? attackData(c.attack) : null;
      const reach = c.attack && attack && c.attackTime > attack.activeA * 0.75 && c.attackTime < attack.activeB ? (c.attack === "lightPunch" ? 18 : c.attack === "lightKick" ? 26 : c.attack === "impact" ? 44 : 34) : 0;
      const now = performance.now();
      const bob = c.grounded ? Math.sin(now * 0.004) * 2 : 0;
      ctx.save(); ctx.translate(c.x + c.facing * reach, c.y + bob); ctx.scale(c.facing, 1);
      ctx.fillStyle = "rgba(0,0,0,.42)"; ctx.beginPath(); ctx.ellipse(0, 2, c.fighter.kind === "monster" ? 78 : 58, 11, 0, 0, Math.PI * 2); ctx.fill();
      if (c.guarding) { ctx.globalAlpha = 0.42; ctx.strokeStyle = c.fighter.color; ctx.lineWidth = 10; ctx.beginPath(); ctx.arc(6, -125, 88, -1.25, 1.25); ctx.stroke(); ctx.globalAlpha = 1; }
      if (c.evadeTime > 0) ctx.globalAlpha = 0.42;
      if (c.flashTime > 0) ctx.globalCompositeOperation = "screen";
      const attackProgress = c.attack && attack ? clamp(c.attackTime / attack.end, 0, 1) : 0;
      const sprite = spriteSheets[c.fighter.id];
      if (sprite?.complete && sprite.naturalWidth > 0) {
        const moving = c.grounded && Math.abs(c.vx) > 30 && !c.attack && !c.guarding && c.hurtTime <= 0;
        const frame = c.hurtTime > 0 ? 7 : c.guarding || c.evadeTime > 0 ? 6 : !c.grounded ? 3 : c.attack === "lightKick" || c.attack === "heavyKick" ? 5 : c.attack ? 4 : moving ? (Math.floor(now / 130) % 2 ? 1 : 2) : 0;
        const cellW = sprite.naturalWidth / 4, cellH = sprite.naturalHeight / 2;
        const sx = (frame % 4) * cellW, sy = Math.floor(frame / 4) * cellH;
        const destH = c.fighter.id === "zara" ? 382 : 365;
        const destW = destH * cellW / cellH;
        const lean = c.hurtTime > 0 ? -0.08 : c.attack ? (frame === 5 ? -0.025 : 0.035) * Math.sin(Math.PI * attackProgress) : 0;
        ctx.save(); ctx.rotate(lean);
        if (c.crouching) { ctx.translate(0, 42); ctx.scale(1, .86); }
        if (c.attack === "special" || c.attack === "super") {
          ctx.globalAlpha = .34; ctx.strokeStyle = c.fighter.color; ctx.lineWidth = c.attack === "super" ? 13 : 8; ctx.beginPath(); ctx.ellipse(0, -destH * .48, destW * .3, destH * .48, 0, 0, Math.PI * 2); ctx.stroke(); ctx.globalAlpha = 1;
        }
        ctx.drawImage(sprite, sx, sy, cellW, cellH, -destW / 2, -destH, destW, destH);
        ctx.restore();
      } else {
        const pose: SpritePose = c.hurtTime > 0 ? "hurt" : c.guarding || c.evadeTime > 0 ? "guard" : c.attack === "lightKick" || c.attack === "heavyKick" ? "kick" : c.attack === "special" || c.attack === "super" ? "special" : c.attack ? "punch" : "idle";
        const fallback = illustratedSprites.get(c.fighter.id)?.[pose];
        if (fallback) {
          const destH = c.fighter.kind === "monster" ? 390 : c.fighter.kind === "youth" ? 326 : 365;
          const destW = destH * fallback.width / fallback.height;
          ctx.save();
          if (c.crouching) { ctx.translate(0, 42); ctx.scale(1, .86); }
          ctx.drawImage(fallback, -destW / 2, -destH, destW, destH);
          ctx.restore();
        }
      }
      ctx.restore();
      if (c.combo > 1) { ctx.fillStyle = c.fighter.color; ctx.font = "900 22px Arial"; ctx.textAlign = "center"; ctx.fillText(`${c.combo} HIT`, c.x, c.y - 330); }
    };

    const drawHud = () => {
      const drawBar = (x: number, y: number, width: number, value: number, color: string, flip = false) => {
        ctx.fillStyle = "rgba(3,6,16,.82)"; ctx.fillRect(x, y, width, 24);
        const w = width * value / 100; const grad = ctx.createLinearGradient(x, 0, x + width, 0); grad.addColorStop(0, color); grad.addColorStop(1, "#ecfcff"); ctx.fillStyle = grad;
        ctx.fillRect(flip ? x + width - w : x, y, w, 24);
      };
      ctx.textBaseline = "alphabetic"; ctx.fillStyle = "#effcff"; ctx.font = "900 26px Arial"; ctx.textAlign = "left"; ctx.fillText(p1.fighter.name, 50, 48); ctx.textAlign = "right"; ctx.fillText(p2.fighter.name, W - 50, 48);
      drawBar(50, 62, 455, p1.health, p1.fighter.color); drawBar(W - 505, 62, 455, p2.health, p2.fighter.color, true);
      ctx.fillStyle = "#172039"; ctx.fillRect(50, 94, 360, 9); ctx.fillRect(W - 410, 94, 360, 9); ctx.fillStyle = p1.fighter.color; ctx.fillRect(50, 94, 360 * p1.drive / 100, 9); ctx.fillStyle = p2.fighter.color; ctx.fillRect(W - 50 - 360 * p2.drive / 100, 94, 360 * p2.drive / 100, 9);
      ctx.fillStyle = "#1c2336"; ctx.fillRect(50, 108, 250, 4); ctx.fillRect(W - 300, 108, 250, 4); ctx.fillStyle = p1.guardMeter < 35 ? "#ff405c" : "#f1f5ff"; ctx.fillRect(50, 108, 250 * p1.guardMeter / 100, 4); ctx.fillStyle = p2.guardMeter < 35 ? "#ff405c" : "#f1f5ff"; ctx.fillRect(W - 50 - 250 * p2.guardMeter / 100, 108, 250 * p2.guardMeter / 100, 4);
      ctx.fillStyle = "#f5fbff"; ctx.textAlign = "center"; ctx.font = "900 38px Arial"; ctx.fillText(String(Math.ceil(timer)).padStart(2, "0"), W / 2, 75); ctx.font = "700 12px Arial"; ctx.fillStyle = "#9ba9c5"; ctx.fillText(`ROUND ${round}`, W / 2, 96);
      for (let i = 0; i < 2; i++) { ctx.fillStyle = i < p1.wins ? p1.fighter.color : "#26304a"; ctx.beginPath(); ctx.arc(444 + i * 20, 118, 6, 0, Math.PI * 2); ctx.fill(); ctx.fillStyle = i < p2.wins ? p2.fighter.color : "#26304a"; ctx.beginPath(); ctx.arc(W - 444 - i * 20, 118, 6, 0, Math.PI * 2); ctx.fill(); }
    };

    const draw = () => {
      ctx.save();
      const sx = shake > 0.5 ? (Math.random() - 0.5) * shake : 0, sy = shake > 0.5 ? (Math.random() - 0.5) * shake * 0.5 : 0;
      ctx.translate(sx, sy); ctx.drawImage(background, 0, 0);
      ctx.save(); ctx.globalCompositeOperation = "lighter";
      for (const q of projectiles) {
        const direction = Math.sign(q.vx) || 1, pulse = 1 + Math.sin(performance.now() * .028 + q.x * .02) * .16;
        const trail = ctx.createLinearGradient(q.x - direction * 125, q.y, q.x + direction * 22, q.y);
        trail.addColorStop(0, "transparent"); trail.addColorStop(.54, `${q.color}55`); trail.addColorStop(1, q.color);
        ctx.fillStyle = trail; ctx.beginPath(); ctx.moveTo(q.x - direction * 128, q.y); ctx.lineTo(q.x - direction * 20, q.y - 25 * pulse); ctx.lineTo(q.x + direction * 23, q.y); ctx.lineTo(q.x - direction * 20, q.y + 25 * pulse); ctx.closePath(); ctx.fill();
        ctx.shadowBlur = 28; ctx.shadowColor = q.color; ctx.strokeStyle = q.color; ctx.lineWidth = 5; ctx.beginPath(); ctx.arc(q.x, q.y, 25 * pulse, 0, Math.PI * 2); ctx.stroke();
        ctx.shadowBlur = 16; ctx.fillStyle = "#ffffff"; ctx.beginPath(); ctx.arc(q.x, q.y, 10 * pulse, 0, Math.PI * 2); ctx.fill();
        ctx.strokeStyle = "#eaffff"; ctx.lineWidth = 2; ctx.beginPath(); ctx.moveTo(q.x - direction * 72, q.y + 5); ctx.lineTo(q.x - direction * 45, q.y - 10); ctx.lineTo(q.x - direction * 18, q.y + 7); ctx.lineTo(q.x + direction * 13, q.y - 3); ctx.stroke();
      }
      ctx.restore();
      drawCombatant(p1); drawCombatant(p2);
      ctx.save(); ctx.globalCompositeOperation = "lighter";
      for (const q of particles) { const alpha = clamp(q.life / q.maxLife, 0, 1); ctx.globalAlpha = alpha; ctx.shadowBlur = 12; ctx.shadowColor = q.color; ctx.strokeStyle = q.color; ctx.lineWidth = Math.max(1.5, q.size * .55); ctx.beginPath(); ctx.moveTo(q.x, q.y); ctx.lineTo(q.x - q.vx * .035, q.y - q.vy * .035); ctx.stroke(); }
      ctx.restore(); ctx.globalAlpha = 1; ctx.shadowBlur = 0;
      drawHud();
      if (roundState === "intro") drawCenterText(`ROUND ${round}`, "FIGHT");
      if (roundState === "ko") drawCenterText("K.O.", p1.health > p2.health ? p1.fighter.name : p2.fighter.name);
      if (comboCallout > 0) { ctx.textAlign = "center"; ctx.shadowBlur = 24; ctx.shadowColor = p1.fighter.color; ctx.fillStyle = p1.fighter.color; ctx.font = "italic 900 34px Arial"; ctx.fillText(player.combo.name, W / 2, 175); ctx.shadowBlur = 0; ctx.font = "800 13px Arial"; ctx.fillStyle = "#eefcff"; ctx.fillText("PERFECT CHAIN — FINISHER DEPLOYED", W / 2, 200); }
      if (finisherTime > 0) {
        const pulse = .68 + Math.sin(performance.now() * .035) * .12;
        ctx.globalAlpha = pulse; ctx.fillStyle = "#02030a"; ctx.fillRect(0, 0, W, H); ctx.globalAlpha = 1;
        ctx.fillStyle = finisherColor; ctx.fillRect(0, 132, W, 5); ctx.fillRect(0, 575, W, 5);
        ctx.textAlign = "center"; ctx.fillStyle = "#f7fdff"; ctx.font = "italic 950 62px Arial"; ctx.fillText(finisherName, W / 2, 345);
        ctx.fillStyle = finisherColor; ctx.font = "900 14px Arial"; ctx.fillText(`${player.name} // CINEMATIC FINISH`, W / 2, 380);
      }
      ctx.restore();
    };

    const drawCenterText = (big: string, small: string) => {
      ctx.fillStyle = "rgba(4,5,14,.7)"; ctx.fillRect(0, 270, W, 155);
      ctx.textAlign = "center"; ctx.fillStyle = "#effdff"; ctx.font = "italic 900 86px Arial"; ctx.fillText(big, W / 2, 360);
      ctx.font = "900 15px Arial"; ctx.fillStyle = "#27f4ff"; ctx.fillText(small, W / 2, 397);
    };

    const loop = (now: number) => {
      if (!running) return;
      const elapsed = Math.min(0.05, (now - last) / 1000); last = now; accumulator += elapsed;
      while (accumulator >= fixed) { update(fixed); accumulator -= fixed; }
      draw(); if (mode === "ONLINE" && role === "host") onSnapshot(snapshot()); raf = requestAnimationFrame(loop);
    };

    const keyMap: Record<string, string> = { KeyA: "left", KeyD: "right", KeyW: "jump", KeyS: "crouch", KeyE: "evade", Space: "guard" };
    const attackKeyMap: Record<string, NonNullable<Combatant["attack"]>> = { KeyY: "lightPunch", KeyU: "heavyPunch", KeyI: "lightKick", KeyL: "heavyKick", KeyO: "special", KeyP: "super" };
    const comboKeyMap: Record<string, string> = { KeyY: "Y", KeyU: "U", KeyI: "I", KeyL: "L" };
    const direction = () => inputs.crouch ? inputs.left ? "DFL" : inputs.right ? "DFR" : "D" : inputs.left ? "L" : inputs.right ? "R" : "N";
    const recordDirection = () => {
      const now = performance.now(), key = direction();
      while (directionHistory[0] && now - directionHistory[0].at > 950) directionHistory.shift();
      if (key !== "N" && directionHistory.at(-1)?.key !== key) directionHistory.push({ key, at: now });
    };
    const hasMotion = (sequence: string[]) => {
      const tail = directionHistory.slice(-sequence.length);
      return tail.length === sequence.length && tail.every((item, index) => item.key === sequence[index]) && tail.at(-1)!.at - tail[0].at <= 850;
    };
    const resolveCommand = (base: NonNullable<Combatant["attack"]>) => {
      const qcfRight = hasMotion(["D", "DFR", "R"]), qcfLeft = hasMotion(["D", "DFL", "L"]);
      const superRight = hasMotion(["D", "DFR", "R", "D", "DFR", "R"]), superLeft = hasMotion(["D", "DFL", "L", "D", "DFL", "L"]);
      const dp = hasMotion(["R", "D", "DFR"]) || hasMotion(["L", "D", "DFL"]);
      if ((base === "heavyPunch" || base === "heavyKick") && (superRight || superLeft)) return "super" as const;
      if ((base === "lightPunch" || base === "heavyPunch") && dp) return "impact" as const;
      if (["lightPunch", "heavyPunch", "lightKick", "heavyKick"].includes(base) && (qcfRight || qcfLeft)) return "special" as const;
      if ((base === "heavyPunch" && !!inputs.heavyKick) || (base === "heavyKick" && !!inputs.heavyPunch)) return "impact" as const;
      return base;
    };
    const pulseAction = (initialAction: NonNullable<Combatant["attack"]>, comboKey?: string) => {
      const comboFinished = comboKey ? recordAction(comboKey) : false;
      const action = comboFinished ? "special" : initialAction;
      inputs[action] = true;
      inputs.actionSeq = Number(inputs.actionSeq ?? 0) + 1;
      inputs.action = action;
      sendInput({ ...inputs });
      window.setTimeout(() => { inputs[action] = false; sendInput({ ...inputs }); }, 72);
    };
    const onKey = (event: KeyboardEvent, down: boolean) => {
      if (role === "spectator") return;
      const control = keyMap[event.code];
      const attack = attackKeyMap[event.code];
      if (!control && !attack) return;
      event.preventDefault();
      if (control) {
        inputs[control] = down;
        if (["left", "right", "crouch"].includes(control)) recordDirection();
        sendInput({ ...inputs });
      } else if (down && !event.repeat && attack) pulseAction(resolveCommand(attack), comboKeyMap[event.code]);
    };
    const keyDown = (e: KeyboardEvent) => onKey(e, true), keyUp = (e: KeyboardEvent) => onKey(e, false);
    window.addEventListener("keydown", keyDown, { passive: false }); window.addEventListener("keyup", keyUp, { passive: false });
    const visibility = () => { last = performance.now(); accumulator = 0; };
    document.addEventListener("visibilitychange", visibility);
    raf = requestAnimationFrame(loop);
    return () => { running = false; cancelAnimationFrame(raf); window.removeEventListener("keydown", keyDown); window.removeEventListener("keyup", keyUp); document.removeEventListener("visibilitychange", visibility); };
  }, [cpu, difficulty, mode, onMatchEnd, onSnapshot, outfit, player, remoteInputRef, remoteStateRef, role, sendInput, stage]);

  const setControl = (control: string, value: boolean) => {
    inputRef.current[control] = value;
    if (value && ["lightPunch", "heavyPunch", "lightKick", "heavyKick", "special", "impact", "super"].includes(control)) {
      inputRef.current.actionSeq = Number(inputRef.current.actionSeq ?? 0) + 1;
      inputRef.current.action = control;
    }
    sendInput({ ...inputRef.current });
  }

  return (
    <div className="arena-wrap">
      <canvas ref={canvasRef} aria-label={`${player.name} versus ${cpu.name} fighting arena`} />
      {mode === "ONLINE" && <div className="room-hud">{role === "spectator" ? "● WATCHING LIVE" : role === "host" ? "P1 // HOST" : "P2 // CHALLENGER"}</div>}
      {role !== "spectator" && <div className="touch-controls" aria-label="Touch controls">
        <div className="touch-move"><TouchButton label="◀" control="left" setControl={setControl} /><TouchButton label="▲" control="jump" setControl={setControl} /><TouchButton label="▼" control="crouch" setControl={setControl} /><TouchButton label="▶" control="right" setControl={setControl} /><TouchButton label="EV" control="evade" setControl={setControl} /></div>
        <div className="touch-action"><TouchButton label="LP" control="lightPunch" setControl={setControl} /><TouchButton label="HP" control="heavyPunch" setControl={setControl} /><TouchButton label="LK" control="lightKick" setControl={setControl} /><TouchButton label="HK" control="heavyKick" setControl={setControl} /><TouchButton label="SP" control="special" setControl={setControl} /><TouchButton label="GD" control="guard" setControl={setControl} /></div>
      </div>}
    </div>
  );
}

function TouchButton({ label, control, setControl }: { label: string; control: string; setControl: (control: string, value: boolean) => void }) {
  return <button onPointerDown={(e) => { e.currentTarget.setPointerCapture(e.pointerId); setControl(control, true); }} onPointerUp={() => setControl(control, false)} onPointerCancel={() => setControl(control, false)} onContextMenu={(e) => e.preventDefault()} aria-label={control}>{label}</button>;
}

function drawBackground(ctx: CanvasRenderingContext2D, w: number, h: number, stage: Stage) {
  const sky = ctx.createLinearGradient(0, 0, 0, h); sky.addColorStop(0, "#050712"); sky.addColorStop(0.55, "#111a34"); sky.addColorStop(1, "#070912"); ctx.fillStyle = sky; ctx.fillRect(0, 0, w, h);
  const glow = ctx.createRadialGradient(w / 2, 265, 20, w / 2, 265, 520); glow.addColorStop(0, `${stage.color}35`); glow.addColorStop(1, "transparent"); ctx.fillStyle = glow; ctx.fillRect(0, 0, w, h);

  if (stage.motif === "skyline" || stage.motif === "harbour") {
    for (let x = 0; x < w; x += 52) { const bh = 80 + ((x * 17) % 150); ctx.fillStyle = x % 104 === 0 ? "#172849" : "#101a33"; ctx.fillRect(x, 350 - bh, 42, bh); ctx.fillStyle = stage.accent; ctx.globalAlpha = 0.25; for (let y = 280; y < 345; y += 18) ctx.fillRect(x + 9, y, 7, 4); ctx.globalAlpha = 1; }
    if (stage.motif === "harbour") { ctx.strokeStyle = stage.color; ctx.lineWidth = 5; ctx.beginPath(); ctx.arc(930, 340, 150, Math.PI, 0); ctx.stroke(); }
  } else if (stage.motif === "rail" || stage.motif === "metro") {
    ctx.fillStyle = "#111b31"; ctx.fillRect(0, 205, w, 170); ctx.fillStyle = stage.color; ctx.globalAlpha = 0.32; ctx.fillRect(0, 230, w, 8); ctx.fillRect(0, 335, w, 4); ctx.globalAlpha = 1;
    for (let x = -80; x < w; x += 180) { ctx.fillStyle = "#263453"; ctx.fillRect(x, 252, 130, 58); ctx.fillStyle = stage.accent; ctx.globalAlpha = 0.16; ctx.fillRect(x + 12, 264, 106, 30); ctx.globalAlpha = 1; }
  } else if (stage.motif === "market" || stage.motif === "plaza") {
    for (let x = 0; x < w; x += 145) { ctx.fillStyle = x % 290 === 0 ? stage.color : stage.accent; ctx.globalAlpha = 0.34; ctx.beginPath(); ctx.moveTo(x, 255); ctx.lineTo(x + 65, 305); ctx.lineTo(x + 125, 255); ctx.fill(); ctx.globalAlpha = 1; ctx.fillStyle = "#161d32"; ctx.fillRect(x + 18, 305, 90, 55); }
    ctx.strokeStyle = stage.accent; ctx.globalAlpha = 0.4; ctx.beginPath(); ctx.moveTo(0, 170); ctx.lineTo(w, 240); ctx.stroke(); ctx.globalAlpha = 1;
  } else if (stage.motif === "forge") {
    for (let x = 30; x < w; x += 250) { ctx.fillStyle = "#20273a"; ctx.fillRect(x, 180, 115, 180); ctx.fillStyle = stage.color; ctx.globalAlpha = 0.32; ctx.beginPath(); ctx.arc(x + 58, 265, 38, 0, Math.PI * 2); ctx.fill(); ctx.globalAlpha = 1; }
    ctx.fillStyle = "rgba(255,95,45,.16)"; ctx.fillRect(0, 340, w, 28);
  } else if (stage.motif === "club") {
    for (let x = 0; x < w; x += 96) { ctx.strokeStyle = x % 192 === 0 ? stage.color : stage.accent; ctx.globalAlpha = 0.22; ctx.lineWidth = 5; ctx.strokeRect(x, 150 + (x % 120), 72, 150); } ctx.globalAlpha = 1;
    ctx.fillStyle = stage.color; ctx.globalAlpha = 0.18; ctx.fillRect(230, 205, 820, 95); ctx.globalAlpha = 1;
  } else if (stage.motif === "court") {
    ctx.fillStyle = "rgba(216,255,71,.12)"; ctx.fillRect(90, 205, 1100, 155); ctx.strokeStyle = stage.color; ctx.lineWidth = 4; ctx.strokeRect(270, 228, 740, 120); ctx.beginPath(); ctx.arc(640, 288, 58, 0, Math.PI * 2); ctx.stroke();
    for (let x = 0; x < w; x += 35) { ctx.fillStyle = x % 70 ? "#13253b" : "#1f3c53"; ctx.fillRect(x, 170 + (x % 105), 25, 55); }
  } else if (stage.motif === "steppe") {
    ctx.fillStyle = "#132b38"; ctx.beginPath(); ctx.moveTo(0, 350); for (let x = 0; x <= w; x += 120) ctx.lineTo(x, 250 + (x % 240 ? 45 : 0)); ctx.lineTo(w, 400); ctx.lineTo(0, 400); ctx.fill();
    ctx.strokeStyle = stage.color; ctx.lineWidth = 5; ctx.beginPath(); ctx.moveTo(640, 135); ctx.lineTo(640, 355); ctx.moveTo(555, 235); ctx.lineTo(725, 235); ctx.stroke();
  }

  ctx.globalAlpha = 0.24; ctx.strokeStyle = stage.color; ctx.lineWidth = 1;
  for (let x = -300; x < w + 300; x += 80) { ctx.beginPath(); ctx.moveTo(w / 2, 320); ctx.lineTo(x, h); ctx.stroke(); }
  for (let y = 430; y < h; y += 42) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(w, y); ctx.stroke(); }
  ctx.globalAlpha = 1; ctx.fillStyle = `${stage.accent}22`; ctx.fillRect(0, 579, w, 9);
  ctx.globalAlpha = 0.05; ctx.fillStyle = "#fff"; for (let y = 0; y < h; y += 4) ctx.fillRect(0, y, w, 1); ctx.globalAlpha = 1;
}

type RigPoint = { x: number; y: number };

function drawArticulatedFighter(ctx: CanvasRenderingContext2D, c: Combatant, outfit: Outfit, now: number, attackProgress: number) {
  const f = c.fighter;
  const youth = f.kind === "youth";
  const robot = f.kind === "robot";
  const monster = f.kind === "monster";
  const elder = f.kind === "elder";
  const heavy = monster || ["Grappler", "Armor", "Juggernaut"].includes(f.style);
  const scale = monster ? 1.28 : youth ? 0.92 : elder ? 1.02 : heavy ? 1.16 : 1.09;
  const moving = c.grounded && Math.abs(c.vx) > 28 && !c.attack && !c.guarding && c.hurtTime <= 0;
  const stride = moving ? Math.sin(now * 0.018 + c.x * 0.012) : 0;
  const breath = Math.sin(now * 0.0038 + c.x) * 2.5;
  const strike = Math.sin(Math.PI * attackProgress);
  const recover = Math.sin(Math.PI * clamp(attackProgress * 1.35, 0, 1));
  const isPunch = c.attack === "lightPunch" || c.attack === "heavyPunch" || c.attack === "impact";
  const isKick = c.attack === "lightKick" || c.attack === "heavyKick";
  const isSpecial = c.attack === "special" || c.attack === "super";
  const crouch = c.crouching ? 62 : 0;
  const hurt = c.hurtTime > 0 ? clamp(c.hurtTime * 4, 0, 1) : 0;
  const lean = isPunch ? 16 * strike : isKick ? -8 * strike : isSpecial ? 5 * strike : -24 * hurt;

  ctx.save();
  ctx.scale(scale, scale);
  ctx.translate(0, crouch / scale);
  ctx.filter = `drop-shadow(0 9px 5px rgba(0,0,0,.72)) drop-shadow(0 0 8px ${f.color})`;

  ctx.save();
  ctx.scale(1 / scale, 1 / scale);
  ctx.fillStyle = "rgba(0,0,0,.46)";
  ctx.beginPath(); ctx.ellipse(0, 1, heavy ? 70 : 56, 13, 0, 0, Math.PI * 2); ctx.fill();
  ctx.restore();

  const outline = "#050814";
  const skin: Record<string, string> = { kael: "#d7a079", zara: "#71452f", atlas: "#b77b58", nyx: "#d3a78f", rio: "#a75e3e", sable: "#d3a18b", mara: "#b96d50", batu: "#a86e4e", lux: "#e1b099", oren: "#c89472", miko: "#e7b38d", teo: "#bd7b55", jun: "#d9a481", raku: "#c58c66" };
  const skinColor = skin[f.id] ?? "#c98d68";
  const dark = outfit.cut === "sleek" ? "#070b17" : outfit.cut === "heatwave" ? f.secondary : "#111827";
  const boot = f.id === "atlas" ? "#a66d35" : f.id === "teo" ? "#24d4c3" : "#101522";

  const segment = (a: RigPoint, b: RigPoint, widthA: number, widthB: number, color: string, glow = false) => {
    const dx = b.x - a.x, dy = b.y - a.y, len = Math.max(1, Math.hypot(dx, dy));
    const nx = -dy / len, ny = dx / len;
    ctx.beginPath();
    ctx.moveTo(a.x + nx * widthA, a.y + ny * widthA);
    ctx.quadraticCurveTo((a.x + b.x) / 2 + nx * Math.max(widthA, widthB) * .22, (a.y + b.y) / 2 + ny * Math.max(widthA, widthB) * .22, b.x + nx * widthB, b.y + ny * widthB);
    ctx.arc(b.x, b.y, widthB, Math.atan2(ny, nx), Math.atan2(-ny, -nx));
    ctx.quadraticCurveTo((a.x + b.x) / 2 - nx * Math.max(widthA, widthB) * .22, (a.y + b.y) / 2 - ny * Math.max(widthA, widthB) * .22, a.x - nx * widthA, a.y - ny * widthA);
    ctx.arc(a.x, a.y, widthA, Math.atan2(-ny, -nx), Math.atan2(ny, nx));
    ctx.closePath(); ctx.fillStyle = color; ctx.strokeStyle = outline; ctx.lineWidth = 6; ctx.fill(); ctx.stroke();
    if (glow) { ctx.strokeStyle = f.color; ctx.lineWidth = 2.5; ctx.globalAlpha = .8; ctx.stroke(); ctx.globalAlpha = 1; }
  };
  const joint = (p: RigPoint, radius: number, color: string) => { ctx.fillStyle = color; ctx.strokeStyle = outline; ctx.lineWidth = 5; ctx.beginPath(); ctx.arc(p.x, p.y, radius, 0, Math.PI * 2); ctx.fill(); ctx.stroke(); };
  const bridge = (p: RigPoint, radius: number, color: string) => { ctx.fillStyle = color; ctx.beginPath(); ctx.arc(p.x, p.y, radius, 0, Math.PI * 2); ctx.fill(); };

  const hipY = -112 + breath - lean * .12;
  const shoulderY = -218 + breath + lean * .16;
  const hipFront = { x: 25 + lean * .25, y: hipY };
  const hipBack = { x: -24 + lean * .25, y: hipY + 3 };
  let frontKnee = { x: 38 + stride * 27, y: -61 };
  let frontFoot = { x: 42 + stride * 48, y: -8 };
  let backKnee = { x: -42 - stride * 25, y: -66 };
  let backFoot = { x: -45 - stride * 42, y: -7 };
  if (!c.grounded) { frontKnee = { x: 42, y: -75 }; frontFoot = { x: 68, y: -43 }; backKnee = { x: -30, y: -65 }; backFoot = { x: -54, y: -28 }; }
  if (isKick) {
    const high = c.attack === "heavyKick" ? -116 : -82;
    frontKnee = { x: lerp(42, 94, strike), y: lerp(-63, high - 18, strike) };
    frontFoot = { x: lerp(45, 174, strike), y: lerp(-7, high, strike) };
    backKnee = { x: -46, y: -58 }; backFoot = { x: -65, y: -5 };
  }
  if (c.guarding) { frontKnee.x = 48; frontFoot.x = 66; backKnee.x = -48; backFoot.x = -61; }
  if (hurt) { frontFoot.x += 30 * hurt; backFoot.x -= 25 * hurt; }

  const legWidth = monster ? 29 : heavy ? 23 : youth ? 16 : 19;
  segment(hipBack, backKnee, legWidth + 3, legWidth, dark, robot); segment(backKnee, backFoot, legWidth, legWidth - 3, dark, robot); if (!robot) bridge(backKnee, legWidth - 2, dark);
  segment(backFoot, { x: backFoot.x + 24, y: backFoot.y + 1 }, legWidth - 4, legWidth - 6, boot, robot);
  segment(hipFront, frontKnee, legWidth + 3, legWidth, dark, robot); segment(frontKnee, frontFoot, legWidth, legWidth - 3, dark, robot); if (!robot) bridge(frontKnee, legWidth - 2, dark);
  segment(frontFoot, { x: frontFoot.x + 28, y: frontFoot.y + 1 }, legWidth - 3, legWidth - 6, boot, robot);
  if (robot) { joint(frontKnee, 11, f.secondary); joint(backKnee, 11, f.secondary); }

  const shoulderFront = { x: 35 + lean, y: shoulderY };
  const shoulderBack = { x: -34 + lean, y: shoulderY + 4 };
  let frontElbow = { x: 58 + lean, y: -166 + breath };
  let frontHand = { x: 38 + lean, y: -119 + breath };
  let backElbow = { x: -58 + lean, y: -165 + breath };
  let backHand = { x: -35 + lean, y: -126 + breath };
  if (moving) { frontElbow.x -= stride * 31; frontHand.x -= stride * 48; backElbow.x += stride * 31; backHand.x += stride * 48; }
  if (isPunch) {
    const heavyPunch = c.attack === "heavyPunch" || c.attack === "impact";
    frontElbow = { x: lerp(58, heavyPunch ? 102 : 112, strike), y: lerp(-166, -205, strike) };
    frontHand = { x: lerp(38, heavyPunch ? 184 : 164, strike), y: lerp(-119, -211, strike) };
    backElbow = { x: -54, y: -188 }; backHand = { x: -6, y: -211 };
  } else if (isSpecial) {
    frontElbow = { x: 74 * recover, y: lerp(-166, -239, strike) }; frontHand = { x: 112 * recover, y: lerp(-119, -276, strike) };
    backElbow = { x: -65 * recover, y: lerp(-165, -230, strike) }; backHand = { x: -102 * recover, y: lerp(-126, -263, strike) };
  } else if (c.guarding) {
    frontElbow = { x: 61, y: -193 }; frontHand = { x: 20, y: -236 };
    backElbow = { x: -43, y: -191 }; backHand = { x: 13, y: -176 };
  } else if (hurt) {
    frontElbow = { x: 64, y: -184 }; frontHand = { x: 96, y: -228 };
    backElbow = { x: -66, y: -175 }; backHand = { x: -98, y: -207 };
  }

  const armWidth = monster ? 27 : heavy ? 21 : youth ? 13 : 17;
  segment(shoulderBack, backElbow, armWidth + 2, armWidth, f.secondary, robot); segment(backElbow, backHand, armWidth, armWidth - 3, f.secondary, robot); if (!robot) bridge(backElbow, armWidth - 2, f.secondary); joint(backHand, armWidth - 2, robot ? f.secondary : skinColor);

  const torsoTop = heavy ? 57 : youth ? 40 : 48, torsoBottom = heavy ? 43 : youth ? 30 : 36;
  const chestGradient = ctx.createLinearGradient(-50, shoulderY, 55, hipY); chestGradient.addColorStop(0, f.color); chestGradient.addColorStop(1, f.secondary);
  ctx.beginPath(); ctx.moveTo(shoulderBack.x - 7, shoulderY - 6); ctx.quadraticCurveTo(lean, shoulderY - 25, shoulderFront.x + 7, shoulderY - 6); ctx.quadraticCurveTo(lean + torsoTop + 10, -168, hipFront.x + torsoBottom, hipY); ctx.quadraticCurveTo(lean, hipY + 18, hipBack.x - torsoBottom, hipY); ctx.quadraticCurveTo(lean - torsoTop - 10, -168, shoulderBack.x - 7, shoulderY - 6); ctx.closePath();
  ctx.fillStyle = chestGradient; ctx.strokeStyle = outline; ctx.lineWidth = 7; ctx.fill(); ctx.stroke();
  ctx.globalAlpha = .45; ctx.strokeStyle = "#effcff"; ctx.lineWidth = 2; ctx.beginPath(); ctx.moveTo(lean, shoulderY - 14); ctx.lineTo(lean, hipY + 7); ctx.moveTo(-torsoTop * .55 + lean, -171); ctx.lineTo(torsoTop * .55 + lean, -171); ctx.stroke(); ctx.globalAlpha = 1;

  if (["zara", "nyx", "sable", "batu", "lux", "oren", "raku"].includes(f.id)) {
    ctx.fillStyle = dark; ctx.strokeStyle = outline; ctx.lineWidth = 5; ctx.beginPath(); ctx.moveTo(-39 + lean, hipY - 10); ctx.lineTo(-58 + lean - stride * 8, -37); ctx.lineTo(-3 + lean, -85); ctx.lineTo(16 + lean, -34); ctx.lineTo(43 + lean, hipY - 10); ctx.closePath(); ctx.fill(); ctx.stroke();
  }
  if (["atlas", "batu"].includes(f.id)) { ctx.strokeStyle = "#e8c17a"; ctx.lineWidth = 7; for (let y = shoulderY + 30; y < hipY - 12; y += 25) { ctx.beginPath(); ctx.moveTo(-34 + lean, y); ctx.lineTo(35 + lean, y); ctx.stroke(); } }
  if (f.id === "kael") { joint({ x: 30 + lean, y: -185 }, 12, "#ffdd72"); }
  if (f.id === "sable") { ctx.strokeStyle = "#d62646"; ctx.lineWidth = 10; ctx.beginPath(); ctx.moveTo(-20 + lean, shoulderY - 13); ctx.quadraticCurveTo(-85, -187, -112 - stride * 20, -135); ctx.stroke(); }
  if (f.id === "raku") { ctx.fillStyle = "#b87943"; ctx.strokeStyle = outline; ctx.lineWidth = 4; ctx.beginPath(); ctx.ellipse(55 + lean, -132, 15, 22, -.2, 0, Math.PI * 2); ctx.fill(); ctx.stroke(); }

  const frontArmColor = f.id === "kael" ? "#f26b25" : f.color;
  segment(shoulderFront, frontElbow, armWidth + 2, armWidth, frontArmColor, robot); segment(frontElbow, frontHand, armWidth, armWidth - 3, frontArmColor, robot); if (!robot) bridge(frontElbow, armWidth - 2, frontArmColor); joint(frontHand, armWidth - 2, robot ? f.secondary : skinColor);
  if (robot) { joint(frontElbow, 10, f.secondary); joint(backElbow, 10, f.secondary); }

  const neck = { x: lean * .55, y: shoulderY - 19 };
  segment(neck, { x: neck.x, y: neck.y - 23 }, youth ? 11 : 14, youth ? 10 : 12, skinColor);
  const headX = lean * .62 + (hurt ? -11 : 0), headY = shoulderY - 58 + breath * .25;
  ctx.fillStyle = robot ? "#2455c6" : monster ? "#4c5149" : skinColor; ctx.strokeStyle = outline; ctx.lineWidth = 6; ctx.beginPath(); ctx.ellipse(headX, headY, youth ? 25 : heavy ? 32 : 28, youth ? 30 : 34, hurt * .18, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
  if (robot) { ctx.strokeStyle = f.secondary; ctx.lineWidth = 5; ctx.beginPath(); ctx.arc(headX, headY, 18, 0, Math.PI * 2); ctx.stroke(); }
  else if (monster) { ctx.fillStyle = "#ffd06a"; ctx.beginPath(); ctx.arc(headX - 10, headY, 4, 0, Math.PI * 2); ctx.arc(headX + 10, headY, 4, 0, Math.PI * 2); ctx.fill(); }
  else {
    ctx.fillStyle = f.id === "raku" ? "#eee7db" : f.id === "lux" ? "#e9d6c4" : "#151522"; ctx.beginPath(); ctx.arc(headX, headY - 6, youth ? 27 : 31, Math.PI, Math.PI * 2); ctx.lineTo(headX + 22, headY - 8); ctx.lineTo(headX + 12, headY - 18); ctx.lineTo(headX, headY - 7); ctx.lineTo(headX - 14, headY - 18); ctx.lineTo(headX - 25, headY - 4); ctx.closePath(); ctx.fill();
    if (["nyx", "sable", "lux"].includes(f.id)) { ctx.fillStyle = f.id === "lux" ? "#b65cff" : "#101523"; ctx.fillRect(headX - 23, headY - 5, 46, 13); ctx.strokeStyle = f.color; ctx.lineWidth = 2; ctx.strokeRect(headX - 20, headY - 3, 40, 9); }
    else { ctx.fillStyle = "#111521"; ctx.fillRect(headX - 17, headY - 2, 9, 3); ctx.fillRect(headX + 8, headY - 2, 9, 3); }
    if (elder) { ctx.fillStyle = "#eee7db"; ctx.beginPath(); ctx.moveTo(headX - 19, headY + 9); ctx.quadraticCurveTo(headX, headY + 47, headX + 20, headY + 9); ctx.quadraticCurveTo(headX + 12, headY + 39, headX, headY + 31); ctx.quadraticCurveTo(headX - 12, headY + 39, headX - 19, headY + 9); ctx.fill(); }
  }

  if (isSpecial) { ctx.globalAlpha = .22 + strike * .34; ctx.strokeStyle = f.color; for (let ring = 0; ring < 3; ring++) { ctx.lineWidth = 5 - ring; ctx.beginPath(); ctx.arc(lean, -160, 72 + ring * 22 + strike * 15, -1.25, 1.25); ctx.stroke(); } ctx.globalAlpha = 1; }
  ctx.restore();
}

type SpritePose = "idle" | "punch" | "kick" | "special" | "guard" | "hurt";

function buildSprite(fighter: Fighter, outfit: Outfit, pose: SpritePose = "idle") {
  const c = document.createElement("canvas"); c.width = 224; c.height = 300; const x = c.getContext("2d")!;
  x.lineJoin = "round"; x.lineCap = "round";
  x.fillStyle = "rgba(0,0,0,.34)"; x.beginPath(); x.ellipse(112, 287, fighter.kind === "monster" ? 92 : 70, 10, 0, 0, Math.PI * 2); x.fill();

  const outline = "#050814";
  const shade = outfit.cut === "sleek" ? "#090d19" : outfit.cut === "heatwave" ? "#25203a" : "#11182a";
  const line = (points: Array<[number, number]>, width: number, color: string, inner = width - 8) => {
    x.strokeStyle = outline; x.lineWidth = width; x.beginPath(); points.forEach(([px, py], index) => index ? x.lineTo(px, py) : x.moveTo(px, py)); x.stroke();
    x.strokeStyle = color; x.lineWidth = Math.max(3, inner); x.beginPath(); points.forEach(([px, py], index) => index ? x.lineTo(px, py) : x.moveTo(px, py)); x.stroke();
  };
  const plate = (px: number, py: number, rx: number, ry: number, color: string | CanvasGradient) => { x.fillStyle = color; x.strokeStyle = outline; x.lineWidth = 6; x.beginPath(); x.ellipse(px, py, rx, ry, 0, 0, Math.PI * 2); x.fill(); x.stroke(); };

  if (fighter.kind === "monster") {
    const rocks: Array<[number, number, number, number]> = [[112,70,38,32],[82,112,38,34],[137,113,43,38],[65,163,37,48],[157,163,42,49],[88,211,38,52],[139,212,40,52],[62,260,35,25],[160,260,38,25]];
    for (const [px,py,rx,ry] of rocks) { const g = x.createRadialGradient(px-10,py-12,4,px,py,rx); g.addColorStop(0,"#6b685d"); g.addColorStop(1,"#252925"); plate(px,py,rx,ry,g); }
    x.strokeStyle = fighter.color; x.lineWidth = 5; x.globalAlpha = .85; for (const [px,py] of [[94,92],[128,139],[76,189],[145,226]]) { x.beginPath(); x.moveTo(px-12,py-9); x.lineTo(px,py+5); x.lineTo(px+13,py-8); x.stroke(); } x.globalAlpha = 1;
    x.fillStyle = "#91bd62"; x.beginPath(); x.ellipse(112,42,48,10,0,0,Math.PI*2); x.fill(); x.fillStyle = "#ffd06a"; x.beginPath(); x.arc(97,67,5,0,Math.PI*2); x.arc(127,67,5,0,Math.PI*2); x.fill();
    return c;
  }

  if (fighter.kind === "robot") {
    line([[78,145],[65,203],[52,270]],31,"#2455c6"); line([[144,145],[157,203],[172,270]],31,"#2455c6");
    line(pose === "punch" ? [[75,105],[40,112],[8,104]] : [[75,105],[47,158],[39,205]],28,"#357cff");
    line(pose === "special" ? [[148,104],[177,81],[205,49]] : [[148,104],[182,157],[190,204]],28,"#357cff");
    x.fillStyle = "#19449f"; x.strokeStyle = outline; x.lineWidth = 7; x.beginPath(); x.moveTo(69,91); x.lineTo(93,76); x.lineTo(136,76); x.lineTo(158,95); x.lineTo(150,166); x.lineTo(75,166); x.closePath(); x.fill(); x.stroke();
    for (const [px,py] of [[68,204],[157,204],[48,157],[178,157]]) plate(px,py,13,13,"#78b8ff");
    plate(112,58,35,35,"#224fb4"); x.strokeStyle = "#85e9ff"; x.lineWidth = 6; x.beginPath(); x.arc(112,58,20,0,Math.PI*2); x.stroke(); x.fillStyle = "#e9ffff"; x.beginPath(); x.arc(112,58,6,0,Math.PI*2); x.fill();
    x.fillStyle = "#8beeff"; x.beginPath(); x.arc(112,119,12,0,Math.PI*2); x.fill();
    return c;
  }

  const youth = fighter.kind === "youth";
  const elder = fighter.kind === "elder";
  const scale = youth ? .82 : elder ? .92 : 1;
  const yShift = youth ? 48 : elder ? 22 : 0;
  const bodyScale = ["Grappler", "Armor", "Juggernaut"].includes(fighter.style) ? 1.13 : ["Aerial", "Trickster", "Skirmisher"].includes(fighter.style) ? .93 : 1;
  x.save(); x.translate(112, 292); x.scale(scale, scale); x.translate(-112, -292 + yShift); x.translate(112, 0); x.scale(bodyScale, 1); x.translate(-112, 0);
  const skinColors: Record<string,string> = { kael:"#d7a079",zara:"#71452f",atlas:"#b77b58",nyx:"#d3a78f",rio:"#a75e3e",sable:"#d3a18b",mara:"#b96d50",batu:"#a86e4e",lux:"#e1b099",oren:"#c89472",miko:"#e7b38d",teo:"#bd7b55",jun:"#d9a481",raku:"#c58c66" };
  const skin = skinColors[fighter.id] ?? "#c98d68";
  const pants = outfit.cut === "heatwave" ? fighter.secondary : shade;
  const coatIds = new Set(["zara","nyx","sable","batu","lux","oren","raku"]);
  const armored = new Set(["atlas","batu"]);

  if (coatIds.has(fighter.id)) {
    x.fillStyle = fighter.id === "raku" ? "#296a68" : shade; x.strokeStyle = outline; x.lineWidth = 7; x.beginPath(); x.moveTo(70,118); x.lineTo(151,118); x.lineTo(181,272); x.lineTo(120,246); x.lineTo(103,272); x.lineTo(45,267); x.closePath(); x.fill(); x.stroke();
    x.strokeStyle = fighter.color; x.lineWidth = 3; x.beginPath(); x.moveTo(78,124); x.lineTo(102,242); x.moveTo(143,124); x.lineTo(120,242); x.stroke();
  }

  const leftLeg: Array<[number,number]> = pose === "kick" ? [[92,174],[73,213],[24,196]] : [[92,170],[78,224],[67,278]];
  const rightLeg: Array<[number,number]> = pose === "kick" ? [[132,173],[146,226],[173,274]] : [[132,170],[146,226],[160,278]];
  line(leftLeg, youth ? 27 : 34, pants); line(rightLeg, youth ? 27 : 34, pants);
  line([[leftLeg.at(-1)![0]-5,leftLeg.at(-1)![1]],[leftLeg.at(-1)![0]+15,leftLeg.at(-1)![1]]], youth ? 18 : 24, fighter.id === "atlas" ? "#a66d35" : "#141a28", youth ? 13 : 17);
  line([[rightLeg.at(-1)![0]-7,rightLeg.at(-1)![1]],[rightLeg.at(-1)![0]+15,rightLeg.at(-1)![1]]], youth ? 18 : 24, fighter.id === "atlas" ? "#a66d35" : "#141a28", youth ? 13 : 17);

  const torso = x.createLinearGradient(70,85,155,180); torso.addColorStop(0,fighter.color); torso.addColorStop(1,fighter.secondary);
  x.fillStyle = torso; x.strokeStyle = outline; x.lineWidth = 8; x.beginPath(); x.moveTo(73,94); x.quadraticCurveTo(111,73,151,95); x.lineTo(158,171); x.quadraticCurveTo(112,192,65,170); x.closePath(); x.fill(); x.stroke();
  if (armored.has(fighter.id)) { x.fillStyle = "rgba(9,12,20,.5)"; for (let row=0;row<3;row++) for (let col=0;col<3;col++) x.fillRect(78+col*23,105+row*19,18,13); }
  else { x.strokeStyle = "rgba(245,255,255,.5)"; x.lineWidth = 3; x.beginPath(); x.moveTo(112,91); x.lineTo(112,171); x.moveTo(80,131); x.lineTo(145,131); x.stroke(); }

  const leftArm: Array<[number,number]> = pose === "guard" ? [[76,105],[92,132],[103,89]] : pose === "hurt" ? [[76,105],[46,126],[31,102]] : [[76,105],[49,157],[38,205]];
  const rightArm: Array<[number,number]> = pose === "punch" ? [[148,105],[181,109],[216,102]] : pose === "special" ? [[148,105],[177,76],[195,34]] : pose === "guard" ? [[148,105],[132,132],[120,91]] : [[148,105],[181,158],[188,204]];
  line(leftArm, youth ? 24 : 31, fighter.id === "atlas" ? "#b38146" : fighter.color);
  line(rightArm, youth ? 24 : 31, fighter.id === "kael" ? "#f26b25" : fighter.secondary);
  plate(leftArm.at(-1)![0],leftArm.at(-1)![1],youth?10:13,youth?10:13,skin); plate(rightArm.at(-1)![0],rightArm.at(-1)![1],youth?10:13,youth?10:13,skin);

  if (fighter.id === "miko") { x.fillStyle = "#ffd43b"; x.strokeStyle = outline; x.lineWidth = 5; x.beginPath(); x.arc(183,65,18,0,Math.PI*2); x.fill(); x.stroke(); x.fillStyle="#7ef1ff"; x.beginPath(); x.arc(183,65,6,0,Math.PI*2); x.fill(); }
  if (fighter.id === "mara") { x.fillStyle = "#f3f1e8"; x.strokeStyle = fighter.color; x.lineWidth=4; x.beginPath(); x.moveTo(82,100); x.lineTo(112,122); x.lineTo(143,100); x.lineTo(135,146); x.lineTo(88,146); x.closePath(); x.fill(); x.stroke(); }
  if (fighter.id === "lux") { x.fillStyle="rgba(255,255,255,.35)"; for(let i=0;i<4;i++){x.beginPath();x.moveTo(72+i*20,112);x.lineTo(88+i*20,134);x.lineTo(70+i*20,154);x.closePath();x.fill();} }
  if (fighter.id === "oren") { x.fillStyle="#e9f7f4"; x.fillRect(103,96,15,78); x.strokeStyle=fighter.color; x.lineWidth=4; x.beginPath(); x.moveTo(65,151); x.lineTo(157,132); x.stroke(); }
  if (fighter.id === "kael") { const reactor=x.createRadialGradient(151,116,2,151,116,18); reactor.addColorStop(0,"#fff"); reactor.addColorStop(.35,"#ffd06b"); reactor.addColorStop(1,"#ff4b1f"); plate(151,116,18,18,reactor); }
  if (fighter.id === "atlas") { plate(70,101,27,17,"#b67938"); plate(154,101,27,17,"#b67938"); x.strokeStyle="#f4d6a0"; x.lineWidth=5; x.beginPath(); x.moveTo(76,116); x.lineTo(112,161); x.lineTo(149,116); x.stroke(); }
  if (fighter.id === "rio") { x.strokeStyle="#d8ff47"; x.lineWidth=9; x.beginPath(); x.moveTo(71,126); x.quadraticCurveTo(28,142,18,184); x.stroke(); }
  if (fighter.id === "sable") { x.strokeStyle="#d62646"; x.lineWidth=10; x.beginPath(); x.moveTo(91,78); x.quadraticCurveTo(55,93,34,142); x.stroke(); x.strokeStyle="#eef4ff"; x.lineWidth=5; x.beginPath(); x.moveTo(169,154); x.lineTo(211,75); x.stroke(); }
  if (fighter.id === "batu") { x.fillStyle="#354b68"; x.strokeStyle=outline; x.lineWidth=5; for(let i=0;i<4;i++){x.fillRect(73+i*20,103,15,58);x.strokeRect(73+i*20,103,15,58);} }
  if (fighter.id === "teo") { x.fillStyle="#24d4c3"; x.strokeStyle=outline; x.lineWidth=5; x.beginPath(); x.roundRect(54,270,116,15,8); x.fill(); x.stroke(); x.fillStyle="#e9ffff"; x.beginPath(); x.arc(72,287,7,0,Math.PI*2); x.arc(153,287,7,0,Math.PI*2); x.fill(); }
  if (fighter.id === "jun") { x.fillStyle="rgba(241,245,255,.82)"; for(const [px,py] of [[48,78],[180,96],[34,154]]){x.beginPath();x.moveTo(px,py);x.lineTo(px+13,py+7);x.lineTo(px+4,py+12);x.lineTo(px-6,py+7);x.closePath();x.fill();} }
  if (fighter.id === "raku") { x.fillStyle="#b87943"; x.strokeStyle=outline; x.lineWidth=5; x.beginPath(); x.ellipse(170,154,18,26,0,0,Math.PI*2); x.fill(); x.stroke(); }

  plate(112,59, youth ? 27 : 33, youth ? 31 : 37, skin);
  x.fillStyle = fighter.id === "lux" ? "#e9d6c4" : fighter.id === "raku" ? "#f2f0e7" : "#151522";
  x.beginPath();
  if (fighter.id === "zara") { x.arc(112,52,39,Math.PI,Math.PI*2); for(let i=0;i<5;i++) x.rect(78+i*15,48,7,48); }
  else if (fighter.id === "raku") { x.arc(112,47,37,Math.PI,Math.PI*2); x.moveTo(80,50); x.quadraticCurveTo(57,74,70,101); x.moveTo(144,50); x.quadraticCurveTo(169,79,151,105); }
  else { x.moveTo(79,57); x.quadraticCurveTo(82,12,113,24); x.quadraticCurveTo(151,14,147,64); x.lineTo(135,48); x.lineTo(124,60); x.lineTo(111,43); x.lineTo(98,59); x.closePath(); }
  x.fill();
  if (fighter.id === "nyx" || fighter.id === "sable") { x.fillStyle="#101523"; x.fillRect(82,55,60,22); x.strokeStyle=fighter.color; x.lineWidth=3; x.strokeRect(87,59,50,12); }
  else if (fighter.id === "lux") { x.fillStyle="#b65cff"; x.beginPath(); x.moveTo(84,54); x.lineTo(104,46); x.lineTo(112,58); x.lineTo(121,46); x.lineTo(141,54); x.lineTo(131,70); x.lineTo(94,70); x.closePath(); x.fill(); }
  else { x.fillStyle="#10121c"; x.fillRect(92,58,12,4); x.fillRect(121,58,12,4); }
  if (elder) { x.fillStyle="#eee7db"; x.beginPath(); x.moveTo(89,73); x.quadraticCurveTo(112,111,137,73); x.quadraticCurveTo(130,125,112,118); x.quadraticCurveTo(91,123,89,73); x.fill(); }
  if (pose === "special") { x.globalAlpha=.58; x.strokeStyle=fighter.color; x.lineWidth=4; for(let ring=0;ring<3;ring++){x.beginPath();x.arc(112,138,64+ring*17,-1.2,1.2);x.stroke();} x.globalAlpha=1; }
  x.fillStyle = fighter.color; x.globalAlpha=.8; x.fillRect(98,116,28,7); x.globalAlpha=1;
  x.restore();
  return c;
}
