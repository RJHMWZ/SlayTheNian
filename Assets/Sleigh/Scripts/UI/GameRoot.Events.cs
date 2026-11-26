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
        EventDef currentEvent;
        EventOutcome eventOutcome;
        string restMessage;

        void ShowEvent(EventDef e)
        { currentEvent = e; eventOutcome = null; RenderEvent(); }
        void RenderEvent()
        {
            NewScreen("Event"); TiledBackground(true); Hud(); var p = PanelBox(ui, 60, 54, 520, 286, Gold);
            Label(p, 12, 12, 496, currentEvent.title.ToUpperInvariant(), 1.2f, Gold, 1);
            Label(p, 20, 55, 480, CharacterText(eventOutcome?.text ?? currentEvent.text), .92f, White, 0, true);
            if (eventOutcome == null)
            {
                for (int i = 0; i < currentEvent.choices.Length; i++)
                {
                    int index = i; var c = currentEvent.choices[i]; bool ok = run.cookies >= c.price && (!c.needsPotion || run.potions.Any(x => !string.IsNullOrEmpty(x)));
                    Button(p, 16, 192 + i * 26, 488, 22, (i + 1) + ". " + c.label, () =>
                    { eventOutcome = EventRules.Resolve(run, currentEvent.id, index); RenderEvent(); HandleEventOutcome(); }, null, ok);
                }
            }
            else Button(p, 160, 244, 200, 24, eventOutcome.fight != null ? "FIGHT!" : "CONTINUE >", () =>
            { if (eventOutcome.fight != null) StartBattle(eventOutcome.fight, eventOutcome.kind); else FinishNode(); }, Hex(0x1c5a3a));
        }
        void HandleEventOutcome()
        {
            if (eventOutcome.choose != null) ChooseGifts(eventOutcome.choose, "CHOOSE A GIFT", id => { run.AddGift(id); RenderEvent(); });
            else if (eventOutcome.picker != null) EventPicker(eventOutcome.picker);
        }
        void EventPicker(string mode)
        {
            var list = mode == "upgrade" ? run.bag.Where(x => x.CanUpgrade).ToList() : mode == "dup" ? run.bag.Where(x => x.Def.type != "curse").ToList() : run.bag.ToList();
            if (mode == "leave" && run.bag.Count <= 5) { run.doubleJoy = false; Toast("Your sack is too light to spare a gift.", Muted); return; }
            if (list.Count == 0) { Toast("Nothing to rewrap.", Muted); return; }
            OpenGiftPicker(mode == "upgrade" ? "REWRAP A GIFT" : mode == "leave" ? "LEAVE A GIFT" : mode == "dup" ? "DUPLICATE A GIFT" : "TRANSFORM A GIFT", list, g =>
            {
                if (mode == "upgrade") g.Upgrade = true;
                else if (mode == "leave") run.LeaveGift(g);
                else if (mode == "dup") run.AddGift(g.Id, g.Upgrade);
                else { int rarity = g.Def.rarity; run.bag.Remove(g); string id = run.RandomGift(rarity >= 1 && rarity <= 3 ? Mathf.Min(3, rarity + (run.Random() < .3 ? 1 : 0)) : run.RollRarity()); run.AddGift(id); eventOutcome.text += "\nIt became " + DB.Gifts[id].name + "!"; }
                RenderEvent(); sound.Play("buff");
            }, "SKIP", () => { run.doubleJoy = false; CloseModal(); }, null, mode == "leave" ? (Func<Gift, string>)(g => { int n = run.Joy(g); if (run.doubleJoy && n > 0) n *= 2; return (n > 0 ? "+" : "") + n + " SPIRIT"; }) : null);
        }
        void ShowRest()
        {
            NewScreen("Rest"); SpringFestivalBackdrop.Build(bg, SpringFestivalBackdrop.Setting.Hearth); Hud();
            Label(ui, 40, 51, 560, "FIRESIDE - A QUIET NIGHT", 1, Hex(0xf08a3c), 1);
            if (restMessage != null)
            { Label(ui, 452, 134, 174, restMessage, .86f, White, 0, true); Button(ui, 460, 248, 170, 24, "CONTINUE >", FinishNode, Hex(0x1c5a3a)); return; }
            int heal = Battle.Round(run.maxHp * (run.Adv(5) ? .22f : .3f)) + (run.Has("cocoapot") ? 15 : 0);
            void finish(string message)
            { restMessage = message; ShowRest(); sound.Play("heal"); }
            Button(ui, 460, 140, 170, 24, "REST (+" + heal + " HP)", () => { run.Heal(heal); finish("You doze by the crackling fire. The aches fade away."); }, Hex(0x1c5a3a));
            Button(ui, 460, 174, 170, 24, "REWRAP A GIFT", () => OpenGiftPicker("REWRAP A GIFT", run.bag.Where(x => x.CanUpgrade).ToList(), g => { g.Upgrade = true; finish("Ribbons tied, bows fluffed. Good as new."); }), null, run.bag.Any(x => x.CanUpgrade));
            Button(ui, 460, 208, 170, 24, "WRITE TO SANTA", () => { run.AddSpirit(8); finish("Dear Nick, remember the boy who carved toys for everyone?\n+8 Spirit"); }, Hex(0x8e1d2b));
        }
        void ShowTreasure()
        {
            NewScreen("Treasure"); TiledBackground(); Hud();
            Label(ui, 40, 56, 560, "A STOCKING STUFFED WITH SOMETHING...", 1, Gold, 1);
            var sock = SpringFestivalProp.BlessingBag(world, 320, 140);
            sock.rectTransform.pivot = new Vector2(.5f, 1); SleighMotion.Add(sock.rectTransform, SleighMotion.Style.Swing, 1.45f, 3);
            Action open = () => { if (busy) return; string id = run.RandomRelic(); if (id != null) run.AddRelic(id); else run.cookies += 100;
                if (Application.isPlaying) StartCoroutine(OpenStocking(sock, id)); else RevealStocking(id); };
            var hot = Hot(world, 280, 130, 80, 150, open, () => { if (!busy) SleighMotion.Add(sock.rectTransform, SleighMotion.Style.Swing, 3.4f, 3); }, () => { if (!busy) SleighMotion.Add(sock.rectTransform, SleighMotion.Style.Swing, 1.45f, 3); });
            Button(ui, 240, 290, 160, 26, "OPEN STOCKING", open, Hex(0x8e1d2b));
        }
    }
}
