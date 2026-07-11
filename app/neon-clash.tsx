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
  attack: "lightPunch" | "heavyPunch" | "lightKick" | "heavyKick" | "special" | "impact" | null;
  attackTime: number;
  attackHit: boolean;
  hurtTime: number;
  stunTime: number;
  flashTime: number;
  combo: number;
  comboWindow: number;
  wins: number;
};

type Projectile = { x: number; y: number; vx: number; life: number; owner: Combatant; color: string; damage: number };
type Particle = { x: number; y: number; vx: number; vy: number; life: number; maxLife: number; color: string; size: number };
type CombatantSnapshot = Omit<Combatant, "fighter">;
type MatchSnapshot = { p1: CombatantSnapshot; p2: CombatantSnapshot; timer: number; round: number; roundState: "intro" | "fight" | "ko" | "done"; projectiles: Array<Omit<Projectile, "owner"> & { owner: 1 | 2 }> };
type RoomRole = "host" | "guest" | "spectator";
type RoomSession = { id: string; token: string; role: RoomRole; players: number; spectators: number; status: string };
type RoomConfig = { playerId: string; cpuId: string; stageId: string; difficulty: "ROOKIE" | "PRO" | "ACE"; outfitId: string };
type OpenRoom = { id: string; players: number; status: string; config: RoomConfig };

