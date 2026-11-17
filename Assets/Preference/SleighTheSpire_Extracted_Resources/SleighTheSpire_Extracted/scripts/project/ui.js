import { E, W, H, FH, PAL, MUTE, Scene, Grp, tween, wrap, pushOverlay, popOverlay, saveSettings, retypeAll,
	toggleFullscreen, isFullscreen, clamp, damp, burst, floatText, hasAnim, hasType, after, textW, fitText, applyVideo } from "./engine.js";
import { sfx } from "./audio.js";
import { icon, potionIcon, partIcon } from "./art.js";
import { GIFTS, RELICS, POTIONS, ST, findKeywords, TYPE_NAME, RAR_NAME } from "./data.js";
import { G, RUN, giftVals, giftCost, giftName, spiritTier, nextTier, nightsLeft, TIERS, dateLabel, canPart, attachPart } from "./run.js";
import { goTitle } from "./scenes.js";
import { PARTS } from "./parts.js";
import { T } from "./characters.js";
import { LedgerOverlay } from "./ledger.js";

export const CW = 66, CH = 88;
export const TYPECOL = { atk: [PAL.red, PAL.red2], skl: [PAL.green, PAL.green2], pow: [PAL.gold, PAL.gold2], curse: [0x5a5a6a, 0x2e2e3a], friend: [PAL.pink, 0xb0406a] };
const BOX = [[PAL.red, PAL.gold], [PAL.green, PAL.red], [0x3b86c8, 0xffffff], [PAL.gold, PAL.red2], [PAL.purple, PAL.gold], [0xffffff, PAL.red], [PAL.pink, PAL.green], [0x2a2a3a, PAL.gold]];
export function boxCols(id) { let h = 0; for (const c of id) h = (h * 31 + c.charCodeAt(0)) | 0; return BOX[Math.abs(h) % BOX.length]; }

const NAME_SC = 0.75;
const NAME_PX = (CW - 6) / NAME_SC;
// two lines max, measured with the font's real glyph widths
export function cardName(name, up) {
	const lines = []; let cur = "";
	for (const w of name.split(" ")) {
		const ww = fitText(w, NAME_PX);
		if (!cur) cur = ww; else if (textW(cur + " " + ww) <= NAME_PX) cur += " " + ww; else { lines.push(cur); cur = ww; }
	}
	if (cur) lines.push(cur);
	const out = lines.slice(0, 2);
	if (lines.length > 2) out[1] = fitText(out[1] + " " + lines[2], NAME_PX);
	if (up) { const l = out.length - 1; out[l] = textW(out[l] + "+") <= NAME_PX ? out[l] + "+" : fitText(out[l], NAME_PX - textW("+")) + "+"; }
	return out.join("\n");
}
export function giftDesc(gi, C = null, tgt = null) {
	const d = GIFTS[gi.id], V = giftVals(gi);
	return d.desc.replace(/\{(\w+)\}/g, (_, k) => {
		let v = V[k];
		if (C && k === "d") v = C.previewDmg(v, tgt);
		if (C && k === "b") v = C.previewBlock(v);
		return String(v);
	});
}
export function cardStat(gi, C = null) {
	const d = GIFTS[gi.id], V = giftVals(gi);
	if (V.d !== undefined) return { icon: "sword", s: (C ? C.previewDmg(V.d) : V.d) + (d.hits ? "x" + d.hits : "") };
	if (V.b !== undefined) return { icon: "shield", s: String(C ? C.previewBlock(V.b) : V.b) };
	if (gi.id === "bellybump") return { icon: "sword", s: C ? String(C.previewDmg(C.p.block)) : "W" };
	return { icon: null, s: { atk: "ATK", skl: "SKILL", pow: "POWER", curse: "CURSE", friend: "FRIEND" }[d.type] };
}
export function giftTip(gi, C = null, tgt = null) {
	const d = GIFTS[gi.id], desc = giftDesc(gi, C, tgt);
	const b = [{ s: d.type === "friend" ? TYPE_NAME.friend : `${TYPE_NAME[d.type]} - ${d.r < 9 ? RAR_NAME[d.r] : "Curse"}`, c: PAL.mute }, desc];
	if (gi.part) b.push({ s: `${PARTS[gi.part].name}: ${PARTS[gi.part].desc}`, c: PARTS[gi.part].col });
	return { t: giftName(gi), c: TYPECOL[d.type][0], b, w: 24, kw: findKeywords(d.desc) };
}

export function makeCard(sc, gi, x, y, layer = "UI", C = null) {
	const d = GIFTS[gi.id];
	const g = sc.g(layer, x, y);
	const name = d.art || d.id, art = hasAnim("Gift", name) ? name : hasAnim("Gift", d.type) ? d.type : null;
	if (art) g.card = g.sp("Gift", 0, 0, { anim: art, w: CW, h: CH });
	const title = cardName(d.name, gi.up), lines = title.split("\n").length;
	const TITLE_MID = -CH / 2 + 3 + FH * NAME_SC;
	g.t(0, Math.round(TITLE_MID - lines * FH * NAME_SC / 2), title, { al: "center", sc: NAME_SC, col: gi.up ? 0xc8ffb0 : PAL.white, sh: true });
	if (!d.flags.includes("u")) {
		icon(g, "cost", -CW / 2 + 4, -CH / 2 + 4);
		g.costK = g.t(-CW / 2 + 4, -CH / 2 - 2, String(giftCost(gi)), { al: "center", col: PAL.white, sh: true });
	}
	const st = cardStat(gi, C);
	if (st.icon) icon(g, st.icon, -12, 30);
	g.statK = g.t(st.icon ? 6 : 0, 27, st.s, { al: "center", col: PAL.white, sh: true });
	if (gi.part) partIcon(g, gi.part, CW / 2 - 4, -CH / 2 + 4);
	g.gi = gi;
	return g;
}
export function refreshCard(g, C, playable) {
	if (g.dead) return;
	const gi = g.gi;
	if (g.costK) {
		const cost = C.costOf(gi);
		g.setT(g.costK, String(cost));
		g.setCol(g.costK, cost > C.energy ? 0xff6060 : cost < giftCost(gi) ? 0x80ff90 : PAL.white);
	}
	g.setT(g.statK, cardStat(gi, C).s);
}

