/**
 * Farm map model — grid state, movement, clearing.
 */

export class FarmMap {
  constructor(data) {
    this.id = data.id;
    this.name = data.name;
    this.width = data.width;
    this.height = data.height;
    this.obstacleTypes = data.obstacleTypes;
    this.tiles = data.tiles.map((row) =>
      row.map((id) => (id === "empty" || !id ? null : id))
    );
    this.soil = data.soil.map((row) => row.slice());
    this.clearedCount = 0;
  }

  inBounds(x, y) {
    return x >= 0 && y >= 0 && x < this.width && y < this.height;
  }

  getObstacleId(x, y) {
    if (!this.inBounds(x, y)) return null;
    return this.tiles[y][x];
  }

  getObstacle(x, y) {
    const id = this.getObstacleId(x, y);
    return id ? this.obstacleTypes[id] : null;
  }

  blocksMovement(x, y) {
    const obs = this.getObstacle(x, y);
    return !!(obs && obs.blocksMovement);
  }

  tryMove(player, dx, dy) {
    const nx = player.x + dx;
    const ny = player.y + dy;
    if (!this.inBounds(nx, ny)) return false;
    if (this.blocksMovement(nx, ny)) return false;
    player.x = nx;
    player.y = ny;
    if (dx > 0) player.facing = "se";
    else if (dx < 0) player.facing = "nw";
    else if (dy > 0) player.facing = "sw";
    else if (dy < 0) player.facing = "ne";
    return true;
  }

  clearTile(x, y) {
    if (!this.inBounds(x, y) || !this.tiles[y][x]) return null;
    const id = this.tiles[y][x];
    const type = this.obstacleTypes[id];
    this.tiles[y][x] = null;
    this.clearedCount += 1;
    return { id, type };
  }

  tilable(x, y) {
    return this.inBounds(x, y) && !this.getObstacleId(x, y);
  }

  obstacleStats() {
    const counts = {};
    let remaining = 0;
    for (let y = 0; y < this.height; y++) {
      for (let x = 0; x < this.width; x++) {
        const id = this.tiles[y][x];
        if (!id) continue;
        remaining += 1;
        counts[id] = (counts[id] || 0) + 1;
      }
    }
    return { remaining, counts, cleared: this.clearedCount };
  }
}
