import { E, W, H, FH, PAL, MUTE, Scene, tween, wait, after, go, pushOverlay, shake, flash, burst, floatText,
	Snow, clamp, wrap, rf, damp, store, hasType, hasAnim, zoomPunch, fx, PAD } from "./engine.js";
import { sfx, music } from "./audio.js";
import { icon, potionIcon, partIcon, creature, backdrop, bgimg, tiledBg, sceneSprite } from "./art.js";
import { GIFTS, RELICS, POTIONS, findKeywords } from "./data.js";
import { G, RUN, NIGHTS, newRun, enterNode, finishNode, startFight, NODE_INFO, nightsLeft, dateLabel, spiritTier, ri, rng, pick,
	addGift, removeGift, upgradeGift, canUpgrade, joyOf, leaveGift, leaveCookies, leaveCookieCost, canSkipLeave, LEAVE_MIN, addSpirit,
	giftChoices, randGiftId, randRelicId, randPotionId, addRelic, addPotion, hasRelic, gainCookies, spendCookies, heal, rollRarity,
	pickWish, wishOk, wishFor, canPart, addFriend, adv, adventUnlocked, unlockNextAdvent } from "./run.js";
import { HUD, makeCard, giftTip, bubble, panel, GiftPicker, ChooseGift, ChoosePart, addPartFlow, openSettings, openLedger, HelpOverlay, CW, CH } from "./ui.js";
import { CHARS, CHAR_ORDER, curChar } from "./characters.js";
import { PARTS, partIds, partPrice, WISH_BONUS, KIDS, kidOf } from "./parts.js";
import { fid } from "./friends.js";
import { RELICS as RELIC_DEFS } from "./data.js";

export function goTitle() { G.active = false; go(() => new TitleScene()); }
const sortBag = l => l.slice().sort((a, b) => { const A = GIFTS[a.id], B = GIFTS[b.id]; return A.type.localeCompare(B.type) || A.name.localeCompare(B.name); });

function village(sc, key, ground = 300, lit = true) {
	if (backdrop(sc, key)) return;
	const g = sc.g("BG", 0, 0);
	bgimg(g, "moon", 520, 76);
	for (let i = 0; i < 5; i++) bgimg(g, lit && i % 2 === 0 ? "house_lit" : "house", 60 + i * 130, ground - 22);
}
function startRun() { sfx.jingle(); go(() => new CharSelectScene()); }
function beginRun(charId, advent = 0) {
	newRun(undefined, charId, advent);
	if (E.settings.skipPoem && store.get("seenPoem")) go(() => new MapScene());
	else go(() => new IntroScene());
}

export class TitleScene extends Scene {
	enter() {
		music.play("calm");
		this.tile = tiledBg(this);
		if (!this.tile) village(this, "title", 300);
		this.sleigh = this.g("BG", -80, 50);
		const sk = bgimg(this.sleigh, "sleighs", 0, 0);
		if (sk) sk.x = sk.w / 2;
		this.snow = new Snow(this, 90, "World");
		this.title = this.g("UI", W / 2, 70);
		let lw = 400, lh = 100;
		if (hasType("GameLogo")) { const k = this.title.sp("GameLogo", 0, 0); lw = k.w; lh = k.h; }
		else this.title.t(0, -24, "SLEIGH THE SPIRE", { al: "center", sc: 3, col: PAL.red, sh: true });
		const ly = Math.max(70, Math.round(lh / 2) + 18);
		this.title.y = ly;
		this.title.s = 0.92; this.title.a = 0;
		tween(this.title, { s: 1, a: 1 }, 0.6, "outCubic");
		after(0.15, () => sfx.bell(84));
		this.titleHot = this.hot({ x: W / 2 - lw / 2, y: ly - lh / 2, w: lw, h: lh, sfx: false, onClick: () => { this.title.s = 1.05; sfx.jingle(); } });
		const bx = W / 2 - 80, by = Math.min(H - 122, Math.max(200, Math.round(ly + lh / 2 + 16)));
		const btns = [
			this.btn({ x: bx, y: by, w: 160, h: 24, label: "NEW RUN", col: PAL.red2, pulse: true, key: "Enter", onClick: () => startRun() }),
			this.btn({ x: bx, y: by + 32, w: 160, h: 22, label: "HOW TO PLAY", onClick: () => pushOverlay(new HelpOverlay()) }),
			this.btn({ x: bx, y: by + 62, w: 160, h: 22, label: "SETTINGS", onClick: () => openSettings(false) }),
			this.btn({ x: bx, y: by + 90, w: 160, h: 22, label: "LEDGER", onClick: () => openLedger() }),
		];
		btns.forEach((b, i) => { b.g.oy = 8; b.g.a = 0; tween(b.g, { oy: 0, a: 1 }, 0.3, "outCubic", 0.3 + i * 0.06); });
	}
	tick(dt) {
		this.snow.update(dt);
		if (!E.tweens.some(t => t.o === this.title && !t.dead)) {
			this.title.oy = Math.sin(this.t * 1.5) * 3;
			this.title.s = damp(this.title.s, E.hovered === this.titleHot ? 1.02 : 1, 10, dt);
		}
		this.sleigh.x += dt * 40; this.sleigh.y = 50 + Math.sin(this.t) * 6; if (this.sleigh.x > W + 40) this.sleigh.x = -300;
	}
}

const POEM_HEAD = [
	["'Twas the night before Christmas, and all through the land,", "Not a sleigh bell was jingling, not a reindeer at hand.", "For Santa had read the long Naughty List through,", "And each name that he read made his heart darker, too."],
];
const poemFor = ch => [POEM_HEAD[0], ["Till one frosty midnight, he grabbed his big sack", "And ran off the North Pole with the coal on his back.", ...ch.stanza2], ch.stanza3];

// both poems use this: lines fade in at the bottom and drift up, hold to speed up
class Scroller {
	constructor(sc, stanzas, o = {}) {
		this.sc = sc; this.lines = []; let off = 0;
		stanzas.forEach((st, si) => { st.forEach(s => { this.lines.push({ s, at: off, g: null }); off += 20; }); off += si < stanzas.length - 1 ? 16 : 0; });
		this.y0 = Math.round(H * 0.8); this.top = o.top ?? 34; this.scroll = -1; this.spawned = 0; this.done = false;
		this.stopAt = this.lines[this.lines.length - 1].at + this.y0 + 20;
	}
	skip() { if (this.done) return; for (const L of this.lines) if (L.g) { const g = L.g; tween(g, { a: 0 }, 0.25, "linear", 0, () => g.kill()); } this.spawned = this.lines.length; this.scroll = this.stopAt; this.done = true; }
	update(dt, active) {
		if (this.done) return false;
		const base = E.settings.textSpeed >= 3 ? 60 : 13 * E.settings.textSpeed;
		const fast = active && (E.mouse.down || E.keys.has(" ") || E.keys.has("Enter")) ? 5 : 1;
		this.scroll = Math.min(this.stopAt, this.scroll + base * fast * dt);
		while (this.spawned < this.lines.length && this.scroll >= this.lines[this.spawned].at) {
			const L = this.lines[this.spawned++];
			L.g = this.sc.g("UI", W / 2, 0); L.g.t(0, 0, L.s, { al: "center", col: this.spawned % 2 ? PAL.cream : PAL.white, sh: true });
			L.g.a = 0; L.g.s = 0.85; tween(L.g, { a: 1, s: 1 }, 0.6, "outCubic");
		}
		for (const L of this.lines) if (L.g && !L.g.dead) {
			L.g.y = this.y0 + L.at - this.scroll;
			if (!E.tweens.some(t => t.o === L.g && !t.dead)) L.g.a = clamp((L.g.y - this.top) / 50, 0, 1);
		}
		if (this.spawned === this.lines.length && this.scroll >= this.stopAt) { this.done = true; return true; }
		return false;
	}
}

