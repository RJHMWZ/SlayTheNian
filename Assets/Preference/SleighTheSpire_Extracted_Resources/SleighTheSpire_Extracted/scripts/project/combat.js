import { E, W, H, FH, PAL, MUTE, Scene, tween, wait, after, go, shake, zoomPunch, hitstop, flash, burst, confetti,
	floatText, Snow, clamp, rf, damp, pushOverlay, hasAnim, hasType, untween, tweening, fx, mixCol } from "./engine.js";
import { sfx, music } from "./audio.js";
import { icon, creature, backdrop, tiledBg } from "./art.js";
import { GIFTS, POTIONS, ST } from "./data.js";
import { ENEMIES } from "./enemies.js";
import { G, RUN, adv, giftVals, giftCost, canUpgrade, fireRelics, hasRelic, relicInst, relicSum, spiritTier, nightsLeft, addGift, rollRarity, randGiftId, finishNode,
	addSpirit, gainCookies, pick } from "./run.js";
import { curChar, T, POWER_START, POWER_GAIN, REINDEER } from "./characters.js";
import { PARTS, partIds, pipsMax, pipHit, SOFT_DMG, JEALOUSY, likeLabel, ledger, ledgerNote } from "./parts.js";
import { HUD, makeCard, refreshCard, cardBright, giftTip, drawStatuses, statusKw, bubble, banner, openBag, boxCols, Confirm, ChoosePart, GiftPicker, CW, CH } from "./ui.js";
import { RewardScene, GameOverScene, VictoryScene } from "./scenes.js";



// move everyone up a bit
const GROUND = 226, PX = 150, BAGX = 30, BAGY = 336, DISCX = 610, DISCY = 336, ORBX = 36, ORBY = 292;
const SLOTS = { 1: [452], 2: [404, 514], 3: [372, 460, 548] };
const ALLY_SLOTS = [94, 52, 73];
const BG_UP = 110, BACKDROP_UP = 12;
const shuffle = a => { for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };

const mixPulse = (c, t) => { const k = 0.5 + 0.5 * Math.sin(t * 8), m = (a, s) => Math.round(((c >> s) & 255) + (255 - ((c >> s) & 255)) * k * 0.5); return (m(c, 16) << 16) | (m(c, 8) << 8) | m(c, 0); };
export class Combat extends Scene {
	constructor(ids, kind = "normal", opts = {}) { super(); this.ids = ids; this.kind = kind; this.opts = opts; this.run = RUN; }

	enter() {
		Object.assign(this, { turn: 0, phase: "intro", locked: true, over: false, left: false, hand: [], drawP: [], disc: [], cons: [],
			energy: 0, maxEnergy: 3, atksThisTurn: 0, sklsThisTurn: 0, playedThisTurn: 0, streak: 0, sel: null, hoverC: null, aim: null, focus: 0,
			foesAll: [], potAim: null, doubleNext: false, pocketBonus: false, transformQ: null, transformed: false, friendsWon: [], secretQ: null,
			queue: [], running: false, wantEnd: false, staged: [], beatT: 0 });
		this.buildBG();
		this.hud = new HUD(this, this);
		const ch = this.ch = curChar();
		const p = this.p = { isPlayer: true, name: ch.name, hp: G.hp, maxHp: G.maxHp, ghost: G.hp, block: 0, st: {}, x: PX, y: GROUND, dirty: true, lo: 0, kb: 0, sqx: 1, sqy: 1 };
		p.g = this.g("World", PX, GROUND);
		const d = creature(p.g, "Claus", ch.prefix + "idle"); p.w = d.w; p.h = d.h; p.ck = d.k;
		p.g.x = -40; tween(p.g, { x: PX }, 0.5, "outCubic");
		const xs = SLOTS[this.ids.length] || SLOTS[3];
		this.hpMul = this.ids.length >= 3 && this.kind !== "boss" ? (this.kind === "elite" ? 0.85 : 0.8) : 1;
		this.ids.forEach((id, i) => this.spawn(id, xs[i], 0.15 + i * 0.12));
		this.drawP = shuffle(G.bag.map(gi => this.mk(gi)));
		this.buildUI();
		music.play(this.kind === "boss" ? "boss" : "battle");
		this.intro();
	}
	exit() { this.left = true; E.tempo = 1; super.exit(); }
	mk(gi) { return { gi: { id: gi.id, up: gi.up, uid: gi.uid, part: gi.part || null, free: false }, def: GIFTS[gi.id], g: null, delay: 0 }; }

	spawn(id, x, delay = 0, summoned = false) {
		const def = ENEMIES[id], mul = summoned ? 1 : this.hpMul ?? 1;
		const tough = (adv(1) && def.tier === "elite" ? 1.1 : 1) * (adv(2) ? 1.1 : 1) * (adv(10) && def.tier === "boss" ? 1.2 : 1);
		const hp = Math.max(1, Math.round((def.hp[0] + Math.floor(Math.random() * (def.hp[1] - def.hp[0] + 1))) * mul * tough));
		const u = { def, id, name: def.name, hp, maxHp: hp, ghost: hp, block: 0, st: {}, x, y: GROUND, turn: 0, hist: [],
			seed: Math.random() < 0.5 ? 0 : 1, dead: false, dirty: true, lo: 0, kb: 0, sqx: 1, sqy: 1, ph: Math.random() * 6 };
		u.g = this.g("World", x, u.y);
		const dims = creature(u.g, "Enemy", id), sc = def.scale || 1; u.w = dims.w * sc; u.h = dims.h * sc; u.ck = dims.k;
		u.g.s = 0; tween(u.g, { s: sc }, 0.45, "outBack", delay);
		u.pipMax = pipsMax(def) + (adv(6) && pipsMax(def) > 0 ? 1 : 0); u.pips = u.pipMax; u.soft = false;
		if (def.moods) this.setMood(u, 0, false);
		ledgerNote("seen", id);
		if (summoned) { u.justSpawned = true; burst(x, u.y - 10, 20, { cols: [0xb5713a, 0xffffff], spd: [40, 140] }); sfx.pop(); }
		def.start?.(u, this);
		u.ig = this.g("UI", x, u.y - u.h - 8);
		this.foesAll.push(u);
		return u;
	}
	foes() { return this.foesAll.filter(f => !f.dead && !f.charmed); }
	allies() { return this.foesAll.filter(f => !f.dead && f.charmed); }
	tgtOf(c) { return c.def.tgt; }
	likes(u) { return { love: u.love ?? u.def.love, hate: u.hate ?? u.def.hate }; }
	setMood(u, i, announce) {
		const m = u.def.moods[i % u.def.moods.length]; u.mood = i; u.love = m.love; u.hate = m.hate;
		if (announce) { floatText(u.x, u.y - u.h - 40, `NOW WANTS: ${likeLabel(m.love).toUpperCase()}`, PAL.pink, { rise: 10, hold: 1.2 }); burst(u.x, u.y - u.h * 0.6, 10, { cols: [PAL.pink, 0xffffff], g: -40 }); }
		u.dirty = true;
	}
	charmable(u) { return u && !u.dead && !u.charmed && u.pipMax > 0 && u !== this.transformQ; }
	randFoe() { const f = this.foes().filter(f => f !== this.transformQ); return f.length ? f[Math.floor(Math.random() * f.length)] : null; }
	setAnim(u, state, dur = 0.45) {
		if (!u.ck || u.g.dead) return;
		const pre = this.ch?.prefix || "", base = u.isPlayer ? pre + "idle" : u.id, name = u.isPlayer ? pre + state : (state === "idle" ? u.id : `${u.id}_${state}`);
		if (!hasAnim(u.isPlayer ? "Claus" : "Enemy", name)) return;
		u.g.anim(u.ck, name);
		if (state !== "idle" && dur) { u._ab = (u._ab || 0) + 1; const tok = u._ab; after(dur, () => { if (u._ab === tok && !u.g.dead && !u.dead) u.g.anim(u.ck, base); }); }
	}

	buildBG() {
		const key = this.kind === "boss" ? "boss" : this.kind === "elite" ? "elite" : "battle";
		if (!tiledBg(this, W + 200, -100, -BG_UP, H + BG_UP)) backdrop(this, key, -BACKDROP_UP);
		this.snow = new Snow(this, 70, "BG", { wind: this.kind === "boss" ? 30 : 8 });
	}
	buildUI() {
		this.orb = this.g("UI", ORBX, ORBY);
		this.bagG = this.g("UI", BAGX, BAGY); this.discG = this.g("UI", DISCX, DISCY);
		this.hot({ x: BAGX - 18, y: BAGY - 18, w: 40, h: 26, tip: { t: "Sack (draw pile)", b: "Click to view." }, onClick: () => openBag(this.drawP.map(c => c.gi), "IN THE SACK (random order)") });
		this.hot({ x: DISCX - 22, y: DISCY - 18, w: 40, h: 26, tip: { t: "Sleigh (given gifts)", b: "Refills the Sack when it runs out." }, onClick: () => openBag(this.disc.map(c => c.gi), "IN THE SLEIGH") });
		this.endBtn = this.btn({ x: 546, y: 284, w: 88, h: 24, label: "END TURN", col: PAL.red2, key: "e", hold: () => E.settings.holdConfirm, onClick: () => this.requestEnd(),
			tip: () => ({ t: E.settings.holdConfirm ? "End Turn (hold E)" : "End Turn (E)" }) });
		this.react = this.g("Pop", 0, 0); this.react.vis = false; this.reactKey = "";
		this.arrow = this.g("Pop", 0, 0);
		this.dots = [];
		if (hasAnim("Icon", "node") && hasAnim("Icon", "arrow")) for (let i = 0; i < 14; i++) this.dots.push(icon(this.arrow, i === 13 ? "arrow" : "node", 0, 0));
		this.arrow.vis = false;
		this.combo = this.g("UI", 92, 252); this.comboK = this.combo.t(0, 0, "", { col: PAL.gold, sh: true }); this.combo.a = 0;
		this.vig = this.g("FX", 0, 0); this.vig.a = 0;
		for (let i = 0; i < 6; i++) { const a = 0.22 - i * 0.035; this.vig.r(0, i * 3, W, 3, PAL.red, a); this.vig.r(0, H - 3 - i * 3, W, 3, PAL.red, a); this.vig.r(i * 3, 0, 3, H, PAL.red, a); this.vig.r(W - 3 - i * 3, 0, 3, H, PAL.red, a); }
		this.drawOrb(); this.drawPiles();
		this.buildPower();
	}
	drawOrb() {
		const g = this.orb; g.clear();
		icon(g, this.energy <= 0 && hasAnim("Icon", "orb_empty") ? "orb_empty" : "orb", 0, 0);
		g.t(0, -FH / 2, `${this.energy}/${this.maxEnergy}`, { al: "center", col: PAL.white, sh: true });
		this.orbHot ??= this.hot({ x: ORBX - 19, y: ORBY - 19, w: 38, h: 38, tip: { t: "Cheer", c: PAL.ice, b: "Refills every turn." } });
	}
	energyBump() { this.drawOrb(); this.orb.s = 1.15; tween(this.orb, { s: 1 }, 0.25, "outBack"); }
	orbShake() { [5, -4, 3, -2, 0].forEach((v, i) => tween(this.orb, { ox: v }, 0.04, "linear", i * 0.04)); this.orb.flash = 0.8; tween(this.orb, { flash: 0 }, 0.3); }
	drawPiles() {
		const b = this.bagG; b.clear(); icon(b, "sack", 0, -4); b.t(12, -4, String(this.drawP.length), { col: PAL.white, sh: true });
		const d = this.discG; d.clear(); icon(d, "sleigh", 0, -4); d.t(-14, -4, String(this.disc.length), { al: "right", col: PAL.white, sh: true });
		if (this.cons.length) d.t(-14, -18, "x" + this.cons.length, { al: "right", col: MUTE() });
	}
	pileBump(g) { g.s = 1.25; tween(g, { s: 1 }, 0.25, "outBack"); }

