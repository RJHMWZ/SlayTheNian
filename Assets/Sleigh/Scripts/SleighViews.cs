using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        RectTransform mapContent;
        float mapScroll;
        string[] rewardGifts, rewardParts, rewardFriends;
        string rewardRelic, rewardPotion;
        int rewardCookies;
        readonly HashSet<string> rewardsClaimed = new HashSet<string>();
        EventDef currentEvent;
        EventOutcome eventOutcome;
        class ShopItem { public string type, id; public int price; public bool sold; }
        List<ShopItem> shopItems;
        int rewrapPrice, donatePrice;
        bool shopRewrapped;
        string restMessage;

        public void ShowMap()
        {
            if (run == null) return;
            NewScreen("Map"); TiledBackground(); if (!world.GetComponent<RectMask2D>()) world.gameObject.AddComponent<RectMask2D>();
            var drag = PixelUI.Ensure<MapDrag>(world); drag.move = DragMap; drag.scale = uiScale;
            Rect(world, 0, 75, 640, 270, Color.clear, true, "Map drag surface");
            mapScroll = mapTarget = Mathf.Clamp(90 + run.night * 92 - 260, 0, 1676); mapFling = 0; mapContent = Group(world, "24 Night Map");
            mapContent.anchoredPosition = new Vector2(-mapScroll, 0);
            float nx(int i) => 90 + i * 92;
            float ny(int i, int option) => 176 + Mathf.Round(Mathf.Sin(i * .9f) * 22) + (run.map[i].options.Length > 1 ? option == 0 ? -30 : 30 : 0);
            for (int i = 0; i < 23; i++)
            {
                for (int a = 0; a < run.map[i].options.Length; a++) for (int b = 0; b < run.map[i + 1].options.Length; b++)
                {
                    Vector2 from = new Vector2(nx(i), ny(i, a)), to = new Vector2(nx(i + 1), ny(i + 1, b)); int count = Mathf.FloorToInt(Vector2.Distance(from, to) / 10);
                    bool lit = run.map[i].done && run.map[i].chosen == run.map[i].options[a] && (i + 1 == run.night || run.map[i + 1].chosen == run.map[i + 1].options[b]);
                    for (int k = 2; k < count - 1; k++) { var pos = Vector2.Lerp(from, to, k / (float)count); var dot = Sprite(mapContent, "Icon", "path_dot", pos.x, pos.y); if (dot) dot.color = lit ? Gold : Hex(0x5a6a98, .7f); }
                }
            }
            for (int i = 0; i < 24; i++)
            {
                int n = i; var night = run.map[i];
                Sprite(mapContent, "BGImage", night.lit ? "house_lit" : "house", nx(i), 318, .8f);
                Label(mapContent, nx(i) - 35, 324, 70, "DEC " + (i + 1), .72f, i == run.night ? Gold : Muted, 1);
                var house = Hot(mapContent, nx(i) - 28, 274, 56, 48, () => { if (n == run.night) { run.PickWish(); ShowItem(DB.Kids[n] + "'S WISH", DB.WishText[run.wish], "gift"); } });
                BindTip(house, DB.Kids[i] + "'s house / DEC " + (i + 1), i < run.night ? night.lit ? "A lovely gift lit up the night." : "The house stayed dark." : "A child is waiting for Christmas.");
                house.drag = e => DragMap(-e.delta.x / uiScale);
                for (int j = 0; j < night.options.Length; j++)
                {
                    string type = night.options[j]; float x = nx(i), y = ny(i, j); bool reach = i == run.night;
                    var icon = Sprite(mapContent, "Icon", "node_" + type, x, y);
                    if (icon) icon.color = night.done && night.chosen != type ? Hex(0x777786) : !reach && !night.done ? Hex(0xaaaaaf) : Color.white;
                    if (night.done && night.chosen == type) Sprite(mapContent, "Icon", "check", x + 16, y - 16);
                    var node = Hot(mapContent, x - 25, y - 25, 50, 50, () => { if (n == run.night) EnterNode(type); }); node.drag = e => DragMap(-e.delta.x / uiScale);
                    string name = type == "rest" ? "Fireside" : type == "shop" ? "Elf Shop" : type == "treasure" ? "Stocking" : type == "boss" ? "Christmas Eve" : char.ToUpperInvariant(type[0]) + type.Substring(1);
                    string desc = type == "battle" ? "Monsters lurk in the snow." : type == "elite" ? "A powerful foe guards a relic." : type == "rest" ? "Rest, rewrap a gift or write to Santa." : type == "shop" ? "Spend Cookies on gifts, relics and potions." : type == "treasure" ? "A stocking stuffed with a relic." : type == "boss" ? "Save Santa's soul." : "Something strange awaits you.";
                    BindTip(node, name + " / DEC " + (i + 1), desc + (reach ? "\nChoose one node for tonight." : ""));
                }
            }
            SleighMotion.Add(Sprite(mapContent, "Icon", "pin", nx(run.night), ny(run.night, 0) - 44).rectTransform, SleighMotion.Style.Float, 3, 4);
            Hud(); Label(ui, 60, 49, 520, (24 - run.night) + " NIGHTS UNTIL CHRISTMAS", 1, Gold, 1);
            Label(ui, 60, 66, 520, "Christmas Spirit: " + run.SpiritName, .85f, run.SpiritTier > 1 ? Green : Pink, 1);
            Label(ui, 90, 346, 460, "Drag, scroll or use arrow keys", .75f, Muted, 1);
            Button(ui, 8, 241, 27, 24, "<", () => MoveMap(-360)); Button(ui, 605, 241, 27, 24, ">", () => MoveMap(360));
        }
        void MoveMap(float delta)
        { if (ScreenName != "Map" || !mapContent || modal) return; mapTarget = Mathf.Clamp(mapTarget + delta, 0, 1676); if (!Application.isPlaying) { mapScroll = mapTarget; mapContent.anchoredPosition = new Vector2(-Mathf.Round(mapScroll), 0); } }

        RectTransform Card(Transform parent, Gift g, float x, float y, float scale = 1, Battle preview = null)
        {
            var prefab = Resources.Load<GameObject>("Sleigh/Prefabs/GiftView");
            var r = Acquire(parent, "Card", "Gift", x, y, 66, 88, prefab);
            r.name = "Gift " + g.Uid + ": " + g.Name; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.localScale = Vector3.one * scale;
            string art = g.Def.type == "friend" ? g.Def.enemy : g.Id;
            if (DB.Frame("Gift", art) == null) art = g.Def.type;
            if (prefab) { var body = r.GetComponent<Image>(); body.sprite = DB.Sprite("Gift", art); body.color = Color.white; body.raycastTarget = false; }
            else Sprite(r, "Gift", art, 0, 0, 1, false, 66, 88);
            Label(r, 3, 3, 60, g.Name, .75f, g.Upgrade ? Hex(0xc8ffb0) : White, 1, true);
            if (!g.Def.flags.Contains("u"))
            { Sprite(r, "Icon", "cost", 4, 4); Label(r, -3, -2, 14, g.Cost.ToString(), .8f, preview != null && g.Cost > preview.Energy ? Red : White, 1); }
            var values = g.Values(); string stat; string icon = null;
            if (values.TryGetValue("d", out int d)) { stat = (preview != null ? preview.DamageCalc(preview.Player, null, d) : d).ToString(); icon = "sword"; if (g.Id == "baubles" || g.Id == "stampede") stat += "x3"; }
            else if (values.TryGetValue("b", out int b)) { stat = (preview != null ? preview.BlockCalc(preview.Player, b) : b).ToString(); icon = "shield"; }
            else if (g.Id == "bellybump") { stat = preview != null ? preview.DamageCalc(preview.Player, null, preview.Player.Block).ToString() : "W"; icon = "sword"; }
            else stat = DB.TypeName(g.Def.type).ToUpperInvariant();
            if (icon != null) Sprite(r, "Icon", icon, 20, 76);
            Label(r, icon != null ? 29 : 1, 71, icon != null ? 34 : 64, stat, 1, White, icon != null ? 0 : 1);
            if (!string.IsNullOrEmpty(g.ToyPart)) Sprite(r, "Icon", "part_" + g.ToyPart, 62, 4);
            return r;
        }
        string GiftTip(Gift g, Battle c = null)
        { return DB.TypeName(g.Def.type) + " / " + (g.Def.rarity == 9 ? "Curse" : new[] { "Starter", "Common", "Uncommon", "Rare", "Friend" }[g.Def.rarity]) + "\n" + g.Description(c, hoveredUnit) + (string.IsNullOrEmpty(g.ToyPart) ? "" : "\n\n" + DB.Parts[g.ToyPart].name + ": " + DB.Parts[g.ToyPart].desc); }
        void OpenBag(IEnumerable<Gift> list, string title)
        {
            OpenGiftPicker(title, list.OrderBy(x => x.Def.type).ThenBy(x => x.Def.rarity).ThenBy(x => x.Name).ToList(),
                g => ShowGiftDetail(g, () => OpenBag(list, title)), "BACK", null, "Hover or tap a gift to inspect it.");
        }
        void ShowGiftDetail(Gift g, Action back)
        {
            var m = OpenModal(g.Name, back); Card(m, g, 90, 95, 1.7f, battle);
            Label(m, 254, 93, 300, GiftTip(g, battle), 1, White, 0, true);
            Button(m, 250, 315, 140, 24, "BACK", back);
        }
        void OpenGiftPicker(string title, IList<Gift> list, Action<Gift> pick, string cancel = "BACK", Action onCancel = null, string subtitle = null, Func<Gift, string> badge = null, Action<RectTransform> footer = null, int page = 0)
        {
            Action close = onCancel ?? CloseModal;
            var m = OpenModal(title, cancel == null ? (Action)(() => { }) : close);
            if (!string.IsNullOrEmpty(subtitle)) Label(m, 30, 39, 580, subtitle, .75f, Pink, 1, true);
            var sorted = list.ToList(); int pageCount = Mathf.Max(1, Mathf.CeilToInt(sorted.Count / 12f)); page = Mathf.Clamp(page, 0, pageCount - 1);
            for (int j = 0; j < 12 && j + page * 12 < sorted.Count; j++)
            {
                var g = sorted[j + page * 12]; float x = 47 + j % 6 * 94, y = 71 + j / 6 * 114;
                var card = Card(m, g, x, y);
                var h = Hot(card, 0, 0, 66, 88, () => { CloseModal(); pick(g); });
                BindTip(h, g.Name, GiftTip(g, ScreenName == "Combat" ? battle : null));
                if (badge != null) Label(m, x - 10, y + 91, 86, badge(g), .63f, Gold, 1, true);
            }
            if (sorted.Count == 0) Label(m, 80, 148, 480, "No gifts available.", 1, Muted, 1);
            int p = page;
            if (pageCount > 1)
            { Button(m, 19, 137, 24, 28, "<", () => OpenGiftPicker(title, list, pick, cancel, onCancel, subtitle, badge, footer, p - 1), null, page > 0);
                Button(m, 600, 137, 24, 28, ">", () => OpenGiftPicker(title, list, pick, cancel, onCancel, subtitle, badge, footer, p + 1), null, page < pageCount - 1);
                Label(m, 255, 304, 130, (page + 1) + " / " + pageCount, .7f, Muted, 1); }
            if (cancel != null) Button(m, 250, 328, 140, 22, cancel, close);
            footer?.Invoke(m);
        }
        void ChooseGifts(string[] ids, string title, Action<string> pick, Action skip = null)
        {
            var m = OpenModal(title, skip ?? CloseModal); Label(m, 30, 59, 580, "Choose one gift. Hover to read its effect.", .85f, Muted, 1);
            float spacing = ids.Length > 4 ? 88 : 100;
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i]; var g = new Gift { Id = id, Uid = -1 }; float x = 320 - (ids.Length - 1) * spacing * .5f - 33 + i * spacing;
                var c = Card(m, g, x, 130); var h = Hot(c, 0, 0, 66, 88, () => { CloseModal(); pick(id); }); BindTip(h, g.Name, GiftTip(g));
                Label(m, x - 8, 230, 82, g.Name, .7f, Gold, 1, true);
            }
            Button(m, 250, 310, 140, 24, "SKIP", () => { CloseModal(); skip?.Invoke(); });
        }
        void ChooseParts(string[] ids, Action<string> pick, Action skip = null)
        {
            var m = OpenModal("CHOOSE A TOY PART", skip ?? CloseModal);
            Label(m, 40, 50, 560, "Then choose a gift to fit it on.", .85f, Muted, 1);
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i]; var d = DB.Parts[id]; float x = 320 - ids.Length * 170 / 2 + i * 170;
                var box = PanelBox(m, x + 5, 104, 160, 158, Gold); Sprite(box, "Icon", "part_" + id, 80, 34, 2);
                Label(box, 8, 61, 144, d.name, .9f, Gold, 1); Label(box, 12, 88, 136, d.desc, .85f, White, 1, true);
                Button(box, 14, 126, 132, 22, "CHOOSE", () => { CloseModal(); pick(id); });
            }
            Button(m, 250, 313, 140, 24, "NOT NOW", () => { CloseModal(); skip?.Invoke(); });
        }
        void InstallPart(string id, Action done)
        {
            OpenGiftPicker("FIT " + DB.Parts[id].name.ToUpperInvariant(), run.bag.Where(g => g.CanPart).ToList(), g => { g.ToyPart = id; done?.Invoke(); }, "BACK", null, DB.Parts[id].desc);
        }
        void PotionMenu(int slot)
        {
            if (busy) return; string id = run.potions[slot]; if (string.IsNullOrEmpty(id)) return;
            var d = DB.Potions[id]; bool inBattle = ScreenName == "Combat";
            var m = OpenModal(d.name); Sprite(m, "Icon", "potion_" + id, 320, 98, 3); Label(m, 130, 147, 380, d.desc, 1, White, 1, true);
            Button(m, 120, 266, 120, 24, "DRINK", () =>
            {
                CloseModal();
                if (inBattle)
                { if (id == "smoke" && battle.Kind != "normal") { Toast("No running from this one!", Red); return; } if (d.target == "e") { potionAim = slot; powerAim = false; selectedGift = null; RefreshBattleUI(); } else { if (battle.UsePotion(slot)) PlayBattleCues(); } }
                else
                { if (id == "hotcocoa") run.Heal(20); if (id == "deermilk") run.GainMaxHP(5); if (id == "wishbone") run.Heal(Battle.Round(run.maxHp * .3f)); if (id == "elixir") run.AddSpirit(10); run.potions[slot] = null; sound.Play("potion"); RefreshCurrentScreen(); }
            }, Hex(0x1c5a3a), inBattle || d.outside);
            Button(m, 260, 266, 120, 24, "DISCARD", () => { run.potions[slot] = null; CloseModal(); RefreshCurrentScreen(); });
            Button(m, 400, 266, 120, 24, "BACK", CloseModal);
        }
        void RefreshCurrentScreen()
        {
            if (ScreenName == "Combat") RefreshBattleUI(); else if (ScreenName == "Map") ShowMap(); else if (ScreenName == "Shop") ShowShop(false);
            else if (ScreenName == "Event") RenderEvent(); else if (ScreenName == "Reward") RenderReward();
            else if (ScreenName == "Rest") ShowRest();
        }
        void ShowReward()
        {
            bool elite = battle.Kind == "elite"; rewardsClaimed.Clear();
            rewardCookies = run.Int(elite ? 28 : 14, elite ? 38 : 22); if (run.Has("reindeerfeed")) rewardCookies = Battle.Round(rewardCookies * 1.25f);
            rewardPotion = null; rewardRelic = elite ? run.RandomRelic() : null;
            if (run.Random() < run.potionChance || elite) { rewardPotion = run.RandomPotion(); run.potionChance = Mathf.Max(.1f, run.potionChance - .1f); } else run.potionChance = Mathf.Min(.9f, run.potionChance + .1f);
            rewardFriends = battle.FriendsWon.Distinct().Where(x => DB.Gifts.ContainsKey("f_" + x)).ToArray(); rewardParts = null;
            if (rewardFriends.Length == 0 && (elite || run.Random() < .5))
            { var parts = DB.All.parts.Where(x => elite || x.rarity < 3).Select(x => x.id).ToList(); run.Shuffle(parts); rewardParts = parts.Take(3).ToArray(); }
            rewardGifts = run.GiftChoices(elite, run.Has("wishlist") ? 4 : 3); RenderReward();
        }
        void RenderReward()
        {
            NewScreen("Reward"); TiledBackground(); Hud(); var p = PanelBox(ui, 170, 56, 300, 258, Gold);
            Label(p, 12, 10, 276, battle.Kind == "elite" ? "ELITE SPOILS" : "SPOILS OF THE NIGHT", .9f, Gold, 1);
            int y = 39;
            void row(string key, string text, Action action, string icon)
            {
                if (!rewardsClaimed.Contains(key)) { var h = Button(p, 20, y, 260, 26, text, action); Sprite(h.transform.parent, "Icon", icon, 12, 13); }
                else Label(p, 20, y + 7, 260, "CLAIMED", .7f, Muted, 1); y += 33;
            }
            row("cookies", rewardCookies + " COOKIES", () => { run.cookies += rewardCookies; rewardsClaimed.Add("cookies"); RenderReward(); sound.Play("cookies"); }, "coin");
            if (rewardPotion != null) row("potion", DB.Potions[rewardPotion].name.ToUpperInvariant(), () => { if (!run.AddPotion(rewardPotion)) { Toast("Potion slots full!", Red); return; } rewardsClaimed.Add("potion"); RenderReward(); }, "potion_" + rewardPotion);
            if (rewardRelic != null) row("relic", DB.Relics[rewardRelic].name.ToUpperInvariant(), () => { run.AddRelic(rewardRelic); rewardsClaimed.Add("relic"); RenderReward(); sound.Play("relic"); }, "relic_" + rewardRelic);
            if (rewardFriends.Length > 0) row("friend", rewardFriends.Length == 1 ? "BEFRIEND " + DB.Enemies[rewardFriends[0]].name.ToUpperInvariant() : "CHOOSE A FRIEND", () =>
            {
                void add(string gid) { string message = run.AddFriend(gid.Substring(2)); rewardsClaimed.Add("friend"); RenderReward(); Toast(message, Pink); }
                if (rewardFriends.Length == 1) add("f_" + rewardFriends[0]); else ChooseGifts(rewardFriends.Select(x => "f_" + x).ToArray(), "CHOOSE A FRIEND", add);
            }, "heart");
            if (rewardParts != null) row("part", "CHOOSE A TOY PART", () => ChooseParts(rewardParts, part => InstallPart(part, () => { rewardsClaimed.Add("part"); RenderReward(); })), "part_" + rewardParts[0]);
            if (rewardFriends.Length == 0) row("gift", "CHOOSE A GIFT", () => ChooseGifts(rewardGifts, "CHOOSE A GIFT", id => { run.AddGift(id); rewardsClaimed.Add("gift"); RenderReward(); }), "gift");
            Button(p, 50, 224, 200, 24, "LEAVE A GIFT >", ShowLeave, Hex(0x1c5a3a));
        }
        void ShowLeave()
        {
            NewScreen("LeaveGift"); TiledBackground(false, 40); Hud(); run.PickWish();
            var house = Sprite(world, "BGImage", "house", 430, 282, 2);
            string kid = DB.Kids[run.night]; Label(ui, 30, 48, 580, "Little " + kid + " is asleep. Leave a gift under the tree.", .85f, Gold, 1);
            bool canLeave = run.bag.Count > 5;
            OpenGiftPicker("LEAVE A GIFT UNDER THE TREE", canLeave ? run.bag : new List<Gift>(), gi =>
            { var copy = gi.Copy(); int joy = run.LeaveGift(gi); BeginDelivery(copy, joy, house); }, null, null,
                kid.ToUpperInvariant() + "'S WISH: " + DB.WishText[run.wish],
                gi => { int joy = run.Joy(gi) + (run.WishOK(run.wish, gi) ? 4 : 0); if (run.doubleJoy && joy > 0) joy *= 2; return (joy >= 0 ? "+" : "") + joy + (run.WishOK(run.wish, gi) ? " WISH!" : gi.Def.type == "friend" ? " ADOPT!" : " SPIRIT"); },
                m =>
                {
                    Button(m, 110, 328, 260, 22, "GIVE " + run.LeaveCookieCost + " COOKIES (+1)", () => { if (run.LeaveCookies()) BeginDelivery(null, 1, house); }, Hex(0xa8741f), run.cookies >= run.LeaveCookieCost);
                    if (run.CanSkipLeave || !canLeave) Button(m, 385, 328, 145, 22, canLeave ? "PATCHED SACK: SKIP" : "NOTHING TO SPARE", () => { if (!canLeave) run.AddSpirit(-5); FinishNode(); });
                });
        }
        void ShowEvent(EventDef e)
        { currentEvent = e; eventOutcome = null; RenderEvent(); }
        void RenderEvent()
        {
            NewScreen("Event"); TiledBackground(true); Hud(); var p = PanelBox(ui, 60, 54, 520, 286, Gold);
            Label(p, 12, 12, 496, currentEvent.title.ToUpperInvariant(), 1.2f, Gold, 1);
            Label(p, 20, 55, 480, CharacterText(eventOutcome?.text ?? currentEvent.text), .92f, White, 0, true);
            if (eventOutcome == null)
            {
                for (int i = 0; i < currentEvent.choices.Length; i++)
                {
                    int index = i; var c = currentEvent.choices[i]; bool ok = run.cookies >= c.price && (!c.needsPotion || run.potions.Any(x => !string.IsNullOrEmpty(x)));
                    Button(p, 16, 192 + i * 26, 488, 22, (i + 1) + ". " + c.label, () =>
                    { eventOutcome = EventRules.Resolve(run, currentEvent.id, index); RenderEvent(); HandleEventOutcome(); }, null, ok);
                }
            }
            else Button(p, 160, 244, 200, 24, eventOutcome.fight != null ? "FIGHT!" : "CONTINUE >", () =>
            { if (eventOutcome.fight != null) StartBattle(eventOutcome.fight, eventOutcome.kind); else FinishNode(); }, Hex(0x1c5a3a));
        }
        void HandleEventOutcome()
        {
            if (eventOutcome.choose != null) ChooseGifts(eventOutcome.choose, "CHOOSE A GIFT", id => { run.AddGift(id); RenderEvent(); });
            else if (eventOutcome.picker != null) EventPicker(eventOutcome.picker);
        }
        void EventPicker(string mode)
        {
            var list = mode == "upgrade" ? run.bag.Where(x => x.CanUpgrade).ToList() : mode == "dup" ? run.bag.Where(x => x.Def.type != "curse").ToList() : run.bag.ToList();
            if (mode == "leave" && run.bag.Count <= 5) { run.doubleJoy = false; Toast("Your sack is too light to spare a gift.", Muted); return; }
            if (list.Count == 0) { Toast("Nothing to rewrap.", Muted); return; }
            OpenGiftPicker(mode == "upgrade" ? "REWRAP A GIFT" : mode == "leave" ? "LEAVE A GIFT" : mode == "dup" ? "DUPLICATE A GIFT" : "TRANSFORM A GIFT", list, g =>
            {
                if (mode == "upgrade") g.Upgrade = true;
                else if (mode == "leave") run.LeaveGift(g);
                else if (mode == "dup") run.AddGift(g.Id, g.Upgrade);
                else { int rarity = g.Def.rarity; run.bag.Remove(g); string id = run.RandomGift(rarity >= 1 && rarity <= 3 ? Mathf.Min(3, rarity + (run.Random() < .3 ? 1 : 0)) : run.RollRarity()); run.AddGift(id); eventOutcome.text += "\nIt became " + DB.Gifts[id].name + "!"; }
                RenderEvent(); sound.Play("buff");
            }, "SKIP", () => { run.doubleJoy = false; CloseModal(); }, null, mode == "leave" ? (Func<Gift, string>)(g => { int n = run.Joy(g); if (run.doubleJoy && n > 0) n *= 2; return (n > 0 ? "+" : "") + n + " SPIRIT"; }) : null);
        }
        void ShowRest()
        {
            NewScreen("Rest"); Sprite(bg, "ChimneyBG", "Animation 1", 0, 0, 1, false); Hud();
            Label(ui, 40, 51, 560, "FIRESIDE - A QUIET NIGHT", 1, Hex(0xf08a3c), 1);
            if (restMessage != null)
            { Label(ui, 452, 134, 174, restMessage, .86f, White, 0, true); Button(ui, 460, 248, 170, 24, "CONTINUE >", FinishNode, Hex(0x1c5a3a)); return; }
            int heal = Battle.Round(run.maxHp * (run.Adv(5) ? .22f : .3f)) + (run.Has("cocoapot") ? 15 : 0);
            void finish(string message)
            { restMessage = message; ShowRest(); sound.Play("heal"); }
            Button(ui, 460, 140, 170, 24, "REST (+" + heal + " HP)", () => { run.Heal(heal); finish("You doze by the crackling fire. The aches fade away."); }, Hex(0x1c5a3a));
            Button(ui, 460, 174, 170, 24, "REWRAP A GIFT", () => OpenGiftPicker("REWRAP A GIFT", run.bag.Where(x => x.CanUpgrade).ToList(), g => { g.Upgrade = true; finish("Ribbons tied, bows fluffed. Good as new."); }), null, run.bag.Any(x => x.CanUpgrade));
            Button(ui, 460, 208, 170, 24, "WRITE TO SANTA", () => { run.AddSpirit(8); finish("Dear Nick, remember the boy who carved toys for everyone?\n+8 Spirit"); }, Hex(0x8e1d2b));
        }
        void ShowTreasure()
        {
            NewScreen("Treasure"); Sprite(bg, "StockingBG", "Animation 1", 0, 0, 1, false); Hud();
            Label(ui, 40, 56, 560, "A STOCKING STUFFED WITH SOMETHING...", 1, Gold, 1);
            var sock = Sprite(world, "Icon", "stocking", 320, 140);
            sock.rectTransform.pivot = new Vector2(.5f, 1); SleighMotion.Add(sock.rectTransform, SleighMotion.Style.Swing, 1.45f, 3);
            Action open = () => { if (busy) return; string id = run.RandomRelic(); if (id != null) run.AddRelic(id); else run.cookies += 100;
                if (Application.isPlaying) StartCoroutine(OpenStocking(sock, id)); else RevealStocking(id); };
            var hot = Hot(world, 280, 130, 80, 150, open, () => { if (!busy) SleighMotion.Add(sock.rectTransform, SleighMotion.Style.Swing, 3.4f, 3); }, () => { if (!busy) SleighMotion.Add(sock.rectTransform, SleighMotion.Style.Swing, 1.45f, 3); });
            Button(ui, 240, 290, 160, 26, "OPEN STOCKING", open, Hex(0x8e1d2b));
        }
        void ShowShop(bool generate)
        {
            if (generate)
            {
                float discount = (run.Has("elfcoupon") ? .8f : 1) * (run.Adv(7) ? 1.15f : 1); int price(int b) => Battle.Round((b + run.Int(-6, 6)) * discount);
                shopItems = new List<ShopItem>(); shopRewrapped = false;
                foreach (var id in run.GiftChoices(false, 5)) shopItems.Add(new ShopItem { type = "gift", id = id, price = price(new[] { 0, 50, 76, 150 }[DB.Gifts[id].rarity]) });
                for (int i = 0; i < 3; i++) { string id = run.RandomRelic(); if (id != null && !shopItems.Any(x => x.type == "relic" && x.id == id)) shopItems.Add(new ShopItem { type = "relic", id = id, price = price(new[] { 0, 150, 210, 270 }[DB.Relics[id].rarity]) }); }
                for (int i = 0; i < 3; i++) shopItems.Add(new ShopItem { type = "potion", id = run.RandomPotion(), price = price(56) });
                var parts = DB.Parts.Keys.ToList(); run.Shuffle(parts); foreach (var id in parts.Take(2)) shopItems.Add(new ShopItem { type = "part", id = id, price = price(new[] { 0, 45, 65, 90 }[DB.Parts[id].rarity]) });
                rewrapPrice = Battle.Round(75 * discount); donatePrice = Battle.Round(40 * discount);
            }
            NewScreen("Shop"); Sprite(bg, "ShopBG", "Animation 1", 0, 0, 1, false); Hud();
            Label(ui, 12, 108, 158, "Welcome to the\nElf Shop!\n\nEverything here\nis hand-made.", .85f, White, 1);
            int gi = 0, ri = 0, pi = 0, ti = 0;
            foreach (var item in shopItems)
            {
                float x, y; RectTransform root; string desc, title;
                if (item.type == "gift") { x = 181 + gi++ * 84; y = 65; var g = new Gift { Id = item.id, Uid = -1 }; root = Card(ui, g, x, y); title = g.Name; desc = GiftTip(g); }
                else
                {
                    x = item.type == "relic" ? 194 + ri++ * 54 : item.type == "potion" ? 374 + pi++ * 44 : 194 + ti++ * 54;
                    y = item.type == "part" ? 278 : 201;
                    var d = item.type == "relic" ? DB.Relics[item.id] : item.type == "potion" ? DB.Potions[item.id] : DB.Parts[item.id];
                    title = d.name; desc = d.desc; root = Group(ui, "Shop:" + title, x, y, 34, 34); Sprite(root, "Icon", item.type + "_" + item.id, 17, 15);
                }
                if (item.sold) { root.gameObject.SetActive(false); Label(ui, x, y + (item.type == "gift" ? 40 : 10), 66, "SOLD", .7f, Muted, 1); continue; }
                if (item.type != "gift") SleighMotion.Add(root, SleighMotion.Style.Float, 1.5f, 2.5f, x);
                float width = item.type == "gift" ? 66 : 34, height = item.type == "gift" ? 88 : 34;
                var h = Hot(root, 0, 0, width, height + 20, () => Buy(item)); BindTip(h, title, desc + "\n\n" + item.price + " Cookies");
                Sprite(root, "Icon", "coin", width / 2 - 14, height + 10); Label(root, width / 2 - 5, height + 4, 40, item.price.ToString(), .8f, run.cookies >= item.price ? Gold : Red);
            }
            Button(ui, 520, 200, 112, 22, "REWRAP " + rewrapPrice, () => OpenGiftPicker("REWRAP A GIFT", run.bag.Where(x => x.CanUpgrade).ToList(), g => { if (run.Spend(rewrapPrice)) { g.Upgrade = true; shopRewrapped = true; ShowShop(false); } }), Hex(0x1c5a3a), !shopRewrapped && run.cookies >= rewrapPrice && run.bag.Any(x => x.CanUpgrade));
            Button(ui, 520, 231, 112, 22, "DONATE " + donatePrice, () => { if (run.Spend(donatePrice)) { run.AddSpirit(6); donatePrice += 20; ShowShop(false); } }, Hex(0xa8741f), run.cookies >= donatePrice);
            Button(ui, 520, 326, 112, 24, "LEAVE >", FinishNode, Hex(0x8e1d2b));
        }
        void Buy(ShopItem item)
        {
            if (item.sold || run.cookies < item.price) { sound.Play("error"); Toast("Not enough cookies, love!", Red); return; }
            if (item.type == "potion" && run.potions.All(x => !string.IsNullOrEmpty(x))) { Toast("Your potion belt is full!", Red); return; }
            if (item.type == "part") { if (!run.bag.Any(x => x.CanPart)) { Toast("None of your gifts has room for it!", Red); return; } InstallPart(item.id, () => { run.Spend(item.price); item.sold = true; ShowShop(false); }); return; }
            if (!run.Spend(item.price)) return;
            if (item.type == "gift") run.AddGift(item.id); if (item.type == "relic") run.AddRelic(item.id); if (item.type == "potion") run.AddPotion(item.id);
            item.sold = true; sound.Play("cookies"); ShowShop(false);
        }
        void OpenLedger(int selection = 0, int page = 0)
        {
            var all = DB.All.enemies.Where(e => e.id != "naughtylist" || Ledger.Get(e.id).seen).ToArray(); selection = Mathf.Clamp(selection, 0, all.Length - 1); page = Mathf.Clamp(page, 0, (all.Length - 1) / 14);
            var m = OpenModal("NICE & NAUGHTY LEDGER / " + all.Count(e => Ledger.Get(e.id).seen) + "/" + all.Length);
            var p = PanelBox(m, 30, 49, 580, 258, Gold); Rect(p, 198, 8, 1, 242, Hex(0x54618c));
            for (int i = 0; i < 14 && page * 14 + i < all.Length; i++)
            { int index = page * 14 + i; var e = all[index]; var l = Ledger.Get(e.id); Button(p, 8, 9 + i * 16, 182, 15, l.seen ? e.name : "???", () => OpenLedger(index, page), index == selection ? Hex(0xa8741f) : Hex(0x253262)); }
            var d = all[selection]; var entry = Ledger.Get(d.id); var art = Sprite(p, "Enemy", d.id, 278, 153, Mathf.Min(1.4f, 95f / DB.Frame("Enemy", d.id).height)); if (art && !entry.seen) art.color = Color.black;
            Label(p, 212, 10, 352, entry.seen ? d.name : "???", 1, Gold, 1);
            string description = entry.seen ? d.lore + "\n\nLoves: " + (entry.love ? DB.LikeLabel(d.love) : "???") + "\nHates: " + (entry.hate ? DB.LikeLabel(d.hate) : "???") + "\nDefeated: " + entry.kills + " / Befriended: " + entry.charms : "You haven't met this monster yet.";
            Label(p, 332, 39, 228, description, .74f, White, 0, true);
            if (entry.charms > 0 && DB.Gifts.ContainsKey("f_" + d.id)) Card(p, new Gift { Id = "f_" + d.id }, 243, 158, .8f);
            int pg = page; Button(m, 50, 319, 50, 22, "<", () => OpenLedger(selection, pg - 1), null, page > 0);
            Button(m, 110, 319, 50, 22, ">", () => OpenLedger(selection, pg + 1), null, (page + 1) * 14 < all.Length);
            Button(m, 455, 319, 140, 22, "BACK", CloseModal);
        }
        void ShowEnding(bool victory, bool afterPoem = false)
        {
            run.active = false; if (!IsBaking) run.Save();
            NewScreen(victory ? "Victory" : "Defeat", victory ? "end" : null); TiledBackground();
            if (victory && !afterPoem)
            {
                var poem = new List<string> { run.secret ? "The Naughty List shredded, its dark magic fled," : "The Krampus fell silent, the dark shadow fled", run.secret ? "And Santa stood laughing, his cheeks rosy red." : "And Santa stood blinking, his cheeks rosy red." };
                poem.Add(run.character == "mrs" ? "\"Martha,\" he whispered, \"you've saved Christmas Day!\"" : run.character == "tinsel" ? "\"Tinsel,\" he whispered, \"you've saved Christmas Day!\"" : "\"Rudie,\" he whispered, \"you've saved Christmas Day!\"");
                poem.Add(run.character == "mrs" ? "\"I know, dear,\" she said. \"Now get on that sleigh!\"" : run.character == "tinsel" ? "\"I know, boss,\" she grinned. \"Now get on that sleigh!\"" : "So Rudolph just snorted and lit up the way.");
                if (run.secret) poem.AddRange(new[] { "And all of the friends that were won on the way", "Climbed into the sleigh for the ride Christmas Day." });
                poem.AddRange(new[] { "And kids heard them say as they flew out of sight:", "\"Merry Christmas to all! Have a jolly good night!\"" });
                PlayPoem(new[] { poem.ToArray() }, () => ShowEnding(true, true)); return;
            }
            Label(ui, 30, 51, 580, victory ? run.secret ? "A TRULY NICE CHRISTMAS!" : "MERRY CHRISTMAS!" : "GAME HO-HO-HOVER!", 1.6f, victory ? Gold : Pink, 1);
            Sprite(world, "Claus", DB.CharacterArt(run.character), 160, 280, 1.5f);
            if (victory)
            {
                int unlocked = Mathf.Min(10, run.advent + 1); if (!IsBaking) PlayerPrefs.SetInt("Sleigh.Advent", Mathf.Max(PlayerPrefs.GetInt("Sleigh.Advent", 0), unlocked));
                Sprite(world, "Enemy", "santa", 475, 280, 1.3f);
                Label(ui, 90, 101, 460, run.secret ? "The list is torn. Every friend you were kind to\ncame to help. Santa remembers who he is." : "The coal falls away. Santa is himself again.\nChristmas Day is safe.", .85f, White, 1, true);
                Label(ui, 190, 165, 260, "Spirit: " + run.spirit + " / " + run.SpiritName + "\nGifts left: " + run.stats.left + "\nWishes granted: " + run.stats.wishes + "\nDefeated: " + run.stats.kills + "\nBefriended: " + run.stats.charmed + "\nAdvent " + unlocked + " unlocked!", .85f, Gold, 1);
            }
            else Label(ui, 170, 130, 300, "Nights survived: " + run.night + " of 24\nMonsters defeated: " + run.stats.kills + "\nGifts left for children: " + run.stats.left + "\nSpirit given: " + run.stats.joy, 1, White, 1, true);
            if (victory) Button(ui, 240, 315, 160, 25, "THE END", ShowTitle, Hex(0x1c5a3a));
            else { Button(ui, 150, 280, 160, 24, "TRY AGAIN", ShowCharacters, Hex(0x8e1d2b)); Button(ui, 330, 280, 160, 24, "TITLE", ShowTitle); }
        }
    }
}