export class IntroScene extends Scene {
	enter() {
		music.play("calm");
		if (!tiledBg(this, W + 200, -100, 0, H, "IntroBG")) backdrop(this, "intro");
		bgimg(this.g("BG", 0, 0), "moon", 540, 76);
		this.snow = new Snow(this, 60, "BG");
		this.poem = new Scroller(this, poemFor(curChar()));
		this.btn({ x: W - 110, y: H - 30, w: 100, h: 20, label: "SKIP", key: "Escape", onClick: () => this.finish() });
		this.hint = this.g("UI", W / 2, H - 22); this.hintK = this.hint.t(0, 0, "hold to speed up", { al: "center", col: MUTE() });
	}
	finish() { if (this.done) return; this.done = true; store.set("seenPoem", true); sfx.jingle(); go(() => new MapScene()); }
	tick(dt, active) {
		this.snow.update(dt);
		if (this.poem.update(dt, active)) after(0.4, () => this.finish());
		this.hint.a = 0.5 + 0.5 * Math.sin(this.t * 3);
	}
}

export class CharSelectScene extends Scene {
	enter() {
		music.play("calm");
		this.idx = 0; this.spotX = 320;
		backdrop(this, "stage");
		this.spot = this.g("World", 320, 0);
		this.spotK = bgimg(this.spot, "spotlight", 0, 0);
		this.actors = CHAR_ORDER.map((id, i) => {
			const ch = CHARS[id], x = 170 + i * 150, g = this.g("World", x, 288);
			const d = creature(g, "Claus", ch.prefix + "idle");
			g.s = 1.5; g.dim = 0.35;
			const h = this.hot({ x: x - 50, y: 288 - d.h * 1.5 - 6, w: 100, h: d.h * 1.5 + 12, sfx: false, onClick: () => this.pick(i, true) });
			return { id, ch, g, x, h, ck: d.k, ph: i * 1.7 };
		});
		this.head = this.g("UI", W / 2, 18); this.head.t(0, 0, "WHO WILL SAVE CHRISTMAS?", { al: "center", sc: 1.6, col: PAL.gold, sh: true });
		this.card = this.g("UI", 0, 0);
		this.btn({ x: 24, y: 160, w: 34, h: 30, label: "<", key: "ArrowLeft", onClick: () => this.pick(this.idx - 1, true) });
		this.btn({ x: W - 58, y: 160, w: 34, h: 30, label: ">", key: "ArrowRight", onClick: () => this.pick(this.idx + 1, true) });
		this.btn({ x: W / 2 + 10, y: 326, w: 150, h: 24, label: "BEGIN  >", col: PAL.green2, pulse: true, key: "Enter", onClick: () => this.begin() });
		this.btn({ x: W / 2 - 160, y: 326, w: 150, h: 24, label: "<  BACK", key: "Escape", onClick: () => go(() => new TitleScene()) });
		this.unlocked = adventUnlocked();
		this.advent = Math.min(this.unlocked, store.get("adventPick", this.unlocked));
		this.advDown = this.btn({ x: W / 2 - 92, y: 294, w: 22, h: 18, label: "-", key: "ArrowDown", onClick: () => this.setAdvent(this.advent - 1) });
		this.advUp = this.btn({ x: W / 2 + 70, y: 294, w: 22, h: 18, label: "+", key: "ArrowUp", onClick: () => this.setAdvent(this.advent + 1) });
		this.advG = this.g("UI", W / 2, 0);
		this.setAdvent(this.advent, true);
		this.pick(0, false);
	}
	pick(i, user) {
		const n = CHAR_ORDER.length; this.idx = (i + n) % n;
		if (user) sfx.spot();
		const a = this.actors[this.idx], ch = a.ch;
		a.g.sy = 0.97; a.g.sx = 1.03; tween(a.g, { sx: 1, sy: 1 }, 0.25, "outCubic");
		const c = this.card; c.clear();
		const x = W / 2 - 150, y = 46;
		panel(c, x, y, 300, 96, ch.col);
		c.t(W / 2, y + 4, ch.name.toUpperCase(), { al: "center", col: ch.col === PAL.green ? 0x7be08f : PAL.gold, sh: true });
		ch.blurb.forEach((l, k) => c.t(W / 2, y + 20 + k * FH, l, { al: "center", col: PAL.white }));
		const r = RELIC_DEFS[ch.relic];
		c.t(W / 2, y + 60, `${ch.hp} HP  -  ${r.name}`, { al: "center", col: PAL.gold });
		if (ch.power) c.t(W / 2, y + 76, `Power: ${ch.power.name}`, { al: "center", col: PAL.pink, sh: true });
		c.a = 0.4; tween(c, { a: 1 }, 0.2);
	}
	setAdvent(n, quiet = false) {
		const v = clamp(n, 0, this.unlocked);
		if (v === this.advent && !quiet) return;
		this.advent = v; store.set("adventPick", v);
		if (!quiet) sfx.tick();
		const g = this.advG; g.clear();
		g.t(0, 297, v ? `ADVENT ${v}` : "ADVENT OFF", { al: "center", col: v ? PAL.gold : MUTE(), sh: true });
		this.advDown.setEnabled(v > 0); this.advUp.setEnabled(v < this.unlocked);
		if (!quiet) { g.s = 1.06; tween(g, { s: 1 }, 0.15, "outQuad"); }
	}
	begin() { if (this.going) return; this.going = true; sfx.charm(); flash(0xfff2c0, 0.3, 0.4); beginRun(CHAR_ORDER[this.idx], this.advent); }
	tick(dt) {
		const a = this.actors[this.idx];
		this.spotX = damp(this.spotX, a.x, 9, dt);
		this.spot.x = this.spotX;
		this.spot.a = 0.85 + Math.sin(this.t * 7) * 0.05;
		for (const b of this.actors) {
			const on = b === a, hov = E.hovered === b.h;
			b.g.dim = damp(b.g.dim, on ? 1 : hov ? 0.6 : 0.3, 10, dt);
			b.g.oy = 0; b.g.rot = 0;
		}
	}
}

