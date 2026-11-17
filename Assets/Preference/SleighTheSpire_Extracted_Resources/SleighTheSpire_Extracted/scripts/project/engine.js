import { sfx, audioUnlock, applyVolumes } from "./audio.js";

export const W = 640, H = 360, FW = 10, FH = 12;
const SPACING = [
[9,">MЖVЛМЦШЪфжмцшъ"],
[8,"ABCDEFGHKNOPQRSTUXYZabcdeghknopqsuvxy1234567890?^*АБВЕЁЗИЙКНОПРСТУХЧЬЭЯабвеёзийклнопрстухьчэяßÄäÀàÁáÃãÂâĄąÖöÕõÔôÓóÈèÉéÊêĘęÑñŃńÜüÙùÚúÛûŠšŚśŞşŽŹŻÇçĆćŁŸÿĞğЄє"],
[7,"fzJLrtГгҐґzžźż©"],
[6,"j@-/łIЇÍÎİíî"],
[5,"iıїl, "],
[4,".!:;'"]
];
export const CHAR_W = {};
for (const [w, chars] of SPACING) for (const ch of chars) CHAR_W[ch] = w;
export function textW(line) { let w = 0; for (const ch of String(line)) w += CHAR_W[ch] ?? FW; return w; }

export const PAL = {
	night0: 0x060914, night1: 0x0d1430, night2: 0x16214a, night3: 0x223167,
	snow: 0xf4f8ff, snow2: 0xbccde8, ice: 0x7fd3ff, ice2: 0x3b86c8,
	red: 0xe0443e, red2: 0x8e1d2b, green: 0x3fa56a, green2: 0x1c5a3a,
	gold: 0xf6c94f, gold2: 0xa8741f, brown: 0x8b5a2b, brown2: 0x4b2c17,
	pink: 0xff8fb1, purple: 0x9a66d6, orange: 0xf08a3c, black: 0x05050b,
	white: 0xfff8ec, mute: 0x95a2c6, panel: 0x141a36, panel2: 0x253262,
	cream: 0xf6ead2, outline: 0x0b0b16, window: 0xffd36b,
};

const DEFAULTS = {
	master: 0.8, music: 0.45, sfx: 0.8, mute: false,
	crt: false, particles: 1, damageNumbers: true,
	shake: 1, flash: true, reduceMotion: false, textSpeed: 1, gameSpeed: 1, highContrast: false,
	largeText: false, holdConfirm: false,
	confirmEnd: false, fastEnemy: false, skipPoem: false, spiritWarn: true,
};

export const E = {
	rt: null, t: 0, dt: 0, dts: 0, frame: 0, tempo: 1,
	mouse: { x: 0, y: 0, down: false, pressed: false, released: false, rpressed: false, wheel: 0, sx: 0, sy: 0, vx: 0, vy: 0 },
	keys: new Set(), kp: new Set(),
	scene: null, overlays: [], groups: [], tweens: [], waits: [], floats: [],
	hovered: null, pressHot: null, tipOverride: null, tipKey: "", tip: null,
	shk: 0, kx: 0, ky: 0, zoom: 0, hs: 0, trans: null, settings: { ...DEFAULTS },
	crtG: null,
};

export const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
export const lerp = (a, b, t) => a + (b - a) * t;
export const rf = (a, b) => a + Math.random() * (b - a);
export const rint = (a, b) => a + Math.floor(Math.random() * (b - a + 1));
export const damp = (a, b, k, dt) => a + (b - a) * (1 - Math.exp(-k * dt));
const _rgb = new Map();
export function rgb(h) {
	let v = _rgb.get(h);
	if (!v) { v = [((h >> 16) & 255) / 255, ((h >> 8) & 255) / 255, (h & 255) / 255]; _rgb.set(h, v); }
	return v;
}
export function mixCol(a, b, t) {
	const ar = (a >> 16) & 255, ag = (a >> 8) & 255, ab = a & 255;
	const br = (b >> 16) & 255, bg = (b >> 8) & 255, bb = b & 255;
	return (Math.round(ar + (br - ar) * t) << 16) | (Math.round(ag + (bg - ag) * t) << 8) | Math.round(ab + (bb - ab) * t);
}
function finalCol(h, f, d) {
	const r = ((h >> 16) & 255) / 255, g = ((h >> 8) & 255) / 255, b = (h & 255) / 255;
	return [Math.min(1, (r + (1 - r) * f) * d), Math.min(1, (g + (1 - g) * f) * d), Math.min(1, (b + (1 - b) * f) * d)];
}
export const MUTE = () => E.settings.highContrast ? 0xffffff : PAL.mute;

export function wrap(s, n) {
	const out = [], max = n * FW;
	for (const para of String(s).split("\n")) {
		let line = "";
		for (const w of para.split(" ")) {
			if (!line) line = w;
			else if (textW(line + " " + w) <= max) line += " " + w;
			else { out.push(line); line = w; }
			while (textW(line) > max && line.length > 1) {
				let cut = line.length - 1; while (cut > 1 && textW(line.slice(0, cut)) > max) cut--;
				out.push(line.slice(0, cut)); line = line.slice(cut);
			}
		}
		out.push(line);
	}
	return out.join("\n");
}
export function fitText(s, maxPx) {
	s = String(s); if (textW(s) <= maxPx) return s;
	while (s.length > 1 && textW(s + ".") > maxPx) s = s.slice(0, -1);
	return s + ".";
}