// needs Brightness as the first effect on Gift, Icon and Txt
export function cardBright(g, v) {
	if (!g || g.dead || g._br === v) return;
	g._br = v;
	if (!g.card) { g.dim = v / 100; return; }
	const shadows = new Set(); for (const k of g.k) if (k.shk) shadows.add(k.shk);
	for (const k of g.k) {
		if ((k.kind !== "s" && k.kind !== "t") || shadows.has(k)) continue;
		try { k.i.effects[0].setParameter(0, v / 100); } catch (_) {}
	}
}

export function drawStatuses(g, st, x, y, maxW = 70) {
	let cx = 0, cy = 0;
	for (const k in st) {
		const v = st[k]; if (!v || !ST[k]) continue;
		const s = ST[k], w = 9 + textW(String(v)) + 2;
		if (cx + w > maxW && cx > 0) { cx = 0; cy += 13; }
		icon(g, "st_" + k, x + cx + 4, y + cy + 6);
		g.t(x + cx + 9, y + cy, String(v), { col: s.debuff ? 0xffb0b0 : PAL.white, sh: true });
		cx += w;
	}
	return cy + 13;
}

export function statusKw(u) {
	const out = [];
	for (const k in u.st) { const v = u.st[k]; if (!v || !ST[k]) continue; out.push({ t: `${ST[k].n} ${v}`, c: ST[k].debuff ? 0xffb0b0 : ST[k].c, b: ST[k].d.replace("{n}", v) }); }
	return out;
}

export function bubble(sc, x, y, text, o = {}) {
	const full = wrap(T(text), o.wc ?? 22), lines = full.split("\n");
	const w = Math.max(...lines.map(textW)) + 14, h = lines.length * FH + 9;
	const g = sc.g("Pop", clamp(x, w / 2 + 6, W - w / 2 - 6), y);
	const bx = -w / 2, by = -h - 9, dark = 0x1a1a2a, bc = o.col ?? 0xfff8ec, ol = PAL.black;
	const tx = clamp(x - g.x, bx + 8, bx + w - 12);
	const tail = [[-4, 8], [-3, 6], [-2, 4], [-1, 2]];
	g.r(bx - 2, by - 2, w + 4, h + 4, ol);
	tail.forEach(([dx, tw], i) => g.r(tx + dx - 2, by + h + i * 2, tw + 4, 2, ol));
	g.r(tx - 1 - 2, by + h + 8, 6, 2, ol);
	g.r(bx, by, w, h, bc);
	tail.forEach(([dx, tw], i) => g.r(tx + dx, by + h - (i ? 0 : 2) + i * 2, tw, i ? 2 : 4, bc));
	const k = g.t(bx + 6, by + 5, "", { col: dark });
	g.s = 0.85; tween(g, { s: 1 }, 0.2, "outBack");
	const cps = E.settings.textSpeed >= 3 ? 9999 : 42 * E.settings.textSpeed;
	let n = 0; const total = full.length;
	const prox = { get v() { return n; }, set v(x) { const nn = Math.floor(x); if (nn !== n) { n = nn; g.setT(k, full.slice(0, n)); if (full[n - 1] !== " ") sfx.type(); } } };
	const res = { g, skipped: false };
	res.done = new Promise(resolve => {
		const fin = () => { tween(g, { a: 0, s: 0.85 }, 0.18, "inQuad", 0, () => { g.kill(); resolve(); }); };
		res.tw = tween(prox, { v: total }, total / cps, "linear", 0, () => {
			res.hold = { t: 0 };
			res.hw = tween(res.hold, { t: 1 }, o.hold ?? (0.9 + total * 0.02), "linear", 0, fin);
		});
		res.kill = () => {
			if (res.killed) return; res.killed = true;
			res.tw.dead = true; if (res.hw) res.hw.dead = true;
			g.kill(); resolve();
		};
		res.skip = () => {
			if (res.skipped) return; res.skipped = true;
			res.tw.dead = true; n = total; g.setT(k, full);
			if (res.hw) res.hw.dead = true;
			tween({ t: 0 }, { t: 1 }, 0.25, "linear", 0, fin);
		};
	});
	return res;
}

export function banner(text, col = PAL.white, sub = "", dur = 0.9) {
	const rib = new Grp(null, "Pop", 0, H / 2);
	rib.r(0, -26, W, 52, PAL.black, 0.78); rib.r(0, -26, W, 2, col); rib.r(0, 25, W, 2, col);
	rib.sy = 0;
	const tx = new Grp(null, "Pop", -W / 2, H / 2);
	tx.t(0, sub ? -20 : -12, text, { al: "center", sc: 2, col, sh: true });
	if (sub) tx.t(0, 8, sub, { al: "center", col: MUTE() });
	const OPEN = 0.16, IN = 0.3, OUT = 0.24, CLOSE = 0.16, out = OPEN * 0.5 + IN + dur;
	return new Promise(r => {
		tween(rib, { sy: 1 }, OPEN, "outCubic");
		tween(tx, { x: W / 2 }, IN, "outBack", OPEN * 0.5);
		tween(tx, { x: W * 1.5 }, OUT, "inCubic", out);
		tween(rib, { sy: 0 }, CLOSE, "inCubic", out + OUT * 0.7, () => { rib.kill(); tx.kill(); r(); });
	});
}

export function panel(g, x, y, w, h, border = PAL.gold) {
	if (hasType("Board")) g.np("Board", x - 4, y - 4, w + 8, h + 8);
}