const NX = i => 90 + i * 92, NY = i => 176 + Math.round(Math.sin(i * 0.9) * 22), mapW = () => NX(NIGHTS - 1) + 110;
export class MapScene extends Scene {
	enter() {
		music.play("calm");
		this.tile = tiledBg(this, W + mapW() + 200);
		if (!this.tile && !backdrop(this, "map")) bgimg(this.g("BG", 0, 0), "moon", 520, 86);
		this.world = this.g("World", 0, 0);
		const w = this.world;
		for (let i = 0; i < NIGHTS; i++) {
			bgimg(w, G.lights[i] ? "house_lit" : "house", NX(i), 318, { sc: 0.8 });
			w.t(NX(i), 324, dateLabel(i), { al: "center", col: i === G.night ? PAL.gold : 0x6a7aa8 });
		}
		this.houses = [];
		for (let i = 0; i < NIGHTS; i++) {
			const kid = kidOf(i);
			this.houses.push(this.hot({ x: 0, y: 274, w: 56, h: 48, sfx: false, ix: i,
				tip: () => {
					if (i < G.night) return { t: `${kid}'s house - ${dateLabel(i)}`, c: G.lights[i] ? PAL.gold : MUTE(), b: G.lights[i] ? `${kid} got a lovely gift.` : `${kid} got... coal.` };
					return { t: `${kid}'s house - ${dateLabel(i)}`, c: PAL.pink, b: `"${wishFor(i).text}"` };
				} }));
		}
		this.pos = i => { const n = G.map[i]; return n.opts.map((t, k) => [NX(i), n.opts.length > 1 ? NY(i) + (k ? 30 : -30) : NY(i)]); };
		this.liveDots = [];
		for (let i = 0; i < NIGHTS - 1; i++) {
			const A = this.pos(i), B = this.pos(i + 1), na = G.map[i], nb = G.map[i + 1];
			A.forEach((a, ka) => B.forEach((b, kb) => {
				const trod = na.done && nb.done && na.chosen === na.opts[ka] && nb.chosen === nb.opts[kb];
				const live = i === G.night - 1 && na.chosen === na.opts[ka];
				const d = Math.hypot(b[0] - a[0], b[1] - a[1]), n = Math.floor(d / 10);
				for (let s = 2; s < n - 1; s++) {
					const t = s / n, px = a[0] + (b[0] - a[0]) * t, py = a[1] + (b[1] - a[1]) * t, col = trod || live ? PAL.gold : 0x5a6a98;
					const k = icon(w, "path_dot", px, py, { col });
					if (!k) continue;
					k.a = trod ? 1 : 0.7;
					if (live) { k.ph = s; this.liveDots.push(k); }
				}
			}));
		}
		this.nodes = [];
		for (let i = 0; i < NIGHTS; i++) {
			const n = G.map[i];
			n.opts.forEach((type, k) => {
				const [x, y] = this.pos(i)[k], info = NODE_INFO[type], boss = type === "boss";
				const g = this.g("World", x, y);
				const done = n.done, reach = i === G.night, picked = n.chosen === type;
				const ni = icon(g, "node_" + type, 0, 0);
				const sz = ni ? Math.round(Math.max(ni.w, ni.h) / 2) : (boss ? 20 : 13);
				if (done && picked) icon(g, "check", sz - 3, -sz + 3);
				g.dim = done && !picked ? 0.45 : (!reach && !done) ? 0.62 : 1;
				const node = { g, i, type, x, y, reach, sz };
				node.h = this.hot({ x: 0, y: y - sz - 3, w: sz * 2 + 6, h: sz * 2 + 6, en: reach,
					tip: { t: `${info.n} - ${dateLabel(i)}`, c: info.col, b: info.d },
					onClick: () => this.choose(node) });
				if (reach) { g.s = 0.8; g.a = 0; tween(g, { s: 1.05, a: 1 }, 0.35, "outCubic", 0.3 + k * 0.08); }
				this.nodes.push(node);
			});
		}
		const cur = this.nodes.filter(n => n.reach);
		this.pin = this.g("World", NX(Math.min(G.night, NIGHTS - 1)), -40);
		const pinK = icon(this.pin, "pin", 0, 0);
		const pinH = pinK ? pinK.h : 16;
		this.pinY = cur.length ? Math.min(...cur.map(n => n.y)) - (cur[0].sz + pinH / 2 + 14) : 120;
		this.pinDrop = 0;
		this.snow = new Snow(this, 70, "BG");
		this.hud = new HUD(this);
		this.head = this.g("UI", W / 2, 46);
		const nl = nightsLeft(G.night);
		this.head.t(0, 0, `${nl} NIGHT${nl === 1 ? "" : "S"} UNTIL CHRISTMAS`, { al: "center", col: PAL.gold, sh: true });
		const tier = spiritTier();
		this.head.t(0, 14, `Christmas Spirit: ${tier.name}`, { al: "center", col: tier.col, sh: true });
		this.head.s = 0.5; tween(this.head, { s: 1 }, 0.4, "outBack");
		this.g("UI", W / 2, H - 13).t(0, 0, "Drag, scroll or use arrow keys", { al: "center", col: MUTE() });
		this.target = clamp(NX(Math.min(G.night, NIGHTS - 1)) - W / 2 + 60, 0, mapW() - W);
		this.scroll = clamp(this.target - 260, 0, mapW() - W);
		if (G.night > 0) floatText(W / 2, 84, "Night falls. The Christmas spirit fades...", MUTE());
	}
	choose(n) {
		if (this.leaving) return;
		this.leaving = true; sfx.bell(88);
		n.g.s = 1.3; tween(n.g, { s: 1 }, 0.3, "outBack");
		fx("select", n.x - this.scroll, n.y);
		burst(n.x - this.scroll, n.y, 10, { cols: [PAL.gold, 0xffffff], spd: [40, 120] });
		after(0.35, () => enterNode(n.i, n.type));
	}
	tick(dt, active) {
		this.snow.update(dt); this.hud.tick(dt);
		const m = E.mouse, max = mapW() - W;
		if (active) {
			this.target += m.wheel * 0.6;
			if (E.keys.has("ArrowRight") || E.keys.has("d")) this.target += 420 * dt;
			if (E.keys.has("ArrowLeft") || E.keys.has("a")) this.target -= 420 * dt;
			if (m.pressed) { this.dragX = m.x; this.dragS = this.target; this.dragging = false; this.flingV = 0; }
			if (m.down && this.dragX !== undefined) {
				if (Math.abs(m.x - this.dragX) > 5) { this.dragging = true; E.pressHot = null; }
				if (this.dragging) { this.target = this.dragS - (m.x - this.dragX); this.flingV = -m.vx; }
			}
			if (!m.down) { if (this.dragging && this.flingV) this.target += this.flingV * 0.15; this.dragX = undefined; this.dragging = false; this.flingV = 0; }
		}
		const pf = PAD.active && PAD.focus;
		if (pf && this.nodes.some(n => n.h === pf.ref)) { const n = this.nodes.find(n => n.h === pf.ref); this.target = n.x - W / 2; }
		// let the map overshoot a little while you drag, feels less stiff
		if (!m.down) this.target = clamp(this.target, 0, max);
		else this.target = clamp(this.target, -40, max + 40);
		this.scroll = damp(this.scroll, this.target, 10, dt);
		const s = Math.round(this.scroll);
		this.world.x = -s; if (this.tile) this.tile.x = -s;
		for (const k of this.liveDots) k.a = 0.4 + 0.6 * Math.max(0, Math.sin(this.t * 6 - k.ph * 0.6));
		for (const h of this.houses) h.x = NX(h.ix) - s - 28;
		for (const n of this.nodes) {
			n.g.x = n.x - s; n.h.x = n.x - s - n.sz - 3;
			const hov = E.hovered === n.h;
			if (n.g.ring) { const p = (this.t * 1.2) % 1; n.g.ring.a = 0.35 * (1 - p); n.g.ring.x = -n.sz - 5 - p * 6; n.g.ring.y = n.g.ring.x; n.g.ring.w = n.g.ring.h = (n.sz + 5 + p * 6) * 2; }
			if (this.leaving || E.tweens.some(t => t.o === n.g && !t.dead)) continue;
			if (n.reach) n.g.s = damp(n.g.s, hov ? 1.2 : 1.05 + Math.sin(this.t * 2.5) * 0.03, 12, dt);
			else n.g.s = damp(n.g.s, hov ? 1.1 : 1, 12, dt);
		}
		this.pinDrop = damp(this.pinDrop, 1, 6, dt);
		this.pin.x = NX(Math.min(G.night, NIGHTS - 1)) - s;
		this.pin.y = lerpPin(this.pinY, this.pinDrop) + Math.sin(this.t * 3) * 3;
	}
}
const lerpPin = (y, p) => -40 + (y + 40) * p;

