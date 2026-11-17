using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sleigh
{
    [Serializable] public class GiftValue { public string key; public int @base, up; }
    [Serializable] public class FriendStep { public string op, key, status; public int hits; }
    [Serializable] public class GiftDef
    {
        public string id, name, type, target, desc, flags, enemy;
        public int rarity; public int[] cost; public GiftValue[] values; public FriendStep[] steps;
    }
    [Serializable] public class ItemDef { public string id, name, desc, target; public int rarity; public bool outside; }
    [Serializable] public class MoveAction { public string op, key, pile; public int n, hits = 1; }
    [Serializable] public class MoveDef { public string id, name; public MoveAction[] actions; }
    [Serializable] public class Weight { public string key; public int n; }
    [Serializable] public class EnemyDef
    {
        public string id, name, tier, love, hate, lore; public int[] hp;
        public int pips; public bool fly, noCharm; public float scale = 1;
        public string[] lines, sequence; public Weight[] weights; public MoveDef[] moves;
        public MoveDef Move(string key) => moves.First(m => m.id == key);
    }
    [Serializable] public class Encounter { public string[] ids; }
    [Serializable] public class EncounterPool { public string tier; public Encounter[] groups; }
    [Serializable] public class EventChoice { public string label; public int price; public bool needsPotion; }
    [Serializable] public class EventDef { public string id, title, text; public EventChoice[] choices; }
    [Serializable] public class DefinitionBundle
    {
        public GiftDef[] gifts; public ItemDef[] relics, potions, parts; public EnemyDef[] enemies;
        public EncounterPool[] encounters; public EventDef[] events;
    }
    [Serializable] public class ArtFrame
    {
        public string type, anim, path, sheet; public int frame, width, height, x, y; public float pivotX, pivotY;
        public Vector2[] eyes;
    }
    [Serializable] public class ArtBundle { public ArtFrame[] frames; }

    public static class DB
    {
        public static DefinitionBundle All { get; private set; }
        public static Dictionary<string, GiftDef> Gifts;
        public static Dictionary<string, EnemyDef> Enemies;
        public static Dictionary<string, ItemDef> Relics, Potions, Parts;
        public static Dictionary<string, EventDef> Events;
        public static Dictionary<string, ArtFrame[]> Art;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static void Load()
        {
            if (All != null) return;
            All = JsonUtility.FromJson<DefinitionBundle>(Resources.Load<TextAsset>("Sleigh/Definitions").text);
            Gifts = All.gifts.ToDictionary(x => x.id); Enemies = All.enemies.ToDictionary(x => x.id);
            Relics = All.relics.ToDictionary(x => x.id); Potions = All.potions.ToDictionary(x => x.id);
            Parts = All.parts.ToDictionary(x => x.id); Events = All.events.ToDictionary(x => x.id);
            Art = JsonUtility.FromJson<ArtBundle>(Resources.Load<TextAsset>("Sleigh/ArtIndex").text).frames
                .GroupBy(x => x.type + "/" + x.anim).ToDictionary(x => x.Key, x => x.OrderBy(y => y.frame).ToArray());
        }
        public static ArtFrame Frame(string type, string anim = "default", int frame = 0)
        {
            Load(); if (!Art.TryGetValue(type + "/" + anim, out var frames))
            { if (!Art.TryGetValue(type + "/Animation 1", out frames)) return null; }
            return frames[Mathf.Abs(frame) % frames.Length];
        }
        public static Sprite Sprite(string type, string anim = "default", int frame = 0)
        {
            var f = Frame(type, anim, frame); if (f == null) return null;
            if (!sprites.TryGetValue(f.path, out var s))
            {
                s = Resources.Load<Sprite>(f.path);
                if (s == null)
                {
                    var tex = Resources.Load<Texture2D>(f.path);
                    if (!tex) return null;
                    tex.filterMode = FilterMode.Point;
                    s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(f.pivotX, 1 - f.pivotY), 1);
                }
                sprites[f.path] = s;
            }
            return s;
        }
        public static string CharacterName(string id) => id == "tinsel" ? "Tinsel" : id == "rudolph" ? "Rudolph" : "Mrs. Claus";
        public static string CharacterArt(string id) => id == "mrs" ? "idle" : id + "_idle";
        public static int CharacterHP(string id) => id == "tinsel" ? 64 : id == "rudolph" ? 84 : 75;
        public static string PowerName(string id) => id == "tinsel" ? "TINKER" : id == "rudolph" ? "REINDEER!" : "KIND WORD";
        public static string PowerDesc(string id) => id == "tinsel" ? "Choose a toy part to add to a gift in your hand. Fills as you play gifts."
            : id == "rudolph" ? "The next reindeer of Santa's team gallops in to help. Fills when you take damage."
            : "Pick a monster: pop 1 Grumpy pip, learn what it loves and apply a random debuff. Fills from Skills and Grumpy pips.";
        public static readonly string[] Deer = { "dasher", "dancer", "prancer", "vixen", "comet", "cupid", "donner", "blitzen" };
        public static readonly string[] DeerDesc = { "Draw 2 gifts and gain 1 Cheer.", "Gain 12 Wrap.", "Gain 2 Might.", "Apply 2 Soggy and 2 Brittle to a random enemy.", "Deal 20 damage to a random enemy.", "Pop 2 Grumpy pips on a random monster.", "Deal 8 damage to ALL enemies.", "Deal 4 damage 5 times to random enemies." };
        public static readonly string[] Advent = { "Elites have 10% more HP.", "All monsters have 10% more HP.", "Start with a Lump of Coal.", "Start with 40 Spirit instead of 50.", "Resting heals less.", "Monsters have 1 extra Grumpy pip.", "Shop prices +15%.", "Start with 5 less Max HP.", "Monsters hit 10% harder.", "Santa and Krampus are tougher." };
        public static readonly string[] Kids = { "Mila", "Oskar", "Ada", "Tommy", "Lea", "Rasmus", "Grete", "Kaspar", "Liis", "Theo", "Nora", "Jakob", "Elsa", "Finn", "Maarja", "Leo", "Ida", "Hugo", "Anni", "Sam", "Rosa", "Eli", "Pip", "Juno", "Kai" };
        public static readonly string[] WishText = { "I want something with wheels!", "I want something that squeaks!", "I want something that plays music!", "I want something that jingles!", "I want something bouncy!", "I want a wind-up toy!", "I want a present with a big bow!", "I want something sparkly!", "I want something warm and cosy!", "I want something I can throw!", "I want something rare and shiny!", "I want a big surprise!" };
        public static readonly string[] StatusIds = { "might", "nimble", "soggy", "brittle", "chilled", "frostbite", "holly", "carol", "cocoa", "ribbon", "tinsel", "candles", "snowfall", "workshop", "star", "choir", "jolly", "drawnext" };
        public static readonly string[] StatusNames = { "Might", "Nimble", "Soggy", "Brittle", "Chilled", "Frostbite", "Holly", "Carol", "Milk & Cookies", "Ribbon", "Tinsel", "Candles", "Snowfall", "Workshop", "Guiding Star", "Choir", "Jolly", "Eager" };
        public static readonly string[] StatusDesc = { "Attacks deal +1 damage per stack.", "Gifts grant +1 Wrap per stack.", "Attack damage -25%. Drops by 1 each turn.", "Attack damage taken +50%. Drops by 1 each turn.", "Wrap gained -25%. Drops by 1 each turn.", "Lose this much HP at turn start; then drops by 1.", "Attackers take this much damage back.", "Gain this much Might at end of turn.", "Heal this much at turn start.", "Negates the next debuff per stack.", "Gain this much Wrap at end of turn.", "Deal this much damage to ALL enemies at turn end.", "Apply this much Frostbite to ALL at turn start.", "Draw this many extra gifts every turn.", "Gain this much Cheer every turn.", "Gain this much Might every turn.", "Gain extra Cheer next turn.", "Draw extra gifts next turn." };
        public static bool Debuff(string key) => key == "soggy" || key == "brittle" || key == "chilled" || key == "frostbite";
        public static string LikeLabel(string like)
        {
            if (like == "rare") return "Rare gifts";
            if (string.IsNullOrEmpty(like)) return "Nothing in particular";
            var a = like.Split(':'); return a[0] == "part" ? Parts[a[1]].name : TypeName(a[1]) + " gifts";
        }
        public static string TypeName(string type) => type == "atk" ? "Attack" : type == "skl" ? "Skill" : type == "pow" ? "Power" : type == "friend" ? "Friend" : "Curse";
    }

    [Serializable] public class Gift
    {
        public string Id, ToyPart; public int Uid; public bool Upgrade, FreeThisTurn;
        public GiftDef Def => DB.Gifts[Id];
        public string Name => Def.name + (Upgrade ? "+" : "");
        public int Cost => FreeThisTurn ? 0 : Mathf.Max(0, Def.cost[Upgrade ? 1 : 0] - (ToyPart == "wheels" ? 1 : 0));
        public bool CanUpgrade => !Upgrade && Def.type != "curse";
        public bool CanPart => string.IsNullOrEmpty(ToyPart) && Def.type != "curse";
        public Gift Copy() => new Gift { Id = Id, Uid = Uid, Upgrade = Upgrade, ToyPart = ToyPart, FreeThisTurn = FreeThisTurn };
        public Dictionary<string, int> Values()
        {
            var v = Def.values.ToDictionary(x => x.key, x => Upgrade ? x.up : x.@base);
            if (ToyPart == "paint") { if (v.ContainsKey("d")) v["d"] += 3; else if (v.ContainsKey("b")) v["b"] += 3; }
            if (ToyPart == "battery") foreach (var k in v.Keys.ToArray()) v[k] *= 2;
            return v;
        }
        public string Description(Battle c = null, Unit target = null)
        {
            string s = Def.desc;
            foreach (var kv in Values())
            {
                int n = kv.Value;
                if (c != null && kv.Key == "d") n = c.DamageCalc(c.Player, target, n);
                if (c != null && kv.Key == "b") n = c.BlockCalc(c.Player, n);
                s = s.Replace("{" + kv.Key + "}", n.ToString());
            }
            return s;
        }
    }
    [Serializable] public class Relic { public string id; public int n; public bool used; }
    [Serializable] public class Night { public string[] options; public string chosen; public bool done, lit; public int wish = -1; }
    [Serializable] public class RunStats { public int kills, left, joy, elites, cookiesLeft, charmed, wishes; }
    [Serializable] public class LedgerEntry { public string id; public bool seen, love, hate; public int kills, charms; }
    [Serializable] public class LedgerData { public List<LedgerEntry> entries = new List<LedgerEntry>(); }
    public static class Ledger
    {
        public static bool SuppressNotes;
        static LedgerData data;
        public static LedgerEntry Get(string id)
        {
            if (data == null) data = JsonUtility.FromJson<LedgerData>(PlayerPrefs.GetString("Sleigh.Ledger", "{}")) ?? new LedgerData();
            if (data.entries == null) data.entries = new List<LedgerEntry>();
            var e = data.entries.Find(x => x.id == id);
            if (e == null) { e = new LedgerEntry { id = id }; data.entries.Add(e); }
            return e;
        }
        public static void Note(string id, string kind)
        {
            if (SuppressNotes) return;
            var e = Get(id); if (kind == "seen") e.seen = true; if (kind == "love") e.love = true;
            if (kind == "hate") e.hate = true; if (kind == "kills") e.kills++; if (kind == "charms") e.charms++;
            PlayerPrefs.SetString("Sleigh.Ledger", JsonUtility.ToJson(data));
        }
    }
}