// only rebuilds when something it shows actually changes (see signature)
export class HUD {
	constructor(sc, ctx = null) {
		this.sc = sc; this.ctx = ctx; this.g = null; this.hots = []; this.sig = "";
		this.disp = { hp: G.hp, cookies: G.cookies, spirit: G.spirit }; this.trail = G.spirit;
		this.build();
	}
	clear() { this.g?.kill(); this.ng?.forEach(n => n.kill()); for (const h of this.hots) this.sc.unhot(h); this.hots = []; }
	hot(o) { const h = this.sc.hot(o); this.hots.push(h); return h; }
	num(x, y, col) { const g = this.sc.g("UI", x, y); g.k0 = g.t(0, 0, "", { col, sh: true }); this.ng.push(g); return g; }
	build() {
		this.clear(); this.ng = [];
		const sc = this.sc, g = this.g = sc.g("UI", 0, 0), tier = spiritTier();
		g.r(0, 0, W, 21, PAL.night0, 0.94); g.r(0, 21, W, 2, PAL.black);
		this.heart = sc.g("UI", 8, 10); this.ng.push(this.heart);
		icon(this.heart, "heart", 0, 0);
		this.hpG = this.num(16, 5, 0xff9a90);
		this.hot({ x: 0, y: 0, w: 90, h: 21, tip: () => ({ t: "Health", c: PAL.red, b: `${G.hp} of ${G.maxHp} HP.` }) });
		icon(g, "coin", 98, 10);
		this.ckG = this.num(106, 5, PAL.gold);
		this.hot({ x: 90, y: 0, w: 62, h: 21, tip: { t: "Cookies", c: PAL.gold, b: "Spend them in the Elf Shop." } });
		for (let i = 0; i < G.potionSlots; i++) {
			const bx = 154 + i * 20, id = G.potions[i];
			g.r(bx - 1, 2, 17, 18, PAL.black); g.r(bx + 1, 4, 13, 14, PAL.panel);
			if (id) potionIcon(g, id, bx + 7, 11);
			else icon(g, "potion_empty", bx + 7, 11);
			this.hot({
				x: bx, y: 2, w: 16, h: 18,
				tip: id ? { t: POTIONS[id].name, c: POTIONS[id].col, b: POTIONS[id].desc } : { t: "Empty potion slot" },
				onClick: id ? () => pushOverlay(new PotionMenu(i, this.ctx, bx)) : null,
			});
		}
		const sx = this.sx = 230 + 17 * Math.max(0, G.potionSlots - 3);
		icon(g, "tree", sx + 5, 10);
		g.r(sx + 12, 7, 104, 9, PAL.black);
		this.trailK = g.r(sx + 14, 9, 0, 5, 0xffffff, 0.6);
		this.fillK = g.r(sx + 14, 9, 0, 5, tier.col);
		this.shineK = g.r(sx + 14, 9, 0, 1, 0xffffff, 0.4);
		for (const t of TIERS) if (t.min) g.r(sx + 14 + t.min, 9, 1, 5, 0xffffff, 0.5);
		this.spG = this.num(sx + 120, 5, tier.col);
		this.hot({ x: sx - 2, y: 0, w: 150, h: 21,
			tip: () => { const t = spiritTier(), nx = nextTier(); return { t: `Christmas Spirit: ${t.name}`, c: t.col, b: [`${G.spirit}/100. ${t.desc}`, ...(nx ? [{ s: `${nx.name} at ${nx.min}: ${nx.short}.`, c: nx.col }] : [])] }; } });
		const nl = nightsLeft(G.night), cx = 410;
		icon(g, "calendar", cx + 12, 10);
		g.t(cx + 12, 6, String(nl), { al: "center", col: PAL.red2 });
		g.t(cx + 30, 5, nl === 1 ? "CHRISTMAS EVE" : "NIGHTS LEFT", { col: nl <= 3 ? PAL.gold : PAL.white, sh: true });
		this.hot({ x: cx, y: 0, w: 150, h: 21, tip: { t: dateLabel(Math.min(23, G.night)), c: PAL.red, b: `${nl} night${nl === 1 ? "" : "s"} until Christmas morning.` } });
		icon(g, "sack", 571, 10);
		g.t(580, 5, String(G.bag.length), { col: PAL.white, sh: true });
		this.hot({ x: 562, y: 0, w: 40, h: 21, tip: { t: "Santa's Sack" }, onClick: () => openBag() });
		this.gear = sc.g("UI", 624, 11); this.ng.push(this.gear);
		icon(this.gear, "gear", 0, 0);
		this.gearH = this.hot({ x: 612, y: 0, w: 28, h: 21, tip: { t: "Settings (Esc)" }, onClick: () => openSettings(true), key: "Escape" });
		this.relicXY = {};
		let rx = 4, ry = 23, rowH = 0;
		G.relics.forEach(r => {
			const d = RELICS[r.id];
			const k = icon(g, "relic_" + r.id, 0, 0);
			const w = k ? k.w : 13, h = k ? k.h : 13;
			if (rx + w > W - 4) { rx = 4; ry += rowH + 2; rowH = 0; }
			if (k) { k.x = rx + w / 2; k.y = ry + h / 2; }
			this.hot({ x: rx - 1, y: ry, w: w + 2, h: h + 2, tip: { t: d.name, c: PAL.gold, b: d.desc, kw: findKeywords(d.desc) } });
			this.relicXY[r.id] = [rx + w / 2, ry + h / 2];
			rx += w + 2; rowH = Math.max(rowH, h);
		});
		this.relicNext = [rx + 6, ry + 6];
		this.sig = this.signature();
		this.tick(0.016, true);
	}
	signature() { return JSON.stringify([G.potions, G.potionSlots, G.relics.map(r => r.id), G.night, G.bag.length, G.maxHp]); }
	relicPos(id) { return this.relicXY?.[id] ?? [10, 30]; }
	ping(id) { const [x, y] = this.relicPos(id); burst(x, y, 10, { cols: [PAL.gold, 0xffffff], spd: [30, 80], g: 0, life: 0.5 }); floatText(x + 4, y + 6, "!", PAL.gold, { rise: 10 }); }
	bump(g) { g.s = 1.2; }
	tick(dt = E.dt, force = false) {
		if (G.changed) { G.changed = false; if (this.signature() !== this.sig) { this.build(); return; } }
		const d = this.disp, tier = spiritTier();
		for (const [key, gr, col] of [["hp", this.hpG, PAL.red], ["cookies", this.ckG, PAL.gold], ["spirit", this.spG, tier.col]]) {
			const tgt = G[key];
			if (force) d[key] = tgt;
			else if (d[key] !== tgt) {
				const before = Math.round(d[key]);
				d[key] = Math.abs(tgt - d[key]) < 0.5 ? tgt : damp(d[key], tgt, 9, dt);
				if (Math.round(d[key]) !== before) { sfx.count(); if (gr.s < 1.2) this.bump(gr, col); }
			}
			gr.s = damp(gr.s, 1, 12, dt || 1);
			const v = Math.round(d[key]);
			gr.setT(gr.k0, key === "hp" ? `${v}/${G.maxHp}` : String(v));
		}
		this.hpG.setCol(this.hpG.k0, G.hp < G.maxHp * 0.3 ? 0xff5050 : 0xff9a90);
		this.spG.setCol(this.spG.k0, tier.col);
		const fw = Math.round(d.spirit);
		this.fillK.w = fw; this.fillK.col = tier.col; this.fillK.cd = true; this.shineK.w = fw;
		this.trail = this.trail > d.spirit ? damp(this.trail, d.spirit, 2.5, dt || 1) : d.spirit;
		this.trailK.w = Math.round(this.trail);
		const low = G.hp < G.maxHp * 0.3, bpm = low ? 2.4 : 1;
		const ph = (E.t * bpm) % 1;
		this.heart.s = 1 + (ph < 0.12 ? 0.25 * Math.sin(ph / 0.12 * Math.PI) : 0) * (low ? 1.4 : 0.6);
		this.gear.rot = damp(this.gear.rot, E.hovered === this.gearH ? Math.PI / 2 : 0, 10, dt || 1);
		if (E.spiritDrop) { const t = E.spiritDrop; E.spiritDrop = null; if (E.settings.spiritWarn) { sfx.warn(); banner("SPIRIT FALLING", t.col, `Now ${t.name}: ${t.desc}`, 1.3); } }
	}
}

