export const EVENTS = [];
function ev(id, title, text, choices) { EVENTS.push({ id, title, text, choices }); }

// fx returns a plain string, or an object asking for a fight / pick / choose
ev("lostelf", "The Lost Elf",
	"A tiny elf shivers in a snowdrift, clutching a broken compass. \"I may have taken a wrong turn at the candy cane forest...\"",
	[
		{ l: "Walk him home (lose 6 HP, gain a relic)", fx: R => { R.hurt(6); const id = R.randRelicId(1); R.addRelic(id); return `The elf's family gives you their heirloom: ${R.relicName(id)}.`; } },
		{ l: "Point the way (+5 Spirit)", fx: R => { R.addSpirit(5); return "He waves goodbye and skips off whistling a carol."; } },
		{ l: "Ignore him (-5 Spirit)", fx: R => { R.addSpirit(-5); return "His little sobs follow you into the night."; } },
	]);
ev("workshop", "Abandoned Workshop",
	"Half-finished toys line the dusty benches. The tools are still warm, as if the elves left in a hurry.",
	[
		{ l: "Tinker (Rewrap a gift)", fx: () => ({ t: "You work late into the night.", pick: { mode: "upgrade" } }) },
		{ l: "Scavenge (random gift + 30 Cookies)", fx: R => { const id = R.randGiftId(1); R.addGift(id); R.gainCookies(30); return `You find ${R.giftName(id)} and a tin of cookies.`; } },
		{ l: "Leave", fx: () => "Some places are best left alone." },
	]);
ev("carolers", "Carolers at the Door",
	"A ragged group of carolers is singing in front of a house. They look like they've been at it for quite a long time. Their voices crack with cold.",
	[
		{ l: "Sing along (+10 Spirit, heal 5)", fx: R => { R.addSpirit(10); R.heal(5); return "For one verse, the whole street glows warmer."; } },
		{ l: "Give them 25 Cookies (random potion)", req: R => R.G.cookies >= 25, fx: R => { R.spendCookies(25); const p = R.randPotionId(); R.addPotion(p); return `They hand you a steaming flask: ${R.potionName(p)}.`; } },
		{ l: "Learn their song (gain Off-Key Carol)", fx: R => { R.addGift("offkey"); return "Terribly out of tune, just how you like it."; } },
	]);
ev("pond", "The Frozen Pond",
	"A frozen pond blocks the path. As you take a closer look, you note coins that twinkle under the ice. The ice creaks under your weight.",
	[
		{ l: "Skate across (50%: +60 Cookies, else -10 HP)", fx: R => R.chance(0.5) ? (R.gainCookies(60), "You glide across and chip out a fistful of coins!") : (R.hurt(10), "CRACK! You fall through. Brrr.") },
		{ l: "Walk around (-4 Spirit)", fx: R => { R.addSpirit(-4); return "The long way. A whole hour lost."; } },
	]);
ev("cookiejar", "The Cookie Jar",
	"On a windowsill, you notice an enormous cookie jar. A note reads: FOR SANTA ONLY.",
	[
		{ l: "Reach in deep (+75 Cookies, gain Lump of Coal)", fx: R => { R.gainCookies(75); R.addGift("coal"); return "Your hand closes on cookies... and something sooty."; } },
		{ l: "Take just one (heal 10, -2 Spirit)", fx: R => { R.heal(10); R.addSpirit(-2); return "Still warm. Delicious."; } },
		{ l: "Leave it", fx: () => "Santa would have wanted that." },
	]);
ev("globeshop", "The Snow Globe Seller",
	"An old woman sells snow globes. You take one from her hands almost absentmindedly.",
	[
		{ l: "Shake a globe (transform a gift)", fx: () => ({ t: "The flakes swirl... and something changes.", pick: { mode: "transform" } }) },
		{ l: "Buy one for 60 Cookies (relic)", req: R => R.G.cookies >= 60, fx: R => { R.spendCookies(60); const id = R.randRelicId(); R.addRelic(id); return `It holds ${R.relicName(id)}.`; } },
		{ l: "Move along", fx: () => "Her eyes follow you down the street." },
	]);