	async intro() {
		await wait(450); if (this.left) return;
		const col = this.kind === "elite" ? PAL.red : this.kind === "boss" ? 0xff3030 : PAL.gold;
		const nl = nightsLeft(G.night);
		await banner(this.kind === "boss" ? "CHRISTMAS EVE" : this.kind === "elite" ? "ELITE!" : `NIGHT ${G.night + 1}`, col, nl === 1 ? "The night before Christmas" : `${nl} nights before Christmas`, 0.6);
		const spoken = new Set();
		for (const e of this.foes()) {
			if (this.left) return;
			if (spoken.has(e.id)) continue;
			spoken.add(e.id);
			const line = e.def.lines[Math.floor(Math.random() * e.def.lines.length)];
			e.sqy = 1.25; e.sqx = 0.85; tween(e, { sqy: 1, sqx: 1 }, 0.35, "outBack"); sfx.pop();
			this.curBubble = bubble(this, e.x, e.y - e.h - 12, T(line));
			await this.curBubble.done; this.curBubble = null;
		}
		if (this.left) return;
		fireRelics("battleStart", this);
		for (const e of this.foes()) this.chooseIntent(e, true);
		await this.startTurn();
	}
	async startTurn() {
		if (this.over || this.left) return;
		E.tempo = 1;
		for (const u of [this.p, ...this.foesAll]) this.settle(u);
		this.turn++; this.phase = "player"; this.atksThisTurn = this.sklsThisTurn = this.playedThisTurn = this.streak = 0;
		const p = this.p;
		p.block = hasRelic("snowshovel") ? Math.min(p.block, 15) : 0; p.dirty = true;
		if (p.st.frostbite > 0) { this.lose(p, p.st.frostbite, PAL.ice); p.st.frostbite--; if (this.over) return; }
		if (p.st.cocoa) this.healP(p.st.cocoa);
		if (p.st.choir) this.apply(p, "might", p.st.choir);
		if (p.st.snowfall) this.applyAll("frostbite", p.st.snowfall);
		this.maxEnergy = 3 + relicSum("energy");
		let en = this.maxEnergy + (p.st.star || 0) + (p.st.jolly || 0), dn = 5 + (p.st.drawnext || 0) + (p.st.workshop || 0);
		delete p.st.jolly; delete p.st.drawnext;
		if (this.pocketBonus) { dn += 3; this.pocketBonus = false; }
		const t = spiritTier();
		if (t.id === "jolly") { en++; floatText(ORBX, ORBY - 30, "JOLLY! +1", PAL.gold); }
		else if (t.id === "festive" && this.turn === 1) { en++; floatText(ORBX, ORBY - 30, "FESTIVE! +1", t.col); }
		if (t.id === "grim") dn--;
		this.energy = en; this.energyBump();
		sfx.turn(); banner("YOUR TURN", PAL.gold, "", 0.3);
		fireRelics("turnStart", this);
		await wait(380); if (this.left) return;
		this.draw(dn);
		await wait(150);
		await this.afterAction();
		if (!this.over) this.locked = false;
	}
	requestEnd() {
		if (this.locked || this.phase !== "player" || this.over) return;
		if (E.settings.confirmEnd && this.energy > 0 && this.hand.some(c => this.playable(c))) {
			pushOverlay(new Confirm("You still have Cheer and gifts you can give. End your turn anyway?", () => this.endTurn(), "END TURN", "KEEP GOING"));
			return;
		}
		this.endTurn();
	}
	async endTurn() {
		if (this.locked || this.phase !== "player" || this.over) return;
		if (this.running || this.queue.length) { this.wantEnd = true; return; }
		this.locked = true; this.cancelSel(); this.potAim = null; this.phase = "enemy";
		await this.resolveSoft(); if (this.over || this.left) return;
		const p = this.p;
		if (p.st.candles) { for (const f of this.foes()) { this.zap(f, p.st.candles); burst(f.x, f.y - f.h / 2, 8, { cols: [PAL.orange, PAL.gold], g: -80 }); } await wait(250); }
		if (p.st.tinsel) this.gainBlock(p, p.st.tinsel, true);
		fireRelics("turnEnd", this);
		for (const c of this.hand) if (c.gi.id === "naughty") this.lose(p, 2, PAL.purple);
		await this.afterAction(); if (this.over || this.left) return;
		const hand = this.hand; this.hand = []; this.hoverC = null;
		hand.forEach((c, i) => {
			const g = c.g; c.g = null; c.gi.free = false;
			if (!g) return;
			if (c.def.flags.includes("m")) { this.cons.push(c); burst(g.x, g.y, 10, { cols: [0x555566, 0x888899], g: -60 }); tween(g, { a: 0, s: 0.5, y: g.y - 30 }, 0.3, "inQuad", i * 0.03, () => g.kill()); }
			else { this.disc.push(c); tween(g, { x: DISCX, y: DISCY, s: 0.15, a: 0.4, rot: g.rot + 3 }, 0.34, "inBack", i * 0.04, () => { g.kill(); this.pileBump(this.discG); }); }
		});
		if (hand.length) sfx.draw();
		this.drawPiles();
		this.decay(p);
		this.hideCombo();
		await wait(380); if (this.left) return;
		await this.alliesAct(); if (this.over || this.left) return;
		await this.enemyTurn();
	}
	async enemyTurn() {
		if (this.over || this.left) return;
		if (E.settings.fastEnemy) E.tempo = 2.2;
		sfx.enemyTurn(); await banner("ENEMY TURN", PAL.red, "", 0.25);
		for (const e of this.foes()) {
			if (this.over || this.left) return;
			if (e.dead) continue;
			e.block = 0; e.dirty = true;
			if (e.st.frostbite > 0) { this.lose(e, e.st.frostbite, PAL.ice); e.st.frostbite--; await wait(260); await this.afterAction(); if (this.over || this.left) return; if (e.dead) continue; }
			if (e.justSpawned || e.skipTurn) {
				if (e.skipTurn && !e.justSpawned) { floatText(e.x, e.y - e.h - 20, "TOO FLUSTERED...", PAL.pink, { rise: 10 }); await wait(300); }
				e.justSpawned = false; e.skipTurn = false; this.intentsDirty = true; continue;
			}
			await this.doMove(e);
			this.settle(e);
			await this.afterAction(); if (this.over || this.left) return;
			if (e.dead) continue;
			if (e.st.carol) this.apply(e, "might", e.st.carol);
			if (e.st.tinsel) this.gainBlock(e, e.st.tinsel, true);
			this.decay(e);
			await wait(240);
		}
		for (const e of this.foes()) { e.turn++; this.chooseIntent(e, true); if (e.def.moods && !e.soft && !e.dead) this.setMood(e, (e.mood || 0) + 1, true); }
		await this.startTurn();
	}
	async afterAction() {
		if (!this.over && !this.left && this.phase === "player") {
			const fs = this.foes().filter(f => f !== this.transformQ);
			if (fs.length === 1 && fs[0].soft && !fs[0].softHit) await this.charm(fs[0]);
		}
		if (this.secretQ && !this.over) await this.secretBoss();
		if (this.transformQ && !this.over) await this.transform();
		if (!this.over && !this.left && this.foes().length === 0) await this.victory();
	}
	decay(u) { for (const k in u.st) if (ST[k]?.turns && u.st[k] > 0) { u.st[k]--; if (!u.st[k]) delete u.st[k]; } u.dirty = true; this.intentsDirty = true; }

	chooseIntent(e, pop = false) {
		const key = e.def.ai(e, this), mv = e.def.moves[key];
		e.move = { key, name: mv.n, acts: typeof mv.a === "function" ? mv.a(e, this) : mv.a };
		e.hist.push(key);
		this.buildIntent(e, pop);
	}
	intentInfo(e) {
		let atk = null, def = 0, buff = 0, deb = 0, steal = false, flee = false;
		for (const a of e.move.acts) {
			if (a[0] === "atk") atk = { d: this.dmgCalc(e, this.p, a[1]), n: a[2] || 1 };
			else if (a[0] === "def") def += a[1];
			else if (a[0] === "steal" || a[0] === "pocket") steal = true;
			else if (a[0] === "flee") flee = true;
			else if (["buff", "buffAll", "heal", "healAll", "summon"].includes(a[0])) buff++;
			else deb++;
		}
		const r = {};
		const tail = (def && atk ? " and defend" : "") + (steal ? " and steal your cookies" : "") + (deb ? " and curse you" : "") + (buff ? " and grow stronger" : "");
		const I = { atk: "intent_attack", def: "intent_defend", buff: "intent_buff", deb: "intent_debuff", unk: "intent_unknown",
			esc: hasAnim("Icon", "intent_escape") ? "intent_escape" : "intent_unknown" };
		const set = (main, sub) => { r.main = I[main]; if (sub) r.sub = I[sub]; };
		if (flee) { set("esc"); r.desc = e.loot > 0 ? `Escape with ${e.loot} of your cookies` : "Run away"; }
		else if (atk) {
			set("atk", def ? "def" : deb ? "deb" : buff ? "buff" : null);
			r.txt = atk.n > 1 ? `${atk.d}x${atk.n}` : String(atk.d); r.total = atk.d * atk.n;
			r.desc = `Attack for ${atk.d}${atk.n > 1 ? ` x${atk.n}` : ""}${tail}`;
		} else if (def) { set("def", buff ? "buff" : deb ? "deb" : null); r.desc = "Defend" + tail; }
		else if (buff) { set("buff", deb ? "deb" : null); r.desc = "Grow stronger" + (deb ? " and curse you" : ""); }
		else if (deb) { set("deb"); r.desc = "Curse you"; }
		else if (steal) { set("deb"); r.desc = "Steal your cookies"; }
		else { set("unk"); r.desc = "Unknown"; }
		r.desc = `${e.move.name}: ${r.desc}.`;
		return r;
	}
	buildIntent(e, pop = false) {
		const g = e.ig; if (!g || g.dead) return;
		g.clear(); g.y = e.y - e.h - 8;
		if (e.dead || !e.move) return;
		if (e.soft || e.skipTurn) {
			if (e.soft) { icon(g, "heart", -6, 0); g.t(8, -6, e.softHit ? "HURT" : "SOFT", { col: e.softHit ? 0xff9090 : PAL.pink, sh: true }); }
			else g.t(0, -6, "Zz", { al: "center", col: MUTE(), sh: true });
			e.dangerous = false;
			if (pop) { g.s = 0; tween(g, { s: 1 }, 0.3, "outBack"); }
			return;
		}
		const r = this.intentInfo(e);
		icon(g, r.main, -6, 0);
		if (r.sub) icon(g, r.sub, 4, 6);
		if (r.txt) g.t(10, -6, r.txt, { col: r.total >= this.p.hp + this.p.block ? 0xff5050 : PAL.white, sh: true });
		e.dangerous = r.total >= 15;
		if (pop) { g.s = 0.6; tween(g, { s: 1 }, 0.25, "outBack"); }
	}

	dmgCalc(src, tgt, base) {
		let d = base + (src.st.might || 0);
		if (src.st.soggy > 0) d *= 0.75;
		if (tgt && tgt.st.brittle > 0) d *= 1.5;
		if (tgt && tgt.soft) d *= SOFT_DMG;
		if (src.isPlayer && this.doubleNext) d *= 2;
		if (!src.isPlayer && !src.charmed && adv(9)) d *= 1.1;
		return Math.max(0, Math.floor(d));
	}
	blockCalc(u, n, raw) { let b = n + (raw ? 0 : (u.st.nimble || 0)); if (!raw && u.st.chilled > 0) b *= 0.75; return Math.max(0, Math.floor(b)); }
	previewDmg(base, tgt = null) { return this.dmgCalc(this.p, tgt, base); }
	previewBlock(n) { return this.blockCalc(this.p, n, false); }
	costOf(gi) { return gi.free ? 0 : giftCost(gi); }
	canAct() { return this.phase === "player" && !this.locked && !this.over; }
	playable(c) { return this.canAct() && !c.def.flags.includes("u") && this.costOf(c.gi) <= this.energy; }