const FIGHTERS: Fighter[] = [
  { id: "kael", name: "KAEL", alias: "SUN BREAKER", city: "SEOUL", style: "Rushdown", special: "Solar Rift", quote: "Speed is a decision.", color: "#27f4ff", secondary: "#1768ff", speed: 9, power: 6, reach: 6, mark: "K", combo: { name: "SOLAR CHAIN", sequence: ["T", "T", "U", "L"] } },
  { id: "zara", name: "ZARA", alias: "VOLT QUEEN", city: "LAGOS", style: "Pressure", special: "Thunder Step", quote: "Hear the storm arrive.", color: "#ff2dba", secondary: "#8a3bff", speed: 8, power: 7, reach: 5, mark: "Z", combo: { name: "VOLTAGE RUSH", sequence: ["T", "U", "Y", "L"] } },
  { id: "atlas", name: "ATLAS", alias: "IRON SAINT", city: "ATHENS", style: "Grappler", special: "Titan Break", quote: "The ground remembers.", color: "#ff7648", secondary: "#ffb627", speed: 4, power: 10, reach: 6, mark: "A", combo: { name: "TITAN LOCK", sequence: ["Y", "K", "Y", "L"] } },
  { id: "nyx", name: "NYX", alias: "VOID SIGNAL", city: "BERLIN", style: "Zoner", special: "Black Pulse", quote: "Distance is control.", color: "#9b6cff", secondary: "#ff2dba", speed: 6, power: 7, reach: 10, mark: "N", combo: { name: "VOID CASCADE", sequence: ["U", "T", "Y", "L"] } },
  { id: "rio", name: "RIO", alias: "SKYLINE KID", city: "SÃO PAULO", style: "Aerial", special: "Comet Kick", quote: "Gravity is optional.", color: "#d8ff47", secondary: "#24df9b", speed: 10, power: 5, reach: 6, mark: "R", combo: { name: "COMET LADDER", sequence: ["U", "U", "K", "L"] } },
  { id: "sable", name: "SABLE", alias: "NIGHT BLADE", city: "TOKYO", style: "Counter", special: "Zero Cut", quote: "Your move. My opening.", color: "#efefff", secondary: "#6676ff", speed: 8, power: 8, reach: 7, mark: "S", combo: { name: "ZERO VERDICT", sequence: ["T", "K", "Y", "L"] } },
  { id: "mara", name: "MARA", alias: "RED ORBIT", city: "MEXICO CITY", style: "Balanced", special: "Meteor Arc", quote: "Burn bright. Hit hard.", color: "#ff405c", secondary: "#ff8b32", speed: 7, power: 8, reach: 7, mark: "M", combo: { name: "ORBIT BREAK", sequence: ["T", "Y", "U", "L"] } },
  { id: "batu", name: "BATU", alias: "STEPPE WALL", city: "ULAANBAATAR", style: "Armor", special: "Stone Wake", quote: "I do not move.", color: "#41d7bf", secondary: "#2587a6", speed: 5, power: 9, reach: 5, mark: "B", combo: { name: "STEPPE QUAKE", sequence: ["K", "Y", "K", "L"] } },
  { id: "lux", name: "LUX", alias: "PRISM FOX", city: "PARIS", style: "Trickster", special: "Mirror Dash", quote: "Catch the afterimage.", color: "#ffdc4a", secondary: "#ff4da9", speed: 9, power: 6, reach: 8, mark: "L", combo: { name: "PRISM FEINT", sequence: ["T", "U", "K", "L"] } },
  { id: "oren", name: "OREN", alias: "TIDE MONK", city: "SYDNEY", style: "Control", special: "Breaker Wave", quote: "Breathe between impacts.", color: "#48a8ff", secondary: "#42f5c5", speed: 6, power: 7, reach: 9, mark: "O", combo: { name: "TIDAL FORM", sequence: ["U", "Y", "K", "L"] } },
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

const CONTROL_LABELS = [["A / D", "MOVE"], ["W / S", "JUMP / SQUAT"], ["T", "LIGHT PUNCH"], ["Y", "HEAVY PUNCH"], ["U", "LIGHT KICK"], ["K", "HEAVY KICK"], ["L", "SKILL"], ["SPACE", "GUARD"]];

const clamp = (n: number, min: number, max: number) => Math.max(min, Math.min(max, n));
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

function createCombatant(fighter: Fighter, x: number, facing: 1 | -1): Combatant {
  return { fighter, x, y: 566, vx: 0, vy: 0, facing, health: 100, drive: 65, grounded: true, crouching: false, guarding: false, attack: null, attackTime: 0, attackHit: false, hurtTime: 0, stunTime: 0, flashTime: 0, combo: 0, comboWindow: 0, wins: 0 };
}

function portraitStyle(fighter: Fighter) {
  return { "--fighter": fighter.color, "--fighter-2": fighter.secondary } as React.CSSProperties;
}

function FighterCard({ fighter, selected, rival, onClick }: { fighter: Fighter; selected: boolean; rival: boolean; onClick: () => void }) {
  return (
    <button className={`fighter-card ${selected ? "is-selected" : ""} ${rival ? "is-rival" : ""}`} onClick={onClick} style={portraitStyle(fighter)} aria-pressed={selected}>
      <span className="fighter-number">{String(FIGHTERS.indexOf(fighter) + 1).padStart(2, "0")}</span>
      <span className="portrait-mark" aria-hidden="true">{fighter.mark}</span>
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
  const remoteInputRef = useRef<Record<string, boolean>>({});
  const remoteStateRef = useRef<MatchSnapshot | null>(null);
  const roomRef = useRef<RoomSession | null>(null);
  const lastStatePush = useRef(0);

  const applyRoomConfig = useCallback((config: RoomConfig) => {
    setPlayerId(config.playerId); setCpuId(config.cpuId); setStageId(config.stageId); setDifficulty(config.difficulty); setOutfitId(config.outfitId);
  }, []);

  useEffect(() => { roomRef.current = room; }, [room]);
  useEffect(() => {
    const code = new URLSearchParams(window.location.search).get("room");
    if (!code) return;
    const timer = window.setTimeout(() => { setMode("ONLINE"); setRoomCode(code.toUpperCase()); setLobbyOpen(true); }, 0);
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
    const poll = async () => {
      try {
        const response = await fetch(`/api/rooms/${room.id}?token=${encodeURIComponent(room.token)}`, { cache: "no-store" });
        const data = await response.json();
        if (!active || !response.ok) return;
        remoteInputRef.current = data.room.guestInput ?? {};
        if (room.role !== "host" && data.room.state && Object.keys(data.room.state).length) remoteStateRef.current = data.room.state;
        setRoom((current) => current ? { ...current, players: data.room.players, spectators: data.room.spectators, status: data.room.status } : current);
        if (room.role !== "host" && data.room.status === "fighting") { setLobbyOpen(false); setScreen("fight"); }
      } catch { if (active) setRoomError("ROOM CONNECTION INTERRUPTED — RETRYING"); }
    };
    void poll(); const interval = window.setInterval(poll, room.role === "spectator" ? 140 : 90);
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

  const joinRoom = async (watchOnly = false, selectedCode?: string) => {
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

  const sendInput = useCallback((value: Record<string, boolean>) => {
    const session = roomRef.current; if (!session || session.role !== "guest") return;
    void fetch(`/api/rooms/${session.id}/input`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token: session.token, input: value }) });
  }, []);

  const publishSnapshot = useCallback((snapshot: MatchSnapshot) => {
    const session = roomRef.current; const now = performance.now();
    if (!session || session.role !== "host" || now - lastStatePush.current < 70) return;
    lastStatePush.current = now;
    void fetch(`/api/rooms/${session.id}/state`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token: session.token, state: snapshot, status: snapshot.roundState === "done" ? "complete" : "fighting" }) });
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
            <div><p className="eyebrow">FIGHTER SELECT / 10 CONTENDERS</p><h1>CHOOSE YOUR<br /><em>FREQUENCY</em></h1></div>
            <p className="intro">Ten fighting styles. One broadcast arena. Select your contender, read the matchup, then take the world circuit live.</p>
          </div>

          <div className="versus-preview">
            <FighterPanel fighter={player} side="player" />
            <div className="vs-spine"><span>ROUND</span><strong>VS</strong><small>01</small></div>
            <FighterPanel fighter={cpu} side="cpu" />
          </div>

          <div className="roster-wrap">
            <div className="roster-label"><span>ROSTER // SELECT P1</span><button onClick={() => randomRival(playerId)}>RANDOMIZE RIVAL ↻</button></div>
            <div className="roster-grid">
              {FIGHTERS.map((fighter) => <FighterCard key={fighter.id} fighter={fighter} selected={fighter.id === playerId} rival={fighter.id === cpuId} onClick={() => { setPlayerId(fighter.id); if (fighter.id === cpuId) randomRival(fighter.id); }} />)}
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
  const share = () => { const url = `${window.location.origin}${window.location.pathname}?room=${room?.id ?? code}`; void navigator.clipboard.writeText(url); };
  const email = () => { const url = `${window.location.origin}${window.location.pathname}?room=${room?.id ?? code}`; window.location.href = `mailto:?subject=${encodeURIComponent("Join my Neon Clash room")}&body=${encodeURIComponent(`Room ${room?.id ?? code}: ${url}`)}`; };
  return <div className="online-backdrop"><section className="online-lobby"><button className="close-lobby" onClick={close}>×</button><p className="eyebrow">PUBLIC MATCHMAKING // LIVE SPECTATORS</p><h2>{room ? `ROOM ${room.id}` : "ENTER THE LOBBY"}</h2><p className="lobby-copy">Every room has exactly two fighter slots. Anyone who joins after both slots are filled enters as a live spectator.</p>{room ? <div className="room-console"><div className="slot-row"><span className="filled">P1<br /><b>HOST</b></span><i>VS</i><span className={room.players === 2 ? "filled" : "waiting"}>P2<br /><b>{room.players === 2 ? "READY" : "WAITING"}</b></span></div><div className="room-metrics"><span>{room.players}/2 PLAYERS</span><span>{room.spectators} WATCHING</span><span>{room.status.toUpperCase()}</span></div><div className="room-actions"><button onClick={share}>COPY ROOM LINK</button><button onClick={email}>EMAIL INVITE</button>{room.role === "host" && <button className="primary" disabled={room.players < 2} onClick={startFight}>{room.players < 2 ? "WAITING FOR P2" : "START MATCH"}</button>}{room.role !== "host" && <button className="primary" disabled>{room.role === "spectator" ? "WATCHING ROOM" : "WAITING FOR HOST"}</button>}</div></div> : <div className="lobby-grid"><div><b>CREATE A ROOM</b><span>Your selected fighters, arena, difficulty, and outfits become the room setup.</span><button className="primary" disabled={busy} onClick={createRoom}>CREATE PUBLIC ROOM</button></div><div><b>JOIN OR WATCH</b><input value={code} onChange={(event) => setCode(event.target.value.toUpperCase().slice(0, 6))} aria-label="Room code" placeholder="6-DIGIT ROOM CODE" /><button disabled={busy || code.length !== 6} onClick={() => joinRoom(false)}>JOIN ROOM</button><button disabled={busy || code.length !== 6} onClick={() => joinRoom(true)}>WATCH ONLY</button></div></div>}{error && <strong className="connection-state failed">{error}</strong>}{!room && openRooms.length > 0 && <div className="open-room-list"><b>OPEN ROOMS</b>{openRooms.map((item) => <div key={item.id}><span><strong>{item.id}</strong><small>{item.players}/2 · {item.status.toUpperCase()}</small></span><button onClick={() => joinRoom(false, item.id)}>{item.players < 2 ? "JOIN" : "WATCH"}</button></div>)}</div>}</section></div>;
}

function FighterPanel({ fighter, side }: { fighter: Fighter; side: "player" | "cpu" }) {
  return (
    <article className={`fighter-panel ${side}`} style={portraitStyle(fighter)}>
      <div className="panel-no">{side === "player" ? "P1" : "CPU"}</div>
      <div className="hero-mark" aria-hidden="true"><span>{fighter.mark}</span></div>
      <div className="fighter-copy">
        <p>{fighter.city} {"//"} {fighter.style}</p><h2>{fighter.name}</h2><h3>{fighter.alias}</h3><blockquote>“{fighter.quote}”</blockquote>
        <div className="stat-row"><Stat label="SPD" value={fighter.speed} /><Stat label="PWR" value={fighter.power} /><Stat label="RNG" value={fighter.reach} /></div>
        <div className="special-tag"><span>SPECIAL</span><strong>{fighter.special}</strong></div>
        <div className="combo-tag"><span>{fighter.combo.name}</span><strong>{fighter.combo.sequence.join(" › ")}</strong></div>
      </div>
    </article>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return <div><span>{label}</span><i><b style={{ width: `${value * 10}%` }} /></i><em>{value}</em></div>;
}

function GameCanvas({ player, cpu, stage, outfit, difficulty, mode, role, remoteInputRef, remoteStateRef, sendInput, onSnapshot, onMatchEnd }: { player: Fighter; cpu: Fighter; stage: Stage; outfit: Outfit; difficulty: "ROOKIE" | "PRO" | "ACE"; mode: "CPU" | "ONLINE"; role: RoomRole; remoteInputRef: React.RefObject<Record<string, boolean>>; remoteStateRef: React.RefObject<MatchSnapshot | null>; sendInput: (value: Record<string, boolean>) => void; onSnapshot: (value: MatchSnapshot) => void; onMatchEnd: (value: string) => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const inputRef = useRef<Record<string, boolean>>({});

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d", { alpha: false });
    if (!ctx) return;

    const W = 1280, H = 720, FLOOR = 584;
    const lowPower = (navigator as Navigator & { deviceMemory?: number }).deviceMemory !== undefined && ((navigator as Navigator & { deviceMemory?: number }).deviceMemory ?? 8) <= 4;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const dpr = Math.min(window.devicePixelRatio || 1, lowPower ? 1.25 : 2);
    canvas.width = Math.round(W * dpr); canvas.height = Math.round(H * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.imageSmoothingEnabled = true;

    const background = document.createElement("canvas");
    background.width = W; background.height = H;
    const bg = background.getContext("2d")!;
    drawBackground(bg, W, H, stage);

    const spriteCache = new Map<string, HTMLCanvasElement>();
    for (const f of [player, cpu]) spriteCache.set(f.id, buildSprite(f, outfit));

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
    let comboBonus = 0;
    let comboCallout = 0;

    const recordAction = (key: string) => {
      const now = performance.now(); inputHistory.push({ key, at: now });
      while (inputHistory.length > 8 || (inputHistory[0] && now - inputHistory[0].at > 1200)) inputHistory.shift();
      const sequence = player.combo.sequence;
      const tail = inputHistory.slice(-sequence.length);
      if (tail.length === sequence.length && tail.every((item, index) => item.key === sequence[index]) && tail[tail.length - 1].at - tail[0].at <= 1100) {
        comboBonus = 12; comboCallout = 1.25; p1.drive = clamp(p1.drive + 18, 0, 100); inputHistory.length = 0;
      }
    };

    const applyCombatantSnapshot = (target: Combatant, value: CombatantSnapshot) => {
      target.x = value.x; target.y = value.y; target.vx = value.vx; target.vy = value.vy; target.facing = value.facing;
      target.health = value.health; target.drive = value.drive; target.grounded = value.grounded; target.crouching = value.crouching; target.guarding = value.guarding;
      target.attack = value.attack; target.attackTime = value.attackTime; target.attackHit = value.attackHit; target.hurtTime = value.hurtTime; target.stunTime = value.stunTime;
      target.flashTime = value.flashTime; target.combo = value.combo; target.comboWindow = value.comboWindow; target.wins = value.wins;
    };

    const applyRemoteSnapshot = () => {
      const snapshot = remoteStateRef.current; if (!snapshot) return;
      applyCombatantSnapshot(p1, snapshot.p1); applyCombatantSnapshot(p2, snapshot.p2); timer = snapshot.timer; round = snapshot.round; roundState = snapshot.roundState;
      projectiles.length = 0;
      for (const item of snapshot.projectiles ?? []) projectiles.push({ ...item, owner: item.owner === 1 ? p1 : p2 });
    };

    const combatantSnapshot = (value: Combatant): CombatantSnapshot => ({ x: value.x, y: value.y, vx: value.vx, vy: value.vy, facing: value.facing, health: value.health, drive: value.drive, grounded: value.grounded, crouching: value.crouching, guarding: value.guarding, attack: value.attack, attackTime: value.attackTime, attackHit: value.attackHit, hurtTime: value.hurtTime, stunTime: value.stunTime, flashTime: value.flashTime, combo: value.combo, comboWindow: value.comboWindow, wins: value.wins });

    const snapshot = (): MatchSnapshot => ({ p1: combatantSnapshot(p1), p2: combatantSnapshot(p2), timer, round, roundState, projectiles: projectiles.map((item) => ({ x: item.x, y: item.y, vx: item.vx, life: item.life, color: item.color, damage: item.damage, owner: item.owner === p1 ? 1 : 2 })) });

    const burst = (x: number, y: number, color: string, count: number) => {
      const room = particleCap - particles.length;
      for (let i = 0; i < Math.min(room, count); i++) {
        const angle = Math.random() * Math.PI * 2;
        const speed = 70 + Math.random() * 260;
        particles.push({ x, y, vx: Math.cos(angle) * speed, vy: Math.sin(angle) * speed, life: 0.25 + Math.random() * 0.35, maxLife: 0.6, color, size: 2 + Math.random() * 8 });
      }
    };

    const startAttack = (c: Combatant, type: Combatant["attack"]) => {
      if (!type || c.attack || c.hurtTime > 0 || c.stunTime > 0 || roundState !== "fight") return;
      const costs = { lightPunch: 0, heavyPunch: 0, lightKick: 0, heavyKick: 0, special: 25, impact: 32 };
      if (c.drive < costs[type]) return;
      c.drive -= costs[type]; c.attack = type; c.attackTime = 0; c.attackHit = false; c.guarding = false;
      if (type === "special" && (c.fighter.style === "Zoner" || c.fighter.style === "Control")) {
        projectiles.push({ x: c.x + c.facing * 70, y: c.y - 116, vx: c.facing * (520 + c.fighter.reach * 10), life: 1.8, owner: c, color: c.fighter.color, damage: 13 + c.fighter.power * 0.45 + (c === p1 ? comboBonus : 0) });
      }
    };

    const hit = (attacker: Combatant, defender: Combatant, damage: number, force: number, color: string, impact = false) => {
      if (defender.hurtTime > 0.02 || roundState !== "fight") return;
      const blocked = defender.guarding && defender.grounded && defender.facing === -attacker.facing;
      const dealt = blocked ? damage * 0.28 : damage;
      defender.health = clamp(defender.health - dealt, 0, 100);
      defender.drive = clamp(defender.drive - (blocked ? 7 : 3), 0, 100);
      defender.vx = attacker.facing * force * (blocked ? 0.35 : 1);
      defender.hurtTime = blocked ? 0.12 : impact ? 0.46 : 0.24;
      defender.flashTime = 0.1;
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
    }[type]);

    const updateCombatant = (c: Combatant, foe: Combatant, move: number, wants: Record<string, boolean>, dt: number) => {
      c.facing = c.x < foe.x ? 1 : -1;
      c.hurtTime = Math.max(0, c.hurtTime - dt); c.stunTime = Math.max(0, c.stunTime - dt); c.flashTime = Math.max(0, c.flashTime - dt);
      c.comboWindow = Math.max(0, c.comboWindow - dt); if (c.comboWindow === 0) c.combo = 0;
      c.drive = clamp(c.drive + dt * (c.guarding ? 1.5 : 5.5), 0, 100);
      c.crouching = wants.crouch && c.grounded && !c.attack;
      c.guarding = wants.guard && c.grounded && !c.attack && c.hurtTime <= 0;
      if (c.hurtTime <= 0 && c.stunTime <= 0 && !c.attack && roundState === "fight") {
        const speed = (190 + c.fighter.speed * 16) * (c.crouching || c.guarding ? 0.22 : 1);
        c.vx = lerp(c.vx, move * speed, 0.26);
        if (wants.jump && c.grounded && !c.crouching && !c.guarding) { c.vy = -(550 + c.fighter.speed * 7); c.grounded = false; }
        if (wants.lightPunch) startAttack(c, "lightPunch");
        else if (wants.heavyPunch) startAttack(c, "heavyPunch");
        else if (wants.lightKick) startAttack(c, "lightKick");
        else if (wants.heavyKick) startAttack(c, "heavyKick");
        else if (wants.special) startAttack(c, "special");
        else if (wants.impact) startAttack(c, "impact");
      } else if (c.hurtTime > 0 || c.stunTime > 0) c.guarding = false;

      if (c.attack) {
        c.attackTime += dt;
        const data = attackData(c.attack);
        const isProjectileSpecial = c.attack === "special" && (c.fighter.style === "Zoner" || c.fighter.style === "Control");
        if (!c.attackHit && !isProjectileSpecial && c.attackTime >= data.activeA && c.attackTime <= data.activeB && Math.abs(c.x - foe.x) < data.range + c.fighter.reach * 2 && Math.abs(c.y - foe.y) < 105) {
          c.attackHit = true; hit(c, foe, data.damage + c.fighter.power * 0.42 + (c === p1 && c.attack === "special" ? comboBonus : 0), data.force, c.fighter.color, c.attack === "impact");
        }
        if (c.attack === "special" && !isProjectileSpecial && c.attackTime < 0.34) c.vx += c.facing * 28;
        if (c.attackTime >= data.end) { if (c === p1 && c.attack === "special") comboBonus = 0; c.attack = null; c.attackTime = 0; }
      }

      c.vy += 1450 * dt; c.x += c.vx * dt; c.y += c.vy * dt; c.vx *= c.grounded ? 0.82 : 0.985;
      if (c.y >= FLOOR) { c.y = FLOOR; c.vy = 0; c.grounded = true; } else c.grounded = false;
      c.x = clamp(c.x, 82, W - 82);
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
      return { move: aiIntent === "approach" ? p2.facing : aiIntent === "retreat" ? -p2.facing : 0, jump: aiIntent === "jump", crouch: false, guard: aiIntent === "guard", lightPunch: aiIntent === "lightPunch", heavyPunch: aiIntent === "heavyPunch", lightKick: aiIntent === "lightKick", heavyKick: aiIntent === "heavyKick", special: aiIntent === "special", impact: aiIntent === "impact" };
    };

    const resetRound = () => {
      p1.x = 350; p1.y = FLOOR; p1.vx = p1.vy = 0; p1.health = 100; p1.drive = 65; p1.attack = null; p1.hurtTime = p1.stunTime = 0;
      p2.x = 930; p2.y = FLOOR; p2.vx = p2.vy = 0; p2.health = 100; p2.drive = 65; p2.attack = null; p2.hurtTime = p2.stunTime = 0;
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

      comboCallout = Math.max(0, comboCallout - dt);
      const local = { jump: !!inputs.jump, crouch: !!inputs.crouch, guard: !!inputs.guard, lightPunch: !!inputs.lightPunch, heavyPunch: !!inputs.heavyPunch, lightKick: !!inputs.lightKick, heavyKick: !!inputs.heavyKick, special: !!inputs.special, impact: !!inputs.impact };
      const localMove = (inputs.left ? -1 : 0) + (inputs.right ? 1 : 0);
      const remote = remoteInputRef.current ?? {};
      const remoteWants = { move: (remote.left ? -1 : 0) + (remote.right ? 1 : 0), jump: !!remote.jump, crouch: !!remote.crouch, guard: !!remote.guard, lightPunch: !!remote.lightPunch, heavyPunch: !!remote.heavyPunch, lightKick: !!remote.lightKick, heavyKick: !!remote.heavyKick, special: !!remote.special, impact: !!remote.impact };
      const ai = mode === "CPU" ? cpuWants(dt) : remoteWants;
      updateCombatant(p1, p2, localMove, local, dt); updateCombatant(p2, p1, ai.move, ai, dt);

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
      const sprite = spriteCache.get(c.fighter.id)!;
      const attack = c.attack ? attackData(c.attack) : null;
      const reach = c.attack && attack && c.attackTime > attack.activeA * 0.75 && c.attackTime < attack.activeB ? (c.attack === "lightPunch" ? 18 : c.attack === "lightKick" ? 26 : c.attack === "impact" ? 44 : 34) : 0;
      const bob = c.grounded ? Math.sin(performance.now() * 0.004) * 2 : 0;
      ctx.save(); ctx.translate(c.x + c.facing * reach, c.y + bob); ctx.scale(c.facing, 1);
      if (c.guarding) { ctx.globalAlpha = 0.42; ctx.strokeStyle = c.fighter.color; ctx.lineWidth = 10; ctx.beginPath(); ctx.arc(6, -125, 88, -1.25, 1.25); ctx.stroke(); ctx.globalAlpha = 1; }
      if (c.flashTime > 0) ctx.globalCompositeOperation = "screen";
      ctx.drawImage(sprite, -112, c.crouching ? -250 : -300, 224, c.crouching ? 250 : 300);
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
      ctx.fillStyle = "#f5fbff"; ctx.textAlign = "center"; ctx.font = "900 38px Arial"; ctx.fillText(String(Math.ceil(timer)).padStart(2, "0"), W / 2, 75); ctx.font = "700 12px Arial"; ctx.fillStyle = "#9ba9c5"; ctx.fillText(`ROUND ${round}`, W / 2, 96);
      for (let i = 0; i < 2; i++) { ctx.fillStyle = i < p1.wins ? p1.fighter.color : "#26304a"; ctx.beginPath(); ctx.arc(444 + i * 20, 118, 6, 0, Math.PI * 2); ctx.fill(); ctx.fillStyle = i < p2.wins ? p2.fighter.color : "#26304a"; ctx.beginPath(); ctx.arc(W - 444 - i * 20, 118, 6, 0, Math.PI * 2); ctx.fill(); }
    };

    const draw = () => {
      ctx.save();
      const sx = shake > 0.5 ? (Math.random() - 0.5) * shake : 0, sy = shake > 0.5 ? (Math.random() - 0.5) * shake * 0.5 : 0;
      ctx.translate(sx, sy); ctx.drawImage(background, 0, 0);
      for (const q of projectiles) { ctx.globalAlpha = 0.28; ctx.fillStyle = q.color; ctx.beginPath(); ctx.arc(q.x, q.y, 34, 0, Math.PI * 2); ctx.fill(); ctx.globalAlpha = 1; ctx.fillStyle = "#f5ffff"; ctx.beginPath(); ctx.arc(q.x, q.y, 15, 0, Math.PI * 2); ctx.fill(); }
      drawCombatant(p1); drawCombatant(p2);
      for (const q of particles) { ctx.globalAlpha = clamp(q.life / q.maxLife, 0, 1); ctx.fillStyle = q.color; ctx.fillRect(q.x, q.y, q.size * 2.4, q.size); } ctx.globalAlpha = 1;
      drawHud();
      if (roundState === "intro") drawCenterText(`ROUND ${round}`, "FIGHT");
      if (roundState === "ko") drawCenterText("K.O.", p1.health > p2.health ? p1.fighter.name : p2.fighter.name);
      if (comboCallout > 0) { ctx.textAlign = "center"; ctx.fillStyle = p1.fighter.color; ctx.font = "italic 900 32px Arial"; ctx.fillText(player.combo.name, W / 2, 175); ctx.font = "800 13px Arial"; ctx.fillStyle = "#eefcff"; ctx.fillText("SKILL COMBO ACTIVATED", W / 2, 198); }
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

    const keyMap: Record<string, string> = { KeyA: "left", KeyD: "right", KeyW: "jump", KeyS: "crouch", KeyT: "lightPunch", KeyY: "heavyPunch", KeyU: "lightKick", KeyK: "heavyKick", KeyL: "special", KeyO: "impact", Space: "guard" };
    const comboKeys: Record<string, string> = { lightPunch: "T", heavyPunch: "Y", lightKick: "U", heavyKick: "K", special: "L" };
    const onKey = (event: KeyboardEvent, down: boolean) => { const control = keyMap[event.code]; if (control && role !== "spectator") { event.preventDefault(); inputs[control] = down; if (down && !event.repeat && comboKeys[control]) recordAction(comboKeys[control]); sendInput({ ...inputs }); } };
    const keyDown = (e: KeyboardEvent) => onKey(e, true), keyUp = (e: KeyboardEvent) => onKey(e, false);
    window.addEventListener("keydown", keyDown, { passive: false }); window.addEventListener("keyup", keyUp, { passive: false });
    const visibility = () => { last = performance.now(); accumulator = 0; };
    document.addEventListener("visibilitychange", visibility);
    raf = requestAnimationFrame(loop);
    return () => { running = false; cancelAnimationFrame(raf); window.removeEventListener("keydown", keyDown); window.removeEventListener("keyup", keyUp); document.removeEventListener("visibilitychange", visibility); };
  }, [cpu, difficulty, mode, onMatchEnd, onSnapshot, outfit, player, remoteInputRef, remoteStateRef, role, sendInput, stage]);

  const setControl = (control: string, value: boolean) => { inputRef.current[control] = value; sendInput({ ...inputRef.current }); };

  return (
    <div className="arena-wrap">
      <canvas ref={canvasRef} aria-label={`${player.name} versus ${cpu.name} fighting arena`} />
      {mode === "ONLINE" && <div className="room-hud">{role === "spectator" ? "● WATCHING LIVE" : role === "host" ? "P1 // HOST" : "P2 // CHALLENGER"}</div>}
      {role !== "spectator" && <div className="touch-controls" aria-label="Touch controls">
        <div className="touch-move"><TouchButton label="◀" control="left" setControl={setControl} /><TouchButton label="▲" control="jump" setControl={setControl} /><TouchButton label="▼" control="crouch" setControl={setControl} /><TouchButton label="▶" control="right" setControl={setControl} /></div>
        <div className="touch-action"><TouchButton label="T" control="lightPunch" setControl={setControl} /><TouchButton label="Y" control="heavyPunch" setControl={setControl} /><TouchButton label="U" control="lightKick" setControl={setControl} /><TouchButton label="K" control="heavyKick" setControl={setControl} /><TouchButton label="L" control="special" setControl={setControl} /><TouchButton label="GD" control="guard" setControl={setControl} /></div>
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

function buildSprite(fighter: Fighter, outfit: Outfit) {
  const c = document.createElement("canvas"); c.width = 224; c.height = 300; const x = c.getContext("2d")!;
  const g = x.createLinearGradient(20, 20, 190, 280); g.addColorStop(0, fighter.color); g.addColorStop(1, fighter.secondary);
  x.fillStyle = "rgba(0,0,0,.28)"; x.beginPath(); x.ellipse(112, 286, 74, 12, 0, 0, Math.PI * 2); x.fill();
  x.fillStyle = g; x.strokeStyle = "#060813"; x.lineWidth = 8; x.lineJoin = "round";
  x.beginPath(); x.moveTo(77, 124); x.lineTo(58, 204); x.lineTo(37, 279); x.lineTo(77, 279); x.lineTo(106, 200); x.lineTo(119, 130); x.closePath(); x.fill(); x.stroke();
  x.beginPath(); x.moveTo(132, 132); x.lineTo(148, 208); x.lineTo(162, 279); x.lineTo(198, 279); x.lineTo(183, 197); x.lineTo(169, 118); x.closePath(); x.fill(); x.stroke();
  x.beginPath(); x.moveTo(72, 92); x.quadraticCurveTo(112, 64, 165, 91); x.lineTo(175, 174); x.quadraticCurveTo(119, 206, 60, 169); x.closePath(); x.fill(); x.stroke();
  if (outfit.cut === "sleek") { x.fillStyle = "#070a14"; x.beginPath(); x.moveTo(78, 92); x.lineTo(113, 155); x.lineTo(158, 92); x.lineTo(168, 164); x.lineTo(64, 164); x.closePath(); x.fill(); x.strokeStyle = fighter.color; x.lineWidth = 4; x.stroke(); }
  if (outfit.cut === "heatwave") { x.fillStyle = "#070a14"; x.fillRect(67, 136, 100, 32); x.clearRect(88, 105, 50, 28); x.strokeStyle = fighter.color; x.lineWidth = 4; x.strokeRect(67, 136, 100, 32); }
  x.beginPath(); x.moveTo(72, 105); x.lineTo(21, 177); x.lineTo(53, 190); x.lineTo(98, 133); x.closePath(); x.fill(); x.stroke();
  x.beginPath(); x.moveTo(154, 102); x.lineTo(205, 157); x.lineTo(184, 183); x.lineTo(137, 135); x.closePath(); x.fill(); x.stroke();
  x.beginPath(); x.arc(117, 56, fighter.style === "Grappler" ? 42 : 34, 0, Math.PI * 2); x.fill(); x.stroke();
  x.fillStyle = "#effcff"; x.globalAlpha = 0.85; x.fillRect(91, 49, 55, 8); x.globalAlpha = 1;
  if (fighter.style === "Zoner" || fighter.style === "Trickster") { x.strokeStyle = fighter.color; x.lineWidth = 9; x.beginPath(); x.moveTo(82, 62); x.quadraticCurveTo(15, 80, 30, 140); x.stroke(); }
  if (fighter.style === "Grappler" || fighter.style === "Armor") { x.fillStyle = "#11182b"; x.fillRect(20, 160, 40, 38); x.fillRect(178, 151, 38, 38); }
  x.fillStyle = "rgba(5,7,18,.6)"; x.font = "900 54px Arial"; x.textAlign = "center"; x.fillText(fighter.mark, 117, 150);
  return c;
}