export const store = {
	get(k, d = null) { try { const v = globalThis.localStorage?.getItem("sleigh." + k); return v == null ? d : JSON.parse(v); } catch (_) { return d; } },
	set(k, v) { try { globalThis.localStorage?.setItem("sleigh." + k, JSON.stringify(v)); } catch (_) {} },
};
export const LIVE_SETTINGS = Object.keys(DEFAULTS);
export function loadSettings() {
	Object.assign(E.settings, store.get("settings", {}));
	for (const k in DEFAULTS) if (!(k in E.settings)) E.settings[k] = DEFAULTS[k];
	E.settings.highContrast = false;
}
export function saveSettings() { store.set("settings", E.settings); applyVolumes(); applyVideo(); }
export function isFullscreen() { return typeof document !== "undefined" && !!document.fullscreenElement; }
export function toggleFullscreen() {
	try {
		if (typeof document === "undefined") return;
		if (document.fullscreenElement) document.exitFullscreen();
		else document.documentElement.requestFullscreen();
	} catch (_) {}
}

const ANIM = new Map();
export const hasType = name => !!E.rt?.objects?.[name];
// making a throwaway instance was the only reliable way to check an anim exists
export function hasAnim(type, anim = null) {
	const key = type + "|" + anim;
	if (ANIM.has(key)) return ANIM.get(key);
	let ok = false, i = null;
	const T = E.rt?.objects?.[type];
	if (T) {
		try {
			i = T.createInstance("FX", -9999, -9999);
			if (anim == null) ok = true;
			else { i.setAnimation(anim); ok = String(i.animationName || "").toLowerCase() === String(anim).toLowerCase(); }
		} catch (_) { ok = false; }
		try { i?.destroy(); } catch (_) {}
	}
	ANIM.set(key, ok);
	return ok;
}
function textType() { return E.settings.largeText && hasType("Text") ? "Text" : "Txt"; }

// a Grp is a bunch of instances that move, scale and fade together
export class Grp {
	constructor(owner, layer, x = 0, y = 0) {
		this.layer = layer; this.x = x; this.y = y; this.ox = 0; this.oy = 0;
		this.s = 1; this.sx = 1; this.sy = 1; this.rot = 0; this.a = 1; this.vis = true;
		this.flash = 0; this.dim = 1; this._cf = -1; this._cd = -1;
		this.k = []; this.dead = false;
		E.groups.push(this);
		if (owner) owner.owned.push(this);
	}
	r(x, y, w, h, col = 0xffffff, a = 1) {
		const i = E.rt.objects.Px.createInstance(this.layer, 0, 0);
		const k = { i, kind: "r", x, y, w, h, col, a, rot: 0, cd: true };
		this.k.push(k); return k;
	}
	sp(type, x, y, o = {}) {
		const i = E.rt.objects[type].createInstance(this.layer, 0, 0);
		if (o.anim != null) try { i.setAnimation(o.anim); } catch (_) {}
		if (o.frame != null) try { i.animationSpeed = 0; i.animationFrame = o.frame; } catch (_) {}
		const nw = Math.abs(i.width) || 1, nh = Math.abs(i.height) || 1, sc = o.sc ?? 1;
		const k = { i, kind: "s", type, anim: o.anim, frame: o.frame, x, y, w: o.w ?? nw * sc, h: o.h ?? nh * sc, nw, nh,
			col: o.col ?? 0xffffff, a: o.a ?? 1, rot: o.rot ?? 0, mir: !!o.mir, cd: true, fl: null };
		this.k.push(k); return k;
	}
	np(type, x, y, w, h, o = {}) { const k = this.sp(type, x, y, { ...o, w, h }); k.kind = "n"; return k; }
	_mkT(x, y, s, sc, al, col, a) {
		const ty = textType();
		const i = E.rt.objects[ty].createInstance(this.layer, 0, 0);
		const k = { i, kind: "t", ty, x, y, sc, al, col, a, rot: 0, txt: true, cd: true };
		this._initT(k);
		this._st(k, s); this.k.push(k); return k;
	}
	_initT(k) {
		const i = k.i;
		try { i.horizontalAlign = k.al; i.verticalAlign = "top"; } catch (_) {}
		if (k.ty === "Text") { try { i.wordWrapMode = "character"; i.fontFace = "monospace"; } catch (_) {} }
	}
	t(x, y, s, o = {}) {
		const sc = o.sc ?? 1, al = o.al ?? "left", col = o.col ?? PAL.white, a = o.a ?? 1;
		let sh = null;
		if (o.sh) sh = this._mkT(x + 1, y + 1, s, sc, al, PAL.black, a * 0.85);
		const k = this._mkT(x, y, s, sc, al, col, a);
		k.shk = sh; return k;
	}
	_st(k, s) {
		k.s = String(s); k.i.text = k.s;
		const lines = k.s.split("\n");
		k.tw = Math.max(FW, ...lines.map(textW)); k.th = lines.length * FH;
	}
	setT(k, s) { if (k.s === String(s)) return; this._st(k, s); if (k.shk) this._st(k.shk, s); }
	setCol(k, c) { if (k.col !== c) { k.col = c; k.cd = true; } }
	anim(k, name) { if (k.anim === name) return; k.anim = name; try { k.i.setAnimation(name); } catch (_) {} if (k.fl) try { k.fl.setAnimation(name); } catch (_) {} }
	retype() {
		for (const k of this.k) if (k.kind === "t") {
			const ty = textType(); if (ty === k.ty) continue;
			k.i.destroy(); k.ty = ty; k.i = E.rt.objects[ty].createInstance(this.layer, 0, 0);
			this._initT(k); k.i.text = k.s; k.cd = true;
		}
		this._cf = -1;
	}
	sync() {
		if (this.dead) return;
		const P = this.popper, A = this.a * (P ? P.a : 1);
		let S = this.s, gx = this.x + this.ox, gy = this.y + this.oy;
		if (P && !this.noPop) { S *= P.s; gx = P.x + (gx - P.x) * P.s; gy = P.y + (gy - P.y) * P.s; }
		const sx = S * this.sx, sy = S * this.sy;
		const R = this.rot, cr = Math.cos(R), sr = Math.sin(R), rotd = R !== 0;
		const cc = this.flash !== this._cf || this.dim !== this._cd;
		for (const k of this.k) {
			const i = k.i, v = this.vis && k.vis !== false;
			i.isVisible = v;
			if (!v) { if (k.fl) k.fl.isVisible = false; continue; }
			const op = clamp(k.a * A, 0, 1);
			i.opacity = op;
			let lx, ly, w, h;
			if (k.kind === "t") {
				const sc = k.sc * S, bw = k.tw * sc + FW * sc, bh = k.th * sc + 2;
				if (k.ty === "Text") { i.sizePt = 7.4 * sc; } else i.characterScale = sc;
				lx = k.x * sx; ly = k.y * sy;
				if (k.al === "center") lx -= bw / 2; else if (k.al === "right") lx -= bw;
				w = bw; h = bh;
			} else { lx = k.x * sx; ly = k.y * sy; w = k.w * sx; h = k.h * sy; }
			if (this.flipX && k.kind !== "t") lx = k.kind === "r" ? -lx - w : -lx;
			if (rotd) { const rx = lx * cr - ly * sr, ry = lx * sr + ly * cr; lx = rx; ly = ry; }
			i.x = gx + lx; i.y = gy + ly;
			i.width = (!!k.mir !== !!(this.flipX && k.kind !== "r" && k.kind !== "t")) ? -w : w; i.height = h;
			const ang = R + k.rot;
			if (ang !== 0 || k._ang) { i.angle = ang; k._ang = ang !== 0; }
			if (cc || k.cd) {
				const c = finalCol(k.col, k.kind === "s" || k.kind === "n" ? 0 : this.flash, this.dim);
				if (k.ty === "Text") { try { i.fontColor = c; } catch (_) { i.colorRgb = c; } } else i.colorRgb = c;
				k.cd = false;
			}
			if (k.kind === "s") {
				if (this.flash > 0.02) {
					if (!k.fl) {
						k.fl = E.rt.objects[k.type].createInstance(this.layer, 0, 0);
						try { if (k.anim != null) k.fl.setAnimation(k.anim); if (k.frame != null) { k.fl.animationSpeed = 0; k.fl.animationFrame = k.frame; } k.fl.blendMode = "additive"; } catch (_) {}
					}
					const f = k.fl; f.isVisible = true; f.x = i.x; f.y = i.y; f.width = i.width; f.height = i.height; f.angle = i.angle;
					f.opacity = op * clamp(this.flash, 0, 1);
					try { f.animationFrame = i.animationFrame; } catch (_) {}
				} else if (k.fl) k.fl.isVisible = false;
			}
		}
		this._cf = this.flash; this._cd = this.dim;
	}
	top() { for (const k of this.k) { k.i.moveToTop(); k.fl?.moveToTop(); } }
	clear() { for (const k of this.k) { k.i.destroy(); k.fl?.destroy(); } this.k.length = 0; }
	kill() { if (this.dead) return; this.dead = true; for (const k of this.k) { k.i.destroy(); k.fl?.destroy(); } this.k.length = 0; }
}
export function retypeAll() { for (const g of E.groups) if (!g.dead) g.retype(); for (const g of E.groups) if (!g.dead) g.top(); }

