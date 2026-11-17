import { PAL } from "./engine.js";

export const KW = {
	Wrap: { c: PAL.ice, d: "Blocks damage until your next turn." },
	Cheer: { c: PAL.ice, d: "Spent to give gifts. Refills every turn." },
	Might: { c: 0xff6a5a, d: "Attacks deal +1 damage per stack." },
	Nimble: { c: 0x7be08f, d: "Gifts that give Wrap give +1 more per stack." },
	Soggy: { c: 0xe8d9a0, d: "Deals 25% less attack damage, reduced by 1 each turn." },
	Brittle: { c: 0xc08cff, d: "Takes 50% more attack damage, reduced by 1 each turn." },
	Chilled: { c: 0x7fd3ff, d: "Gains 25% less Wrap, reduced by 1 each turn." },
	Frostbite: { c: 0xbff6ff, d: "Loses that much HP at the start of its turn, then drops by 1." },
	Holly: { c: 0x4fc06a, d: "The attacker takes that much damage back." },
	Tinsel: { c: PAL.gold, d: "Gains that much Wrap at the end of each turn." },
	Ribbon: { c: PAL.pink, d: "Negates the next debuff." },
	Carol: { c: PAL.gold, d: "Gains that much Might at the end of each turn." },
	Consume: { c: 0xbbbbbb, d: "Gone for the rest of the battle once given." },
	Melt: { c: 0xbbbbbb, d: "Gone for this battle if it's still in your hand at end of turn." },
	Ungiftable: { c: 0xbbbbbb, d: "Cannot be given." },
	Rewrap: { c: 0xc8ffb0, d: "Rewrapped gifts are stronger and worth +2 Spirit." },
	Spirit: { c: 0x9fffd0, d: "Fades every night. Leave gifts to raise it." },
	Grumpy: { c: 0xc070ff, d: "Gifts a monster loves pop 2 pips. Pop them all to soften it." },
	Softened: { c: PAL.pink, d: "Skips its turn, takes +50% damage. Don't hit it and it joins you." },
};
export function findKeywords(text) {
	const hits = [];
	for (const k in KW) {
		const m = new RegExp("\\b" + k + "(s|ped|ping)?\\b").exec(text);
		if (m) hits.push({ k, i: m.index });
	}
	return hits.sort((a, b) => a.i - b.i).map(h => ({ t: h.k, c: KW[h.k].c, b: KW[h.k].d }));
}

export const ST = {
	might:     { n: "Might", c: 0xff6a5a, d: "Attacks deal +{n} damage." },
	nimble:    { n: "Nimble", c: 0x7be08f, d: "Gains +{n} Wrap from gifts." },
	soggy:     { n: "Soggy", c: 0xe8d9a0, d: "Deals 25% less attack damage for {n} turns.", debuff: 1, turns: 1 },
	brittle:   { n: "Brittle", c: 0xc08cff, d: "Takes 50% more attack damage for {n} turns.", debuff: 1, turns: 1 },
	chilled:   { n: "Chilled", c: 0x7fd3ff, d: "Gains 25% less Wrap for {n} turns.", debuff: 1, turns: 1 },
	frostbite: { n: "Frostbite", c: 0xbff6ff, d: "Loses {n} HP at turn start, then drops by 1.", debuff: 1 },
	holly:     { n: "Holly", c: 0x4fc06a, d: "Attackers take {n} damage back." },
	carol:     { n: "Carol", c: PAL.gold, d: "Gains {n} Might at the end of each turn." },
	cocoa:     { n: "Milk & Cookies", c: 0xd8a070, d: "Heals {n} HP at turn start." },
	ribbon:    { n: "Ribbon", c: PAL.pink, d: "Blocks the next {n} debuffs." },
	tinsel:    { n: "Tinsel", c: PAL.gold, d: "Gains {n} Wrap at the end of each turn." },
	candles:   { n: "Candles", c: PAL.orange, d: "End of turn: {n} damage to ALL enemies." },
	snowfall:  { n: "Snowfall", c: 0xffffff, d: "Turn start: {n} Frostbite to ALL enemies." },
	workshop:  { n: "Workshop", c: 0xd0b080, d: "Draws {n} extra gifts each turn." },
	star:      { n: "Guiding Star", c: PAL.gold, d: "Gains {n} Cheer each turn." },
	choir:     { n: "Choir", c: 0xffffff, d: "Gains {n} Might each turn." },
	jolly:     { n: "Jolly", c: PAL.ice, d: "Gains {n} Cheer next turn." },
	drawnext:  { n: "Eager", c: 0xd0b080, d: "Draws {n} extra gifts next turn." },
};

