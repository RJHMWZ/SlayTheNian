using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance;
        [SerializeField] SleighStage stage;
        [NonSerialized]
        public Run run;
        [NonSerialized]
        public Battle battle;
        public bool ReduceMotion => reduceMotion;
        public bool IsBaking { get; private set; }
        public SleighStage Stage => stage;
        public string ScreenName { get; private set; }
        RectTransform area, bg, world, ui, fx, pop, modal, tip;
        PixelLabel tipText;
        Canvas canvas;
        SleighAudio sound;
        bool busy, reduceMotion, fastEnemy, confirmEnd;
        float uiScale = 1;
        int characterIndex, advent;
        readonly string[] characters = { "mrs", "tinsel", "rudolph" };
        readonly Dictionary<Unit, ActorVisual> actors = new Dictionary<Unit, ActorVisual>();
        Action modalBack;
        Action skipPoem;
        string tipContent;
        readonly List<Snowflake> snow = new List<Snowflake>();
        class Snowflake { public RectTransform rect; public float x, y, speed, phase; }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; } Instance = this;
            DB.Load(); Application.targetFrameRate = 60; Application.runInBackground = true; Screen.orientation = ScreenOrientation.LandscapeLeft;
            if (!Application.isEditor && (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer))
                Screen.SetResolution(1920, 1080, Screen.fullScreenMode);
            if (!stage || !stage.canvas)
            { Debug.LogError("This scene has no baked stage. Run Sleigh/Bake Scene and Presentation.", this); enabled = false; return; }
            BindStage(stage, false);
            reduceMotion = PlayerPrefs.GetInt("Sleigh.ReduceMotion", 0) != 0; fastEnemy = PlayerPrefs.GetInt("Sleigh.FastEnemy", 0) != 0; confirmEnd = PlayerPrefs.GetInt("Sleigh.ConfirmEnd", 0) != 0;
        }
        void Start() { if (stage && ScreenName == null) ShowTitle(); }
        public void BindStage(SleighStage value, bool baking)
        {
            stage = value; IsBaking = baking; canvas = value.canvas; area = value.viewport; sound = value.audioPlayer;
            fx = value.effectPool; pop = value.popupPool; uiScale = -1;
            snow.Clear(); var rng = new System.Random(931);
            foreach (var r in value.snowParticles) snow.Add(new Snowflake { rect = r, x = rng.Next(640), y = rng.Next(360), speed = 10 + rng.Next(20), phase = snow.Count });
        }
        void Update()
        {
            if (!area) return;
            float fit = Mathf.Min(Screen.width / 640f, Screen.height / 360f); float desired = fit >= 1 ? Mathf.Floor(fit) : fit;
            if (Mathf.Abs(desired - uiScale) > .001f) { uiScale = desired; area.localScale = Vector3.one * uiScale; }
            foreach (var s in snow)
            {
                if (!s.rect) continue;
                s.y += s.speed * Time.unscaledDeltaTime; s.x += (8 + Mathf.Sin(Time.unscaledTime + s.phase) * 5) * Time.unscaledDeltaTime;
                if (s.y > 365) { s.y = -5; s.x = Mathf.Repeat(s.x + 77, 640); } s.x = Mathf.Repeat(s.x, 640);
                s.rect.anchoredPosition = new Vector2(Mathf.Round(s.x), -Mathf.Round(s.y));
            }
            foreach (var a in actors.Values) a.Tick(reduceMotion);
            TickPresentation();
            if (tip && tip.gameObject.activeSelf)
            {
                Vector2 p = LocalPointer(Input.mousePosition); float height = tip.sizeDelta.y;
                tip.anchoredPosition = combatTipDock ? new Vector2(28, -50)
                    : new Vector2(Mathf.Clamp(p.x + 14, 4, 396), -Mathf.Clamp(p.y - height - 8, 45, 354 - height)); tip.SetAsLastSibling();
            }
            if (busy) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            { if (modal) { modalBack?.Invoke(); } else if (skipPoem != null) skipPoem(); else if (ScreenName == "Combat" && (selectedGift != null || powerAim || potionAim >= 0)) CancelAim(); else ShowSettings(); return; }
            if (modal) return;
            if (Input.GetKeyDown(KeyCode.Tab) && run != null) OpenBag(run.bag, "YOUR SACK");
            if (ScreenName == "Combat")
            {
                if (Input.GetKeyDown(KeyCode.E)) RequestEndTurn();
                if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Q)) PressPower();
                for (int i = 0; i < Mathf.Min(9, battle.Hand.Count); i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectGift(battle.Hand[i]);
            }
            else if (ScreenName == "Title" && Input.GetKeyDown(KeyCode.Return)) ShowCharacters();
            else if (ScreenName == "CharacterSelect")
            { if (Input.GetKeyDown(KeyCode.LeftArrow)) { characterIndex = (characterIndex + 2) % 3; ShowCharacters(); } if (Input.GetKeyDown(KeyCode.RightArrow)) { characterIndex = (characterIndex + 1) % 3; ShowCharacters(); } if (Input.GetKeyDown(KeyCode.Return)) BeginRun(); }
            else if (ScreenName == "Map")
            { if (Input.GetKey(KeyCode.RightArrow)) MoveMap(300 * Time.unscaledDeltaTime); if (Input.GetKey(KeyCode.LeftArrow)) MoveMap(-300 * Time.unscaledDeltaTime); }
        }
        void OnApplicationQuit() { PlayerPrefs.Save(); }
        Vector2 LocalPointer(Vector2 screen)
        { RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var p); return new Vector2(p.x + 320, 180 - p.y); }
        void NewScreen(string name, string music = "calm")
        {
            if (Application.isPlaying) StopAllCoroutines();
            HideTip(); CloseModal(); skipPoem = null; ScreenName = name; busy = false; selectedGift = null; powerAim = false; potionAim = -1;
            var view = stage.Show(name); bg = view.background; world = view.world; ui = view.ui;
            foreach (var layer in new[] { bg, world, ui, fx, pop }) Clear(layer);
            actors.Clear(); nianPhases.Clear(); handViews.Clear(); handOrder.Clear(); displayedUnits.Clear(); assistingGuest = null; usingVisualNumbers = false; ambientTimer = .2f; ambientFireTimer = .25f; floatingRelic = null; tip = null; sound.Music(music); stage.Transition();
            Rect(bg, 0, 0, 640, 360, Hex(0x6ca7a1));
        }
        void TiledBackground(bool intro = false, float up = 0)
        {
            if (ScreenName == "CharacterSelect") ScenicBackground("characters");
            else if (ScreenName == "Map") ScenicBackground("map");
            else if (ScreenName == "Combat") ScenicBackground("battle");
            else if (ScreenName == "Title" || ScreenName == "Intro" || ScreenName == "Victory" || ScreenName == "Defeat")
                MenuArtwork();
            else Sprite(bg, "FestivalRegion", NianTheme.RegionArt(run?.night ?? 0), 0, -up, 1, false, 640, 360 + up);
            // The authored scenes already contain snow. Keep the active gameplay area clear.
            Snow(ScreenName == "Title" || ScreenName == "CharacterSelect" || ScreenName == "Map" || ScreenName == "Combat" ? 0 : 12);
        }
        Sprite menuArtworkSprite;
        void MenuArtwork()
        {
            if (!stage.titleArtwork) { FitBackdrop(Sprite(bg, "NianTitle", "default", 0, 0, 1, false)); return; }
            if (!menuArtworkSprite || menuArtworkSprite.texture != stage.titleArtwork)
                menuArtworkSprite = UnityEngine.Sprite.Create(stage.titleArtwork, new Rect(0, 0, stage.titleArtwork.width, stage.titleArtwork.height), new Vector2(.5f, .5f), 1);
            var image = Rect(bg, 0, 0, 640, 360, Color.white, false, "Menu artwork: Bg1");
            image.sprite = menuArtworkSprite; FitBackdrop(image);
        }
        void ScenicBackground(string scene)
        { FitBackdrop(Sprite(bg, "Scenery", scene, 0, 0, 1, false)); }
        static void FitBackdrop(Image image)
        {
            if (!image || !image.sprite) return;
            var size = image.sprite.rect.size;
            size *= Mathf.Max(640 / size.x, 360 / size.y);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = new Vector2((640 - size.x) * .5f, (size.y - 360) * .5f);
        }
        void Snow(int count)
        {
            stage.snowRoot.gameObject.SetActive(true);
            for (int i = 0; i < snow.Count; i++) snow[i].rect.gameObject.SetActive(i < count);
        }
        void BindTip(Hotspot h, string title, string description, bool enemy = false)
        {
            Action enter = h.enter, exit = h.exit;
            h.enter = () => { enter?.Invoke(); ShowTip(title + "\n\n" + description, enemy); };
            h.exit = () => { exit?.Invoke(); HideTip(); };
        }
        bool combatTipDock;
        void ShowTip(string content, bool enemy = false)
        {
            combatTipDock = enemy && ScreenName == "Combat";
            if (combatTipDock) content = content.Replace("\n\n", " · ").Replace("\n", " · ");
            content = Chinese.Text(content);
            if (busy) return; tipContent = content;
            float width = combatTipDock ? 584 : 240, scale = combatTipDock ? .7f : .8f;
            if (!tip)
            {
                tip = PanelBox(pop, 0, 0, width, 100, Gold);
                tipText = Label(tip, 9, 8, width - 18, content, scale, White, 0, true);
            }
            tipText.rectTransform.sizeDelta = new Vector2(width - 18, 120); tipText.fontScale = scale;
            tipText.Set(content); tipText.SetVerticesDirty();
            float h = 18 + PixelLabel.Lines(content, width - 18, scale, true).Count * 12 * scale;
            tip.sizeDelta = new Vector2(width, h);
            var imgs = tip.GetComponentsInChildren<Image>();
            if (imgs.Length >= 3) { imgs[0].rectTransform.sizeDelta = new Vector2(width, h); imgs[1].rectTransform.sizeDelta = new Vector2(width - 2, h - 2); imgs[2].rectTransform.sizeDelta = new Vector2(width - 6, h - 6); }
            if (imgs.Length >= 4) imgs[3].rectTransform.sizeDelta = new Vector2(width - 8, 1);
            tip.gameObject.SetActive(true); tip.SetAsLastSibling();
        }
        void HideTip() { tipContent = null; if (tip) tip.gameObject.SetActive(false); }
        RectTransform OpenModal(string title, Action back = null)
        {
            CloseModal(); HideTip(); modal = Group(pop, "Modal:" + title); Rect(modal, 0, 0, 640, 360, Hex(0x6b3f38, .88f), true);
            modalBack = back ?? CloseModal; Label(modal, 30, 18, 580, title, 1.1f, Gold, 1);
            if (tip) tip.SetAsLastSibling(); return modal;
        }
        void CloseModal() { if (modal) Release(modal); modal = null; modalBack = null; HideTip(); }
        void Toast(string text, Color? c = null) { if (!fx || !Application.isPlaying) return; var t = Label(fx, 40, 56, 560, text, .9f, c ?? Gold, 1, true); StartCoroutine(FloatAndDestroy(t.rectTransform, 1.8f, 14)); }
        IEnumerator FloatAndDestroy(RectTransform r, float duration, float rise)
        {
            if (!r) yield break; var slot = r.GetComponent<PixelPoolElement>(); int token = slot ? slot.generation : 0;
            var start = r.anchoredPosition; float t = 0; var group = PixelUI.Ensure<CanvasGroup>(r);
            while (t < duration && r && (!slot || slot.generation == token)) { t += Time.unscaledDeltaTime; r.anchoredPosition = start + Vector2.up * (rise * t / duration); group.alpha = Mathf.Min(1, (duration - t) * 3); yield return null; }
            if (r && (!slot || slot.generation == token)) Release(r);
        }
        void Hud()
        {
            if (run == null) return;
            Rect(ui, 0, 0, 640, 26, Hex(0x9a4039, .98f)); Rect(ui, 0, 25, 640, 1, Gold);
            Sprite(ui, "Icon", "heart", 15, 13); hudHp = Label(ui, 26, 7, 92, run.hp + "/" + run.maxHp, .8f);
            Sprite(ui, "Icon", "coin", 116, 13); hudCookies = Label(ui, 127, 7, 58, run.cookies.ToString(), .8f, Gold);
            for (int i = 0; i < run.potionSlots; i++)
            {
                int slot = i; string id = run.potions[i]; float x = 184 + i * 19;
                if (string.IsNullOrEmpty(id)) { Rect(ui, x - 5, 9, 9, 9, Hex(0x632b2d)); continue; }
                Sprite(ui, "Icon", "potion_" + id, x, 13);
                BindTip(Hot(ui, x - 8, 3, 17, 22, () => PotionMenu(slot)), DB.Potions[id].name, DB.Potions[id].desc + "\nClick to drink or discard.");
            }
            Sprite(ui, "Icon", "tree", 274, 13);
            hudSpirit = Label(ui, 285, 7, 140, "SPIRIT " + run.spirit + " " + run.SpiritName, .7f, run.SpiritTier >= 2 ? Green : Pink);
            BindTip(Hot(ui, 266, 1, 159, 23, () => ShowSpirit()), "Christmas Spirit", "Fades by 1 each night.\n0-24: draw 1 fewer.\n25-49: Krampus grows stronger.\n50-69: Merry.\n70-89: +1 Cheer on turn 1.\n90-100: +1 Cheer each turn.\nLeave gifts and fulfill wishes to raise it.");
            BindTip(Button(ui, 435, 3, 66, 19, "SACK " + run.bag.Count, () => OpenBag(run.bag, "YOUR SACK")), "Your gifts", "Tab: view your permanent deck.");
            Button(ui, 507, 3, 66, 19, "LEDGER", () => OpenLedger());
            Button(ui, 579, 3, 53, 19, "MENU", () => ShowSettings());
            for (int i = 0; i < run.relics.Count; i++)
            {
                var r = run.relics[i]; float x = 12 + i % 27 * 19, y = 36 + i / 27 * 18;
                Sprite(ui, "Icon", "relic_" + r.id, x, y);
                BindTip(Hot(ui, x - 8, y - 8, 17, 17, () => ShowItem(DB.Relics[r.id].name, DB.Relics[r.id].desc, "relic_" + r.id)), DB.Relics[r.id].name, DB.Relics[r.id].desc);
            }
            Label(ui, 530, 31, 100, "第 " + Mathf.Min(24, run.night + 1) + " / 24", .7f, Muted, 2);
        }
        void ShowSpirit() => ShowItem("CHRISTMAS SPIRIT: " + run.spirit, "Current tier: " + run.SpiritName + ".\n\nSpirit fades every night. Leave gifts to keep Christmas alive.\n\nGrim: draw 1 fewer. Fading: Krampus grows stronger. Merry: kids still believe. Festive: +1 Cheer on the first turn. Jolly: +1 Cheer every turn.", "tree");
        void ShowItem(string title, string text, string icon)
        { var m = OpenModal(title); Sprite(m, "Icon", icon, 320, 90, 3); Label(m, 110, 130, 420, text, 1, White, 1, true); Button(m, 250, 308, 140, 24, "BACK", CloseModal); }
        string CharacterText(string s)
        { string id = run?.character ?? characters[characterIndex]; return Chinese.Text(s).Replace("{nick}", DB.CharacterName(id)).Replace("{name}", DB.CharacterName(id)).Replace("{title}", id == "mrs" ? "守灯人" : id == "rudolph" ? "巡山客" : "符匠"); }
        public void ShowTitle()
        {
            battle = null; run = null; NewScreen("Title"); TiledBackground();
            var logo = Label(ui, 20, 62, 600, "逐年", 3.5f, Hex(0xffe9a4), 1);
            SleighMotion.Add(logo.rectTransform, SleighMotion.Style.Float, 2, 1.5f);
            Label(ui, 100, 122, 440, "除夕之战 · Hunt the Nian", 1.1f, White, 1);
            titleFireworkTimer = .65f; titleFireworkIndex = 0;
            Label(ui, 180, 165, 280, "24 NIGHTS TO SAVE CHRISTMAS", .75f, White, 1);
            var saved = Run.Load(); bool canContinue = saved != null && saved.active && saved.hp > 0 && saved.night < 24;
            Button(ui, 240, 196, 160, 24, "NEW RUN", ShowCharacters, Hex(0xd94332), fontScale: 1f);
            if (canContinue) Button(ui, 240, 227, 160, 22, "CONTINUE / DEC " + (saved.night + 1), () => { run = saved; ShowMap(); }, fontScale: 1f);
            Button(ui, 240, canContinue ? 256 : 227, 160, 22, "HOW TO PLAY", ShowHelp, fontScale: 1f);
            Button(ui, 240, canContinue ? 285 : 256, 160, 22, "SETTINGS", ShowSettings, fontScale: 1f);
            Button(ui, 240, canContinue ? 314 : 285, 160, 22, "LEDGER", () => OpenLedger(), fontScale: 1f);
        }
        void ShowCharacters()
        {
            NewScreen("CharacterSelect"); TiledBackground();
            string id = characters[characterIndex];
            Label(ui, 40, 16, 560, "WHO WILL SAVE CHRISTMAS?", 1.5f, Gold, 1);
            var panel = Group(ui, "Guardian information", 444, 50, 182, 244);
            Rect(panel, 0, 0, 182, 244, Gold); Rect(panel, 2, 2, 178, 240, Hex(0x252e49, .97f)); Appear(panel);
            Label(panel, 10, 12, 162, DB.CharacterName(id), 1, Gold, 1);
            Rect(panel, 12, 34, 158, 1, Hex(0x8f765d));
            string[] blurbs = { "Runs the North Pole while Santa\ntakes all the credit.", "Head toymaker of Santa's\nworkshop and his trusted advisor.", "The reindeer with the brightest\nnose and the biggest heart." };
            Label(panel, 12, 45, 158, blurbs[characterIndex], .8f, White, 0, true);
            string relic = id == "mrs" ? "cookietin" : id == "tinsel" ? "toolbelt" : "rednose";
            Label(panel, 12, 94, 158, "生命：" + DB.CharacterHP(id), .85f, White);
            Label(panel, 12, 115, 158, "遗物：" + DB.Relics[relic].name, .8f, Gold, 0, true);
            Rect(panel, 12, 142, 158, 1, Hex(0x8f765d));
            Label(panel, 12, 153, 158, DB.PowerName(id), .95f, Ice);
            Label(panel, 12, 178, 158, DB.PowerDesc(id), .73f, White, 0, true);
            for (int i = 0; i < 3; i++)
            {
                int idx = i; float x = 90 + i * 145;
                Rect(world, x - 33, 277, 66, 2, i == characterIndex ? Gold : Hex(0x77758c));
                var a = Sprite(world, "Claus", DB.CharacterArt(characters[i]), x, 278, 1.25f);
                if (a) { a.color = i == characterIndex ? Color.white : Hex(0xbec5d2); if (i == characterIndex) SleighMotion.Add(a.rectTransform, SleighMotion.Style.Pulse, .012f, 2.2f); }
                Rect(ui, x - 54, 284, 108, 18, Hex(0x252e49, .94f));
                Label(ui, x - 52, 287, 104, DB.CharacterName(characters[i]), .8f, i == characterIndex ? Gold : White, 1);
                Hot(ui, x - 54, 118, 108, 185, () => { characterIndex = idx; ShowCharacters(); });
            }
            int unlocked = PlayerPrefs.GetInt("Sleigh.Advent", 0); advent = Mathf.Clamp(advent, 0, unlocked);
            Rect(ui, 120, 304, 220, 22, Hex(0x252e49, .92f));
            Button(ui, 126, 305, 22, 19, "-", () => { advent = Mathf.Max(0, advent - 1); ShowCharacters(); });
            Label(ui, 154, 309, 155, advent == 0 ? "ADVENT OFF" : "ADVENT " + advent, .8f, Gold, 1);
            Button(ui, 316, 305, 22, 19, "+", () => { advent = Mathf.Min(unlocked, advent + 1); ShowCharacters(); }, null, advent < unlocked);
            if (advent > 0) BindTip(Hot(ui, 151, 305, 160, 19, () => ShowItem("ADVENT " + advent, string.Join("\n", DB.Advent.Take(advent)), "calendar")), "Advent " + advent, string.Join("\n", DB.Advent.Take(advent)));
            Button(ui, 45, 330, 160, 23, "< BACK", ShowTitle, fontScale: .95f);
            Button(ui, 225, 330, 160, 23, "BEGIN >", BeginRun, Hex(0x398268), fontScale: .95f);
        }
        void BeginRun()
        {
            run = Run.New(characters[characterIndex], advent, (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000000)); run.Save(); ShowIntro();
        }
        void ShowIntro()
        {
            NewScreen("Intro"); TiledBackground(true); Sprite(bg, "BGImage", "moon", 540, 76);
            string[] first = { "每逢岁末，年兽穿过岁门，吞噬人间灯火。", "今年除夕，守护村镇的岁火意外熄灭。", "年关失守，妖邪沿古道而下，万家归途断绝。", "你带着最后一盏岁灯，踏上二十四关的寻火之路。" };
            string[] second = { "腊月古镇、雪封山道、灯火集市、岁门禁地。", "散落的岁火，藏在尚未放弃团圆的人心里。",
                run.character == "mrs" ? "阿照接过先师的残灯，学会以春符安魂。" : run.character == "tinsel" ? "符匠青禾背起百工囊，要把旧手艺送回千家。" : "巡山客赤行提灯入雪，要为失散的山灵寻回旧约。",
                "爆竹驱煞，门神护身；一张法牌，也能点亮一户灯。" };
            string[] third = { "怨气并非不可化解，安抚后的妖灵也愿与你同行。", "但若将年兽的妖壳强行击碎，噬岁之焰便会苏醒。", "若能唤回它的守岁旧约，岁门后的真魇才会现身。", "无论走哪条路，都要让新年的第一缕光照进人间。" };
            PlayPoem(new[] { first, second, third }, ShowMap);
        }
        void PlayPoem(string[][] stanzas, Action onDone)
        {
            var group = Group(ui, "Original poem"); var lines = new List<PixelLabel>(); var offsets = new List<float>(); float offset = 0;
            foreach (var stanza in stanzas) { foreach (var line in stanza) { lines.Add(Label(group, 20, 288 + offset, 600, line, 1, lines.Count % 2 == 0 ? Hex(0xf6ead2) : White, 1)); offsets.Add(offset); offset += 20; } offset += 16; }
            bool done = false; Action finish = () => { if (done) return; done = true; skipPoem = null; onDone(); };
            skipPoem = finish; Button(ui, 530, 330, 100, 20, "SKIP", finish);
            Label(ui, 150, 338, 340, "hold to speed up", .75f, Muted, 1);
            if (Application.isPlaying) StartCoroutine(ScrollPoem(group, lines, offsets, () => done, finish));
        }
        IEnumerator ScrollPoem(RectTransform group, IList<PixelLabel> lines, IList<float> offsets, Func<bool> done, Action finish)
        {
            float scroll = -1, stopAt = offsets.Last() + 308;
            while (!done() && group && scroll < stopAt)
            {
                float fast = modal == null && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return)) ? 5 : 1;
                scroll += 13 * fast * Time.unscaledDeltaTime;
                for (int i = 0; i < lines.Count; i++) { float y = 288 + offsets[i] - scroll; lines[i].rectTransform.anchoredPosition = new Vector2(20, -Mathf.Round(y)); var c = lines[i].color; c.a = Mathf.Clamp01((y - 34) / 50) * Mathf.Clamp01((360 - y) / 40); lines[i].color = c; }
                yield return null;
            }
            if (!done() && group) finish();
        }
        void FinishNode()
        { CloseModal(); battle = null; run.FinishNode(); if (run.night >= 24) ShowEnding(true); else ShowMap(); }
        void EnterNode(string type)
        {
            run.Save(); run.map[run.night].chosen = type; if (run.night > 0) run.AddSpirit(-(1 + (run.Has("treestar") ? 1 : 0)));
            switch (type)
            { case "battle": StartBattle(run.Encounter(run.night < 3 ? "weak" : "normal"), "normal"); break;
                case "elite": StartBattle(run.Encounter("elite"), "elite"); break; case "boss": StartBattle(new[] { "santa" }, "boss"); break;
                case "event": ShowEvent(run.Event()); break; case "shop": ShowShop(true); break; case "rest": restMessage = null; ShowRest(); break; case "treasure": ShowTreasure(); break; }
        }
        void ShowHelp()
        {
            var m = OpenModal("HOW TO SAVE CHRISTMAS");
            Label(m, 72, 58, 496, "消耗灯火打出法牌：点击后选择目标，也可拖放。灯火每回合恢复；格挡在下回合开始时清空。E结束回合，P释放蓄满的绝技。\n\n给法牌附上符印。用妖灵喜爱的法牌或红绳结消除怨气；息怒后留它一回合不受伤，它就会结盟。再次攻击会失去信任。收服奖励能获得伙伴牌。\n\n战后可留下法牌，为沿途人家点灯；符合心愿额外获得4岁火。行囊至少保留5张。岁火逐关消耗：低于25少抽1张；70起首回合多1灯火；90起每回合多1灯火。它也决定噬岁年兽的强度。\n\n穿过24关。击破年兽进入狂暴决战；以红绳安抚年兽则开启岁门魇路线，途中伙伴会前来助阵。", .86f, White, 0, true);
            Button(m, 240, 318, 160, 24, "LET'S GO", CloseModal, Hex(0x398268));
        }
        void ShowSettings()
        {
            if (busy) return; var m = OpenModal("SETTINGS");
            void volume(string name, float y, Func<float> get, Action<float> set)
            {
                Label(m, 160, y + 4, 220, name + " " + Mathf.RoundToInt(get() * 100) + "%", .9f);
                Button(m, 390, y, 24, 20, "-", () => { set(Mathf.Clamp01(get() - .1f)); sound.Apply(); ShowSettings(); });
                Button(m, 424, y, 24, 20, "+", () => { set(Mathf.Clamp01(get() + .1f)); sound.Apply(); ShowSettings(); });
            }
            volume("MASTER", 64, () => sound.master, v => sound.master = v); volume("MUSIC", 94, () => sound.musicVolume, v => sound.musicVolume = v); volume("EFFECTS", 124, () => sound.effectsVolume, v => sound.effectsVolume = v);
            Button(m, 160, 158, 288, 23, "MUTE: " + (sound.mute ? "ON" : "OFF"), () => { sound.mute = !sound.mute; sound.Apply(); ShowSettings(); });
            Button(m, 160, 188, 288, 23, "REDUCE MOTION: " + (reduceMotion ? "ON" : "OFF"), () => { reduceMotion = !reduceMotion; PlayerPrefs.SetInt("Sleigh.ReduceMotion", reduceMotion ? 1 : 0); ShowSettings(); });
            Button(m, 160, 218, 288, 23, "FAST ENEMY: " + (fastEnemy ? "ON" : "OFF"), () => { fastEnemy = !fastEnemy; PlayerPrefs.SetInt("Sleigh.FastEnemy", fastEnemy ? 1 : 0); ShowSettings(); });
            Button(m, 160, 248, 288, 23, "CONFIRM END TURN: " + (confirmEnd ? "ON" : "OFF"), () => { confirmEnd = !confirmEnd; PlayerPrefs.SetInt("Sleigh.ConfirmEnd", confirmEnd ? 1 : 0); ShowSettings(); });
            Button(m, 160, 307, 134, 24, "BACK", CloseModal, Hex(0x398268));
            if (run != null && run.active) Button(m, 314, 307, 134, 24, "TITLE", () => Confirm("Return to title? Continue resumes from the start of this night.", ShowTitle));
        }
        void Confirm(string text, Action yes)
        { var m = OpenModal("ARE YOU SURE?"); Label(m, 120, 115, 400, text, 1, White, 1, true); Button(m, 170, 250, 140, 26, "YES", () => { CloseModal(); yes(); }, Hex(0xd94332)); Button(m, 330, 250, 140, 26, "BACK", CloseModal); }
    }
}