export class RewardScene extends Scene {
	constructor(kind, opts = {}) { super(); this.kind = kind; this.opts = opts; }
	enter() {
		music.play("calm");
		this.tile = tiledBg(this);
		if (!this.tile) village(this, "reward", 300);
		this.snow = new Snow(this, 50, "BG");
		this.hud = new HUD(this);
		const mark = this.owned.length;
		const p = this.g("UI", 0, 0), x = W / 2 - 150, y = 56;
		panel(p, x, y, 300, 240);
		p.t(W / 2, y + 8, this.kind === "elite" ? "ELITE SPOILS" : "SPOILS OF THE NIGHT", { al: "center", col: PAL.gold, sh: true });
		const elite = this.kind === "elite";
		let ck = elite ? ri(28, 38) : ri(14, 22); if (hasRelic("reindeerfeed")) ck = Math.round(ck * 1.25);
		const rows = [{ k: "cookies", label: `${ck} COOKIES`, icon: (g2, ix, iy) => icon(g2, "coin", ix, iy), claim: () => { gainCookies(ck); return true; } }];
		if (rng() < G.potionChance || elite) {
			const pid = randPotionId(); G.potionChance = Math.max(0.1, G.potionChance - 0.1);
			rows.push({ k: "potion", label: POTIONS[pid].name.toUpperCase(), icon: (g2, ix, iy) => { potionIcon(g2, pid, ix, iy); }, tip: { t: POTIONS[pid].name, b: POTIONS[pid].desc, c: POTIONS[pid].col },
				claim: () => { if (!addPotion(pid)) { floatText(W / 2, 100, "Potion slots full!", PAL.red); sfx.error(); return false; } return true; } });
		} else G.potionChance = Math.min(0.9, G.potionChance + 0.1);
		if (elite) { const rid = randRelicId(); if (rid) rows.push({ k: "relic", label: RELICS[rid].name.toUpperCase().slice(0, 22), icon: (g2, ix, iy) => icon(g2, "relic_" + rid, ix, iy), tip: { t: RELICS[rid].name, b: RELICS[rid].desc, c: PAL.gold, kw: findKeywords(RELICS[rid].desc) }, claim: () => { addRelic(rid); return true; } }); }
		const friends = [...new Set(this.opts.friends || [])].filter(id => GIFTS[fid(id)]);
		const fname = id => GIFTS[fid(id)].name.toUpperCase();
		const fstate = id => { const h = G.bag.find(gi => gi.id === fid(id)); return !h ? "NEW" : !h.up ? "UPGRADE" : "+2 SPIRIT"; };
		if (friends.length === 1) {
			const id = friends[0];
			rows.push({ k: "friend", label: `${fname(id)} (${fstate(id)})`.slice(0, 26), icon: (g2, ix, iy) => icon(g2, "heart", ix, iy),
				tip: () => { const t = giftTip({ id: fid(id), up: fstate(id) !== "NEW" }); t.b.unshift({ s: fstate(id) === "NEW" ? "A new friend!" : fstate(id) === "UPGRADE" ? "Upgrades your friend." : "Already upgraded: +2 Spirit.", c: PAL.pink }); return t; },
				claim: () => { this.friendFx(addFriend(id), id); return true; } });
		} else if (friends.length > 1) {
			rows.push({ k: "friend", label: "CHOOSE A FRIEND", icon: (g2, ix, iy) => icon(g2, "heart", ix, iy),
				tip: { t: "Choose a friend", c: PAL.pink, b: "Ones you already have get upgraded." },
				claim: row => { pushOverlay(new ChooseGift({ ids: friends.map(fid), title: "CHOOSE A FRIEND", sub: "Already have one? It gets upgraded.", onPick: gid => { this.friendFx(addFriend(gid.slice(2)), gid.slice(2)); row.btn.kill(); } })); return false; } });
		}
		if (!friends.length && (elite || rng() < 0.5)) {
			const pool = partIds().filter(id => elite || PARTS[id].r < 3), opts = [];
			while (opts.length < 3 && opts.length < pool.length) { const id = pick(pool); if (!opts.includes(id)) opts.push(id); }
			rows.push({ k: "part", label: "CHOOSE A TOY PART", icon: (g2, ix, iy) => partIcon(g2, opts[0], ix, iy),
				claim: row => { pushOverlay(new ChoosePart({ ids: opts, sub: "Then pick a gift to fit it on.", onPick: id => addPartFlow(id, () => row.btn.kill()) })); return false; } });
		}
		const ids = giftChoices(elite, hasRelic("wishlist") ? 4 : 3);
		if (!friends.length) rows.push({ k: "gift", label: "CHOOSE A GIFT", icon: (g2, ix, iy) => icon(g2, "gift", ix, iy),
			claim: (row) => { pushOverlay(new ChooseGift({ ids, sub: "Tip: rare gifts are worth more Spirit.", onPick: id => { addGift(id); row.btn.kill(); } })); return false; } });
		const rh = rows.length > 5 ? 28 : 34;
		rows.forEach((r, i) => {
			r.btn = this.btn({ x: x + 20, y: y + 36 + i * rh, w: 260, h: 26, label: r.label, tip: r.tip, drawIcon: r.icon,
				onClick: () => { if (r.claim(r)) { const by = y + 49 + i * rh; burst(x + 150, by, 8, { cols: [PAL.gold, 0xffffff] }); tween(r.btn.g, { ox: 300, a: 0 }, 0.25, "inBack", 0, () => r.btn.kill()); r.btn.hot.en = false; } } });
		});
		this.popIn(W / 2, y + 120, mark);
		const leaveMode = !this.opts.noLeave;
		this.btn({ x: x + 50, y: y + 206, w: 200, h: 24, label: leaveMode ? "LEAVE A GIFT  >" : "CONTINUE  >", col: PAL.green2, pulse: true, key: "Enter",
			onClick: () => leaveMode ? go(() => new LeaveScene()) : finishNode() });
	}
	friendFx(res, id) {
		const name = GIFTS[fid(id)].name;
		floatText(W / 2, 40, res === "new" ? `${name} joins your sack!` : res === "up" ? `${name}+ upgraded!` : `${name} is already your best friend!`, PAL.pink, { big: res !== "max", hold: 1 });
		sfx.love();
	}
	tick(dt) { this.snow.update(dt); this.hud.tick(dt); }
}

