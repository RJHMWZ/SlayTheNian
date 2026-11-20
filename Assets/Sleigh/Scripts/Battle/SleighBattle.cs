using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sleigh
{
    public class Unit
    {
        public string Id; public EnemyDef Def;
        public int Hp, MaxHp, Block, Turn, Seed, Pips, PipMax, Mood, Loot, Slot;
        public float X;
        public bool IsPlayer, Dead, Charmed, Soft, SoftHit, SkipTurn, JustSpawned, Pending;
        public string Love, Hate, Move;
        public Dictionary<string, int> Status = new Dictionary<string, int>();
        public List<string> History = new List<string>();
        public int S(string key) => Status.TryGetValue(key, out int n) ? n : 0;
        public void Set(string key, int n) { if (n <= 0) Status.Remove(key); else Status[key] = n; }
        public string Name => IsPlayer ? DB.CharacterName(Id) : Def.name;
        public Unit Snapshot()
        { var copy = (Unit)MemberwiseClone(); copy.Status = new Dictionary<string, int>(Status); return copy; }
    }
    public class BattleCue
    {
        public string kind, text; public Unit unit, target; public int n;
        public Unit frame, targetFrame;
        public Gift gift;
        public int playerHp, playerMaxHp, energy, power, cookies, spirit, drawCount, discardCount, consumedCount;
        public BattleCue(string kind, Unit unit = null, int n = 0, string text = null, Unit target = null)
        { this.kind = kind; this.unit = unit; this.n = n; this.text = text; this.target = target; }
    }
    public class Battle
    {
        public readonly Run Run;
        public Unit Player;
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Gift> Hand = new List<Gift>(), DrawPile = new List<Gift>(), Discard = new List<Gift>(), Consumed = new List<Gift>();
        public readonly List<BattleCue> Cues = new List<BattleCue>();
        public readonly List<string> FriendsWon = new List<string>();
        public string Kind;
        public int Turn, Energy, MaxEnergy = 3, Power = 40, Attacks, Skills, Played, AidIndex;
        public string[] AidOrder;
        public bool PlayerPhase, Ended, Won, Escaped, Transformed, SecretFight, PocketBonus, DoubleNext;
        float hpMultiplier = 1;
        bool checking;
        public Battle(Run run, string[] ids, string kind)
        {
            Run = run; Kind = kind;
            Player = new Unit { IsPlayer = true, Id = run.character, Hp = run.hp, MaxHp = run.maxHp, X = 150 };
            AidOrder = DB.ZodiacAid.ToArray(); run.Shuffle(AidOrder);
            hpMultiplier = ids.Length >= 3 && kind != "boss" ? kind == "elite" ? .85f : .8f : 1;
            float[] slots = ids.Length == 1 ? new float[] { 452 } : ids.Length == 2 ? new float[] { 404, 514 } : new float[] { 372, 460, 548 };
            for (int i = 0; i < ids.Length; i++) Spawn(ids[i], slots[Mathf.Min(i, 2)]);
            DrawPile.AddRange(run.bag.Select(x => x.Copy())); run.Shuffle(DrawPile);
            Cue("banner", null, 0, kind == "boss" ? "CHRISTMAS EVE" : kind == "elite" ? "ELITE!" : "NIGHT " + (run.night + 1));
            foreach (var u in Foes().GroupBy(x => x.Id).Select(x => x.First())) Cue("say", u, 0, run.Pick(u.Def.lines));
            Relics("battleStart"); foreach (var e in Foes()) ChooseIntent(e); StartTurn();
        }
        public void Cue(string kind, Unit u = null, int n = 0, string text = null, Unit target = null, Gift gift = null)
        {
            Cues.Add(new BattleCue(kind, u, n, text, target) { frame = u?.Snapshot(), targetFrame = target?.Snapshot(), gift = gift?.Copy(),
                playerHp = Player?.Hp ?? Run.hp, playerMaxHp = Player?.MaxHp ?? Run.maxHp, energy = Energy, power = Power, cookies = Run.cookies, spirit = Run.spirit,
                drawCount = DrawPile.Count, discardCount = Discard.Count, consumedCount = Consumed.Count });
        }
        public List<Unit> Foes() => Units.Where(x => !x.Dead && !x.Charmed).ToList();
        public List<Unit> Allies() => Units.Where(x => !x.Dead && x.Charmed).ToList();
        public Unit RandomFoe() => Run.Pick(Foes().Where(x => !x.Pending).ToList());
        public Unit Spawn(string id, float x, bool summoned = false)
        {
            var d = DB.Enemies[id]; float tough = (Run.Adv(1) && d.tier == "elite" ? 1.1f : 1) * (Run.Adv(2) ? 1.1f : 1) * (Run.Adv(10) && d.tier == "boss" ? 1.2f : 1);
            int hp = Round(Run.Int(d.hp[0], d.hp[1]) * (summoned ? 1 : hpMultiplier) * tough);
            var u = new Unit { Id = id, Def = d, Hp = hp, MaxHp = hp, Seed = Run.Int(0, 1), X = x, Love = d.love, Hate = d.hate, JustSpawned = summoned };
            u.PipMax = d.noCharm ? 0 : d.pips + (Run.Adv(6) ? 1 : 0); u.Pips = u.PipMax;
            if (id == "alpha") u.Set("ribbon", 2); if (id == "jackfrost") u.Set("holly", 2);
            Units.Add(u); Ledger.Note(id, "seen"); Cue("spawn", u);
            return u;
        }
        public static int Round(float n) => Mathf.FloorToInt(n + .5f);
        public static int Round(double n) => (int)Math.Floor(n + .5);
        public int DamageCalc(Unit src, Unit tgt, int n)
        {
            float d = n + src.S("might"); if (src.S("soggy") > 0) d *= .75f;
            if (tgt != null && tgt.S("brittle") > 0) d *= 1.5f; if (tgt != null && tgt.Soft) d *= 1.5f;
            if (src.IsPlayer && DoubleNext) d *= 2;
            if (!src.IsPlayer && !src.Charmed && Run.Adv(9)) d *= 1.1f;
            return Mathf.Max(0, Mathf.FloorToInt(d));
        }
        public int BlockCalc(Unit u, int n, bool raw = false)
        { float b = n + (raw ? 0 : u.S("nimble")); if (!raw && u.S("chilled") > 0) b *= .75f; return Mathf.Max(0, Mathf.FloorToInt(b)); }
        public void Block(Unit u, int n, bool raw = false)
        { if (u == null || u.Dead) return; int b = BlockCalc(u, n, raw); if (b <= 0) return; u.Block += b; Cue("block", u, b); }
        public void Apply(Unit u, string key, int n)
        {
            if (u == null || u.Dead || n == 0 || !DB.StatusIds.Contains(key)) return;
            if (DB.Debuff(key) && n > 0 && u.S("ribbon") > 0) { u.Set("ribbon", u.S("ribbon") - 1); Cue("buff", u, 0, "RIBBON!"); return; }
            u.Set(key, u.S(key) + n); Cue(DB.Debuff(key) ? "debuff" : "buff", u, n, DB.StatusNames[Array.IndexOf(DB.StatusIds, key)]);
        }
        public void ApplyAll(string key, int n) { foreach (var f in Foes()) Apply(f, key, n); }
        public void Heal(Unit u, int n)
        { if (u.Dead) return; int h = Mathf.Min(n, u.MaxHp - u.Hp); if (h <= 0) return; u.Hp += h; Sync(); Cue("heal", u, h); }
        public void GainMaxHP(int n) { Player.MaxHp += n; Player.Hp += n; Sync(); Cue("heal", Player, n, "MAX HP"); }
        public void GainEnergy(int n) { Energy += n; Cue("energy", Player, n); }
        public void Sync() { Run.hp = Mathf.Max(0, Player.Hp); Run.maxHp = Player.MaxHp; }
        public void Charge(string kind, int n = 1)
        {
            int gain = Run.character == "mrs" ? kind == "pip" ? 15 : kind == "skl" ? 10 : 0
                : Run.character == "tinsel" ? kind == "gift" ? 8 : kind == "part" ? 6 : 0
                : kind == "hurt" ? 3 : kind == "blocked" ? 1 : 0;
            int was = Power; Power = Mathf.Min(100, Power + gain * n);
            if (was < 100 && Power >= 100) Cue("ready", Player, 0, "POWER READY!");
        }
        void SoftHurt(Unit u) { if (u.Soft && !u.SoftHit && PlayerPhase) { u.SoftHit = true; Cue("say", u, 0, "IT WON'T JOIN NOW"); } }
        public void Damage(Unit u, int n, bool direct = false)
        {
            if (u == null || u.Dead || u.Pending || Ended) return;
            SoftHurt(u);
            if (n <= 0) { Cue("hit", u, 0); return; }
            int blocked = direct ? 0 : Mathf.Min(u.Block, n), loss = n - blocked;
            int oldBlock = u.Block; u.Block -= blocked; u.Hp -= loss;
            if (u.IsPlayer) { Charge("hurt", loss); Charge("blocked", blocked); }
            Cue(loss > 0 ? "hit" : "blocked", u, loss, blocked > 0 ? blocked + " WRAP" : null);
            if (blocked >= 4 && oldBlock > 0 && u.Block == 0) Cue("wrapBreak", u, blocked, "WRAP BROKEN!");
            Sync(); if (u.Hp <= 0) Kill(u);
        }
        void Kill(Unit u)
        {
            if (u.IsPlayer)
            {
                var a = Run.Relic("treeangel");
                if (a != null && !a.used) { a.used = true; u.Hp = Round(u.MaxHp * .5f); Sync(); Cue("heal", u, u.Hp, "TREE ANGEL!"); return; }
                Ended = true; Won = false; Run.hp = 0; Cue("defeat", u, 0, "CHRISTMAS FADES..."); return;
            }
            if (u.Id == "santa" && !Transformed && !u.Charmed) { u.Hp = 0; u.Pending = true; return; }
            if (u.Dead) return; u.Dead = true; u.Hp = 0; Cue("death", u);
            if (u.Charmed) return;
            Run.stats.kills++; Ledger.Note(u.Id, "kills");
            if (u.Loot > 0) { Run.cookies += u.Loot; Cue("cookies", u, u.Loot); u.Loot = 0; }
            Relics("enemyDie");
        }
        public void Strike(Unit src, Unit tgt, int n, bool thorns = true)
        {
            if (src == null || src.Dead || tgt == null || tgt.Dead || tgt.Pending || Ended) return;
            Cue("attack", src, 0, null, tgt); Damage(tgt, DamageCalc(src, tgt, n));
            if (thorns && tgt.S("holly") > 0 && !src.Dead && src.Hp > 0) Damage(src, tgt.S("holly"));
        }
        public void Attack(Unit target, int n, int hits = 1, bool random = false, bool thorns = true)
        {
            for (int i = 0; i < hits; i++)
            { if (random || target == null || target.Dead || target.Pending || target.Charmed) target = RandomFoe(); if (target == null || Ended) return; Strike(Player, target, n, thorns); }
        }
        public void AttackAll(int n, int hits = 1)
        { for (int i = 0; i < hits && !Ended; i++) foreach (var f in Foes().Where(x => !x.Pending)) Strike(Player, f, n); }
        public void Draw(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (Hand.Count >= 10) { Cue("say", Player, 0, "HAND FULL"); break; }
                if (DrawPile.Count == 0) { if (Discard.Count == 0) break; DrawPile.AddRange(Discard); Discard.Clear(); Run.Shuffle(DrawPile); Cue("shuffle"); }
                var g = DrawPile[DrawPile.Count - 1]; DrawPile.RemoveAt(DrawPile.Count - 1); Hand.Add(g); Cue("draw", Player, gift: g);
            }
        }
        public Gift AddToHand(string id, bool free = false)
        { var g = new Gift { Id = id, Uid = -Run.uid++, FreeThisTurn = free }; bool full = Hand.Count >= 10; if (full) Discard.Add(g); else Hand.Add(g); Cue(full ? "addDiscard" : "draw", Player, gift: g); return g; }
        void Decay(Unit u) { foreach (var key in new[] { "soggy", "brittle", "chilled" }) u.Set(key, u.S(key) - 1); }
        public void StartTurn()
        {
            if (Ended) return; Turn++; PlayerPhase = true; Attacks = Skills = Played = 0;
            Player.Block = Run.Has("snowshovel") ? Mathf.Min(Player.Block, 15) : 0;
            if (Player.S("frostbite") > 0) { Damage(Player, Player.S("frostbite"), true); Player.Set("frostbite", Player.S("frostbite") - 1); if (Ended) return; }
            if (Player.S("cocoa") > 0) Heal(Player, Player.S("cocoa"));
            if (Player.S("choir") > 0) Apply(Player, "might", Player.S("choir"));
            if (Player.S("snowfall") > 0) ApplyAll("frostbite", Player.S("snowfall"));
            MaxEnergy = 3 + (Run.Has("treestar") ? 1 : 0);
            Energy = MaxEnergy + Player.S("star") + Player.S("jolly");
            int draw = 5 + Player.S("drawnext") + Player.S("workshop") + (PocketBonus ? 3 : 0);
            PocketBonus = false; Player.Set("jolly", 0); Player.Set("drawnext", 0);
            if (Run.SpiritTier == 4 || Run.SpiritTier == 3 && Turn == 1) Energy++;
            if (Run.SpiritTier == 0) draw--;
            Cue("turn", Player, 0, "YOUR TURN " + Turn); Relics("turnStart"); Draw(draw); Check();
        }
        public bool Playable(Gift gi) => !Ended && PlayerPhase && Hand.Contains(gi) && !gi.Def.flags.Contains("u") && gi.Cost <= Energy;
        public bool Play(Gift gi, Unit t)
        {
            if (!Playable(gi) || gi.Def.target == "e" && (t == null || t.Charmed || t.Dead || t.Pending)) return false;
            Energy -= gi.Cost; Hand.Remove(gi); Played++;
            if (gi.Def.type == "atk") Attacks++; if (gi.Def.type == "skl") { Skills++; Charge("skl"); }
            Charge("gift"); if (!string.IsNullOrEmpty(gi.ToyPart)) Charge("part");
            Cue("gift", Player, 0, gi.Name, t, gi); Relics("prePlay", gi.Def);
            ResolveGift(gi, t); PartEffect(gi, t); PipEffect(gi, t); DoubleNext = false;
            Relics("play", gi.Def); gi.FreeThisTurn = false;
            if (gi.Def.type == "pow") Cue("powerGift", Player, text: gi.Name, gift: gi);
            else if (gi.Def.flags.Contains("x") || gi.ToyPart == "battery") { Consumed.Add(gi); Cue("consume", Player, text: gi.Name, gift: gi); }
            else { Discard.Add(gi); Cue("discard", Player, gift: gi); }
            Check(); Sync(); return true;
        }
        void ResolveGift(Gift g, Unit t)
        {
            var v = g.Values(); int d = v.TryGetValue("d", out var dv) ? dv : 0, b = v.TryGetValue("b", out var bv) ? bv : 0, n = v.TryGetValue("n", out var nv) ? nv : 0;
            if (g.Def.type == "friend")
            {
                Cue("friend", Player, 0, g.Def.enemy);
                foreach (var s in g.Def.steps)
                {
                    if (Ended) break; int a = v[s.key];
                    var target = t != null && !t.Dead && !t.Charmed && !t.Pending ? t : RandomFoe();
                    switch (s.op)
                    { case "atk": Attack(target, a, s.hits, false, false); break; case "def": Block(Player, a); break;
                        case "buff": Apply(Player, s.status, a); break; case "debuff": Apply(target, s.status, a); break;
                        case "heal": Heal(Player, a); break; case "cookies": Run.cookies += a; Cue("cookies", Player, a); break; }
                }
                Cue("guestExit", Player);
                return;
            }
            switch (g.Id)
            {
                case "sock": case "chimney": case "midnight": Attack(t, d); break;
                case "mitten": case "scarf": case "snowfort": case "bigbow": Block(Player, b); break;
                case "snowball": Attack(t, d); Apply(t, "brittle", n); break;
                case "canejab": Attack(t, d); Draw(n); break;
                case "icicle": Attack(t, d); Apply(t, "frostbite", n); break;
                case "baubles": Attack(null, d, 3, true); break;
                case "sledsweep": AttackAll(d); break;
                case "nutchomp": Attack(t, d); Apply(t, "soggy", n); break;
                case "tinselwhip": Block(Player, b); Attack(t, d); break;
                case "bellslam": bool brittle = t != null && t.S("brittle") > 0; Attack(t, d); if (brittle) { GainEnergy(1); Draw(1); } break;
                case "crumbs": Attack(t, d); Draw(1); break;
                case "cocoasip": Block(Player, b); Draw(1); break;
                case "offkey": ApplyAll("soggy", n); break;
                case "wrapspree": Block(Player, b); Apply(Player, "drawnext", 1); break;
                case "stuffer": Draw(n); break;
                case "eggshot": GainEnergy(n); break;
                case "frostbreath": Apply(t, "frostbite", n); break;
                case "stampede": AttackAll(d, 3); break;
                case "kiss": Attack(t, d); Heal(Player, n); break;
                case "bellybump": Attack(t, Player.Block); break;
                case "flurry": Attack(t, d); if (t != null && !t.Dead && t.S("frostbite") > 0) Apply(t, "frostbite", t.S("frostbite")); break;
                case "jingle": Apply(Player, "jolly", 2); Apply(Player, "drawnext", n); break;
                case "doublewrap": Block(Player, Player.Block, true); break;
                case "globe": Block(Player, b); ApplyAll("chilled", n); break;
                case "fireplace": Heal(Player, n); break;
                case "checktwice": Apply(t, "soggy", n); Apply(t, "brittle", n); break;
                case "elfhelp": AddToHand(Run.RolledGift(), true); break;
                case "spiked": Apply(Player, "might", n); break;
                case "tinselarmor": Apply(Player, "tinsel", n); break;
                case "hollywreath": Apply(Player, "holly", n); break;
                case "candles": Apply(Player, "candles", n); break;
                case "snowfall": Apply(Player, "snowfall", n); break;
                case "rhythm": Apply(Player, "workshop", 1); break;
                case "starfall": AttackAll(d); ApplyAll("brittle", n); break;
                case "birch": Attack(t, d); if (t != null && t.Dead) GainMaxHP(n); break;
                case "miracle": GainEnergy(n); Draw(2); break;
                case "milkcookies": Apply(Player, "cocoa", n); break;
                case "guidingstar": Apply(Player, "star", 1); break;
                case "choir": Apply(Player, "choir", n); break;
                case "coal": case "soot": case "naughty": break;
                default: throw new InvalidOperationException("Unmigrated gift: " + g.Id);
            }
        }
        void PartEffect(Gift g, Unit t)
        {
            var f = t != null && !t.IsPlayer && !t.Dead && !t.Charmed && !t.Pending ? t : RandomFoe();
            switch (g.ToyPart)
            { case "squeaker": Apply(f, "soggy", 1); break; case "glitter": Apply(f, "brittle", 1); break;
                case "bell": Draw(1); break; case "spring": Damage(RandomFoe(), 4); break;
                case "musicbox": Heal(Player, 2); break; case "windup": Apply(Player, "jolly", 1); break; }
        }
        static bool Matches(string like, Gift g)
        {
            if (string.IsNullOrEmpty(like)) return false; if (like == "rare") return g.Def.rarity == 3;
            var a = like.Split(':'); return a[0] == "part" ? g.ToyPart == a[1] : g.Def.type == a[1];
        }
        void PipEffect(Gift g, Unit t)
        {
            if (g.Def.type == "curse") return;
            bool aimed = g.Def.target == "e" && t != null && !t.Dead && !t.Charmed;
            var pool = Foes().Where(x => x.PipMax > 0 && !x.Pending).ToList();
            Unit ribbon = !aimed && g.ToyPart == "ribbon" ? Run.Pick(pool.Where(x => x.Pips > 0).ToList()) : null;
            foreach (var f in pool)
            {
                bool love = Matches(f.Love, g), hate = !love && Matches(f.Hate, g);
                if (aimed && f != t) { if (hate) ChangePips(f, -1, false, true); continue; }
                int n = (love ? 2 : 0) - (hate ? 1 : 0) + (g.ToyPart == "ribbon" && (aimed || f == ribbon) ? 1 : 0);
                if (n != 0 || love || hate) ChangePips(f, n, love, hate);
            }
        }
        public void ChangePips(Unit u, int n, bool love = false, bool hate = false)
        {
            if (u == null || u.PipMax == 0 || u.Dead || u.Pending || u.Charmed) return;
            if (love) { Ledger.Note(u.Id, "love"); Cue("love", u, 0, "LOVES IT!"); }
            if (hate) { Ledger.Note(u.Id, "hate"); Cue("hate", u, 0, "HATES IT!"); }
            int before = u.Pips; u.Pips = Mathf.Clamp(u.Pips - n, 0, u.PipMax);
            if (u.Pips < before) { Charge("pip", before - u.Pips); Cue("pips", u, before - u.Pips, "GRUMPY"); }
            if (u.Pips > before && u.Soft) { u.Soft = u.SoftHit = u.SkipTurn = false; Cue("say", u, 0, "GRUMPY AGAIN!"); }
            if (u.Pips == 0 && !u.Soft) { u.Soft = true; u.SoftHit = false; u.SkipTurn = true; Cue("soft", u, 0, "SOFTENED! Leave it be to win it over."); }
        }
        void Charm(Unit u)
        {
            if (u.Charmed || u.Dead) return;
            u.Charmed = true; u.Pips = 0; u.Soft = u.SoftHit = u.SkipTurn = false; u.Status.Clear(); u.Block = 0;
            Ledger.Note(u.Id, "charms"); Ledger.Note(u.Id, "love"); Ledger.Note(u.Id, "hate"); Run.stats.charmed++;
            Run.cookies += u.Loot; u.Loot = 0;
            if (u.Id != "santa")
            { if (!Run.charmed.Contains(u.Id)) Run.charmed.Add(u.Id); FriendsWon.Add(u.Id); Run.AddSpirit(3); }
            u.Slot = Enumerable.Range(0, 3).FirstOrDefault(i => !Allies().Any(a => a != u && a.Slot == i));
            u.X = u.Id == "santa" ? 80 : new float[] { 94, 52, 73 }[u.Slot]; Cue("charm", u, 0, "WON OVER!");
            if (u.Id == "santa") SecretBoss();
            else
            {
                ChooseIntent(u);
                foreach (var other in Foes().Where(x => x.Id == u.Id && x.PipMax > 0 && !x.Soft)) ChangePips(other, 1);
            }
        }
        void SecretBoss()
        {
            Transformed = SecretFight = true;
            Cue("banner", null, 0, "The LIST... it was the list all along!");
            var list = Spawn("naughtylist", 452); ChooseIntent(list);
            var army = Run.charmed.Where(x => x != "santa" && DB.Enemies.ContainsKey(x)).ToList();
            foreach (var id in army.Skip(Mathf.Max(0, army.Count - 8))) { Cue("friend", Player, 0, id); Cue("attack", Player, target: list); Damage(list, 6); Cue("guestExit", Player); }
            Cue("banner", list, 0, "THE NAUGHTY LIST");
        }
        void Transform(Unit u)
        {
            Transformed = true; u.Pending = false;
            var d = DB.Enemies["krampus"]; var tier = Run.SpiritTier;
            int hp = Round(d.hp[0] * new[] { 1.15f, 1.08f, 1, .9f, .8f }[tier] * (Run.Adv(10) ? 1.2f : 1));
            u.Id = d.id; u.Def = d; u.Hp = u.MaxHp = hp; u.Block = 0; u.Status.Clear(); u.Turn = 0; u.History.Clear(); u.Dead = false;
            u.SkipTurn = !PlayerPhase; u.Pips = u.PipMax = 0; u.Soft = u.SoftHit = false; u.Love = u.Hate = null;
            if (tier == 4) { u.Set("brittle", 2); u.Set("soggy", 1); } if (tier == 3) u.Set("brittle", 1);
            if (tier == 1) u.Set("might", 1); if (tier == 0) { u.Set("might", 3); u.Set("carol", 1); }
            if (Run.Adv(10)) u.Set("might", u.S("might") + 1);
            ChooseIntent(u); Cue("transform", u, 0, "KRAMPUS / Spirit: " + Run.SpiritName);
        }
        public void Check()
        {
            if (Ended || checking) return; checking = true;
            try
            {
                var foes = Foes().Where(x => !x.Pending).ToList();
                if (PlayerPhase && foes.Count == 1 && foes[0].Soft && !foes[0].SoftHit) Charm(foes[0]);
                foreach (var u in Foes().Where(x => x.Pending).ToList()) Transform(u);
                if (Foes().Count == 0)
                {
                    Won = Ended = true; Relics("battleEnd"); Sync();
                    if (Kind == "elite") Run.stats.elites++; if (Kind == "boss") Run.secret = SecretFight;
                    Cue("victory", Player, 0, Kind == "boss" ? "CHRISTMAS IS SAVED!" : "VICTORY!");
                }
            }
            finally { checking = false; }
        }
        public void EndTurn()
        {
            if (Ended || !PlayerPhase) return; PlayerPhase = false;
            foreach (var f in Foes())
            {
                if (!f.Soft) continue;
                if (!f.SoftHit) Charm(f);
                else { f.Soft = f.SoftHit = false; f.Pips = f.PipMax; Cue("say", f, 0, "GRUMPY AGAIN!"); }
            }
            Check(); if (Ended) return;
            if (Player.S("candles") > 0) foreach (var f in Foes()) Damage(f, Player.S("candles"));
            if (Player.S("tinsel") > 0) Block(Player, Player.S("tinsel"), true);
            Relics("turnEnd"); foreach (var g in Hand) if (g.Id == "naughty") Damage(Player, 2, true);
            Check(); if (Ended) return;
            foreach (var g in Hand) { g.FreeThisTurn = false; if (g.Def.flags.Contains("m")) Consumed.Add(g); else Discard.Add(g); Cue(g.Def.flags.Contains("m") ? "melt" : "handDiscard", Player, gift: g); }
            Hand.Clear(); Decay(Player);
            foreach (var a in Allies())
            {
                if (Ended || Foes().Count == 0) break;
                ExecuteMove(a, true); a.Turn++; ChooseIntent(a); Check();
            }
            if (Ended) return; Cue("turn", null, 0, "ENEMY TURN");
            foreach (var e in Foes())
            {
                if (Ended) return; if (e.Dead || e.Charmed) continue;
                e.Block = 0;
                if (e.S("frostbite") > 0) { Damage(e, e.S("frostbite"), true); e.Set("frostbite", e.S("frostbite") - 1); Check(); if (Ended) return; if (e.Dead) continue; }
                if (e.JustSpawned || e.SkipTurn) { e.JustSpawned = e.SkipTurn = false; continue; }
                ExecuteMove(e, false); Check(); if (Ended) return; if (e.Dead) continue;
                if (e.S("carol") > 0) Apply(e, "might", e.S("carol"));
                if (e.S("tinsel") > 0) Block(e, e.S("tinsel"), true); Decay(e);
            }
            foreach (var e in Foes())
            {
                e.Turn++; ChooseIntent(e);
                if (e.Id == "santa" && !e.Soft && !e.Dead)
                { e.Mood++; string[] love = { "part:ribbon", "part:musicbox", "part:bell", "rare" }, hate = { "part:battery", "part:spring", "part:squeaker", "part:wheels" }; e.Love = love[e.Mood % 4]; e.Hate = hate[e.Mood % 4]; Cue("say", e, 0, "NOW WANTS: " + DB.LikeLabel(e.Love)); }
            }
            StartTurn();
        }
        string Weighted(Unit u, params Weight[] weights)
        {
            var w = weights.Length > 0 ? weights : u.Def.weights;
            string ban = u.History.Count >= 2 && u.History[u.History.Count - 1] == u.History[u.History.Count - 2] ? u.History[u.History.Count - 1] : null;
            var pool = w.Where(x => x.key != ban).ToArray(); double r = Run.Random() * pool.Sum(x => x.n);
            foreach (var x in pool) { r -= x.n; if (r <= 0) return x.key; } return pool[0].key;
        }
        public void ChooseIntent(Unit e)
        {
            string key; string last = e.History.LastOrDefault();
            switch (e.Id)
            {
                case "snowman": key = (e.Turn + e.Seed) % 2 != 0 ? "glare" : "ball"; break;
                case "fruitcake": key = e.Turn == 0 ? "slam" : Weighted(e); break;
                case "creeper": key = e.Turn == 0 ? "entangle" : Weighted(e); break;
                case "angel": key = Foes().Any(f => f.Hp < f.MaxHp - 8) && last != "hymn" && Run.Random() < .4 ? "hymn" : Weighted(e); break;
                case "rogueelf": key = e.Loot > 0 && last == "pickpocket" ? "lurk" : e.Loot > 0 && last == "lurk" ? "flee" : "pickpocket"; break;
                case "yeti": key = e.Turn == 0 ? "roar" : new[] { "maul", "avalanche", "maul", "roar" }[(e.Turn - 1) % 4]; break;
                case "alpha": key = e.Turn == 0 ? "bellow" : new[] { "gore", "stampede", "gore", "bellow" }[e.Turn % 4]; break;
                case "witch": key = e.Turn % 3 == 2 && Foes().Count < 3 ? "bake" : Weighted(e); break;
                default: key = e.Def.weights.Length > 0 ? Weighted(e) : e.Def.sequence[e.Turn % e.Def.sequence.Length]; break;
            }
            e.Move = key; e.History.Add(key); Cue("intent", e);
        }
        void ExecuteMove(Unit e, bool ally)
        {
            if (string.IsNullOrEmpty(e.Move)) ChooseIntent(e);
            var move = e.Def.Move(e.Move); Cue("move", e, 0, move.name);
            foreach (var a in move.actions)
            {
                if (Ended || e.Dead) return; Unit f = RandomFoe();
                if (ally)
                {
                    switch (a.op)
                    { case "atk": for (int i = 0; i < a.hits; i++) Strike(e, RandomFoe(), a.n); break;
                        case "def": Block(Player, a.n); break; case "buff": case "buffAll": Apply(e, a.key, a.n); break;
                        case "debuff": Apply(f, a.key, a.n); break; case "heal": case "healAll": Heal(Player, Mathf.CeilToInt(a.n / 2f)); break;
                        case "steal": case "pocket": int coins = Mathf.Min(15, a.n > 0 ? a.n : 10); Run.cookies += coins; Cue("cookies", e, coins); break; }
                    Check(); continue;
                }
                switch (a.op)
                {
                    case "atk": for (int i = 0; i < a.hits && !Ended && !e.Dead; i++) Strike(e, Player, a.n); break;
                    case "def": Block(e, a.n); break;
                    case "buff": Apply(e, a.key, a.n); break;
                    case "buffAll": foreach (var u in Foes()) Apply(u, a.key, a.n); break;
                    case "debuff": Apply(Player, a.key, a.n); break;
                    case "heal": Heal(e, a.n); break;
                    case "healAll": foreach (var u in Foes()) Heal(u, a.n); break;
                    case "add":
                        for (int i = 0; i < a.n; i++)
                        { if (a.pile == "bag") Run.AddGift(a.key); else { var g = new Gift { Id = a.key, Uid = -Run.uid++ }; if (a.pile == "draw") DrawPile.Add(g); else Discard.Add(g); } }
                        if (a.pile == "draw") Run.Shuffle(DrawPile); Cue("say", Player, a.n, DB.Gifts[a.key].name); break;
                    case "summon":
                        if (Foes().Count < 3) { float[] slots = { 372, 460, 548 }; float x = slots.FirstOrDefault(s => !Foes().Any(u => Mathf.Abs(u.X - s) < 35)); var u = Spawn(a.key, x == 0 ? 548 : x, true); ChooseIntent(u); } break;
                    case "steal": case "pocket": int n = Mathf.Min(Run.cookies, a.n); Run.cookies -= n; if (a.op == "pocket") e.Loot += n; Cue("cookies", e, -n); break;
                    case "flee": e.Dead = true; Cue("flee", e, e.Loot, "ESCAPED!"); return;
                    default: throw new InvalidOperationException("Unmigrated move: " + a.op);
                }
            }
        }
        public bool UsePower(Unit target = null, string part = null, Gift gift = null)
        {
            if (Ended || !PlayerPhase || Power < 100) return false;
            if (Run.character == "mrs")
            {
                if (target == null || target.Dead || target.Charmed || target.Pending) return false;
                Power = 0; Cue("blessing", Player, 0, "KIND WORD");
                if (target.PipMax > 0)
                { Ledger.Note(target.Id, "love"); Cue("say", target, 0, "LOVES " + DB.LikeLabel(target.Love)); if (!target.Soft) { int pw = Power; ChangePips(target, 1); Power = pw; } }
                Apply(target, Run.Pick(new[] { "soggy", "brittle", "chilled", "frostbite" }), target.PipMax > 0 ? 1 : 2);
            }
            else if (Run.character == "tinsel")
            {
                if (gift == null || !Hand.Contains(gift) || gift.Def.type == "curse" || !DB.Parts.ContainsKey(part ?? "")) return false;
                Power = 0; gift.ToyPart = part; var bag = Run.bag.Find(x => x.Uid == gift.Uid); if (bag != null) bag.ToyPart = part;
                Cue("cast", Player, 0, "TINKER: " + DB.Parts[part].name);
            }
            else
            {
                Power = 0; string guardian = AidOrder[AidIndex++ % AidOrder.Length]; Cue("zodiac", Player, 0, guardian);
                var f = RandomFoe();
                switch (guardian)
                {
                    case "rat": Draw(2); GainEnergy(1); break; case "coalgolem": Block(Player, 12); break; case "yeti": Apply(Player, "might", 2); break;
                    case "alpha": Apply(f, "soggy", 2); Apply(f, "brittle", 2); break; case "viper": Attack(f, 20, 1, false, false); break;
                    case "penguin": var pool = Foes().Where(x => x.PipMax > 0 && !x.Soft && !x.Pending).ToList(); if (pool.Count > 0) ChangePips(Run.Pick(pool), 2); else Attack(f, 6, 1, false, false); break;
                    case "grumpkin": foreach (var u in Foes()) Damage(u, DamageCalc(Player, u, 8)); break;
                    case "rogueelf": Attack(null, 4, 5, true, false); break;
                    case "toysoldier": Draw(2); break;
                    case "creeper": Heal(Player, 6); break;
                    case "angel": Block(Player, 8); GainEnergy(1); break;
                    case "slime": Apply(Player, "nimble", 2); break;
                }
                Cue("guestExit", Player);
            }
            Check(); return true;
        }
        public bool UsePotion(int slot, Unit target = null)
        {
            if (Ended || !PlayerPhase || slot < 0 || slot >= Run.potionSlots) return false;
            string id = Run.potions[slot]; if (string.IsNullOrEmpty(id)) return false;
            if (id == "smoke" && Kind != "normal") return false;
            var d = DB.Potions[id]; if (d.target == "e" && (target == null || target.Dead || target.Charmed || target.Pending)) return false;
            Run.potions[slot] = null; Cue("potion", Player, 0, d.name);
            switch (id)
            {
                case "hotcocoa": Heal(Player, 20); break; case "eggnog": Apply(Player, "might", 2); break; case "cider": Apply(Player, "nimble", 2); break;
                case "peppermint": GainEnergy(2); break; case "snowflask": Block(Player, 12, true); break;
                case "cracker": Damage(target, 20); break; case "blizzard": foreach (var f in Foes()) Damage(f, 10); break;
                case "glitter": Apply(target, "brittle", 3); break; case "milk": ApplyAll("soggy", 2); break; case "frostvial": Apply(target, "frostbite", 8); break;
                case "dough": Draw(3); break; case "elfdust": AddToHand(Run.RandomGift(3), true); break; case "deermilk": GainMaxHP(5); break;
                case "punch": Apply(Player, "holly", 3); break; case "starlight": Apply(Player, "ribbon", 2); break;
                case "smoke": Ended = Escaped = true; Cue("flee", Player, 0, "ESCAPED!"); break;
                case "ginger": foreach (var g in Hand.Where(x => x.CanUpgrade)) g.Upgrade = true; Cue("cast", Player, 0, "REWRAPPED!"); break;
                case "wishbone": Heal(Player, Round(Player.MaxHp * .3f)); break; case "wax": Apply(Player, "tinsel", 4); break;
                case "elixir": Run.AddSpirit(10); break;
                default: throw new InvalidOperationException("Unmigrated potion " + id);
            }
            Check(); Sync(); return true;
        }
        void Relics(string hook, GiftDef gift = null)
        {
            foreach (var r in Run.relics)
            {
                if (hook == "battleStart")
                {
                    switch (r.id)
                    { case "toolbelt": case "iceskates": Apply(Player, "nimble", 1); break; case "rednose": Cue("firecracker", Player); ApplyAll("brittle", 1); break;
                        case "warmsocks": Block(Player, 10, true); break; case "snowglobe": ApplyAll("brittle", 1); break;
                        case "eggnogmug": Apply(Player, "might", 1); break; case "hollysprig": Apply(Player, "holly", 3); break; }
                }
                if (hook == "turnStart")
                { if (r.id == "jinglebell" && Turn == 1) Draw(2); if (r.id == "candybowl" && Turn == 1) GainEnergy(1); if (r.id == "reindeerbell" && Turn % 3 == 0) GainEnergy(1); }
                if (hook == "prePlay" && r.id == "nutjaw" && gift.type == "atk") { r.n++; if (r.n >= 10) { r.n = 0; DoubleNext = true; Cue("relic", Player, 0, DB.Relics[r.id].name); } }
                if (hook == "play")
                { if (r.id == "gingercharm" && gift.type == "atk" && Attacks % 3 == 0) Apply(Player, "might", 1); if (r.id == "lostmitten" && gift.type == "skl" && Skills % 3 == 0) Apply(Player, "nimble", 1); }
                if (hook == "turnEnd")
                { if (r.id == "tinselgarland" && Player.Block == 0) Block(Player, 6, true); if (r.id == "pocketwatch" && Played <= 3) PocketBonus = true; if (r.id == "yulelog") Damage(RandomFoe(), 3); }
                if (hook == "enemyDie" && r.id == "churchbell") { GainEnergy(1); Draw(1); }
                if (hook == "battleEnd") { if (r.id == "cookietin") Heal(Player, 6); if (r.id == "toolbelt") Run.cookies += 15; }
            }
        }
        public string Intent(Unit e)
        {
            if (e.Soft) return e.SoftHit ? "Softened / won't join" : "Softened / joins if unharmed";
            if (e.SkipTurn) return "Too flustered...";
            if (string.IsNullOrEmpty(e.Move)) return "?";
            var m = e.Def.Move(e.Move); var a = m.actions.FirstOrDefault(x => x.op == "atk");
            return m.name + (a != null ? "  " + DamageCalc(e, e.Charmed ? null : Player, a.n) + (a.hits > 1 ? "x" + a.hits : "") : "");
        }
    }
}
