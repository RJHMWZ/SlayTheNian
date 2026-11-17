const rand = Math.random;
export const seq = (...keys) => e => keys[e.turn % keys.length];
// weighted pick that won't do the same move three times in a row
export const rnd = w => e => {
	const h = e.hist, ban = h.length >= 2 && h[h.length - 1] === h[h.length - 2] ? h[h.length - 1] : null;
	const ent = Object.entries(w).filter(([k]) => k !== ban);
	let tot = ent.reduce((s, [, v]) => s + v, 0), r = rand() * tot;
	for (const [k, v] of ent) { r -= v; if (r <= 0) return k; }
	return ent[0][0];
};
const first = (k, then) => (e, C) => e.turn === 0 ? k : then(e, C);

export const ENEMIES = {};
function enemy(id, o) { ENEMIES[id] = { id, ...o }; }

enemy("gingerling", {
	name: "Gingerbread Man", tier: "weak", hp: [16, 20],
	lines: ["It's gingerDREAD man now!", "Who are ya callin' half-baked?!"],
	moves: { bite: { n: "Crumbly Bite", a: [["atk", 6]] }, rush: { n: "Sugar Rush", a: [["def", 4], ["buff", "might", 1]] } },
	ai: rnd({ bite: 3, rush: 1 }),
});
enemy("snowman", {
	name: "Snowman", tier: "weak", hp: [22, 26],
	lines: ["Imma stick you with my carrot!", "I'll keep you frozen till New Year!"],
	moves: { ball: { n: "Iceball", a: [["atk", 7]] }, glare: { n: "Frost Glare", a: [["debuff", "chilled", 2], ["def", 6]] } },
	ai: e => (e.turn + e.seed) % 2 ? "glare" : "ball",
});
enemy("ornament", {
	name: "Bad Bauble", tier: "weak", hp: [15, 18], fly: true,
	lines: ["I'll shatter your face!", "Tink tink! You'll break before I do!"],
	moves: { shards: { n: "Shards", a: [["atk", 3, 2]] }, glint: { n: "Glint", a: [["debuff", "soggy", 1]] } },
	ai: rnd({ shards: 2, glint: 1 }),
});
enemy("rat", {
	name: "Chimney Rat", tier: "weak", hp: [16, 20],
	lines: ["Squeak!", "Stay away from my chimney!"],
	moves: { gnaw: { n: "Gnaw", a: [["atk", 5]] }, squeak: { n: "Squeak", a: [["buff", "might", 2]] } },
	ai: rnd({ gnaw: 3, squeak: 1 }),
});
enemy("lights", {
	name: "Tangled Lights", tier: "weak", hp: [20, 25],
	lines: ["Bzzt!", "Night night, {nick}!"],
	moves: { zap: { n: "Zap", a: [["atk", 4], ["debuff", "soggy", 1]] }, short: { n: "Short Circuit", a: [["atk", 9]] }, tangle: { n: "Tangle", a: [["debuff", "chilled", 2]] } },
	ai: rnd({ zap: 2, short: 2, tangle: 1 }),
});
enemy("penguin", {
	name: "Wind-up Penguin", tier: "weak", hp: [18, 23],
	lines: ["Click-click-click... MAXIMUM WADDLE.", "I only know how to move forward."],
	moves: { waddle: { n: "Waddle Smack", a: [["atk", 6]] }, wind: { n: "Wind Up", a: [["buff", "might", 3]] } },
	ai: seq("waddle", "waddle", "wind"),
});

