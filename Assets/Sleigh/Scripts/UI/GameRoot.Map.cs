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
                    for (int k = 2; k < count - 1; k++) { var pos = Vector2.Lerp(from, to, k / (float)count); var dot = Sprite(mapContent, "Icon", "path_dot", pos.x, pos.y); if (dot) dot.color = lit ? Gold : Hex(0xb87d59, .7f); }
                }
            }
            for (int i = 0; i < 24; i++)
            {
                int n = i; var night = run.map[i];
                SpringFestivalProp.Home(mapContent, nx(i), 318, .8f, night.lit);
                Label(mapContent, nx(i) - 35, 324, 70, "DEC " + (i + 1), .72f, i == run.night ? Gold : Muted, 1);
                var house = Hot(mapContent, nx(i) - 28, 274, 56, 48, () => { if (n == run.night) { run.PickWish(); ShowItem(DB.Kids[n] + "'S WISH", DB.WishText[run.wish], "gift"); } });
                BindTip(house, DB.Kids[i] + "'s house / DEC " + (i + 1), i < run.night ? night.lit ? "A lovely gift lit up the night." : "The house stayed dark." : "A child is waiting for Christmas.");
                house.drag = e => DragMap(-e.delta.x / uiScale);
                for (int j = 0; j < night.options.Length; j++)
                {
                    string type = night.options[j]; float x = nx(i), y = ny(i, j); bool reach = i == run.night;
                    var icon = Sprite(mapContent, "Icon", "node_" + type, x, y);
                    if (icon) icon.color = night.done && night.chosen != type ? Hex(0x80675e) : !reach && !night.done ? Hex(0xc6aa90) : Color.white;
                    if (night.done && night.chosen == type) Sprite(mapContent, "Icon", "check", x + 16, y - 16);
                    var node = Hot(mapContent, x - 25, y - 25, 50, 50, () => { if (n == run.night) EnterNode(type); }); node.drag = e => DragMap(-e.delta.x / uiScale);
                    string name = type == "rest" ? "围炉歇脚" : type == "shop" ? "年货集市" : type == "treasure" ? "年礼宝箱" : type == "boss" ? "除夕岁关·年兽" : type == "elite" ? "精英战·醒狮试炼" : type == "battle" ? "普通战·爆竹驱祟" : "年俗奇遇";
                    string desc = type == "battle" ? "前路出现岁祟，准备送出年礼、战斗驱祟。" : type == "elite" ? "强敌守着一份年礼，战胜后可获得遗物。" : type == "rest" ? "围炉休息、强化年礼，或写下新春祝愿。" : type == "shop" ? "花费压岁钱购买年礼、遗物和年节补给。" : type == "treasure" ? "一份守岁惊喜，等你开启。" : type == "boss" ? "迎战年兽，守住大家的团圆。" : "守岁途中有意想不到的相遇，前去看看吧。";
                    BindTip(node, name + " / DEC " + (i + 1), desc + (reach ? "\nChoose one node for tonight." : ""));
                }
            }
            SleighMotion.Add(Sprite(mapContent, "Icon", "pin", nx(run.night), ny(run.night, 0) - 44).rectTransform, SleighMotion.Style.Float, 3, 4);
            Hud(); Rect(ui, 60, 45, 520, 39, Hex(0x3f1c2b, .9f));
            Label(ui, 60, 49, 520, (24 - run.night) + " NIGHTS UNTIL CHRISTMAS", 1, Gold, 1);
            Label(ui, 60, 66, 520, "Christmas Spirit: " + run.SpiritName, .85f, run.SpiritTier > 1 ? Green : Pink, 1);
            Label(ui, 90, 346, 460, "Drag, scroll or use arrow keys", .75f, Muted, 1);
            Button(ui, 8, 241, 27, 24, "<", () => MoveMap(-360)); Button(ui, 605, 241, 27, 24, ">", () => MoveMap(360));
        }
        void MoveMap(float delta)
        { if (ScreenName != "Map" || !mapContent || modal) return; mapTarget = Mathf.Clamp(mapTarget + delta, 0, 1676); if (!Application.isPlaying) { mapScroll = mapTarget; mapContent.anchoredPosition = new Vector2(-Mathf.Round(mapScroll), 0); } }
    }
}
