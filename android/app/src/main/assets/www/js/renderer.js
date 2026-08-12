/**
 * Brown Dust 2 HD-2D farm renderer
 * Art direction from: art-refs/bd2-victory-grid, bd2-hd2d-field, bd2-sunset-cliff
 * — painted terrain, tactical grid, god rays, tilt-shift, dense obstacles
 */

const P = {
  // sunset / golden hour (cliff ref)
  skyDeep: "#1a1530",
  skyMid: "#8a3a28",
  skyHot: "#e87830",
  skyGlow: "#ffc878",
  sea: "#3a4a68",
  // grass / soil (field + victory refs)
  grassDeep: "#1e3a28",
  grassMid: "#2f5a38",
  grassLit: "#4a7a48",
  grassHi: "#6a9a58",
  soil: "#5a3a28",
  soilLit: "#7a5238",
  tilled: "#6b442c",
  path: "#6a6258",
  pathLit: "#8a8278",
  // foliage
  leafDark: "#163820",
  leaf: "#2a5a30",
  leafHi: "#4a8a40",
  autumn: "#c87828",
  autumnHi: "#e8a038",
  pine: "#1a4030",
  pineHi: "#2a5840",
  wood: "#4a3020",
  woodHi: "#7a5030",
  // rock
  rock: "#5a5858",
  rockLit: "#8a8888",
  rockDark: "#2e2c2c",
  rockWarm: "#6a5040",
  // ui / fx
  amber: "#e8b060",
  pale: "#f0e6d4",
  grid: "rgba(255,255,255,0.22)",
  spot: "rgba(255,210,140,0.18)",
  ink: "#0a0c10",
  playerSkin: "#e0b090",
  playerCloth: "#3a5a88",
  playerHair: "#2a1810",
};

function iso(x, y, tw, th) {
  return { sx: (x - y) * (tw / 2), sy: (x + y) * (th / 2) };
}

function hash(n) {
  const x = Math.sin(n * 127.1) * 43758.5453;
  return x - Math.floor(x);
}