	lunge(src, dir, dist = 16) {
		if (src._lt) src._lt.dead = true;
		if (src._lt2) src._lt2.dead = true;
		src._lt = tween(src, { lo: dir * dist }, 0.07, "outQuad", 0, () => { src._lt2 = tween(src, { lo: 0 }, 0.24, "outCubic"); });
	}
	settle(u) {
		if (!u || u.dead || !u.g || u.g.dead) return;
		tween(u, { sqx: 1, sqy: 1, lo: 0 }, 0.18, "outQuad");
	}
	async strike(src, tgt, base, first = true) {
		const fwd = src.isPlayer || src.charmed ? 1 : -1;
		if (!src.isPlayer && first) {
			sfx.windup(); this.setAnim(src, "attack", 0.5);
			tween(src, { lo: -fwd * 8, sqx: 0.88, sqy: 1.12 }, 0.12, "outQuad");
			await wait(140); if (this.left || src.dead) return;
			src.sqx = 1.15; src.sqy = 0.9; tween(src, { sqx: 1, sqy: 1 }, 0.3, "outBack");
		}
		if (src.isPlayer) this.setAnim(src, "attack", 0.35);
		this.lunge(src, fwd, src.isPlayer ? 16 : 22);
		await wait(80); if (this.left || src.dead || tgt.dead) return;
		this.hitUnit(src, tgt, base);
		await wait(140);
	}
	hitUnit(src, tgt, base) {
		this.damage(tgt, this.dmgCalc(src, tgt, base), { src });
		if (tgt.st.holly > 0 && !src.dead && src.hp > 0) this.damage(src, tgt.st.holly, { thorns: true });
	}
	async atk(t, base, times = 1) {
		for (let i = 0; i < times; i++) {
			if (!t || t.dead || t === this.transformQ) t = this.randFoe();
			if (!t || this.over) return;
			await this.strike(this.p, t, base);
		}
	}
	async atkRand(base, times = 1) { for (let i = 0; i < times; i++) { const t = this.randFoe(); if (!t || this.over) return; await this.strike(this.p, t, base); } }
	async atkAll(base, times = 1) {
		for (let i = 0; i < times; i++) {
			const fs = this.foes().filter(f => f !== this.transformQ); if (!fs.length || this.over) return;
			this.setAnim(this.p, "attack", 0.35);
			this.lunge(this.p, 1); await wait(80); if (this.left) return;
			for (const f of fs) this.hitUnit(this.p, f, base);
			await wait(160);
		}
	}
	damage(u, d, o = {}) {
		if (!u || u.dead || this.left) return;
		this.softHurt(u);
		const hx = u.x, fy = u.y - u.h * 0.55, dir = u.isPlayer ? -1 : 1;
		if (d <= 0) { floatText(hx, fy - 10, "0", 0xaaaaaa, { num: true }); sfx.whiff(); return; }
		const blocked = Math.min(u.block, d), loss = d - blocked;
		if (u.isPlayer) { if (loss > 0) this.charge("hurt", loss); if (blocked > 0) this.charge("blocked", blocked); }
		u.block -= blocked;
		if (blocked > 0) {
			sfx.blockHit(); burst(hx - dir * 10, fy, 8, { cols: [PAL.ice, 0xffffff], spd: [40, 140], g: 120, a0: dir > 0 ? Math.PI * 0.6 : -Math.PI * 0.4, a1: dir > 0 ? Math.PI * 1.4 : Math.PI * 0.4 });
			if (!loss) { floatText(hx, fy - 10, "BLOCKED", PAL.ice); shake(1.5, dir * 2, 0); }
			if (u.block === 0 && blocked >= 4) { floatText(hx, fy - 24, "WRAP BROKEN!", PAL.ice, { rise: 14 }); burst(hx, fy, 14, { cols: [PAL.ice, 0xffffff], spd: [80, 180] }); }
		}
		if (loss > 0) {
			const big = loss >= 15;
			u.hp -= loss;
			floatText(hx + rf(-6, 6), fy - 10, String(loss), u.isPlayer ? 0xff6060 : PAL.white, { num: true, pop: true, big, dir });
			u.g.flash = 1; tween(u.g, { flash: 0 }, big ? 0.3 : 0.18);
			u.sqx = 1.15; u.sqy = 0.88; tween(u, { sqx: 1, sqy: 1 }, 0.3, "outBack");
			u.kb = dir * Math.min(18, 5 + loss * 0.6);
			this.setAnim(u, "hit", 0.35);
			fx(big ? "hit_big" : "hit", hx, fy, { rot: rf(-0.4, 0.4) }) || (big && fx("hit", hx, fy, { sc: 1.5 }));
			const a0 = dir > 0 ? -Math.PI * 0.45 : Math.PI * 0.55, a1 = dir > 0 ? Math.PI * 0.45 : Math.PI * 1.45;
			burst(hx, fy, 8 + Math.min(22, loss), { cols: u.isPlayer ? [PAL.red, 0xffffff] : [0xffffff, PAL.snow2, PAL.red, PAL.gold], spd: [80, 240], g: 380, life: 0.6, a0, a1, floor: GROUND + 6 });
			shake(Math.min(9, 1.5 + loss * 0.3), dir * Math.min(8, loss * 0.4), 0);
			hitstop(big ? 0.09 : 0.035);
			if (big) { zoomPunch(0.05); sfx.crit(); flash(0xffffff, 0.18, 0.15); } else sfx.hit(loss);
			if (u.isPlayer) { flash(0xff2020, 0.22, 0.3); G.hp = Math.max(0, u.hp); G.changed = true; }
		}
		u.dirty = true; this.intentsDirty = true;
		if (u.hp <= 0) this.kill(u);
	}
	zap(u, n) { this.damage(u, n); }
	lose(u, n, col = PAL.red) {
		if (!u || u.dead) return;
		this.softHurt(u);
		u.hp -= n; floatText(u.x, u.y - u.h * 0.55 - 10, "-" + n, col, { num: true, pop: true });
		u.g.flash = 0.8; tween(u.g, { flash: 0 }, 0.25); sfx.hit(n);
		if (u.isPlayer) { G.hp = Math.max(0, u.hp); G.changed = true; }
		u.dirty = true;
		if (u.hp <= 0) this.kill(u);
	}
	kill(u) {
		if (u.isPlayer) {
			if (this.over) return;
			const ang = relicInst("treeangel");
			if (ang && !ang.used) {
				ang.used = true; u.hp = Math.round(u.maxHp * 0.5); G.hp = u.hp; G.changed = true; u.dirty = true;
				floatText(u.x, u.y - u.h - 10, "TREE ANGEL!", PAL.gold, { big: true }); burst(u.x, u.y - 20, 40, { cols: [PAL.gold, 0xffffff], g: -100 }); sfx.heal(); flash(0xffffff, 0.5, 0.5);
				return;
			}
			this.defeat(); return;
		}
		if (u.def.phase2 && !this.transformed && !u.charmed) { u.hp = 0; this.transformQ = u; return; }
		if (u.dead) return;
		if (u.charmed) {
			u.dead = true; burst(u.x, u.y - u.h / 2, 20, { cols: [PAL.pink, 0xffffff], g: -60 });
			tween(u.g, { a: 0, s: 0.4 }, 0.35, "inBack", 0, () => { u.g.kill(); u.hg?.kill(); }); u.ig.kill(); return;
		}
		u.dead = true; u.hp = 0; G.stats.kills++; ledgerNote("kills", u.id);
		if (u.loot > 0) {
			G.cookies += u.loot; G.changed = true; sfx.coin();
			floatText(u.x, u.y - u.h - 24, `+${u.loot} COOKIES`, PAL.gold, { big: true });
			burst(u.x, u.y - u.h / 2, 20, { cols: [PAL.gold, 0xc89050], spd: [60, 180], g: 200 });
			u.loot = 0;
		}
		sfx.death(); hitstop(0.08); zoomPunch(0.03);
		this.setAnim(u, "die", 0);
		burst(u.x, u.y - u.h / 2, 18, { cols: [0xffffff, PAL.snow2], spd: [60, 200], g: 260, life: 0.7, floor: GROUND + 6 });
		this.whiteOut(u.g);
		u.ig.kill(); if (u.bars) tween(u.bars, { a: 0 }, 0.3, "linear", 0, () => u.bars.kill());
		fireRelics("enemyDie", this, u);
	}
	whiteOut(g) {
		const ks = g.k.filter(k => k.kind === "s" && k.i.effects?.length);
		let v = 1;
		const P = { get b() { return v; }, set b(x) { v = x; for (const k of ks) try { k.i.effects[0].setParameter(0, x); } catch (_) {} } };
		if (!ks.length) { g.flash = 0; tween(g, { flash: 1 }, 0.2); }
		tween(P, { b: 2 }, 0.2, "outQuad", 0, () => tween(g, { a: 0 }, 0.1, "linear", 0.08, () => g.kill()));
	}
	gainBlock(u, n, raw = false) {
		const b = this.blockCalc(u, n, raw); if (b <= 0 || u.dead) return;
		u.block += b; u.dirty = true; sfx.block();
		floatText(u.x - 22, u.y - u.h * 0.5, "+" + b, PAL.ice, { rise: 14, num: true });
		burst(u.x, u.y - u.h / 2, 12, { cols: [PAL.ice, 0xffffff], spd: [30, 90], g: -40, life: 0.5 });
		u.sqy = 1.12; u.sqx = 0.94; tween(u, { sqy: 1, sqx: 1 }, 0.3, "outBack");
		if (u.isPlayer) this.setAnim(u, "cast", 0.35);
	}
	block(n, raw = false) { this.gainBlock(this.p, n, raw); }
	apply(u, k, n) {
		if (!u || u.dead || !n || !ST[k]) return;
		const s = ST[k];
		if (s.debuff && n > 0 && u.st.ribbon > 0) { u.st.ribbon--; if (!u.st.ribbon) delete u.st.ribbon; floatText(u.x, u.y - u.h - 6, "RIBBON!", PAL.pink); sfx.block(); u.dirty = true; return; }
		u.st[k] = (u.st[k] || 0) + n; if (!u.st[k]) delete u.st[k];
		floatText(u.x, u.y - u.h - 4, `${n > 0 ? "+" : ""}${n} ${s.n}`, s.c, { rise: 16 });
		if (s.debuff) { sfx.debuff(); burst(u.x, u.y - u.h / 2, 12, { cols: [s.c, PAL.purple], spd: [20, 80], g: 120 }); u.sqx = 0.9; u.sqy = 1.08; tween(u, { sqx: 1, sqy: 1 }, 0.3, "outBack"); }
		else { sfx.buff(); burst(u.x, u.y - 4, 14, { cols: [s.c, 0xffffff], spd: [20, 70], a0: -2.4, a1: -0.7, g: -60 }); u.sqy = 1.15; tween(u, { sqy: 1 }, 0.35, "outBack"); if (u.isPlayer) this.setAnim(u, "cast", 0.35); }
		u.dirty = true; this.intentsDirty = true;
	}
	applyAll(k, n) { for (const f of this.foes()) this.apply(f, k, n); }
	healUnit(u, n) {
		const h = Math.min(n, u.maxHp - u.hp); if (h <= 0 || u.dead) return;
		u.hp += h; u.ghost = u.hp; u.dirty = true;
		floatText(u.x, u.y - u.h * 0.55, "+" + h, PAL.green, { num: true, pop: true }); sfx.heal();
		burst(u.x, u.y - u.h / 2, 14, { cols: [PAL.green, 0xffffff], g: -80, spd: [20, 60] });
		if (u.isPlayer) { G.hp = u.hp; G.changed = true; }
	}
	healP(n) { this.healUnit(this.p, n); }
	gainMaxHp(n) { const p = this.p; p.maxHp += n; p.hp += n; G.maxHp = p.maxHp; G.hp = p.hp; G.changed = true; p.dirty = true; floatText(p.x, p.y - p.h - 6, `+${n} MAX HP`, PAL.green); sfx.heal(); }
	gainEnergy(n) { this.energy += n; this.energyBump(); floatText(ORBX, ORBY - 26, "+" + n, PAL.ice); sfx.energy(); burst(ORBX, ORBY, 12, { cols: [PAL.ice, 0xffffff], spd: [40, 100], g: 0 }); }
	draw(n) {
		let drawn = 0;
		for (let i = 0; i < n; i++) {
			if (this.hand.length >= 10) { floatText(W / 2, 240, "HAND FULL", PAL.red); break; }
			if (!this.drawP.length) {
				if (!this.disc.length) break;
				this.drawP = shuffle(this.disc); this.disc = []; sfx.shuffle(); floatText(BAGX + 20, BAGY - 26, "SHUFFLE", MUTE());
				this.pileBump(this.bagG);
			}
			const c = this.drawP.pop(); this.hand.push(c); this.showCard(c, BAGX, BAGY, drawn * 0.07); drawn++;
		}
		this.drawPiles();
	}
	showCard(c, x, y, delay = 0) {
		c.g = makeCard(this, c.gi, x, y, "UI", this); c.g.s = 0.3; c.g.rot = -0.25; c.delay = delay; c.g.vis = delay <= 0;
		after(delay, () => { if (!this.left) { sfx.draw(); if (c.g) c.g.vis = true; } });
		this.restack();
	}
	addToHand(id, o = {}) {
		const c = this.mk({ id, up: !!o.up, uid: -1 }); c.gi.free = !!o.free;
		if (this.hand.length >= 10) { this.disc.push(c); this.drawPiles(); return; }
		this.hand.push(c); this.showCard(c, W / 2, 170); c.g.rot = 0; c.g.s = 1.6;
		burst(W / 2, 170, 18, { cols: [PAL.gold, 0xffffff], spd: [40, 140] });
	}
	randomGiftId(r) { return randGiftId(r ?? rollRarity(false)); }
	upgradeHand() {
		for (const c of this.hand) if (canUpgrade(c.gi) && c.g) {
			c.gi.up = true; const old = c.g; c.g = makeCard(this, c.gi, old.x, old.y, "UI", this); c.g.s = old.s * 1.3; c.g.rot = old.rot; old.kill();
			burst(c.g.x, c.g.y, 12, { cols: [0xc8ffb0, PAL.gold], spd: [30, 90], g: -40 });
		}
		this.restack(); sfx.buff();
	}
	async addStatusCards(src, id, n, pile) {
		for (let i = 0; i < n; i++) {
			const c = this.mk({ id, up: false, uid: -1 });
			if (pile === "draw") this.drawP.splice(Math.floor(Math.random() * (this.drawP.length + 1)), 0, c);
			else this.disc.push(c);
			if (pile === "bag") addGift(id);
			const tx = pile === "draw" ? BAGX : DISCX, ty = pile === "draw" ? BAGY : DISCY;
			const g = makeCard(this, c.gi, src.x, src.y - src.h / 2, "Pop"); g.s = 0.5; g.rot = 1;
			tween(g, { x: W / 2 + (i - (n - 1) / 2) * 40, y: 170, s: 0.85, rot: (i - (n - 1) / 2) * 0.15 }, 0.28, "outBack");
			tween(g, { x: tx, y: ty, s: 0.15, a: 0.3, rot: 3 }, 0.3, "inBack", 0.6, () => g.kill());
		}
		sfx.debuff();
		floatText(W / 2, 120, `+${n} ${GIFTS[id].name}${pile === "bag" ? " (permanent!)" : ""}`, PAL.purple);
		await wait(950); this.drawPiles();
	}
	summon(id) {
		const used = this.foes().map(f => f.x), x = SLOTS[3].find(sx => !used.some(u => Math.abs(u - sx) < 40));
		if (x === undefined) return;
		const u = this.spawn(id, x, 0, true); this.chooseIntent(u, true);
	}
	escape() {
		this.over = true; this.locked = true; this.phase = "over";
		floatText(PX, GROUND - 60, "ESCAPED!", 0xcccccc, { big: true }); burst(PX, GROUND - 20, 40, { cols: [0x606070, 0x909090], spd: [30, 120], g: -60, life: 1 });
		tween(this.p.g, { x: -60 }, 0.6, "inQuad");
		G.hp = this.p.hp; G.changed = true;
		after(0.9, () => { if (!this.left) finishNode(); });
	}
	async flee(e) {
		if (e.dead) return;
		e.dead = true; e.fled = true; e.hp = 0;
		floatText(e.x, e.y - e.h - 12, e.loot > 0 ? `ESCAPED WITH ${e.loot} COOKIES!` : "ESCAPED!", 0xcccccc, { big: !!e.loot });
		sfx.whoosh(); burst(e.x, GROUND - 6, 20, { cols: [0xdce8f8, 0xffffff], spd: [30, 110], g: 200, life: 0.6, floor: GROUND + 6 });
		e.ig.kill(); if (e.bars) tween(e.bars, { a: 0 }, 0.25, "linear", 0, () => e.bars.kill());
		for (const k of e.g.k) if (k.kind === "s") k.mir = !k.mir;
		tween(e.g, { x: W + 80 }, 0.55, "inQuad", 0.1, () => e.g.kill());
		await wait(650);
	}
	relicPing(id) { this.hud.ping(id); }
	say(u, text) { this.pBub?.kill?.(); this.pBub = bubble(this, u.x, u.y - u.h - 6, text, { hold: 0.8 }); }