// v is [base, upgraded] for every number the desc shows
export const GIFTS = {};
function gift(id, name, type, r, cost, tgt, v, desc, play, flags = "", extra = {}) {
	GIFTS[id] = { id, name, type, r, cost: Array.isArray(cost) ? cost : [cost, cost], tgt, v, desc, play, flags, ...extra };
}
const A = "atk", S = "skl", P = "pow", X = "curse";

gift("sock", "Sock Swat", A, 0, 1, "e", { d: [6, 9] }, "Deal {d} damage.", async (C, t, V) => { await C.atk(t, V.d); });
gift("mitten", "Woolly Mitten", S, 0, 1, "n", { b: [5, 8] }, "Gain {b} Wrap.", async (C, t, V) => { C.block(V.b); });
gift("snowball", "Snowball", A, 0, 2, "e", { d: [8, 10], n: [2, 3] }, "Deal {d} damage. Apply {n} Brittle.", async (C, t, V) => { await C.atk(t, V.d); C.apply(t, "brittle", V.n); });
gift("canejab", "Candy Cane Jab", A, 1, 1, "e", { d: [9, 10], n: [1, 2] }, "Deal {d} damage. Draw {n}.", async (C, t, V) => { await C.atk(t, V.d); C.draw(V.n); });
gift("icicle", "Icicle Toss", A, 1, 1, "e", { d: [3, 4], n: [3, 5] }, "Deal {d} damage. Apply {n} Frostbite.", async (C, t, V) => { await C.atk(t, V.d); C.apply(t, "frostbite", V.n); });
gift("baubles", "Bauble Barrage", A, 1, 1, "n", { d: [3, 4] }, "Deal {d} damage to a random enemy 3 times.", async (C, t, V) => { await C.atkRand(V.d, 3); }, "", { hits: 3 });
gift("sledsweep", "Sled Sweep", A, 1, 1, "a", { d: [8, 11] }, "Deal {d} damage to ALL enemies.", async (C, t, V) => { await C.atkAll(V.d); });
gift("nutchomp", "Nutcracker Chomp", A, 1, 2, "e", { d: [12, 15], n: [2, 3] }, "Deal {d} damage. Apply {n} Soggy.", async (C, t, V) => { await C.atk(t, V.d); C.apply(t, "soggy", V.n); });
gift("tinselwhip", "Tinsel Whip", A, 1, 1, "e", { d: [5, 7], b: [5, 7] }, "Gain {b} Wrap. Deal {d} damage.", async (C, t, V) => { C.block(V.b); await C.atk(t, V.d); });
gift("bellslam", "Bell Slam", A, 1, 1, "e", { d: [8, 11] }, "Deal {d} damage. If the enemy is Brittle, gain 1 Cheer and draw 1.", async (C, t, V) => { const br = (t.st.brittle || 0) > 0; await C.atk(t, V.d); if (br) { C.gainEnergy(1); C.draw(1); } });
gift("crumbs", "Cookie Crumbs", A, 1, 0, "e", { d: [3, 5] }, "Deal {d} damage. Draw 1.", async (C, t, V) => { await C.atk(t, V.d); C.draw(1); });
gift("scarf", "Scarf Wrap", S, 1, 1, "n", { b: [8, 11] }, "Gain {b} Wrap.", async (C, t, V) => { C.block(V.b); });
gift("cocoasip", "Cocoa Sip", S, 1, 1, "n", { b: [5, 8] }, "Gain {b} Wrap. Draw 1.", async (C, t, V) => { C.block(V.b); C.draw(1); });
gift("offkey", "Off-Key Carol", S, 1, 1, "n", { n: [2, 3] }, "Apply {n} Soggy to ALL enemies.", async (C, t, V) => { C.applyAll("soggy", V.n); });
gift("wrapspree", "Wrapping Spree", S, 1, 1, "n", { b: [6, 9] }, "Gain {b} Wrap. Next turn, draw 1 extra.", async (C, t, V) => { C.block(V.b); C.apply(C.p, "drawnext", 1); });
gift("stuffer", "Stocking Stuffer", S, 1, 0, "n", { n: [2, 3] }, "Draw {n}. Consume.", async (C, t, V) => { C.draw(V.n); }, "x");
gift("eggshot", "Eggnog Shot", S, 1, 0, "n", { n: [1, 2] }, "Gain {n} Cheer. Consume.", async (C, t, V) => { C.gainEnergy(V.n); }, "x");
gift("snowfort", "Snow Fort", S, 1, 2, "n", { b: [13, 17] }, "Gain {b} Wrap.", async (C, t, V) => { C.block(V.b); });
gift("frostbreath", "Frosty Breath", S, 1, 1, "e", { n: [5, 7] }, "Apply {n} Frostbite.", async (C, t, V) => { C.apply(t, "frostbite", V.n); });
gift("chimney", "Chimney Drop", A, 2, 2, "e", { d: [20, 26] }, "Deal {d} damage.", async (C, t, V) => { await C.atk(t, V.d); });
gift("stampede", "Reindeer Stampede", A, 2, 2, "a", { d: [4, 5] }, "Deal {d} damage to ALL enemies 3 times.", async (C, t, V) => { await C.atkAll(V.d, 3); }, "", { hits: 3 });
gift("kiss", "Mistletoe Kiss", A, 2, 1, "e", { d: [7, 9], n: [4, 6] }, "Deal {d} damage. Heal {n}. Consume.", async (C, t, V) => { await C.atk(t, V.d); C.healP(V.n); }, "x");
gift("bellybump", "Belly Bump", A, 2, [1, 0], "e", {}, "Deal damage equal to your Wrap.", async (C, t) => { await C.atk(t, C.p.block); });
gift("flurry", "Frostbite Flurry", A, 2, 1, "e", { d: [6, 8] }, "Deal {d} damage. Double the enemy's Frostbite.", async (C, t, V) => { await C.atk(t, V.d); if (!t.dead && t.st.frostbite) C.apply(t, "frostbite", t.st.frostbite); });
gift("jingle", "Jingle Jangle", S, 2, 1, "n", { n: [1, 2] }, "Next turn, gain 2 Cheer and draw {n} extra.", async (C, t, V) => { C.apply(C.p, "jolly", 2); C.apply(C.p, "drawnext", V.n); });
gift("doublewrap", "Double Wrap", S, 2, [1, 0], "n", {}, "Double your Wrap.", async (C) => { C.block(C.p.block, true); });
gift("globe", "Snow Globe", S, 2, 1, "n", { b: [7, 10], n: [1, 2] }, "Gain {b} Wrap. Apply {n} Chilled to ALL enemies.", async (C, t, V) => { C.block(V.b); C.applyAll("chilled", V.n); });
gift("fireplace", "Cozy Fireplace", S, 2, 1, "n", { n: [6, 9] }, "Heal {n}. Consume.", async (C, t, V) => { C.healP(V.n); }, "x");
gift("checktwice", "Check It Twice", S, 2, 1, "e", { n: [2, 3] }, "Apply {n} Soggy and {n} Brittle. Consume.", async (C, t, V) => { C.apply(t, "soggy", V.n); C.apply(t, "brittle", V.n); }, "x");
gift("elfhelp", "Elf Helper", S, 2, [1, 0], "n", {}, "Add a random gift to your hand. It costs 0 this turn.", async (C) => { C.addToHand(C.randomGiftId(), { free: true }); });
gift("spiked", "Spiked Eggnog", P, 2, 1, "n", { n: [2, 3] }, "Gain {n} Might.", async (C, t, V) => { C.apply(C.p, "might", V.n); });
gift("tinselarmor", "Tinsel Armor", P, 2, 1, "n", { n: [3, 4] }, "At the end of your turn, gain {n} Wrap.", async (C, t, V) => { C.apply(C.p, "tinsel", V.n); });
gift("hollywreath", "Holly Wreath", P, 2, 1, "n", { n: [3, 4] }, "Gain {n} Holly.", async (C, t, V) => { C.apply(C.p, "holly", V.n); });
gift("candles", "Advent Candles", P, 2, 1, "n", { n: [3, 4] }, "At the end of your turn, deal {n} damage to ALL enemies.", async (C, t, V) => { C.apply(C.p, "candles", V.n); });
gift("snowfall", "Snowfall", P, 2, 1, "n", { n: [2, 3] }, "At the start of your turn, apply {n} Frostbite to ALL enemies.", async (C, t, V) => { C.apply(C.p, "snowfall", V.n); });
gift("rhythm", "Workshop Rhythm", P, 2, [2, 1], "n", {}, "At the start of your turn, draw 1 extra gift.", async (C) => { C.apply(C.p, "workshop", 1); });
gift("midnight", "Midnight Ride", A, 3, 3, "e", { d: [32, 42] }, "Deal {d} damage.", async (C, t, V) => { await C.atk(t, V.d); });
gift("starfall", "Star Fall", A, 3, 2, "a", { d: [10, 14], n: [2, 2] }, "Deal {d} damage to ALL enemies. Apply {n} Brittle to ALL.", async (C, t, V) => { await C.atkAll(V.d); C.applyAll("brittle", V.n); });
gift("birch", "Birch Breaker", A, 3, 1, "e", { d: [12, 16], n: [3, 4] }, "Deal {d} damage. If this kills, gain {n} Max HP. Consume.", async (C, t, V) => { await C.atk(t, V.d); if (t.dead) C.gainMaxHp(V.n); }, "x");
gift("bigbow", "Big Red Bow", S, 3, 2, "n", { b: [30, 40] }, "Gain {b} Wrap. Consume.", async (C, t, V) => { C.block(V.b); }, "x");
gift("miracle", "Xmas Miracle", S, 3, 0, "n", { n: [2, 3] }, "Gain {n} Cheer. Draw 2. Consume.", async (C, t, V) => { C.gainEnergy(V.n); C.draw(2); }, "x");
gift("milkcookies", "Milk & Cookies", P, 3, 2, "n", { n: [2, 3] }, "At the start of your turn, heal {n}.", async (C, t, V) => { C.apply(C.p, "cocoa", V.n); });
gift("guidingstar", "Guiding Star", P, 3, [3, 2], "n", {}, "At the start of your turn, gain 1 Cheer.", async (C) => { C.apply(C.p, "star", 1); });
gift("choir", "Choir of Angels", P, 3, 3, "n", { n: [2, 3] }, "At the start of your turn, gain {n} Might.", async (C, t, V) => { C.apply(C.p, "choir", V.n); });
gift("coal", "Lump of Coal", X, 9, 0, "n", {}, "Ungiftable. Leaving it costs Spirit.", null, "u");
gift("soot", "Soot", X, 9, 0, "n", {}, "Ungiftable. Melt.", null, "um");
gift("naughty", "Naughty Mark", X, 9, 0, "n", {}, "Ungiftable. At the end of your turn, lose 2 HP.", null, "u");