export class Scene {
	constructor() { this.owned = []; this.hots = []; this.btns = []; this.t = 0; }
	g(layer = "UI", x = 0, y = 0) { return new Grp(this, layer, x, y); }
	hot(o) { const h = Object.assign({ en: true, vis: true }, o); this.hots.push(h); return h; }
	unhot(h) { const i = this.hots.indexOf(h); if (i >= 0) this.hots.splice(i, 1); }
	btn(o) { const b = new Button(this, o); this.btns.push(b); return b; }
	enter() {}
	popIn(x = W / 2, y = H / 2, from = 0, dur = 0.22) {
		const P = { a: 0, s: 0.9, x, y }, gs = this.owned.slice(from);
		for (const g of gs) g.popper = P;
		tween(P, { a: 1, s: 1 }, dur, "outBack", 0, () => { for (const g of gs) g.popper = null; });
	}
	tick(dt, active) {}
	_tick(dt, active) { this.t += dt; for (const b of this.btns.slice()) b.update(dt); this.tick(dt, active); }
	exit() {
		for (const o of this.owned) o.kill();
		this.owned.length = 0; this.hots.length = 0; this.btns.length = 0;
	}
}

export class Button {
	constructor(sc, o) {
		this.sc = sc; this.o = o; this.w = o.w; this.h = o.h ?? 20; this.holdT = 0; this.fired = false;
		this.g = sc.g(o.layer ?? "UI", o.x + this.w / 2, o.y + this.h / 2);
		this.draw();
		this.hot = sc.hot({
			x: o.x, y: o.y, w: this.w, h: this.h, en: !o.disabled, tip: o.tip, native: o.native, key: o.key,
			isHold: () => !!o.hold?.(),
			onPress: () => { this.g.sx = 1.08; this.g.sy = 0.86; },
			onClick: () => { if (o.hold?.()) return; this.fire(); },
		});
	}
	fire() {
		if (this.o.sfx !== false) sfx.click();
		this.g.sx = 1.15; this.g.sy = 0.85;
		this.o.onClick?.(this);
	}
	draw() {
		const g = this.g, o = this.o, w = this.w, h = this.h, c = o.col ?? PAL.panel2;
		if (hasAnim("ButtonPanel")) this.bg = g.np("ButtonPanel", -w / 2, -h / 2, w, h, { col: c });
		this.fill = g.r(-w / 2, -h / 2, 0, h, 0xffffff, 0.35);
		const lx = o.drawIcon ? 7 : 0;
		if (o.drawIcon) o.drawIcon(g, -w / 2 + 20, 0);
		this.lab = g.t(lx, -FH / 2, o.label ?? "", { al: "center", col: o.tcol ?? PAL.white, sh: true });
	}
	setLabel(s) { this.g.setT(this.lab, s); }
	setEnabled(b) { this.hot.en = b; }
	move(x, y) { this.o.x = x; this.o.y = y; this.hot.x = x; this.hot.y = y; this.g.x = x + this.w / 2; this.g.y = y + this.h / 2; }
	update(dt) {
		const hv = E.hovered === this.hot, en = this.hot.en, vis = this.hot.vis;
		this.g.vis = vis;
		const pr = hv && E.mouse.down && E.pressHot === this.hot;
		if (this.o.hold?.() && en) {
			const holding = pr || (this.o.key && (E.keys.has(this.o.key) || E.keys.has(this.o.key.toUpperCase())));
			if (holding && !this.fired) {
				this.holdT += dt; sfx.holdTick(this.holdT / 0.5);
				if (this.holdT >= 0.5) { this.fired = true; this.holdT = 0; this.fire(); }
			} else if (!holding) { this.holdT = Math.max(0, this.holdT - dt * 3); this.fired = false; }
			this.fill.w = this.w * clamp(this.holdT / 0.5, 0, 1);
		} else this.fill.w = 0;
		const tgt = !en ? 1 : pr ? 0.94 : hv ? 1.07 : 1 + (this.o.pulse ? Math.sin(E.t * 3) * 0.015 : 0);
		this.g.s = damp(this.g.s, tgt, 22, dt);
		this.g.sx = damp(this.g.sx, 1, 14, dt); this.g.sy = damp(this.g.sy, 1, 14, dt);
		this.g.oy = damp(this.g.oy, hv && en ? -1 : 0, 20, dt);
		this.g.dim = en ? (hv ? 1.2 : 1) : 0.45;
	}
	kill() { this.g.kill(); this.sc.unhot(this.hot); const i = this.sc.btns.indexOf(this); if (i >= 0) this.sc.btns.splice(i, 1); }
}