ev("stable", "The Empty Stable",
	"Santa's reserve reindeer pace in their stalls, hungry and restless.",
	[
		{ l: "Feed them 30 Cookies (+6 Max HP)", req: R => R.G.cookies >= 30, fx: R => { R.spendCookies(30); R.gainMaxHp(6); return "They nuzzle you gratefully. You feel sturdier."; } },
		{ l: "Ride one (lose 10 HP, gain a relic)", fx: R => { R.hurt(10); const id = R.randRelicId(2); R.addRelic(id); return `A wild ride! You find ${R.relicName(id)} in the saddlebag.`; } },
		{ l: "Leave them be", fx: () => "They stamp impatiently." },
	]);
ev("well", "The Wishing Well",
	"You stop by a wishing well and look inside. You see countless coins far below.",
	[
		{ l: "Toss 25 Cookies (+8 Spirit)", req: R => R.G.cookies >= 25, fx: R => { R.spendCookies(25); R.addSpirit(8); return "Somewhere, a child's wish comes true."; } },
		{ l: "Toss a gift in (leave a gift)", fx: () => ({ t: "The well sighs happily.", pick: { mode: "leave" } }) },
		{ l: "Drink (heal 15, -5 Spirit)", fx: R => { R.heal(15); R.addSpirit(-5); return "Refreshing. A little selfish."; } },
	]);
ev("elfgame", "Mischievous Elves",
	"Two elves with red eyes block the path. \"Hey, stranger! How about we play a game? Winner takes all!\"",
	[
		{ l: "Play (fight two Krampus Imps)", fx: () => ({ t: "\"The game is called TAG. You're IT!\"", fight: ["krampimp", "krampimp"], kind: "normal" }) },
		{ l: "Bribe them (-40 Cookies)", req: R => R.G.cookies >= 40, fx: R => { R.spendCookies(40); return "They scamper off giggling."; } },
		{ l: "Scold them (-6 HP)", fx: R => { R.hurt(6); return "They kick your shins before running away."; } },
	]);
ev("factory", "The Toy Factory",
	"The conveyor belts at a toy factory still run. Toys roll by, waiting to be wrapped.",
	[
		{ l: "Work overtime (lose 7 HP, choose a gift)", fx: R => { R.hurt(7); return { t: "Three fine toys roll off the line.", choose: [R.randGiftId(2), R.randGiftId(2), R.randGiftId(R.chance(0.4) ? 3 : 2)] }; } },
		{ l: "Take a nap on the sacks (heal 12)", fx: R => { R.heal(12); return "The hum of machines lulls you to sleep."; } },
	]);
ev("gingerhouse", "The Gingerbread House",
	"A cottage made entirely of gingerbread. You feel as though there's something or someone inside.",
	[
		{ l: "Nibble the roof (heal 20, gain Lump of Coal)", fx: R => { R.heal(20); R.addGift("coal"); return "Delicious! ...Wait, why is the gutter made of coal?"; } },
		{ l: "Knock on the door (ELITE fight)", fx: () => ({ t: "The door creaks open. \"Come in, dearie!\"", fight: ["witch", "gingerling"], kind: "elite" }) },
		{ l: "Back away slowly", fx: () => "The cackling stops. You don't look back." },
	]);
ev("sleigh", "Santa's Abandoned Sleigh",
	"Santa's sleigh lies tipped in a ditch, one of its runners snapped. The seat is still warm.",
	[
		{ l: "Search the seat (relic, -6 Spirit)", fx: R => { const id = R.randRelicId(); R.addRelic(id); R.addSpirit(-6); return `You find ${R.relicName(id)}. It feels like stealing.`; } },
		{ l: "Polish the bells (+12 Spirit)", fx: R => { R.addSpirit(12); return "They ring once, clear and bright, across the valley."; } },
	]);
