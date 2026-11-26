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
        string[] rewardGifts, rewardParts, rewardFriends;
        string rewardRelic, rewardPotion;
        int rewardCookies;
        readonly HashSet<string> rewardsClaimed = new HashSet<string>();

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
            NewScreen("LeaveGift"); TiledBackground(); Hud(); run.PickWish();
            var house = SpringFestivalProp.Home(world, 430, 282, 2);
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
    }
}