export const EASE = {
	linear: t => t, inQuad: t => t * t, outQuad: t => t * (2 - t),
	inOutQuad: t => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t,
	outCubic: t => 1 - Math.pow(1 - t, 3), inCubic: t => t * t * t,
	outBack: t => { const c = 1.1, c3 = c + 1; return 1 + c3 * Math.pow(t - 1, 3) + c * Math.pow(t - 1, 2); },
	inBack: t => { const c = 1.70158; return (c + 1) * t * t * t - c * t * t; },
	outElastic: t => t === 0 ? 0 : t === 1 ? 1 : Math.pow(2, -10 * t) * Math.sin((t * 10 - 0.75) * (2 * Math.PI) / 3) + 1,
	inOutSine: t => -(Math.cos(Math.PI * t) - 1) / 2,
};
export function tween(o, to, dur, ease = "outQuad", delay = 0, done = null) {
	const tw = { o, to, from: null, dur: Math.max(0.001, dur), t: -delay, ease: EASE[ease] || EASE.linear, done, dead: false };
	E.tweens.push(tw); return tw;
}
export function untween(o) { for (const tw of E.tweens) if (tw.o === o) tw.dead = true; }
export const tweening = o => E.tweens.some(t => t.o === o && !t.dead);
function stepTweens(dt) {
	const list = E.tweens, n = list.length;
	for (let i = 0; i < n; i++) {
		const tw = list[i];
		if (tw.dead) continue;
		if (tw.o && tw.o.dead) { tw.dead = true; continue; }
		tw.t += dt;
		if (tw.t < 0) continue;
		if (!tw.from) { tw.from = {}; for (const k in tw.to) tw.from[k] = tw.o[k]; }
		const p = Math.min(1, tw.t / tw.dur), e = tw.ease(p);
		for (const k in tw.to) tw.o[k] = tw.from[k] + (tw.to[k] - tw.from[k]) * e;
		if (p >= 1) { tw.dead = true; if (tw.done) tw.done(); }
	}
	if (E.frame % 30 === 0) E.tweens = E.tweens.filter(t => !t.dead);
}

export function wait(ms) { return new Promise(r => E.waits.push({ t: ms / 1000, r })); }
export function after(sec, fn) { E.waits.push({ t: sec, r: fn }); }
function stepWaits(dt) {
	if (!E.waits.length) return;
	const done = [];
	for (const w of E.waits) { w.t -= dt; if (w.t <= 0) done.push(w); }
	if (done.length) { E.waits = E.waits.filter(w => w.t > 0); for (const w of done) w.r(); }
}

