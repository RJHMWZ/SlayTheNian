using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sleigh
{
    [Serializable] public class Run
    {
        public const int SaveSlotCount = 5;
        const string LegacySaveKey = "Sleigh.Run";
        const string MigrationKey = "Sleigh.RunSlotsV1";
        public int saveSlot = -1;
        public long savedUtcTicks;
        public bool CanContinue => active && hp > 0 && night >= 0 && night < 24;
        public int seed, advent, hp, maxHp, cookies = 99, spirit = 50, night, uid = 1, potionSlots = 3, leaveCookieUses;
        public uint rngState;
        public string character;
        public bool active = true, secret, doubleJoy;
        public float potionChance = .4f;
        public int wish = -1;
        public List<Gift> bag = new List<Gift>();
        public List<Relic> relics = new List<Relic>();
        public List<string> charmed = new List<string>(), usedEvents = new List<string>(), usedEnc = new List<string>();
        public string[] potions = new string[3];
        public Night[] map;
        public RunStats stats = new RunStats();
        static readonly string[][] Template = {
            new[]{"battle"},new[]{"battle","event"},new[]{"event","battle"},new[]{"battle"},
            new[]{"elite","event"},new[]{"rest"},new[]{"battle","event"},new[]{"treasure"},new[]{"battle","event"},
            new[]{"shop"},new[]{"elite","battle"},new[]{"event","battle"},new[]{"battle"},new[]{"rest","shop"},
            new[]{"elite"},new[]{"battle","event"},new[]{"battle","treasure"},new[]{"event","battle"},new[]{"elite","battle"},
            new[]{"battle","event"},new[]{"shop","event"},new[]{"elite","battle"},new[]{"rest"},new[]{"boss"}
        };
        public static Run New(string character, int advent, int seed)
        {
            DB.Load();
            var r = new Run { character = character, advent = advent, seed = seed, rngState = (uint)seed };
            r.hp = r.maxHp = DB.CharacterHP(character);
            r.map = Template.Select(x => { var opts = x.ToArray(); r.Shuffle(opts); return new Night { options = opts }; }).ToArray();
            for (int i = 0; i < 4; i++) r.AddGift("sock"); r.AddGift("sock", false, "ribbon");
            for (int i = 0; i < 4; i++) r.AddGift("mitten"); r.AddGift("snowball");
            if (r.Adv(3)) r.AddGift("coal"); if (r.Adv(4)) r.spirit = 40;
            if (r.Adv(8)) { r.maxHp -= 5; r.hp = r.maxHp; }
            r.AddRelic(character == "tinsel" ? "toolbelt" : character == "rudolph" ? "rednose" : "cookietin");
            return r;
        }
        public double Random()
        {
            unchecked
            {
                rngState += 0x6D2B79F5u;
                uint t = rngState; t = (t ^ (t >> 15)) * (1u | t); t ^= t + (t ^ (t >> 7)) * (61u | t);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
        public int Int(int a, int b) => a + (int)(Random() * (b - a + 1));
        public T Pick<T>(IList<T> list) => list.Count == 0 ? default : list[Int(0, list.Count - 1)];
        public void Shuffle<T>(IList<T> a) { for (int i = a.Count - 1; i > 0; i--) { int j = Int(0, i); T t = a[i]; a[i] = a[j]; a[j] = t; } }
        public bool Adv(int n) => advent >= n;
        public bool Has(string id) => relics.Any(x => x.id == id);
        public Relic Relic(string id) => relics.Find(x => x.id == id);
        public Gift AddGift(string id, bool up = false, string part = null)
        { var g = new Gift { Id = id, Uid = uid++, Upgrade = up, ToyPart = part }; bag.Add(g); return g; }
        public void UpgradeRandom(int n = 1)
        { for (int i = 0; i < n; i++) { var pool = bag.Where(x => x.CanUpgrade).ToList(); if (pool.Count == 0) break; Pick(pool).Upgrade = true; } }
        public string AddFriend(string enemy)
        {
            var id = "f_" + enemy; var g = bag.Find(x => x.Id == id);
            if (g == null) { AddGift(id); return "NEW FRIEND"; }
            if (!g.Upgrade) { g.Upgrade = true; return "FRIEND UPGRADED"; }
            AddSpirit(2); return "+2 SPIRIT";
        }
        public void AddRelic(string id)
        {
            if (string.IsNullOrEmpty(id) || Has(id)) return;
            relics.Add(new Relic { id = id });
            if (id == "fruitcake") GainMaxHP(10);
            if (id == "sugarplum") { potionSlots = 4; Array.Resize(ref potions, 4); }
        }
        public bool AddPotion(string id)
        { for (int i = 0; i < potionSlots; i++) if (string.IsNullOrEmpty(potions[i])) { potions[i] = id; return true; } return false; }
        public void Heal(int n) => hp = Mathf.Min(maxHp, hp + n);
        public void Hurt(int n) => hp = Mathf.Max(1, hp - n);
        public void GainMaxHP(int n) { maxHp = Mathf.Max(1, maxHp + n); hp = Mathf.Clamp(hp + Mathf.Max(0, n), 1, maxHp); }
        public void AddSpirit(int n) => spirit = Mathf.Clamp(spirit + n, 0, 100);
        public bool Spend(int n) { if (cookies < n) return false; cookies -= n; return true; }
        public int SpiritTier => spirit >= 90 ? 4 : spirit >= 70 ? 3 : spirit >= 50 ? 2 : spirit >= 25 ? 1 : 0;
        public string SpiritName => new[] { "Grim", "Fading", "Merry", "Festive", "Jolly" }[SpiritTier];
        public int RollRarity(bool elite = false) { double r = Random(); return elite ? r < .15 ? 3 : r < .5 ? 2 : 1 : r < .07 ? 3 : r < .38 ? 2 : 1; }
        public string RandomGift(int rarity = -1)
        { int r = rarity < 0 ? RollRarity() : rarity; return Pick(DB.All.gifts.Where(x => x.rarity == r).Select(x => x.id).ToList()); }
        public string RolledGift() => RandomGift(RollRarity());
        public string[] GiftChoices(bool elite = false, int n = 3)
        { var l = new List<string>(); for (int i = 0; l.Count < n && i < 100; i++) { string id = RandomGift(RollRarity(elite)); if (!l.Contains(id)) l.Add(id); } return l.ToArray(); }
        public string RandomRelic(int rarity = -1)
        {
            var pool = DB.All.relics.Where(x => x.rarity > 0 && !Has(x.id)).ToList(); if (pool.Count == 0) return null;
            int want = rarity >= 0 ? rarity : Random() < .5 ? 1 : Random() < .67 ? 2 : 3;
            var tier = pool.Where(x => x.rarity == want).ToList(); return Pick(tier.Count > 0 ? tier : pool).id;
        }
        public string RandomPotion() => Pick(DB.All.potions).id;
        public string[] Encounter(string tier)
        {
            var all = DB.All.encounters.First(x => x.tier == tier).groups;
            var pool = all.Where(x => !usedEnc.Contains(string.Join(",", x.ids))).ToArray();
            var g = Pick(pool.Length > 0 ? pool : all); usedEnc.Add(string.Join(",", g.ids)); return g.ids;
        }
        public EventDef Event()
        {
            var pool = DB.All.events.Where(x => !usedEvents.Contains(x.id)).ToList();
            var e = Pick(pool.Count > 0 ? pool : DB.All.events.ToList()); usedEvents.Add(e.id); return e;
        }
        public int Joy(Gift gi)
        {
            if (gi.Def.type == "curse") return Has("coalscuttle") ? 2 : -5;
            int joy = gi.Def.type == "friend" ? 10 : new[] { 2, 3, 5, 8 }[gi.Def.rarity];
            return joy + (gi.Upgrade ? 2 : 0) + (string.IsNullOrEmpty(gi.ToyPart) ? 0 : DB.Parts[gi.ToyPart].rarity) + (gi.ToyPart == "ribbon" ? 3 : 0);
        }
        public bool WishOK(int w, Gift gi)
        {
            if (gi.Def.type == "curse") return false;
            string[] parts = { "wheels", "squeaker", "musicbox", "bell", "spring", "windup", "ribbon" };
            if (w >= 0 && w < 7) return gi.ToyPart == parts[w];
            if (w == 7) return gi.ToyPart == "glitter" || gi.ToyPart == "paint";
            if (w == 8) return gi.Values().ContainsKey("b");
            if (w == 9) return gi.Def.type == "atk";
            if (w == 10) return gi.Def.rarity == 3 || gi.Upgrade;
            return w == 11 && gi.Def.type == "pow";
        }
        public void PickWish()
        {
            var n = map[night]; if (n.wish < 0)
            { var can = Enumerable.Range(0, 12).Where(w => bag.Any(g => WishOK(w, g))).ToList(); n.wish = can.Count > 0 && Random() < .7 ? Pick(can) : Int(0, 11); }
            wish = n.wish;
        }
        public int LeaveGift(Gift gi)
        {
            int joy = Joy(gi); if (WishOK(wish, gi)) { joy += 4; stats.wishes++; }
            if (doubleJoy && joy > 0) joy *= 2; doubleJoy = false;
            bag.Remove(gi); map[night].lit = joy > 0; stats.left++; if (joy > 0) stats.joy += joy;
            if (Has("gifttag")) cookies += 15; if (Has("ledger")) AddSpirit(3); if (Has("workbench")) UpgradeRandom();
            AddSpirit(joy); return joy;
        }
        public int LeaveCookieCost => 30 + 15 * leaveCookieUses;
        public bool LeaveCookies()
        {
            int cost = LeaveCookieCost; if (!Spend(cost)) return false;
            leaveCookieUses++; stats.cookiesLeft += cost; map[night].lit = true; AddSpirit(1); return true;
        }
        public bool CanSkipLeave => Has("sackpatch") && bag.Count <= 12;
        public void FinishNode() { map[night].done = true; wish = -1; night++; Save(); }
        static string SlotKey(int slot)
        {
            if (slot < 0 || slot >= SaveSlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return "Sleigh.Run.Slot." + slot;
        }
        static Run ReadSave(string json, int slot)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var saved = JsonUtility.FromJson<Run>(json);
                if (saved == null || saved.map == null || saved.map.Length != 24 || saved.night < 0 || saved.night > 24 ||
                    saved.map.Any(n => n == null || n.options == null || n.options.Length == 0) ||
                    saved.bag == null || saved.relics == null || saved.potions == null || saved.stats == null ||
                    saved.maxHp <= 0 || saved.potionSlots < 0 || saved.potionSlots > saved.potions.Length ||
                    (saved.character != "mrs" && saved.character != "rudolph" && saved.character != "tinsel")) return null;
                // The storage slot is authoritative, including when loading a legacy save.
                saved.saveSlot = slot;
                return saved;
            }
            catch { return null; }
        }
        static void MigrateLegacySave()
        {
            if (PlayerPrefs.GetInt(MigrationKey, 0) != 0) return;
            string legacy = PlayerPrefs.GetString(LegacySaveKey, "");
            var saved = ReadSave(legacy, 0);
            if (saved != null)
            {
                for (int slot = 0; slot < SaveSlotCount; slot++)
                    if (string.IsNullOrEmpty(PlayerPrefs.GetString(SlotKey(slot), "")))
                    {
                        saved.saveSlot = slot;
                        PlayerPrefs.SetString(SlotKey(slot), JsonUtility.ToJson(saved));
                        break;
                    }
            }
            PlayerPrefs.SetInt(MigrationKey, 1);
            PlayerPrefs.Save();
        }
        public static bool HasSave(int slot)
        {
            string key = SlotKey(slot); MigrateLegacySave();
            return !string.IsNullOrEmpty(PlayerPrefs.GetString(key, ""));
        }
        public void Save()
        {
            MigrateLegacySave();
            if (saveSlot == -1)
            {
                for (int slot = 0; slot < SaveSlotCount; slot++)
                    if (!HasSave(slot)) { saveSlot = slot; break; }
                if (saveSlot == -1) throw new InvalidOperationException("All five save slots are occupied. Choose a slot before starting a run.");
            }
            string key = SlotKey(saveSlot);
            savedUtcTicks = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(key, JsonUtility.ToJson(this)); PlayerPrefs.Save();
        }
        public static Run Load(int slot = 0)
        {
            string key = SlotKey(slot); DB.Load(); MigrateLegacySave();
            return ReadSave(PlayerPrefs.GetString(key, ""), slot);
        }
        public static void DeleteSave(int slot)
        {
            string key = SlotKey(slot); MigrateLegacySave();
            PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
        }
    }
}