	async doMove(e) {
		const mv = e.move; if (!mv) return;
		floatText(e.x, e.y - e.h - 30, mv.name, PAL.gold, { rise: 8, hold: 0.8 });
		e.ig.s = 1.4; tween(e.ig, { s: 0 }, 0.2, "inBack");
		let first = true;
		for (const a of mv.acts) {
			if (this.over || this.left || e.dead) return;
			switch (a[0]) {
				case "atk": for (let i = 0; i < (a[2] || 1); i++) { if (this.over || e.dead) return; await this.strike(e, this.p, a[1], first); first = false; } break;
				case "def": this.gainBlock(e, a[1]); await wait(160); break;
				case "buff": this.apply(e, a[1], a[2]); await wait(160); break;
				case "buffAll": for (const f of this.foes()) this.apply(f, a[1], a[2]); await wait(160); break;
				case "debuff": this.lunge(e, -1, 8); this.apply(this.p, a[1], a[2]); await wait(180); break;
				case "heal": this.healUnit(e, a[1]); await wait(160); break;
				case "healAll": for (const f of this.foes()) this.healUnit(f, a[1]); await wait(160); break;
				case "add": await this.addStatusCards(e, a[1], a[2], a[3]); break;
				case "summon": this.summon(a[1]); await wait(320); break;
				case "pocket": {
					const n = Math.min(G.cookies, a[1]);
					if (n > 0) {
						G.cookies -= n; G.changed = true; e.loot = (e.loot || 0) + n; e.dirty = true;
						floatText(e.x, e.y - e.h - 10, `STOLE ${n} COOKIES!`, PAL.gold); sfx.coin();
						burst(PX, GROUND - 30, 14, { cols: [PAL.gold, 0xc89050], spd: [60, 160] });
						this.lunge(e, 1, 10);
					}
					await wait(220); break;
				}
				case "flee": await this.flee(e); return;
				case "steal": {
					const n = Math.min(G.cookies, a[1]);
					if (n > 0) { G.cookies -= n; G.changed = true; floatText(e.x, e.y - e.h - 10, `-${n} COOKIES`, PAL.gold); sfx.coin(); burst(PX, GROUND - 30, 12, { cols: [PAL.gold, 0xc89050], spd: [60, 160] }); }
					await wait(200); break;
				}
			}
		}
	}

	navTargets() {
		if (!this.canAct()) return [];
		const out = [];
		if (this.potAim) { for (const f of this.foes()) out.push({ x: f.x, y: f.y - f.h / 2, w: Math.max(30, f.w), h: f.h, ref: f, click: true }); return out; }
		if (this.sel) {
			if (this.tgtOf(this.sel) === "e") for (const f of this.foes()) { if (f !== this.transformQ) out.push({ x: f.x, y: f.y - f.h / 2, w: Math.max(30, f.w), h: f.h, ref: f, click: true }); }
			else out.push({ x: W / 2, y: 180, w: 120, h: 60, ref: "play", click: true });
			return out;
		}
		for (const c of this.hand) if (c.g && c.delay <= 0) out.push({ x: c.g.x, y: Math.max(c.g.y, 300), w: CW * 0.8, h: CH * 0.6, ref: c, click: true });
		return out;
	}
	navDefault() {
		if (this.potAim) return this.foes()[0];
		if (this.sel) return this.tgtOf(this.sel) === "e" ? this.foes().find(f => f !== this.transformQ) : "play";
		return this.hand.find(c => this.playable(c)) || this.hand[0];
	}

	cardAt(x, y) {
		const order = [...this.hand].reverse();
		if (this.hoverC) order.unshift(this.hoverC);
		for (const c of order) { const g = c.g; if (!g || c.delay > 0) continue; const s = g.s; if (x >= g.x - CW / 2 * s && x <= g.x + CW / 2 * s && y >= g.y - CH / 2 * s && y <= g.y + CH / 2 * s + 20) return c; }
		return null;
	}
	unitAt(x, y, foesOnly = false) {
		const list = foesOnly ? this.foes().filter(f => f !== this.transformQ) : [...this.foes(), ...this.allies(), this.p];
		for (const u of list) if (Math.abs(x - u.x) <= Math.max(18, u.w / 2) && y >= u.y - u.h && y <= GROUND + 40) return u;
		return null;
	}
	restack() { for (const c of this.hand) c.g?.top(); (this.sel || this.hoverC)?.g?.top(); }
	select(c, byKey = false) {
		if (!this.playable(c)) { this.reject(c); return; }
		this.sel = c; this.sticky = false; this.selKey = byKey; this.pressX = E.mouse.x; this.pressY = E.mouse.y; this.hoverC = null;
		sfx.pick(); this.restack();
		c.g.s *= 1.1;
	}
	reject(c) {
		sfx.error();
		const g = c.g; if (g) [6, -5, 4, -2, 0].forEach((v, i) => tween(g, { ox: v }, 0.04, "linear", i * 0.04));
		const noCheer = !c.def.flags.includes("u") && this.costOf(c.gi) > this.energy;
		if (noCheer) this.orbShake();
		this.say(this.p, c.def.flags.includes("u") ? "I can't give that one." : noCheer ? "Not enough Cheer!" : "Wait a moment, dear.");
	}
	cancelSel() { this.sel = null; this.aim = null; this.selKey = false; this.sticky = false; }
	release(byKey) {
		const c = this.sel;
		if (this.tgtOf(c) === "e") { if (this.aim) this.play(c, this.aim); else this.cancelSel(); }
		else if (byKey || E.mouse.y < 232) this.play(c, null);
		else this.cancelSel();
	}
	play(c, t) {
		this.cancelSel(); this.hoverC = null;
		const def = c.def;
		this.energy -= this.costOf(c.gi); this.energyBump();
		this.hand.splice(this.hand.indexOf(c), 1);
		this.streak++;
		this.showCombo();
		const g = c.g; c.g = null;
		const slot = this.staged.length;
		const tx = t ? clamp((t.x + PX) / 2 + 30, 140, 520) : W / 2, ty = (t ? t.y - t.h / 2 - 40 : 180) - slot * 6;
		this.staged.push(g);
		tween(g, { x: tx + slot * 8, y: ty, s: 1.1, rot: 0 }, 0.14, "outCubic");
		sfx.play(this.streak - 1);
		this.enqueue(async () => {
			await wait(120); if (this.left) return;
			const i = this.staged.indexOf(g); if (i >= 0) this.staged.splice(i, 1);
			burst(g.x, g.y, 10, { cols: [...boxCols(c.gi.id), 0xffffff], spd: [50, 160], g: 300, life: 0.45 });
			sfx.unwrap(); fx("unwrap", g.x, g.y);
			tween(g, { s: 1.25, a: 0 }, 0.16, "inQuad", 0, () => g.kill());
			this.playedThisTurn++; if (def.type === "atk") this.atksThisTurn++; if (def.type === "skl") { this.sklsThisTurn++; this.charge("skl"); }
			this.charge("gift"); if (c.gi.part) this.charge("part");
			fireRelics("prePlay", this, def);
			if (def.tgt === "e" && (!t || t.dead || t.charmed)) t = this.randFoe();
			try { if (t || def.tgt !== "e") await def.play?.(this, t, giftVals(c.gi), c); await this.partFx(c.gi, t); this.pipFx(c.gi, def, t); } catch (err) { console.error("[Sleigh] gift error", def.id, err); }
			this.doubleNext = false;
			if (this.left) return;
			fireRelics("play", this, def);
			c.gi.free = false;
			if (def.type === "pow") burst(PX, GROUND - 30, 24, { cols: [PAL.gold, 0xffffff], g: -120, spd: [20, 90] });
			else if (def.flags.includes("x") || c.gi.part === "battery") { this.cons.push(c); floatText(tx, ty - 10, "CONSUMED", MUTE(), { rise: 10 }); }
			else { this.disc.push(c); this.pileBump(this.discG); }
			this.drawPiles();
		});
	}
	async partFx(gi, t) {
		const tgt = () => (t && !t.isPlayer && !t.dead && !t.charmed ? t : this.randFoe());
		switch (gi.part) {
			case "squeaker": { const f = tgt(); if (f) this.apply(f, "soggy", 1); break; }
			case "glitter": { const f = tgt(); if (f) this.apply(f, "brittle", 1); break; }
			case "bell": this.draw(1); break;
			case "spring": { const f = this.randFoe(); if (f) { await wait(90); this.zap(f, 4); } break; }
			case "musicbox": this.healP(2); break;
			case "windup": this.apply(this.p, "jolly", 1); break;
		}
	}