// every particle is this one Particle frame, just tinted. change the name if you draw a new dot
export const PARTICLE = "snow";
const PARTS = [], POOL = [];
export function part(x, y, o = {}) {
	if (PARTS.length >= 420 * E.settings.particles || !hasAnim("Particle", PARTICLE)) return;
	let i = POOL.pop();
	if (!i) {
		i = E.rt.objects.Particle.createInstance("FX", x, y);
		try { i.setAnimation(PARTICLE); i.animationSpeed = 0; i.animationFrame = 0; } catch (_) {}
	}
	const sz = o.sz ?? 2;
	i.isVisible = true; i.colorRgb = rgb(o.col ?? 0xffffff); i.opacity = 1; i.angle = 0;
	try { i.blendMode = o.add ? "additive" : "normal"; } catch (_) {}
	const life = o.life ?? 0.6;
	PARTS.push({ i, x, y, vx: o.vx ?? 0, vy: o.vy ?? 0, g: o.g ?? 0, drag: o.drag ?? 0, life, max: life, sz, shrink: o.shrink !== false, a: o.a ?? 1, spin: o.spin ?? 0, rot: 0, floor: o.floor });
}
export function burst(x, y, n, o = {}) {
	n = Math.round(n * (E.settings.reduceMotion ? 0.35 : 1) * E.settings.particles);
	const cols = o.cols || [0xffffff], s0 = o.spd?.[0] ?? 40, s1 = o.spd?.[1] ?? 140;
	const a0 = o.a0 ?? 0, a1 = o.a1 ?? Math.PI * 2;
	for (let k = 0; k < n; k++) {
		const a = a0 + Math.random() * (a1 - a0), s = s0 + Math.random() * (s1 - s0);
		part(x + (Math.random() - 0.5) * (o.jx || 0), y + (Math.random() - 0.5) * (o.jy || 0), {
			vx: Math.cos(a) * s, vy: Math.sin(a) * s, g: o.g ?? 220, drag: o.drag ?? 1.5,
			life: (o.life || 0.6) * (0.6 + Math.random() * 0.8), sz: (o.sz || 2) + Math.floor(Math.random() * (o.szr ?? 2)),
			col: cols[(Math.random() * cols.length) | 0], add: o.add, shrink: o.shrink, spin: o.spin ? rf(-o.spin, o.spin) : 0, floor: o.floor,
		});
	}
}
export function confetti(x, y, n, cols, o = {}) {
	burst(x, y, n, { cols, spd: o.spd ?? [80, 240], a0: o.a0 ?? -Math.PI * 0.95, a1: o.a1 ?? -Math.PI * 0.05, g: 380, drag: 1.2, life: o.life ?? 1.2, sz: 2, szr: 2, spin: 12, shrink: false });
}
export function fx(name, x, y, o = {}) {
	if (!hasAnim("Particle", name)) return false;
	const g = new Grp(null, o.layer || "FX", x, y);
	g.sp("Particle", 0, 0, { anim: name, sc: o.sc ?? 1, rot: o.rot ?? 0, col: o.col });
	tween(g, { a: o.fade === false ? 1 : 0 }, o.life ?? 0.6, "inQuad", 0, () => g.kill());
	return true;
}
function stepParts(dt) {
	for (let k = PARTS.length - 1; k >= 0; k--) {
		const p = PARTS[k];
		p.life -= dt;
		if (p.life <= 0) { p.i.isVisible = false; POOL.push(p.i); PARTS.splice(k, 1); continue; }
		p.vy += p.g * dt;
		const d = Math.max(0, 1 - p.drag * dt);
		p.vx *= d; p.vy *= d; p.x += p.vx * dt; p.y += p.vy * dt;
		if (p.floor != null && p.y > p.floor && p.vy > 0) { p.y = p.floor; p.vy *= -0.35; p.vx *= 0.6; }
		const f = p.life / p.max, s = p.shrink ? Math.max(1, Math.round(p.sz * Math.min(1, f * 1.5))) : p.sz;
		p.i.x = p.x; p.i.y = p.y; p.i.width = s; p.i.height = p.spin ? Math.max(1, s * Math.abs(Math.cos(p.rot += p.spin * dt))) : s;
		p.i.opacity = p.a * Math.min(1, f * 2);
	}
}

export function shake(m, dx = 0, dy = 0) {
	if (E.settings.reduceMotion) return;
	const s = E.settings.shake;
	E.shk = Math.min(14, Math.max(E.shk, m * s));
	E.kx += dx * s; E.ky += dy * s;
}
export function zoomPunch(z = 0.04) { if (!E.settings.reduceMotion) E.zoom = Math.max(E.zoom, z * E.settings.shake); }
export function hitstop(s) { if (!E.settings.reduceMotion) E.hs = Math.max(E.hs, s); }
export function flash(col = 0xffffff, a = 0.6, dur = 0.25) {
	if (!E.settings.flash) a *= 0.2;
	const g = new Grp(null, "FX", 0, 0);
	g.r(0, 0, W, H, col, a);
	tween(g, { a: 0 }, dur, "outQuad", 0, () => g.kill());
}
export function floatText(x, y, s, col = PAL.white, o = {}) {
	if (o.num && !E.settings.damageNumbers) return null;
	const g = new Grp(null, "FX", x, y);
	g.t(0, 0, s, { al: "center", col, sh: true, sc: o.sc || 1 });
	const base = o.big ? 1.6 : 1;
	g.s = base * (o.big ? 1.5 : 1.3);
	tween(g, { s: base }, 0.3, "outBack");
	if (o.pop && !E.settings.reduceMotion) {
		
		E.floats.push({ g, vx: rf(-50, 50) + (o.dir || 0) * 60, vy: -150 - (o.big ? 70 : 0), floor: y + 18, life: o.life ?? 1.0, max: o.life ?? 1.0 });
	} else {
		tween(g, { y: y - (o.rise ?? 26) }, 0.9, "outCubic");
		tween(g, { a: 0 }, 0.3, "linear", o.hold ?? 0.65, () => g.kill());
	}
	return g;
}
function stepFloats(dt) {
	for (let k = E.floats.length - 1; k >= 0; k--) {
		const f = E.floats[k], g = f.g;
		f.life -= dt;
		f.vy += 520 * dt; g.x += f.vx * dt; g.y += f.vy * dt;
		if (g.y > f.floor && f.vy > 0) { g.y = f.floor; f.vy *= -0.42; f.vx *= 0.5; g.rot *= 0.5; }
		g.a = clamp(f.life / 0.3, 0, 1);
		if (f.life <= 0) { g.kill(); E.floats.splice(k, 1); }
	}
}

export class Snow {
	constructor(scene, n = 70, layer = "BG", o = {}) {
		this.g = scene.g(layer, 0, 0); this.f = []; this.wind = o.wind ?? 8;
		n = Math.round(n * (0.4 + 0.6 * E.settings.particles));
		if (!hasAnim("Particle", PARTICLE)) n = 0;
		for (let i = 0; i < n; i++) {
			const z = Math.random();
			const k = this.g.sp("Particle", Math.random() * W, Math.random() * H, { anim: PARTICLE, sc: 0.5 + z * 0.5, a: 0.35 + z * 0.6, col: z < 0.5 ? PAL.snow2 : PAL.snow });
			k.vy = 8 + z * 22; k.ph = Math.random() * 6.28; k.amp = 4 + z * 10; k.z = z; this.f.push(k);
		}
	}
	update(dt) {
		const mx = (E.mouse.x - W / 2) / W;
		for (const k of this.f) {
			k.y += k.vy * dt; k.x += (Math.sin(E.t * 1.3 + k.ph) * k.amp * 0.5 + this.wind + mx * 14 * k.z) * dt;
			if (k.y > H) { k.y = -2; k.x = Math.random() * W; }
			if (k.x > W) k.x -= W; if (k.x < 0) k.x += W;
		}
	}
}

