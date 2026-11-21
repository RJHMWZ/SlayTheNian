#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text.RegularExpressions;
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
        [MenuItem("Sleigh/Verify Spring Festival Theme")]
        public static void VerifyTheme()
        {
            count = 0; DB.Load();
            string[] guardians = { "rat", "coalgolem", "yeti", "penguin", "alpha", "viper", "toysoldier", "creeper", "rogueelf", "angel", "grumpkin", "slime" };
            string[] zodiac = { "子鼠", "丑牛", "寅虎", "卯兔", "辰龙", "巳蛇", "午马", "未羊", "申猴", "酉鸡", "戌狗", "亥猪" };
            for (int i = 0; i < guardians.Length; i++)
            {
                string id = guardians[i];
                Require(DB.Enemies[id].name == zodiac[i] + "守护者", "zodiac guardian " + id);
                Require(DB.Gifts["f_" + id].name == DB.Enemies[id].name, "guardian friend card " + id);
                Require(DB.All.encounters.Any(p => p.groups.Any(g => g.ids.Contains(id))), "reachable guardian " + id);
                Require(DB.Enemies[id].pips > 0 && !DB.Enemies[id].noCharm, "recruitable guardian " + id);
            }
            Require(DB.CharacterName("mrs") == "一二" && DB.CharacterName("rudolph") == "布布", "playable character names");
            Require(Chinese.Text(DB.PowerName("mrs")) == "迎春送福" && Chinese.Text(DB.PowerName("rudolph")) == "生肖拜年", "festival character abilities");
            Require(DB.Relics["cookietin"].name == "团圆食盒" && DB.Relics["rednose"].name == "迎春爆竹", "festival starter relics");
            Require(DB.ZodiacAid.SequenceEqual(guardians) && DB.ZodiacAidDesc.Length == guardians.Length, "all twelve zodiac helpers have descriptions");
            foreach (string description in DB.ZodiacAidDesc)
                Require(description.All(c => char.IsWhiteSpace(c) || ChinesePixelFont.HasGlyph(c)), "zodiac helper description glyphs");
            Require(DB.Enemies["santa"].name == "年兽·临岁" && DB.Enemies["krampus"].name == "年兽·狂祟" && DB.Enemies["naughtylist"].name == "岁祟之源", "both Nian routes");
            Require(Chinese.Text("DEC 24 / 24") == "岁程 24 / 24", "formatted route progress");
            Require(Chinese.Text("KRAMPUS / Spirit: Merry") == "年兽·狂祟 / 年味：平安", "formatted boss banner");
            var catalog = JsonUtility.FromJson<Chinese.Catalog>(Resources.Load<TextAsset>("Sleigh/Localization/zh-CN").text);
            foreach (var entry in catalog.entries)
            {
                Require(!Regex.IsMatch(entry.text, "圣诞|雪橇|北极|坎卜斯|姜饼|胡桃夹|槲寄生|蛋奶酒|饼干罐|红鼻子|驯鹿|卯兔·团团|子鼠·巧巧|午马·追风"), "theme residue: " + entry.source);
                string[] Tokens(string value) => Regex.Matches(value, @"\{[^{}]+\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x).ToArray();
                Require(Tokens(entry.source).SequenceEqual(Tokens(entry.text)), "format tokens: " + entry.source);
                Require(entry.text.Where(c => !char.IsWhiteSpace(c)).All(ChinesePixelFont.HasGlyph), "font coverage: " + entry.source);
            }
            Debug.Log("Spring Festival: " + count + " theme assertions passed, including all twelve recruitable guardians, Nian routes, format tokens and font coverage.");
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
                foreach (string category in new[] { "atk", "skl", "pow", "curse", "friend" })
                    Require(DB.Sprite("CardFrame", category), "Spring Festival card frame " + category);
                foreach (var enemy in DB.All.enemies) Require(DB.Sprite("Enemy", enemy.id), "Spring Festival enemy " + enemy.id);
                foreach (var gift in DB.All.gifts)
                    Require(DB.Sprite("Gift", gift.type == "friend" ? gift.enemy : gift.id), "Spring Festival gift " + gift.id);
                foreach (var f in DB.Art.Values.SelectMany(x => x))
                {
                    var sprite = DB.Sprite(f.type, f.anim, f.frame);
                    Require(sprite, "sprite " + f.path);
                    Require(!f.path.Contains("Gift__") && !f.path.Contains("Enemy__"), "no legacy Christmas sprite path " + f.path);
                    if (f.sourceWidth > 0)
                        Require(f.x >= 0 && f.y >= 0 && f.x + f.sourceWidth <= sprite.texture.width && f.y + f.sourceHeight <= sprite.texture.height,
                            "atlas region inside texture: " + f.type + "/" + f.anim);
                }
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
