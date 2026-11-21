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
        [SerializeField] string menuTitle = "爆竹驱年";
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
        int characterIndex, advent, newRunSlot = -1;
        readonly string[] characters = { "mrs", "rudolph" };
        readonly Dictionary<Unit, ActorVisual> actors = new Dictionary<Unit, ActorVisual>();
        Action modalBack;
        Action skipPoem;
        string tipContent;

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
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= PrepareForEditorReload;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += PrepareForEditorReload;
#endif
        }
#if UNITY_EDITOR
        void OnEnable()
        {
            // Script reload preserves scene objects, but not Awake/Start's runtime bindings.
            if (Application.isPlaying && !Instance && stage && stage.canvas)
            {
                Awake();
                if (enabled) StartCoroutine(RestoreEditorMenu());
            }
        }
        IEnumerator RestoreEditorMenu()
        {
            // Audio and other scene components must finish their OnEnable recovery first.
            yield return null;
            if (enabled && Instance == this) ShowTitle();
        }
#endif
        void Start() { if (stage && ScreenName == null) ShowTitle(); }
        public void BindStage(SleighStage value, bool baking)
        {
            stage = value; IsBaking = baking; canvas = value.canvas; area = value.viewport; sound = value.audioPlayer;
            fx = value.effectPool; pop = value.popupPool; uiScale = -1;
            if (Application.isPlaying && !baking) uiScale = stage.FitViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            if (value.snowRoot) value.snowRoot.gameObject.SetActive(false);
        }
        void Update()
        {
            if (!area) return;
            uiScale = stage.FitViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            foreach (var a in actors.Values) a.Tick(reduceMotion);
            TickPresentation();
            if (tip && tip.gameObject.activeSelf)
            {
                Vector2 p = LocalPointer(Input.mousePosition); float height = tip.sizeDelta.y;
                tip.anchoredPosition = new Vector2(Mathf.Clamp(p.x + 14, 4, 636 - tip.sizeDelta.x), -Mathf.Clamp(p.y - height - 8, 45, Mathf.Max(45, 354 - height))); tip.SetAsLastSibling();
            }
            if (busy || stage.IsTransitioning) return;
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
            else if (ScreenName == "Title" && Input.GetKeyDown(KeyCode.Return)) Navigate(ShowCharacters);
            else if (ScreenName == "CharacterSelect")
            { if (Input.GetKeyDown(KeyCode.LeftArrow)) { characterIndex = (characterIndex + characters.Length - 1) % characters.Length; ShowCharacters(); } if (Input.GetKeyDown(KeyCode.RightArrow)) { characterIndex = (characterIndex + 1) % characters.Length; ShowCharacters(); } if (Input.GetKeyDown(KeyCode.Return)) Navigate(BeginRun); }
            else if (ScreenName == "Map")
            { if (Input.GetKey(KeyCode.RightArrow)) MoveMap(300 * Time.unscaledDeltaTime); if (Input.GetKey(KeyCode.LeftArrow)) MoveMap(-300 * Time.unscaledDeltaTime); }
        }
        void OnApplicationQuit() { PlayerPrefs.Save(); }
        void OnApplicationFocus(bool focused) { if (!focused) ResetPointerInteraction(); }
        void OnApplicationPause(bool paused) { if (paused) ResetPointerInteraction(); }
        void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= PrepareForEditorReload;