export function activeScene() { return E.overlays.length ? E.overlays[E.overlays.length - 1] : E.scene; }
export function pushOverlay(s) { E.overlays.push(s); s.enter(); E.hovered = null; E.pressHot = null; return s; }
export function popOverlay(s) {
	const i = E.overlays.indexOf(s);
	if (i >= 0) { E.overlays.splice(i, 1); s.exit(); }
	E.hovered = null; E.pressHot = null;
}
function swapScene(make) {
	while (E.overlays.length) E.overlays.pop().exit();
	if (E.scene) E.scene.exit();
	E.hovered = null; E.pressHot = null; E.tipOverride = null; E.tempo = 1;
	E.scene = make(); E.scene.enter();
}
export function go(make, instant = false) {
	if (E.trans) return;
	if (instant || !E.scene) { swapScene(make); return; }
	const g = new Grp(null, "FX", 0, 0), kids = [];
	for (let i = 0; i < 16; i++) kids.push(g.r(i * 40, 0, 40, 0, i % 2 ? PAL.night0 : PAL.night1));
	E.trans = { make, t: 0, phase: 0, g, kids };
	sfx.whoosh();
}
function stepTrans(dt) {
	const T = E.trans; if (!T) return;
	T.t += dt;
	const D = 0.24, ST = 0.012, total = D + T.kids.length * ST;
	for (let i = 0; i < T.kids.length; i++) {
		const k = T.kids[i], p = clamp((T.t - i * ST) / D, 0, 1);
		if (T.phase === 0) { k.y = 0; k.h = H * EASE.outCubic(p); }
		else { k.y = H * EASE.inCubic(p); k.h = H; }
	}
	if (T.phase === 0 && T.t >= total) { T.g.top(); swapScene(T.make); T.g.top(); T.phase = 1; T.t = -0.06; }
	else if (T.phase === 1 && T.t >= total) { T.g.kill(); E.trans = null; }
}

const TIP_OL = 4, TIP_GAP = TIP_OL * 2 + 1;
function tipBox(title, col, body, wc) {
	const lines = [];
	if (title) lines.push({ s: title, c: col ?? PAL.gold });
	const arr = Array.isArray(body) ? body : body ? [body] : [];
	for (const it of arr) {
		const o = typeof it === "string" ? { s: it } : it;
		for (const l of wrap(o.s, wc).split("\n")) lines.push({ s: l, c: o.c ?? PAL.white });
	}
	const w = Math.max(...lines.map(l => textW(l.s))) + 10, h = lines.length * FH + 8;
	const g = new Grp(null, "FX", 0, 0);
	g.r(-4, -4, w + 8, h + 8, PAL.black);
	g.r(-2, -2, w + 4, h + 4, col ?? PAL.gold);
	g.r(0, 0, w, h, PAL.black, 0.96);
	lines.forEach((l, i) => g.t(5, 4 + i * FH, l.s, { col: l.c === PAL.mute ? MUTE() : l.c }));
	g.tw = w; g.th = h; g.a = 0; g.s = 0.94;
	tween(g, { a: 1, s: 1 }, 0.12, "outCubic");
	return g;
}
function buildTip(req) {
	const main = tipBox(req.t, req.c, req.b, req.w || 30);
	const kws = (req.kw || []).map((k, i) => { const g = tipBox(k.t, k.c ?? PAL.mute, k.b, 22); g.a = 0; tween(g, { a: 1 }, 0.15, "linear", 0.05 + i * 0.03); return g; });
	return { main, kws, kill() { main.kill(); for (const k of kws) k.kill(); } };
}
function updateTip() {
	let req = E.tipOverride; E.tipOverride = null;
	if (!req && E.hovered && E.hovered.tip) req = typeof E.hovered.tip === "function" ? E.hovered.tip() : E.hovered.tip;
	if (E.trans) req = null;
	const key = req ? (req.key || req.t + "|" + JSON.stringify(req.b) + "|" + (req.kw || []).length) : "";
	if (key !== E.tipKey) { E.tip?.kill(); E.tip = null; E.tipKey = key; if (req) E.tip = buildTip(req); }
	const T = E.tip; if (!T) return;
	const g = T.main;
	let x, y;
	if (req.at) { x = req.at[0] - g.tw / 2; y = req.at[1] - g.th; }
	else { x = E.mouse.x + 11; y = E.mouse.y + 13; if (x + g.tw > W - 2) x = E.mouse.x - g.tw - 7; if (y + g.th > H - 2) y = E.mouse.y - g.th - 5; }
	const M = TIP_OL + 2;
	x = clamp(x, M, W - g.tw - M); y = clamp(y, M, H - g.th - M);
	g.x = x; g.y = y;
	if (T.kws.length) {
		const kw = Math.max(...T.kws.map(k => k.tw));
		const tot = T.kws.reduce((s, k) => s + k.th + TIP_GAP, 0) - TIP_GAP;
		let kx = x + g.tw + TIP_GAP, ky = clamp(y, M, Math.max(M, H - tot - M));
		if (kx + kw > W - M) kx = x - kw - TIP_GAP;
		if (kx < M) { kx = clamp(x, M, W - kw - M); ky = y + g.th + TIP_GAP; }
		for (const k of T.kws) { k.x = kx; k.y = ky; ky += k.th + TIP_GAP; }
	}
	g.top(); for (const k of T.kws) k.top();
}