export function openBag(list = null, title = null) {
	const l = (list || G.bag).slice().sort((a, b) => { const A = GIFTS[a.id], B = GIFTS[b.id]; return A.type.localeCompare(B.type) || A.name.localeCompare(B.name); });
	pushOverlay(new GiftPicker({ title: title || `SANTA'S SACK - ${l.length} GIFTS`, list: l, cancel: "CLOSE" }));
}
export function openSettings(inRun) { if (!E.overlays.some(o => o instanceof SettingsOverlay)) pushOverlay(new SettingsOverlay(inRun)); }
export function openLedger() { if (!E.overlays.some(o => o instanceof LedgerOverlay)) pushOverlay(new LedgerOverlay()); }

export function addPartFlow(partId, done = null, cancel = null) {
	const list = G.bag.filter(canPart);
	if (!list.length) { floatText(W / 2, 90, "None of your gifts can take a part.", PAL.red); sfx.error(); cancel?.(); return false; }
	const p = PARTS[partId];
	pushOverlay(new GiftPicker({ title: `ADD ${p.name.toUpperCase()} TO A GIFT`, sub: p.desc, list, cancel: "BACK", onCancel: cancel,
		label: () => ({ s: "+ " + p.name, c: p.col }),
		onPick: gi => { attachPart(gi, partId); sfx.buff(); burst(W / 2, 180, 26, { cols: [p.col, 0xffffff, PAL.gold], spd: [60, 200] }); floatText(W / 2, 150, `${gi.id ? GIFTS[gi.id].name : ""} + ${p.name}!`, p.col); done?.(gi); } }));
	return true;
}

export class ChoosePart extends Scene {
	constructor(o) { super(); this.o = o; this.tiles = []; }
	onBack() { popOverlay(this); this.o.onSkip?.(); }
	enter() {
		const o = this.o, n = o.ids.length, tw = 150, th = 120, gap = 14, x0 = W / 2 - (n * tw + (n - 1) * gap) / 2;
		const bg = this.g("Pop", 0, 0); bg.r(0, 0, W, H, PAL.black, 0.8); bg.noPop = true;
		const tt = this.g("Pop", W / 2, 54); tt.t(0, 0, o.title || "CHOOSE A TOY PART", { al: "center", col: PAL.gold, sh: true, sc: 2 });
		if (o.sub) tt.t(0, 26, o.sub, { al: "center", col: MUTE() });
		o.ids.forEach((id, i) => {
			const p = PARTS[id], x = x0 + i * (tw + gap), y = 110, g = this.g("Pop", x + tw / 2, y + th / 2);
			panel(g, -tw / 2, -th / 2, tw, th, p.col);
			partIcon(g, id, 0, -28);
			g.t(0, -8, p.name, { al: "center", col: p.col, sh: true });
			wrap(p.desc, 14).split("\n").forEach((l, j) => g.t(0, 8 + j * FH, l, { al: "center", col: PAL.white }));
			g.t(0, th / 2 - 16, ["", "Common", "Uncommon", "Rare"][p.r], { al: "center", col: MUTE() });
			g.y = H + 80; tween(g, { y: y + th / 2 }, 0.4, "outBack", 0.05 + i * 0.07);
			const h = this.hot({ x, y, w: tw, h: th, onClick: () => { sfx.pick(); popOverlay(this); o.onPick(id); } });
			this.tiles.push({ g, h, y: y + th / 2 });
		});
		this.btn({ x: W / 2 - 70, y: 290, w: 140, h: 22, layer: "Pop", label: o.skipLabel || "SKIP", key: "Escape", onClick: () => this.onBack() });
		this.popIn(W / 2, H / 2);
	}
	tick(dt) {
		for (const t of this.tiles) if (!E.tweens.some(w => w.o === t.g && !w.dead)) {
			const hov = E.hovered === t.h;
			t.g.s = damp(t.g.s, hov ? 1.08 : 1, 16, dt);
			
			t.g.y = damp(t.g.y, t.y - (hov ? 4 : 0), 14, dt);
		}
	}
}

