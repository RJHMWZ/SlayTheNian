using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        Gift selectedGift;
        Unit hoveredUnit;
        bool powerAim;
        int potionAim = -1;
        RectTransform dragGhost, handRoot, battleControls;
        int dragPointer = int.MinValue;
        PixelLabel orbNumber, drawNumber, discardNumber, consumedNumber, comboNumber;
        Image powerFill;
        Image assistingGuest;
        float guestHomeX, guestKick;
        RectTransform powerButton;
        readonly Dictionary<Unit, Unit> displayedUnits = new Dictionary<Unit, Unit>();
        readonly Dictionary<int, HandVisual> handViews = new Dictionary<int, HandVisual>();
        readonly List<Gift> presentedHand = new List<Gift>(10);
        readonly List<HandVisual> handOrder = new List<HandVisual>(10);
        readonly Dictionary<Material, Material> actorMaterials = new Dictionary<Material, Material>();
        int hoveredCard = int.MinValue, comboCount;
        float heartbeatAt, hitPause;
        bool usingVisualNumbers;
        int viewHp, viewMaxHp, viewEnergy, viewPower, viewCookies, viewSpirit, viewDraw, viewDiscard, viewConsumed;

        sealed class HandVisual
        { public Gift gift; public RectTransform root; public Image illustration; public float x, y, rotation, scale = 1; public bool drawing; }
        sealed class ActorVisual
        {
            static readonly int FlashProperty = Shader.PropertyToID("_Flash");
            public Unit unit, frame; public RectTransform root, hud; public Image art, ghostFill, healthFill, previewFill;
            public Image[] hearts, pips, pipPreview; public PixelLabel hpLabel, intentLabel;
            public float x, y = 226, offsetX, offsetY, sx = 1, sy = 1, alpha = 1, flash, ghost, ghostDelay;
            public bool hidden;
            float lastFlash = float.NaN;
            public float Height => art ? art.rectTransform.sizeDelta.y : 70;
            public float Width => art ? art.rectTransform.sizeDelta.x : 60;
            public void Tick(bool reduced)
            {
                if (!root || hidden || !art) return;
                float dt = Time.unscaledDeltaTime, breath = reduced ? 0 : Mathf.Sin(Time.unscaledTime * 2.2f + x) * .018f;
                root.anchoredPosition = new Vector2(Mathf.Round(x + (reduced ? 0 : offsetX)), -Mathf.Round(y + (reduced ? 0 : offsetY)));
                root.localScale = reduced ? Vector3.one : new Vector3(sx * (1 - breath * .5f), sy * (1 + breath), 1);
                art.color = new Color(1, 1, 1, alpha);
                float visibleFlash = reduced ? 0 : flash;
                if (lastFlash != visibleFlash)
                {
                    if (art.material && art.material.HasProperty(FlashProperty)) art.material.SetFloat(FlashProperty, visibleFlash);
                    lastFlash = visibleFlash;
                }
                if (frame != null && ghostFill)
                {
                    if (ghost > frame.Hp) { ghostDelay += dt; if (ghostDelay > .35f) ghost = Mathf.Max(frame.Hp, ghost - dt * frame.MaxHp * .9f); }
                    else { ghost = frame.Hp; ghostDelay = 0; }
                    ghostFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(56 * Mathf.Max(0, ghost) / frame.MaxHp), 5);
                }
                if (intentLabel)
                    intentLabel.rectTransform.localScale = Vector3.one * (reduced ? 1 : 1 + (frame != null && frame.Soft ? .03f : .015f) * Mathf.Sin(Time.unscaledTime * 3 + x));
            }
        }
        Unit Display(Unit u) => displayedUnits.TryGetValue(u, out var f) ? f : u;
        public void StartBattle(string[] ids, string kind)
        {
            NewScreen("Combat", kind == "boss" ? "boss" : "battle"); TiledBackground();
            battle = new Battle(run, ids, kind); presentedHand.Clear(); usingVisualNumbers = true; comboCount = 0;
            viewHp = run.hp; viewMaxHp = run.maxHp; viewCookies = run.cookies; viewSpirit = run.spirit;
            viewEnergy = 0; viewPower = 40; viewDraw = run.bag.Count; viewDiscard = viewConsumed = 0;
            var player = battle.Player.Snapshot(); player.Status.Clear(); player.Block = 0; displayedUnits[battle.Player] = player;
            foreach (var cue in battle.Cues) if (cue.kind == "spawn") displayedUnits[cue.unit] = cue.frame;
            AddActor(battle.Player);
            foreach (var e in battle.Units) { var a = AddActor(e); if (!reduceMotion) a.sx = a.sy = .01f; }
            RefreshBattleUI();
            if (Application.isPlaying) { var a = actors[battle.Player]; if (!reduceMotion) { a.x = -40; StartCoroutine(Tween(.5f, p => a.x = Mathf.Lerp(-40, 150, EaseOut(p)))); } PlayBattleCues(); }
        }
        ActorVisual AddActor(Unit model)
        {
            if (actors.TryGetValue(model, out var existing)) return existing;
            var frame = Display(model);
            var prefab = Resources.Load<GameObject>("Sleigh/Prefabs/ActorView");
            var root = Acquire(world, "Actor", model.Name, frame.X, 226, 1, 1, prefab);
            var a = new ActorVisual { unit = model, frame = frame, x = frame.X, ghost = frame.Hp, root = root };
            a.art = Sprite(root, frame.IsPlayer ? "Claus" : "Enemy", frame.IsPlayer ? DB.CharacterArt(frame.Id) : frame.Id, 0, 0, frame.IsPlayer ? 1 : frame.Def.scale);
            if (Application.isPlaying && !IsBaking && a.art.material && a.art.material.shader.name == "Sleigh/UI Pixel Flash")
                a.art.material = ActorMaterial(a.art.material);
            a.hearts = new[] { Sprite(root, "HeartEye", "Animation 1", -6, -50), Sprite(root, "HeartEye", "Animation 1", 6, -50) };
            actors.Add(model, a); ConfigureHearts(a); return a;
        }
        Material ActorMaterial(Material source)
        {
            if (actorMaterials.TryGetValue(source, out var material)) return material;
            if (actorMaterials.ContainsValue(source)) return source;
            material = new Material(source) { name = source.name + " (Runtime)", hideFlags = HideFlags.DontSave };
            actorMaterials.Add(source, material);
            return material;
        }
        void ReleaseActorMaterials()
        {
            foreach (var material in actorMaterials.Values)
                if (material) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            actorMaterials.Clear();
        }
