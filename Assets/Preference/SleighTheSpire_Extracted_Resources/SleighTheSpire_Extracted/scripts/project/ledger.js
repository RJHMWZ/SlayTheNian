import { E, W, H, FH, PAL, MUTE, Scene, popOverlay, clamp, damp, wrap, fitText, tween, hasType } from "./engine.js";
import { sfx } from "./audio.js";
import { creature } from "./art.js";
import { ENEMIES } from "./enemies.js";
import { ledger, likeLabel } from "./parts.js";
import { panel, makeCard } from "./ui.js";
import { fid } from "./friends.js";
import { GIFTS } from "./data.js";

const ORDER = ["weak", "normal", "elite", "boss"];
const TIER_NAME = { weak: "Weak", normal: "Monster", elite: "Elite", boss: "Boss" };

export class LedgerOverlay extends Scene {
	constructor() { super(); this.sel = 0; this.scroll = 0; this.target = 0; }
	onBack() { popOverlay(this); }
	enter() {
		const L = ledger();
		this.list = Object.values(ENEMIES).filter(d => d.id !== "naughtylist" || L.seen.naughtylist)
			.sort((a, b) => ORDER.indexOf(a.tier) - ORDER.indexOf(b.tier));
		const bg = this.g("Pop", 0, 0); bg.r(0, 0, W, H, PAL.black, 0.8); bg.noPop = true;
		const px = 30, py = 18, pw = W - 60, ph = H - 36;
		this.box = { px, py, pw, ph };
		const p = this.g("Pop", 0, 0);
		if (hasType("EnemyList")) p.np("EnemyList", px - 4, py - 4, pw + 8, ph + 8);
		else panel(p, px, py, pw, ph);
		const seen = this.list.filter(d => L.seen[d.id]).length;
		p.t(W / 2, py + 6, `NICE & NAUGHTY LEDGER   ${seen}/${this.list.length}`, { al: "center", col: PAL.gold, sh: true });
		p.r(px + 10, py + 22, pw - 20, 2, PAL.gold2);
		p.r(px + 200, py + 28, 2, ph - 66, PAL.panel2);
		this.LY = py + 30; this.LH = ph - 70; this.rowH = 16;
		// rows outside the window just get hidden, no clipping needed
		this.rows = this.list.map((d, i) => {
			const g = this.g("Pop", px + 12, 0), known = !!L.seen[d.id];
			g.hl = g.r(-4, -2, 186, this.rowH, PAL.gold2, 0);
			g.t(0, 0, known ? fitText(d.name, 150) : "???", { col: known ? PAL.white : MUTE() });
			if (L.charms[d.id]) g.t(168, 0, "<3", { col: 0x7be08f });
			const h = this.hot({ x: px + 8, y: 0, w: 188, h: this.rowH, onClick: () => { this.pick(i); sfx.tick(); } });
			return { g, h, d };
		});
		this.max = Math.max(0, this.list.length * this.rowH - this.LH);
		this.detail = this.g("Pop", 0, 0);
		this.art = null;
		this.btn({ x: px + pw - 126, y: py + ph - 30, w: 112, h: 20, layer: "Pop", label: "BACK", key: "Escape", onClick: () => this.onBack() });
		this.pick(0);
		this.popIn(W / 2, H / 2);
	}
	pick(i) {
		this.sel = clamp(i, 0, this.list.length - 1);
		const d = this.list[this.sel], L = ledger(), known = !!L.seen[d.id], { px, py, pw } = this.box;
		const x0 = px + 216, g = this.detail; g.clear();
		this.art?.kill();
		const a = this.art = this.g("Pop", x0 + 70, py + 150);
		const dims = creature(a, "Enemy", d.id);
		const sc = clamp(90 / Math.max(dims.h, 1), 1, 2.4); a.s = sc;
		// dim all the way down = black silhouette for the ones you haven't met
		if (!known) a.dim = 0.03;
		a.oy = 8; a.a = 0; tween(a, { oy: 0, a: 1 }, 0.25, "outBack");
		this.card?.kill(); this.card = null;
		const friend = known && L.charms[d.id] && GIFTS[fid(d.id)];
		if (friend) { const c = this.card = makeCard(this, { id: fid(d.id), up: false, uid: -1 }, px + pw - 50, py + 220, "Pop"); c.a = 0; tween(c, { a: 1 }, 0.2); }
		const tx = x0 + 150, w = pw - 350;
		g.t(tx, py + 34, known ? d.name : "???", { col: known ? PAL.gold : MUTE(), sh: true });
		g.t(tx, py + 48, known ? `${TIER_NAME[d.tier]} - HP ${d.hp[0]}${d.hp[1] !== d.hp[0] ? "-" + d.hp[1] : ""}` : TIER_NAME[d.tier], { col: MUTE() });
		let y = py + 70;
		const line = (s, c) => { for (const l of wrap(s, Math.floor(w / 10)).split("\n")) { g.t(tx, y, l, { col: c }); y += FH; } y += 4; };
		if (d.noCharm) line("Can't be charmed.", 0xff9090);
		else if (d.moods) line("Changes what he loves and hates every round. Watch what he asks for!", PAL.pink);
		else {
			line("Loves: " + (L.love[d.id] ? likeLabel(d.love) : "???"), L.love[d.id] ? PAL.pink : MUTE());
			line("Hates: " + (L.hate[d.id] ? likeLabel(d.hate) : "???"), L.hate[d.id] ? 0xff9090 : MUTE());
		}
		y += 4;
		line(known ? d.lore || "" : "You haven't met this one yet.", known ? PAL.cream : MUTE());
		if (known) line(`Defeated: ${L.kills[d.id] || 0}    Won over: ${L.charms[d.id] || 0}`, MUTE());
		if (known && !friend && !d.noCharm) line("Win it over to get its friend card.", MUTE());
		// keep the selection in view when you're using the arrow keys
		const top = this.sel * this.rowH;
		if (top < this.target) this.target = top; else if (top + this.rowH > this.target + this.LH) this.target = top + this.rowH - this.LH;
	}
	tick(dt, active) {
		if (active) {
			this.target += E.mouse.wheel * 0.3;
			if (E.kp.has("ArrowDown")) { this.pick(this.sel + 1); sfx.tick(); }
			if (E.kp.has("ArrowUp")) { this.pick(this.sel - 1); sfx.tick(); }
		}
		this.target = clamp(this.target, 0, this.max);
		this.scroll = damp(this.scroll, this.target, 14, dt);
		this.rows.forEach((r, i) => {
			const y = this.LY + i * this.rowH - this.scroll, vis = y >= this.LY - 2 && y + this.rowH <= this.LY + this.LH + 2;
			r.g.y = y; r.g.vis = vis; r.h.y = y - 2; r.h.vis = vis;
			const on = i === this.sel, hov = E.hovered === r.h;
			r.g.hl.a = on ? 0.55 : hov ? 0.25 : 0;
			r.g.ox = damp(r.g.ox, on ? 4 : 0, 16, dt);
		});
		
	}
}