enemy("nutgrunt", {
	name: "Nutcracker", tier: "normal", hp: [42, 48],
	lines: ["Nuts and grannies get cracked alike!", "CRACK. CRACK. CRACK."],
	moves: { crack: { n: "Crack", a: [["atk", 11]] }, guard: { n: "Stand Guard", a: [["def", 9], ["atk", 4]] }, grind: { n: "Grind", a: [["atk", 5, 2]] } },
	ai: rnd({ crack: 2, guard: 1, grind: 2 }),
});
enemy("toysoldier", {
	name: "Tin Soldier", tier: "normal", hp: [28, 33],
	lines: ["Left, right, left, right... CHARGE!", "Make war, not love!"],
	moves: { march: { n: "Bayonet", a: [["atk", 7]] }, volley: { n: "Volley", a: [["atk", 3, 3]] }, drill: { n: "Drill", a: [["buffAll", "might", 1], ["def", 4]] } },
	ai: rnd({ march: 2, volley: 2, drill: 1 }),
});
enemy("jack", {
	name: "Jack-in-the-Box", tier: "normal", hp: [32, 36],
	lines: ["Pop goes the... GRANNY!", "Round and round the handle turns..."],
	moves: { wind: { n: "Winding...", a: [["def", 8], ["buff", "might", 1]] }, pop: { n: "POP!", a: [["atk", 16]] } },
	ai: seq("wind", "pop"),
});
enemy("fruitcake", {
	name: "Fruity Snake", tier: "normal", hp: [46, 52],
	lines: ["I'm a fruity cutie patootie!", "Feelin' thirsty? How about a PUNCH?"],
	moves: { slam: { n: "Slam", a: [["atk", 13]] }, stale: { n: "Stale Crumbs", a: [["add", "soot", 2, "discard"]] }, harden: { n: "Harden", a: [["def", 12]] } },
	ai: first("slam", rnd({ slam: 2, stale: 1, harden: 1 })),
});
enemy("angel", {
	name: "Broken Bauble", tier: "normal", hp: [34, 38], fly: true,
	lines: ["I'll shard all over you!", "Silent night, violent night."],
	moves: { gust: { n: "Wing Gust", a: [["atk", 6, 2]] }, hymn: { n: "Cold Hymn", a: [["healAll", 8]] }, lament: { n: "Lament", a: [["debuff", "soggy", 2], ["debuff", "brittle", 1]] } },
	ai: (e, C) => C.foes().some(f => f.hp < f.maxHp - 8) && e.hist.at(-1) !== "hymn" && rand() < 0.4 ? "hymn" : rnd({ gust: 2, lament: 1 })(e, C),
});
enemy("slime", {
	name: "Eggnog Slime", tier: "normal", hp: [36, 40],
	lines: ["Ya callin' me spoiled?!", "I'm gloopy and goopy."],
	moves: { splash: { n: "Splash", a: [["atk", 9], ["debuff", "soggy", 1]] }, gloop: { n: "Gloop", a: [["debuff", "chilled", 2], ["def", 5]] } },
	ai: rnd({ splash: 2, gloop: 1 }),
});
enemy("bat", {
	name: "Icicle Bat", tier: "normal", hp: [16, 20], fly: true,
	lines: ["Skreee!", "I knew I should've stayed in Mt. Moon."],
	moves: { dive: { n: "Dive", a: [["atk", 6]] }, screech: { n: "Screech", a: [["debuff", "soggy", 1]] } },
	ai: rnd({ dive: 3, screech: 1 }),
});
enemy("grumpkin", {
	name: "Grumpkin", tier: "normal", hp: [38, 42],
	lines: ["You wouldn't like me when I'm grumpy.", "Your cookies. Hand them over."],
	moves: { swipe: { n: "Swipe", a: [["atk", 8], ["steal", 15]] }, sulk: { n: "Sulk", a: [["def", 12]] }, grumble: { n: "Grumble", a: [["debuff", "soggy", 2]] } },
	ai: rnd({ swipe: 3, sulk: 1, grumble: 1 }),
});
enemy("rogueelf", {
	name: "Rogue Elf", tier: "normal", hp: [24, 28],
	lines: ["Your cookies or your life! ...Actually, just the cookies.", "Santa never paid us overtime."],
	moves: {
		pickpocket: { n: "Pickpocket", a: [["atk", 7], ["pocket", 25]] },
		lurk: { n: "Lie Low", a: [["def", 10]] },
		flee: { n: "Escape", a: [["flee"]] },
	},
	ai: e => { const last = e.hist.at(-1); if (e.loot > 0 && last === "pickpocket") return "lurk"; if (e.loot > 0 && last === "lurk") return "flee"; return "pickpocket"; },
});
enemy("coalgolem", {
	name: "Coal Golem", tier: "normal", hp: [48, 54],
	lines: ["Every naughty child... made me BIGGER.", "COAL FOR EVERYONE."],
	moves: { fist: { n: "Coal Fist", a: [["atk", 14]] }, smog: { n: "Smog", a: [["add", "soot", 1, "draw"], ["def", 10]] } },
	ai: rnd({ fist: 2, smog: 1 }),
});
enemy("viper", {
	name: "Candy Cane Viper", tier: "normal", hp: [26, 30],
	lines: ["Sssso sweet... sssso deadly.", "Refresh yourssself with sssome peppermint venom!"],
	moves: { fangs: { n: "Fangs", a: [["atk", 6], ["debuff", "frostbite", 3]] }, coil: { n: "Coil", a: [["def", 7], ["buff", "might", 1]] } },
	ai: rnd({ fangs: 2, coil: 1 }),
});
enemy("creeper", {
	name: "Mistletoe Creeper", tier: "normal", hp: [40, 44],
	lines: ["Kiss kiss bang bang.", "A kiss? How about a SQUEEZE."],
	moves: { lash: { n: "Vine Lash", a: [["atk", 10]] }, entangle: { n: "Entangle", a: [["debuff", "chilled", 2], ["debuff", "soggy", 1]] }, grow: { n: "Grow", a: [["buff", "holly", 3], ["def", 6]] } },
	ai: first("entangle", rnd({ lash: 3, grow: 1, entangle: 1 })),
});
enemy("krampimp", {
	name: "Krampus Imp", tier: "normal", hp: [24, 28],
	lines: ["The Master is coming! Heh heh!", "Naughty, naughty, NAUGHTY!"],
	moves: { switch: { n: "Switch", a: [["atk", 4, 2]] }, snicker: { n: "Snicker", a: [["buff", "might", 1], ["def", 4]] } },
	ai: rnd({ switch: 3, snicker: 1 }),
});