export class LeaveScene extends Scene {
	constructor(onDone = null) { super(); this.onDone = onDone; }
	enter() {
		music.play("calm");
		const BG_UP = 40;
		if (!tiledBg(this, W + 200, -100, -BG_UP, H + BG_UP) && !backdrop(this, "leave")) bgimg(this.g("BG", 0, 0), "moon", 90, 76);
		this.house = this.g("World", 430, 282);
		this.spriteHouse = bgimg(this.house, "house", 0, 0, { sc: 2 });
		this.hx = 430; this.hy = this.spriteHouse ? Math.round(282 - this.spriteHouse.h * 0.45) : 221;
		this.snow = new Snow(this, 60, "BG");
		this.hud = new HUD(this);
		this.kid = KIDS[G.night % KIDS.length];
		this.wish = pickWish();
		this.head = this.g("UI", W / 2, 46);
		this.head.t(0, 0, `Little ${this.kid} is asleep. Leave a gift under the tree.`, { al: "center", col: PAL.gold, sh: true });
		this.wishLine = this.g("UI", W / 2, 62);
		this.wishLine.t(0, 0, `${this.kid}'s wish: "${this.wish.text}"`, { al: "center", col: PAL.pink, sh: true });
		this.wishLine.s = 0; tween(this.wishLine, { s: 1 }, 0.4, "outBack", 0.15);
		after(0.5, () => { if (!this.done && !this.left) this.openPicker(); });
	}
	openPicker() {
		const canLeave = G.bag.length > LEAVE_MIN, cost = leaveCookieCost(), extras = [];
		extras.push({ label: `GIVE ${cost} COOKIES (+1)`, disabled: G.cookies < cost, onClick: () => this.cookies(),
			tip: { t: "Leave Cookies instead", c: PAL.gold, b: "Keep your gifts. +1 Spirit. The price goes up each time." } });
		if (canSkipLeave() || !canLeave) extras.push({ label: canLeave ? "SKIP (PATCHED SACK)" : "NOTHING TO SPARE", onClick: () => this.skip(!canLeave),
			tip: { t: "Skip", b: canLeave ? "Keep everything." : "Your sack is too light. -5 Spirit." } });
		pushOverlay(new GiftPicker({
			title: "LEAVE A GIFT UNDER THE TREE", subCol: canLeave ? PAL.pink : null,
			sub: canLeave ? `${this.kid.toUpperCase()}'S WISH: "${this.wish.text}"` : "Your sack is nearly empty...",
			list: canLeave ? sortBag(G.bag) : [], onPick: gi => this.leave(gi), cancel: false, extras,
			label: gi => { const w = wishOk(this.wish, gi); let j = joyOf(gi) + (w ? WISH_BONUS : 0); if (G.doubleJoy && j > 0) j *= 2; const adopt = GIFTS[gi.id].type === "friend"; return { s: (j > 0 ? "+" : "") + j + (w ? " WISH!" : adopt ? " ADOPT!" : " SPIRIT"), c: w ? PAL.gold : adopt ? PAL.pink : j > 0 ? 0x9fffd0 : 0xff7070 }; },
		}));
	}
	async leave(gi) {
		this.done = true;
		this.lastGift = gi;
		const c = makeCard(this, gi, W / 2, 180, "Pop"); c.s = 1.3;
		tween(c, { x: this.hx, y: this.hy, s: 0.1, a: 0 }, 0.55, "inQuad");
		await wait(620); if (this.left) return;
		c.kill();
		const joy = leaveGift(gi);
		this.react(joy > 0, joy);
	}
	cookies() { this.done = true; if (!leaveCookies()) { this.done = false; this.openPicker(); return; } burst(this.hx, this.hy, 20, { cols: [PAL.gold, 0xc89050] }); this.react(true, 1); }
	skip(penalty) { this.done = true; if (penalty) addSpirit(-5); this.react(!penalty, 0); }
	react(good, joy) {
		if (good) {
			if (this.spriteHouse) this.house.anim(this.spriteHouse, hasAnim("BGImage", "house_lit") ? "house_lit" : "house");
			this.house.flash = 0.6; tween(this.house, { flash: 0 }, 0.6);
			this.house.sy = 1.08; tween(this.house, { sy: 1 }, 0.4, "outBack");
			burst(this.hx, this.hy, 22, { cols: [PAL.gold, 0xffffff], spd: [50, 180], g: 120, life: 0.9 });
			flash(PAL.window, 0.25, 0.5); zoomPunch(0.03);
		} else { sfx.debuff(); burst(this.hx, this.hy, 20, { cols: [0x222222, 0x555555], g: -40 }); shake(3); }
		const granted = this.lastGift && wishOk(this.wish, this.lastGift);
		if (granted) { tween(this.wishLine, { s: 1.4 }, 0.2, "outBack"); tween(this.wishLine, { s: 0, a: 0, y: this.hy }, 0.45, "inBack", 0.6); sfx.charm(); }
		const adopted = this.lastGift && GIFTS[this.lastGift.id].type === "friend" ? GIFTS[this.lastGift.id].name : null;
		if (adopted) { for (let i = 0; i < 3; i++) after(0.3 + i * 0.15, () => burst(this.hx, this.hy - 10, 16, { cols: [PAL.pink, 0xff4f8f, 0xffffff], spd: [40, 160], g: -60, life: 0.9 })); after(0.3, () => sfx.love()); }
		const msg = adopted ? pick([`${this.kid} wakes up to a ${adopted} under the tree. Best friends forever!`, `A ${adopted}?! ${this.kid} has a new best friend!`, `${this.kid} names the ${adopted} "Mr. Sprinkles". It approves.`])
			: granted ? `Wish granted! ${this.kid} will remember this forever!` : good ? (joy >= 8 ? `${this.kid} will remember this Christmas forever!` : `${this.kid} will wake up smiling.`) : `${this.kid} will wake up to... coal?`;
		const t = this.g("UI", W / 2, 112); wrap(msg, 56).split("\n").forEach((l, i) => t.t(0, i * FH, l, { al: "center", col: adopted ? PAL.pink : good ? 0x9fffd0 : 0xff9090, sh: true })); t.s = 0; tween(t, { s: 1 }, 0.4, "outBack");
		this.btn({ x: W / 2 - 90, y: 150, w: 180, h: 24, label: "CONTINUE  >", col: PAL.green2, pulse: true, key: "Enter", onClick: () => (this.onDone || finishNode)() });
	}
	exit() { this.left = true; super.exit(); }
	tick(dt) { this.snow.update(dt); this.hud.tick(dt); }
}

