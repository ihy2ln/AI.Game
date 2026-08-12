/**
 * Farm section entry — 4×4 starter plot with level-gated clearing.
 */

import { FarmMap } from "./map.js";
import { FarmRenderer } from "./renderer.js";
import { canClear, applyXp, createPlayer, xpForNextLevel } from "./obstacles.js";
import farmMapData from "../data/farm-map-4x4.js";

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
  remainingEl.textContent = String(stats.cleared + stats.remaining);

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
  renderer.spawnFx(x, y, `+${cleared.type.xp} EXP`, "#e8b060");
  pushLog(`Cleared ${cleared.type.label} (+${cleared.type.xp} XP).`, "ok");
  if (xpResult.leveledUp) {
    renderer.spawnFx(x, y, `Lv.${player.level}!`, "#6f9b7a");
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
  if (key === "arrowup" || key === "w") tryStep(0, -1);
  else if (key === "arrowdown" || key === "s") tryStep(0, 1);
  else if (key === "arrowleft" || key === "a") tryStep(-1, 0);
  else if (key === "arrowright" || key === "d") tryStep(1, 0);
  else if (key === " " || key === "enter" || key === "e") {
    e.preventDefault();
    attemptClear(player.x, player.y);
  }
}

function tryStep(dx, dy) {
  const moved = map.tryMove(player, dx, dy);
  if (moved) {
    refreshHud();
    return;
  }
  const tx = player.x + dx;
  const ty = player.y + dy;
  if (map.inBounds(tx, ty) && map.blocksMovement(tx, ty)) {
    const obs = map.getObstacle(tx, ty);
    const gate = canClear(player, obs);
    pushLog(gate.ok ? `Tap CLEAR to remove ${obs.label}.` : gate.reason, gate.ok ? "info" : "warn");
  }
}

function bindPointer() {
  const onHover = (clientX, clientY) => {
    const rect = canvas.getBoundingClientRect();
    renderer.hover = renderer.screenToTile(clientX - rect.left, clientY - rect.top, map);
  };
  canvas.addEventListener("mousemove", (e) => onHover(e.clientX, e.clientY));
  canvas.addEventListener("mouseleave", () => {
    renderer.hover = null;
  });
  canvas.addEventListener("touchstart", (e) => {
    if (e.touches[0]) onHover(e.touches[0].clientX, e.touches[0].clientY);
  }, { passive: true });

  const onTap = (clientX, clientY) => {
    const rect = canvas.getBoundingClientRect();
    const tile = renderer.screenToTile(clientX - rect.left, clientY - rect.top, map);
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
        gate.ok ? `Move next to it, then CLEAR. Target: ${obs.label}.` : gate.reason,
        gate.ok ? "info" : "warn"
      );
    }
  };
  canvas.addEventListener("click", (e) => onTap(e.clientX, e.clientY));
}

function bindTouchPad() {
  document.querySelectorAll(".touch-pad [data-move]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const [dx, dy] = btn.getAttribute("data-move").split(",").map(Number);
      tryStep(dx, dy);
    });
  });
  document.getElementById("btnClear").addEventListener("click", () => {
    attemptClear(player.x, player.y);
  });
}

function loop() {
  renderer.draw(map, player, lastMessage);
  requestAnimationFrame(loop);
}

async function boot() {
  const data = farmMapData;
  map = new FarmMap(data);
  player = createPlayer(data.playerStart);
  renderer = new FarmRenderer(canvas);
  document.getElementById("mapName").textContent = data.name;
  document.getElementById("mapSize").textContent = `${data.width}×${data.height}`;

  window.addEventListener("keydown", onKey);
  window.addEventListener("resize", () => renderer.resize());
  bindPointer();
  bindTouchPad();
  refreshHud();
  pushLog("Welcome to the starter farm. Clear weeds first — boulders wait for higher levels.", "info");
  loop();
}

boot().catch((err) => {
  console.error(err);
  pushLog("Failed to boot farm section.", "warn");
});