	// aimed gifts only touch their target, everything else checks every monster's taste
	pipFx(gi, d, t) {
		const aimed = d.tgt === "e" && t && !t.dead && !t.charmed;
		const pool = this.foes().filter(f => f.pipMax > 0 && f !== this.transformQ);
		const list = aimed ? pool.filter(f => f === t) : pool;
		const ribTo = !aimed && gi.part === "ribbon" ? pick(pool.filter(f => f.pips > 0)) : null;
		for (const f of list) {
			const r = pipHit(gi, d, this.likes(f), aimed || f === ribTo);
			if (r.n || r.love || r.hate) this.pipChange(f, r);
		}
		if (aimed && JEALOUSY) for (const f of pool) {
			if (f === t) continue;
			const r = pipHit(gi, d, this.likes(f), false);
			if (r.hate) this.pipChange(f, { n: -1, love: false, hate: true, jealous: true });
		}
	}
	pipChange(f, r) {
		const fy = f.y - f.h - 6, before = f.pips;
		if (r.love) {
			ledgerNote("love", f.id); floatText(f.x, fy - 14, "LOVES IT!", PAL.pink, { rise: 18 }); sfx.love();
			burst(f.x, f.y - f.h * 0.7, 16, { cols: [PAL.pink, 0xff4f8f, 0xffffff], spd: [30, 120], g: -70, life: 0.8 });
		}
		if (r.hate) {
			ledgerNote("hate", f.id); floatText(f.x, fy - 14, r.jealous ? "HATES THAT!" : "HATES IT!", 0xff7070, { rise: 18 }); sfx.hate();
			burst(f.x, f.y - f.h * 0.7, 10, { cols: [0x333333, PAL.red], spd: [40, 120], g: -40 });
		}
		f.pips = clamp(f.pips - r.n, 0, f.pipMax);
		if (f.pips < before) {
			this.charge("pip", before - f.pips);
			floatText(f.x + rf(-6, 6), GROUND + 2, `-${before - f.pips} GRUMPY`, 0xd8a0ff, { num: true, pop: true, rise: 8 });
			f.sqy = 1.18; f.sqx = 0.9; tween(f, { sqx: 1, sqy: 1 }, 0.35, "outBack"); sfx.pop();
		} else if (f.pips > before) {
			f.sqx = 1.1; f.sqy = 0.92; tween(f, { sqx: 1, sqy: 1 }, 0.25, "outBack"); shake(2, 2, 0);
			if (f.soft) { f.soft = false; f.softHit = false; f.skipTurn = false; floatText(f.x, fy - 28, "GRUMPY AGAIN!", 0xff9090); }
		}
		if (f.pips === 0 && !f.soft) this.soften(f);
		f.dirty = true; this.intentsDirty = true;
	}
	soften(f) {
		f.soft = true; f.softHit = false; f.skipTurn = true;
		floatText(f.x, f.y - f.h - 26, "SOFTENED!", PAL.pink, { big: true });
		sfx.charm(); hitstop(0.05); zoomPunch(0.02);
		for (let i = 0; i < 2; i++) after(i * 0.15, () => burst(f.x, f.y - f.h * 0.6, 14, { cols: [PAL.pink, 0xffffff], spd: [30, 110], g: -60, life: 0.9 }));
		if (!this.softTold) { this.softTold = true; after(0.5, () => { if (!this.left && f.soft && !f.dead) this.say(this.p, "Easy now... leave it be and it might come round!"); }); }
	}
	softHurt(u) {
		if (!u.soft || u.softHit || this.phase !== "player") return;
		u.softHit = true; this.intentsDirty = true; u.dirty = true;
		floatText(u.x, u.y - u.h - 30, "IT WON'T JOIN NOW", 0xff9090, { rise: 10 });
	}
	async resolveSoft() {
		for (const f of this.foes()) {
			if (!f.soft) continue;
			if (!f.softHit) { await this.charm(f); if (this.left) return; continue; }
			f.soft = false; f.softHit = false; f.pips = f.pipMax; f.dirty = true; this.intentsDirty = true;
			floatText(f.x, f.y - f.h - 20, "GRUMPY AGAIN!", 0xff9090, { rise: 10 });
		}
		await this.afterAction();
	}
	buildPower() {
		const pw = this.ch.power; if (!pw) return;
		this.power = POWER_START; this.deck = [...REINDEER].sort(() => Math.random() - 0.5); this.deerI = 0;
		this.powBtn = this.btn({ x: 546, y: 254, w: 88, h: 22, label: pw.name, col: mixCol(this.ch.col, 0x000000, 0.5), key: "q", onClick: () => this.usePower(),
			tip: () => {
				const b = [pw.desc, { s: this.power >= 100 ? "READY! (Q)" : `${Math.floor(this.power)}%`, c: this.power >= 100 ? PAL.gold : PAL.mute }];
				if (this.ch.id === "rudolph") { const d = this.nextDeer(); b.splice(1, 0, { s: `Next: ${d.name}. ${d.desc}`, c: PAL.pink }); }
				return { t: pw.name, c: this.ch.col, b };
			} });
		const bg = this.powBtn.g, lab = this.powBtn.lab;
		this.powFill = hasType("PowerFill") ? bg.sp("PowerFill", -44, -11, { w: 0, h: 22, col: this.ch.col }) : null;
		lab.shk?.i.moveToTop(); lab.i.moveToTop();
		this.powDeer = this.g("UI", 590, 244); this.powDeerK = this.powDeer.t(0, 0, "", { al: "center", col: PAL.pink, sh: true });
	}
	nextDeer() { return this.deck[this.deerI % this.deck.length]; }
	charge(kind, n = 1) {
		const g = POWER_GAIN[this.ch?.id]?.[kind]; if (!g || this.power === undefined || this.over) return;
		const was = this.power; this.power = Math.min(100, this.power + g * n);
		if (was < 100 && this.power >= 100) { sfx.spirit(); floatText(590, 240, "READY!", this.ch.col, { big: true }); burst(590, 266, 18, { cols: [this.ch.col, 0xffffff, PAL.gold], spd: [30, 120], g: -40 }); if (this.powBtn) this.powBtn.g.s = 1.3; }
	}
	powerReady() {
		if (!this.ch.power || this.power < 100 || !this.canAct() || this.running || this.potAim) return false;
		if (this.ch.id === "tinsel" && !this.hand.some(c => c.def.type !== "curse")) return false;
		return true;
	}
	spendPower() { this.power = 0; if (this.powBtn) { this.powBtn.g.s = 1.25; } flash(this.ch.col, 0.15, 0.3); zoomPunch(0.03); }
	usePower() {
		if (!this.powerReady()) { if (this.power < 100) { sfx.error(); this.say(this.p, "Not yet... it's still charging!"); } return; }
		this.cancelSel();
		if (this.ch.id === "mrs") { this.potAim = { power: true }; sfx.pick(); return; }
		if (this.ch.id === "tinsel") return this.tinker();
		if (this.ch.id === "rudolph") { this.spendPower(); this.enqueue(() => this.reindeer()); }
	}
	kindWord(t) {
		if (!t || t.dead || t.charmed) return;
		this.spendPower(); this.setAnim(this.p, "cast", 0.4); sfx.charm();
		this.enqueue(async () => {
			floatText(PX, GROUND - 70, "Now, now...", PAL.pink, { rise: 8 });
			await wait(200); if (this.left || t.dead) return;
			if (t.pipMax > 0) {
				const lk = this.likes(t);
				if (lk.love) { ledgerNote("love", t.id); floatText(t.x, t.y - t.h - 40, `LOVES ${likeLabel(lk.love).toUpperCase()}`, PAL.pink, { rise: 12, hold: 1.4 }); }
				if (!t.soft) { const pw = this.power; this.pipChange(t, { n: 1, love: false, hate: false }); this.power = pw; }
			}
			const deb = pick(["soggy", "brittle", "chilled", "frostbite"]);
			this.apply(t, deb, t.pipMax > 0 ? 1 : 2);
			await wait(200);
		});
	}
	tinker() {
		const pool = partIds(), ids = [];
		while (ids.length < 3 && ids.length < pool.length) { const id = pick(pool); if (!ids.includes(id)) ids.push(id); }
		pushOverlay(new ChoosePart({ ids, title: "TINKER!", sub: "Then pick a gift in your hand to fit it on.", skipLabel: "NOT NOW", onPick: pid => {
			const list = this.hand.filter(c => c.def.type !== "curse").map(c => c.gi);
			pushOverlay(new GiftPicker({ title: `FIT THE ${PARTS[pid].name.toUpperCase()}`, sub: "It replaces any part already on that gift.", list, cancel: "CANCEL",
				onPick: gi => {
					const c = this.hand.find(h => h.gi === gi); if (!c) return;
					this.spendPower(); sfx.clink?.(); this.setAnim(this.p, "cast", 0.4);
					const old = gi.part; gi.part = pid;
					const bag = gi.uid >= 0 && G.bag.find(b => b.uid === gi.uid); if (bag) { bag.part = pid; G.changed = true; }
					if (c.g) { const { x, y, rot } = c.g; c.g.kill(); c.g = makeCard(this, gi, x, y, "UI", this); c.g.rot = rot; c.g.s = 1.5; burst(x, y, 20, { cols: [PARTS[pid].col, PAL.gold, 0xffffff], spd: [40, 140] }); }
					floatText(W / 2, 220, old ? `${PARTS[old].name} -> ${PARTS[pid].name}!` : `${PARTS[pid].name} fitted!`, PARTS[pid].col, { big: true });
					this.restack();
				} }));
		} }));
	}
	async reindeer() {
		const d = this.nextDeer(); this.deerI++;
		const an = hasAnim("Enemy", "deer_" + d.id) ? "deer_" + d.id : "deer";
		const g = this.g("World", -40, GROUND); creature(g, "Enemy", an); g.flipX = true;
		tween(g, { x: PX + 70 }, 0.3, "outCubic"); sfx.whoosh();
		banner(d.name.toUpperCase() + "!", PAL.pink, d.desc, 0.4);
		await wait(450); if (this.left) { g.kill(); return; }
		const p = this.p, hitR = async (n, times = 1) => { for (let i = 0; i < times; i++) { const t = this.randFoe(); if (!t || this.over) return; tween(g, { ox: 16 }, 0.07, "outQuad", 0, () => tween(g, { ox: 0 }, 0.2)); await wait(70); this.damage(t, this.dmgCalc(p, t, n), { src: p }); await wait(110); } };
		switch (d.id) {
			case "dasher": this.draw(2); this.energy++; this.energyBump(); break;
			case "dancer": this.block(12); break;
			case "prancer": this.apply(p, "might", 2); break;
			case "vixen": { const t = this.randFoe(); if (t) { this.apply(t, "soggy", 2); this.apply(t, "brittle", 2); } break; }
			case "comet": await hitR(20); break;
			case "cupid": { const fs = this.foes().filter(f => f.pipMax > 0 && !f.soft && f !== this.transformQ); const t = pick(fs); if (t) this.pipChange(t, { n: 2, love: false, hate: false }); else await hitR(6); break; }
			case "donner": for (const t of this.foes()) this.damage(t, this.dmgCalc(p, t, 8), { src: p }); shake(4); await wait(200); break;
			case "blitzen": await hitR(4, 5); break;
		}
		await wait(200); if (this.left) { g.kill(); return; }
		tween(g, { x: W + 60 }, 0.45, "inQuad", 0, () => g.kill());
	}
	tickPower(dt) {
		if (!this.powBtn) return;
		const full = this.power >= 100, ready = this.powerReady();
		if (this.powFill) {
			this.powFill.w = Math.round(88 * this.power / 100);
			this.powFill.col = full ? mixPulse(this.ch.col, this.t) : this.ch.col; this.powFill.cd = true;
		}
		this.powBtn.setEnabled(ready || (!full && this.canAct()));
		this.powBtn.o.pulse = ready;
		this.powBtn.g.s = damp(this.powBtn.g.s, 1, 10, dt);
		if (this.ch.id === "rudolph") { this.powDeer.setT(this.powDeerK, "NEXT: " + this.nextDeer().name.toUpperCase()); this.powDeer.a = this.canAct() ? 1 : 0.5; }
	}
	async friendHelp(def, steps, V, t) {
		const g = this.g("World", -30, GROUND), sc = def.scale || 1;
		const d = creature(g, "Enemy", def.id); g.flipX = true; g.s = sc;
		const fx0 = PX + 44 + d.w * sc / 2;
		tween(g, { x: fx0 }, 0.3, "outCubic"); sfx.whoosh();
		await wait(300); if (this.left) { g.kill(); return; }
		floatText(fx0, GROUND - d.h * sc - 14, def.moves ? GIFTS["f_" + def.id]?.move ?? def.name : def.name, PAL.pink, { rise: 8 });
		const aimAt = () => (t && !t.dead && !t.charmed ? t : this.randFoe());
		for (const s of steps) {
			if (this.over || this.left) break;
			const n = V[s.k];
			if (s.t === "atk") for (let i = 0; i < s.hits; i++) {
				const tg = aimAt(); if (!tg || this.over) break;
				tween(g, { ox: 18 }, 0.07, "outQuad", 0, () => tween(g, { ox: 0 }, 0.2, "outCubic"));
				await wait(80); if (this.left) break;
				this.damage(tg, this.dmgCalc(this.p, tg, n), { src: this.p });
				await wait(140);
			}
			else if (s.t === "def") { this.block(n); await wait(140); }
			else if (s.t === "buff") { this.apply(this.p, s.st, n); await wait(140); }
			else if (s.t === "debuff") { const tg = aimAt(); if (tg) { this.apply(tg, s.st, n); await wait(160); } }
			else if (s.t === "heal") { this.healP(n); await wait(140); }
			else if (s.t === "cookies") { gainCookies(n); await wait(140); }
		}
		if (this.left) { g.kill(); return; }
		burst(g.x, GROUND - 20, 12, { cols: [PAL.pink, 0xffffff], g: -80 });
		tween(g, { x: -40, a: 0 }, 0.35, "inQuad", 0.1, () => g.kill());
	}
	allySlot() { const used = this.allies().map(a => a.slot); return ALLY_SLOTS.findIndex((_, i) => !used.includes(i)); }
	async charm(u) {
		if (u.charmed || u.dead) return;
		u.charmed = true; u.pips = 0; u.soft = false; u.skipTurn = false; u.st = {}; u.block = 0;
		ledgerNote("charms", u.id); G.stats.charmed = (G.stats.charmed || 0) + 1;
		if (!u.def.phase2 && !G.charmed.includes(u.id)) G.charmed.push(u.id);
		if (u.loot > 0) { G.cookies += u.loot; G.changed = true; floatText(u.x, u.y - u.h - 24, `+${u.loot} COOKIES`, PAL.gold); u.loot = 0; }
		u.ig.clear(); u.bars?.kill(); u.bars = null;
		sfx.charm(); flash(PAL.pink, 0.25, 0.4); hitstop(0.06); zoomPunch(0.03);
		floatText(u.x, u.y - u.h - 20, "WON OVER!", PAL.pink, { big: true });
		for (let i = 0; i < 3; i++) after(i * 0.12, () => burst(u.x, u.y - u.h * 0.6, 16, { cols: [PAL.pink, 0xff4f8f, 0xffffff], spd: [40, 160], g: -60, life: 0.9 }));
		if (u.def.love) ledgerNote("love", u.id);
		if (u.def.hate) ledgerNote("hate", u.id);
		if (!u.def.phase2) { this.friendsWon.push(u.id); addSpirit(3); }
		const si = Math.max(0, this.allySlot()); u.slot = si;
		const tx = u.def.phase2 ? PX - 70 : ALLY_SLOTS[si];
		untween(u.g); u.lo = 0; u.kb = 0;
		u.hop = 0;
		tween(u.g, { x: tx }, 0.6, "outCubic", 0, () => { if (!u.g.dead) u.g.flipX = true; });
		u.x = tx; u.ig.x = tx;
		this.heartEyes(u);
		await wait(650); if (this.left) return;
		burst(tx, GROUND - 2, 12, { cols: [0xdce8f8, 0xffffff], spd: [30, 90], g: 200, floor: GROUND + 6 });
		if (u.def.phase2) { this.secretQ = u; return; }
		this.chooseIntent(u, true);
		for (const f of this.foes()) if (f.id === u.id && f.pipMax > 0 && !f.soft) { floatText(f.x, f.y - f.h - 34, "IF THEY LIKE YOU...", PAL.pink, { rise: 10 }); this.pipChange(f, { n: 1, love: false, hate: false }); }
	}
	heartEyes(u) {
		if (!u.ck || !hasType("HeartEye")) return;
		u.hg = this.g("World", 0, 0); u.hk = [u.hg.sp("HeartEye", 0, 0), u.hg.sp("HeartEye", 0, 0)];
		u.hg.s = 0; tween(u.hg, { s: 1 }, 0.4, "outBack", 0.3);
	}
	syncHearts(u) {
		if (!u.hk || u.g.dead) return;
		u.hk.forEach((k, i) => { try { k.x = u.ck.i.getImagePointX(i + 1); k.y = u.ck.i.getImagePointY(i + 1); } catch (_) { k.x = u.g.x + (i ? 3 : -3); k.y = u.g.y - u.h * 0.78; } });
		u.hg.top();
	}
	async alliesAct() {
		for (const a of this.allies()) {
			if (this.over || this.left || !this.foes().length) return;
			const mv = a.move; if (!mv) continue;
			floatText(a.x, a.y - a.h - 26, mv.name, PAL.pink, { rise: 8, hold: 0.7 });
			a.ig.s = 1.4; tween(a.ig, { s: 0 }, 0.2, "inBack");
			let first = true;
			for (const act of mv.acts) {
				if (this.over || this.left || a.dead) break;
				const f = this.randFoe();
				switch (act[0]) {
					case "atk": for (let i = 0; i < (act[2] || 1); i++) { const t = this.randFoe(); if (!t || this.over) break; await this.strike(a, t, act[1], first); first = false; } break;
					case "def": this.gainBlock(this.p, act[1]); await wait(160); break;
					case "buff": case "buffAll": this.apply(a, act[1], act[2]); await wait(140); break;
					case "debuff": if (f) { this.lunge(a, 1, 8); this.apply(f, act[1], act[2]); await wait(160); } break;
					case "heal": case "healAll": this.healP(Math.ceil(act[1] / 2)); await wait(140); break;
					case "steal": case "pocket": gainCookies(Math.min(15, act[1] || 10)); await wait(140); break;
					default: break;
				}
				await this.afterAction();
			}
			this.settle(a);
			a.turn++; this.chooseIntent(a, true);
			await wait(200);
		}
	}
	async secretBoss() {
		const santa = this.secretQ; this.secretQ = null; this.transformed = true; this.secretFight = true;
		const wasLocked = this.locked; this.locked = true; E.tempo = 1;
		const b = bubble(this, santa.x, santa.y - santa.h - 10, "{nick}...? I remember now. The LIST... it was the list all along!"); await b.done; if (this.left) return;
		music.stop(); sfx.transform();
		for (let i = 0; i < 6; i++) { shake(2 + i); burst(SLOTS[1][0], GROUND - 60, 14, { cols: [0xeadcb4, 0x2a1a1a, PAL.red], spd: [50, 200], g: -40, life: 0.8 }); await wait(150); if (this.left) return; }
		flash(0xffffff, 0.6, 0.5); zoomPunch(0.06);
		const L = this.spawn("naughtylist", SLOTS[1][0], 0);
		await wait(500); if (this.left) return;
		const b2 = bubble(this, L.x, L.y - L.h - 10, L.def.lines[0]); await b2.done; if (this.left) return;
		const army = G.charmed.filter(id => ENEMIES[id] && id !== "santa").slice(-8);
		if (army.length) {
			await banner("FRIENDS ARRIVE!", PAL.pink, "Everyone you were kind to came to help!", 0.5); if (this.left) return;
			for (const id of army) {
				const g = this.g("World", -40, GROUND);
				creature(g, "Enemy", id); g.flipX = true;
				tween(g, { x: L.x - 80 }, 0.32, "outCubic"); sfx.whoosh();
				await wait(330); if (this.left) return;
				tween(g, { x: L.x - 60 }, 0.06, "outQuad", 0, () => tween(g, { x: L.x - 80 }, 0.15));
				this.damage(L, 6);
				await wait(160);
				burst(g.x, GROUND - 20, 14, { cols: [PAL.pink, 0xffffff], g: -80 });
				tween(g, { a: 0, s: 0.3, y: GROUND - 30 }, 0.25, "inQuad", 0, () => g.kill());
				await wait(90); if (this.left) return;
			}
		}
		music.play("boss");
		await banner("THE NAUGHTY LIST", PAL.red, "The real villain of Christmas", 0.9); if (this.left) return;
		this.chooseIntent(L, true);
		this.chooseIntent(santa, true);
		this.locked = wasLocked;
	}

