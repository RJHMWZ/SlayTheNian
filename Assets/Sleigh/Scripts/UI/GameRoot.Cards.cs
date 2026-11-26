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
        RectTransform Card(Transform parent, Gift g, float x, float y, float scale = 1, Battle preview = null)
        {
            var prefab = Resources.Load<GameObject>("Sleigh/Prefabs/GiftView");
            var r = Acquire(parent, "Card", "Gift", x, y, 66, 88, prefab);
            r.name = "Gift " + g.Uid + ": " + g.Name; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.localScale = Vector3.one * scale;
            string art = g.Def.type == "friend" ? g.Def.enemy : g.Id;
            if (DB.Frame("Gift", art) == null) art = g.Def.type;
            if (prefab) { var body = r.GetComponent<Image>(); body.sprite = DB.Sprite("CardFrame", g.Def.type); body.color = Color.white; body.raycastTarget = false; }
            else Sprite(r, "CardFrame", g.Def.type, 0, 0, 1, false, 66, 88);
            Image illustration;
            if (g.Def.type == "friend")
            {
                var f = DB.Frame("Enemy", art);
                illustration = Sprite(r, "Enemy", art, 33, 66, Mathf.Min(42f / f.width, 44f / f.height));
            }
            else illustration = Sprite(r, "Gift", art, 12, 24, 1, false, 42, 42);
            if (illustration) illustration.name = "Festival card illustration";
            Label(r, 3, 3, 60, g.Name, .75f, g.Upgrade ? Hex(0xc8ffb0) : White, 1, true);
            if (!g.Def.flags.Contains("u"))
            { Sprite(r, "Icon", "cost", 4, 4); Label(r, -3, -2, 14, g.Cost.ToString(), .8f, preview != null && g.Cost > preview.Energy ? Red : White, 1); }
            var values = g.Values(); string stat; string icon = null;
            if (values.TryGetValue("d", out int d)) { stat = (preview != null ? preview.DamageCalc(preview.Player, null, d) : d).ToString(); icon = "sword"; if (g.Id == "baubles" || g.Id == "stampede") stat += "×3"; }
            else if (values.TryGetValue("b", out int b)) { stat = (preview != null ? preview.BlockCalc(preview.Player, b) : b).ToString(); icon = "shield"; }
            else if (g.Id == "bellybump") { stat = preview != null ? preview.DamageCalc(preview.Player, null, preview.Player.Block).ToString() : "护甲"; icon = "sword"; }
            else stat = DB.TypeName(g.Def.type).ToUpperInvariant();
            if (icon != null) Sprite(r, "Icon", icon, 20, 76);
            Label(r, icon != null ? 29 : 1, 71, icon != null ? 34 : 64, stat, 1, White, icon != null ? 0 : 1);
            if (!string.IsNullOrEmpty(g.ToyPart)) Sprite(r, "Icon", "part_" + g.ToyPart, 62, 4);
            if (Application.isPlaying && !IsBaking)
                PixelUI.Ensure<CardSurfaceEffects>(r).Configure(g.Uid, reduceMotion);
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
    }
}
