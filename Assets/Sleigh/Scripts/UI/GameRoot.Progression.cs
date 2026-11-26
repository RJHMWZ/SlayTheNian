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
        void OpenLedger(int selection = 0, int page = 0)
        {
            const int pageSize = 9;
            var all = DB.All.enemies.Where(e => e.id != "naughtylist" || Ledger.Get(e.id).seen).ToArray(); selection = Mathf.Clamp(selection, 0, all.Length - 1); page = Mathf.Clamp(page, 0, (all.Length - 1) / pageSize);
            var m = OpenModal("NICE & NAUGHTY LEDGER / " + all.Count(e => Ledger.Get(e.id).seen) + "/" + all.Length, titleScale: 1.4f);
            var p = PanelBox(m, 30, 49, 580, 258, Gold); Rect(p, 198, 8, 1, 242, Hex(0x54618c));
            for (int i = 0; i < pageSize && page * pageSize + i < all.Length; i++)
            { int index = page * pageSize + i; var e = all[index]; var l = Ledger.Get(e.id); Button(p, 8, 9 + i * 26, 182, 24, l.seen ? e.name : "???", () => OpenLedger(index, page), index == selection ? Hex(0xa8741f) : Hex(0x253262), captionScale: 1.05f); }
            var d = all[selection]; var entry = Ledger.Get(d.id); var art = Sprite(p, "Enemy", d.id, 278, 153, Mathf.Min(1.4f, 95f / DB.Frame("Enemy", d.id).height)); if (art && !entry.seen) art.color = Color.black;
            Label(p, 212, 10, 352, entry.seen ? d.name : "???", 1.3f, Gold, 1);
            string description = entry.seen ? d.lore + "\n\nLoves: " + (entry.love ? DB.LikeLabel(d.love) : "???") + "\nHates: " + (entry.hate ? DB.LikeLabel(d.hate) : "???") + "\nDefeated: " + entry.kills + " / Befriended: " + entry.charms : "You haven't met this monster yet.";
            Label(p, 332, 43, 228, description, 1.1f, White, 0, true);
            if (entry.charms > 0 && DB.Gifts.ContainsKey("f_" + d.id)) Card(p, new Gift { Id = "f_" + d.id }, 243, 158, .8f);
            int pg = page; Button(m, 50, 316, 50, 28, "<", () => OpenLedger((pg - 1) * pageSize, pg - 1), null, page > 0, 1.1f);
            Button(m, 110, 316, 50, 28, ">", () => OpenLedger((pg + 1) * pageSize, pg + 1), null, (page + 1) * pageSize < all.Length, 1.1f);
            Label(m, 174, 323, 160, (page + 1) + " / " + ((all.Length + pageSize - 1) / pageSize), 1.05f, White, 1);
            Button(m, 455, 316, 140, 28, "BACK", CloseModal, captionScale: 1.05f);
        }
        void ShowEnding(bool victory, bool afterPoem = false)
        {
            run.active = false; if (!IsBaking) run.Save();
            NewScreen(victory ? "Victory" : "Defeat", victory ? "end" : null); TiledBackground(victory && !afterPoem);
            if (victory && !afterPoem)
            {
                var poem = new List<string> { run.secret ? "The Naughty List shredded, its dark magic fled," : "The Krampus fell silent, the dark shadow fled", run.secret ? "And Santa stood laughing, his cheeks rosy red." : "And Santa stood blinking, his cheeks rosy red." };
                poem.Add(run.character == "mrs" ? "\"Martha,\" he whispered, \"you've saved Christmas Day!\"" : run.character == "tinsel" ? "\"Tinsel,\" he whispered, \"you've saved Christmas Day!\"" : "\"Rudie,\" he whispered, \"you've saved Christmas Day!\"");
                poem.Add(run.character == "mrs" ? "\"I know, dear,\" she said. \"Now get on that sleigh!\"" : run.character == "tinsel" ? "\"I know, boss,\" she grinned. \"Now get on that sleigh!\"" : "So Rudolph just snorted and lit up the way.");
                if (run.secret) poem.AddRange(new[] { "And all of the friends that were won on the way", "Climbed into the sleigh for the ride Christmas Day." });
                poem.AddRange(new[] { "And kids heard them say as they flew out of sight:", "\"Merry Christmas to all! Have a jolly good night!\"" });
                PlayPoem(new[] { poem.ToArray() }, () => ShowEnding(true, true)); return;
            }
            Label(ui, 30, 51, 580, victory ? run.secret ? "A TRULY NICE CHRISTMAS!" : "MERRY CHRISTMAS!" : "GAME HO-HO-HOVER!", 1.6f, victory ? Gold : Pink, 1);
            Sprite(world, "Claus", DB.CharacterArt(run.character), 160, 280, 1.5f);
            if (victory)
            {
                int unlocked = Mathf.Min(10, run.advent + 1); if (!IsBaking) PlayerPrefs.SetInt("Sleigh.Advent", Mathf.Max(PlayerPrefs.GetInt("Sleigh.Advent", 0), unlocked));
                Sprite(world, "Enemy", "santa", 475, 280, 1.3f);
                Label(ui, 90, 101, 460, run.secret ? "The list is torn. Every friend you were kind to\ncame to help. Santa remembers who he is." : "The coal falls away. Santa is himself again.\nChristmas Day is safe.", .85f, White, 1, true);
                Label(ui, 190, 165, 260, "Spirit: " + run.spirit + " / " + run.SpiritName + "\nGifts left: " + run.stats.left + "\nWishes granted: " + run.stats.wishes + "\nDefeated: " + run.stats.kills + "\nBefriended: " + run.stats.charmed + "\nAdvent " + unlocked + " unlocked!", .85f, Gold, 1);
            }
            else Label(ui, 170, 130, 300, "Nights survived: " + run.night + " of 24\nMonsters defeated: " + run.stats.kills + "\nGifts left for children: " + run.stats.left + "\nSpirit given: " + run.stats.joy, 1, White, 1, true);
            if (victory) Button(ui, 240, 315, 160, 25, "THE END", ShowTitle, Hex(0x1c5a3a));
            else { Button(ui, 150, 280, 160, 24, "TRY AGAIN", ShowCharacters, Hex(0x8e1d2b)); Button(ui, 330, 280, 160, 24, "TITLE", ShowTitle); }
        }
    }
}