	enqueue(job) { this.queue.push(job); this.pump(); }
	async pump() {
		if (this.running) return;
		this.running = true;
		while (this.queue.length && !this.over && !this.left) {
			const job = this.queue.shift();
			await job();
			await this.afterAction();
		}
		this.running = false;
		for (const g of this.staged) g.kill(); this.staged = [];
		if (this.wantEnd && !this.over && !this.left) { this.wantEnd = false; this.endTurn(); }
	}
	showCombo() {
		if (this.streak < 2) return;
		const words = ["", "", "NICE", "MERRY", "JOLLY", "FESTIVE", "DAZZLING", "HO-HO-HOLY"];
		this.combo.setT(this.comboK, `x${this.streak} ${words[Math.min(words.length - 1, this.streak)]}!`);
		this.combo.setCol(this.comboK, this.streak >= 5 ? PAL.red : this.streak >= 3 ? PAL.gold : PAL.white);
		untween(this.combo); this.combo.a = 1; this.combo.s = 1.25;
		tween(this.combo, { s: 1 }, 0.2, "outBack");
	}
	hideCombo() { tween(this.combo, { a: 0 }, 0.3); }

	canUsePotion() { return this.canAct(); }
	usePotion(slot) {
		const id = G.potions[slot], d = POTIONS[id]; if (!d) return;
		if (id === "smoke" && this.kind !== "normal") { this.say(this.p, "No running from this one!"); sfx.error(); return; }
		if (d.tgt === "e") { this.potAim = { slot, d }; this.cancelSel(); return; }
		this.drink(slot, null);
	}
	drink(slot, t) {
		const d = POTIONS[G.potions[slot]]; if (!d) return;
		this.potAim = null;
		G.potions[slot] = null; G.changed = true; sfx.potion();
		burst(161 + slot * 17, 12, 16, { cols: [d.col, 0xffffff], spd: [30, 110], g: 200 });
		floatText(PX, GROUND - 70, d.name, d.col);
		this.setAnim(this.p, "cast", 0.4);
		this.enqueue(async () => { try { await d.use(this, t); } catch (err) { console.error(err); } });
	}