#if UNITY_EDITOR
        void PrepareForEditorReload()
        {
            if (actorMaterials.Count == 0) return;
            var originals = actorMaterials.ToDictionary(pair => pair.Value, pair => pair.Key);
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (image.material && originals.TryGetValue(image.material, out var source)) image.material = source;
            foreach (var material in actorMaterials.Values) if (material) DestroyImmediate(material);
            actorMaterials.Clear();
        }
#endif
        void ConfigureHearts(ActorVisual a)
        {
            var f = DB.Frame(a.frame.IsPlayer ? "Claus" : "Enemy", a.frame.IsPlayer ? DB.CharacterArt(a.frame.Id) : a.frame.Id);
            for (int i = 0; i < a.hearts.Length; i++)
            {
                var heart = a.hearts[i]; if (!heart) continue; heart.gameObject.SetActive(a.frame.Charmed);
                Vector2 point = f.eyes != null && i < f.eyes.Length ? f.eyes[i] : new Vector2(.45f + i * .1f, .22f);
                heart.rectTransform.anchoredPosition = new Vector2((point.x - f.pivotX) * a.Width, -(point.y - f.pivotY) * a.Height);
                if (a.frame.Charmed) { var pos = heart.rectTransform.anchoredPosition; pos.x = -pos.x; heart.rectTransform.anchoredPosition = pos; }
            }
            a.art.rectTransform.localScale = new Vector3(a.frame.Charmed ? -1 : 1, 1, 1);
        }
        void UpdateActorArt(ActorVisual a)
        {
            var u = a.frame; var f = DB.Frame(u.IsPlayer ? "Claus" : "Enemy", u.IsPlayer ? DB.CharacterArt(u.Id) : u.Id);
            a.art.sprite = DB.Sprite(u.IsPlayer ? "Claus" : "Enemy", u.IsPlayer ? DB.CharacterArt(u.Id) : u.Id);
            a.art.rectTransform.sizeDelta = new Vector2(f.width, f.height) * (u.IsPlayer ? 1 : u.Def.scale);
            a.art.rectTransform.pivot = new Vector2(f.pivotX, 1 - f.pivotY); ConfigureHearts(a);
        }
        void RefreshBattleUI()
        {
            if (ScreenName != "Combat" || battle == null) return;
            foreach (var actor in actors.Values) { actor.hud = null; actor.healthFill = actor.ghostFill = actor.previewFill = null; actor.pips = actor.pipPreview = null; actor.intentLabel = null; }
            Clear(ui); handViews.Clear(); handOrder.Clear(); Hud();
            Hot(ui, 0, 45, 640, 206, () => { if (!busy) CancelAim(); });
            foreach (var u in new[] { battle.Player }.Concat(battle.Units)) if (!Display(u).Dead) BuildUnitHud(AddActor(u));
            handRoot = Acquire(ui, "BattleHand", "Hand (reused gift slots)");
            var cards = IsBaking ? battle.Hand : usingVisualNumbers ? presentedHand : battle.Hand;
            if (!usingVisualNumbers) { presentedHand.Clear(); presentedHand.AddRange(battle.Hand.Select(g => g.Copy())); }
            foreach (var g in cards.ToArray()) MakeHandVisual(g, false);
            battleControls = Acquire(ui, "BattleControls", "Battle controls");
            Sprite(battleControls, "Icon", battle.Energy > 0 ? "orb" : "orb_empty", 36, 292);
            orbNumber = Label(battleControls, 9, 286, 54, (usingVisualNumbers ? viewEnergy : battle.Energy) + "/" + battle.MaxEnergy, 1, White, 1);
            BindTip(Hot(battleControls, 14, 270, 44, 43, null), "Cheer", "Spent to give gifts. Refills every turn.");
            Sprite(battleControls, "Icon", "sack", 30, 336); drawNumber = Label(battleControls, 42, 330, 45, battle.DrawPile.Count.ToString(), 1);
            BindTip(Hot(battleControls, 8, 317, 70, 35, () => OpenBag(battle.DrawPile.OrderBy(x => x.Name).ToList(), "IN THE SACK (random order)")), "Draw pile", "Click to view. Order is hidden.");
            Sprite(battleControls, "Icon", "sleigh", 610, 336); discardNumber = Label(battleControls, 552, 330, 43, battle.Discard.Count.ToString(), 1, White, 2);
            consumedNumber = Label(battleControls, 554, 316, 62, "×" + battle.Consumed.Count, .75f, Muted, 2);
            BindTip(Hot(battleControls, 551, 314, 80, 41, () => OpenBag(battle.Discard, "IN THE SLEIGH")), "Discard pile", "Refills the Sack when it runs out.");
            var power = Button(battleControls, 546, 258, 88, 20, DB.PowerName(run.character), PressPower, battle.Power >= 100 ? Hex(0xa8741f) : Hex(0x253262));
            powerButton = (RectTransform)power.transform.parent;
            Rect(battleControls, 547, 280, 86, 3, Hex(0x253262)); powerFill = Rect(battleControls, 547, 280, (usingVisualNumbers ? viewPower : battle.Power) * .86f, 3, Pink);
            string nextAid = battle.AidOrder[battle.AidIndex % battle.AidOrder.Length];
            BindTip(power, DB.PowerName(run.character) + " / " + battle.Power + "%", DB.PowerDesc(run.character) + (run.character == "rudolph" ? "\n下一位：" + DB.Enemies[nextAid].name + "\n" + DB.ZodiacAidDescription(nextAid) : ""));
            Button(battleControls, 546, 285, 88, 24, "END TURN", RequestEndTurn, Hex(0x8e1d2b));
            comboNumber = Label(battleControls, 82, 253, 180, comboCount >= 2 ? "GIFT COMBO x" + comboCount : "", .85f, Gold);
            Label(battleControls, 10, 54, 140, "TURN " + battle.Turn, .75f, Muted);
            if (selectedGift != null || powerAim || potionAim >= 0) Label(ui, 125, 70, 390, "CHOOSE A MONSTER / RIGHT CLICK TO CANCEL", .75f, Gold, 1);
            if (usingVisualNumbers) UpdateBattleCounters();
        }
        void BuildUnitHud(ActorVisual a)
        {
            var u = a.frame = Display(a.unit);
            if (!a.hud) a.hud = Acquire(ui, "UnitHUD", "Unit HUD: " + u.Name, u.X, 232, 1, 1); else Clear(a.hud);
            a.hud.gameObject.SetActive(!u.Dead); a.hud.anchoredPosition = new Vector2(a.x, -232);
            if (u.Dead) return;
            var p = a.hud;
            if (!u.Charmed)
            {
                Rect(p, -30, -2, 60, 9, Hex(0x05050b)); Rect(p, -28, 0, 56, 5, Hex(0x3a1018));
                a.ghostFill = Rect(p, -28, 0, 56 * Mathf.Max(0, a.ghost) / u.MaxHp, 5, Color.white);
                a.healthFill = Rect(p, -28, 0, Mathf.Round(56 * Mathf.Max(0, u.Hp) / u.MaxHp), 5, u.Block > 0 ? Hex(0x3b86c8) : Red);
                Rect(p, -28, 0, Mathf.Round(56 * Mathf.Max(0, u.Hp) / u.MaxHp), 1, Hex(0xffffff, .35f));
                a.previewFill = Rect(p, 0, 0, 0, 5, Color.clear);
                int textY = 8;
                if (!u.IsPlayer)
                {
                    Rect(p, -30, 5, 60, 8, Hex(0x05050b)); textY = 15;
                    Rect(p, -28, 7, 56, 4, Hex(0x1a1020));
                    a.pips = new Image[u.PipMax]; a.pipPreview = new Image[u.PipMax];
                    int width = u.PipMax > 0 ? Mathf.Min(10, (56 - u.PipMax + 1) / u.PipMax) : 0;
                    float x0 = -Mathf.Round((u.PipMax * width + u.PipMax - 1) / 2f);
                    for (int i = 0; i < u.PipMax; i++)
                    {
                        a.pips[i] = Rect(p, x0 + i * (width + 1), 7, width, 4, u.Soft ? u.SoftHit ? Hex(0x8a5a6a) : Pink : i < u.Pips ? Hex(0xb05ae0) : Hex(0x3a2848));
                        a.pipPreview[i] = Rect(p, x0 + i * (width + 1), 7, width, 4, Color.clear);
                    }
                    if (u.PipMax == 0) { Rect(p, -28, 7, 56, 4, Hex(0x3a3a44)); for (int x = -26; x < 28; x += 6) Rect(p, x, 8, 3, 2, Hex(0x55556a)); }
                }
                a.hpLabel = Label(p, -46, textY, 92, Mathf.Max(0, u.Hp) + "/" + u.MaxHp, .8f, White, 1);
                if (u.Block > 0) { Sprite(p, "Icon", "shield", -36, 2); Label(p, -75, -3, 34, u.Block.ToString(), 1, Ice, 2); }
                int st = 0;
                foreach (var kv in u.Status)
                {
                    if (kv.Value <= 0) continue; float x = -34 + st % 4 * 23, y = textY + 12 + st / 4 * 13;
                    Sprite(p, "Icon", "st_" + kv.Key, x + 4, y + 6); Label(p, x + 9, y, 22, kv.Value.ToString(), .75f, DB.Debuff(kv.Key) ? Pink : White);
                    int id = Array.IndexOf(DB.StatusIds, kv.Key); if (id >= 0) BindTip(Hot(p, x - 3, y - 2, 24, 13, null), DB.StatusNames[id] + " " + kv.Value, DB.StatusDesc[id]); st++;
                }
            }
            if (!u.IsPlayer)
            {
                float intentY = -a.Height - 15;
                var move = !string.IsNullOrEmpty(u.Move) ? u.Def.Move(u.Move) : null;
                string icon = u.Soft || u.Charmed ? "heart" : move != null && move.actions.Any(ac => ac.op == "atk") ? "intent_attack" : move != null && move.actions.Any(ac => ac.op == "flee") ? "intent_escape" : move != null && move.actions.Any(ac => ac.op == "def") ? "intent_defend" : "intent_buff";
                Sprite(p, "Icon", icon, 0, intentY - 5); string intent = DisplayIntent(u);
                a.intentLabel = Label(p, -66, intentY + 3, 132, intent, .8f, u.Charmed || u.Soft ? Pink : Gold, 1, true);
                var model = a.unit;
                var hot = Hot(p, -Mathf.Max(30, a.Width / 2), -a.Height - 6, Mathf.Max(60, a.Width), a.Height + 23, () => ClickUnit(model), () => { hoveredUnit = model; }, () => { if (hoveredUnit == model) hoveredUnit = null; });
                var entry = Ledger.Get(u.Id);
                BindTip(hot, u.Name, intent + (u.PipMax > 0 ? "\nGrumpy: " + u.Pips + "/" + u.PipMax + "\nLoves: " + (entry.love || u.Id == "santa" ? DB.LikeLabel(u.Love) : "???") + "\nHates: " + (entry.hate || u.Id == "santa" ? DB.LikeLabel(u.Hate) : "???") : "\nCannot be won over.") + (u.Soft ? "\nLeave it unharmed to win it over." : ""));
            }
        }
        string DisplayIntent(Unit u)
        {
            if (u.Soft) return u.SoftHit ? "Softened / won't join" : "Softened / joins if unharmed";
            if (u.SkipTurn) return "Too flustered...";
            if (string.IsNullOrEmpty(u.Move)) return "?";
            var move = u.Def.Move(u.Move); var attack = move.actions.FirstOrDefault(x => x.op == "atk");
            return move.name + (attack == null ? "" : " " + battle.DamageCalc(u, u.Charmed ? null : Display(battle.Player), attack.n) + (attack.hits > 1 ? "×" + attack.hits : ""));
        }
        HandVisual MakeHandVisual(Gift gift, bool drawing)
        {
            if (handViews.TryGetValue(gift.Uid, out var old)) return old;
            int i = handViews.Count; float x = 326 + (i - (presentedHand.Count - 1) * .5f) * Mathf.Min(58, 420f / Mathf.Max(1, presentedHand.Count));
            var r = Card(handRoot, gift, x - 33, 274, 1, battle); r.pivot = new Vector2(.5f, .5f);
            var picture = r.Find("Festival card illustration");
            var v = new HandVisual { gift = gift, root = r, illustration = picture ? picture.GetComponent<Image>() : null, x = drawing ? 30 : x, y = drawing ? 336 : 318, scale = drawing ? .15f : 1, drawing = drawing };
            r.anchoredPosition = new Vector2(v.x, -v.y); r.localScale = Vector3.one * v.scale;
            var hot = Hot(r, 0, 0, 66, 88, () => SelectGift(battle.Hand.Find(g => g.Uid == gift.Uid) ?? gift));
            hot.enter = () => { if (busy) return; hoveredCard = gift.Uid; ShowTip(gift.Name + "\n\n" + GiftTip(gift, battle)); sound.Play("cardHover", Mathf.Max(0, presentedHand.FindIndex(g => g.Uid == gift.Uid))); };
            hot.exit = () => { if (hoveredCard == gift.Uid) hoveredCard = int.MinValue; HideTip(); };
            hot.beginDrag = e =>
            {
                var g = battle.Hand.Find(c => c.Uid == gift.Uid); if (busy || modal || dragGhost || g == null || !battle.Playable(g)) return;
                selectedGift = g; powerAim = false; potionAim = -1; HideTip(); sound.Play("pick");
                dragPointer = e.pointerId;
                var p = LocalPointer(e.position); dragGhost = Card(fx, g, p.x - 33, p.y - 44); var cg = PixelUI.Ensure<CanvasGroup>(dragGhost); cg.alpha = .92f; cg.blocksRaycasts = false;
            };
            hot.drag = e => { if (dragGhost && e.pointerId == dragPointer && selectedGift?.Uid == gift.Uid) { var p = LocalPointer(e.position); dragGhost.anchoredPosition = new Vector2(p.x - 33, -p.y + 44); } };
            hot.endDrag = e =>
            {
                if (e.pointerId != dragPointer || selectedGift?.Uid != gift.Uid) return;
                dragPointer = int.MinValue;
                if (dragGhost) { Release(dragGhost); dragGhost = null; }
                if (busy || selectedGift == null || selectedGift.Uid != gift.Uid) return;
                var p = LocalPointer(e.position); var target = TargetAt(p);
                if (p.y < 252 && (selectedGift.Def.target != "e" || target != null)) PlayGift(selectedGift, target); else CancelAim();
            };
            handViews.Add(gift.Uid, v); handOrder.Add(v); return v;
        }
        void SelectGift(Gift gift)
        {
            if (dragGhost) return;
            if (gift == null || busy || modal || !battle.Playable(gift)) { sound.Play("error"); if (!busy) Toast("Not enough Cheer, or this gift is ungiftable.", Red); return; }
            if (gift.Def.target != "e") { PlayGift(gift, null); return; }
            selectedGift = selectedGift == gift ? null : gift; powerAim = false; potionAim = -1; sound.Play("pick");
            if (selectedGift != null) ShowTip(gift.Name + "\n\n" + GiftTip(gift, battle));
            else HideTip();
        }
        Unit TargetAt(Vector2 point)
        {
            if (battle == null) return null;
            // Same frontmost-foe ordering, without allocating a list and LINQ closures every frame.
            for (int i = battle.Units.Count - 1; i >= 0; i--)
            {
                var u = battle.Units[i];
                if (u.Dead || u.Charmed || u.Pending || !actors.TryGetValue(u, out var a)) continue;
                if (Mathf.Abs(point.x - a.x) < Mathf.Max(32, a.Width / 2) && point.y >= 220 - a.Height && point.y <= 253) return u;
            }
            return null;
        }
        void ResetPointerInteraction()
        {
            if (dragGhost) { Release(dragGhost); dragGhost = null; }
            dragPointer = int.MinValue;
            selectedGift = null; powerAim = false; potionAim = -1;
            hoveredCard = int.MinValue; hoveredUnit = null; previousAim = null; mapFling = 0;
            HideTip();
            if (stage && stage.arrowRoot) stage.arrowRoot.gameObject.SetActive(false);
            if (stage && stage.reactionIcon) stage.reactionIcon.gameObject.SetActive(false);
        }
        void ClickUnit(Unit target)
        {
            if (busy || dragGhost || target.Charmed || target.Dead || target.Pending) return;
            if (selectedGift != null) { PlayGift(selectedGift, target); return; }
            if (powerAim) { powerAim = false; if (battle.UsePower(target)) PlayBattleCues(); return; }
            if (potionAim >= 0) { int slot = potionAim; potionAim = -1; if (battle.UsePotion(slot, target)) PlayBattleCues(); return; }
            ShowItem(target.Name, battle.Intent(target) + "\n\n" + target.Def.lore + "\n\n" + (target.PipMax > 0 ? "Grumpy: " + target.Pips + "/" + target.PipMax + "\nGive it gifts it loves. Leave it unharmed once Softened." : "This enemy cannot be won over."), "intent_attack");
        }
        void PlayGift(Gift gift, Unit target)
        { if (busy) return; HideTip(); if (!battle.Play(gift, target)) { sound.Play("error"); return; } selectedGift = null; hoveredUnit = null; hoveredCard = int.MinValue; PlayBattleCues(); }
        void CancelAim()
        { if (busy) return; ResetPointerInteraction(); sound.Play("whiff"); }
        void PressPower()
        {
            if (busy || battle.Ended || !battle.PlayerPhase || battle.Power < 100) { sound.Play("error"); Toast("Still charging...", Muted); return; }
            selectedGift = null; potionAim = -1;
            if (run.character == "mrs") { powerAim = true; sound.Play("pick"); }
            else if (run.character == "rudolph") { if (battle.UsePower()) PlayBattleCues(); }
            else
            {
                if (!battle.Hand.Any(x => x.Def.type != "curse")) { Toast("No gifts to tinker with.", Muted); return; }
                var parts = DB.Parts.Keys.ToList(); run.Shuffle(parts);
                ChooseParts(parts.Take(3).ToArray(), part => OpenGiftPicker("FIT " + DB.Parts[part].name.ToUpperInvariant(), battle.Hand.Where(x => x.Def.type != "curse").ToList(),
                    g => { if (battle.UsePower(null, part, g)) PlayBattleCues(); }, "CANCEL", null, "Replaces any part already on the gift. Permanent for gifts in your sack."));
            }
        }
        void RequestEndTurn()
        {
            if (busy || modal || battle.Ended || !battle.PlayerPhase) return;
            void end() { selectedGift = null; powerAim = false; potionAim = -1; comboCount = 0; battle.EndTurn(); PlayBattleCues(); }
            if (confirmEnd && battle.Energy > 0 && battle.Hand.Any(battle.Playable)) Confirm("You still have Cheer and gifts you can give. End your turn anyway?", end); else end();
        }
        void PlayBattleCues() { if (!busy && Application.isPlaying) StartCoroutine(AnimateBattle()); }
        void ApplyCueFrame(BattleCue cue)
        {
            // The intent is chosen by the model before the transformation cue; keep Santa visible until that cue is played.
            if (cue.kind == "intent" && cue.unit != null && actors.TryGetValue(cue.unit, out var pendingActor) && pendingActor.frame != null && cue.frame != null && pendingActor.frame.Id != cue.frame.Id) return;
            usingVisualNumbers = true; viewHp = cue.playerHp; viewMaxHp = cue.playerMaxHp; viewEnergy = cue.energy; viewPower = cue.power; viewCookies = cue.cookies; viewSpirit = cue.spirit;
            viewDraw = cue.drawCount; viewDiscard = cue.discardCount; viewConsumed = cue.consumedCount;
            if (cue.unit != null && cue.frame != null)
            {
                displayedUnits[cue.unit] = cue.frame;
                if (!cue.frame.Dead || actors.ContainsKey(cue.unit))
                {
                    var actor = AddActor(cue.unit); string oldId = actor.frame?.Id; actor.frame = cue.frame;
                    if (oldId != cue.frame.Id && cue.kind != "transform") UpdateActorArt(actor);
                    if (cue.kind != "death" && cue.kind != "charm" && cue.kind != "flee" && cue.kind != "transform") BuildUnitHud(actor);
                }
            }
            UpdateBattleCounters();
        }
        void UpdateBattleCounters()
        {
            if (orbNumber) orbNumber.Set(viewEnergy + "/" + battle.MaxEnergy); if (drawNumber) drawNumber.Set(viewDraw.ToString());
            if (discardNumber) discardNumber.Set(viewDiscard.ToString()); if (consumedNumber) consumedNumber.Set("×" + viewConsumed);
            if (powerFill) powerFill.rectTransform.sizeDelta = new Vector2(viewPower * .86f, 3);
            UpdateHudNumbers();
        }
        IEnumerator AnimateBattle()
        {
            busy = true; HideTip(); stage.arrowRoot.gameObject.SetActive(false); stage.reactionIcon.gameObject.SetActive(false);
            var barrier = Rect(pop, 0, 0, 640, 360, Color.clear, true, "Resolving actions");
            stage.CoverScreen(barrier.rectTransform);
            var cues = battle.Cues.ToArray(); battle.Cues.Clear(); ActorVisual lunging = null; bool enemySequence = false;
            Gift effectGift = null; var impactStyle = FestivalBattleEffects.Style.Slash; bool attackPending = false;
            for (int index = 0; index < cues.Length; index++)
            {
                var cue = cues[index]; if (ScreenName != "Combat") break;
                ApplyCueFrame(cue); actors.TryGetValue(cue.unit ?? battle.Player, out var a);
                if (cue.kind == "turn") { enemySequence = cue.text.StartsWith("ENEMY"); effectGift = null; attackPending = false; }
                float speed = fastEnemy && enemySequence ? 2.2f : 1;
                if (cue.kind == "intent") continue;
                if (cue.kind == "draw")
                {
                    sound.Play("draw"); if (cue.gift != null)
                    {
                        if (!presentedHand.Any(g => g.Uid == cue.gift.Uid)) presentedHand.Add(cue.gift);
                        var card = MakeHandVisual(cue.gift, true); float targetX = 326 + (presentedHand.Count - 1) * 18;
                        yield return Tween(.21f / speed, p => { card.x = Mathf.Lerp(30, targetX, EaseOut(p)); card.y = Mathf.Lerp(336, 307, EaseOut(p)); card.scale = Mathf.Lerp(.15f, 1, EaseOut(p)); }); card.drawing = false;
                    }
                    continue;
                }
                if (cue.kind == "shuffle") { sound.Play("shuffle"); Burst(30, 336, Gold, 12); yield return Wait(.22f / speed); continue; }
                if (cue.kind == "gift") { effectGift = cue.gift; comboCount++; sound.Play("play", comboCount - 1); if (comboNumber) comboNumber.Set(comboCount >= 2 ? "GIFT COMBO x" + comboCount : ""); yield return FlyPlayedGift(cue); continue; }
                if (cue.kind == "handDiscard" || cue.kind == "melt") { if (cue.gift != null) yield return DiscardHandVisual(cue.gift, cue.kind == "melt"); continue; }
                if (cue.kind == "discard" || cue.kind == "addDiscard") { Pulse(discardNumber?.rectTransform); continue; }
                if (cue.kind == "consume") { Floating(cue.unit, "CONSUMED", Muted, .75f); continue; }
                if (cue.kind == "powerGift") { Burst(150, 196, Gold, 24, true); continue; }
                if (cue.kind == "ready") { sound.Play("spirit"); Burst(590, 266, Gold, 18, true); Pulse(powerButton); Floating(battle.Player, "POWER READY!", Gold); continue; }
                if (cue.kind == "spawn") { if (a != null && !reduceMotion) { yield return Tween(.32f / speed, p => a.sx = a.sy = BackOut(p)); a.sx = a.sy = 1; } continue; }
                if (cue.kind == "attack")
                {
                    impactStyle = cue.frame.IsPlayer && effectGift != null
                        ? FestivalBattleEffects.ForGift(effectGift.Id, effectGift.Def.type) : FestivalBattleEffects.Style.Slash;
                    attackPending = true;
                    if (assistingGuest && cue.frame.IsPlayer)
                    {
                        yield return Tween(.07f / speed, p => { guestKick = 16 * EaseOut(p); assistingGuest.rectTransform.anchoredPosition = new Vector2(guestHomeX + guestKick, -226); });
                        FestivalFlight(impactStyle, cue.unit, cue.target); if (!reduceMotion) yield return Wait(.12f); continue;
                    }
                    float direction = cue.frame.IsPlayer || cue.frame.Charmed ? 1 : -1;
                    if (a != null && !reduceMotion)
                    {
                        if (!cue.frame.IsPlayer) { sound.Play("windup"); yield return Tween(.12f / speed, p => { a.offsetX = -direction * 8 * p; a.sx = 1 - .12f * p; a.sy = 1 + .12f * p; }); }
                        yield return Tween(.08f / speed, p => { a.offsetX = Mathf.Lerp(-direction * 5, direction * (cue.frame.IsPlayer ? 16 : 22), EaseOut(p)); a.sx = 1.15f; a.sy = .9f; });
                    }
                    FestivalFlight(impactStyle, cue.unit, cue.target);
                    if (!reduceMotion) yield return Wait(.12f);
                    lunging = a; continue;
                }
                if (cue.kind == "hit" || cue.kind == "blocked")
                {
                    var hitStyle = cue.kind == "blocked" ? FestivalBattleEffects.Style.Guard : attackPending ? impactStyle : FestivalBattleEffects.Style.Firecracker;
                    FestivalEffect(hitStyle, cue.unit, .85f + Mathf.Min(25, cue.n) * .02f);
                    attackPending = false;
                    bool large = cue.n >= 15; sound.Play(cue.kind == "blocked" ? "blockHit" : cue.n > 0 ? "hit" : "whiff", cue.n);
                    if (cue.kind == "hit" && cue.n > 0)
                    {
                        if (hitStyle == FestivalBattleEffects.Style.Firecracker) sound.Play("firecracker");
                        else if (hitStyle == FestivalBattleEffects.Style.Gong) sound.Play("bell");
                        else if (hitStyle == FestivalBattleEffects.Style.Frost) sound.Play("clink");
                    }
                    if (large) sound.Play("crit");
                    Floating(cue.unit, cue.kind == "blocked" ? "BLOCKED" : cue.n.ToString(), cue.frame.IsPlayer ? Red : White, large ? 1.5f : 1);
                    if (a != null)
                    {
                        Burst(a.x, 226 - a.Height * .55f, FestivalBattleEffects.Ink(hitStyle), 6 + Mathf.Min(14, cue.n));
                        // Present contact on this frame, before the impact hold.
                        if (!reduceMotion && cue.n > 0) { a.flash = 1; a.sx = 1.12f; a.sy = .9f; }
                    }
                    stage.Impact(cue.kind == "blocked" ? 1.5f : Mathf.Min(9, 1.5f + cue.n * .3f), large ? .05f : .012f);
                    if (large) stage.Flash(White, .18f, .15f); if (cue.frame.IsPlayer && cue.n > 0) stage.Flash(Hex(0xff2020), .22f, .3f);
                    if (!reduceMotion && cue.n > 0) { hitPause = large ? .09f : .035f; yield return Wait(hitPause / speed); hitPause = 0; }
                    if (a != null)
                    {
                        float recoil = (cue.frame.IsPlayer ? -1 : 1) * Mathf.Min(18, 5 + cue.n * .6f), startLunge = lunging?.offsetX ?? 0;
                        yield return Tween(.16f / speed, p =>
                        { a.flash = (1 - p) * (cue.n > 0 ? 1 : 0); a.offsetX = recoil * (1 - EaseOut(p)); a.sx = 1 + .15f * (1 - p); a.sy = 1 - .12f * (1 - p);
                            if (assistingGuest) assistingGuest.rectTransform.anchoredPosition = new Vector2(guestHomeX + guestKick * (1 - EaseOut(p)), -226);
                            if (lunging != null && lunging != a) { lunging.offsetX = startLunge * (1 - EaseOut(p)); lunging.sx = Mathf.Lerp(1.15f, 1, p); lunging.sy = Mathf.Lerp(.9f, 1, p); } });
                        a.flash = 0; a.offsetX = 0; a.sx = a.sy = 1;
                    }
                    if (lunging != null) { lunging.offsetX = 0; lunging.sx = lunging.sy = 1; lunging = null; }
                    guestKick = 0;
                    continue;
                }
                if (cue.kind == "death")
                {
                    sound.Play("death"); if (a != null)
                    { if (a.hud) a.hud.gameObject.SetActive(false); Burst(a.x, 226 - a.Height / 2, White, 24); stage.Impact(3, .03f);
                        yield return Tween(.3f / speed, p => { a.flash = Mathf.Clamp01(p * 3); a.alpha = 1 - p; a.sx = a.sy = 1 - p * .6f; }); a.hidden = true; Release(a.root); actors.Remove(cue.unit); }
                    continue;
                }
                if (cue.kind == "charm")
                {
                    sound.Play("charm"); stage.Flash(Pink, .25f, .4f); Floating(cue.unit, "WON OVER!", Pink, 1.1f);
                    if (a != null) { Burst(a.x, 226 - a.Height * .6f, Pink, 40, true); if (a.hud) a.hud.gameObject.SetActive(false); float x0 = a.x;
                        if (reduceMotion) a.x = cue.frame.X;
                        else yield return Tween(.6f, p => { a.x = Mathf.Lerp(x0, cue.frame.X, EaseOut(p)); a.offsetY = -Mathf.Sin(p * Mathf.PI) * 15; }); a.offsetY = 0; ConfigureHearts(a); BuildUnitHud(a); }
                    continue;
                }
                if (cue.kind == "transform")
                {
                    sound.Duck(.25f); sound.Play("transform");
                    for (int i = 0; i < 8; i++) { if (a != null) a.flash = i % 2; stage.Impact(3 + i, .01f * i); Burst(a?.x ?? 452, 176, i % 2 == 0 ? Red : Hex(0x222222), 16, true); yield return Wait(.17f); }
                    stage.Flash(Red, .8f, .6f); stage.Impact(12, .08f); if (a != null) { UpdateActorArt(a); a.flash = 0; a.sx = a.sy = .6f; yield return Tween(.5f, p => a.sx = a.sy = .6f + .4f * BackOut(p)); BuildUnitHud(a); }
                    sound.Duck(1); yield return Banner("KRAMPUS", cue.text, Red, .95f); continue;
                }
                if (cue.kind == "friend" || cue.kind == "zodiac") { if (cue.kind == "zodiac") sound.Play("zodiac"); yield return Guest(cue.text, DB.Enemies[cue.text].name + (cue.kind == "zodiac" ? "·拜年助阵" : "")); continue; }
                if (cue.kind == "guestExit") { yield return GuestExit(); continue; }
                if (cue.kind == "flee")
                { sound.Play("whoosh"); Floating(cue.unit, cue.text, Muted); if (a != null) { float x = a.x; yield return Tween(.5f / speed, p => a.x = Mathf.Lerp(x, cue.frame.IsPlayer ? -80 : 720, p * p)); Release(a.root); actors.Remove(cue.unit); } continue; }
                if (cue.kind == "victory" || cue.kind == "defeat")
                {
                    sound.Music(null); sound.Play(cue.kind);
                    if (cue.kind == "victory") stage.particles.Confetti(new Vector2(320, 110), 90); else stage.Flash(Color.black, .6f, 1.2f);
                    yield return Banner(cue.kind == "victory" ? "VICTORY!" : "GAME HO-HO-HOVER!", cue.text, cue.kind == "victory" ? Gold : Red, 1); continue;
                }
                if (cue.kind == "banner" || cue.kind == "turn")
                {
                    if (cue.kind == "turn") sound.Play(cue.text.StartsWith("ENEMY") ? "enemyTurn" : "turn");
                    yield return Banner(CharacterText(cue.text), "", cue.text.StartsWith("ENEMY") ? Red : Gold, .35f); continue;
                }
                if (cue.kind == "say") { yield return Say(a, CharacterText(cue.text)); continue; }
                if (cue.kind == "move") { Floating(cue.unit, cue.text, cue.frame.Charmed ? Pink : Gold, .9f); yield return Wait(.22f / speed); continue; }
                if (cue.kind == "wrapBreak") { sound.Play("blockHit"); Floating(cue.unit, cue.text, Ice, .8f); Burst(a?.x ?? 150, 190, Ice, 18); continue; }
                sound.Play(cue.kind);
                if (cue.kind == "heal") FestivalEffect(FestivalBattleEffects.Style.Heal, cue.unit ?? battle.Player);
                else if (cue.kind == "block") FestivalEffect(FestivalBattleEffects.Style.Guard, cue.unit ?? battle.Player);
                else if (cue.kind == "debuff") FestivalEffect(effectGift != null && FestivalBattleEffects.ForGift(effectGift.Id, effectGift.Def.type) == FestivalBattleEffects.Style.Frost
                    ? FestivalBattleEffects.Style.Frost : FestivalBattleEffects.Style.Seal, cue.unit);
                else if (cue.kind == "buff" || cue.kind == "energy" || cue.kind == "love" || cue.kind == "soft")
                    FestivalEffect(FestivalBattleEffects.Style.Blessing, cue.unit ?? battle.Player);
                string text = cue.text ?? (cue.kind == "heal" ? "HP" : cue.kind == "block" ? "WRAP" : cue.kind == "energy" ? "CHEER" : "");
                if (cue.n != 0) text = (cue.n > 0 ? "+" : "") + cue.n + " " + text;
                Color tint = cue.kind == "heal" ? Green : cue.kind == "block" ? Ice : cue.kind == "debuff" || cue.kind == "hate" || cue.kind == "soft" ? Pink : Gold;
                if (!string.IsNullOrEmpty(text)) Floating(cue.unit ?? battle.Player, text, tint, .85f);
                if (a != null && (cue.kind == "heal" || cue.kind == "buff" || cue.kind == "debuff" || cue.kind == "block" || cue.kind == "soft" || cue.kind == "love"))
                { Burst(a.x, cue.kind == "buff" || cue.kind == "heal" ? 220 : 226 - a.Height / 2, tint, cue.kind == "soft" ? 30 : 14, true); a.sx = cue.kind == "debuff" ? .9f : .94f; a.sy = 1.12f;
                    float sx = a.sx, sy = a.sy; yield return Tween(.14f / speed, p => { a.sx = Mathf.Lerp(sx, 1, p); a.sy = Mathf.Lerp(sy, 1, p); }); }
                else yield return Wait(.06f / speed);
            }
            if (barrier) Release(barrier); busy = false; usingVisualNumbers = false; hoveredCard = int.MinValue;
            if (ScreenName != "Combat") yield break;
            if (battle.Ended) { if (battle.Escaped) FinishNode(); else if (!battle.Won) ShowEnding(false); else if (battle.Kind == "boss") ShowEnding(true); else ShowReward(); }
            else { displayedUnits.Clear(); foreach (var u in new[] { battle.Player }.Concat(battle.Units)) displayedUnits[u] = u.Snapshot(); RefreshBattleUI(); }
        }
        IEnumerator FlyPlayedGift(BattleCue cue)
        {
            var gift = cue.gift; if (gift == null) yield break;
            Vector2 start = new Vector2(326, 280);
            if (handViews.TryGetValue(gift.Uid, out var hand)) { start = new Vector2(hand.x, hand.y); RemoveHandVisual(gift.Uid); }
            var card = Card(fx, gift, start.x - 33, start.y - 44); card.pivot = new Vector2(.5f, .5f); var group = PixelUI.Ensure<CanvasGroup>(card);
            float x = cue.targetFrame != null ? Mathf.Clamp((cue.targetFrame.X + 150) / 2 + 30, 140, 520) : 320, y = cue.targetFrame != null ? 226 - (actors.TryGetValue(cue.target, out var target) ? target.Height : 80) / 2 - 40 : 180;
            yield return Tween(.14f, p => { card.anchoredPosition = new Vector2(Mathf.Lerp(start.x, x, EaseOut(p)), -Mathf.Lerp(start.y, y, EaseOut(p))); card.localScale = Vector3.one * Mathf.Lerp(1, 1.1f, p); });
            sound.Play("unwrap");
            var style = FestivalBattleEffects.ForGift(gift.Id, gift.Def.type);
            if (gift.Def.type != "atk" && gift.Def.target != "e" && style != FestivalBattleEffects.Style.Guard
                && style != FestivalBattleEffects.Style.Heal && style != FestivalBattleEffects.Style.Summon)
                FestivalEffect(style, battle.Player);
            Burst(x, y, FestivalBattleEffects.Ink(style), 10);
            var surface = card.GetComponent<CardSurfaceEffects>();
            if (reduceMotion || !surface)
                yield return Tween(.14f, p => group.alpha = 1 - p);
            else
                yield return Tween(.34f, p => surface.SetDissolve(p));
            Release(card);
        }
        IEnumerator DiscardHandVisual(Gift gift, bool melt)
        {
            if (!handViews.TryGetValue(gift.Uid, out var visual)) yield break;
            visual.drawing = true; var start = new Vector2(visual.x, visual.y); sound.Play("draw");
            yield return Tween(.14f, p => { visual.x = Mathf.Lerp(start.x, melt ? start.x : 610, p * p); visual.y = Mathf.Lerp(start.y, melt ? start.y - 30 : 336, p); visual.scale = Mathf.Lerp(1, .15f, p); visual.rotation = p * 140; });
            if (melt) Burst(start.x, start.y, Hex(0x777788), 10); RemoveHandVisual(gift.Uid);
        }
        void RemoveHandVisual(int uid)
        { if (handViews.TryGetValue(uid, out var visual)) { Release(visual.root); handOrder.Remove(visual); handViews.Remove(uid); } presentedHand.RemoveAll(g => g.Uid == uid); }
        static Color GiftColor(Gift gift) => gift.Def.type == "atk" ? Red : gift.Def.type == "skl" ? Green : gift.Def.type == "friend" ? Pink : Gold;
        IEnumerator Guest(string id, string text)
        {
            if (assistingGuest) Release(assistingGuest);
            sound.Play("whoosh"); assistingGuest = Sprite(fx, "Enemy", id, -40, 226, DB.Enemies[id].scale); if (!assistingGuest) yield break;
            assistingGuest.rectTransform.localScale = new Vector3(-1, 1, 1); guestKick = 0;
            guestHomeX = 194 + assistingGuest.rectTransform.sizeDelta.x / 2;
            FestivalEffect(FestivalBattleEffects.Style.Summon, battle.Player);
            yield return Tween(.3f, p => assistingGuest.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-40, guestHomeX, EaseOut(p)), -226));
            Floating(battle.Player, text, Pink, .9f); yield return Wait(.15f);
        }
        IEnumerator GuestExit()
        {
            if (!assistingGuest) yield break;
            var guest = assistingGuest; float from = guest.rectTransform.anchoredPosition.x;
            yield return Wait(.15f); yield return Tween(.45f, p => guest.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(from, 700, p * p), -226));
            Release(guest); assistingGuest = null; guestKick = 0;
        }
        IEnumerator Banner(string title, string subtitle, Color tint, float hold)
        {
            var r = stage.bannerRoot; r.gameObject.SetActive(true); stage.bannerTitle.Set(title); stage.bannerTitle.color = tint; stage.bannerSubtitle.Set(subtitle);
            var cg = r.GetComponent<CanvasGroup>(); Vector2 home = new Vector2(100, -100);
            yield return Tween(.18f, p => { cg.alpha = p; r.anchoredPosition = home + (reduceMotion ? Vector2.zero : Vector2.right * 60 * (1 - EaseOut(p))); r.localScale = reduceMotion ? Vector3.one : Vector3.one * Mathf.Lerp(.92f, 1, BackOut(p)); });
            yield return Wait(hold); yield return Tween(.15f, p => { cg.alpha = 1 - p; r.anchoredPosition = home + (reduceMotion ? Vector2.zero : Vector2.left * p * 40); }); r.gameObject.SetActive(false);
        }
        IEnumerator Say(ActorVisual actor, string text)
        {
            text = Chinese.Text(text);
            var r = stage.bubbleRoot; r.gameObject.SetActive(true); stage.bubbleText.Set(""); var cg = r.GetComponent<CanvasGroup>(); cg.alpha = 1;
            r.anchoredPosition = new Vector2(Mathf.Clamp((actor?.x ?? 320) - 135, 5, 365), -Mathf.Clamp(226 - (actor?.Height ?? 80) - 80, 46, 170));
            float characters = 0; while (characters < text.Length)
            {
                characters += Time.unscaledDeltaTime * (reduceMotion || Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) ? 800 : 58);
                int count = Mathf.Min(text.Length, (int)characters);
                if (count != stage.bubbleText.text.Length) { stage.bubbleText.Set(text.Substring(0, count)); sound.Play("type"); }
                yield return null;
            }
            stage.bubbleText.Set(text); yield return Wait(.6f); r.gameObject.SetActive(false);
        }
        static float EaseOut(float p) => 1 - Mathf.Pow(1 - p, 3);
        static float BackOut(float p) { float t = p - 1; return 1 + 2.70158f * t * t * t + 1.70158f * t * t; }
        IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(seconds); }
        IEnumerator Tween(float duration, Action<float> setter)
        { float t = 0; while (t < duration) { t += Time.unscaledDeltaTime; setter(Mathf.Clamp01(t / duration)); yield return null; } setter(1); }
        void Floating(Unit unit, string text, Color tint, float scale = 1)
        {
            if (!Application.isPlaying) return;
            float x = unit != null && actors.TryGetValue(unit, out var actor) ? actor.x : 320;
            float h = unit != null && actors.TryGetValue(unit, out var a) ? a.Height : 70;
            var label = Label(fx, Mathf.Clamp(x - 100, 4, 436), 226 - h * .6f - 18, 200, text, scale, tint, 1, true); StartCoroutine(FloatAndDestroy(label.rectTransform, .95f, 24));
        }
        void Burst(float x, float y, Color tint, int count, bool up = false)
        { if (!reduceMotion && Application.isPlaying) stage.particles.Burst(new Vector2(x, y), count, tint, 140, up ? -70 : 220, .65f, up, !up); }
        void Pulse(RectTransform rect)
        { if (rect && Application.isPlaying && !reduceMotion) StartCoroutine(Tween(.2f, p => rect.localScale = Vector3.one * (1 + Mathf.Sin(p * Mathf.PI) * .2f))); }
    }
}
