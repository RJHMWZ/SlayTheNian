#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sleigh.Editor
{
    public static class SleighRuleChecks
    {
        static int count;
        static void Require(bool value, string message) { count++; if (!value) throw new Exception("Sleigh rule check: " + message); }
        static Battle Fight(string enemy = "yeti", string character = "mrs", string kind = "normal")
        {
            var r = Run.New(character, 0, 12345); var b = new Battle(r, new[] { enemy }, kind); b.Cues.Clear();
            return b;
        }
        [MenuItem("Sleigh/Verify Migrated Rules")]
        public static void Verify()
        {
            bool suppression = Ledger.SuppressNotes; Ledger.SuppressNotes = true; count = 0;
            try
            {
                DB.Load(); Require(DB.All.gifts.Count(x => x.type != "friend") == 47, "47 base gifts");
                Require(DB.All.gifts.Count(x => x.type == "friend") == 24, "24 dynamically derived friends");
                Require(DB.All.enemies.Length == 28 && DB.All.relics.Length == 32 && DB.All.potions.Length == 20 && DB.All.events.Length == 26 && DB.All.parts.Length == 10, "source catalogs");
                Require(DB.Art.Values.Sum(x => x.Length) >= 314, "original frames plus Nian event artwork");
                foreach (var f in DB.Art.Values.SelectMany(x => x)) Require(DB.Sprite(f.type, f.anim, f.frame) != null, "sprite " + f.path);
                foreach (string character in new[] { "mrs", "tinsel", "rudolph" })
                {
                    var r = Run.New(character, 0, 49); Require(r.hp == DB.CharacterHP(character), character + " HP");
                    Require(r.bag.Count == 10 && r.bag.Count(x => x.Id == "sock") == 5 && r.bag.Count(x => x.Id == "mitten") == 4, "starter deck");
                    Require(r.bag.Count(x => x.ToyPart == "ribbon") == 1, "starter ribbon"); Require(r.spirit == 50 && r.cookies == 99 && r.map.Length == 24, "initial resources");
                    var b = new Battle(r, new[] { "gingerling" }, "normal"); Require(b.Power == 40 && b.Energy == 3 && b.Hand.Count == 5, "battle setup");
                }
                foreach (var def in DB.All.gifts.Where(x => x.type != "curse"))
                foreach (bool up in new[] { false, true })
                {
                    var b = Fight(); var t = b.Foes()[0]; t.Hp = t.MaxHp = 10000; t.Pips = t.PipMax = 1000;
                    b.Player.Hp = 30; b.Player.Block = 10; b.Hand.Clear(); b.Energy = 100;
                    var g = new Gift { Id = def.id, Uid = -1, Upgrade = up }; b.Hand.Add(g);
                    Require(b.Play(g, def.target == "e" ? t : null), "play " + def.id + (up ? "+" : ""));
                    Require(!b.Hand.Contains(g) && b.Energy >= 0, "gift leaves hand " + def.id);
                    Require(def.type == "pow" || b.Consumed.Contains(g) || b.Discard.Contains(g), "correct pile " + def.id);
                }
                {
                    var b = Fight("snowman"); var t = b.Foes()[0]; t.Status.Clear(); b.Player.Status.Clear();
                    Require(b.DamageCalc(b.Player, t, 6) == 6, "base attack"); b.Apply(b.Player, "might", 2); b.Apply(b.Player, "soggy", 1); b.Apply(t, "brittle", 1);
                    Require(b.DamageCalc(b.Player, t, 6) == 9, "multiplicative damage floor"); b.Apply(b.Player, "nimble", 2); b.Apply(b.Player, "chilled", 1);
                    Require(b.BlockCalc(b.Player, 5) == 5 && b.BlockCalc(b.Player, 5, true) == 5, "wrap modifiers");
                    b.Apply(t, "ribbon", 1); b.Apply(t, "frostbite", 5); Require(t.S("frostbite") == 0 && t.S("ribbon") == 0, "ribbon negates debuff");
                }
                {
                    var b = Fight("gingerling"); var t = b.Foes()[0]; b.ChangePips(t, 2); b.Check();
                    Require(t.Charmed && b.Won && b.FriendsWon.Contains("gingerling") && b.Run.spirit == 53, "peaceful charm and reward");
                    b = Fight("gingerling"); t = b.Foes()[0]; b.ChangePips(t, 2); b.Damage(t, 0); b.EndTurn();
                    Require(!t.Charmed && t.Pips == t.PipMax, "harming softened breaks trust even at zero damage");
                }
                for (int tier = 0; tier < 5; tier++)
                {
                    var b = Fight("santa", "mrs", "boss"); b.Run.spirit = new[] { 0, 25, 50, 70, 90 }[tier]; var t = b.Foes()[0]; b.Damage(t, 1000, true); b.Check();
                    Require(t.Id == "krampus" && !b.Ended, "Santa phase transition"); Require(t.MaxHp == new[] { 184, 173, 160, 144, 128 }[tier], "Spirit scales Krampus HP");
                }
                {
                    var b = Fight("santa", "mrs", "boss"); b.Run.charmed.AddRange(new[] { "gingerling", "snowman" }); var santa = b.Foes()[0]; b.ChangePips(santa, 8); b.Check();
                    Require(santa.Charmed && b.SecretFight && b.Foes().Single().Id == "naughtylist", "hidden Santa route");
                    Require(b.Foes()[0].Hp == 188, "friends each deal 6 to Naughty List"); b.Damage(b.Foes()[0], 1000, true); b.Check(); Require(b.Won && b.Run.secret, "hidden victory");
                }
                foreach (var enemy in DB.All.enemies)
                {
                    var b = Fight(enemy.id); var t = b.Foes()[0];
                    for (int turn = 0; turn < 12; turn++) { t.Turn = turn; b.ChooseIntent(t); Require(enemy.moves.Any(m => m.id == t.Move), "AI intent " + enemy.id); }
                }
                foreach (var p in DB.All.potions)
                {
                    var b = Fight(); b.Player.Hp = 20; b.Run.potions[0] = p.id;
                    Require(b.UsePotion(0, p.target == "e" ? b.Foes()[0] : null), "potion " + p.id); Require(b.Run.potions[0] == null, "potion consumed " + p.id);
                }
                foreach (var ev in DB.All.events)
                    for (int i = 0; i < ev.choices.Length; i++)
                    { var r = Run.New("mrs", 0, 123); r.cookies = 999; r.potions[0] = "hotcocoa"; var o = EventRules.Resolve(r, ev.id, i); Require(o != null && !string.IsNullOrEmpty(o.text), "event " + ev.id + "/" + i); Require(r.hp > 0 && r.cookies >= 0, "event resources"); }
                {
                    var r = Run.New("mrs", 10, 88); Require(r.maxHp == 70 && r.spirit == 40 && r.bag.Any(x => x.Id == "coal"), "Advent initial rules");
                    r = Run.New("mrs", 0, 19); var g = r.bag.First(x => x.ToyPart == "ribbon"); r.wish = 6; Require(r.Joy(g) == 7, "ribbon starter Spirit");
                    int before = r.spirit; r.LeaveGift(g); Require(r.spirit - before == 11 && r.stats.wishes == 1, "wish bonus");
                    Require(r.LeaveCookieCost == 30, "first cookie price"); r.LeaveCookies(); Require(r.LeaveCookieCost == 45, "cookie escalation");
                }
                Debug.Log("Sleigh: " + count + " migrated rule assertions passed, including all gifts/upgrades, AI catalogs, potions, events and both boss routes.");
            }
            finally { Ledger.SuppressNotes = suppression; }
        }
    }
}
#endif
