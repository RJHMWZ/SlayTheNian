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
        // Optional equal-cell atlas. Existing single-sprite paths remain supported.
        public int columns, rows, tile;
        public bool flipX;
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
            Chinese.Localize(All);
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
            string spriteKey = f.columns > 0 ? f.path + "#" + f.tile : f.path;
            if (!sprites.TryGetValue(spriteKey, out var s))
            {
                s = f.columns > 0 ? null : Resources.Load<Sprite>(f.path);
                if (s == null)
                {
                    var tex = Resources.Load<Texture2D>(f.path);
                    if (!tex) return null;
                    tex.filterMode = f.columns > 0 ? FilterMode.Bilinear : FilterMode.Point;
                    Rect region = new Rect(0, 0, tex.width, tex.height);
                    if (f.columns > 0)
                    {
                        int cellWidth = tex.width / f.columns, cellHeight = tex.height / f.rows;
                        region = new Rect(f.tile % f.columns * cellWidth, tex.height - (f.tile / f.columns + 1) * cellHeight, cellWidth, cellHeight);
                    }
                    s = UnityEngine.Sprite.Create(tex, region, new Vector2(f.pivotX, 1 - f.pivotY), 1);
                }
                sprites[spriteKey] = s;
            }
            return s;
        }
        // Legacy IDs are intentionally retained for save compatibility.
        public static string CharacterName(string id) => id == "tinsel" ? "符匠·青禾" : id == "rudolph" ? "巡山客·赤行" : "守灯人·阿照";
        public static string CharacterArt(string id) => id == "mrs" ? "idle" : id + "_idle";
        public static int CharacterHP(string id) => id == "tinsel" ? 64 : id == "rudolph" ? 84 : 75;
        public static string PowerName(string id) => id == "tinsel" ? "百工绘符" : id == "rudolph" ? "八方援手" : "春符安魂";
        public static string PowerDesc(string id) => id == "tinsel" ? "选择一枚符印，附于手中的法牌。打出法牌时蓄力。"
            : id == "rudolph" ? "依次呼唤八方山灵助阵。受到伤害时蓄力。"
            : "选择妖灵：消除1点怨气，探知喜好并施加随机减益。打出技能和消除怨气时蓄力。";
        public static readonly string[] Deer = { "dasher", "dancer", "prancer", "vixen", "comet", "cupid", "donner", "blitzen" };
        public static readonly string[] DeerDesc = { "抽2张法牌，获得1灯火。", "获得12格挡。", "获得2神勇。", "对随机敌人施加2虚弱和2破绽。", "对随机敌人造成20伤害。", "消除随机妖灵的2点怨气。", "对所有敌人造成8伤害。", "对随机敌人造成4伤害，重复5次。" };
        public static readonly string[] Advent = { "精英生命增加10%。", "所有妖灵生命增加10%。", "初始携带一张晦炭。", "初始岁火从50降至40。", "围炉疗伤效果降低。", "妖灵额外拥有1点怨气。", "年货铺价格增加15%。", "初始生命上限降低5。", "妖灵攻击提高10%。", "年兽及噬岁形态更加强大。" };
        public static readonly string[] Kids = { "阿满", "小穗", "团团", "阿福", "小桃", "石生", "春儿", "青竹", "阿喜", "冬生", "暖暖", "小桥", "阿宁", "长安", "小灯", "元元", "阿棠", "竹青", "小禾", "福来", "云生", "阿圆", "平安", "归舟", "新月" };
        public static readonly string[] WishText = { "想要一道轻身符！", "想听傩鼓声！", "想要团圆印！", "想听铜铃报平安！", "想放爆竹！", "想要更鼓印！", "想用红绳结个平安结！", "想要金箔剪纸！", "想要一道护身法术！", "想要能赶走妖邪的法牌！", "想要史诗或淬炼过的法牌！", "想要一道能力法牌！" };
        public static readonly string[] StatusIds = { "might", "nimble", "soggy", "brittle", "chilled", "frostbite", "holly", "carol", "cocoa", "ribbon", "tinsel", "candles", "snowfall", "workshop", "star", "choir", "jolly", "drawnext" };
        public static readonly string[] StatusNames = { "神勇", "灵巧", "虚弱", "破绽", "封息", "寒煞", "荆符", "傩歌", "团圆汤", "护命", "金缕", "长明灯", "朔风阵", "百工", "引岁灯", "傩舞", "蓄灯", "待晓" };
        public static readonly string[] StatusDesc = { "每层使攻击伤害+1。", "每层使法牌提供的格挡+1。", "攻击伤害降低25%，每回合减少1层。", "受到的攻击伤害增加50%，每回合减少1层。", "获得的格挡降低25%，每回合减少1层。", "回合开始失去等同层数的生命，然后减少1层。", "受到攻击时反伤等同层数。", "回合结束获得等同层数的神勇。", "回合开始恢复等同层数的生命。", "每层抵挡下一次减益。", "回合结束获得等同层数的格挡。", "回合结束对所有敌人造成等同层数的伤害。", "回合开始对所有敌人施加等同层数的寒煞。", "每回合额外抽等同层数的法牌。", "每回合额外获得等同层数的灯火。", "每回合获得等同层数的神勇。", "下回合额外获得灯火。", "下回合额外抽牌。" };
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