export class PotionMenu extends Scene {
	constructor(slot, ctx, x) { super(); this.slot = slot; this.ctx = ctx; this.px = x; }
	onBack() { popOverlay(this); }
	enter() {
		const id = G.potions[this.slot], d = POTIONS[id];
		this.hot({ x: 0, y: 0, w: W, h: H, onClick: () => popOverlay(this), sfx: false });
		const w = 200, x = clamp(this.px - 20, 4, W - w - 4), y = 24;
		const body = wrap(d.desc, 18).split("\n"), h = 40 + body.length * FH + 26;
		const g = this.g("Pop", 0, 0);
		panel(g, x, y, w, h, d.col);
		potionIcon(g, id, x + 14, y + 12);
		g.t(x + 28, y + 6, d.name, { col: d.col, sh: true });
		body.forEach((l, i) => g.t(x + 8, y + 26 + i * FH, l));
		this.hot({ x, y, w, h });
		const canUse = this.ctx ? this.ctx.canUsePotion() : d.any;
		this.btn({ x: x + 8, y: y + h - 24, w: 86, h: 18, label: "USE", col: PAL.green2, disabled: !canUse, layer: "Pop",
			tip: canUse ? null : { b: this.ctx ? "Wait for your turn." : "Only usable in battle." },
			onClick: () => { popOverlay(this); if (this.ctx) this.ctx.usePotion(this.slot); else { d.out(RUN); G.potions[this.slot] = null; G.changed = true; sfx.potion(); } } });
		this.btn({ x: x + w - 94, y: y + h - 24, w: 86, h: 18, label: "TOSS", col: PAL.red2, layer: "Pop", onClick: () => { G.potions[this.slot] = null; G.changed = true; popOverlay(this); } });
		this.popIn(x + w / 2, y + h / 2);
	}
}

export class GiftPicker extends Scene {
	constructor(o) { super(); this.o = o; this.scroll = 0; this.target = 0; this.cards = []; }
	onBack() { if (!this.o.cancel) return; popOverlay(this); this.o.onCancel?.(); }
	enter() {
		const o = this.o;
		if (!o.cancel) this.onBack = null;
		const bg = this.g("Pop", 0, 0); bg.r(0, 0, W, H, PAL.black, 0.85);
		bg.a = 0; tween(bg, { a: 1 }, 0.2);
		const cols = 6, cw = 86, ch = 106, x0 = W / 2 - (cols * cw) / 2 + cw / 2, y0 = 118;
		o.list.forEach((gi, i) => {
			const x = x0 + (i % cols) * cw, y = y0 + Math.floor(i / cols) * ch;
			const c = makeCard(this, gi, x, y + 30, "Pop");
			if (o.label) { const L = o.label(gi); c.t(0, CH / 2 + 3, L.s, { al: "center", col: L.c ?? PAL.white, sh: true }); }
			c.by = y; c.a = 0;
			tween(c, { a: 1, y }, 0.25, "outCubic", Math.min(0.3, i * 0.015));
			c.h = this.hot({ x: x - CW / 2, y: y - CH / 2, w: CW, h: CH, tip: () => giftTip(gi), sfx: !!o.onPick,
				onClick: o.onPick ? () => { sfx.pick(); popOverlay(this); o.onPick(gi); } : null });
			this.cards.push(c);
		});
		if (!o.list.length) bg.t(W / 2, H / 2, "(empty)", { al: "center", col: MUTE() });
		const rows = Math.ceil(o.list.length / cols);
		this.max = Math.max(0, y0 + rows * ch - (H - 40) + 10);
		const top = this.g("Pop", 0, 0);
		top.r(0, 0, W, 64, PAL.night0); top.r(0, 64, W, 2, PAL.gold2);
		top.t(W / 2, 20, o.title, { al: "center", col: PAL.gold, sh: true });
		if (o.sub) top.t(W / 2, 38, fitText(o.sub, W - 20), { al: "center", col: o.subCol ?? MUTE(), sh: !!o.subCol });
		const bot = this.g("Pop", 0, 0);
		bot.r(0, H - 36, W, 36, PAL.night0); bot.r(0, H - 38, W, 2, PAL.gold2);
		let bx = 10;
		for (const e of o.extras || []) { const w = e.w ?? 230; this.btn({ x: bx, y: H - 29, w, h: 22, layer: "Pop", label: e.label, col: e.col, disabled: e.disabled, tip: e.tip, onClick: () => { popOverlay(this); e.onClick(); } }); bx += w + 8; }
		if (o.cancel) this.btn({ x: W - 130, y: H - 29, w: 120, h: 22, layer: "Pop", label: o.cancel, key: "Escape", onClick: () => { popOverlay(this); o.onCancel?.(); } });
		if (this.max > 0) bot.t(W / 2, H - 24, "SCROLL", { al: "center", col: MUTE() });
	}
	tick(dt, active) {
		if (active) {
			this.target += E.mouse.wheel * 0.4;
			if (E.keys.has("ArrowDown")) this.target += 300 * dt;
			if (E.keys.has("ArrowUp")) this.target -= 300 * dt;
			if (E.mouse.down && Math.abs(E.mouse.y - E.mouse.sy) > 6) { this.target -= (E.mouse.y - (this.ly ?? E.mouse.y)); E.pressHot = null; }
			this.ly = E.mouse.down ? E.mouse.y : null;
		}
		this.target = clamp(this.target, 0, this.max);
		this.scroll = damp(this.scroll, this.target, 14, dt);
		for (const c of this.cards) {
			const hov = E.hovered === c.h;
			c.s = damp(c.s, hov ? 1.12 : 1, 18, dt);
			if (c.by !== undefined && !this._intro(c)) { c.y = c.by - this.scroll - (hov ? 4 : 0); }
			c.h.y = c.by - this.scroll - CH / 2;
			c.h.vis = c.h.y > 40 && c.h.y + CH < H - 20;
		}
	}
	_intro(c) { return E.tweens.some(t => t.o === c && !t.dead && t.to.y !== undefined); }
}