#endif
            ReleaseActorMaterials(); if (Instance == this) Instance = null;
        }
        void Navigate(Action next)
        {
            if (IsBaking || !Application.isPlaying) next?.Invoke();
            else stage.Navigate(next);
        }
        Vector2 LocalPointer(Vector2 screen)
        { RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var p); return new Vector2(p.x + 320, 180 - p.y); }
        void NewScreen(string name, string music = "calm", bool preserveLayout = false)
        {
            if (Application.isPlaying) StopAllCoroutines();
            ResetPointerInteraction();
            HideTip(); CloseModal(); skipPoem = null; ScreenName = name; busy = false; selectedGift = null; powerAim = false; potionAim = -1;
            var view = stage.Show(name); bg = view.background; world = view.world; ui = view.ui;
            if (!preserveLayout) foreach (var layer in new[] { bg, world, ui }) Clear(layer);
            foreach (var layer in new[] { fx, pop }) Clear(layer);
            actors.Clear(); handViews.Clear(); handOrder.Clear(); displayedUnits.Clear(); assistingGuest = null; usingVisualNumbers = false; ambientTimer = .2f; ambientFireTimer = .25f; floatingRelic = null; tip = null; sound.Duck(1); sound.Music(music); stage.Transition();
            if (!preserveLayout) Rect(bg, 0, 0, 640, 360, Hex(0x060914));
        }
        void TiledBackground(bool intro = false, float up = 0)
        {
            if (ScreenName == "Combat" || ScreenName == "Map")
            {
                // Resolve the imported asset directly, including when art is reimported in an open editor session.
                string asset = ScreenName == "Combat" ? "FestivalBattleBackground" : "FestivalMapBackground";
                var sprite = Resources.Load<Sprite>("Sleigh/Art/Backgrounds/" + asset);
                if (!sprite) throw new InvalidOperationException("Missing festival background: " + asset);
                var backdrop = Ensure<Image>(Acquire(bg, "FestivalScenery", asset, 0, 0, 640, 360));
                backdrop.sprite = sprite; backdrop.type = Image.Type.Simple;
                backdrop.color = Color.white; backdrop.raycastTarget = false;
            }
            else SpringFestivalBackdrop.Build(bg, SpringFestivalBackdrop.Setting.Village, intro);
        }
        void BindTip(Hotspot h, string title, string description)
        {
            Action enter = h.enter, exit = h.exit;
            h.enter = () => { enter?.Invoke(); ShowTip(title + "\n\n" + description); };
            h.exit = () => { exit?.Invoke(); HideTip(); };
        }
        void ShowTip(string content)
        {
            content = Chinese.Text(content);
            if (busy) return; tipContent = content;
            if (!tip)
            {
                tip = PanelBox(pop, 0, 0, 264, 100, Gold);
                tipText = Label(tip, 9, 8, 246, content, 1, White, 0, true);
            }
            tipText.Set(content); float h = 22 + PixelLabel.Lines(content, 246, 1, true).Count * 12;
            tip.sizeDelta = new Vector2(264, h);
            var imgs = tip.GetComponentsInChildren<Image>();
            if (imgs.Length >= 3) { imgs[0].rectTransform.sizeDelta = new Vector2(264, h); imgs[1].rectTransform.sizeDelta = new Vector2(262, h - 2); imgs[2].rectTransform.sizeDelta = new Vector2(258, h - 6); }
            tip.gameObject.SetActive(true); tip.SetAsLastSibling();
        }
        void HideTip() { tipContent = null; if (tip) tip.gameObject.SetActive(false); }
        RectTransform OpenModal(string title, Action back = null, float titleScale = 1.1f)
        {
            ResetPointerInteraction(); CloseModal(); HideTip(); modal = Group(pop, "Modal:" + title);
            stage.CoverScreen(Rect(modal, 0, 0, 640, 360, Hex(0x21121b, .94f), true).rectTransform);
            modalBack = back ?? CloseModal; Label(modal, 30, 18, 580, title, titleScale, Gold, 1);
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
            Rect(ui, 0, 0, 640, 26, Hex(0x311b25, .96f)); Rect(ui, 0, 25, 640, 1, Hex(0x865044));
            Sprite(ui, "Icon", "heart", 15, 13); hudHp = Label(ui, 26, 7, 92, run.hp + "/" + run.maxHp, .8f);
            Sprite(ui, "Icon", "coin", 116, 13); hudCookies = Label(ui, 127, 7, 58, run.cookies.ToString(), .8f, Gold);
            for (int i = 0; i < run.potionSlots; i++)
            {
                int slot = i; string id = run.potions[i]; float x = 184 + i * 19;
                if (string.IsNullOrEmpty(id))
                {
                    Sprite(ui, "Icon", "potion_empty", x, 13);
                    BindTip(Hot(ui, x - 8, 3, 17, 22, null), "空葫芦位", "可携带一份年节补给。获得补给后，葫芦会亮起。");
                    continue;
                }
                Sprite(ui, "Icon", "potion_" + id, x, 13);
                BindTip(Hot(ui, x - 8, 3, 17, 22, () => PotionMenu(slot)), DB.Potions[id].name, DB.Potions[id].desc + "\nClick to drink or discard.");
            }
            Sprite(ui, "Icon", "tree", 274, 13);
            hudSpirit = Label(ui, 285, 7, 140, "SPIRIT " + run.spirit + " " + run.SpiritName, .7f, run.SpiritTier >= 2 ? Green : Pink);
            BindTip(Hot(ui, 266, 1, 159, 23, () => ShowSpirit()), "Christmas Spirit", "Fades by 1 each night.\n0-24: draw 1 fewer.\n25-49: Krampus grows stronger.\n50-69: Merry.\n70-89: +1 Cheer on turn 1.\n90-100: +1 Cheer each turn.\nLeave gifts and fulfill wishes to raise it.");
            var bagButton = Button(ui, 435, 3, 66, 19, "", () => OpenBag(run.bag, "YOUR SACK"));
            var bagRoot = bagButton.transform.parent;
            Sprite(bagRoot, "Icon", "sack", 10, 9, 1, true, 13, 14);
            Label(bagRoot, 20, 5, 44, "年礼 " + run.bag.Count, .7f, White, 1);
            BindTip(bagButton, "年礼福袋", "Tab: view your permanent deck.");
            var ledgerButton = Button(ui, 507, 3, 66, 19, "", () => OpenLedger());
            Sprite(ledgerButton.transform.parent, "Icon", "ledger", 10, 9, 1, true, 13, 13);
            Label(ledgerButton.transform.parent, 20, 5, 44, "图鉴", .75f, White, 1);
            var menuButton = Button(ui, 579, 3, 53, 19, "", () => ShowSettings());
            Sprite(menuButton.transform.parent, "Icon", "menu", 10, 9, 1, true, 13, 13);
            Label(menuButton.transform.parent, 20, 5, 31, "菜单", .75f, White, 1);
            for (int i = 0; i < run.relics.Count; i++)
            {
                var r = run.relics[i]; float x = 12 + i % 27 * 19, y = 36 + i / 27 * 18;
                Sprite(ui, "Icon", "relic_" + r.id, x, y);
                BindTip(Hot(ui, x - 8, y - 8, 17, 17, () => ShowItem(DB.Relics[r.id].name, DB.Relics[r.id].desc, "relic_" + r.id)), DB.Relics[r.id].name, DB.Relics[r.id].desc);
            }
            Label(ui, 530, 31, 100, "DEC " + Mathf.Min(24, run.night + 1) + " / 24", .7f, Muted, 2);
        }
        void ShowSpirit() => ShowItem("CHRISTMAS SPIRIT: " + run.spirit, "Current tier: " + run.SpiritName + ".\n\nSpirit fades every night. Leave gifts to keep Christmas alive.\n\nGrim: draw 1 fewer. Fading: Krampus grows stronger. Merry: kids still believe. Festive: +1 Cheer on the first turn. Jolly: +1 Cheer every turn.", "tree");
        void ShowItem(string title, string text, string icon)
        { var m = OpenModal(title); Sprite(m, "Icon", icon, 320, 90, 3); Label(m, 110, 130, 420, text, 1, White, 1, true); Button(m, 250, 308, 140, 24, "BACK", CloseModal); }
        string CharacterText(string s)
        { string id = run?.character ?? characters[characterIndex]; return Chinese.Text(s).Replace("{nick}", DB.CharacterName(id)).Replace("{name}", DB.CharacterName(id)).Replace("{title}", DB.CharacterName(id)); }
        public void ShowTitle()
        {
            var titleView = stage.views.First(v => v.name == "Title").ui.GetComponent<SpringFestivalTitleView>();
            bool authored = !IsBaking && titleView && titleView.IsReady;
            battle = null; run = null; newRunSlot = -1; NewScreen("Title", "menu", preserveLayout: authored);
            if (!authored)
            {
                SpringFestivalMenu.Build(bg, ui, menuTitle);
                titleView = PixelUI.Ensure<SpringFestivalTitleView>(ui);
                titleView.SetAuthoredButtons(new[] {
                    SpringFestivalMenu.MenuButton(ui, 180, 194, 264, 28, "NEW RUN", null, 1.25f),
                    SpringFestivalMenu.MenuButton(ui, 188, 234, 248, 26, "CONTINUE", null, 1.15f),
                    SpringFestivalMenu.MenuButton(ui, 166, 272, 76, 22, "玩法", null),
                    SpringFestivalMenu.MenuButton(ui, 382, 272, 76, 22, "SETTINGS", null),
                    SpringFestivalMenu.MenuButton(ui, 274, 272, 76, 22, "LEDGER", null)
                });
            }
            titleView.Bind(() => Navigate(ShowCharacters), () => ShowSaveSlots(false), ShowHelp, ShowSettings, () => OpenLedger());
        }
        void ShowCharacters()
        {
            NewScreen("CharacterSelect", "menu"); Sprite(bg, "Background", "stage", 0, 0, 1, false);
            string id = characters[characterIndex];
            Label(ui, 40, 16, 560, "WHO WILL SAVE CHRISTMAS?", 1.5f, Gold, 1);
            var panel = PanelBox(ui, 170, 45, 300, 96, id == "tinsel" ? Green : Red); Appear(panel);
            Label(panel, 8, 6, 284, DB.CharacterName(id).ToUpperInvariant(), 1, Gold, 1);
            string[] blurbs = { "Runs the North Pole while Santa\ntakes all the credit.", "The reindeer with the brightest\nnose and the biggest heart." };
            Label(panel, 8, 23, 284, blurbs[characterIndex], .85f, White, 1);
            string relic = id == "mrs" ? "cookietin" : id == "tinsel" ? "toolbelt" : "rednose";
            Label(panel, 8, 60, 284, DB.CharacterHP(id) + " HP / " + DB.Relics[relic].name, .8f, Gold, 1);
            Label(panel, 8, 77, 284, "Power: " + DB.PowerName(id), .8f, Pink, 1);
            for (int i = 0; i < characters.Length; i++)
            {
                int idx = i; float x = 320 + (i - (characters.Length - 1) * .5f) * 150;
                var a = Sprite(world, "Claus", DB.CharacterArt(characters[i]), x, 288, 1.5f);
                if (a) { a.color = i == characterIndex ? Color.white : Hex(0x666676); if (i == characterIndex) SleighMotion.Add(a.rectTransform, SleighMotion.Style.Pulse, .018f, 2.2f); }
                var hot = Hot(ui, x - 50, 150, 100, 138, () => { characterIndex = idx; ShowCharacters(); }); BindTip(hot, DB.PowerName(characters[i]), DB.PowerDesc(characters[i]));
            }
            int unlocked = PlayerPrefs.GetInt("Sleigh.Advent", 0); advent = Mathf.Clamp(advent, 0, unlocked);
            Button(ui, 228, 295, 22, 19, "-", () => { advent = Mathf.Max(0, advent - 1); ShowCharacters(); });
            Label(ui, 255, 298, 130, advent == 0 ? "ADVENT OFF" : "ADVENT " + advent, .8f, Gold, 1);
            Button(ui, 390, 295, 22, 19, "+", () => { advent = Mathf.Min(unlocked, advent + 1); ShowCharacters(); }, null, advent < unlocked);
            if (advent > 0) BindTip(Hot(ui, 250, 294, 140, 22, () => ShowItem("ADVENT " + advent, string.Join("\n", DB.Advent.Take(advent)), "calendar")), "Advent " + advent, string.Join("\n", DB.Advent.Take(advent)));
            Button(ui, 160, 327, 150, 24, "< BACK", () => Navigate(ShowTitle)); Button(ui, 330, 327, 150, 24, "BEGIN >", () => Navigate(BeginRun), Hex(0x1c5a3a));
        }
        void BeginRun()
        {
            if (newRunSlot >= 0 && !Run.HasSave(newRunSlot)) StartRunInSlot(newRunSlot);
            else ShowSaveSlots(true);
        }
        void ShowIntro()
        {
            NewScreen("Intro"); TiledBackground(true);
            PlayPoem(FestivalOpening(run.character), () => Navigate(ShowMap));
        }
        void PlayPoem(string[][] stanzas, Action onDone)
        {
            PlayFestivalStory(stanzas, onDone);
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
            var m = OpenModal("HOW TO SAVE CHRISTMAS", titleScale: 1.4f);
            Label(m, 56, 62, 528, "Give Gifts using Cheer. Choose a gift, then a monster; drag-and-drop also works. Cheer refills each turn. Wrap absorbs damage until your next turn. E ends the turn. P uses your charged power.\n\nFit toy parts to gifts. Give monsters things they love to pop Grumpy pips. A Softened monster skips its turn and joins you if you leave it unharmed. Hitting it breaks its trust.\n\nTake rewards, then leave a gift for a child. Fulfill their wish for +4 Spirit. Your sack needs at least 5 gifts. Spirit fades nightly and changes Krampus's strength.\n\nBeat all 24 nights. Can you bring Santa home without hurting him?", 1.2f, White, 0, true);
            Button(m, 240, 314, 160, 28, "LET'S GO", CloseModal, Hex(0x1c5a3a), captionScale: 1.05f);
        }
        void ShowSettings()
        {
            if (busy) return; var m = OpenModal("SETTINGS", titleScale: 1.4f);
            void volume(string name, float y, Func<float> get, Action<float> set)
            {
                Label(m, 160, y + 6, 208, name + " " + Mathf.RoundToInt(get() * 100) + "%", 1.15f);
                Button(m, 380, y, 32, 28, "-", () => { set(Mathf.Clamp01(get() - .1f)); sound.Apply(); ShowSettings(); }, captionScale: 1.1f);
                Button(m, 422, y, 32, 28, "+", () => { set(Mathf.Clamp01(get() + .1f)); sound.Apply(); ShowSettings(); }, captionScale: 1.1f);
            }
            volume("MASTER", 56, () => sound.master, v => sound.master = v); volume("MUSIC", 90, () => sound.musicVolume, v => sound.musicVolume = v); volume("EFFECTS", 124, () => sound.effectsVolume, v => sound.effectsVolume = v);
            Button(m, 160, 160, 288, 28, "MUTE: " + (sound.mute ? "ON" : "OFF"), () => { sound.mute = !sound.mute; sound.Apply(); ShowSettings(); }, captionScale: 1.05f);
            Button(m, 160, 194, 288, 28, "REDUCE MOTION: " + (reduceMotion ? "ON" : "OFF"), () => { reduceMotion = !reduceMotion; PlayerPrefs.SetInt("Sleigh.ReduceMotion", reduceMotion ? 1 : 0); ShowSettings(); }, captionScale: 1.05f);
            Button(m, 160, 228, 288, 28, "FAST ENEMY: " + (fastEnemy ? "ON" : "OFF"), () => { fastEnemy = !fastEnemy; PlayerPrefs.SetInt("Sleigh.FastEnemy", fastEnemy ? 1 : 0); ShowSettings(); }, captionScale: 1.05f);
            Button(m, 160, 262, 288, 28, "CONFIRM END TURN: " + (confirmEnd ? "ON" : "OFF"), () => { confirmEnd = !confirmEnd; PlayerPrefs.SetInt("Sleigh.ConfirmEnd", confirmEnd ? 1 : 0); ShowSettings(); }, captionScale: 1.05f);
            Button(m, 160, 314, 134, 28, "BACK", CloseModal, Hex(0x1c5a3a), captionScale: 1.05f);
            if (run != null && run.active) Button(m, 314, 314, 134, 28, "TITLE", () => Confirm("Return to title? Continue resumes from the start of this night.", () => Navigate(ShowTitle)), captionScale: 1.05f);
        }
        void Confirm(string text, Action yes)
        { var m = OpenModal("ARE YOU SURE?"); Label(m, 120, 115, 400, text, 1, White, 1, true); Button(m, 170, 250, 140, 26, "YES", () => { CloseModal(); yes(); }, Hex(0x8e1d2b)); Button(m, 330, 250, 140, 26, "BACK", CloseModal); }
    }
}