const ELF = {
	greet: ["Welcome to the Elf Shop, {nick}!", "Cuttin' it a little close for Christmas shopping.", "Everything here is hand-made!", "Back again? Business is a-booming."],
	poor: ["Not enough cookies, love!", "Cookies first, presents later!"],
	full: "Your potion belt is heavy enough!", rewrap: "Good as new!", donate: "The kids will love these!",
};
export class ShopScene extends Scene {
	enter() {
		music.play("calm");
		if (!sceneSprite(this, "ShopBG")) backdrop(this, "shop");
		this.hud = new HUD(this);
		this.say(pick(ELF.greet));
		const disc = (hasRelic("elfcoupon") ? 0.8 : 1) * (adv(7) ? 1.15 : 1), price = b => Math.round((b + ri(-6, 6)) * disc);
		this.items = [];
		giftChoices(false, 5).forEach((id, i) => {
			const r = GIFTS[id].r, x = 214 + i * 84, y = 108;
			const c = makeCard(this, { id, up: false, uid: -1 }, x, y, "UI");
			c.y = y - 12; c.a = 0; tween(c, { y, a: 1 }, 0.3, "outCubic", 0.05 + i * 0.05);
			this.item(c, x, y, CW, CH, price({ 1: 50, 2: 76, 3: 150 }[r]), () => giftTip({ id, up: false }), () => { addGift(id); this.fly(c, 572, 10); });
		});
		const used = new Set();
		for (let i = 0; i < 3; i++) {
			const rid = randRelicId(); if (!rid || used.has(rid)) continue; used.add(rid);
			const d = RELICS[rid], x = 214 + i * 54, y = 222, c = this.g("UI", x, y);
			icon(c, "relic_" + rid, 0, 0);
			c.s = 0; tween(c, { s: 1 }, 0.4, "outBack", 0.3 + i * 0.06);
			this.item(c, x, y, 34, 34, price({ 1: 150, 2: 210, 3: 270 }[d.r]), { t: d.name, c: PAL.gold, b: d.desc, kw: findKeywords(d.desc) }, () => { const [tx, ty] = this.hud.relicNext || [10, 30]; addRelic(rid); this.fly(c, tx, ty); });
		}
		for (let i = 0; i < 3; i++) {
			const pid = randPotionId(), d = POTIONS[pid], x = 392 + i * 44, y = 222, c = this.g("UI", x, y);
			potionIcon(c, pid, 0, 0);
			c.s = 0; tween(c, { s: 1 }, 0.4, "outBack", 0.45 + i * 0.06);
			this.item(c, x, y, 28, 34, price(56), { t: d.name, c: d.col, b: d.desc, kw: findKeywords(d.desc) }, () => { if (!addPotion(pid)) { this.say(ELF.full); return false; } this.fly(c, 180, 10); });
		}
		const parts = []; while (parts.length < 2) { const id = pick(partIds()); if (!parts.includes(id)) parts.push(id); }
		parts.forEach((pid, i) => {
			const p = PARTS[pid], x = 214 + i * 54, y = 300, c = this.g("UI", x, y);
			partIcon(c, pid, 0, 0);
			c.s = 0; tween(c, { s: 1 }, 0.4, "outBack", 0.55 + i * 0.06);
			const it = this.item(c, x, y, 30, 30, price(partPrice(pid)), { t: p.name, c: p.col, b: p.desc },
				() => {
					if (!G.bag.some(canPart)) { this.say("None of your gifts has room for it!"); return false; }
					addPartFlow(pid, () => { spendCookies(it.cost); sfx.coin(); it.sold = true; it.h.en = false; it.g.kill(); this.refresh(); });
					return false;
				});
		});
		this.rewrapPrice = Math.round(75 * disc); this.donatePrice = Math.round(40 * disc);
		this.rewrap = this.btn({ x: 520, y: 200, w: 112, h: 20, label: `REWRAP ${this.rewrapPrice}`, col: PAL.green2,
			tip: { c: PAL.green, b: "Upgrade one gift." }, onClick: () => this.doRewrap() });
		this.donate = this.btn({ x: 520, y: 226, w: 112, h: 20, label: `DONATE ${this.donatePrice}`, col: PAL.gold2,
			tip: { c: PAL.gold, b: "+6 Christmas Spirit." }, onClick: () => this.doDonate() });
		this.btn({ x: 520, y: 326, w: 112, h: 24, label: "LEAVE  >", col: PAL.red2, key: "Escape", onClick: () => finishNode() });
		this.refresh();
	}
	say(t) { this.bub?.kill?.(); this.bub = bubble(this, 110, 214, t, { hold: 2.2 }); }
	item(g, x, y, w, h, cost, tip, buy) {
		const it = { g, x, y, cost, sold: false };
		it.tag = this.g("UI", x, y + h / 2 + 4);
		it.h = this.hot({ x: x - w / 2, y: y - h / 2, w, h: h + 18, tip: typeof tip === "function" ? tip : () => tip,
			onClick: () => {
				if (it.sold) return;
				if (G.cookies < it.cost) { sfx.error(); this.say(pick(ELF.poor)); [5, -4, 3, 0].forEach((v, i) => tween(g, { ox: v }, 0.04, "linear", i * 0.04)); it.tag.flash = 1; tween(it.tag, { flash: 0 }, 0.3); return; }
				if (buy() === false) return;
				spendCookies(it.cost); sfx.coin(); it.sold = true; it.h.en = false; this.refresh();
				burst(x, y, 10, { cols: [PAL.gold, 0xffffff], spd: [40, 140] });
			} });
		this.items.push(it);
		return it;
	}
	fly(g, tx, ty) { tween(g, { x: tx, y: ty, s: 0.2, a: 0.2 }, 0.4, "inQuad", 0, () => g.kill()); }
	refresh() {
		for (const it of this.items) {
			it.tag.clear();
			if (it.sold) { it.tag.t(0, 0, "SOLD", { al: "center", col: MUTE() }); continue; }
			icon(it.tag, "coin", -String(it.cost).length * 5 - 6, 6);
			it.tag.t(4, 0, String(it.cost), { al: "center", col: G.cookies >= it.cost ? PAL.gold : 0xff6060, sh: true });
		}
		this.rewrap.setEnabled(!this.rewrapped && G.cookies >= this.rewrapPrice && G.bag.some(canUpgrade));
		this.donate.setEnabled(G.cookies >= this.donatePrice);
	}
	doRewrap() {
		pushOverlay(new GiftPicker({ title: "REWRAP A GIFT", list: sortBag(G.bag.filter(canUpgrade)), cancel: "BACK",
			onPick: gi => { spendCookies(this.rewrapPrice); upgradeGift(gi); this.rewrapped = true; sfx.buff(); this.say(ELF.rewrap); this.refresh(); } }));
	}
	doDonate() { spendCookies(this.donatePrice); addSpirit(6); this.donatePrice += 20; this.donate.setLabel(`DONATE ${this.donatePrice}`); this.say(ELF.donate); this.refresh(); }
	tick(dt) {
		this.hud.tick(dt);
		for (const it of this.items) if (!it.sold && !it.g.dead && !E.tweens.some(t => t.o === it.g && !t.dead)) {
			const hov = E.hovered === it.h;
			it.g.s = damp(it.g.s, hov ? 1.12 : 1, 14, dt);
			it.g.oy = damp(it.g.oy, hov ? -3 : Math.sin(this.t * 2 + it.x) * 1, 10, dt);
		}
		if (this._ck !== G.cookies) { this._ck = G.cookies; this.refresh(); }
	}
}

export class RestScene extends Scene {
	enter() {
		music.play("calm");
		if (!sceneSprite(this, "ChimneyBG")) backdrop(this, "rest");
		this.hud = new HUD(this);
		this.g("UI", W / 2, 44).t(0, 0, "FIRESIDE - A QUIET NIGHT", { al: "center", col: PAL.orange, sh: true });
		let n = Math.round(G.maxHp * (adv(5) ? 0.22 : 0.3)); for (const r of G.relics) if (RELICS[r.id].restHeal) n = RELICS[r.id].restHeal(n);
		this.healN = n;
		this.choices = [
			this.btn({ x: 460, y: 140, w: 170, h: 24, label: `REST (+${n} HP)`, col: PAL.green2, key: "1", tip: { b: `Heal ${n} HP.` }, onClick: () => this.pickC("rest") }),
			this.btn({ x: 460, y: 172, w: 170, h: 24, label: "REWRAP A GIFT", col: PAL.panel2, key: "2", disabled: !G.bag.some(canUpgrade), tip: { b: "Upgrade one gift." }, onClick: () => this.pickC("rewrap") }),
			this.btn({ x: 460, y: 204, w: 170, h: 24, label: "WRITE TO SANTA", col: PAL.red2, key: "3", tip: { c: PAL.red, b: "+8 Christmas Spirit." }, onClick: () => this.pickC("letter") }),
		];
		this.choices.forEach((b, i) => { b.g.ox = 200; tween(b.g, { ox: 0 }, 0.4, "outBack", 0.1 + i * 0.07); });
	}
	pickC(k) {
		if (this.chose) return;
		if (k === "rewrap") {
			pushOverlay(new GiftPicker({ title: "REWRAP A GIFT", list: sortBag(G.bag.filter(canUpgrade)), cancel: "BACK", onPick: gi => { upgradeGift(gi); sfx.buff(); this.finish("Ribbons tied, bows fluffed. Good as new."); } }));
			return;
		}
		if (k === "rest") { heal(this.healN); this.finish("You doze by the crackling fire. The aches fade away."); }
		if (k === "letter") { addSpirit(8); this.finish("\"Dear Nick, remember the boy who carved toys for everyone?\""); }
	}
	finish(msg) {
		this.chose = true; for (const b of this.choices) b.kill();
		burst(320, 250, 30, { cols: [PAL.orange, PAL.gold], g: -100, spd: [30, 120], add: true });
		const t = this.g("UI", 0, 0); wrap(msg, 22).split("\n").forEach((l, i) => t.t(460, 140 + i * FH, l, { col: PAL.cream, sh: true }));
		t.a = 0; tween(t, { a: 1 }, 0.4);
		this.btn({ x: 460, y: 230, w: 170, h: 24, label: "CONTINUE  >", col: PAL.green2, pulse: true, key: "Enter", onClick: () => finishNode() });
	}
	tick(dt) {
		this.hud.tick(dt);
		if (Math.random() < dt * 30 * E.settings.particles) { const fx = 320 + rf(-40, 40); burst(fx, 262, 1, { cols: [PAL.orange, PAL.gold, PAL.red, 0xffe080], spd: [10, 30], a0: -2, a1: -1.1, g: -140, life: 0.9, sz: 3, add: true }); }
		if (Math.random() < dt * 4) sfx.fire();
	}
}