export const TYPE_NAME = { atk: "Attack", skl: "Skill", pow: "Power", curse: "Curse", friend: "Friend" };
export const RAR_NAME = ["Starter", "Common", "Uncommon", "Rare", "Friend"];

export const RELICS = {};
// relic hooks fire once per ornament, twice if it's on the star (fireRelics in run.js)
function relic(id, name, r, desc, hooks = {}) { RELICS[id] = { id, name, r, desc, ...hooks }; }

relic("toolbelt", "Elf Toolbelt", 0, "At the start of each battle, gain 1 Nimble. After each battle, gain 15 Cookies.", { battleStart: C => C.apply(C.p, "nimble", 1), battleEnd: C => C.run.gainCookies(15) });
relic("rednose", "Red Nose", 0, "At the start of each battle, apply 1 Soggy to ALL enemies. Dazzling!", { battleStart: C => C.applyAll("soggy", 1) });
relic("cookietin", "Cookie Tin", 0, "At the end of each battle, heal 6 HP.", { battleEnd: C => C.healP(6) });
relic("jinglebell", "Jingle Bell", 2, "At the start of each battle, draw 2 extra gifts.", { turnStart: C => { if (C.turn === 1) C.draw(2); } });
relic("warmsocks", "Warm Socks", 1, "At the start of each battle, gain 10 Wrap.", { battleStart: C => C.block(10, true) });
relic("candybowl", "Candy Bowl", 1, "Gain 1 extra Cheer on the first turn of each battle.", { turnStart: C => { if (C.turn === 1) C.gainEnergy(1); } });
relic("snowglobe", "Snow Globe", 1, "At the start of each battle, apply 1 Brittle to ALL enemies.", { battleStart: C => C.applyAll("brittle", 1) });
relic("eggnogmug", "Eggnog Mug", 1, "At the start of each battle, gain 1 Might.", { battleStart: C => C.apply(C.p, "might", 1) });
relic("iceskates", "Ice Skates", 1, "At the start of each battle, gain 1 Nimble.", { battleStart: C => C.apply(C.p, "nimble", 1) });
relic("hollysprig", "Holly Sprig", 1, "At the start of each battle, gain 3 Holly.", { battleStart: C => C.apply(C.p, "holly", 3) });
relic("nutjaw", "Nutcracker Jaw", 2, "Every 10th Attack you give deals double damage.", {
	prePlay: (C, R, def) => { if (def.type === "atk") { R.n = (R.n || 0) + 1; if (R.n >= 10) { R.n = 0; C.doubleNext = true; C.relicPing("nutjaw"); } } },
});
relic("tinselgarland", "Tinsel Garland", 2, "If you end your turn with 0 Wrap, gain 6 Wrap.", { turnEnd: C => { if (C.p.block === 0) C.block(6, true); } });
relic("gingercharm", "Gingerbread Charm", 2, "Every 3rd Attack in a turn grants 1 Might.", { play: (C, R, def) => { if (def.type === "atk" && C.atksThisTurn % 3 === 0) C.apply(C.p, "might", 1); } });
relic("lostmitten", "Lost Mitten", 2, "Every 3rd Skill in a turn grants 1 Nimble.", { play: (C, R, def) => { if (def.type === "skl" && C.sklsThisTurn % 3 === 0) C.apply(C.p, "nimble", 1); } });
relic("reindeerbell", "Reindeer Collar", 2, "Every 3rd turn, gain 1 extra Cheer.", { turnStart: C => { if (C.turn % 3 === 0) C.gainEnergy(1); } });
relic("treestar", "Tree-Top Star", 3, "Gain 1 extra Cheer every turn. Spirit fades 1 faster each night.", { energy: 1, decay: 1 });
relic("coalscuttle", "Coal Scuttle", 3, "Leaving coal or curses GAINS 2 Spirit instead of losing it.");
relic("gifttag", "Gift Tag", 1, "Whenever you leave a gift, gain 15 Cookies.", { leave: R => R.gainCookies(15) });
relic("ledger", "Mrs. Claus's Ledger", 2, "Whenever you leave a gift, gain 3 extra Spirit.", { leave: R => R.addSpirit(3) });
relic("workbench", "Workshop Bench", 2, "Whenever you leave a gift, Rewrap (upgrade) a random gift.", { leave: R => R.upgradeRandom() });
relic("sackpatch", "Patched Sack", 3, "With 12 or fewer gifts, you may skip leaving a gift.");
relic("cocoapot", "Cocoa Pot", 1, "Resting by the fire heals 15 extra HP.", { restHeal: n => n + 15 });
relic("elfcoupon", "Elf Coupon", 2, "Everything in the Elf Shop is 20% cheaper.");
relic("pocketwatch", "Santa's Pocket Watch", 3, "If you give 3 or fewer gifts in a turn, draw 3 extra next turn.", { turnEnd: C => { if (C.playedThisTurn <= 3) C.pocketBonus = true; } });
relic("fruitcake", "Fruitcake", 1, "On pickup, gain 10 Max HP.", { pickup: R => R.gainMaxHp(10) });
relic("sugarplum", "Sugarplum Belt", 2, "Gain an extra potion slot.", { pickup: R => { R.G.potionSlots = 4; R.G.potions.length = 4; R.G.potions[3] ??= null; } });
relic("snowshovel", "Snow Shovel", 3, "Up to 15 Wrap is kept between turns.");
relic("churchbell", "Church Bell", 3, "Whenever an enemy is defeated, gain 1 Cheer and draw 1.", { enemyDie: C => { C.gainEnergy(1); C.draw(1); } });
relic("yulelog", "Yule Log", 2, "At the end of your turn, deal 3 damage to a random enemy.", { turnEnd: C => { const f = C.foes(); if (f.length) C.zap(f[Math.floor(Math.random() * f.length)], 3); } });
relic("reindeerfeed", "Reindeer Feed", 1, "Battles drop 25% more Cookies.");
relic("wishlist", "Wish List", 2, "Gift rewards offer 4 choices instead of 3.");
relic("treeangel", "Tree Angel", 3, "Once per run, when you would fall, rise with 50% HP.");

