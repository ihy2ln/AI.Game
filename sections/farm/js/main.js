/**
 * Farm section entry — 4×4 starter plot with level-gated clearing.
 */

import { FarmMap } from "./map.js";
import { FarmRenderer } from "./renderer.js";
import { canClear, applyXp, createPlayer, xpForNextLevel } from "./obstacles.js";

const logEl = document.getElementById("log");
const levelEl = document.getElementById("level");
const xpEl = document.getElementById("xp");
const xpBarEl = document.getElementById("xpBar");
const clearedEl = document.getElementById("cleared");
const remainingEl = document.getElementById("remaining");
const tileInfoEl = document.getElementById("tileInfo");
const canvas = document.getElementById("farm");

let map;
let player;
let renderer;
let lastMessage = "Arrow keys / WASD move · Space or click clears the tile under you.";

function pushLog(text, kind = "info") {
  lastMessage = text;
  const line = document.createElement("div");
  line.className = `log-line ${kind}`;
  line.textContent = text;
  logEl.prepend(line);
  while (logEl.children.length > 8) logEl.removeChild(logEl.lastChild);
}

function refreshHud() {
  levelEl.textContent = String(player.level);
  const need = xpForNextLevel(player.level);
  if (need == null) {
    xpEl.textContent = "MAX";
    xpBarEl.style.width = "100%";
  } else {
    xpEl.textContent = `${player.xp} / ${need}`;
    xpBarEl.style.width = `${Math.min(100, (player.xp / need) * 100)}%`;
  }
  const stats = map.obstacleStats();
  clearedEl.textContent = String(stats.cleared);
  remainingEl.textContent = String(stats.remaining);

  const obs = map.getObstacle(player.x, player.y);
  const soil = map.soil[player.y][player.x];
  if (obs) {
    const gate = canClear(player, obs);
    tileInfoEl.innerHTML = `
      <strong>${obs.label}</strong>
      <span>Requires Farm Lv.${obs.requiredLevel} · ${obs.tool}</span>
      <span class="${gate.ok ? "ok" : "warn"}">${gate.reason}</span>
    `;
  } else {
    tileInfoEl.innerHTML = `
      <strong>Clear soil</strong>
      <span>${soil === "tilled" ? "Tilled — ready for crops later." : "Untilled grass."}</span>
      <span class="ok">No obstacle.</span>
    `;
  }
}

function attemptClear(x, y) {
  const obs = map.getObstacle(x, y);
  if (!obs) {
    pushLog("Nothing to clear here.", "muted");
    return;
  }
  const gate = canClear(player, obs);
  if (!gate.ok) {
    pushLog(gate.reason, "warn");
    return;
  }
  const cleared = map.clearTile(x, y);
  const xpResult = applyXp(player, cleared.type.xp);
  renderer.pulse();
  pushLog(`Cleared ${cleared.type.label} (+${cleared.type.xp} XP).`, "ok");
  if (xpResult.leveledUp) {
    pushLog(`Farm level up! Now Lv.${player.level}.`, "level");
  }
  const stats = map.obstacleStats();
  if (stats.remaining === 0) {
    pushLog("Starter plot cleared. Larger maps come next.", "level");
  }
  refreshHud();
}

function onKey(e) {
  const key = e.key.toLowerCase();
  let moved = false;
  if (key === "arrowup" || key === "w") moved = map.tryMove(player, 0, -1);
  else if (key === "arrowdown" || key === "s") moved = map.tryMove(player, 0, 1);
  else if (key === "arrowleft" || key === "a") moved = map.tryMove(player, -1, 0);
  else if (key === "arrowright" || key === "d") moved = map.tryMove(player, 1, 0);
  else if (key === " " || key === "enter" || key === "e") {
    e.preventDefault();
    attemptClear(player.x, player.y);
    return;
  } else return;

  if (moved) refreshHud();
  else if (["arrowup", "arrowdown", "arrowleft", "arrowright", "w", "a", "s", "d"].includes(key)) {
    const dx = key === "arrowright" || key === "d" ? 1 : key === "arrowleft" || key === "a" ? -1 : 0;
    const dy = key === "arrowdown" || key === "s" ? 1 : key === "arrowup" || key === "w" ? -1 : 0;
    const tx = player.x + dx;
    const ty = player.y + dy;
    if (map.inBounds(tx, ty) && map.blocksMovement(tx, ty)) {
      const obs = map.getObstacle(tx, ty);
      const gate = canClear(player, obs);
      pushLog(gate.ok ? `Press Space to clear ${obs.label}.` : gate.reason, gate.ok ? "info" : "warn");
    }
  }
}

function bindPointer() {
  canvas.addEventListener("mousemove", (e) => {
    const rect = canvas.getBoundingClientRect();
    const tile = renderer.screenToTile(e.clientX - rect.left, e.clientY - rect.top, map);
    renderer.hover = tile;
  });
  canvas.addEventListener("mouseleave", () => {
    renderer.hover = null;
  });
  canvas.addEventListener("click", (e) => {
    const rect = canvas.getBoundingClientRect();
    const tile = renderer.screenToTile(e.clientX - rect.left, e.clientY - rect.top, map);
    if (!tile) return;
    const dx = Math.abs(tile.x - player.x) + Math.abs(tile.y - player.y);
    if (dx === 0) {
      attemptClear(tile.x, tile.y);
      return;
    }
    if (dx === 1 && !map.blocksMovement(tile.x, tile.y)) {
      player.x = tile.x;
      player.y = tile.y;
      refreshHud();
      return;
    }
    if (map.getObstacle(tile.x, tile.y)) {
      const obs = map.getObstacle(tile.x, tile.y);
      const gate = canClear(player, obs);
      pushLog(
        gate.ok
          ? `Walk adjacent / stand on clearable tiles, then Space. Target: ${obs.label}.`
          : gate.reason,
        gate.ok ? "info" : "warn"
      );
    }
  });
}

function loop() {
  renderer.draw(map, player, lastMessage);
  requestAnimationFrame(loop);
}

async function boot() {
  const res = await fetch("./data/farm-map-4x4.json");
  const data = await res.json();
  map = new FarmMap(data);
  player = createPlayer(data.playerStart);
  renderer = new FarmRenderer(canvas);
  document.getElementById("mapName").textContent = data.name;
  document.getElementById("mapSize").textContent = `${data.width}×${data.height}`;

  window.addEventListener("keydown", onKey);
  window.addEventListener("resize", () => renderer.resize());
  bindPointer();
  refreshHud();
  pushLog("Welcome to the starter farm. Clear weeds first — boulders wait for higher levels.", "info");
  loop();
}

boot().catch((err) => {
  console.error(err);
  pushLog("Failed to load farm map data.", "warn");
});
