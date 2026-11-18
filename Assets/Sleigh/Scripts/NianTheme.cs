using UnityEngine;

namespace Sleigh
{
    /// <summary>Presentation only. All save IDs and combat rules retain their established meanings.</summary>
    public static class NianTheme
    {
        public const string Title = "逐年：除夕之战";
        public static string Chapter(int night) => night < 6 ? "腊月古镇" : night < 12 ? "雪封山道" : night < 18 ? "灯火集市" : "岁门禁地";
        public static string RegionArt(int night) => night < 6 ? "town" : night < 12 ? "mountain" : night < 18 ? "market" : "gate";
        public static Color Rarity(int rarity) => PixelUI.Hex(rarity == 3 ? 0xeec461 : rarity == 2 ? 0xb88cdd : rarity == 4 ? 0x7fc9a3 : rarity == 9 ? 0x7d717b : 0xc7b28e);
    }
}