// stocking: the swing builds up, it bursts, the relic floats out at 2x
export class TreasureScene extends Scene {
	enter() {
		music.play("calm");
		if (!sceneSprite(this, "StockingBG")) backdrop(this, "treasure");
		this.sock = this.g("World", 320, 140);
		const sk = icon(this.sock, "stocking", 0, 0);
		if (sk) sk.y = sk.h / 2;
		this.sockH = sk ? sk.h : 110;
		this.swing = { a: 0, f: 3 };
		this.hud = new HUD(this);
		this.g("UI", W / 2, 44).t(0, 0, "A STOCKING, STUFFED TO BURSTING", { al: "center", col: PAL.pink, sh: true });
		this.h = this.hot({ x: 270, y: 135, w: 100, h: 110, tip: { b: "Something heavy is inside..." }, onClick: () => this.open() });
	}
	async open() {
		if (this.opened) return; this.opened = true; this.h.en = false;
		this.swing.f = 13;
		tween(this.swing, { a: 0.2 }, 0.75, "inQuad");
		tween(this.sock, { sx: 0.92, sy: 1.08 }, 0.75, "inQuad");
		for (let i = 0; i < 5; i++) after(i * 0.15, () => { if (!this.left) sfx.jingle(); });
		await wait(780); if (this.left) return;
		this.swing.f = 9;
		tween(this.swing, { a: 0 }, 1.1, "outCubic");
		this.sock.sx = 1.22; this.sock.sy = 0.82;
		tween(this.sock, { sx: 1, sy: 1 }, 0.45, "outBack");
		shake(4); flash(PAL.gold, 0.3, 0.5); zoomPunch(0.04); sfx.unwrap();
		const mouthY = 140 + Math.round(this.sockH * 0.15);
		burst(320, mouthY, 26, { cols: [PAL.gold, 0xffffff], spd: [60, 200], g: 160, life: 1 });
		const rid = randRelicId(), d = RELICS[rid];
		if (!rid) { gainCookies(100); }
		else {
			const r = this.relic = this.g("Pop", 320, mouthY);
			icon(r, "relic_" + rid, 0, 0);
			r.s = 0.3; r.a = 0;
			tween(r, { a: 1 }, 0.25, "linear");
			tween(r, { y: 98 }, 0.9, "outCubic");
			tween(r, { s: 2 }, 0.9, "outBack", 0, () => { this.floating = true; this.floatT0 = this.t; });
			addRelic(rid);
			const t = this.g("Pop", W / 2, 260); t.t(0, 0, d.name, { al: "center", col: PAL.gold, sh: true }); t.t(0, 16, wrap(d.desc, 50), { al: "center", col: PAL.white, sh: true });
			t.a = 0; t.oy = 8; tween(t, { a: 1, oy: 0 }, 0.5, "outCubic", 0.6);
		}
		const b = this.btn({ x: W / 2 - 90, y: 316, w: 180, h: 24, label: "CONTINUE  >", col: PAL.green2, pulse: true, key: "Enter", onClick: () => finishNode() });
		b.g.a = 0; tween(b.g, { a: 1 }, 0.4, "linear", 0.9);
	}
	exit() { this.left = true; super.exit(); }
	tick(dt) {
		this.hud.tick(dt);
		const rm = E.settings.reduceMotion;
		if (!this.opened) {
			this.sock.oy = Math.sin(this.t * 2) * 2;
			this.swing.a = damp(this.swing.a, E.hovered === this.h ? 0.06 : 0.025, 6, dt);
		}
		this.sock.rot = rm ? 0 : Math.sin(this.t * this.swing.f) * this.swing.a;
		if (this.floating && this.relic && !this.relic.dead) {
			const k = this.t - this.floatT0;
			this.relic.oy = rm ? 0 : Math.sin(k * 2.2) * 3;
			if (Math.random() < dt * 6) burst(320 + rf(-22, 22), 98 + rf(-22, 22), 2, { cols: [PAL.gold, 0xffffff], spd: [5, 20], g: -10, life: 0.6, sz: 2, szr: 1 });
		}
	}
}

export class EventScene extends Scene {
	constructor(ev) { super(); this.ev = ev; }
	enter() {
		music.play("calm");
		if (!tiledBg(this, W + 200, -100, 0, H, "IntroBG")) backdrop(this, "event");
		this.snow = new Snow(this, 50, "BG");
		this.hud = new HUD(this);
		const mark = this.owned.length;
		const ev = this.ev, g = this.pg = this.g("UI", 0, 0);
		panel(g, 60, 40, 520, 300, PAL.gold2);
		this.art = this.g("UI", W / 2, 60);
		this.art.t(0, 0, ev.title.toUpperCase(), { al: "center", sc: 2, col: PAL.gold, sh: true });
		this.body = this.g("UI", 0, 0);
		this.setText(ev.text, true);
		this.showChoices(true);
		this.popIn(W / 2, 190, mark);
	}
	setText(s, first = false) {
		this.cur = s; this.body.clear();
		const lines = wrap(s, 50).split("\n");
		lines.forEach((l, i) => this.body.t(80, 100 + i * 14, l, { col: PAL.white }));
		if (!first) { this.body.a = 0; this.body.oy = 6; tween(this.body, { a: 1, oy: 0 }, 0.3, "outCubic"); sfx.page(); }
	}
	clearBtns() { for (const b of this.btns.slice()) b.kill(); }
	showChoices(first = false) {
		this.clearBtns();
		this.ev.choices.forEach((ch, i) => {
			const ok = !ch.req || ch.req(RUN);
			const b = this.btn({ x: 76, y: 236 + i * 26, w: 488, h: 22, label: `${i + 1}. ${ch.l}`, disabled: !ok, col: PAL.panel2, key: String(i + 1),
				tip: ok ? null : { c: PAL.red, b: "You can't afford this." }, onClick: () => this.choose(ch) });
			if (!first) { b.g.ox = -40; b.g.a = 0; tween(b.g, { ox: 0, a: 1 }, 0.3, "outBack", 0.15 + i * 0.07); }
		});
	}
	choose(ch) {
		this.clearBtns();
		let res; try { res = ch.fx(RUN); } catch (e) { console.error(e); res = "Something strange happened."; }
		this.art.s = 1.1; tween(this.art, { s: 1 }, 0.3, "outBack");
		if (typeof res === "string") { this.setText(res); this.cont(); return; }
		this.setText(res.t || "");
		if (res.fight) { shake(4); this.btn({ x: 220, y: 290, w: 200, h: 24, label: "FIGHT!", col: PAL.red2, pulse: true, key: "Enter", onClick: () => startFight(res.fight, res.kind || "normal", { fromEvent: true }) }); return; }
		if (res.choose) { after(0.4, () => pushOverlay(new ChooseGift({ ids: res.choose, onPick: id => { addGift(id); this.cont(); }, onSkip: () => this.cont() }))); return; }
		if (res.pick) { after(0.4, () => this.picker(res.pick.mode)); return; }
		this.cont();
	}
	picker(mode) {
		const done = msg => { if (msg) this.setText((this.cur ? this.cur + "\n" : "") + msg); this.cont(); };
		if (mode === "upgrade") {
			const l = G.bag.filter(canUpgrade); if (!l.length) return done("Nothing to rewrap.");
			pushOverlay(new GiftPicker({ title: "REWRAP A GIFT", list: sortBag(l), cancel: "SKIP", onPick: gi => { upgradeGift(gi); sfx.buff(); done(); }, onCancel: () => done() }));
		} else if (mode === "leave") {
			if (G.bag.length <= LEAVE_MIN) return done("Your sack is too light to spare a gift.");
			pushOverlay(new GiftPicker({ title: "LEAVE A GIFT", list: sortBag(G.bag), cancel: "KEEP THEM", onCancel: () => { G.doubleJoy = false; done(); },
				label: gi => { let j = joyOf(gi); if (G.doubleJoy && j > 0) j *= 2; return { s: (j > 0 ? "+" : "") + j + " SPIRIT", c: j > 0 ? 0x9fffd0 : 0xff7070 }; },
				onPick: gi => { leaveGift(gi); done(); } }));
		} else if (mode === "transform") {
			pushOverlay(new GiftPicker({ title: "TRANSFORM A GIFT", list: sortBag(G.bag), cancel: "SKIP", onCancel: () => done(),
				onPick: gi => { const r = GIFTS[gi.id].r; removeGift(gi); const id = randGiftId(r >= 1 && r <= 3 ? Math.min(3, r + (rng() < 0.3 ? 1 : 0)) : rollRarity(false)); addGift(id); sfx.relic(); done(`It became ${GIFTS[id].name}!`); } }));
		} else if (mode === "dup") {
			pushOverlay(new GiftPicker({ title: "DUPLICATE A GIFT", list: sortBag(G.bag.filter(g => GIFTS[g.id].type !== "curse")), cancel: "SKIP", onCancel: () => done(),
				onPick: gi => { addGift(gi.id, gi.up); sfx.relic(); done(`A second ${GIFTS[gi.id].name}!`); } }));
		}
	}
	cont() { this.clearBtns(); const b = this.btn({ x: 220, y: 300, w: 200, h: 24, label: "CONTINUE  >", col: PAL.green2, pulse: true, key: "Enter", onClick: () => finishNode() }); b.g.s = 0; tween(b.g, { s: 1 }, 0.3, "outBack"); }
	tick(dt) { this.snow.update(dt); this.hud.tick(dt); this.art.oy = Math.sin(this.t * 2) * 1; }
}