export class ChooseGift extends Scene {
	constructor(o) { super(); this.o = o; this.cards = []; }
	onBack() { popOverlay(this); this.o.onSkip?.(); }
	enter() {
		const o = this.o, n = o.ids.length, sp = 92;
		const bg = this.g("Pop", 0, 0); bg.r(0, 0, W, H, PAL.black, 0.8); bg.a = 0; tween(bg, { a: 1 }, 0.2);
		const tt = this.g("Pop", W / 2, 64); tt.t(0, 0, o.title || "CHOOSE A GIFT", { al: "center", col: PAL.gold, sh: true, sc: 2 }); tt.s = 0.3; tween(tt, { s: 1 }, 0.4, "outBack");
		if (o.sub) bg.t(W / 2, 94, o.sub, { al: "center", col: MUTE() });
		o.ids.forEach((id, i) => {
			const gi = { id, up: !!o.up, uid: -1 }, x = W / 2 - ((n - 1) * sp) / 2 + i * sp, y = 180;
			const c = makeCard(this, gi, x, H + 60, "Pop");
			c.s = 0.9;
			tween(c, { y, s: 1 }, 0.35, "outCubic", 0.05 + i * 0.06);
			after(0.05 + i * 0.08, () => sfx.draw());
			c.ty = y;
			c.h = this.hot({ x: x - CW / 2 - 4, y: y - CH / 2 - 4, w: CW + 8, h: CH + 8, tip: () => giftTip(gi),
				onClick: () => { sfx.pick(); burst(x, y, 24, { cols: [PAL.gold, PAL.red, PAL.green, 0xffffff], spd: [60, 200] }); popOverlay(this); o.onPick(id); } });
			this.cards.push(c);
		});
		this.btn({ x: W / 2 - 70, y: 286, w: 140, h: 22, layer: "Pop", label: o.skipLabel || "SKIP", key: "Escape", onClick: () => { popOverlay(this); o.onSkip?.(); } });
	}
	tick(dt) {
		for (const c of this.cards) {
			const hov = E.hovered === c.h;
			if (!E.tweens.some(t => t.o === c && !t.dead)) {
				c.s = damp(c.s, hov ? 1.1 : 1, 16, dt);
				c.y = damp(c.y, c.ty - (hov ? 6 : 0), 14, dt);
			}
		}
	}
}

export class Confirm extends Scene {
	constructor(text, onYes, yes = "YES", no = "NO") { super(); this.text = text; this.onYes = onYes; this.yes = yes; this.no = no; }
	onBack() { popOverlay(this); }
	enter() {
		const g = this.g("Pop", 0, 0); g.r(0, 0, W, H, PAL.black, 0.7); g.noPop = true;
		const lines = wrap(this.text, 30).split("\n"), w = 330, h = 50 + lines.length * FH, x = W / 2 - w / 2, y = H / 2 - h / 2;
		const p = this.g("Pop", 0, 0);
		panel(p, x, y, w, h);
		lines.forEach((l, i) => p.t(W / 2, y + 8 + i * FH, l, { al: "center" }));
		this.btn({ x: x + 20, y: y + h - 28, w: 130, h: 20, layer: "Pop", label: this.yes, col: PAL.red2, key: "Enter", onClick: () => { popOverlay(this); this.onYes(); } });
		this.btn({ x: x + w - 150, y: y + h - 28, w: 130, h: 20, layer: "Pop", label: this.no, key: "Escape", onClick: () => popOverlay(this) });
		this.popIn(W / 2, H / 2);
	}
}

