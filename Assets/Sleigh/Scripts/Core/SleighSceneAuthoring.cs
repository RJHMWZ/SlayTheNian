#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        // Called through the editor/MCP baker. None of this authoring path runs in a player build.
        public void BakeViewsInEditor()
        {
            IsBaking = true; bool oldNotes = Ledger.SuppressNotes; Ledger.SuppressNotes = true;
            try
            {
                ShowTitle(); ShowCharacters();
                run = AuthoringRun(); ShowIntro(); ShowMap();
                ShowEvent(DB.All.events[0]); eventOutcome = new EventOutcome("Every letter is a tiny, hopeful heart."); RenderEvent();
                ShowShop(true); restMessage = null; ShowRest(); restMessage = "You doze by the crackling fire. The aches fade away."; ShowRest(); restMessage = null;
                ShowTreasure();
                ui.GetComponentsInChildren<Hotspot>(true).First(h => h.transform.parent.name == "Button:OPEN STOCKING").click();
                NewScreen("Combat", "battle"); TiledBackground();
                battle = new Battle(run, new[] { "nutgrunt", "jackfrost", "santa" }, "elite");
                battle.Cues.Clear(); battle.Hand.Clear();
                foreach (var d in DB.All.gifts.Take(10)) battle.Hand.Add(new Gift { Id = d.id, Uid = -battle.Hand.Count - 1, ToyPart = DB.All.parts[battle.Hand.Count].id });
                for (int i = 0; i < DB.StatusIds.Length; i++) battle.Player.Set(DB.StatusIds[i], 2);
                foreach (var u in battle.Units) { u.Pips = u.PipMax = 9; foreach (var key in DB.StatusIds) u.Set(key, 2); }
                AddActor(battle.Player); foreach (var u in battle.Units) AddActor(u);
                for (int i = actors.Count; i < 16; i++) AddActor(new Unit { Id = "gingerling", Def = DB.Enemies["gingerling"], X = -200, Hp = 20, MaxHp = 20, Pips = 9, PipMax = 9 });
                RefreshBattleUI();
                foreach (var actor in actors.Values) if (!actor.hud) BuildUnitHud(actor);
                int materialIndex = 0;
                foreach (var a in actors.Values)
                { if (a.art) a.art.material = stage.actorFlashMaterials[materialIndex++ % stage.actorFlashMaterials.Length]; }
                rewardCookies = 38; rewardPotion = "hotcocoa"; rewardRelic = "jinglebell"; rewardParts = new[] { "wheels", "ribbon", "battery" };
                rewardFriends = Array.Empty<string>(); rewardGifts = new[] { "midnight", "snowfall", "miracle", "starfall" }; rewardsClaimed.Clear(); RenderReward();
                ShowLeave(); CloseModal();
                ShowEnding(true, true); ShowEnding(true); ShowEnding(false);
                run = AuthoringRun();
                OpenBag(run.bag, "YOUR SACK"); CloseModal();
                OpenGiftPicker("CHOOSE A GIFT", run.bag, g => { }); CloseModal();
                ChooseParts(new[] { "wheels", "ribbon", "battery" }, p => { }); CloseModal();
                ChooseGifts(rewardGifts, "CHOOSE A GIFT", g => { }); CloseModal();
                ShowSettings(); CloseModal(); OpenLedger(); CloseModal(); ShowHelp(); CloseModal();
                ShowItem("SNOW GLOBE", "Apply 1 Brittle to all enemies.", "relic_snowglobe"); CloseModal();
                foreach (var view in stage.views.Where(v => v.name != "Title" && v.name != "CharacterSelect" && v.name != "Intro" && v.name != "Victory" && v.name != "Defeat"))
                    for (int i = 0; i < 4; i++) Sprite(view.ui, "Icon", "potion_empty", -100, 0);
                // Warm transient pools separately from static scene layouts.
                Clear(fx);
                for (int i = 0; i < 48; i++) { var label = Label(fx, 0, 0, 560, "", 1); if (!label.GetComponent<CanvasGroup>()) label.gameObject.AddComponent<CanvasGroup>(); }
                for (int i = 0; i < 8; i++) Card(fx, run.bag[i], 0, 0);
                for (int i = 0; i < 12; i++) Sprite(fx, "Enemy", "gingerling", -100, 226);
                Clear(pop); Rect(pop, 0, 0, 640, 360, Color.clear, true, "Action barrier (prebuilt)");
                var tooltip = PanelBox(pop, 0, 0, 240, 100, Gold); Label(tooltip, 9, 8, 222, "", .8f, White, 0, true);
                ShowTitle();
            }
            finally { IsBaking = false; run = null; battle = null; Ledger.SuppressNotes = oldNotes; }
        }
        Run AuthoringRun()
        {
            var r = Run.New("mrs", 0, 7181); r.hp = r.maxHp = 500; r.cookies = 9999; r.spirit = 90;
            foreach (var item in DB.All.relics) r.AddRelic(item.id);
            foreach (var gift in DB.All.gifts) r.AddGift(gift.id, gift.rarity == 3, gift.type == "curse" ? null : "ribbon");
            r.potionSlots = 4; r.potions = new[] { "hotcocoa", "cracker", "elixir", "milk" }; return r;
        }
    }
}
#endif