function processHots(act) {
	const m = E.mouse; let hov = null;
	if (m.rpressed && act && act !== E.scene && act.onBack) { act.onBack(); m.rpressed = false; sfx.click(); return; }
	if (act) for (let i = act.hots.length - 1; i >= 0; i--) {
		const h = act.hots[i];
		if (!h.vis) continue;
		if (m.x >= h.x && m.x < h.x + h.w && m.y >= h.y && m.y < h.y + h.h) { hov = h; break; }
	}
	if (hov !== E.hovered) {
		E.hovered?.onHover?.(false);
		if (hov) { hov.onHover?.(true); if (hov.en && hov.onClick && hov.sfx !== false) sfx.hover(); }
		E.hovered = hov;
	}
	if (m.pressed) { E.pressHot = hov; if (hov?.en) hov.onPress?.(); }
	if (m.released) { const p = E.pressHot; E.pressHot = null; if (p && p === hov && hov.en) hov.onClick?.(); }
	if (act && E.kp.size) for (const h of act.hots) {
		if (!h.key || !h.en || !h.vis || h.isHold?.()) continue;
		if (E.kp.has(h.key) || E.kp.has(h.key.toUpperCase())) { E.kp.delete(h.key); h.onClick?.(); break; }
	}
}

export function applyVideo() {
	if (!E.rt) return;
	if (E.settings.crt && !E.crtG) {
		const g = E.crtG = new Grp(null, "FX", 0, 0);
		for (let y = 0; y < H; y += 3) g.r(0, y, W, 1, 0x000000, 0.12);
		for (let i = 0; i < 8; i++) { const a = 0.05 * (8 - i); g.r(0, i, W, 1, 0, a); g.r(0, H - 1 - i, W, 1, 0, a); g.r(i, 0, 1, H, 0, a); g.r(W - 1 - i, 0, 1, H, 0, a); }
	} else if (!E.settings.crt && E.crtG) { E.crtG.kill(); E.crtG = null; }
}

export function resetEngine() {
	E.scene = null; E.overlays.length = 0; E.groups.length = 0; E.tweens.length = 0; E.waits.length = 0; E.floats.length = 0;
	E.hovered = E.pressHot = E.tip = E.trans = null; E.tipKey = ""; E.crtG = null;
	E.shk = E.kx = E.ky = E.zoom = E.hs = 0; E._cx = E._cy = E._z = undefined;
	PARTS.length = 0; POOL.length = 0; PAD.g = null; PAD.focus = null;
}
export function initEngine(rt) {
	E.rt = rt; loadSettings();
	if (E.inited) { applyVideo(); return; }
	E.inited = true;
	const ui = () => rt.layout.getLayer("UI");
	let lx = 0, ly = 0;
	const setM = e => { try { const p = ui().cssPxToLayer(e.clientX, e.clientY); E.mouse.x = p[0]; E.mouse.y = p[1]; } catch (_) {} };
	rt.addEventListener("pointermove", e => { setM(e); PAD.active = false; });
	rt.addEventListener("pointerdown", e => {
		setM(e); audioUnlock(); PAD.active = false;
		E.touch = e.pointerType === "touch";
		if (e.button === 2) { E.mouse.rpressed = true; return; }
		E.mouse.down = true; E.mouse.pressed = true; E.mouse.sx = E.mouse.x; E.mouse.sy = E.mouse.y;
	});
	rt.addEventListener("pointerup", e => { setM(e); if (e.button === 2) return; E.mouse.down = false; E.mouse.released = true; if (e.pointerType === "touch") E.touchLift = true; });
	rt.addEventListener("pointercancel", () => { E.mouse.down = false; });
	rt.addEventListener("keydown", e => { audioUnlock(); const k = e.key.length === 1 ? e.key.toLowerCase() : e.key; if (!E.keys.has(k)) E.kp.add(k); E.keys.add(k); });
	rt.addEventListener("keyup", e => { const k = e.key.length === 1 ? e.key.toLowerCase() : e.key; E.keys.delete(k); });
	if (typeof window !== "undefined" && window.addEventListener) {
		window.addEventListener("wheel", e => { E.mouse.wheel += e.deltaY; }, { passive: true });
		window.addEventListener("contextmenu", e => e.preventDefault());
		window.addEventListener("pointerup", () => {
			const h = E.hovered;
			if (h && h.native && h.en && E.pressHot === h) { try { h.native(); } catch (err) { console.warn(err); } }
		}, true);
		window.addEventListener("keydown", e => { if (e.key === "F11") { e.preventDefault(); toggleFullscreen(); } if (e.key === "Tab") e.preventDefault(); });
	} else {
		rt.addEventListener("wheel", e => { E.mouse.wheel += e.deltaY; });
		console.warn("[Sleigh] Running in a worker. Set Project > Advanced > Use worker = No.");
	}
	E._lx = () => { E.mouse.vx = (E.mouse.x - lx) / Math.max(E.dt, 1 / 120); E.mouse.vy = (E.mouse.y - ly) / Math.max(E.dt, 1 / 120); lx = E.mouse.x; ly = E.mouse.y; };
	applyVideo();
}

export function engineTick() {
	const rdt = Math.min(E.rt.dt, 1 / 20);
	E.dt = rdt; E.t += rdt; E.frame++;
	E._lx?.();
	if (E.hs > 0) { E.hs -= rdt; E.dts = 0; } else E.dts = rdt * E.settings.gameSpeed * E.tempo;
	stepWaits(E.dts);
	stepTweens(E.dts);
	const act = activeScene();
	pollPad(act);
	if (!E.trans) processHots(act); else E.hovered = null;
	if (E.scene) E.scene._tick(rdt, act === E.scene && !E.trans);
	for (const o of E.overlays.slice()) o._tick(rdt, act === o && !E.trans);
	stepTrans(rdt);
	stepParts(E.dts);
	stepFloats(E.hs > 0 ? 0 : rdt);
	updateTip();
	E.kx = damp(E.kx, 0, 14, rdt); E.ky = damp(E.ky, 0, 14, rdt);
	if (E.shk > 0.05) E.shk = Math.max(0, E.shk - rdt * (E.shk * 7 + 3)); else E.shk = 0;
	const sx = E.shk ? (Math.random() * 2 - 1) * E.shk : 0, sy = E.shk ? (Math.random() * 2 - 1) * E.shk : 0;
	const cx = W / 2 + sx + E.kx, cy = H / 2 + sy + E.ky;
	if (cx !== E._cx || cy !== E._cy) { E.rt.layout.scrollTo(cx, cy); E._cx = cx; E._cy = cy; }
	E.zoom = damp(E.zoom, 0, 9, rdt);
	const z = 1 + (E.zoom < 0.0005 ? 0 : E.zoom);
	if (z !== E._z) { try { E.rt.layout.scale = z; } catch (_) {} E._z = z; }
	let dead = 0;
	for (const g of E.groups) { if (g.dead) dead++; else g.sync(); }
	if (dead > 40) E.groups = E.groups.filter(g => !g.dead);
	drawPadFocus();
	const m = E.mouse; m.pressed = m.released = m.rpressed = false; m.wheel = 0; E.kp.clear();
	if (E.touchLift) { E.touchLift = false; m.x = -999; m.y = -999; }
}

