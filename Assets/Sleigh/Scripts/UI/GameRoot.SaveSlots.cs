using System;
using UnityEngine;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        void StartRunInSlot(int slot)
        {
            run = Run.New(characters[characterIndex], advent, (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000000));
            run.saveSlot = slot; run.Save(); newRunSlot = -1; CloseModal(); ShowIntro();
        }
        void ShowSaveSlots(bool startingNew)
        {
            var m = OpenModal(startingNew ? "选择存档" : "继续冒险");
            Label(m, 64, 42, 512, startingNew ? "选择保存位置，本次冒险会自动保存至此。最多五个存档。" : "选择要继续的冒险，也可以在空位开启新冒险。", .72f, Muted, 1);
            for (int i = 0; i < Run.SaveSlotCount; i++)
            {
                int slot = i; float y = 63 + i * 46;
                var saved = Run.Load(slot); bool occupied = Run.HasSave(slot);
                PanelBox(m, 64, y, 512, 42, saved != null && saved.CanContinue ? Gold : Muted);
                string title = "存档 " + (slot + 1) + " · " + (saved != null ? DB.CharacterName(saved.character) : occupied ? "存档损坏" : "空存档");
                if (saved != null) title += " · " + (saved.CanContinue ? "岁程 " + (saved.night + 1) + "/24" : saved.night >= 24 || saved.hp > 0 ? "已通关" : "冒险结束");
                Label(m, 76, y + 5, 346, title, .8f, saved != null ? Gold : White);
                string detail = "可用于保存新的冒险";
                if (saved != null)
                {
                    string time = saved.savedUtcTicks > 0 && saved.savedUtcTicks <= DateTime.MaxValue.Ticks
                        ? new DateTime(saved.savedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "早期存档";
                    detail = "生命 " + saved.hp + "/" + saved.maxHp + " · " + time;
                }
                else if (occupied) detail = "无法读取，可删除或覆盖此存档";
                Label(m, 76, y + 23, 346, detail, .65f, Muted);
                if (startingNew)
                    Button(m, 434, y + 10, 126, 24, occupied ? "覆盖并开始" : "保存并开始", () =>
                    {
                        if (Run.HasSave(slot)) ConfirmSaveSlot("覆盖存档 " + (slot + 1) + "？原有进度将被替换。", () => StartRunInSlot(slot), () => ShowSaveSlots(true));
                        else StartRunInSlot(slot);
                    }, Hex(0x1c5a3a));
                else if (!occupied)
                    Button(m, 434, y + 10, 126, 24, "新建冒险", () => { newRunSlot = slot; CloseModal(); Navigate(ShowCharacters); }, Hex(0x1c5a3a));
                else
                {
                    Button(m, 434, y + 10, 58, 24, "读取", () =>
                    {
                        var loaded = Run.Load(slot);
                        if (loaded == null || !loaded.CanContinue) { ShowSaveSlots(false); return; }
                        CloseModal(); Navigate(() => { run = loaded; ShowMap(); });
                    }, Hex(0x1c5a3a), saved != null && saved.CanContinue);
                    Button(m, 502, y + 10, 58, 24, "删除", () => ConfirmSaveSlot("删除存档 " + (slot + 1) + "？删除后无法恢复。", () => { Run.DeleteSave(slot); ShowSaveSlots(false); }, () => ShowSaveSlots(false)), Hex(0x8e1d2b));
                }
            }
            Button(m, 240, 318, 160, 24, "BACK", CloseModal);
        }
        void ConfirmSaveSlot(string text, Action yes, Action back)
        {
            var m = OpenModal("存档管理", back);
            Label(m, 120, 115, 400, text, 1, White, 1, true);
            Button(m, 170, 250, 140, 26, "确认", () => { CloseModal(); yes(); }, Hex(0x8e1d2b));
            Button(m, 330, 250, 140, 26, "BACK", back);
        }
    }
}
