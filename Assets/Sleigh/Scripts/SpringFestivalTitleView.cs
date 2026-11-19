using System;
using UnityEngine;

namespace Sleigh
{
    // The illustration, title and button skins are authored and saved in the scene.
    // Only player data and non-serialized event handlers are bound at runtime.
    public sealed class SpringFestivalTitleView : MonoBehaviour
    {
        [SerializeField] Hotspot[] buttons;
        [SerializeField] PixelLabel continueCaption;
        [SerializeField] bool showingContinue = true;
        public bool IsReady => buttons != null && buttons.Length == 5 &&
            Array.TrueForAll(buttons, button => button) && continueCaption;

        public void SetAuthoredButtons(Hotspot[] value)
        {
            buttons = value;
            showingContinue = true;
            continueCaption = buttons[1].transform.parent.GetComponentInChildren<PixelLabel>(true);
        }

        public void Bind(Run saved, Action start, Action resume, Action help, Action settings, Action ledger)
        {
            bool canContinue = saved != null && saved.active && saved.hp > 0 && saved.night < 24;
            Action[] actions = { start, resume, help, settings, ledger };
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                var frame = (RectTransform)button.transform.parent;
                frame.gameObject.SetActive(i != 1 || canContinue);
                // Preserve scene-authored positions, including manual layout edits.
                if (i >= 2 && showingContinue != canContinue)
                    frame.anchoredPosition += Vector2.up * (canContinue ? -29 : 29);
                var art = frame.GetComponentInChildren<FirecrackerButtonBackground>(true);
                var action = actions[i];
                button.ClearBindings(); art.ResetInteraction();
                button.click = () => { SleighAudio.Instance?.Play("click"); action?.Invoke(); };
                button.enter = () => { art.SetHovered(true); SleighAudio.Instance?.Play("hover"); };
                button.exit = () => art.SetHovered(false);
            }
            showingContinue = canContinue;
            continueCaption.Set(canContinue ? "CONTINUE / DEC " + (saved.night + 1) : "CONTINUE");
        }
    }
}