export class GameOverScene extends Scene {
	enter() {
		music.stop();
		backdrop(this, "gameover");
		this.snow = new Snow(this, 40, "BG", { wind: -10 });
		const g = this.g("UI", W / 2, 64);
		if (hasType("GameOver")) { const k = g.sp("GameOver", 0, 10); const f = Math.min(1, 360 / k.nw, 90 / k.nh); k.w = k.nw * f; k.h = k.nh * f; }
		else g.t(0, -8, "GAME HO-HO-HOVER!", { al: "center", sc: 2, col: 0xff5050, sh: true });
		g.s = 1.3; g.a = 0; tween(g, { s: 1, a: 1 }, 0.45, "outCubic"); after(0.35, () => { shake(6); sfx.thud(); });
		const s = G.stats || {}, st = this.g("UI", W / 2, 140);
		if (hasType("GameOver")) st.t(0, -14, "GAME HO-HO-HOVER!", { al: "center", col: 0xff5050, sh: true });
		const lines = [`Nights survived: ${G.night} of ${NIGHTS}`, `Monsters defeated: ${s.kills || 0}`, `Gifts left for children: ${s.left || 0}`, `Spirit given: ${s.joy || 0}`];
		lines.forEach((l, i) => st.t(0, 10 + i * 16, l, { al: "center", col: PAL.white, sh: true }));
		st.a = 0; tween(st, { a: 1 }, 0.8, "linear", 0.6);
		this.btn({ x: W / 2 - 170, y: 280, w: 160, h: 24, label: "TRY AGAIN", col: PAL.red2, pulse: true, key: "Enter", onClick: () => startRun() });
		this.btn({ x: W / 2 + 10, y: 280, w: 160, h: 24, label: "TITLE", onClick: () => goTitle() });
		sfx.lose();
	}
	tick(dt) { this.snow.update(dt); }
}

const endingFor = (ch, secret) => [[
	...(secret ? ["The Naughty List shredded, its dark magic fled,", "And Santa stood laughing, his cheeks rosy red."]
		: ["The Krampus fell silent, the dark shadow fled", "And Santa stood blinking, his cheeks rosy red."]),
	...ch.ending,
	...(secret ? ["And all of the friends that were won on the way", "Climbed into the sleigh for the ride Christmas Day."] : []),
	"And kids heard them say as they flew out of sight:",
	"\"Merry Christmas to all! Have a jolly good night!\"",
]];

export class VictoryScene extends Scene {
	enter() {
		music.play("end");
		this.newAdvent = unlockNextAdvent();
		this.tile = tiledBg(this);
		if (!this.tile) village(this, "victory", 300, true);
		this.sleigh = this.g("BG", -200, 90);
		this.sleighSprite = bgimg(this.sleigh, "sleighs", 0, 0);
		if (this.sleighSprite) this.sleighSprite.x = this.sleighSprite.w / 2;
		this.sleighW = this.sleighSprite ? this.sleighSprite.w : 90;
		this.sleigh.x = -this.sleighW - 10;
		this.snow = new Snow(this, 90, "World");
		this.poem = new Scroller(this, endingFor(curChar(), G.secret));
		this.skipB = this.btn({ x: W - 110, y: H - 30, w: 100, h: 20, label: "SKIP", key: "Escape", onClick: () => { this.poem.skip(); this.showEnd(); } });
		this.hint = this.g("UI", W / 2, H - 22); this.hint.t(0, 0, "hold to speed up", { al: "center", col: MUTE() });
		sfx.bell(84);
	}
	showEnd() {
		if (this.shown) return; this.shown = true;
		this.skipB.kill(); this.hint.kill();
		const tier = spiritTier(), s = G.stats || {};
		const tt = this.g("UI", W / 2, 60); tt.t(0, 0, G.secret ? "A TRULY NICE CHRISTMAS!" : "MERRY CHRISTMAS!", { al: "center", sc: 2, col: PAL.gold, sh: true });
		tt.s = 0.8; tt.a = 0; tween(tt, { s: 1, a: 1 }, 0.5, "outBack"); sfx.jingle();
		burst(W / 2, 72, 40, { cols: [PAL.gold, 0xffffff, PAL.red, PAL.green], spd: [60, 200], g: 120 });
		const g = this.g("UI", W / 2, 0);
		const st = [`Christmas Spirit: ${G.spirit} (${tier.name})`, `Gifts left for children: ${s.left || 0}   Wishes granted: ${s.wishes || 0}`, `Monsters defeated: ${s.kills || 0}   Monsters befriended: ${s.charmed || 0}`];
		if (G.advent) st.unshift(`Advent ${G.advent} cleared!`);
		if (this.newAdvent) st.push(`Advent ${this.newAdvent} unlocked!`);
		st.forEach((l, i) => { const k = g.t(0, 120 + i * 16, l, { al: "center", col: i ? PAL.white : tier.col, sh: true }); k.a = 0; tween(k, { a: 1 }, 0.4, "linear", 0.4 + i * 0.25); after(0.4 + i * 0.25, () => sfx.count()); });
		const b = this.btn({ x: W / 2 - 80, y: 236, w: 160, h: 24, label: "THE END", col: PAL.green2, pulse: true, key: "Enter", onClick: () => goTitle() });
		b.g.a = 0; b.g.oy = 12; tween(b.g, { a: 1, oy: 0 }, 0.4, "outBack", 1.2);
	}
	tick(dt, active) {
		this.snow.update(dt);
		if (this.poem.update(dt, active)) this.showEnd();
		if (this.hint && !this.hint.dead) this.hint.a = 0.5 + 0.5 * Math.sin(this.t * 3);
		this.sleigh.x += dt * 40; this.sleigh.y = 90 + Math.sin(this.t * 1.3) * 6;
		if (this.sleigh.x > W + 10) this.sleigh.x = -this.sleighW - 10;
		if (this.shown && Math.random() < dt * 1.5) { const x = rf(60, 580), y = rf(40, 140); burst(x, y, 30, { cols: [pick([PAL.red, PAL.gold, PAL.green, 0x4aa8ff, PAL.pink]), 0xffffff], spd: [60, 160], g: 90, life: 1.1, add: true }); sfx.pop(); }
	}
}
if (typeof window !== "undefined") window.jump = (night, type) => {
	if (!G.active) return console.warn("Start a run first.");
	const i = Math.max(0, Math.min(NIGHTS - 1, night - 1));
	type ??= G.map[i].opts[0];
	for (let k = 0; k < i; k++) G.map[k].done = true;
	G.night = i;
	enterNode(i, type);
};