const TABS = [["audio", "AUDIO"], ["video", "VIDEO"], ["gameplay", "GAMEPLAY"], ["access", "ACCESS"]];
export class SettingsOverlay extends Scene {
	constructor(inRun, tab = "audio", quick = false) { super(); this.inRun = inRun; this.tab = tab; this.quick = quick; this.items = []; this.sliders = []; this.toggles = []; }
	onBack() { saveSettings(); popOverlay(this); }
	rowsFor(tab) {
		const S = E.settings, tg = (label, key, after) => ({ type: "toggle", label, get: () => S[key], set: v => { S[key] = v; after?.(); } });
		return {
			audio: [
				{ type: "slider", label: "Master volume", key: "master" },
				{ type: "slider", label: "Music", key: "music" },
				{ type: "slider", label: "Sound FX", key: "sfx" },
				tg("Mute all", "mute"),
			],
			video: [
				{ type: "toggle", label: "Fullscreen (F11)", get: () => isFullscreen(), set: () => {}, native: () => toggleFullscreen() },
				tg("CRT scanlines", "crt", applyVideo),
				{ type: "cycle", label: "Particles", key: "particles", opts: [["LOW", 0.35], ["MEDIUM", 0.7], ["HIGH", 1]] },
				{ type: "cycle", label: "Screenshake", key: "shake", opts: [["OFF", 0], ["LOW", 0.5], ["FULL", 1]] },
				tg("Screen flashes", "flash"),
			],
			gameplay: [
				{ type: "cycle", label: "Text speed", key: "textSpeed", opts: [["SLOW", 0.5], ["NORMAL", 1], ["FAST", 2], ["INSTANT", 3]] },
				{ type: "cycle", label: "Game speed", key: "gameSpeed", opts: [["1x", 1], ["1.5x", 1.5], ["2x", 2]] },
				tg("Confirm end turn", "confirmEnd"),
				tg("Fast enemy turns", "fastEnemy"),
				tg("Hold to end turn", "holdConfirm"),
				tg("Skip intro poem", "skipPoem"),
				tg("Spirit warnings", "spiritWarn"),
			],
			access: [
				tg("Reduce motion", "reduceMotion"),
				tg("Damage numbers", "damageNumbers"),
				...(hasType("Text") ? [tg("Large text", "largeText", retypeAll)] : []),
			],
		}[tab];
	}
	enter() {
		const g = this.g("Pop", 0, 0); g.r(0, 0, W, H, PAL.black, 0.78); g.noPop = true;
		const S = E.settings, rows = this.rowsFor(this.tab);
		const pw = 440, ph = 64 + 7 * 23 + 40;
		this.px = W / 2 - pw / 2; this.py = Math.round(H / 2 - ph / 2);
		const p = this.g("Pop", 0, 0);
		panel(p, this.px, this.py, pw, ph);
		p.t(W / 2, this.py + 6, "SETTINGS", { al: "center", col: PAL.gold, sh: true });
		const tw = (pw - 20 - 3 * 4) / 4;
		TABS.forEach(([id, label], i) => {
			const on = id === this.tab;
			const b = this.btn({ x: this.px + 10 + i * (tw + 4), y: this.py + 22, w: tw, h: 18, layer: "Pop", label, col: on ? PAL.gold2 : PAL.panel2, sfx: !on,
				onClick: () => { if (on) return; sfx.page(); popOverlay(this); pushOverlay(new SettingsOverlay(this.inRun, id, true)); } });
			if (on) b.g.oy = -2;
		});
		p.r(this.px + 10, this.py + 42, pw - 20, 2, PAL.gold2);
		const x = this.px + 24; let y = this.py + 52;
		const row = label => { const g = this.g("Pop", 0, 0); g.t(x, y + 3, label, { col: PAL.white }); this.items.push(g); };
		for (const r of rows) {
			row(r.label);
			if (r.type === "slider") {
				const bx = x + 170, by = y + 7, bw = 140, g = this.g("Pop", 0, 0), s = { key: r.key, bx, by, bw, g };
				const bh = 6;
				g.r(bx - 2, by - 2, bw + 4, bh + 4, PAL.black);
				s.fill = hasAnim("SliderBar") ? g.sp("SliderBar", bx, by, { w: 0, h: bh }) : { w: 0 };
				s.knob = this.g("Pop", 0, by + bh / 2); s.knob.r(-4, -7, 8, 14, PAL.black); s.knob.r(-2, -5, 4, 10, PAL.white);
				s.lab = g.t(bx + bw + 10, by - 4, "", { col: MUTE() });
				s.draw = () => { const v = S[r.key]; s.fill.w = Math.round(bw * v); s.knob.x = bx + Math.round(bw * v); g.setT(s.lab, Math.round(v * 100) + "%"); };
				s.draw(); s.h = this.hot({ x: bx - 6, y: by - 8, w: bw + 12, h: 20, onClick: () => {}, sfx: false });
				this.sliders.push(s);
			} else if (r.type === "toggle") {
				const tx = x + 270, ty = y + 2, g = this.g("Pop", tx, ty), t = { g, get: r.get, set: r.set, native: r.native };
				const custom = hasAnim("Toggle", "on") && hasAnim("Toggle", "off") && hasAnim("Toggle", "knob");
				let tw2 = 32, th = 14, kw = 12;
				if (custom) {
					t.track = g.sp("Toggle", 0, 0, { anim: "off" }); tw2 = t.track.w; th = t.track.h; t.track.x = tw2 / 2; t.track.y = th / 2;
					t.knob = this.g("Pop", 0, ty + th / 2); kw = t.knob.sp("Toggle", 0, 0, { anim: "knob" }).w;
				} else t.knob = this.g("Pop", 0, ty + th / 2);
				const kOff = tx + kw / 2 + 1, kOn = tx + tw2 - kw / 2 - 1;
				t.lab = g.t(tw2 + 8, 1, "", { col: MUTE() });
				t.sync = anim => {
					const on = !!r.get(), kx = on ? kOn : kOff;
					if (anim) { tween(t.knob, { x: kx }, 0.18, "outBack"); t.knob.sx = 1.4; t.knob.sy = 0.7; tween(t.knob, { sx: 1, sy: 1 }, 0.25, "outBack"); } else t.knob.x = kx;
					if (custom) g.anim(t.track, on ? "on" : "off");
					g.setT(t.lab, on ? "ON" : "OFF"); g.setCol(t.lab, on ? PAL.green : MUTE());
				};
				t.sync(false);
				this.hot({ x: x - 4, y: y - 2, w: 340, h: 20, sfx: false, native: r.native ? () => { r.native(); setTimeout(() => t.sync(true), 300); } : null,
					onClick: () => { if (r.native) return; r.set(!r.get()); sfx.toggle(!!r.get()); t.sync(true); saveSettings(); } });
				this.toggles.push(t);
			} else {
				const cx = x + 200, cw = 140, idx = () => Math.max(0, r.opts.findIndex(o => o[1] === S[r.key]));
				const b = this.btn({ x: cx, y, w: cw, h: 18, layer: "Pop", label: "< " + r.opts[idx()][0] + " >", sfx: false,
					onClick: () => { const dir = E.mouse.x < cx + cw / 2 ? -1 : 1; S[r.key] = r.opts[(idx() + dir + r.opts.length) % r.opts.length][1]; b.setLabel("< " + r.opts[idx()][0] + " >"); sfx.tick(); saveSettings(); } });
			}
			y += 23;
		}
		const by = this.py + ph - 28;
		this.btn({ x: this.px + pw - 126, y: by, w: 112, h: 20, layer: "Pop", label: "BACK", key: "Escape", onClick: () => this.onBack() });
		this.btn({ x: this.px + pw - 252, y: by, w: 118, h: 20, layer: "Pop", label: "LEDGER", onClick: () => openLedger() });
		if (this.inRun) this.btn({ x: this.px + 14, y: by, w: 140, h: 20, layer: "Pop", label: "ABANDON RUN", col: PAL.red2,
			onClick: () => pushOverlay(new Confirm("Abandon this run? Christmas will have to wait another year.", () => { saveSettings(); goTitle(); })) });
		if (!this.quick) this.popIn(W / 2, H / 2);
	}
	tick(dt, active) {
		for (const s of this.sliders) {
			const drag = active && E.mouse.down && E.pressHot === s.h;
			if (drag) {
				const v = Math.round(clamp((E.mouse.x - s.bx) / s.bw, 0, 1) * 20) / 20;
				if (v !== E.settings[s.key]) { E.settings[s.key] = v; s.draw(); saveSettings(); sfx.tick(); }
			}
			if (active && E.hovered === s.h && (E.kp.has("ArrowLeft") || E.kp.has("ArrowRight"))) { E.settings[s.key] = clamp(E.settings[s.key] + (E.kp.has("ArrowRight") ? 0.05 : -0.05), 0, 1); s.draw(); saveSettings(); sfx.tick(); }
			s.knob.s = damp(s.knob.s, drag ? 1.35 : E.hovered === s.h ? 1.15 : 1, 18, dt);
		}
		for (const t of this.toggles) t.knob.s = damp(t.knob.s, 1, 14, dt);
	}
}

