/**
 * Farm obstacle & leveling rules (Stardew / Rune Factory style).
 * Obstacles block planting until cleared; clearance gated by farm level.
 */

export const LEVEL_CURVE = [
  0,   // 1
  15,  // 2
  40,  // 3
  80,  // 4
  140, // 5
  220, // 6
];

export function xpForNextLevel(level) {
  if (level >= LEVEL_CURVE.length) return null;
  return LEVEL_CURVE[level];
}

export function applyXp(player, amount) {
  const result = { leveledUp: false, levelsGained: 0, xpGained: amount };
  player.xp += amount;
  while (true) {
    const need = xpForNextLevel(player.level);
    if (need == null || player.xp < need) break;
    player.xp -= need;
    player.level += 1;
    result.leveledUp = true;
    result.levelsGained += 1;
  }
  return result;
}

export function canClear(player, obstacleType) {
  if (!obstacleType) return { ok: false, reason: "Nothing to clear." };
  if (player.level < obstacleType.requiredLevel) {
    return {
      ok: false,
      reason: `Need Farm Lv.${obstacleType.requiredLevel} (you are Lv.${player.level}).`,
    };
  }
  return { ok: true, reason: `Clear with ${obstacleType.tool}.` };
}

export function createPlayer(start) {
  return {
    x: start.x,
    y: start.y,
    level: 1,
    xp: 0,
    facing: "se",
  };
}
