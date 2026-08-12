/**
 * Brown Dust 2–inspired HD-2D isometric farm renderer.
 * Crisp pixel tiles, soft atmospheric haze, layered sprites.
 */

const PALETTE = {
  skyTop: "#1a2840",
  skyMid: "#3d5a7a",
  skyHorizon: "#c4a070",
  haze: "rgba(255, 220, 180, 0.12)",
  grassA: "#3d6b45",
  grassB: "#355f3d",
  grassHi: "#5a9a62",
  grassShadow: "#2a4a32",
  soil: "#6b4a32",
  soilDark: "#4a3224",
  tilled: "#8a5e3c",
  tilledLine: "#6b452c",
  wood: "#5c3a22",
  woodHi: "#8a5a34",
  leaf: "#2f6b3a",
  leafHi: "#4a9a55",
  leafDark: "#1e4a28",
  rock: "#6a7080",
  rockHi: "#9aa2b0",
  rockDark: "#3e4450",
  weed: "#4a7a38",
  bush: "#2d5530",
  thorn: "#8a6a40",
  amber: "#c89257",
  pale: "#e8dcc8",
  ink: "#0c1116",
  player: "#d4a574",
  playerCloth: "#4a6fa5",
  playerHair: "#2a2018",
  locked: "rgba(192, 87, 79, 0.35)",
  unlockable: "rgba(111, 155, 122, 0.3)",
};

function iso(x, y, tileW, tileH) {
  return {
    sx: (x - y) * (tileW / 2),
    sy: (x + y) * (tileH / 2),
  };
}