export const PAD = { prev: [], focus: null, rep: 0, active: false, click: 0, g: null };
function padTargets(act) {
	const out = [];
	if (!act) return out;
	for (const h of act.hots) if (h.vis && (h.en && h.onClick || h.tip) && h.w < W - 4 && h.x + h.w > 0 && h.x < W)
		out.push({ x: h.x + h.w / 2, y: h.y + h.h / 2, w: h.w, h: h.h, ref: h, click: !!(h.en && h.onClick) });
	for (const t of act.navTargets?.() || []) out.push(t);
	return out;
}
function padMove(T, f, dx, dy) {
	let best = null, bs = 1e9;
	for (const t of T) {
		if (t === f) continue;
		const vx = t.x - f.x, vy = t.y - f.y, along = vx * dx + vy * dy;
		if (along <= 2) continue;
		const across = Math.abs(vx * dy - vy * dx), s = along + across * 2.5 + (t.click ? 0 : 40);
		if (s < bs) { bs = s; best = t; }
	}
	return best || f;
}
function pollPad(act) {
	if (PAD.click) { E.mouse.down = false; E.mouse.released = true; PAD.click = 0; }
	let gp = null;
	try { gp = typeof navigator !== "undefined" && navigator.getGamepads ? [...navigator.getGamepads()].find(g => g && g.connected) : null; } catch (_) {}
	if (!gp) return;
	const prev = PAD.prev, b = i => !!gp.buttons[i]?.pressed, pr = i => b(i) && !prev[i];
	const ax = gp.axes[0] || 0, ay = gp.axes[1] || 0, ry = gp.axes[3] || 0;
	const dx = clamp((b(15) ? 1 : 0) - (b(14) ? 1 : 0) + (Math.abs(ax) > 0.5 ? Math.sign(ax) : 0), -1, 1);
	const dy = clamp((b(13) ? 1 : 0) - (b(12) ? 1 : 0) + (Math.abs(ay) > 0.5 ? Math.sign(ay) : 0), -1, 1);
	if (gp.buttons.some(x => x.pressed) || dx || dy || Math.abs(ry) > 0.3) PAD.active = true;
	PAD.prev = gp.buttons.map(x => x.pressed);
	if (!PAD.active || E.trans) return;
	const T = padTargets(act);
	let f = T.find(t => t.ref === PAD.focus?.ref);
	if (!f) {
		const d = act?.navDefault?.();
		f = (d && T.find(t => t.ref === d)) || T.filter(t => t.click).sort((a, c) => Math.hypot(a.x - W / 2, a.y - H * 0.6) - Math.hypot(c.x - W / 2, c.y - H * 0.6))[0] || T[0] || null;
	}
	if (dx || dy) {
		if (PAD.rep <= 0) { if (f) { const n = padMove(T, f, dx, dy); if (n !== f) sfx.hover(); f = n; } PAD.rep = PAD.held ? 0.11 : 0.3; PAD.held = true; }
		else PAD.rep -= E.dt;
	} else { PAD.rep = 0; PAD.held = false; }
	PAD.focus = f;
	if (f) { E.mouse.x = f.x; E.mouse.y = f.y; }
	if (pr(0) && f) { E.mouse.pressed = true; E.mouse.down = true; E.mouse.sx = f.x; E.mouse.sy = f.y; PAD.click = 1; }
	if (pr(1)) E.mouse.rpressed = true;
	if (pr(3)) E.kp.add("e");
	if (pr(9)) E.kp.add("Escape");
	if (pr(4) || pr(5)) E.kp.add("Tab");
	if (Math.abs(ry) > 0.3) E.mouse.wheel += ry * 18;
}
function drawPadFocus() {
	const f = PAD.active ? PAD.focus : null;
	if (!f) { if (PAD.g) PAD.g.vis = false; return; }
	if (!PAD.g || PAD.g.dead) {
		PAD.g = new Grp(null, "FX", 0, 0);
		PAD.corners = [];
		for (let i = 0; i < 8; i++) PAD.corners.push(PAD.g.r(0, 0, 1, 1, PAL.gold));
	}
	const g = PAD.g, p = 2 + Math.sin(E.t * 8) * 1.5, w = (f.w || 20) / 2 + p, h = (f.h || 20) / 2 + p, L = 6;
	g.vis = true; g.x = f.x; g.y = f.y;
	const c = PAD.corners, set = (k, x, y, ww, hh) => { k.x = x; k.y = y; k.w = ww; k.h = hh; };
	set(c[0], -w, -h, L, 2); set(c[1], -w, -h, 2, L); set(c[2], w - L, -h, L, 2); set(c[3], w - 2, -h, 2, L);
	set(c[4], -w, h - 2, L, 2); set(c[5], -w, h - L, 2, L); set(c[6], w - L, h - 2, L, 2); set(c[7], w - 2, h - L, 2, L);
	g.top();
}