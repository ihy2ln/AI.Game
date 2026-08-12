/** Tiny 2×2 aesthetic sandbox — ES module for web/APK preview. */
export const farmMap4x4 = {
  id: "farm_aesthetics_2x2",
  name: "Aesthetic Sandbox",
  width: 2,
  height: 2,
  tileSize: 64,
  aesthetic: "2.5d-anime-hd-pixel",
  description: "Tiny map for locking 2.5D anime × HD pixel art.",
  playerStart: { x: 1, y: 1 },
  obstacleTypes: {
    weed: {
      label: "Weed",
      requiredLevel: 1,
      xp: 6,
      tool: "scythe",
      blocksMovement: false,
      blocksPlanting: true,
    },
    rock: {
      label: "Rock",
      requiredLevel: 2,
      xp: 12,
      tool: "pickaxe",
      blocksMovement: true,
      blocksPlanting: true,
    },
    tree: {
      label: "Oak Tree",
      requiredLevel: 3,
      xp: 20,
      tool: "axe",
      blocksMovement: true,
      blocksPlanting: true,
    },
  },
  tiles: [
    ["tree", "weed"],
    ["rock", ""],
  ],
  soil: [
    ["untilled", "untilled"],
    ["untilled", "tilled"],
  ],
};

export default farmMap4x4;
