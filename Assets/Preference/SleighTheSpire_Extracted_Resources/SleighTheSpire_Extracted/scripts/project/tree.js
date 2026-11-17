import { E, W, H, FH, PAL, MUTE, Scene, tween, popOverlay, clamp, damp, burst, wrap, hasType } from "./engine.js";
import { sfx } from "./audio.js";
import { icon, bgimg } from "./art.js";
import { RELICS } from "./data.js";
import { G, RUN } from "./run.js";
import { panel } from "./ui.js";

const TX = 230;
// tuned to the tree image, move these if the art changes
const HOOKS = [[TX, 70], [TX - 36, 118], [TX + 36, 118], [TX - 62, 172], [TX + 62, 172], [TX - 88, 226], [TX + 88, 226]];
const BOX = { x: 24, y: 290, w: 400, h: 56 };
const STR = 8;

export class TreeOverlay extends Scene {
	constructor(readOnly = false) { super(); this.readOnly = readOnly; this.orn = []; this.drag = null; }
	onBack() { if (this.drag) return; popOverlay(this); }
	enter() {
		const bg = this.g("Pop", 0, 0); bg.r(0, 0, W, H, PAL.black, 0.86); bg.noPop = true;
		this.drawTree();
		this.lines = this.g("Pop", 0, 0);
		const hk = this.g("Pop", 0, 0);
		const node = hasType("TreeNode");
		if (node) HOOKS.forEach(([x, y]) => hk.sp("TreeNode", x, y));
		const bx = this.g("Pop", 0, 0);
		panel(bx, BOX.x, BOX.y, BOX.w, BOX.h, 0x6a4a2a);
		bx.t(BOX.x + 8, BOX.y + 4, "ORNAMENT BOX (not active)", { col: MUTE() });
		this.info = this.g("Pop", 0, 0);
		this.buildInfo();
		for (const r of G.relics) {
			const hook = G.tree.indexOf(r.id);
			const o = { id: r.id, hook, ang: 0, angV: 0, ph: Math.random() * 6 };
			o.g = this.g("Pop", 0, 0);
			o.g.r(-0.5, 0, 1, STR, 0xd8d0b0);
			const k = icon(o.g, "relic_" + r.id, 0, 0);
			o.h = k ? k.h : 13; o.w = k ? k.w : 13;
			if (k) k.y = STR + o.h / 2;
			this.orn.push(o);
		}
		this.layoutBox(true);
		for (const o of this.orn) if (o.hook >= 0) { const [x, y] = HOOKS[o.hook]; o.g.x = x; o.g.y = y - 60; o.g.a = 0; tween(o.g, { y, a: 1 }, 0.4, "outBack", Math.random() * 0.2); }
		this.hot({ x: 0, y: 0, w: W, h: H, sfx: false });
		this.btn({ x: W - 150, y: H - 34, w: 132, h: 22, layer: "Pop", label: "DONE", col: PAL.green2, key: "Escape", onClick: () => this.onBack() });
		this.drawLines();
		this.popIn(W / 2, H / 2);
	}
	drawTree() {
		bgimg(this.g("Pop", 0, 0), "tree", TX, 282);
	}
	buildInfo() {
		const g = this.info; g.clear();
		const x = 440, y = 24;
		panel(g, x, y, 186, 254, PAL.gold2);
		g.t(x + 93, y + 6, "ORNAMENT TREE", { al: "center", col: PAL.gold, sh: true });
		const rows = [
			{ s: "Only ornaments on the tree work.", c: PAL.white },
			{ s: "", c: 0 },
			{ s: "STAR: the ornament at the top works twice.", c: PAL.gold },
			{ s: "", c: 0 },
			{ s: "Drag ornaments between hooks, or into the box to switch them off.", c: MUTE() },
		];
		let yy = y + 26;
		for (const r of rows) { if (!r.s) { yy += 4; continue; } for (const l of wrap(r.s, 17).split("\n")) { g.t(x + 10, yy, l, { col: r.c }); yy += FH; } yy += 2; }
		if (this.readOnly) g.t(x + 93, y + 234, "Rearrange on the map.", { al: "center", col: PAL.red });
	}
	drawLines() {}
	layoutBox(snap = false) {
		let i = 0;
		for (const o of this.orn) if (o.hook < 0) {
			o.bx = BOX.x + 20 + i * 28; o.by = BOX.y + 18; i++;
			if (snap) { o.g.x = o.bx; o.g.y = o.by; }
		}
	}
	ornAt(x, y) {
		for (let i = this.orn.length - 1; i >= 0; i--) {
			const o = this.orn[i], gx = o.g.x, gy = o.g.y;
			if (Math.abs(x - gx) <= Math.max(8, o.w / 2 + 2) && y >= gy - 2 && y <= gy + STR + o.h + 2) return o;
		}
		return null;
	}
	place(o, hook) {
		const from = o.hook;
		if (hook >= 0) {
			const other = this.orn.find(q => q !== o && q.hook === hook);
			if (other) { other.hook = from; other.angV += 6; }
			o.hook = hook;
		} else o.hook = -1;
		G.tree.fill(null);
		for (const q of this.orn) if (q.hook >= 0) {
			G.tree[q.hook] = q.id;
			const inst = G.relics.find(r => r.id === q.id);
			// first time a boxed ornament gets hung, run its pickup (fruitcake etc)
			if (inst && inst.hook < 0 && !inst.picked) { inst.picked = true; RELICS[q.id].pickup?.(RUN, inst); }
			if (inst) inst.hook = q.hook;
		}
		G.changed = true;
		this.layoutBox();
		this.drawLines(); this.buildInfo();
	}
	tick(dt, active) {
		const m = E.mouse;
		if (active && !this.readOnly) {
			if (m.pressed && !this.drag) {
				const o = this.ornAt(m.x, m.y);
				if (o) { this.drag = o; o.lastX = m.x; o.g.top(); sfx.grab(); o.g.s = 1.25; }
			}
			if (this.drag && !m.down) {
				const o = this.drag; this.drag = null;
				let best = -1, bd = 26;
				HOOKS.forEach(([x, y], i) => { const d = Math.hypot(m.x - x, m.y - y); if (d < bd) { bd = d; best = i; } });
				this.place(o, best);
				sfx.clink(); burst(o.g.x, o.g.y + STR + o.h / 2, 10, { cols: [PAL.gold, 0xffffff], spd: [30, 90], g: 60, life: 0.4 });
				o.angV += (m.vx || 0) * -0.01;
			}
		}
		for (const o of this.orn) {
			let tx, ty;
			if (o === this.drag) { tx = m.x; ty = m.y - 4; }
			else if (o.hook >= 0) [tx, ty] = HOOKS[o.hook];
			else { tx = o.bx; ty = o.by; }
			const px = o.g.x;
			if (o === this.drag) { o.g.x = tx; o.g.y = ty; }
			else if (!E.tweens.some(t => t.o === o.g && !t.dead)) { o.g.x = damp(o.g.x, tx, 16, dt); o.g.y = damp(o.g.y, ty, 16, dt); }
			// fake pendulum: the string swings against how hard the hook accelerates
			const vx = (o.g.x - px) / Math.max(dt, 1 / 120), acc = (vx - (o.vx || 0)) / Math.max(dt, 1 / 120); o.vx = vx;
			const idle = E.settings.reduceMotion ? 0 : Math.sin(this.t * 1.6 + o.ph) * 0.05;
			o.angV += (-(o.ang - idle) * 70 - o.angV * 5 - clamp(acc, -4000, 4000) * 0.0009) * dt;
			o.ang = clamp(o.ang + o.angV * dt, -1.2, 1.2);
			o.g.rot = -o.ang;
			o.g.s = damp(o.g.s, o === this.drag ? 1.2 : 1, 14, dt);
		}
		if (active && !this.drag) {
			const o = this.ornAt(m.x, m.y);
			if (o) {
				const d = RELICS[o.id];
				const note = o.hook < 0 ? { s: "In the box: not active.", c: PAL.red } : o.hook === 0 ? { s: "On the STAR: works twice.", c: PAL.gold } : null;
				E.tipOverride = { t: d.name, c: PAL.gold, b: note ? [d.desc, note] : d.desc };
			}
		}
	}
}