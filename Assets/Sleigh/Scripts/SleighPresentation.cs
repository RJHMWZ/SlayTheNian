using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        PixelLabel hudHp, hudCookies, hudSpirit;
        float ambientTimer, ambientFireTimer, mapTarget, mapFling, lastMapDrag;
        RectTransform floatingRelic;
        readonly System.Collections.Generic.HashSet<string> nianPhases = new System.Collections.Generic.HashSet<string>();
        Gift previewGift;
        string previewPart;
        int previewBaseDamage;
        Unit previousAim;
        void UpdateHudNumbers()
        {
            if (!usingVisualNumbers || run == null) return;
            if (hudHp) hudHp.Set(Mathf.Max(0, viewHp) + "/" + viewMaxHp);
            if (hudCookies) hudCookies.Set(viewCookies.ToString());
            if (hudSpirit) { string tier = viewSpirit >= 90 ? "Jolly" : viewSpirit >= 70 ? "Festive" : viewSpirit >= 50 ? "Merry" : viewSpirit >= 25 ? "Fading" : "Grim"; hudSpirit.Set("SPIRIT " + viewSpirit + " " + tier); hudSpirit.color = viewSpirit >= 50 ? Green : Pink; }
        }
        void TickPresentation()
        {
            if (IsBaking || !Application.isPlaying) return;
            float dt = Time.unscaledDeltaTime;
            if (ScreenName == "Map" && mapContent)
            {
                if (!Input.GetMouseButton(0) && Time.unscaledTime - lastMapDrag > .03f) { mapTarget = Mathf.Clamp(mapTarget + mapFling * dt, 0, 1676); mapFling *= Mathf.Exp(-8 * dt); }
                mapScroll = Mathf.Lerp(mapScroll, mapTarget, 1 - Mathf.Exp(-10 * dt)); mapContent.anchoredPosition = new Vector2(-Mathf.Round(mapScroll), 0);
            }
            if (ScreenName == "Rest" || ScreenName == "Treasure" || ScreenName == "Victory")
            {
                ambientTimer -= dt;
                if (ambientTimer <= 0)
                {
                    ambientTimer = ScreenName == "Victory" ? .65f : ScreenName == "Rest" ? 1f / 15 : 1f / 6;
                    if (ScreenName == "Victory") { if (!reduceMotion) stage.particles.Confetti(new Vector2(UnityEngine.Random.Range(100, 540), UnityEngine.Random.Range(55, 145)), 30); }
                    else if (ScreenName == "Rest") stage.particles.Burst(new Vector2(UnityEngine.Random.Range(280, 360), 262), 2, Hex(0xf08a3c), 30, -140, .9f, true, false);
                    else if (floatingRelic) stage.particles.Burst(new Vector2(UnityEngine.Random.Range(298, 342), UnityEngine.Random.Range(76, 120)), 2, Gold, 20, -10, .6f, false, false);
                }
                if (ScreenName == "Rest") { ambientFireTimer -= dt; if (ambientFireTimer <= 0) { ambientFireTimer = .25f; sound.Play("fire"); } }
            }
            if (ScreenName != "Combat" || battle == null) return;
            TickHand(dt); TickAim();
            if (powerButton && !reduceMotion && !busy) powerButton.localScale = Vector3.one * (viewPower >= 100 || !usingVisualNumbers && battle.Power >= 100 ? 1 + .025f * Mathf.Sin(Time.unscaledTime * 5) : 1);
            var player = Display(battle.Player); bool low = player.Hp > 0 && player.Hp < player.MaxHp * .3f;
            stage.lowHealthRoot.gameObject.SetActive(low && !reduceMotion);
            if (low)
            {
                stage.lowHealthRoot.GetComponent<CanvasGroup>().alpha = .6f + .4f * Mathf.Max(0, Mathf.Sin(Time.unscaledTime * 7));
                if (Time.unscaledTime >= heartbeatAt) { heartbeatAt = Time.unscaledTime + .9f; sound.Play("heartbeat"); }
            }
            foreach (var a in actors.Values)
            {
                var boss = a.frame;
                if (boss != null && boss.Def != null && boss.Def.tier == "boss" && !boss.Dead && !boss.Charmed)
                {
                    bool finalFlame = boss.Id == "krampus" && boss.Hp <= boss.MaxHp / 2;
                    string phase = boss.Id == "santa" ? "壹 · 年兽初现" : boss.Id == "naughtylist" ? "隐 · 岁门魇" : finalFlame ? "叁 · 岁火决战" : "贰 · 噬岁狂年";
                    if (nianPhases.Add(phase))
                    { Toast(phase, Gold); sound.Play("jingle"); stage.Flash(finalFlame ? Gold : Red, .2f, .45f); Burst(a.x, 210, finalFlame ? Gold : Red, 24); }
                    if (finalFlame && !reduceMotion && a.art)
                    { a.art.color = Color.Lerp(Color.white, Hex(0xffbd76), .2f + .15f * Mathf.Sin(Time.unscaledTime * 6)); }
                }
                if (a.hud) a.hud.anchoredPosition = new Vector2(a.x, -232);
                if (a.hearts != null) foreach (var h in a.hearts) if (h) { var c = h.color; c.a = a.alpha; h.color = c; }
            }
        }
        void TickHand(float dt)
        {
            int count = handOrder.Count; float spacing = Mathf.Min(58, 420f / Mathf.Max(1, count)), mid = (count - 1) * .5f;
            int hover = selectedGift != null || busy || modal ? -1 : handOrder.FindIndex(v => v.gift.Uid == hoveredCard);
            float k = 1 - Mathf.Exp(-14 * dt);
            for (int i = 0; i < count; i++)
            {
                var v = handOrder[i]; if (!v.root || !v.root.gameObject.activeSelf) continue;
                if (!v.drawing && hitPause <= 0)
                {
                    float off = i - mid, x = 326 + off * spacing, y = 318 + off * off * 1.4f, scale = 1, angle = -off * .06f * Mathf.Rad2Deg;
                    if (hover >= 0 && i != hover) x += i < hover ? -18 : 18;
                    if (i == hover) { y = 272; scale = 1.28f; angle = 0; }
                    if (selectedGift != null && selectedGift.Uid == v.gift.Uid) { x = 326; y = 258 + (reduceMotion ? 0 : Mathf.Sin(Time.unscaledTime * 5) * 2); scale = 1.15f; angle = reduceMotion ? 0 : Mathf.Sin(Time.unscaledTime * 3) * 1.7f; }
                    float rate = selectedGift != null && selectedGift.Uid == v.gift.Uid ? 1 - Mathf.Exp(-28 * dt) : k;
                    v.x = Mathf.Lerp(v.x, x, rate); v.y = Mathf.Lerp(v.y, y, rate); v.scale = Mathf.Lerp(v.scale, scale, rate); v.rotation = Mathf.LerpAngle(v.rotation, angle, k);
                }
                v.root.anchoredPosition = new Vector2(v.x, -v.y); v.root.localScale = Vector3.one * v.scale; v.root.localRotation = Quaternion.Euler(0, 0, v.rotation);
                var body = v.root.GetComponent<Image>(); bool affordable = !v.gift.Def.flags.Contains("u") && v.gift.Cost <= (usingVisualNumbers ? viewEnergy : battle.Energy);
                if (body) body.color = affordable || busy ? Color.white : Hex(0x888899);
            }
            if (hover >= 0) handOrder[hover].root.SetAsLastSibling();
            if (selectedGift != null && handViews.TryGetValue(selectedGift.Uid, out var selected)) selected.root.SetAsLastSibling();
        }
        static bool LikeMatches(string like, Gift gift)
        {
            if (string.IsNullOrEmpty(like)) return false; if (like == "rare") return gift.Def.rarity == 3;
            string value = like.StartsWith("part:", StringComparison.Ordinal) ? gift.ToyPart : gift.Def.type;
            return value != null && like.Length == value.Length + 5 && string.CompareOrdinal(like, 5, value, 0, value.Length) == 0;
        }
        void TickAim()
        {
            bool on = !busy && !modal && (selectedGift != null && selectedGift.Def.target == "e" || powerAim || potionAim >= 0);
            stage.arrowRoot.gameObject.SetActive(on);
            Vector2 point = LocalPointer(Input.mousePosition); Unit aim = on ? TargetAt(point) : null;
            if (aim != previousAim) { if (aim != null) sound.Play("tick"); previousAim = aim; }
            if (on)
            {
                float sx = powerAim ? 590 : potionAim >= 0 ? 184 + potionAim * 19 : handViews.TryGetValue(selectedGift.Uid, out var card) ? card.x : 326;
                float sy = powerAim ? 268 : potionAim >= 0 ? 22 : handViews.TryGetValue(selectedGift.Uid, out var selected) ? selected.y - 44 * selected.scale : 230;
                float cx = (sx + point.x) * .5f, cy = Mathf.Min(sy, point.y) - 70;
                for (int i = 0; i < stage.arrowDots.Length; i++)
                {
                    float t = (i + 1f) / stage.arrowDots.Length, a = 1 - t;
                    var dot = stage.arrowDots[i]; dot.rectTransform.anchoredPosition = new Vector2(a * a * sx + 2 * a * t * cx + t * t * point.x, -(a * a * sy + 2 * a * t * cy + t * t * point.y));
                    dot.color = aim != null ? Red : White; if (i == stage.arrowDots.Length - 1) dot.rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(point.y - cy, point.x - cx) * Mathf.Rad2Deg);
                }
            }
            if (previewGift != selectedGift || selectedGift != null && previewPart != selectedGift.ToyPart)
            {
                previewGift = selectedGift; previewPart = selectedGift?.ToyPart; previewBaseDamage = 0;
                if (selectedGift != null) selectedGift.Values().TryGetValue("d", out previewBaseDamage);
            }
            bool reaction = false;
            foreach (var pair in actors)
            {
                var a = pair.Value; var u = a.frame; if (u == null || u.IsPlayer || u.Charmed || u.Dead) continue;
                int damage = 0, pip = 0; bool loved = false, hated = false;
                if (!busy && selectedGift != null)
                {
                    bool here = selectedGift.Def.target == "e" && pair.Key == aim;
                    if (previewBaseDamage > 0 && (here || selectedGift.Def.target == "a" && point.y < 232))
                        damage = Mathf.Max(0, battle.DamageCalc(Display(battle.Player), u, previewBaseDamage) * (selectedGift.Id == "stampede" ? 3 : 1) - u.Block);
                    var entry = Ledger.Get(u.Id); bool rawLove = LikeMatches(u.Love, selectedGift); loved = rawLove && (entry.love || u.Id == "santa"); hated = !rawLove && LikeMatches(u.Hate, selectedGift) && (entry.hate || u.Id == "santa");
                    if (here) pip = (loved ? 2 : 0) - (hated ? 1 : 0) + (selectedGift.ToyPart == "ribbon" ? 1 : 0); else if (aim != null && hated) pip = -1;
                    if (here && (loved || hated))
                    {
                        reaction = true; stage.reactionIcon.sprite = DB.Sprite("Icon", hated ? "intent_debuff" : "heart"); stage.reactionIcon.rectTransform.anchoredPosition = new Vector2(a.x, -(226 - a.Height - 35 + Mathf.Sin(Time.unscaledTime * 6) * 2));
                    }
                }
                float alpha = .45f + .45f * Mathf.Sin(Time.unscaledTime * 10);
                if (a.previewFill)
                {
                    int lost = Mathf.Min(Mathf.Max(0, u.Hp), damage); a.previewFill.rectTransform.anchoredPosition = new Vector2(-28 + Mathf.Round(56f * (u.Hp - lost) / u.MaxHp), 0);
                    a.previewFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(56f * lost / u.MaxHp), 5); a.previewFill.color = Hex(0xffffff, lost > 0 ? alpha : 0);
                }
                if (a.pipPreview != null)
                    for (int i = 0; i < a.pipPreview.Length; i++)
                    { bool pop = pip > 0 && !u.Soft && i < u.Pips && i >= u.Pips - pip, back = pip < 0 && i == u.Pips; if (a.pipPreview[i]) a.pipPreview[i].color = Hex(back ? 0xff5050 : 0xffffff, pop || back ? alpha : 0); }
            }
            stage.reactionIcon.gameObject.SetActive(reaction);
        }
        void Appear(RectTransform rect, float delay = 0, bool scale = false)
        {
            if (!rect) return; var cg = PixelUI.Ensure<CanvasGroup>(rect);
            if (!Application.isPlaying) { cg.alpha = 1; return; }
            StartCoroutine(Appearance(rect, cg, delay, scale));
        }
        IEnumerator Appearance(RectTransform rect, CanvasGroup cg, float delay, bool scale)
        {
            Vector2 origin = rect.anchoredPosition; Vector3 size = rect.localScale; cg.alpha = 0;
            if (delay > 0) yield return Wait(delay);
            yield return Tween(.35f, p => { cg.alpha = p; rect.anchoredPosition = origin + Vector2.down * 8 * (1 - EaseOut(p)); if (scale) rect.localScale = size * Mathf.Lerp(.92f, 1, BackOut(p)); });
        }
        void DragMap(float delta)
        { MoveMap(delta); mapFling = delta / Mathf.Max(.01f, Time.unscaledDeltaTime); lastMapDrag = Time.unscaledTime; }
    }
}