	async transform() {
		const u = this.transformQ; this.transformQ = null; this.transformed = true;
		const inEnemyPhase = this.phase === "enemy";
		const wasLocked = this.locked; this.locked = true; u.ig.clear();
		E.tempo = 1;
		const b = bubble(this, u.x, u.y - u.h - 10, "No... NO! The coal... it BURNS!"); await b.done; if (this.left) return;
		music.stop(); sfx.transform();
		for (let i = 0; i < 8; i++) { u.g.flash = i % 2; shake(3 + i); zoomPunch(0.01 * i); burst(u.x, u.y - u.h / 2, 16, { cols: [0x1a0a0a, PAL.red, 0x333333], spd: [50, 220], g: -50, life: 0.9 }); await wait(170); if (this.left) return; }
		flash(0xff0000, 0.8, 0.6); shake(12); hitstop(0.15); zoomPunch(0.08);
		u.g.kill();
		const def = ENEMIES[u.def.phase2], tier = spiritTier();
		const hp = Math.round(def.hp[0] * { jolly: 0.8, festive: 0.9, merry: 1, fading: 1.08, grim: 1.15 }[tier.id] * (adv(10) ? 1.2 : 1));
		Object.assign(u, { def, id: def.id, name: def.name, hp, maxHp: hp, ghost: hp, block: 0, st: {}, turn: 0, hist: [], dead: false, skipTurn: inEnemyPhase, pipMax: pipsMax(def), pips: pipsMax(def), soft: false, softHit: false, love: undefined, hate: undefined, dirty: true, lo: 0, kb: 0, sqx: 1, sqy: 1 });
		u.g = this.g("World", u.x, u.y);
		const d = creature(u.g, "Enemy", def.id); u.w = d.w; u.h = d.h; u.ck = d.k;
		u.g.s = 0.6; tween(u.g, { s: 1 }, 0.5, "outBack");
		const sub = { jolly: "The Christmas Spirit weakens him!", festive: "The Christmas Spirit cracks his armor!", merry: "The Christmas Spirit holds him back.", fading: "The fading Christmas Spirit feeds him...", grim: "Despair makes him unstoppable!" }[tier.id];
		if (tier.id === "jolly") { u.st.brittle = 2; u.st.soggy = 1; }
		if (tier.id === "festive") u.st.brittle = 1;
		if (tier.id === "fading") u.st.might = 1;
		if (tier.id === "grim") { u.st.might = 3; u.st.carol = 1; }
		if (adv(10)) u.st.might = (u.st.might || 0) + 1;
		music.play("boss");
		await banner("KRAMPUS", 0xff3030, `Spirit ${tier.name}: ${sub}`, 1.1); if (this.left) return;
		const b2 = bubble(this, u.x, u.y - u.h - 10, def.lines[0]); await b2.done; if (this.left) return;
		this.chooseIntent(u, true);
		if (inEnemyPhase && E.settings.fastEnemy) E.tempo = 2.2;
		this.locked = wasLocked;
	}

	async victory() {
		if (this.over) return;
		this.over = true; this.locked = true; this.phase = "over"; this.cancelSel(); this.potAim = null; E.tempo = 1;
		fireRelics("battleEnd", this);
		G.hp = Math.max(1, this.p.hp); G.maxHp = this.p.maxHp; G.changed = true;
		music.stop(); sfx.win(); this.setAnim(this.p, "victory", 0);
		zoomPunch(0.05);
		banner("VICTORY!", PAL.gold, this.kind === "boss" ? "Christmas is saved!" : "", 0.9);
		for (let i = 0; i < 3; i++) after(i * 0.2, () => confetti(rf(160, 480), rf(90, 160), 12, [PAL.red, PAL.green, PAL.gold, 0xffffff]));
		this.hand.forEach((c, i) => { if (c.g) tween(c.g, { y: H + 80 }, 0.35, "inQuad", i * 0.03); });
		if (this.kind === "elite") G.stats.elites++;
		await wait(1600); if (this.left) return;
		if (this.kind === "boss") { G.secret = !!this.secretFight; go(() => new VictoryScene()); }
		else go(() => new RewardScene(this.kind, { ...this.opts, friends: this.friendsWon }), true);
	}
	async defeat() {
		this.over = true; this.locked = true; this.phase = "over"; E.tempo = 1;
		G.hp = 0; G.changed = true;
		music.stop(); sfx.lose(); this.setAnim(this.p, "defeat", 0);
		flash(0x000000, 0.6, 1.2); zoomPunch(0.06);
		await wait(1500); if (this.left) return;
		go(() => new GameOverScene());
	}

	buildBars(u) {
		u.bars?.kill(); u.bars = null;
		if (u.dead || u.charmed) return;
		const bw = 56, g = u.bars = this.g("UI", u.x, GROUND + 6), foe = !u.isPlayer;
		g.r(-bw / 2 - 2, -2, bw + 4, 9, PAL.black); g.r(-bw / 2, 0, bw, 5, 0x3a1018);
		u.ghostK = g.r(-bw / 2, 0, Math.round(bw * Math.max(0, u.ghost) / u.maxHp), 5, 0xffffff);
		const col = u.block > 0 ? PAL.ice2 : PAL.red, fw = Math.round(bw * Math.max(0, u.hp) / u.maxHp);
		g.r(-bw / 2, 0, fw, 5, col); g.r(-bw / 2, 0, fw, 1, 0xffffff, 0.35);
		u.pvHK = g.r(0, 0, 0, 5, 0xffffff, 0);
		let ty = 8;
		u.pipOv = null;
		if (foe) {
			g.r(-bw / 2 - 2, 5, bw + 4, 8, PAL.black);
			if (u.pipMax > 0) {
				const n = u.pipMax, pw = Math.min(10, Math.floor((bw - (n - 1)) / n)), tw = n * pw + (n - 1), x0 = -Math.round(tw / 2);
				g.r(-bw / 2, 7, bw, 4, 0x1a1020);
				u.pipOv = [];
				for (let i = 0; i < n; i++) {
					const x = x0 + i * (pw + 1), full = i < u.pips;
					const col = u.soft ? (u.softHit ? 0x8a5a6a : PAL.pink) : full ? 0xb05ae0 : 0x3a2848;
					g.r(x, 7, pw, 4, col);
					if (full || u.soft) g.r(x, 7, pw, 1, 0xffffff, 0.35);
					u.pipOv.push(g.r(x, 7, pw, 4, 0xffffff, 0));
				}
			} else { g.r(-bw / 2, 7, bw, 4, 0x3a3a44); for (let x = -bw / 2 + 2; x < bw / 2; x += 6) g.r(x, 8, 3, 2, 0x55556a); }
			ty = 15;
		}
		g.t(0, ty, `${Math.max(0, u.hp)}/${u.maxHp}`, { al: "center", col: PAL.white, sh: true });
		if (u.block > 0) {
			icon(g, "shield", -bw / 2 - 8, 2);
			g.t(-bw / 2 - 13, -3, String(u.block), { al: "right", col: PAL.ice, sh: true });
		}
		drawStatuses(g, u.st, -bw / 2 - 6, ty + 12, 72);
		if (u._lastHp !== undefined && u._lastHp !== u.hp) { g.sx = 1.12; tween(g, { sx: 1 }, 0.25, "outBack"); }
		u._lastHp = u.hp;
	}

