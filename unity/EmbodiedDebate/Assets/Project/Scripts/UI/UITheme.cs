using UnityEngine;

namespace ArgusUnity.UI
{
    /// <summary>
    /// Central design tokens for the AI Company HUD.
    /// ponytail: static constants only; no MonoBehaviour overhead.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color BgPrimary = new Color(0.102f, 0.114f, 0.149f, 0.88f);
        public static readonly Color BgSecondary = new Color(0.145f, 0.165f, 0.212f, 1f);
        public static readonly Color BgTertiary = new Color(0.196f, 0.227f, 0.290f, 1f);
        public static readonly Color Accent = new Color(0.298f, 0.788f, 0.941f, 1f);
        public static readonly Color AccentWarm = new Color(0.976f, 0.780f, 0.310f, 1f);
        public static readonly Color Success = new Color(0.565f, 0.745f, 0.427f, 1f);
        public static readonly Color Warning = new Color(0.973f, 0.588f, 0.118f, 1f);
        public static readonly Color Danger = new Color(0.976f, 0.255f, 0.267f, 1f);
        public static readonly Color TextPrimary = new Color(0.969f, 0.973f, 0.980f, 1f);
        public static readonly Color TextSecondary = new Color(0.659f, 0.694f, 0.769f, 1f);
        public static readonly Color TextMuted = new Color(0.420f, 0.447f, 0.502f, 1f);

        public const int FontXS = 12;
        public const int FontS = 14;
        public const int FontM = 16;
        public const int FontL = 20;
        public const int FontXL = 28;

        public const float PanelRadius = 12f;
        public const float ButtonRadius = 8f;
        public const float ChipRadius = 20f;

        public const float DrawerOpenSeconds = 0.20f;
        public const float CommandBarOpenSeconds = 0.15f;
        public const float ToastInSeconds = 0.25f;
        public const float BannerInSeconds = 0.20f;
        public const float ButtonHoverSeconds = 0.10f;

        public static Color MoodColor(int mood)
        {
            if (mood >= 80) return Success;
            if (mood >= 40) return AccentWarm;
            if (mood >= 20) return Warning;
            return Danger;
        }
    }
}