export class FarmRenderer {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext("2d");
    this.tileW = 88;
    this.tileH = 44;
    this.originX = 0;
    this.originY = 0;
    this.hover = null;
    this.flash = null;
    this.t = 0;
  }

  resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const w = this.canvas.clientWidth;
    const h = this.canvas.clientHeight;
    this.canvas.width = Math.floor(w * dpr);
    this.canvas.height = Math.floor(h * dpr);
    this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    this.viewW = w;
    this.viewH = h;
  }

  layout(map) {
    const spanX = (map.width + map.height) * (this.tileW / 2);
    const spanY = (map.width + map.height) * (this.tileH / 2);
    this.originX = Math.round(this.viewW / 2);
    this.originY = Math.round(this.viewH * 0.38 - spanY / 2 + this.tileH);
    this.mapSpan = { spanX, spanY };
  }

  worldToScreen(x, y) {
    const p = iso(x, y, this.tileW, this.tileH);
    return { x: this.originX + p.sx, y: this.originY + p.sy };
  }

  screenToTile(mx, my, map) {
    const lx = mx - this.originX;
    const ly = my - this.originY;
    const tx = (lx / (this.tileW / 2) + ly / (this.tileH / 2)) / 2;
    const ty = (ly / (this.tileH / 2) - lx / (this.tileW / 2)) / 2;
    const x = Math.round(tx);
    const y = Math.round(ty);
    if (!map.inBounds(x, y)) return null;
    return { x, y };
  }

  draw(map, player, message) {
    const ctx = this.ctx;
    this.t += 0.016;
    this.resize();
    this.layout(map);

    this.drawSky(ctx);
    this.drawGroundShadow(ctx, map);

    const order = [];
    for (let y = 0; y < map.height; y++) {
      for (let x = 0; x < map.width; x++) order.push({ x, y });
    }
    order.sort((a, b) => a.x + a.y - (b.x + b.y));

    for (const cell of order) {
      this.drawTile(ctx, map, cell.x, cell.y, player);
    }

    for (const cell of order) {
      this.drawObstacle(ctx, map, cell.x, cell.y, player);
      if (player.x === cell.x && player.y === cell.y) {
        this.drawPlayer(ctx, player);
      }
    }

    this.drawVignette(ctx);
    if (this.flash) this.drawFlash(ctx);
  }

  drawSky(ctx) {
    const g = ctx.createLinearGradient(0, 0, 0, this.viewH);
    g.addColorStop(0, PALETTE.skyTop);
    g.addColorStop(0.45, PALETTE.skyMid);
    g.addColorStop(0.72, PALETTE.skyHorizon);
    g.addColorStop(1, "#2a4030");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, this.viewW, this.viewH);

    // soft sun glow
    const sx = this.viewW * 0.72;
    const sy = this.viewH * 0.18;
    const rad = ctx.createRadialGradient(sx, sy, 10, sx, sy, 180);
    rad.addColorStop(0, "rgba(255, 210, 140, 0.55)");
    rad.addColorStop(0.4, "rgba(255, 180, 100, 0.18)");
    rad.addColorStop(1, "rgba(255, 180, 100, 0)");
    ctx.fillStyle = rad;
    ctx.fillRect(0, 0, this.viewW, this.viewH);

    // floating dust motes
    ctx.fillStyle = "rgba(255, 230, 190, 0.35)";
    for (let i = 0; i < 18; i++) {
      const px = ((i * 97 + this.t * 12) % (this.viewW + 40)) - 20;
      const py = 40 + ((i * 53) % (this.viewH * 0.45));
      const r = 1 + (i % 3);
      ctx.beginPath();
      ctx.arc(px, py + Math.sin(this.t + i) * 3, r, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  drawGroundShadow(ctx, map) {
    const c = this.worldToScreen((map.width - 1) / 2, (map.height - 1) / 2);
    ctx.fillStyle = "rgba(10, 16, 12, 0.35)";
    ctx.beginPath();
    ctx.ellipse(c.x, c.y + map.height * 18, map.width * 52, map.height * 22, 0, 0, Math.PI * 2);
    ctx.fill();
  }

  drawDiamond(ctx, x, y, fill, stroke) {
    const hw = this.tileW / 2;
    const hh = this.tileH / 2;
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + hw, y + hh);
    ctx.lineTo(x, y + this.tileH);
    ctx.lineTo(x - hw, y + hh);
    ctx.closePath();
    ctx.fillStyle = fill;
    ctx.fill();
    if (stroke) {
      ctx.strokeStyle = stroke;
      ctx.lineWidth = 1;
      ctx.stroke();
    }
  }

  drawTile(ctx, map, x, y, player) {
    const p = this.worldToScreen(x, y);
    const soil = map.soil[y][x];
    const checker = (x + y) % 2 === 0;
    let fill = checker ? PALETTE.grassA : PALETTE.grassB;
    let stroke = PALETTE.grassShadow;

    if (soil === "tilled") {
      fill = PALETTE.tilled;
      stroke = PALETTE.tilledLine;
    }

    // side face for depth
    ctx.fillStyle = PALETTE.grassShadow;
    ctx.beginPath();
    ctx.moveTo(p.x - this.tileW / 2, p.y + this.tileH / 2);
    ctx.lineTo(p.x, p.y + this.tileH);
    ctx.lineTo(p.x, p.y + this.tileH + 10);
    ctx.lineTo(p.x - this.tileW / 2, p.y + this.tileH / 2 + 10);
    ctx.closePath();
    ctx.fill();

    ctx.fillStyle = "#243828";
    ctx.beginPath();
    ctx.moveTo(p.x + this.tileW / 2, p.y + this.tileH / 2);
    ctx.lineTo(p.x, p.y + this.tileH);
    ctx.lineTo(p.x, p.y + this.tileH + 10);
    ctx.lineTo(p.x + this.tileW / 2, p.y + this.tileH / 2 + 10);
    ctx.closePath();
    ctx.fill();

    this.drawDiamond(ctx, p.x, p.y, fill, stroke);

    if (soil === "tilled") {
      ctx.strokeStyle = PALETTE.soilDark;
      ctx.globalAlpha = 0.45;
      for (let i = 0; i < 3; i++) {
        const oy = 10 + i * 8;
        ctx.beginPath();
        ctx.moveTo(p.x - 28 + i * 2, p.y + oy);
        ctx.lineTo(p.x + 28 - i * 2, p.y + oy + 2);
        ctx.stroke();
      }
      ctx.globalAlpha = 1;
    } else {
      // grass blades highlight
      ctx.fillStyle = PALETTE.grassHi;
      ctx.globalAlpha = 0.35;
      ctx.fillRect(p.x - 6, p.y + 14, 2, 5);
      ctx.fillRect(p.x + 10, p.y + 18, 2, 4);
      ctx.fillRect(p.x - 18, p.y + 20, 2, 3);
      ctx.globalAlpha = 1;
    }

    const hover =
      this.hover && this.hover.x === x && this.hover.y === y;
    const selected = player.x === x && player.y === y;
    if (hover || selected) {
      const obs = map.getObstacle(x, y);
      let tint = "rgba(232, 220, 200, 0.22)";
      if (obs) {
        tint =
          player.level >= obs.requiredLevel
            ? PALETTE.unlockable
            : PALETTE.locked;
      }
      this.drawDiamond(ctx, p.x, p.y, tint, PALETTE.amber);
    }
  }

  drawObstacle(ctx, map, x, y, player) {
    const id = map.getObstacleId(x, y);
    if (!id) return;
    const p = this.worldToScreen(x, y);
    const cx = p.x;
    const cy = p.y + this.tileH * 0.35;
    const obs = map.getObstacle(x, y);
    const locked = player.level < obs.requiredLevel;

    switch (id) {
      case "weed":
        this.spriteWeed(ctx, cx, cy);
        break;
      case "bush":
        this.spriteBush(ctx, cx, cy);
        break;
      case "stump":
        this.spriteStump(ctx, cx, cy);
        break;
      case "tree":
        this.spriteTree(ctx, cx, cy);
        break;
      case "rock":
        this.spriteRock(ctx, cx, cy, false);
        break;
      case "boulder":
        this.spriteRock(ctx, cx, cy, true);
        break;
    }

    // level badge
    const badgeY = cy - (id === "tree" ? 58 : id === "boulder" ? 36 : 28);
    ctx.fillStyle = locked ? "#c0574f" : "#6f9b7a";
    ctx.beginPath();
    ctx.roundRect(cx - 14, badgeY - 8, 28, 14, 4);
    ctx.fill();
    ctx.fillStyle = PALETTE.pale;
    ctx.font = "bold 9px 'Segoe UI', sans-serif";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(`Lv${obs.requiredLevel}`, cx, badgeY - 1);

    if (locked) {
      ctx.fillStyle = "rgba(20, 12, 16, 0.28)";
      ctx.beginPath();
      ctx.ellipse(cx, cy + 8, 22, 10, 0, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  spriteWeed(ctx, cx, cy) {
    ctx.fillStyle = PALETTE.weed;
    for (let i = -1; i <= 1; i++) {
      ctx.beginPath();
      ctx.moveTo(cx + i * 8, cy + 6);
      ctx.quadraticCurveTo(cx + i * 10, cy - 6, cx + i * 4, cy - 14);
      ctx.quadraticCurveTo(cx + i * 6, cy - 2, cx + i * 8, cy + 6);
      ctx.fill();
    }
  }

  spriteBush(ctx, cx, cy) {
    ctx.fillStyle = PALETTE.bush;
    ctx.beginPath();
    ctx.ellipse(cx, cy - 4, 20, 14, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = PALETTE.leafHi;
    ctx.beginPath();
    ctx.ellipse(cx - 6, cy - 10, 10, 8, -0.3, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = PALETTE.thorn;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(cx + 8, cy - 2);
    ctx.lineTo(cx + 16, cy - 10);
    ctx.moveTo(cx - 10, cy);
    ctx.lineTo(cx - 18, cy - 8);
    ctx.stroke();
  }

  spriteStump(ctx, cx, cy) {
    ctx.fillStyle = PALETTE.wood;
    ctx.fillRect(cx - 12, cy - 10, 24, 16);
    ctx.fillStyle = PALETTE.woodHi;
    ctx.beginPath();
    ctx.ellipse(cx, cy - 10, 12, 5, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = PALETTE.soilDark;
    ctx.beginPath();
    ctx.ellipse(cx, cy - 10, 7, 3, 0, 0, Math.PI * 2);
    ctx.stroke();
  }

  spriteTree(ctx, cx, cy) {
    const sway = Math.sin(this.t * 1.4) * 1.5;
    ctx.fillStyle = PALETTE.wood;
    ctx.fillRect(cx - 5 + sway * 0.2, cy - 28, 10, 34);
    ctx.fillStyle = PALETTE.leafDark;
    ctx.beginPath();
    ctx.ellipse(cx + sway, cy - 42, 26, 22, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = PALETTE.leaf;
    ctx.beginPath();
    ctx.ellipse(cx - 8 + sway, cy - 48, 16, 14, -0.2, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = PALETTE.leafHi;
    ctx.beginPath();
    ctx.ellipse(cx + 6 + sway, cy - 52, 12, 10, 0.2, 0, Math.PI * 2);
    ctx.fill();
  }

  spriteRock(ctx, cx, cy, large) {
    const s = large ? 1.45 : 1;
    ctx.fillStyle = PALETTE.rockDark;
    ctx.beginPath();
    ctx.moveTo(cx - 16 * s, cy + 4);
    ctx.lineTo(cx - 10 * s, cy - 14 * s);
    ctx.lineTo(cx + 4 * s, cy - 20 * s);
    ctx.lineTo(cx + 18 * s, cy - 8 * s);
    ctx.lineTo(cx + 14 * s, cy + 6);
    ctx.closePath();
    ctx.fill();
    ctx.fillStyle = PALETTE.rock;
    ctx.beginPath();
    ctx.moveTo(cx - 14 * s, cy + 2);
    ctx.lineTo(cx - 8 * s, cy - 12 * s);
    ctx.lineTo(cx + 2 * s, cy - 16 * s);
    ctx.lineTo(cx + 12 * s, cy - 4 * s);
    ctx.lineTo(cx + 8 * s, cy + 4);
    ctx.closePath();
    ctx.fill();
    ctx.fillStyle = PALETTE.rockHi;
    ctx.beginPath();
    ctx.moveTo(cx - 6 * s, cy - 8 * s);
    ctx.lineTo(cx - 2 * s, cy - 14 * s);
    ctx.lineTo(cx + 4 * s, cy - 10 * s);
    ctx.closePath();
    ctx.fill();
  }

  drawPlayer(ctx, player) {
    const p = this.worldToScreen(player.x, player.y);
    const cx = p.x;
    const cy = p.y + 6;
    const bob = Math.sin(this.t * 6) * 1.2;

    ctx.fillStyle = "rgba(12, 16, 20, 0.35)";
    ctx.beginPath();
    ctx.ellipse(cx, cy + 18, 12, 5, 0, 0, Math.PI * 2);
    ctx.fill();

    // chibi body — BD2 field scale
    ctx.fillStyle = PALETTE.playerCloth;
    ctx.beginPath();
    ctx.roundRect(cx - 8, cy + bob, 16, 16, 4);
    ctx.fill();

    ctx.fillStyle = PALETTE.player;
    ctx.beginPath();
    ctx.arc(cx, cy - 6 + bob, 9, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = PALETTE.playerHair;
    ctx.beginPath();
    ctx.arc(cx, cy - 10 + bob, 9, Math.PI, 0);
    ctx.fill();

    ctx.fillStyle = "#1a1410";
    ctx.beginPath();
    ctx.arc(cx - 3, cy - 6 + bob, 1.4, 0, Math.PI * 2);
    ctx.arc(cx + 3, cy - 6 + bob, 1.4, 0, Math.PI * 2);
    ctx.fill();

    // selection ring
    ctx.strokeStyle = PALETTE.amber;
    ctx.lineWidth = 1.5;
    ctx.globalAlpha = 0.7 + Math.sin(this.t * 4) * 0.2;
    ctx.beginPath();
    ctx.ellipse(cx, cy + 18, 16, 7, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.globalAlpha = 1;
  }

  drawVignette(ctx) {
    const g = ctx.createRadialGradient(
      this.viewW / 2,
      this.viewH / 2,
      this.viewH * 0.25,
      this.viewW / 2,
      this.viewH / 2,
      this.viewH * 0.85
    );
    g.addColorStop(0, "rgba(0,0,0,0)");
    g.addColorStop(1, "rgba(8, 12, 18, 0.55)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, this.viewW, this.viewH);

    ctx.fillStyle = PALETTE.haze;
    ctx.fillRect(0, 0, this.viewW, this.viewH * 0.5);
  }

  drawFlash(ctx) {
    ctx.fillStyle = `rgba(232, 220, 200, ${this.flash.a})`;
    ctx.fillRect(0, 0, this.viewW, this.viewH);
    this.flash.a -= 0.04;
    if (this.flash.a <= 0) this.flash = null;
  }

  pulse() {
    this.flash = { a: 0.35 };
  }
}