enemy("general", {
	name: "Nutcracker General", tier: "elite", hp: [82, 86],
	lines: ["Crush the intruder!", "You're just another nut to crush."],
	moves: { command: { n: "Command", a: [["buff", "might", 2], ["def", 10]] }, saber: { n: "Saber Slash", a: [["atk", 18]] }, jaw: { n: "Iron Jaw", a: [["atk", 8, 2], ["debuff", "brittle", 2]] } },
	ai: seq("command", "saber", "jaw"),
});
enemy("yeti", {
	name: "Abominable Snowman", tier: "elite", hp: [92, 96],
	lines: ["RRRAAAWR!", "GRRRR!"],
	moves: { roar: { n: "Roar", a: [["buff", "might", 2], ["debuff", "soggy", 2]] }, maul: { n: "Maul", a: [["atk", 8, 2]] }, avalanche: { n: "Avalanche", a: [["atk", 22]] } },
	ai: first("roar", e => ["maul", "avalanche", "maul", "roar"][(e.turn - 1) % 4]),
});
enemy("coalbaron", {
	name: "Coal Baron", tier: "elite", hp: [76, 80],
	lines: ["Coal is the gift that keeps on giving.", "GRAAAAWR!"],
	moves: { shower: { n: "Coal Shower", a: [["atk", 6, 3]] }, tax: { n: "Coal Tax", a: [["add", "soot", 3, "draw"], ["steal", 20]] }, pile: { n: "Stockpile", a: [["def", 15], ["buff", "tinsel", 3]] } },
	ai: seq("pile", "shower", "tax", "shower"),
});
enemy("alpha", {
	name: "Reindeer Alpha", tier: "elite", hp: [80, 85],
	lines: ["The sleigh is broken. And so are the reins.", "We pulled for HIM. Now we run wild!"],
	start: e => { e.st.ribbon = 2; },
	moves: { bellow: { n: "Bellow", a: [["buff", "might", 3]] }, gore: { n: "Gore", a: [["atk", 15]] }, stampede: { n: "Stampede", a: [["atk", 5, 3]] } },
	ai: first("bellow", seq("gore", "stampede", "gore", "bellow")),
});
enemy("witch", {
	name: "Gingerbread Witch", tier: "elite", hp: [84, 88],
	lines: ["You son of a witch!", "Mmm. Granny on gingerbread."],
	moves: { hex: { n: "Hex", a: [["atk", 12], ["debuff", "chilled", 1]] }, bake: { n: "Bake a Minion", a: [["summon", "gingerling"], ["def", 6]] }, wall: { n: "Candy Wall", a: [["def", 14]] } },
	ai: (e, C) => (e.turn % 3 === 2 && C.foes().length < 3) ? "bake" : rnd({ hex: 2, wall: 1 })(e, C),
});
enemy("jackfrost", {
	name: "Jack Frost", tier: "elite", hp: [76, 80],
	lines: ["I'll frostbite your face off!", "Christmas is cancelled!"],
	start: e => { e.st.holly = 2; },
	moves: { freeze: { n: "Freeze Over", a: [["debuff", "frostbite", 7], ["debuff", "chilled", 2]] }, nip: { n: "Icy Nips", a: [["atk", 4, 3]] }, wall: { n: "Ice Wall", a: [["def", 16]] } },
	ai: seq("freeze", "nip", "wall", "nip"),
});

