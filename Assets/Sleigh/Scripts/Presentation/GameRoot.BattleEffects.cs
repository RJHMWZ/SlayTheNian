using UnityEngine;
using static Sleigh.PixelUI;

namespace Sleigh
{
    public partial class GameRoot
    {
        FestivalBattleEffects BattleEffects()
        {
            if (!stage.battleEffects)
            {
                // Supports an already-open scene authored before the effect field was added.
                var root = Group(stage.effectLayer, "Pooled Festival Battle Effects (32)");
                stage.battleEffects = Ensure<FestivalBattleEffects>(root);
                stage.battleEffects.raycastTarget = false;
                root.SetAsFirstSibling();
            }
            stage.battleEffects.reduced = reduceMotion;
            return stage.battleEffects;
        }
        Vector2 EffectCenter(Unit unit)
        {
            if (unit != null && actors.TryGetValue(unit, out var actor))
                return new Vector2(actor.x, 226 - Mathf.Min(110, actor.Height) * .52f);
            return new Vector2(unit?.X ?? 150, 186);
        }
        void FestivalEffect(FestivalBattleEffects.Style style, Unit unit, float strength = 1)
        {
            if (!Application.isPlaying || reduceMotion) return;
            var center = EffectCenter(unit);
            if (style == FestivalBattleEffects.Style.Guard) center.y += 18;
            BattleEffects().Play(style, center, center, false, strength);
        }
        void FestivalFlight(FestivalBattleEffects.Style style, Unit source, Unit target)
        {
            if (!Application.isPlaying || reduceMotion || target == null) return;
            Vector2 from = assistingGuest && source != null && source.IsPlayer
                ? new Vector2(guestHomeX, 182) : EffectCenter(source);
            BattleEffects().Play(style, from, EffectCenter(target), true);
        }
    }
}