	tick(dt, active) {
		if (this.left) return;
		this.snow.update(dt); this.hud.tick(dt);
		const rm = E.settings.reduceMotion, all = [this.p, ...this.foesAll];
		for (const u of all) {
			if (u.dead || !u.g || u.g.dead) continue;
			const breath = rm ? 0 : Math.sin(this.t * 2.2 + (u.ph || 0)) * 0.018;
			u.kb = damp(u.kb, 0, 10, E.dts || dt);
			u.g.ox = u.lo + u.kb;
			u.g.oy = -(u.hop || 0);
			if (u.charmed) this.syncHearts(u);
			u.g.sx = u.sqx * (1 - breath * 0.5); u.g.sy = u.sqy * (1 + breath);
			if (u.ig && !u.ig.dead) {
				u.ig.oy = rm ? 0 : Math.sin(this.t * 3 + u.x) * 1.5;
				if (!tweening(u.ig) && u.move) u.ig.s = damp(u.ig.s, u.dangerous && this.phase === "player" ? 1 + 0.08 * Math.abs(Math.sin(this.t * 6)) : 1, 10, dt);
			}
			if (u.dirty) { u.dirty = false; this.buildBars(u); }
			if (u.ghost > u.hp) { u.ghostT = (u.ghostT || 0) + dt; if (u.ghostT > 0.35 && u.ghostK) { u.ghost = Math.max(u.hp, u.ghost - dt * u.maxHp * 0.9); u.ghostK.w = Math.round(56 * Math.max(0, u.ghost) / u.maxHp); } }
			else { u.ghost = u.hp; u.ghostT = 0; }
		}
		if (this.intentsDirty) { this.intentsDirty = false; for (const e of this.foes()) if (e.move) this.buildIntent(e); }
		if (this.phase === "intro" && active && (E.mouse.pressed || E.kp.has(" ") || E.kp.has("Enter"))) this.curBubble?.skip();
		const low = this.p.hp > 0 && this.p.hp < this.p.maxHp * 0.3 && !this.over;
		this.vig.a = damp(this.vig.a, low ? 0.6 + 0.4 * Math.max(0, Math.sin(this.t * 7)) : 0, 6, dt);
		if (low) { this.beatT -= dt; if (this.beatT <= 0) { this.beatT = 0.9; sfx.heartbeat(); } }
		if (this.hud.gearH) this.hud.gearH.key = this.sel || this.potAim ? null : "Escape";
		if (active) this.handleInput();
		this.layoutHand(dt);
		for (const c of this.hand) if (c.g) refreshCard(c.g, this, this.playable(c));
		this.orb.rot = damp(this.orb.rot, 0, 8, dt);
		if (this.energy > 0 && this.canAct() && !rm) this.orb.s = damp(this.orb.s, 1 + 0.04 * Math.sin(this.t * 4), 6, dt);
		this.updateArrow();
		this.tickPower(dt);
		this.updatePreview();
		this.updateTips(active);
		const can = this.canAct();
		this.endBtn.setEnabled(can);
		this.endBtn.setLabel(this.wantEnd ? "..." : "END TURN");
		this.endBtn.o.pulse = can && !this.hand.some(c => this.playable(c));
	}
	handleInput() {
		const m = E.mouse;
		if (!this.canAct()) { this.hoverC = null; if (this.sel) this.cancelSel(); return; }
		if (this.potAim) {
			this.aim = this.unitAt(m.x, m.y, true);
			if (m.rpressed || E.kp.has("Escape")) { this.potAim = null; this.aim = null; return; }
			if (m.pressed && !E.hovered) { const pa = this.potAim; this.potAim = null; if (this.aim) { if (pa.power) this.kindWord(this.aim); else this.drink(pa.slot, this.aim); } this.aim = null; }
			return;
		}
		if (!this.sel) {
			const h = E.hovered ? null : this.cardAt(m.x, m.y);
			if (h !== this.hoverC) { this.hoverC = h; this.restack(); if (h) sfx.cardHover(this.hand.indexOf(h)); }
			if (m.pressed && h) this.select(h);
			for (let i = 0; i < 10; i++) if (E.kp.has(String((i + 1) % 10)) && this.hand[i]) { this.select(this.hand[i], true); break; }
			if (E.kp.has("Tab")) this.focus++;
			return;
		}
		const c = this.sel;
		if (!this.hand.includes(c)) { this.cancelSel(); return; }
		if (m.rpressed || E.kp.has("Escape")) { this.cancelSel(); sfx.whiff(); return; }
		if (E.kp.has("Tab")) { this.focus++; this.selKey = true; }
		if (this.tgtOf(c) === "e") {
			const fs = this.foes().filter(f => f !== this.transformQ);
			const prev = this.aim;
			this.aim = this.unitAt(m.x, m.y, true) ?? (this.selKey && fs.length ? fs[this.focus % fs.length] : null);
			if (this.aim && this.aim !== prev) sfx.tick();
		} else this.aim = null;
		for (let i = 0; i < 10; i++) if (E.kp.has(String((i + 1) % 10)) && this.hand[i]) {
			if (this.hand[i] === c) { this.release(true); return; }
			this.cancelSel(); this.select(this.hand[i], true); return;
		}
		if (E.kp.has("Enter") || E.kp.has(" ")) { this.release(true); return; }
		if (m.released && !this.sticky && !this.selKey) {
			if (Math.hypot(m.x - this.pressX, m.y - this.pressY) < 8) this.sticky = true; else this.release(false);
		} else if (m.pressed && (this.sticky || this.selKey)) {
			const other = this.cardAt(m.x, m.y);
			if (other && other !== c && m.y > 250) { this.cancelSel(); this.select(other); return; }
			this.release(false);
		}
	}
	// cards spring to their slot every frame (no tweens), so the fan never fights itself
	layoutHand(dt) {
		if (this.over) return;
		const n = this.hand.length, sp = Math.min(58, 420 / Math.max(1, n)), mid = (n - 1) / 2, x0 = W / 2 + 6 - mid * sp;
		const hi = this.hoverC ? this.hand.indexOf(this.hoverC) : -1;
		this.hand.forEach((c, i) => {
			if (!c.g) return;
			if (c.delay > 0) { c.delay -= dt; return; }
			const off = i - mid;
			let tx = x0 + i * sp, ty = 318 + off * off * 1.4, ts = 1, tr = off * 0.06;
			if (hi >= 0 && i !== hi) tx += i < hi ? -18 : 18;
			if (c === this.hoverC) { ty = 272; ts = 1.28; tr = 0; }
			if (c === this.sel) {
				if (this.tgtOf(c) === "e") { tx = W / 2 + 6; ty = 258 + Math.sin(this.t * 5) * 2; ts = 1.15; tr = Math.sin(this.t * 3) * 0.03; }
				else { tx = E.mouse.x; ty = Math.min(E.mouse.y, 300); ts = 1.12; tr = clamp(E.mouse.vx * 0.0012, -0.4, 0.4); }
			}
			const k = c === this.sel ? 28 : 14;
			c.g.x = damp(c.g.x, tx, k, dt); c.g.y = damp(c.g.y, ty, k, dt); c.g.s = damp(c.g.s, ts, k, dt); c.g.rot = damp(c.g.rot, tr, c === this.sel ? 10 : 12, dt);
			cardBright(c.g,
				c === this.sel ? (this.tgtOf(c) !== "e" && E.mouse.y < 232 ? 150 : 130)
				: c === this.hoverC ? 115
				: this.playable(c) ? 100 : 55);
		});
	}
	updateArrow() {
		const c = this.sel, pa = this.potAim;
		const on = (c && this.tgtOf(c) === "e" && c.g) || pa;
		for (const f of this.foesAll) if (f.g && !f.g.dead && (!on || f !== this.aim)) f.g.dim = 1;
		this.arrow.vis = !!on; if (!on) return;
		const sx = pa ? (pa.power ? 590 : 161 + pa.slot * 17) : c.g.x, sy = pa ? (pa.power ? 256 : 22) : c.g.y - CH / 2 * c.g.s;
		const ex = E.mouse.x, ey = E.mouse.y, cx = (sx + ex) / 2, cy = Math.min(sy, ey) - 70;
		const col = this.aim ? 0xff5050 : 0xffffff, n = this.dots.length;
		this.dots.forEach((k, i) => {
			const t = (i + 1) / n, head = i === n - 1;
			const tt = head ? 1 : t, iit = 1 - tt;
			const px = iit * iit * sx + 2 * iit * tt * cx + tt * tt * ex, py = iit * iit * sy + 2 * iit * tt * cy + tt * tt * ey;
			k.x = px; k.y = py;
			if (head) k.rot = Math.atan2(ey - cy, ex - cx);
			this.arrow.setCol(k, col);
		});
		this.arrow.top();
		if (this.aim && !this.aim.dead) this.aim.g.dim = 1.15 + Math.sin(this.t * 12) * 0.1;
	}
	knownPip(gi, d, f, aimed) {
		const L = ledger(), r = pipHit(gi, d, this.likes(f), aimed), open = !!f.def.moods;
		const love = r.love && (open || !!L.love[f.id]), hate = r.hate && (open || !!L.hate[f.id]);
		return { n: (love ? 2 : 0) + (r.rib ? 1 : 0) - (hate ? 1 : 0), love, hate };
	}
	updatePreview() {
		const c = this.sel, aim = this.aim, blink = 0.45 + 0.45 * Math.sin(this.t * 10), live = c && this.canAct();
		for (const f of this.foes()) {
			let dmg = 0, pip = 0;
			if (live) {
				const V = giftVals(c.gi), d = c.def, hits = d.hits || 1, aimedHere = d.tgt === "e" && f === aim;
				if (V.d !== undefined && (aimedHere || (d.tgt === "a" && E.mouse.y < 232))) dmg = Math.max(0, this.previewDmg(V.d, f) * hits - f.block);
				if (f.pipMax > 0 && (aimedHere || (d.tgt !== "e" && E.mouse.y < 232))) pip = this.knownPip(c.gi, d, f, aimedHere).n;
				else if (JEALOUSY && f.pipMax > 0 && d.tgt === "e" && aim && aim !== f && this.knownPip(c.gi, d, f, false).hate) pip = -1;
			}
			const bw = 56;
			if (f.pvHK) {
				const hp = Math.max(0, f.hp), lost = Math.min(hp, dmg);
				f.pvHK.x = -bw / 2 + Math.round(bw * (hp - lost) / f.maxHp); f.pvHK.w = Math.round(bw * lost / f.maxHp); f.pvHK.a = lost ? blink : 0;
			}
			if (f.pipOv && f.bars) f.pipOv.forEach((k, i) => {
				const pops = pip > 0 && !f.soft && i < f.pips && i >= f.pips - pip, back = pip < 0 && i === f.pips;
				k.a = pops || back ? blink : 0;
				f.bars.setCol(k, back ? 0xff5050 : 0xffffff);
			});
		}
		const r = c && aim && aim.pipMax > 0 && this.canAct() ? this.knownPip(c.gi, c.def, aim, c.def.tgt === "e") : null;
		const show = !!(r && (r.love || r.hate)), key = show ? aim.id + ":" + aim.x + ":" + c.gi.uid + c.gi.id : "";
		if (key !== this.reactKey) {
			this.reactKey = key; this.react.clear(); this.react.vis = show;
			if (show) {
				icon(this.react, r.hate ? "intent_debuff" : "heart", 0, 0, { sc: 2 });
				this.react.s = 0.3; tween(this.react, { s: 1 }, 0.25, "outBack");
			}
		}
		if (show) { this.react.x = aim.x; this.react.y = aim.y - aim.h - 30 + Math.sin(this.t * 6) * 2; this.react.top(); }
	}
	updateTips(active) {
		if (!active) return;
		const c = this.hoverC;
		if (c && c.g && !this.sel) {
			const tip = giftTip(c.gi, this, null);
			tip.at = [clamp(c.g.x, 140, W - 140), c.g.y - CH / 2 * c.g.s - 4];
			E.tipOverride = tip; return;
		}
		if (this.sel && this.aim) {
			const tip = giftTip(this.sel.gi, this, this.aim), a = this.aim; tip.at = [W / 2, 200];
			if (a.pipMax > 0 && !a.charmed) {
				const r = this.knownPip(this.sel.gi, this.sel.def, a, true);
				if (a.soft) tip.b.push(a.softHit ? { s: "Hurt this turn: won't join.", c: 0xff9090 } : { s: "Hitting it means it won't join!", c: PAL.pink });
				else if (r.hate) tip.b.push({ s: "HATES this: +1 Grumpy", c: 0xff7070 });
				else if (r.n > 0) tip.b.push({ s: `-${r.n} Grumpy${r.love ? ", LOVES it" : ""}${r.n >= a.pips ? ": softens!" : ""}`, c: PAL.pink });
			}
			E.tipOverride = tip; return;
		}
		if (this.sel && E.touch && E.mouse.down && this.sel.g && Math.hypot(E.mouse.x - this.pressX, E.mouse.y - this.pressY) < 8) {
			this.holdT = (this.holdT || 0) + E.dt;
			if (this.holdT > 0.35) { const tip = giftTip(this.sel.gi, this, null); tip.at = [clamp(this.sel.g.x, 140, W - 140), 200]; E.tipOverride = tip; }
			return;
		}
		this.holdT = 0;
		if (!this.sel && !E.hovered) {
			const u = this.unitAt(E.mouse.x, E.mouse.y);
			if (u) {
				const b = [];
				if (u.charmed) b.push({ s: "Fights on your side.", c: PAL.pink });
				if (!u.isPlayer && u.move) b.push({ s: (u.charmed ? "Next: " : "") + this.intentInfo(u).desc, c: PAL.gold });
				if (!u.isPlayer && !u.charmed) {
					if (u.pipMax > 0) {
						const L = ledger();
						if (u.soft) b.push(u.softHit ? { s: "Hurt this turn: won't join.", c: 0xff9090 } : { s: "Softened! Don't hit it.", c: PAL.pink });
						else b.push({ s: `Grumpy ${u.pips}/${u.pipMax}`, c: 0xd8a0ff });
						const lk = this.likes(u), open = !!u.def.moods;
						b.push({ s: `${open ? "Now loves" : "Loves"} ${open || L.love[u.id] ? likeLabel(lk.love) : "???"}, hates ${open || L.hate[u.id] ? likeLabel(lk.hate) : "???"}`, c: open ? PAL.pink : MUTE() });
					} else b.push({ s: "Can't be won over.", c: 0xff9090 });
				}
				if (u.block) b.push({ s: `Wrap ${u.block}`, c: PAL.ice });
				E.tipOverride = { t: `${u.name}  ${Math.max(0, u.hp)}/${u.maxHp}`, c: u.isPlayer ? PAL.red : PAL.white, b, kw: statusKw(u) };
			}
		}
	}
}