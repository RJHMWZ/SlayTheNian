using System.Linq;

namespace Sleigh
{
    public class EventOutcome
    {
        public string text, picker, kind = "normal"; public string[] choose, fight;
        public EventOutcome(string text) { this.text = text; }
    }
    public static class EventRules
    {
        public static EventOutcome Resolve(Run r, string id, int i)
        {
            string relic(int rarity = -1) { string rid = r.RandomRelic(rarity); r.AddRelic(rid); return rid == null ? "nothing" : DB.Relics[rid].name; }
            string gift(int rarity) { string gid = r.RandomGift(rarity); r.AddGift(gid); return DB.Gifts[gid].name; }
            string potion() { string p = r.RandomPotion(); r.AddPotion(p); return DB.Potions[p].name; }
            EventOutcome result(string text) => new EventOutcome(text);
            EventOutcome picker(string text, string mode) => new EventOutcome(text) { picker = mode };
            EventOutcome fight(string text, string[] ids, string kind = "normal") => new EventOutcome(text) { fight = ids, kind = kind };
            EventOutcome choices(string text, params string[] ids) => new EventOutcome(text) { choose = ids };
            switch (id)
            {
                case "lostelf":
                    if (i == 0) { r.Hurt(6); return result("The elf's family gives you their heirloom: " + relic(1) + "."); }
                    if (i == 1) { r.AddSpirit(5); return result("He waves goodbye and skips off whistling a carol."); }
                    r.AddSpirit(-5); return result("His little sobs follow you into the night.");
                case "workshop":
                    if (i == 0) return picker("You work late into the night.", "upgrade");
                    if (i == 1) { string g = gift(1); r.cookies += 30; return result("You find " + g + " and a tin of cookies."); }
                    return result("Some places are best left alone.");
                case "carolers":
                    if (i == 0) { r.AddSpirit(10); r.Heal(5); return result("For one verse, the whole street glows warmer."); }
                    if (i == 1) { r.Spend(25); return result("They hand you a steaming flask: " + potion() + "."); }
                    r.AddGift("offkey"); return result("Terribly out of tune, just how you like it.");
                case "pond":
                    if (i == 0) { if (r.Random() < .5) { r.cookies += 60; return result("You glide across and chip out a fistful of coins!"); } r.Hurt(10); return result("CRACK! You fall through. Brrr."); }
                    r.AddSpirit(-4); return result("The long way. A whole hour lost.");
                case "cookiejar":
                    if (i == 0) { r.cookies += 75; r.AddGift("coal"); return result("Your hand closes on cookies... and something sooty."); }
                    if (i == 1) { r.Heal(10); r.AddSpirit(-2); return result("Still warm. Delicious."); } return result("Santa would have wanted that.");
                case "globeshop":
                    if (i == 0) return picker("The flakes swirl... and something changes.", "transform");
                    if (i == 1) { r.Spend(60); return result("It holds " + relic() + "."); } return result("Her eyes follow you down the street.");
                case "stable":
                    if (i == 0) { r.Spend(30); r.GainMaxHP(6); return result("They nuzzle you gratefully. You feel sturdier."); }
                    if (i == 1) { r.Hurt(10); return result("A wild ride! You find " + relic(2) + " in the saddlebag."); } return result("They stamp impatiently.");
                case "well":
                    if (i == 0) { r.Spend(25); r.AddSpirit(8); return result("Somewhere, a child's wish comes true."); }
                    if (i == 1) return picker("The well sighs happily.", "leave");
                    r.Heal(15); r.AddSpirit(-5); return result("Refreshing. A little selfish.");
                case "elfgame":
                    if (i == 0) return fight("The game is called TAG. You're IT!", new[] { "krampimp", "krampimp" });
                    if (i == 1) { r.Spend(40); return result("They scamper off giggling."); } r.Hurt(6); return result("They kick your shins before running away.");
                case "factory":
                    if (i == 0) { r.Hurt(7); return choices("Three fine toys roll off the line.", r.RandomGift(2), r.RandomGift(2), r.RandomGift(r.Random() < .4 ? 3 : 2)); }
                    r.Heal(12); return result("The hum of machines lulls you to sleep.");
                case "gingerhouse":
                    if (i == 0) { r.Heal(20); r.AddGift("coal"); return result("Delicious! ...Wait, why is the gutter made of coal?"); }
                    if (i == 1) return fight("The door creaks open. Come in, dearie!", new[] { "witch", "gingerling" }, "elite"); return result("The cackling stops. You don't look back.");
                case "sleigh":
                    if (i == 0) { string name = relic(); r.AddSpirit(-6); return result("You find " + name + ". It feels like stealing."); }
                    r.AddSpirit(12); return result("They ring once, clear and bright, across the valley.");
                case "naughtykid":
                    if (i == 0) { r.doubleJoy = true; return picker("His frown wobbles... and cracks into a grin.", "leave"); }
                    if (i == 1) { r.Hurt(5); r.AddGift("snowball", true); return result("You lose in a very pathetic fashion."); }
                    r.cookies += 40; r.AddSpirit(-8); return result("He drops his pocket money and runs.");
                case "mistletoe":
                    if (i == 0) { r.Heal(12); r.AddGift("kiss"); return result("A warm feeling spreads through you."); }
                    return result("It turns into " + gift(2) + " in your hands.");
                case "toymaker":
                    if (i == 0) { int n = r.bag.Count(x => x.CanUpgrade); r.UpgradeRandom(2); return result(n > 0 ? System.Math.Min(n, 2) + " gift(s) shine like new." : "Everything is already perfect."); }
                    if (i == 1) { r.Spend(50); return choices("He shows you his finest work.", r.RandomGift(2), r.RandomGift(2), r.RandomGift(3)); } return result("Merry Christmas, he murmurs.");
                case "blizzard":
                    if (i == 0) { r.Hurt(10); return result("You emerge frostbitten but on schedule."); }
                    if (i == 1) { r.AddSpirit(-8); return result("Hours lost."); }
                    int pslot = System.Array.FindIndex(r.potions, p => !string.IsNullOrEmpty(p)); if (pslot >= 0) r.potions[pslot] = null; return result("Warm and toasty, you march on.");
                case "postman":
                    if (i == 0) { r.AddSpirit(12); return result("Every letter is a tiny, hopeful heart."); }
                    if (i == 1) { r.Hurt(6); r.AddSpirit(18); return result("Mailbox after mailbox, the village lights up."); }
                    r.Heal(12); r.AddSpirit(-12); return result("The fire is warm, but your heart isn't.");
                case "cocoastand":
                    if (i == 0) { r.Spend(20); r.Heal(18); return result("Perfect temperature."); }
                    if (i == 1) { r.Spend(45); potion(); potion(); return result("Two flasks, carefully corked."); } return result("Suit yourself, {nick}!");
                case "fishing":
                    if (i == 1) return result("Patience is overrated, anyway.");
                    double roll = r.Random(); if (roll < .35) return result("A floating bottle: " + potion() + "!");
                    if (roll < .7) { r.cookies += 45; return result("A sunken purse full of cookies!"); } r.Hurt(6); return result("An angry pike bites your finger.");
                case "ball":
                    if (i == 0) { r.Hurt(5); return picker("Your feet ache, but your spirit soars.", "upgrade"); }
                    if (i == 1) return fight("En garde!", new[] { "nutgrunt" }); return result("The music fades behind you.");
                case "stocking":
                    if (i == 0) { string pn = potion(); r.cookies += 30; return result("Inside: " + pn + " and some coins."); }
                    return picker("Someone will be very surprised.", "leave");
                case "angels":
                    if (i == 0) { r.Heal(8); r.AddSpirit(6); return result("You laugh like a child. When did you last do that?"); } return result("Duty calls.");
                case "fallenstar":
                    if (i == 0) { if (!r.Has("treestar")) { r.AddRelic("treestar"); return result("It settles warmly in your sack."); } r.cookies += 100; return result("It crumbles into golden coins."); }
                    r.AddSpirit(20); return result("It rises back into the sky. The world around you feels brighter.");
                case "exchange": return i == 0 ? picker("You trade boxes.", "transform") : result("The stranger vanishes in a puff of smoke.");
                case "snowmanfriend":
                    if (i == 0) { r.Hurt(4); r.AddSpirit(10); return result("Two snowmen, side by side. What a jolly sight!"); }
                    r.AddGift("scarf", true); r.AddSpirit(-8); return result("He watches you go with coal-black eyes.");
                case "mirror":
                    if (i == 0) { r.GainMaxHP(-6); return picker("Something comes back with you.", "dup"); }
                    if (i == 1) { r.AddSpirit(10); return result("That's the man you're fighting for."); }
                    r.Hurt(4); r.cookies += 40; return result("So many coins, so many shattered memories.");
                default: throw new System.InvalidOperationException("Unmigrated event " + id);
            }
        }
    }
}