export const POTIONS = {};
// out = the version you can drink on the map, when there is one
function potion(id, name, col, tgt, desc, use, out = null) { POTIONS[id] = { id, name, col, tgt, desc, use, out, any: !!out }; }
potion("hotcocoa", "Hot Cocoa", 0x7a4a2a, "n", "Heal 20 HP.", async C => C.healP(20), R => R.heal(20));
potion("eggnog", "Eggnog", 0xf2e6c0, "n", "Gain 2 Might.", async C => C.apply(C.p, "might", 2));
potion("cider", "Hot Cider", 0xe0902a, "n", "Gain 2 Nimble.", async C => C.apply(C.p, "nimble", 2));
potion("peppermint", "Peppermint Tea", 0xff8080, "n", "Gain 2 Cheer.", async C => C.gainEnergy(2));
potion("snowflask", "Snow Flask", 0xbfe8ff, "n", "Gain 12 Wrap.", async C => C.block(12, true));
potion("cracker", "Christmas Cracker", PAL.red, "e", "Deal 20 damage.", async (C, t) => C.zap(t, 20));
potion("blizzard", "Blizzard in a Jar", 0xe0f4ff, "n", "Deal 10 damage to ALL enemies.", async C => { for (const f of C.foes()) C.zap(f, 10); });
potion("glitter", "Glitter Bomb", PAL.purple, "e", "Apply 3 Brittle.", async (C, t) => C.apply(t, "brittle", 3));
potion("milk", "Milk Dunk", 0xffffff, "n", "Apply 2 Soggy to ALL enemies.", async C => C.applyAll("soggy", 2));
potion("frostvial", "Frostbite Vial", 0x7fd3ff, "e", "Apply 8 Frostbite.", async (C, t) => C.apply(t, "frostbite", 8));
potion("dough", "Cookie Dough", 0xd8b070, "n", "Draw 3 gifts.", async C => C.draw(3));
potion("elfdust", "Elf Dust", PAL.green, "n", "Add a random Rare gift to your hand. It costs 0.", async C => C.addToHand(C.randomGiftId(3), { free: true }));
potion("deermilk", "Reindeer Milk", 0xf8f0e0, "n", "Gain 5 Max HP.", async C => C.gainMaxHp(5), R => R.gainMaxHp(5));
potion("punch", "Holly Punch", 0xc02040, "n", "Gain 3 Holly.", async C => C.apply(C.p, "holly", 3));
potion("starlight", "Bottled Star", PAL.gold, "n", "Gain 2 Ribbon.", async C => C.apply(C.p, "ribbon", 2));
potion("smoke", "Chimney Smoke", 0x606070, "n", "Escape a normal battle (no rewards, no gift to leave).", async C => C.escape());
potion("ginger", "Ginger Shot", 0xd09040, "n", "Rewrap (upgrade) all gifts in your hand this battle.", async C => C.upgradeHand());
potion("wishbone", "Wishbone Broth", 0xe8c890, "n", "Heal 30% of your Max HP.", async C => C.healP(Math.round(C.p.maxHp * 0.3)), R => R.heal(Math.round(R.G.maxHp * 0.3)));
potion("wax", "Candle Wax", 0xfff0d0, "n", "Gain 4 Tinsel this battle.", async C => C.apply(C.p, "tinsel", 4));
potion("elixir", "Spirit Elixir", 0x9fffd0, "n", "Gain 10 Christmas Spirit.", async C => C.run.addSpirit(10), R => R.addSpirit(10));