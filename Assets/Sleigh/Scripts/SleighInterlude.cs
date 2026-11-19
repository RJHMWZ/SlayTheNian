using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        IEnumerator OpenStocking(Graphic sock, string relic)
        {
            busy = true; HideTip(); var motion = sock.GetComponent<SleighMotion>(); motion.ResetMotion();
            var buttonBlock = Rect(pop, 0, 0, 640, 360, Color.clear, true);
            stage.CoverScreen(buttonBlock.rectTransform);
            float jingle = 0;
            yield return Tween(.78f, p => {
                float t = p * .78f; sock.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 13) * Mathf.Lerp(1.45f, 11.5f, p));
                sock.rectTransform.localScale = new Vector3(1 - p * .08f, 1 + p * .08f, 1);
                if (t >= jingle) { sound.Play("jingle"); jingle += .15f; }
            });
            sound.Play("unwrap"); stage.Impact(4, .04f); stage.Flash(Gold, .3f, .5f); Burst(320, 155, Gold, 26, true);
            StartCoroutine(Tween(.45f, p => { sock.rectTransform.localScale = new Vector3(Mathf.Lerp(1.22f, 1, EaseOut(p)), Mathf.Lerp(.82f, 1, EaseOut(p)), 1); sock.rectTransform.localRotation = Quaternion.identity; }));
            var reward = Sprite(world, "Icon", relic == null ? "coin" : "relic_" + relic, 320, 155);
            reward.rectTransform.localScale = Vector3.one * .3f;
            var cg = PixelUI.Ensure<CanvasGroup>(reward);
            sound.Play("relic");
            yield return Tween(.9f, p => { reward.rectTransform.anchoredPosition = new Vector2(320, -Mathf.Lerp(155, 98, EaseOut(p))); reward.rectTransform.localScale = Vector3.one * Mathf.Lerp(.3f, 2, BackOut(p)); cg.alpha = Mathf.Clamp01(p * 4); });
            RevealStocking(relic); Release(buttonBlock); busy = false;
            floatingRelic = reward.rectTransform; SleighMotion.Add(reward.rectTransform, SleighMotion.Style.Float, 3, 2.2f);
        }
        void RevealStocking(string relic)
        {
            Clear(ui); Hud();
            if (!Application.isPlaying) Sprite(world, "Icon", relic == null ? "coin" : "relic_" + relic, 320, 98, 2);
            Appear(Label(ui, 80, 260, 480, relic != null ? DB.Relics[relic].name : "+100 Cookies", 1.2f, Gold, 1).rectTransform);
            Appear(Label(ui, 120, 279, 400, relic != null ? DB.Relics[relic].desc : "Something sweet for the road.", .8f, White, 1, true).rectTransform, .1f);
            Appear((RectTransform)Button(ui, 230, 316, 180, 24, "CONTINUE >", FinishNode, Hex(0x1c5a3a)).transform, .2f);
        }
        void BeginDelivery(Gift gift, int joy, SpringFestivalProp house)
        {
            CloseModal(); if (Application.isPlaying) StartCoroutine(DeliverGift(gift, joy, house));
        }
        IEnumerator DeliverGift(Gift gift, int joy, SpringFestivalProp house)
        {
            busy = true; HideTip(); var barrier = Rect(pop, 0, 0, 640, 360, Color.clear, true);
            stage.CoverScreen(barrier.rectTransform);
            RectTransform parcel = gift != null ? Card(fx, gift, 140, 180) : Sprite(fx, "Icon", "coin", 173, 224, 3).rectTransform;
            parcel.pivot = new Vector2(.5f, .5f); var cg = PixelUI.Ensure<CanvasGroup>(parcel);
            sound.Play("whoosh");
            yield return Tween(.55f, p => { parcel.anchoredPosition = new Vector2(Mathf.Lerp(173, 430, EaseOut(p)), -Mathf.Lerp(224, 282 - house.rectTransform.sizeDelta.y * house.rectTransform.localScale.y * .45f, EaseOut(p))); parcel.localScale = Vector3.one * Mathf.Lerp(1, .1f, p); parcel.localRotation = Quaternion.Euler(0, 0, -p * 25); cg.alpha = 1 - p * p; });
            Release(parcel); bool lit = run.map[run.night].lit;
            house.SetLit(lit);
            sound.Play("give"); sound.Play(joy > 0 ? "spirit" : "hate"); Burst(430, 220, joy > 0 ? Gold : Pink, 30, true);
            Clear(ui); Hud();
            Appear(Label(ui, 40, 85, 560, joy > 0 ? "A little Christmas magic lights up the night." : "The house stays quiet in the snow.", 1, Gold, 1, true).rectTransform);
            Appear(Label(ui, 90, 120, 460, (joy >= 0 ? "+" : "") + joy + " CHRISTMAS SPIRIT", 1.3f, joy > 0 ? Green : Red, 1).rectTransform, .2f, true);
            yield return Wait(1.5f); Release(barrier); busy = false; FinishNode();
        }
    }
}