export const HELP = [
	["THE GIFT BAG", "Each turn you pull 5 gifts from Santa's Sack. Giving a gift costs CHEER, shown on the top-left corner. Drag a gift onto a monster to throw it at them or up to activate non-attacking gifts.\n\nKeys 1-9: pick a gift\nTab: change target\nEnter: confirm\nE: end turn"],
	["WRAP & INTENTS", "WRAP blocks damage until your next turn. Icons above monsters show what they will do next: swords attack, shields defend, arrows buff, skulls curse you. Hover the enemy for more details."],
	["LEAVE A GIFT", "After EVERY battle you must leave one gift from your bag under a child's tree. The rarer the gift, the more CHRISTMAS SPIRIT it brings. You can pay Cookies instead to keep your bag, but the price increases every time."],
	["TOY PARTS & WISHES", "Some rewards are TOY PARTS: wheels, bells, springs and more. Each gift can hold one part, which changes what it does. Every child has a WISH. Leave a gift that matches it for extra Christmas Spirit."],
	["NICE & NAUGHTY", "Purple GRUMPY pips sit under a monster's health. Hit it with a gift it LOVES to pop 2 (a Big Ribbon pops 1); a gift it HATES puts one back. With none left it SOFTENS. Don't hit it again this turn and it joins you, then becomes a FRIEND gift in your sack!"],
	["RELICS", "Relics are lasting treasures shown under the top bar. Each one works on its own for the rest of the run. Hover a relic to see what it does. Elites, Stockings, the Elf Shop and some events give you new ones."],
	["SUPERPOWERS", "Every hero has a SUPERPOWER meter next to END TURN. Mrs. Claus fills it by popping Grumpy pips and playing Skills, Tinsel by playing gifts, Rudolph by taking hits. When it's full, click it (or press Q).\n\nKind Word: pop a pip and learn a love.\n\Tinker: fit a new toy part.\n\Reindeer: the next reindeer helps."],
	["CHRISTMAS SPIRIT", "Spirit fades 1 every night. MERRY (50+) keeps the kids believing, FESTIVE (70+) gives +1 Cheer on your first turn, JOLLY (90+) +1 Cheer every turn. GRIM (below 25) costs you a draw. Your Spirit also decides how strong Krampus becomes!"],
];
export class HelpOverlay extends Scene {
	constructor() { super(); this.page = 0; }
	onBack() { popOverlay(this); }
	enter() {
		this.bg = this.g("Pop", 0, 0); this.bg.r(0, 0, W, H, PAL.black, 0.8); this.bg.noPop = true;
		this.frame = this.g("Pop", 0, 0); panel(this.frame, 40, 26, 560, 300);
		this.build(0);
		this.popIn(W / 2, H / 2);
	}
	build(dir) {
		this.c?.kill(); for (const b of this.btns.slice()) b.kill();
		const g = this.c = this.g("Pop", 0, 0), [t, body] = HELP[this.page];
		const px = 40, py = 26, pw = 560, ph = 300;
		g.t(W / 2, py + 10, `${t}  (${this.page + 1}/${HELP.length})`, { al: "center", col: PAL.gold, sh: true });
		const hasImg = hasType("TutorialImage"), cols = hasImg ? 27 : 50;
		wrap(body, cols).split("\n").forEach((l, i) => g.t(px + 20, py + 36 + i * 14, l));
		if (hasImg) {
			const k = g.sp("TutorialImage", 0, 0, { frame: this.page });
			const f = Math.min(1, 250 / k.nw, 220 / k.nh); k.w = k.nw * f; k.h = k.nh * f;
			k.x = px + pw - 20 - 125; k.y = py + 36 + 110;
		}
		if (dir) { g.ox = dir * 40; g.a = 0; tween(g, { ox: 0, a: 1 }, 0.22, "outCubic"); }
		this.btn({ x: px + 20, y: py + ph - 30, w: 110, h: 20, layer: "Pop", label: "< PREV", key: "ArrowLeft", disabled: this.page === 0, onClick: () => { this.page--; sfx.page(); this.build(-1); } });
		this.btn({ x: W / 2 - 55, y: py + ph - 30, w: 110, h: 20, layer: "Pop", label: "CLOSE", key: "Escape", onClick: () => popOverlay(this) });
		this.btn({ x: px + pw - 130, y: py + ph - 30, w: 110, h: 20, layer: "Pop", label: "NEXT >", key: "ArrowRight", disabled: this.page === HELP.length - 1, onClick: () => { this.page++; sfx.page(); this.build(1); } });
	}
}