// doubleJoy gets cleared in leaveGift, so it can't leak into the next night
ev("naughtykid", "The Naughty Child",
	"A boy pelts you with snowballs. \"Santa never came last year! Why in the hell should I be nice?\"",
	[
		{ l: "Give him a gift (leave; DOUBLE Spirit)", fx: R => { R.G.doubleJoy = true; return { t: "His frown wobbles... and cracks into a grin.", pick: { mode: "leave" } }; } },
		{ l: "Snowball fight! (-5 HP, gain Snowball+)", fx: R => { R.hurt(5); R.addGift("snowball", true); return "You lose in a very pathetic fashion."; } },
		{ l: "Scold him (+40 Cookies, -8 Spirit)", fx: R => { R.gainCookies(40); R.addSpirit(-8); return "He drops his pocket money and runs."; } },
	]);
ev("mistletoe", "Mistletoe Arch",
	"A great arch of mistletoe appears above the path.",
	[
		{ l: "Pass under (heal 12, gain Mistletoe Kiss)", fx: R => { R.heal(12); R.addGift("kiss"); return "A warm feeling spreads through you."; } },
		{ l: "Cut a sprig (gain a random Uncommon gift)", fx: R => { const id = R.randGiftId(2); R.addGift(id); return `It turns into ${R.giftName(id)} in your hands.`; } },
	]);
ev("toymaker", "The Old Toymaker",
	"A stooped toymaker fixes a rocking horse by candlelight. \"Need anything mended, dear?\"",
	[
		{ l: "Mend my gifts (Rewrap 2 random gifts)", fx: R => { const n = R.upgradeRandom(2); return n ? `${n} gift(s) shine like new.` : "Everything is already perfect."; } },
		{ l: "Buy a toy (50 Cookies, choose a gift)", req: R => R.G.cookies >= 50, fx: R => { R.spendCookies(50); return { t: "He shows you his finest work.", choose: [R.randGiftId(2), R.randGiftId(2), R.randGiftId(3)] }; } },
		{ l: "Leave", fx: () => "\"Merry Christmas,\" he murmurs." },
	]);
ev("blizzard", "Blizzard!",
	"A howling blizzard swallows the road. You can barely see the tip of your own nose.",
	[
		{ l: "Push through (-10 HP)", fx: R => { R.hurt(10); return "You emerge frostbitten but on schedule."; } },
		{ l: "Shelter until it passes (-8 Spirit)", fx: R => { R.addSpirit(-8); return "Hours lost."; } },
		{ l: "Drink a potion for warmth (lose a potion)", req: R => R.G.potions.some(p => p), fx: R => { R.losePotion(); return "Warm and toasty, you march on."; } },
	]);
ev("postman", "The Frozen Postman",
	"A postman lies stuck in a snowdrift, his bag bursting with letters to Santa.",
	[
		{ l: "Read the letters (+12 Spirit)", fx: R => { R.addSpirit(12); return "Every letter is a tiny, hopeful heart."; } },
		{ l: "Deliver them yourself (-6 HP, +18 Spirit)", fx: R => { R.hurt(6); R.addSpirit(18); return "Mailbox after mailbox, the village lights up."; } },
		{ l: "Burn them for warmth (heal 12, -12 Spirit)", fx: R => { R.heal(12); R.addSpirit(-12); return "The fire is warm, but your heart isn't."; } },
	]);
ev("cocoastand", "Hot Cocoa Stand",
	"A cheerful elf runs a cocoa stand in the middle of nowhere. \"Marshmallows are extra!\"",
	[
		{ l: "Small cup, 20 Cookies (heal 18)", req: R => R.G.cookies >= 20, fx: R => { R.spendCookies(20); R.heal(18); return "Perfect temperature."; } },
		{ l: "To go, 45 Cookies (2 random potions)", req: R => R.G.cookies >= 45, fx: R => { R.spendCookies(45); R.addPotion(R.randPotionId()); R.addPotion(R.randPotionId()); return "Two flasks, carefully corked."; } },
		{ l: "No thanks", fx: () => "\"Suit yourself, {nick}!\"" },
	]);