enemy("santa", {
	name: "Corrupted Santa", tier: "boss", hp: [128, 128],
	lines: ["What a ho-ho-horrible night to have a curse!"],
	moves: {
		ho: { n: "Ho Ho HURT", a: [["atk", 14]] },
		nice: { n: "Naughty or Nice", a: [["add", "soot", 2, "discard"], ["debuff", "soggy", 2]] },
		slam: { n: "Sack Slam", a: [["atk", 5, 3]] },
		guard: { n: "Chimney Guard", a: [["def", 20], ["buff", "might", 2]] },
	},
	ai: seq("ho", "nice", "slam", "guard"),
	phase2: "krampus",
});
enemy("krampus", {
	name: "KRAMPUS", tier: "boss", hp: [160, 160],
	lines: ["SANTA IS GONE. KRAMPUS IS FOREVER!"],
	moves: {
		chains: { n: "Chains of the Naughty", a: [["debuff", "soggy", 2], ["debuff", "brittle", 2]] },
		lash: { n: "Birch Lash", a: [["atk", 4, 4]] },
		howl: { n: "Midnight Howl", a: [["buff", "carol", 2], ["def", 15]] },
		sack: { n: "Into the Sack", a: [["atk", 18]] },
	},
	ai: seq("chains", "lash", "howl", "sack", "lash"),
});

enemy("naughtylist", {
	name: "The Naughty List", tier: "boss", hp: [200, 200], scale: 1.6, noCharm: true,
	lines: ["I CAN SEE INTO YOUR HEART."],
	moves: {
		read: { n: "Read the Names", a: [["debuff", "soggy", 2], ["debuff", "brittle", 1]] },
		ink: { n: "Ink Lash", a: [["atk", 6, 3]] },
		mark: { n: "Mark as Naughty", a: [["add", "naughty", 2, "discard"], ["def", 14]] },
		cut: { n: "Paper Cut", a: [["atk", 20]] },
	},
	ai: seq("read", "ink", "mark", "cut"),
});

