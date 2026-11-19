using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public sealed class FestivalStoryView : MonoBehaviour
    {
        public CanvasGroup body;
        public PixelLabel heading, counter, nextCaption;
        public PixelLabel[] lines;
        public Image[] progress;
        bool ending;
        static readonly string[] OpeningTitles = { "灯火迎春", "年兽将至", "一同守岁" };

        public static FestivalStoryView Build(Transform parent, string character, bool ending, Action next, Action skip)
        {
            var root = Group(parent, "Spring Festival story cards");
            var view = Ensure<FestivalStoryView>(root); view.ending = ending;
            Label(root, 200, 17, 240, ending ? "爆竹驱年 · 尾声" : "爆竹驱年 · 序章", .65f, Hex(0xe9b78a), 1);
            view.heading = Label(root, 178, 40, 284, "灯火迎春", 2.2f, Hex(0xffe6ad), 1);
            Label(root, 175, 78, 290, "十二生肖守岁记 · " + DB.CharacterName(character), .68f, Hex(0xffd192), 1);
            view.counter = Label(root, 538, 19, 67, "01 / 03", .65f, Gold, 2);

            var panel = Group(root, "Readable story panel", 36, 223, 568, 105);
            Rect(panel, 0, 2, 568, 105, Hex(0x1b111b, .55f));
            Rect(panel, 0, 0, 568, 105, Hex(0x48232e, .97f));
            Rect(panel, 0, 0, 568, 1, Hex(0xe8ac64)); Rect(panel, 0, 104, 568, 1, Hex(0xb87c51));
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? 8 : 548;
                Rect(panel, x, 8, 12, 1, Gold); Rect(panel, x, 8, 1, 10, Gold);
                Rect(panel, x, 95, 12, 1, Gold); Rect(panel, x + 11, 86, 1, 10, Gold);
            }
            var textRoot = Group(panel, "Story text", 0, 0, 568, 105);
            view.body = Ensure<CanvasGroup>(textRoot); view.lines = new PixelLabel[4];
            for (int i = 0; i < 4; i++)
                view.lines[i] = Label(textRoot, 23, 15 + i * 20, 522, "", .96f, i == 3 ? Hex(0xffd887) : Hex(0xffedd0), 1);
            Button(root, 36, 334, 80, 20, "跳过剧情", skip, Hex(0x5c303a));
            Label(root, 133, 340, 286, "自动翻页 · 空格 / 回车继续", .6f, Hex(0xe8b68d), 1);
            var nextButton = Button(root, 447, 334, 157, 20, "下一幕 >", next, Hex(0x983c40));
            view.nextCaption = nextButton.transform.parent.GetComponentInChildren<PixelLabel>();
            view.progress = new Image[3];
            for (int i = 0; i < 3; i++) view.progress[i] = Rect(root, 297 + i * 17, 321, 12, 2, Gold);
            return view;
        }

        public void SetPage(string[] text, int page, int count)
        {
            heading.Set(ending ? "灯火归家" : OpeningTitles[Mathf.Min(page, OpeningTitles.Length - 1)]);
            counter.Set((page + 1).ToString("00") + " / " + count.ToString("00"));
            for (int i = 0; i < lines.Length; i++) lines[i].Set(i < text.Length ? text[i] : "");
            nextCaption.Set(page == count - 1 ? ending ? "共迎新春 >" : "踏上守岁路 >" : "下一幕 >");
            for (int i = 0; i < progress.Length; i++)
            { progress[i].gameObject.SetActive(i < count); progress[i].color = i == page ? Gold : Hex(0x9b6352); }
            body.alpha = 1;
        }
    }

    public partial class GameRoot
    {
        public static string[][] FestivalOpening(string character)
        {
            string arrival = character == "mrs" ? "克劳斯夫人带上点心，循着花灯赶来，"
                : character == "tinsel" ? "廷瑟背起工具，循着锣鼓声赶来，" : "鲁道夫带着驯鹿伙伴，循着灯火赶来，";
            return new[] {
                new[] { "腊月的岁安镇，家家贴春联，户户挂红灯。", "年货摊前笑声不断，灶上的年糕正冒着热气。", "十二生肖忙着送福，孩子们提着花灯等团圆。", "这是一年里，大家最舍不得熄灯的夜晚。" },
                new[] { "可山口传来年兽的低吼，祟气正逼近村庄。", "守护归乡路的生肖灵身，被祟气扰乱了心神。", "一封求援信越过屋檐，送到熟悉的朋友手中。", arrival + "要把这份热闹守到新年。" },
                new[] { "前方是二十四站守岁路，终点便是除夕岁关。", "送出年礼，唤醒生肖，让沿途的灯火重新亮起。", "用红绸、锣鼓与爆竹，迎战挡路的年兽。", "今年，谁都不能错过这顿团圆饭。" }
            };
        }

        void PlayFestivalStory(string[][] stanzas, Action onDone)
        {
            var pages = new List<string[]>();
            foreach (var stanza in stanzas)
                for (int i = 0; i < stanza.Length; i += 4)
                {
                    var page = new string[Mathf.Min(4, stanza.Length - i)];
                    Array.Copy(stanza, i, page, 0, page.Length); pages.Add(page);
                }
            if (pages.Count == 0) { onDone(); return; }
            bool done = false, advance = false;
            Action finish = () => { if (done) return; done = true; skipPoem = null; onDone(); };
            skipPoem = finish;
            var view = FestivalStoryView.Build(ui, run?.character ?? "mrs", ScreenName != "Intro", () => advance = true, finish);
            view.SetPage(pages[0], 0, pages.Count);
            if (Application.isPlaying) StartCoroutine(ReadFestivalStory(view, pages, () => done, () => advance, () => advance = false, finish));
        }

        IEnumerator ReadFestivalStory(FestivalStoryView view, List<string[]> pages, Func<bool> done, Func<bool> advance, Action reset, Action finish)
        {
            for (int page = 0; page < pages.Count && view && !done(); page++)
            {
                reset(); view.SetPage(pages[page], page, pages.Count);
                float elapsed = 0, duration = 8.5f;
                // Fade text in place, so it never runs under navigation buttons.
                while (view && !done())
                {
                    if (modal) { yield return null; continue; }
                    elapsed += Time.unscaledDeltaTime;
                    view.body.alpha = reduceMotion ? 1 : Mathf.Clamp01(elapsed / .35f);
                    if (advance() || elapsed > .35f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) || elapsed >= duration) break;
                    yield return null;
                }
                if (view && !done()) view.body.alpha = 1;
            }
            if (view && !done()) finish();
        }
    }
}
