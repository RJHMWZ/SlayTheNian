import { E, go, floatText, PAL, store } from "./engine.js";
import { sfx } from "./audio.js";
import { GIFTS, RELICS, POTIONS } from "./data.js";
import { ENC } from "./enemies.js";
import { EVENTS } from "./events.js";
import { Combat } from "./combat.js";
import { MapScene, EventScene, ShopScene, RestScene, TreasureScene, VictoryScene } from "./scenes.js";
import { setChar, curChar } from "./characters.js";
import { PARTS, WISHES, WISH_BONUS } from "./parts.js";
import { fid, ADOPT_JOY } from "./friends.js";

export const G = { active: false };
export const NIGHTS = 24;
export const HUDPOS = { hp: [44, 26], cookies: [124, 26], spirit: [300, 26], potions: [190, 26] };

let _rng = Math.random;
// seeded, so a run can be replayed from G.seed
function mulberry32(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
export const rng = () => _rng();
export const ri = (a, b) => a + Math.floor(rng() * (b - a + 1));
export const pick = a => a[Math.floor(rng() * a.length)];
export function shuf(a) { for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(rng() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; }

let UID = 1;
export const giftDef = gi => GIFTS[gi.id];
export const giftName = gi => GIFTS[gi.id].name + (gi.up ? "+" : "");
export function giftVals(gi) {
	const d = GIFTS[gi.id], V = {};
	for (const k in d.v) V[k] = d.v[k][gi.up ? 1 : 0];
	if (gi.part === "paint") { if (V.d !== undefined) V.d += 3; else if (V.b !== undefined) V.b += 3; }
	if (gi.part === "battery") for (const k in V) V[k] *= 2;
	return V;
}
export const giftCost = gi => Math.max(0, GIFTS[gi.id].cost[gi.up ? 1 : 0] - (gi.part === "wheels" ? 1 : 0));
export const canPart = gi => !gi.part && GIFTS[gi.id].type !== "curse";
export function attachPart(gi, id) { if (!canPart(gi) || !PARTS[id]) return false; gi.part = id; G.changed = true; return true; }
export const canUpgrade = gi => !gi.up && GIFTS[gi.id].type !== "curse";
export function joyOf(gi) {
	const d = GIFTS[gi.id];
	if (d.type === "curse") return hasRelic("coalscuttle") ? 2 : -5;
	if (d.type === "friend") return ADOPT_JOY + (gi.up ? 2 : 0) + (gi.part ? PARTS[gi.part].r : 0) + (gi.part === "ribbon" ? 3 : 0);
	return [2, 3, 5, 8][d.r] + (gi.up ? 2 : 0) + (gi.part ? PARTS[gi.part].r : 0) + (gi.part === "ribbon" ? 3 : 0);
}
export function addFriend(enemyId) {
	const id = fid(enemyId), have = G.bag.find(gi => gi.id === id);
	if (!have) { addGift(id); return "new"; }
	if (!have.up) { have.up = true; G.changed = true; return "up"; }
	addSpirit(2); return "max";
}
export function addGift(id, up = false, part = null) { const gi = { id, up, uid: UID++, part }; G.bag.push(gi); G.changed = true; return gi; }
export function removeGift(gi) { const i = G.bag.indexOf(gi); if (i >= 0) G.bag.splice(i, 1); G.changed = true; }
export function upgradeGift(gi) { if (canUpgrade(gi)) { gi.up = true; G.changed = true; return true; } return false; }
export function upgradeRandom(n = 1) {
	let c = 0;
	for (let i = 0; i < n; i++) { const pool = G.bag.filter(canUpgrade); if (!pool.length) break; upgradeGift(pick(pool)); c++; }
	return c;
}

export const hasRelic = id => !!G.relics?.some(r => r.id === id);
export const relicInst = id => G.relics?.find(r => r.id === id) || null;
export function addRelic(id, silent = false) {
	if (!id || hasRelic(id)) return null;
	const inst = { id, n: 0, used: false };
	G.relics.push(inst);
	RELICS[id].pickup?.(RUN, inst);
	G.changed = true;
	if (!silent) sfx.relic();
	return inst;
}
export function fireRelics(hook, ctx, ...args) {
	for (const r of G.relics) {
		const d = RELICS[r.id]; if (!d[hook]) continue;
		d[hook](ctx, r, ...args);
	}
}
export function relicSum(key) { let s = 0; for (const r of G.relics) s += RELICS[r.id][key] || 0; return s; }

export function addPotion(id) {
	for (let i = 0; i < G.potionSlots; i++) if (!G.potions[i]) { G.potions[i] = id; G.changed = true; sfx.potion(); return true; }
	return false;
}
export const potionFull = () => G.potions.slice(0, G.potionSlots).every(p => p);

export function heal(n) { const b = G.hp; G.hp = Math.min(G.maxHp, G.hp + n); const d = G.hp - b; if (d > 0) { floatText(...HUDPOS.hp, "+" + d, PAL.green); sfx.heal(); } G.changed = true; return d; }
export function hurt(n) { G.hp = Math.max(1, G.hp - n); floatText(...HUDPOS.hp, "-" + n, PAL.red); sfx.hit(n); G.changed = true; }
export function gainMaxHp(n) { G.maxHp = Math.max(1, G.maxHp + n); G.hp = Math.max(1, Math.min(G.maxHp, G.hp + Math.max(0, n))); floatText(...HUDPOS.hp, (n > 0 ? "+" : "") + n + " MAX", n > 0 ? PAL.green : PAL.red); G.changed = true; }
export function gainCookies(n) { G.cookies += n; floatText(...HUDPOS.cookies, "+" + n, PAL.gold); sfx.coin(); G.changed = true; }
export function spendCookies(n) { if (G.cookies < n) return false; G.cookies -= n; floatText(...HUDPOS.cookies, "-" + n, PAL.gold); G.changed = true; return true; }
const tierIdx = v => TIERS.reduce((a, t, i) => v >= t.min ? i : a, 0);
export function addSpirit(n) {
	const b = G.spirit; G.spirit = Math.max(0, Math.min(100, G.spirit + n)); const d = G.spirit - b;
	if (tierIdx(G.spirit) < tierIdx(b)) E.spiritDrop = TIERS[tierIdx(G.spirit)];
	if (d) { floatText(...HUDPOS.spirit, (d > 0 ? "+" : "") + d + " SPIRIT", d > 0 ? 0x9fffd0 : PAL.red); if (d > 0) sfx.spirit(); else sfx.debuff(); }
	G.changed = true; return d;
}
export const TIERS = [
	{ id: "grim", min: 0, name: "Grim", col: 0x8a5a6a, desc: "Draw 1 fewer gift every turn.", short: "draw 1 fewer" },
	{ id: "fading", min: 25, name: "Fading", col: 0x7fa0d0, desc: "Krampus grows stronger.", short: "Krampus grows stronger" },
	{ id: "merry", min: 50, name: "Merry", col: 0x7be08f, desc: "Kids still believe in Christmas.", short: "kids still believe" },
	{ id: "festive", min: 70, name: "Festive", col: 0x9fffd0, desc: "+1 Cheer on the first turn of each battle.", short: "+1 Cheer on turn 1" },
	{ id: "jolly", min: 90, name: "Jolly", col: PAL.gold, desc: "+1 Cheer every turn.", short: "+1 Cheer every turn" },
];
export const nextTier = () => TIERS.find(t => t.min > G.spirit) || null;
export const spiritTier = () => [...TIERS].reverse().find(t => G.spirit >= t.min);
export const nightsLeft = i => NIGHTS - i;
export const dateLabel = i => "DEC " + (i + 1);

export function rollRarity(elite) { const r = rng(); return elite ? (r < 0.15 ? 3 : r < 0.5 ? 2 : 1) : (r < 0.07 ? 3 : r < 0.38 ? 2 : 1); }
export function randGiftId(rar) { const pool = Object.values(GIFTS).filter(g => g.r === (rar ?? rollRarity(false))); return pick(pool).id; }
export function giftChoices(elite, n = 3) {
	const out = [];
	let guard = 0;
	while (out.length < n && guard++ < 100) { const id = randGiftId(rollRarity(elite)); if (!out.includes(id)) out.push(id); }
	return out;
}
export function randRelicId(rar) {
	const owned = new Set(G.relics.map(r => r.id));
	let pool = Object.values(RELICS).filter(r => r.r > 0 && !owned.has(r.id));
	if (!pool.length) return null;
	const want = rar ?? (rng() < 0.5 ? 1 : rng() < 0.67 ? 2 : 3);
	const p2 = pool.filter(r => r.r === want);
	return pick(p2.length ? p2 : pool).id;
}
export const randPotionId = () => pick(Object.keys(POTIONS));

export const RUN = {
	get G() { return G; },
	heal, hurt, gainMaxHp, gainCookies, spendCookies, addSpirit, upgradeRandom, hasRelic,
	addGift: (id, up = false) => addGift(id, up), randGiftId, giftName: id => GIFTS[id].name,
	addRelic: id => addRelic(id), randRelicId, relicName: id => id ? RELICS[id].name : "nothing",
	addPotion: id => addPotion(id), randPotionId, potionName: id => POTIONS[id].name,
	losePotion() { const i = G.potions.findIndex(p => p); if (i >= 0) { G.potions[i] = null; G.changed = true; } },
	chance: p => rng() < p, ri,
};

const TEMPLATE = [
	["battle"], ["battle", "event"], ["event", "battle"], ["battle"],
	["elite", "event"], ["rest"], ["battle", "event"], ["treasure"], ["battle", "event"],
	["shop"], ["elite", "battle"], ["event", "battle"], ["battle"], ["rest", "shop"],
	["elite"], ["battle", "event"], ["battle", "treasure"], ["event", "battle"], ["elite", "battle"],
	["battle", "event"], ["shop", "event"], ["elite", "battle"], ["rest"], ["boss"],
];
export function genMap() { return TEMPLATE.map((opts, i) => ({ i, opts: shuf([...opts]), chosen: null, done: false })); }
export const NODE_INFO = {
	battle: { n: "Battle", d: "Monsters lurk in the snow.", col: 0xc0c8e0 },
	elite: { n: "Elite", d: "A powerful foe guards a relic.", col: PAL.red },
	event: { n: "Event", d: "Something strange awaits you.", col: PAL.gold },
	shop: { n: "Elf Shop", d: "Spend Cookies on gifts, relics and potions.", col: PAL.green },
	rest: { n: "Fireside", d: "Rest, upgrade a gift or write to Santa.", col: PAL.orange },
	treasure: { n: "Stocking", d: "A stocking stuffed with a relic.", col: PAL.pink },
	boss: { n: "Christmas Eve", d: "Save Santa's soul.", col: 0xff3030 },
};

// each Advent level keeps every rule below it
export const ADVENT = [
	"Elites have 10% more HP.",
	"All monsters have 10% more HP.",
	"Start with a Lump of Coal.",
	"Start with 40 Spirit instead of 50.",
	"Resting by the fire heals less.",
	"Monsters have 1 extra Grumpy pip.",
	"The Elf Shop costs 15% more.",
	"Start with 5 less Max HP.",
	"Monsters hit 10% harder.",
	"Santa and Krampus are tougher.",
];
export const ADVENT_MAX = ADVENT.length;
export const adv = n => (G.advent || 0) >= n;
export const adventUnlocked = () => Math.min(ADVENT_MAX, store.get("advent", 0));
export function unlockNextAdvent() {
	const next = Math.min(ADVENT_MAX, (G.advent || 0) + 1);
	if (next > adventUnlocked()) { store.set("advent", next); return next; }
	return 0;
}

export function newRun(seed = (Date.now() % 1e9) | 0, charId = "mrs", advent = 0) {
	_rng = mulberry32(seed);
	setChar(charId);
	const ch = curChar();
	Object.assign(G, {
		active: true, seed, advent, char: ch.id, hp: ch.hp, maxHp: ch.hp, cookies: 99, spirit: 50, bag: [], relics: [],
		charmed: [], secret: false, wish: null, wishes: {},
		potions: [null, null, null], potionSlots: 3, night: 0, map: genMap(), lights: {},
		usedEvents: [], usedEnc: [],
		leaveCookieUses: 0, potionChance: 0.4, doubleJoy: false, changed: true,
		stats: { kills: 0, left: 0, joy: 0, elites: 0, cookiesLeft: 0, charmed: 0, wishes: 0 },
	});
	for (let i = 0; i < 4; i++) addGift("sock");
	addGift("sock", false, "ribbon");
	for (let i = 0; i < 4; i++) addGift("mitten");
	addGift("snowball");
	if (adv(3)) addGift("coal");
	if (adv(4)) G.spirit = 40;
	if (adv(8)) { G.maxHp -= 5; G.hp = G.maxHp; }
	addRelic(ch.relic, true);
}

function pickEnc(tier) {
	const pool = ENC[tier].filter(e => !G.usedEnc.includes(e.join()));
	const e = pick(pool.length ? pool : ENC[tier]);
	G.usedEnc.push(e.join());
	return e;
}
function pickEvent() {
	const pool = EVENTS.filter(e => !G.usedEvents.includes(e.id));
	const e = pick(pool.length ? pool : EVENTS);
	G.usedEvents.push(e.id);
	return e;
}

export function enterNode(i, type) {
	const n = G.map[i];
	n.chosen = type;
	if (i > 0) addSpirit(-(1 + relicSum("decay")));
	G.changed = true;
	switch (type) {
		case "battle": startFight(pickEnc(i < 3 ? "weak" : "normal"), "normal"); break;
		case "elite": startFight(pickEnc("elite"), "elite"); break;
		case "boss": startFight(["santa"], "boss"); break;
		case "event": go(() => new EventScene(pickEvent())); break;
		case "shop": go(() => new ShopScene()); break;
		case "rest": go(() => new RestScene()); break;
		case "treasure": go(() => new TreasureScene()); break;
	}
}
export function startFight(ids, kind, opts = {}) { go(() => new Combat(ids, kind, opts)); }
export function finishNode() {
	if (G.map[G.night]) G.map[G.night].done = true;
	G.wish = null;
	G.night++;
	G.changed = true;
	if (G.night >= NIGHTS) go(() => new VictoryScene());
	else go(() => new MapScene());
}

export const LEAVE_MIN = 5;
export const leaveCookieCost = () => 30 + 15 * G.leaveCookieUses;
export function wishFor(night) {
	G.wishes ||= {};
	if (!G.wishes[night]) {
		const can = WISHES.filter(w => G.bag.some(gi => wishOk(w, gi)));
		G.wishes[night] = (can.length && rng() < 0.7 ? pick(can) : pick(WISHES)).id;
	}
	return WISHES.find(w => w.id === G.wishes[night]);
}
export function pickWish() { const w = wishFor(G.night); G.wish = w.id; return w; }
export const curWish = () => WISHES.find(w => w.id === G.wish) || null;
export function wishOk(w, gi) { if (!w) return false; const d = GIFTS[gi.id]; return d.type !== "curse" && !!w.test(gi, d, giftVals(gi)); }
// the wish bonus goes in before doubleJoy, so the naughty kid doubles it too
export function leaveGift(gi) {
	let joy = joyOf(gi);
	if (wishOk(curWish(), gi)) { joy += WISH_BONUS; G.stats.wishes++; }
	if (G.doubleJoy && joy > 0) joy *= 2;
	G.doubleJoy = false;
	removeGift(gi);
	G.lights[G.night] = joy > 0;
	G.stats.left++;
	if (joy > 0) G.stats.joy += joy;
	fireRelics("leave", RUN, gi);
	addSpirit(joy);
	return joy;
}
export function leaveCookies() {
	const c = leaveCookieCost();
	if (!spendCookies(c)) return false;
	G.leaveCookieUses++; G.stats.cookiesLeft += c;
	G.lights[G.night] = true;
	addSpirit(1);
	return true;
}
export const canSkipLeave = () => hasRelic("sackpatch") && G.bag.length <= 12;