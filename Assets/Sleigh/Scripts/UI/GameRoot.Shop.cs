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
        class ShopItem { public string type, id; public int price; public bool sold; }
        List<ShopItem> shopItems;
        int rewrapPrice, donatePrice;
        bool shopRewrapped;

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
            NewScreen("Shop"); SpringFestivalBackdrop.Build(bg, SpringFestivalBackdrop.Setting.Market); Hud();
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
    }
}