ev("fishing", "Ice Fishing",
	"A fishing hole and an abandoned rod. Something tugs at the line.",
	[
		{ l: "Reel it in", fx: R => { const r = Math.random(); if (r < 0.35) { const p = R.randPotionId(); R.addPotion(p); return `A floating bottle: ${R.potionName(p)}!`; } if (r < 0.7) { R.gainCookies(45); return "A sunken purse full of cookies!"; } R.hurt(6); return "An angry pike bites your finger."; } },
		{ l: "Leave the line", fx: () => "Patience is overrated, anyway." },
	]);
ev("ball", "The Nutcracker's Ball",
	"Music drifts from a ballroom where nutcrackers waltz to the tune. One of them bows to you.",
	[
		{ l: "Dance (-5 HP, Rewrap a gift)", fx: R => { R.hurt(5); return { t: "Your feet ache, but your spirit soars.", pick: { mode: "upgrade" } }; } },
		{ l: "Challenge him to a duel (fight)", fx: () => ({ t: "\"En garde!\"", fight: ["nutgrunt"], kind: "normal" }) },
		{ l: "Slip away", fx: () => "The music fades behind you." },
	]);
ev("stocking", "Stocking on a Fence",
	"A lonely stocking hangs on a fence post, bulging with something.",
	[
		{ l: "Peek inside (potion + 30 Cookies)", fx: R => { const p = R.randPotionId(); R.addPotion(p); R.gainCookies(30); return `Inside: ${R.potionName(p)} and some coins.`; } },
		{ l: "Fill it instead (leave a gift)", fx: () => ({ t: "Someone will be very surprised.", pick: { mode: "leave" } }) },
	]);
ev("angels", "Snow Angels",
	"A field of perfect, untouched snow sparkles under the moon.",
	[
		{ l: "Make a snow angel (heal 8, +6 Spirit)", fx: R => { R.heal(8); R.addSpirit(6); return "You laugh like a child. When did you last do that?"; } },
		{ l: "Keep moving", fx: () => "Duty calls." },
	]);
ev("fallenstar", "The Fallen Star",
	"A star has fallen into a snowbank, still glowing and sparking.",
	[
		{ l: "Take it (Tree-Top Star, or 100 Cookies)", fx: R => { if (!R.hasRelic("treestar")) { R.addRelic("treestar"); return "It settles warmly in your sack."; } R.gainCookies(100); return "It crumbles into golden coins."; } },
		{ l: "Make a wish (+20 Spirit)", fx: R => { R.addSpirit(20); return "It rises back into the sky. The world around you feels brighter."; } },
	]);
ev("exchange", "Secret Santa Swap",
	"A masked stranger holds out a wrapped box. \"Care for a swap, {title}? No peeking.\"",
	[
		{ l: "Swap (transform a gift)", fx: () => ({ t: "You trade boxes.", pick: { mode: "transform" } }) },
		{ l: "Decline", fx: () => "The stranger vanishes in a puff of smoke." },
	]);
ev("snowmanfriend", "The Lonely Snowman",
	"A small, friendly snowman sits alone in a park. It years for companionship.",
	[
		{ l: "Build him a friend (-4 HP, +10 Spirit)", fx: R => { R.hurt(4); R.addSpirit(10); return "Two snowmen, side by side. What a jolly sight!"; } },
		{ l: "Take his scarf (gain Scarf Wrap+, -8 Spirit)", fx: R => { R.addGift("scarf", true); R.addSpirit(-8); return "He watches you go with coal-black eyes."; } },
	]);
ev("mirror", "Mirror of Christmas Past",
	"An ornate mirror shows you young, laughing beside an even younger Santa.",
	[
		{ l: "Reach into the memory (duplicate a gift, -6 Max HP)", fx: R => { R.gainMaxHp(-6); return { t: "Something comes back with you.", pick: { mode: "dup" } }; } },
		{ l: "Smile and remember (+10 Spirit)", fx: R => { R.addSpirit(10); return "That's the man you're fighting for."; } },
		{ l: "Shatter it (-4 HP, +40 Cookies)", fx: R => { R.hurt(4); R.gainCookies(40); return "So many coins, so many shattered memories."; } },
	]);