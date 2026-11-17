import { PAL } from "./engine.js";

// Claus uses plain animation names (ADD THEM LATER!)
export const CHARS = {
	mrs: {
		id: "mrs", name: "Mrs. Claus", nick: "Mrs. C", title: "missus", hp: 75, relic: "cookietin", prefix: "", col: PAL.red,
		power: { name: "KIND WORD", desc: "Pick a monster: pop 1 Grumpy pip, learn what it loves and apply a random debuff.", charge: "Fills when you play Skill gifts.", aim: true },
		blurb: ["Runs the North Pole while Santa", "takes all the credit."],
		stanza2: ["But up at his home, by the fire's warm glow,", "Sat wise Mrs. Claus. She won't let it go!"],
		stanza3: ["\"Twenty-five nights until Christmas morn breaks.", "I'll keep Christmas living, whatever it takes!", "I'll fight every monster from here to the Pole", "And bring my dear husband back, body and soul!\""],
		ending: ["\"Martha,\" he whispered, \"you've saved Christmas Day!\"", "\"I know, dear,\" she said. \"Now get on that sleigh!\""],
	},
	tinsel: {
		id: "tinsel", name: "Tinsel", nick: "Tinsel", title: "little elf", hp: 64, relic: "toolbelt", prefix: "tinsel_", col: PAL.green,
		power: { name: "TINKER", desc: "Choose a toy part to add to a gift in your hand.", charge: "Fills as you play gifts." },
		blurb: ["Head toymaker of Santa's", "workshop and his trusted advisor."],
		stanza2: ["But down in the workshop, by lantern-light's glow,", "Worked Tinsel the elf, who would not let it go!"],
		stanza3: ["\"Twenty-five nights till the Christmas bells chime.", "I'll mend every toy and I'll do it in time!", "I'll hammer and tinker my way to the Pole", "And bring the boss back, body and soul!\""],
		ending: ["\"Tinsel,\" he whispered, \"you've saved Christmas Day!\"", "\"I know, boss,\" she grinned. \"Now get on that sleigh!\""],
	},
	rudolph: {
		id: "rudolph", name: "Rudolph", nick: "Rudie", title: "red-nose", hp: 84, relic: "rednose", prefix: "rudolph_", col: 0xff4040,
		power: { name: "REINDEER!", desc: "The next reindeer of Santa's team gallops in to help.", charge: "Fills when you take damage." },
		blurb: ["The reindeer with the brightest", "nose and the biggest heart."],
		stanza2: ["But out in the stable, his nose all aglow,", "Stood Rudolph the reindeer, who would not let go!"],
		stanza3: ["\"Twenty-five nights till the sleigh bells will chime.", "I'll light up the dark and the cold, all in time!", "I'll gallop through blizzards from here to the Pole", "And bring Santa back, body and soul!\""],
		ending: ["\"Rudie,\" he whispered, \"you've saved Christmas Day!\"", "So Rudolph just snorted and lit up the way."],
	},
};
export const CHAR_ORDER = ["mrs", "tinsel", "rudolph"];

export const POWER_START = 40;
export const POWER_GAIN = {
	mrs: { pip: 15, skl: 10 },
	tinsel: { gift: 8, part: 6 },
	rudolph: { hurt: 3, blocked: 1 },
};
// gets shuffled at the start of every battle
export const REINDEER = [
	{ id: "dasher", name: "Dasher", desc: "Draw 2 gifts and gain 1 Cheer." },
	{ id: "dancer", name: "Dancer", desc: "Gain 12 Wrap." },
	{ id: "prancer", name: "Prancer", desc: "Gain 2 Might." },
	{ id: "vixen", name: "Vixen", desc: "Apply 2 Soggy and 2 Brittle to a random enemy." },
	{ id: "comet", name: "Comet", desc: "Deal 20 damage to a random enemy." },
	{ id: "cupid", name: "Cupid", desc: "Pop 2 Grumpy pips on a random monster (no damage)." },
	{ id: "donner", name: "Donner", desc: "Thunder! Deal 8 damage to ALL enemies." },
	{ id: "blitzen", name: "Blitzen", desc: "Lightning! Deal 4 damage 5 times to random enemies." },
];

let cur = "mrs";
export const setChar = id => { cur = CHARS[id] ? id : "mrs"; };
export const curChar = () => CHARS[cur];
// swaps {nick} {name} {title} for whoever
export function T(s) {
	if (typeof s !== "string" || s.indexOf("{") < 0) return s;
	const c = CHARS[cur];
	return s.replace(/\{nick\}/g, c.nick).replace(/\{name\}/g, c.name).replace(/\{title\}/g, c.title);
}