// likes/hates live here instead of in the stat blocks so charm tuning is all in one place
const PREFS = {
	gingerling: { love: "part:paint", hate: "part:glitter", lore: "Ran out of the North Pole ovens the moment the door opened and had been living off his own crumbs for months." },
	snowman: { love: "part:musicbox", hate: "part:battery", lore: "Came to life to a song once. Has been waiting for another one ever since." },
	ornament: { love: "part:glitter", hate: "part:squeaker", lore: "Fell off the tree Last Christmas  and has held a grudge against holidays ever since." },
	rat: { love: "part:squeaker", hate: "part:bell", lore: "Despite their name, chimney rats prefer to live in arch-shaped holes cut into walls." },
	lights: { love: "part:battery", hate: "part:spring", lore: "Every string of lights that was ever stuffed in a box, tangled into one." },
	penguin: { love: "part:windup", hate: "part:wheels", lore: "A wind-up toy that kept going after its mechanism broke. It only knows forward." },
	nutgrunt: { love: "part:paint", hate: "part:squeaker", lore: "Cracks nuts for a living and wants everyone to know it." },
	toysoldier: { love: "part:windup", hate: "part:glitter", lore: "Marches in perfect lines." },
	jack: { love: "part:spring", hate: "part:windup", lore: "Has a habit of popping out at the worst possible moment. The reason is anxiety." },
	fruitcake: { love: "part:ribbon", hate: "type:atk", lore: "Gifted and regifted forty times. All it ever wants is to be eaten." },
	angel: { love: "part:glitter", hate: "part:paint", lore: "A shapely fallen angel made out of fresh snow." },
	slime: { love: "type:skl", hate: "part:battery", lore: "A spilled mug of eggnog that got ideas. Loves anything warm and cosy." },
	bat: { love: "part:bell", hate: "part:squeaker", lore: "Hangs in frozen caves. A good jingle echoes for days in there." },
	grumpkin: { love: "part:ribbon", hate: "part:squeaker", lore: "Hates the noise, the noise, the NOISE. Secretly loves a well-tied bow." },
	rogueelf: { love: "part:wheels", hate: "part:ribbon", lore: "Quit the workshop over unpaid overtime. Always planning a getaway." },
	coalgolem: { love: "part:battery", hate: "part:paint", lore: "Built from every lump of coal ever given. Craves a little spark." },
	viper: { love: "part:musicbox", hate: "part:bell", lore: "A candy cane that grew fangs. A soft tune still charms it." },
	creeper: { love: "part:ribbon", hate: "part:glitter", lore: "Mistletoe that got tired of waiting for kisses." },
	krampimp: { noCharm: true, lore: "Krampus's little helpers. Pure mischief; nothing you give them is ever enough." },
	general: { love: "rare", hate: "part:squeaker", lore: "Commander of the Nutcracker army. Accepts only the finest tribute." },
	alpha: { love: "part:bell", hate: "part:wheels", lore: "Led the sleigh team for a hundred Christmases. Still answers to the jingle of sleigh bells." },
	yeti: { love: "part:musicbox", hate: "part:bell", lore: "Lonely, enormous and very loud. A lullaby calms it right down." },
	coalbaron: { love: "rare", hate: "part:ribbon", lore: "Hoards coal and cookies alike. Values a gift by its price tag." },
	witch: { love: "part:glitter", hate: "part:wheels", lore: "Lives in a gingerbread house. Will trade anything for a sprinkle of sparkle." },
	jackfrost: { love: "part:spring", hate: "part:musicbox", lore: "Nips noses for fun. Can't sit still; loves anything bouncy." },
	santa: { love: "part:ribbon", hate: "part:battery", lore: "Read the whole Naughty List in one night. Somewhere under the coal, he's still Nick.", pips: 8,
		moods: [
			{ love: "part:ribbon", hate: "part:battery" },
			{ love: "part:musicbox", hate: "part:spring" },
			{ love: "part:bell", hate: "part:squeaker" },
			{ love: "rare", hate: "part:wheels" },
		] },
	krampus: { noCharm: true, lore: "What Santa becomes when the coal wins. There is no gift for this." },
	naughtylist: { lore: "The list itself, growing bigger with every naughty deed. Santa never had a chance." },
};
for (const id in PREFS) Object.assign(ENEMIES[id], PREFS[id]);

// groups of three get a bit less HP each, see Combat.enter
export const ENC = {
	weak: [["gingerling"], ["snowman"], ["ornament", "ornament"], ["rat", "rat"], ["lights"], ["penguin"],
		["ornament", "rat"],
		["gingerling", "penguin"]],
	normal: [["nutgrunt"], ["toysoldier", "toysoldier"], ["jack"], ["fruitcake"], ["snowman", "ornament"], ["slime"],
		["bat", "bat", "bat"], ["slime", "rat"], ["grumpkin"], ["coalgolem"], ["viper"], ["creeper"],
		["krampimp", "krampimp"], ["gingerling", "gingerling", "penguin"], ["snowman", "lights"], ["rogueelf"], ["gingerling", "rogueelf"],
		["rat", "ornament", "ornament"],
		["viper", "bat", "bat"],
		["fruitcake", "grumpkin", "creeper"],
		["snowman", "gingerling", "coalgolem"],
		["krampimp", "rogueelf"],
		["angel", "ornament"],
		["jack", "toysoldier"],
		["nutgrunt", "toysoldier"],
		["jack", "lights"]],
	elite: [["general"], ["yeti"], ["coalbaron"], ["alpha"], ["witch", "gingerling"], ["jackfrost"],
		["general", "toysoldier", "toysoldier"],
		["yeti", "snowman"],
		["jackfrost", "lights"]],
	boss: [["santa"]],
};