export class FarmRenderer {
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext("2d");
    this.tileW = 96;
    this.tileH = 48;
    this.hover = null;
    this.flash = null;
    this.t = 0;
    this.fx = [];
    this.viewW = 0;
    this.viewH = 0;
  }

  spawnFx(x, y, text, color) {
    this.fx.push({ x, y, text, color, life: 1 });
  }

  resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2.5);
    const w = this.canvas.clientWidth || window.innerWidth;
    const h = this.canvas.clientHeight || window.innerHeight;
    this.canvas.width = Math.floor(w * dpr);
    this.canvas.height = Math.floor(h * dpr);
    this.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    this.viewW = w;
    this.viewH = h;
  }

  layout(map) {
    const spanY = (map.width + map.height) * (this.tileH / 2);
    const scale = Math.min(1.15, Math.max(0.75, this.viewW / 520));
    this.tileW = Math.round(96 * scale);
    this.tileH = Math.round(48 * scale);
    this.originX = Math.round(this.viewW / 2);
    this.originY = Math.round(this.viewH * 0.42 - spanY / 2);
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

  draw(map, player) {
    this.t += 0.016;
    this.resize();
    this.layout(map);
    const ctx = this.ctx;

    this.drawSky(ctx);
    this.drawDistantBand(ctx);
    this.drawForestFrame(ctx, map);
    this.drawGroundBloom(ctx, map);

    const order = [];
    for (let y = 0; y < map.height; y++)
      for (let x = 0; x < map.width; x++) order.push({ x, y });
    order.sort((a, b) => a.x + a.y - (b.x + b.y));

    for (const c of order) this.drawTile(ctx, map, c.x, c.y, player);
    for (const c of order) {
      this.drawObstacle(ctx, map, c.x, c.y, player);
      if (player.x === c.x && player.y === c.y) this.drawPlayer(ctx, player);
    }

    this.drawGodRays(ctx);
    this.drawParticles(ctx);
    this.drawTiltShift(ctx);
    this.drawVignette(ctx);
    this.drawFx(ctx);
    if (this.flash) {
      ctx.fillStyle = `rgba(255, 230, 180, ${this.flash.a})`;
      ctx.fillRect(0, 0, this.viewW, this.viewH);
      this.flash.a -= 0.045;
      if (this.flash.a <= 0) this.flash = null;
    }
  }

  drawSky(ctx) {
    const g = ctx.createLinearGradient(0, 0, 0, this.viewH);
    g.addColorStop(0, P.skyDeep);
    g.addColorStop(0.28, "#4a2848");
    g.addColorStop(0.48, P.skyMid);
    g.addColorStop(0.62, P.skyHot);
    g.addColorStop(0.78, "#3a4030");
    g.addColorStop(1, "#121810");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, this.viewW, this.viewH);

    // sun disc
    const sx = this.viewW * 0.78;
    const sy = this.viewH * 0.22;
    const sun = ctx.createRadialGradient(sx, sy, 4, sx, sy, 120);
    sun.addColorStop(0, "rgba(255,240,180,0.95)");
    sun.addColorStop(0.25, "rgba(255,180,80,0.55)");
    sun.addColorStop(1, "rgba(255,120,40,0)");
    ctx.fillStyle = sun;
    ctx.fillRect(0, 0, this.viewW, this.viewH);

    // soft clouds
    ctx.fillStyle = "rgba(40, 24, 48, 0.35)";
    for (let i = 0; i < 5; i++) {
      const cx = ((i * 180 + this.t * 4) % (this.viewW + 200)) - 100;
      const cy = 30 + i * 18;
      ctx.beginPath();
      ctx.ellipse(cx, cy, 90 + i * 10, 18, 0, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  drawDistantBand(ctx) {
    // far treeline silhouette (hd2d field / cliff refs)
    const baseY = this.viewH * 0.36;
    ctx.fillStyle = "#152418";
    ctx.beginPath();
    ctx.moveTo(0, baseY + 40);
    for (let i = 0; i <= 24; i++) {
      const x = (i / 24) * this.viewW;
      const h = 28 + hash(i * 3.1) * 55;
      ctx.lineTo(x, baseY - h);
    }
    ctx.lineTo(this.viewW, baseY + 60);
    ctx.closePath();
    ctx.fill();

    // water / haze strip
    const wg = ctx.createLinearGradient(0, baseY + 20, 0, baseY + 90);
    wg.addColorStop(0, "rgba(58,74,104,0.35)");
    wg.addColorStop(1, "rgba(58,74,104,0)");
    ctx.fillStyle = wg;
    ctx.fillRect(0, baseY + 10, this.viewW, 90);
  }

  drawForestFrame(ctx, map) {
    // dense dark foliage framing playable grid (victory ref)
    const c = this.worldToScreen((map.width - 1) / 2, (map.height - 1) / 2);
    for (let i = 0; i < 14; i++) {
      const ang = (i / 14) * Math.PI * 2 + 0.2;
      const dist = 150 + hash(i) * 40;
      const x = c.x + Math.cos(ang) * dist * 1.35;
      const y = c.y + Math.sin(ang) * dist * 0.55 + 20;
      this.paintBush(ctx, x, y, 0.9 + hash(i + 2) * 0.5, true);
    }
  }

  drawGroundBloom(ctx, map) {
    const c = this.worldToScreen((map.width - 1) / 2, (map.height - 1) / 2);
    const spot = ctx.createRadialGradient(c.x, c.y + 20, 20, c.x, c.y + 20, 220);
    spot.addColorStop(0, P.spot);
    spot.addColorStop(0.55, "rgba(255,180,100,0.06)");
    spot.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = spot;
    ctx.fillRect(0, 0, this.viewW, this.viewH);

    ctx.fillStyle = "rgba(8,10,12,0.45)";
    ctx.beginPath();
    ctx.ellipse(c.x, c.y + map.height * 22, map.width * 58, map.height * 26, 0, 0, Math.PI * 2);
    ctx.fill();
  }

  diamond(ctx, x, y, fill, stroke) {
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
    const n = hash(x * 12.3 + y * 7.7);

    // extrusion / terrace edge
    ctx.fillStyle = "#142018";
    ctx.beginPath();
    ctx.moveTo(p.x - this.tileW / 2, p.y + this.tileH / 2);
    ctx.lineTo(p.x, p.y + this.tileH);
    ctx.lineTo(p.x, p.y + this.tileH + 12);
    ctx.lineTo(p.x - this.tileW / 2, p.y + this.tileH / 2 + 12);
    ctx.closePath();
    ctx.fill();
    ctx.fillStyle = "#1c2a20";
    ctx.beginPath();
    ctx.moveTo(p.x + this.tileW / 2, p.y + this.tileH / 2);
    ctx.lineTo(p.x, p.y + this.tileH);
    ctx.lineTo(p.x, p.y + this.tileH + 12);
    ctx.lineTo(p.x + this.tileW / 2, p.y + this.tileH / 2 + 12);
    ctx.closePath();
    ctx.fill();

    let fill = n > 0.5 ? P.grassMid : P.grassDeep;
    if (soil === "tilled") fill = P.tilled;
    this.diamond(ctx, p.x, p.y, fill, "rgba(0,0,0,0.25)");

    // painted grass texture / flowers
    if (soil !== "tilled") {
      ctx.save();
      this.clipDiamond(ctx, p.x, p.y);
      for (let i = 0; i < 10; i++) {
        const ox = (hash(x + i * 1.7) - 0.5) * this.tileW * 0.7;
        const oy = 8 + hash(y + i * 2.3) * (this.tileH - 14);
        ctx.fillStyle = hash(i + x) > 0.55 ? P.grassHi : P.grassLit;
        ctx.globalAlpha = 0.45;
        ctx.fillRect(p.x + ox, p.y + oy, 2, 4 + hash(i) * 3);
        if (hash(i + 9) > 0.82) {
          ctx.globalAlpha = 0.9;
          ctx.fillStyle = "#f0f0e0";
          ctx.beginPath();
          ctx.arc(p.x + ox, p.y + oy - 1, 1.2, 0, Math.PI * 2);
          ctx.fill();
        }
      }
      ctx.restore();
      ctx.globalAlpha = 1;
    } else {
      ctx.strokeStyle = "rgba(40,24,16,0.45)";
      for (let i = 0; i < 4; i++) {
        ctx.beginPath();
        ctx.moveTo(p.x - 30 + i, p.y + 10 + i * 7);
        ctx.lineTo(p.x + 30 - i, p.y + 12 + i * 7);
        ctx.stroke();
      }
    }

    // BD2 tactical grid overlay
    ctx.strokeStyle = P.grid;
    ctx.lineWidth = 1;
    this.diamond(ctx, p.x, p.y, "rgba(0,0,0,0)", P.grid);

    // highlight rim on sun side
    ctx.strokeStyle = "rgba(255,200,120,0.18)";
    ctx.beginPath();
    ctx.moveTo(p.x, p.y);
    ctx.lineTo(p.x + this.tileW / 2, p.y + this.tileH / 2);
    ctx.stroke();

    const hover = this.hover && this.hover.x === x && this.hover.y === y;
    const selected = player.x === x && player.y === y;
    if (hover || selected) {
      const obs = map.getObstacle(x, y);
      let tint = "rgba(255,230,180,0.2)";
      if (obs) {
        tint =
          player.level >= obs.requiredLevel
            ? "rgba(90,160,100,0.32)"
            : "rgba(180,60,60,0.32)";
      }
      this.diamond(ctx, p.x, p.y, tint, P.amber);
    }
  }

  clipDiamond(ctx, x, y) {
    const hw = this.tileW / 2;
    const hh = this.tileH / 2;
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + hw, y + hh);
    ctx.lineTo(x, y + this.tileH);
    ctx.lineTo(x - hw, y + hh);
    ctx.closePath();
    ctx.clip();
  }

  drawObstacle(ctx, map, x, y, player) {
    const id = map.getObstacleId(x, y);
    if (!id) return;
    const p = this.worldToScreen(x, y);
    const cx = p.x;
    const cy = p.y + this.tileH * 0.32;
    const obs = map.getObstacle(x, y);
    const locked = player.level < obs.requiredLevel;

    // contact shadow
    ctx.fillStyle = "rgba(0,0,0,0.35)";
    ctx.beginPath();
    ctx.ellipse(cx, cy + 10, id === "boulder" || id === "tree" ? 22 : 14, 7, 0, 0, Math.PI * 2);
    ctx.fill();

    switch (id) {
      case "weed":
        this.spriteWeed(ctx, cx, cy);
        break;
      case "bush":
        this.paintBush(ctx, cx, cy - 4, 0.85, false);
        break;
      case "stump":
        this.spriteStump(ctx, cx, cy);
        break;
      case "tree":
        this.spriteTree(ctx, cx, cy);
        break;
      case "rock":
        this.spriteRock(ctx, cx, cy, 1);
        break;
      case "boulder":
        this.spriteRock(ctx, cx, cy, 1.55);
        break;
    }

    // level plate like BD2 UI chips
    const badgeY = cy - (id === "tree" ? 62 : id === "boulder" ? 40 : 30);
    ctx.fillStyle = "rgba(12,14,18,0.72)";
    ctx.beginPath();
    ctx.roundRect(cx - 18, badgeY - 9, 36, 16, 4);
    ctx.fill();
    ctx.strokeStyle = locked ? "#c0574f" : "#6f9b7a";
    ctx.lineWidth = 1;
    ctx.stroke();
    ctx.fillStyle = locked ? "#ff8a80" : "#b6e0c0";
    ctx.font = "bold 10px 'Segoe UI', sans-serif";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(`Lv.${obs.requiredLevel}`, cx, badgeY - 1);
  }

  spriteWeed(ctx, cx, cy) {
    for (let i = -2; i <= 2; i++) {
      ctx.strokeStyle = i % 2 ? P.grassHi : P.leaf;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(cx + i * 5, cy + 8);
      ctx.quadraticCurveTo(cx + i * 7, cy - 4, cx + i * 3, cy - 16 + Math.abs(i));
      ctx.stroke();
    }
  }

  paintBush(ctx, cx, cy, s, dark) {
    const clusters = [
      [0, 0, 18],
      [-12, 2, 14],
      [12, 3, 13],
      [-4, -10, 12],
      [6, -8, 11],
    ];
    for (const [ox, oy, r] of clusters) {
      ctx.fillStyle = dark ? "#0e1c14" : P.leafDark;
      ctx.beginPath();
      ctx.ellipse(cx + ox * s, cy + oy * s, r * s, r * 0.75 * s, 0, 0, Math.PI * 2);
      ctx.fill();
    }
    if (!dark) {
      ctx.fillStyle = P.leafHi;
      ctx.globalAlpha = 0.55;
      ctx.beginPath();
      ctx.ellipse(cx - 6 * s, cy - 10 * s, 8 * s, 6 * s, -0.3, 0, Math.PI * 2);
      ctx.fill();
      ctx.globalAlpha = 1;
      ctx.strokeStyle = P.woodHi;
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(cx + 8 * s, cy);
      ctx.lineTo(cx + 16 * s, cy - 8 * s);
      ctx.stroke();
    }
  }

  spriteStump(ctx, cx, cy) {
    ctx.fillStyle = P.wood;
    ctx.fillRect(cx - 14, cy - 8, 28, 16);
    ctx.fillStyle = P.woodHi;
    ctx.beginPath();
    ctx.ellipse(cx, cy - 8, 14, 6, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = "#2a1810";
    ctx.beginPath();
    ctx.ellipse(cx, cy - 8, 8, 3.5, 0, 0, Math.PI * 2);
    ctx.stroke();
    // moss
    ctx.fillStyle = P.leaf;
    ctx.globalAlpha = 0.6;
    ctx.beginPath();
    ctx.ellipse(cx + 6, cy - 6, 5, 3, 0.4, 0, Math.PI * 2);
    ctx.fill();
    ctx.globalAlpha = 1;
  }

  spriteTree(ctx, cx, cy) {
    const sway = Math.sin(this.t * 1.2) * 2;
    // trunk
    ctx.fillStyle = P.wood;
    ctx.beginPath();
    ctx.moveTo(cx - 6, cy + 8);
    ctx.lineTo(cx - 4 + sway * 0.2, cy - 34);
    ctx.lineTo(cx + 5 + sway * 0.2, cy - 34);
    ctx.lineTo(cx + 7, cy + 8);
    ctx.fill();
    // canopy — autumn/gold accent like field ref + green body
    const layers = [
      [0, -48, 28, 22, P.leafDark],
      [-8 + sway, -54, 18, 14, P.leaf],
      [10 + sway, -56, 16, 13, P.autumn],
      [2 + sway, -62, 12, 10, P.autumnHi],
      [-4 + sway, -58, 10, 8, P.leafHi],
    ];
    for (const [ox, oy, rx, ry, col] of layers) {
      ctx.fillStyle = col;
      ctx.beginPath();
      ctx.ellipse(cx + ox, cy + oy, rx, ry, 0, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  spriteRock(ctx, cx, cy, s) {
    // warm-toned boulder cluster (sunset cliff ref)
    const polys = [
      [
        [-16, 4],
        [-12, -16],
        [2, -22],
        [16, -8],
        [12, 6],
      ],
      [
        [-6, 2],
        [-2, -12],
        [10, -10],
        [8, 4],
      ],
    ];
    for (let pi = 0; pi < polys.length; pi++) {
      ctx.fillStyle = pi === 0 ? P.rockDark : P.rockWarm;
      ctx.beginPath();
      polys[pi].forEach(([x, y], i) => {
        const px = cx + x * s;
        const py = cy + y * s;
        if (i === 0) ctx.moveTo(px, py);
        else ctx.lineTo(px, py);
      });
      ctx.closePath();
      ctx.fill();
    }
    ctx.fillStyle = P.rockLit;
    ctx.beginPath();
    ctx.moveTo(cx - 4 * s, cy - 10 * s);
    ctx.lineTo(cx + 2 * s, cy - 18 * s);
    ctx.lineTo(cx + 8 * s, cy - 8 * s);
    ctx.closePath();
    ctx.fill();
  }

  drawPlayer(ctx, player) {
    const p = this.worldToScreen(player.x, player.y);
    const cx = p.x;
    const cy = p.y + 4;
    const bob = Math.sin(this.t * 5.5) * 1.4;

    ctx.fillStyle = "rgba(0,0,0,0.4)";
    ctx.beginPath();
    ctx.ellipse(cx, cy + 20, 12, 5, 0, 0, Math.PI * 2);
    ctx.fill();

    // refined chibi — BD2 field scale
    ctx.fillStyle = P.playerCloth;
    ctx.beginPath();
    ctx.roundRect(cx - 9, cy + 1 + bob, 18, 17, 4);
    ctx.fill();
    // cape accent
    ctx.fillStyle = "#c04040";
    ctx.fillRect(cx + 6, cy + 2 + bob, 4, 12);

    ctx.fillStyle = P.playerSkin;
    ctx.beginPath();
    ctx.arc(cx, cy - 7 + bob, 10, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = P.playerHair;
    ctx.beginPath();
    ctx.arc(cx, cy - 11 + bob, 10, Math.PI * 1.05, Math.PI * 1.95);
    ctx.fill();
    ctx.fillStyle = "#1a1010";
    ctx.beginPath();
    ctx.arc(cx - 3.5, cy - 7 + bob, 1.5, 0, Math.PI * 2);
    ctx.arc(cx + 3.5, cy - 7 + bob, 1.5, 0, Math.PI * 2);
    ctx.fill();

    ctx.strokeStyle = P.amber;
    ctx.globalAlpha = 0.55 + Math.sin(this.t * 4) * 0.25;
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.ellipse(cx, cy + 20, 18, 7, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.globalAlpha = 1;
  }

  drawGodRays(ctx) {
    const sx = this.viewW * 0.78;
    const sy = this.viewH * 0.18;
    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < 6; i++) {
      const a = -0.9 + i * 0.12 + Math.sin(this.t * 0.3 + i) * 0.02;
      ctx.fillStyle = `rgba(255, 190, 110, ${0.035 + (i % 2) * 0.02})`;
      ctx.beginPath();
      ctx.moveTo(sx, sy);
      ctx.lineTo(sx + Math.cos(a) * this.viewH * 1.4, sy + Math.sin(a) * this.viewH * 1.4);
      ctx.lineTo(
        sx + Math.cos(a + 0.06) * this.viewH * 1.4,
        sy + Math.sin(a + 0.06) * this.viewH * 1.4
      );
      ctx.closePath();
      ctx.fill();
    }
    ctx.restore();
  }

  drawParticles(ctx) {
    ctx.fillStyle = "rgba(255,230,180,0.5)";
    for (let i = 0; i < 22; i++) {
      const x = ((i * 89 + this.t * (8 + (i % 5))) % (this.viewW + 30)) - 15;
      const y = 40 + hash(i * 4) * this.viewH * 0.55 + Math.sin(this.t + i) * 6;
      const r = 1 + (i % 3) * 0.7;
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  drawTiltShift(ctx) {
    // miniature DOF — blur bands top/bottom (hd2d field ref)
    const top = ctx.createLinearGradient(0, 0, 0, this.viewH * 0.28);
    top.addColorStop(0, "rgba(20,16,24,0.55)");
    top.addColorStop(0.6, "rgba(20,16,24,0.2)");
    top.addColorStop(1, "rgba(20,16,24,0)");
    ctx.fillStyle = top;
    ctx.fillRect(0, 0, this.viewW, this.viewH * 0.28);

    const bot = ctx.createLinearGradient(0, this.viewH * 0.72, 0, this.viewH);
    bot.addColorStop(0, "rgba(10,12,10,0)");
    bot.addColorStop(0.4, "rgba(10,12,10,0.25)");
    bot.addColorStop(1, "rgba(10,12,10,0.65)");
    ctx.fillStyle = bot;
    ctx.fillRect(0, this.viewH * 0.72, this.viewW, this.viewH * 0.28);
  }

  drawVignette(ctx) {
    const g = ctx.createRadialGradient(
      this.viewW / 2,
      this.viewH / 2,
      this.viewH * 0.2,
      this.viewW / 2,
      this.viewH / 2,
      this.viewH * 0.85
    );
    g.addColorStop(0, "rgba(0,0,0,0)");
    g.addColorStop(1, "rgba(6,8,12,0.62)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, this.viewW, this.viewH);
  }

  drawFx(ctx) {
    for (let i = this.fx.length - 1; i >= 0; i--) {
      const f = this.fx[i];
      const p = this.worldToScreen(f.x, f.y);
      ctx.globalAlpha = f.life;
      ctx.fillStyle = f.color;
      ctx.font = "bold 14px 'Segoe UI', sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(f.text, p.x, p.y - 30 - (1 - f.life) * 28);
      ctx.globalAlpha = 1;
      f.life -= 0.02;
      if (f.life <= 0) this.fx.splice(i, 1);
    }
  }

  pulse() {
    this.flash = { a: 0.28 };
  }
}
