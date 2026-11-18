using System.Linq;

namespace Sleigh
{
    public class EventOutcome
    {
        public string text, picker, kind = "normal"; public string[] choose, fight;
        public EventOutcome(string text) { this.text = text; }
    }
    public static class EventRules
    {
        public static EventOutcome Resolve(Run r, string id, int i)
        {
            string relic(int rarity = -1) { string rid = r.RandomRelic(rarity); r.AddRelic(rid); return rid == null ? "nothing" : DB.Relics[rid].name; }
            string gift(int rarity) { string gid = r.RandomGift(rarity); r.AddGift(gid); return DB.Gifts[gid].name; }
            string potion() { string p = r.RandomPotion(); r.AddPotion(p); return DB.Potions[p].name; }
            EventOutcome result(string text) => new EventOutcome(text);
            EventOutcome picker(string text, string mode) => new EventOutcome(text) { picker = mode };
            EventOutcome fight(string text, string[] ids, string kind = "normal") => new EventOutcome(text) { fight = ids, kind = kind };
            EventOutcome choices(string text, params string[] ids) => new EventOutcome(text) { choose = ids };
            switch (id)
            {
                case "lostelf":
                    if (i == 0) { r.Hurt(6); return result("纸童的家人将传家法器交给你：" + relic(1) + "."); }
                    if (i == 1) { r.AddSpirit(5); return result("纸童谢过你，哼着傩调踏上归途。"); }
                    r.AddSpirit(-5); return result("纸童的哭声留在雪里，你的灯火暗了一分。");
                case "workshop":
                    if (i == 0) return picker("你写到更深，朱砂凝成了一道更强的符。", "upgrade");
                    if (i == 1) { string g = gift(1); r.cookies += 30; return result("你找到" + g + "，还有30枚铜钱。"); }
                    return result("你掩好工坊的门，留待百工归来。");
                case "carolers":
                    if (i == 0) { r.AddSpirit(10); r.Heal(5); return result("傩鼓与狮步合拍，伤痛稍缓，长街也添了灯火。"); }
                    if (i == 1) { r.Spend(25); return result("舞狮人递来一壶热汤：" + potion() + "."); }
                    r.AddGift("offkey"); return result("你学会以傩鼓乱敌心神。");
                case "pond":
                    if (i == 0) { if (r.Random() < .5) { r.cookies += 60; return result("你破冰拾得60枚铜钱，平安抵岸。"); } r.Hurt(10); return result("冰层碎裂！你跌入河中，失去10生命。"); }
                    r.AddSpirit(-4); return result("绕过冰河耗去了时辰，岁火渐弱。");
                case "cookiejar":
                    if (i == 0) { r.cookies += 75; r.AddGift("coal"); return result("你取走75枚香资，也沾上了一枚晦炭。"); }
                    if (i == 1) { r.Heal(10); r.AddSpirit(-2); return result("灶糖尚温，饥饿稍缓，心里却有些不安。"); } return result("你把供案留给归家的灶君。");
                case "globeshop":
                    if (i == 0) return picker("剪刀落下，窗花改写了一道旧符。", "transform");
                    if (i == 1) { r.Spend(60); return result("窗花下藏着" + relic() + "."); } return result("老妇收起剪刀，目送你离开。");
                case "stable":
                    if (i == 0) { r.Spend(30); r.GainMaxHP(6); return result("狮灵俯首回礼，你的筋骨更强健了。"); }
                    if (i == 1) { r.Hurt(10); return result("狮灵奔过险坡，你在旧鞍中发现" + relic(2) + "。"); } return result("狮灵焦躁地踏着空棚。");
                case "well":
                    if (i == 0) { r.Spend(25); r.AddSpirit(8); return result("井中传来铜钱落水声，远处一户灯亮了。"); }
                    if (i == 1) return picker("古井接住法牌，将祝福送到等待的人家。", "leave");
                    r.Heal(15); r.AddSpirit(-5); return result("井水疗好伤口，也耗掉了一份给归人的祝愿。");
                case "elfgame":
                    if (i == 0) return fight("两只小祟掀开傩面，恶念扑面而来！", new[] { "krampimp", "krampimp" });
                    if (i == 1) { r.Spend(40); return result("小祟拿走40枚铜钱，怪笑着让开道路。"); } r.Hurt(6); return result("小祟偷袭你的腿，随后钻入黑暗。");
                case "factory":
                    if (i == 0) { r.Hurt(7); return choices("三道新符绘成，你可以带走一道。", r.RandomGift(2), r.RandomGift(2), r.RandomGift(r.Random() < .4 ? 3 : 2)); }
                    r.Heal(12); return result("你枕着符纸短眠，醒来时精神稍振。");
                case "gingerhouse":
                    if (i == 0) { r.Heal(20); r.AddGift("coal"); return result("灶糖恢复了体力，可糖里竟藏着晦炭。"); }
                    if (i == 1) return fight("纸门骤开，灶糖婆带着童子现身！", new[] { "witch", "gingerling" }, "elite"); return result("你退回古道，任身后的怪笑消失。");
                case "sleigh":
                    if (i == 0) { string name = relic(); r.AddSpirit(-6); return result("你找到" + name + "。取走遗物后，车上最后的光熄了。"); }
                    r.AddSpirit(12); return result("铜铃越过山谷，失散的行人又认出了归途。");
                case "naughtykid":
                    if (i == 0) { r.doubleJoy = true; return picker("你把法牌化为灯火，孩子终于愿意再相信一次。", "leave"); }
                    if (i == 1) { r.Hurt(5); r.AddGift("snowball", true); return result("陪孩子练过投雷，你带走了升级的震煞雷。"); }
                    r.cookies += 40; r.AddSpirit(-8); return result("孩子丢下40枚压岁钱跑开，你的灯火也暗了。");
                case "mistletoe":
                    if (i == 0) { r.Heal(12); r.AddGift("kiss"); return result("团圆的记忆化作回响，温暖流过你的伤口。"); }
                    return result("桃枝在掌中化为" + gift(2) + "。");
                case "toymaker":
                    if (i == 0) { int n = r.bag.Count(x => x.CanUpgrade); r.UpgradeRandom(2); return result(n > 0 ? System.Math.Min(n, 2) + "张法牌重新淬炼。" : "所有法牌都已淬炼，老人微笑点头。"); }
                    if (i == 1) { r.Spend(50); return choices("符匠铺开珍藏的三道法术。", r.RandomGift(2), r.RandomGift(2), r.RandomGift(3)); } return result("老人说：愿你平安过年。");
                case "blizzard":
                    if (i == 0) { r.Hurt(10); return result("你顶着寒风穿过山口，失去10生命。"); }
                    if (i == 1) { r.AddSpirit(-8); return result("等待耗去了岁火，山口终于稍稍平静。"); }
                    int pslot = System.Array.FindIndex(r.potions, p => !string.IsNullOrEmpty(p)); if (pslot >= 0) r.potions[pslot] = null; return result("你饮尽一瓶药汤，驱寒后继续赶路。");
                case "postman":
                    if (i == 0) { r.AddSpirit(12); return result("每封家书都藏着平安归来的愿望，岁灯重新明亮。"); }
                    if (i == 1) { r.Hurt(6); r.AddSpirit(18); return result("你踏雪送信，失去6生命，沿途灯火却一户户亮起。"); }
                    r.Heal(12); r.AddSpirit(-12); return result("家书烧成灰烬，身体暖了，岁火却冷了。");
                case "cocoastand":
                    if (i == 0) { r.Spend(20); r.Heal(18); return result("姜枣汤温热，恢复18生命。"); }
                    if (i == 1) { r.Spend(45); potion(); potion(); return result("你装好两壶药汤，留待前路。"); } return result("摊主向你挥手：{nick}，一路平安！");
                case "fishing":
                    if (i == 1) return result("你把鱼竿留给下一位行人。");
                    double roll = r.Random(); if (roll < .35) return result("你钓起一瓶" + potion() + "!");
                    if (roll < .7) { r.cookies += 45; return result("钱袋里有45枚铜钱！"); } r.Hurt(6); return result("鱼妖咬伤你的手，失去6生命。");
                case "ball":
                    if (i == 0) { r.Hurt(5); return picker("傩步磨破脚底，你却悟出新的符法。", "upgrade"); }
                    if (i == 1) return fight("纸甲门卒抱拳立定，试阵开始！", new[] { "nutgrunt" }); return result("傩鼓渐远，你继续赶路。");
                case "stocking":
                    if (i == 0) { string pn = potion(); r.cookies += 30; return result("红包里装着" + pn + "和30枚铜钱。"); }
                    return picker("你给红包添入法牌，愿后来人平安。", "leave");
                case "angels":
                    if (i == 0) { r.Heal(8); r.AddSpirit(6); return result("岁灯拨亮，你在灯下歇息，恢复8生命并获得6岁火。"); } return result("你护住残灯，继续走向岁门。");
                case "fallenstar":
                    if (i == 0) { if (!r.Has("treestar")) { r.AddRelic("treestar"); return result("灯芯在行囊中温暖地跳动。"); } r.cookies += 100; return result("已有的灯芯回应着它，余光化为100枚铜钱。"); }
                    r.AddSpirit(20); return result("岁火升向夜空，照亮更远的人家。");
                case "exchange": return i == 0 ? picker("你交换符匣，一道旧法即将改变。", "transform") : result("傩面客收起符匣，隐入灯市。");
                case "snowmanfriend":
                    if (i == 0) { r.Hurt(4); r.AddSpirit(10); return result("两个纸偶并肩坐着，等归家人推开祠门。"); }
                    r.AddGift("scarf", true); r.AddSpirit(-8); return result("纸偶失去红绫，默默望着你离开。");
                case "mirror":
                    if (i == 0) { r.GainMaxHP(-6); return picker("记忆带回另一道法牌，代价是6点生命上限。", "dup"); }
                    if (i == 1) { r.AddSpirit(10); return result("你记起先师点灯的手，也记起此行的理由。"); }
                    r.Hurt(4); r.cookies += 40; return result("铜镜碎裂，40枚铜钱落地，回忆再也看不清了。");
                default: throw new System.InvalidOperationException("Unmigrated event " + id);
            }
        }
    }
}
