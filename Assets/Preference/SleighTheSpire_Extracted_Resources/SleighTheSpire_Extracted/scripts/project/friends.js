import { GIFTS, ST } from "./data.js";
import { ENEMIES } from "./enemies.js";

export const FRIEND_MULT = [1.25, 1.6];
// capped, otherwise Jack Frost's friend card is a 7 Frostbite bomb
export const FRIEND_ST = 3;
export const FRIEND_COST = { weak: 1, normal: 1, elite: 2, boss: 2 };
export const ADOPT_JOY = 10;
export const fid = enemyId => "f_" + enemyId;
export const isFriend = id => typeof id === "string" && id.startsWith("f_") && !!GIFTS[id];
export const friendOf = id => ENEMIES[id.slice(2)];

// rough value of a move, only used to decide which one becomes the card
function score(acts) {
	let s = 0;
	for (const a of acts) {
		if (a[0] === "atk") s += a[1] * (a[2] || 1);
		else if (a[0] === "def") s += a[1] * 0.8;
		else if (a[0] === "buff" || a[0] === "buffAll" || a[0] === "debuff") s += Math.min(a[2], 4) * 3;
		else if (a[0] === "heal" || a[0] === "healAll") s += a[1] * 0.6;
		else if (a[0] === "steal" || a[0] === "pocket") s += 2;
	}
	return s;
}
const USE = new Set(["atk", "def", "buff", "buffAll", "debuff", "heal", "healAll", "steal", "pocket"]);
export function bestMove(def) {
	let best = null;
	for (const [key, mv] of Object.entries(def.moves || {})) {
		if (!Array.isArray(mv.a)) continue;
		const acts = mv.a.filter(a => USE.has(a[0]) && (a[0] !== "buff" && a[0] !== "buffAll" && a[0] !== "debuff" || ST[a[1]]));
		if (!acts.length) continue;
		const s = score(acts);
		if (!best || s > best.s) best = { key, name: mv.n, acts, s };
	}
	return best;
}

function makeFriend(def) {
	const mv = bestMove(def); if (!mv) return;
	const v = {}, parts = [], steps = [];
	let n = 0, aims = false;
	const key = k => k && !v[k] ? k : "k" + n++;
	const val = (x, k) => { k = key(k); v[k] = [Math.max(1, Math.round(x * FRIEND_MULT[0])), Math.max(1, Math.round(x * FRIEND_MULT[1]))]; return k; };
	const stv = x => { const k = key(); const b = Math.min(x, FRIEND_ST); v[k] = [b, b + 1]; return k; };
	const raw = x => { const k = key(); v[k] = [x, Math.round(x * 1.5)]; return k; };
	for (const a of mv.acts) {
		const [t] = a;
		if (t === "atk") { const k = val(a[1], "d"), hits = a[2] || 1; parts.push(`Deal {${k}} damage${hits > 1 ? ` x${hits}` : ""}.`); steps.push({ t, k, hits }); aims = true; }
		else if (t === "def") { const k = val(a[1], "b"); parts.push(`Gain {${k}} Wrap.`); steps.push({ t, k }); }
		else if (t === "buff" || t === "buffAll") { const k = stv(a[2]); parts.push(`Gain {${k}} ${ST[a[1]].n}.`); steps.push({ t: "buff", k, st: a[1] }); }
		else if (t === "debuff") { const k = stv(a[2]); parts.push(`Apply {${k}} ${ST[a[1]].n}.`); steps.push({ t, k, st: a[1] }); aims = true; }
		else if (t === "heal" || t === "healAll") { const k = val(a[1]); parts.push(`Heal {${k}} HP.`); steps.push({ t: "heal", k }); }
		else if (t === "steal" || t === "pocket") { const k = raw(Math.min(a[1] || 10, 12)); parts.push(`Gain {${k}} Cookies.`); steps.push({ t: "cookies", k }); }
	}
	const id = fid(def.id);
	GIFTS[id] = {
		id, name: def.name, type: "friend", r: 4, cost: [FRIEND_COST[def.tier] ?? 1, Math.max(0, (FRIEND_COST[def.tier] ?? 1))],
		tgt: aims ? "e" : "n", v, flags: "x", enemy: def.id, art: def.id, move: mv.name,
		desc: `${mv.name}! ${parts.join(" ")} Consume.`,
		play: async (C, t, V) => C.friendHelp(def, steps, V, t),
	};
}
export function buildFriends() { for (const def of Object.values(ENEMIES)) if (!def.noCharm && def.tier !== "boss") makeFriend(def); }
// runs on import, so GIFTS has the friend cards before anyone reads it
buildFriends();

