import { PAL, store } from "./engine.js";

export const PARTS = {
	wheels:   { id: "wheels",   name: "Wheels",      r: 1, col: 0xc0c8d8, desc: "Costs 1 less Cheer." },
	paint:    { id: "paint",    name: "Fresh Paint", r: 1, col: PAL.red,  desc: "+3 damage or Wrap." },
	squeaker: { id: "squeaker", name: "Squeaker",    r: 1, col: PAL.gold, desc: "Apply 1 Soggy." },
	glitter:  { id: "glitter",  name: "Glitter",     r: 1, col: 0xd8a0ff, desc: "Apply 1 Brittle." },
	bell:     { id: "bell",     name: "Little Bell", r: 2, col: PAL.gold, desc: "Draw 1 gift." },
	spring:   { id: "spring",   name: "Spring",      r: 2, col: 0xb8c0d0, desc: "Deal 4 damage to a random enemy." },
	musicbox: { id: "musicbox", name: "Music Box",   r: 2, col: 0xd8a070, desc: "Heal 2 HP." },
	windup:   { id: "windup",   name: "Wind-up Key", r: 2, col: 0xc8a040, desc: "Next turn, gain 1 Cheer." },
	ribbon:   { id: "ribbon",   name: "Big Ribbon",  r: 2, col: PAL.pink, desc: "Pops 1 Grumpy pip on any monster." },
	battery:  { id: "battery",  name: "Battery",     r: 3, col: 0x60d080, desc: "Doubles this gift's power. Consume." },
};
export const partIds = r => Object.keys(PARTS).filter(k => r == null || PARTS[k].r === r);
export const partPrice = id => [0, 45, 65, 90][PARTS[id].r];

export const KIDS = ["Mila", "Oskar", "Ada", "Tommy", "Lea", "Rasmus", "Grete", "Kaspar", "Liis", "Theo", "Nora", "Jakob", "Elsa", "Finn", "Maarja", "Leo", "Ida", "Hugo", "Anni", "Sam", "Rosa", "Eli", "Pip", "Juno", "Kai"];
export const kidOf = night => KIDS[night % KIDS.length];
export const WISH_BONUS = 4;
// test(gi, d, V): the gift, its definition, its final values
export const WISHES = [
	{ id: "wheels", text: "I want something with wheels!", test: gi => gi.part === "wheels" },
	{ id: "squeak", text: "I want something that squeaks!", test: gi => gi.part === "squeaker" },
	{ id: "music", text: "I want something that plays music!", test: gi => gi.part === "musicbox" },
	{ id: "jingle", text: "I want something that jingles!", test: gi => gi.part === "bell" },
	{ id: "bouncy", text: "I want something bouncy!", test: gi => gi.part === "spring" },
	{ id: "windup", text: "I want a wind-up toy!", test: gi => gi.part === "windup" },
	{ id: "bow", text: "I want a present with a big bow!", test: gi => gi.part === "ribbon" },
	{ id: "sparkly", text: "I want something sparkly!", test: gi => gi.part === "glitter" || gi.part === "paint" },
	{ id: "warm", text: "I want something warm and cosy!", test: (gi, d, V) => V.b !== undefined },
	{ id: "throw", text: "I want something I can throw!", test: (gi, d) => d.type === "atk" },
	{ id: "shiny", text: "I want something rare and shiny!", test: (gi, d) => d.r === 3 || gi.up },
	{ id: "surprise", text: "I want a big surprise!", test: (gi, d) => d.type === "pow" },
];

export function likeMatches(like, gi, d) {
	if (!like) return false;
	if (like === "rare") return d.r === 3;
	const [k, v] = like.split(":");
	if (k === "part") return gi.part === v;
	if (k === "type") return d.type === v;
	return false;
}
export function likeLabel(like) {
	if (!like) return "nothing in particular";
	if (like === "rare") return "Rare gifts";
	const [k, v] = like.split(":");
	if (k === "part") return PARTS[v]?.name ?? v;
	return { atk: "Attack gifts", skl: "Skill gifts", pow: "Power gifts" }[v] ?? v;
}
// pips by tier. elites and the boss are meant to take the whole fight
export const PIPS = { weak: 2, normal: 3, elite: 5, boss: 8 };
export const LOVE_POP = 2, RIBBON_POP = 1, HATE_ADD = 1, SOFT_DMG = 1.5;
export const JEALOUSY = true;
export const pipsMax = def => def.noCharm ? 0 : def.pips ?? PIPS[def.tier] ?? 3;
export function pipHit(gi, d, def, ribbon = true) {
	if (!d || d.type === "curse") return { n: 0, love: false, hate: false, rib: false };
	const love = likeMatches(def.love, gi, d), hate = !love && likeMatches(def.hate, gi, d), rib = ribbon && gi.part === "ribbon";
	return { n: (love ? LOVE_POP : 0) + (rib ? RIBBON_POP : 0) - (hate ? HATE_ADD : 0), love, hate, rib };
}

let L = null;
// loads from localStorage once, after that it's just an object we poke
export function ledger() {
	if (!L) { L = store.get("ledger", null) || {}; for (const k of ["seen", "love", "hate", "kills", "charms"]) L[k] ||= {}; }
	return L;
}
export function ledgerNote(kind, id) {
	const l = ledger();
	if (kind === "kills" || kind === "charms") l[kind][id] = (l[kind][id] || 0) + 1;
	else { if (l[kind][id]) return; l[kind][id] = 1; }
	store.set("ledger", l);
}