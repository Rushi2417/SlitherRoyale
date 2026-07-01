using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    public enum ColorblindMode { None, Protanopia, Deuteranopia, Tritanopia }

    public static class AccessibilityService
    {
        public static ColorblindMode CurrentMode { get; set; } = ColorblindMode.None;
        public static bool LowEndDevice { get; private set; }
        public static bool ReduceMotion { get; set; }
        public static event Action OnModeChanged;
        public static event Action OnReduceMotionChanged;

        // Original Bio-Arcade colors
        public static readonly Color ArcViolet = new Color(0.42f, 0.31f, 1f);
        public static readonly Color EmberCoral = new Color(1f, 0.42f, 0.36f);
        public static readonly Color BioMint = new Color(0.25f, 0.88f, 0.77f);
        public static readonly Color GoldYolk = new Color(1f, 0.79f, 0.3f);
        public static readonly Color FogGrey = new Color(0.66f, 0.69f, 0.76f);

        // Colorblind-safe alternatives
        public static Color SafeViolet => CurrentMode switch
        {
            ColorblindMode.Protanopia => new Color(0.5f, 0.4f, 0.9f),
            ColorblindMode.Deuteranopia => new Color(0.55f, 0.35f, 0.95f),
            ColorblindMode.Tritanopia => new Color(0.42f, 0.31f, 1f),
            _ => ArcViolet,
        };
        public static Color SafeRed => CurrentMode switch
        {
            ColorblindMode.Protanopia => new Color(0.9f, 0.5f, 0.3f),
            ColorblindMode.Deuteranopia => new Color(0.95f, 0.45f, 0.35f),
            ColorblindMode.Tritanopia => new Color(1f, 0.42f, 0.36f),
            _ => EmberCoral,
        };
        public static Color SafeGreen => CurrentMode switch
        {
            ColorblindMode.Protanopia => new Color(0.3f, 0.7f, 0.6f),
            ColorblindMode.Deuteranopia => new Color(0.25f, 0.75f, 0.65f),
            ColorblindMode.Tritanopia => new Color(0.5f, 0.6f, 0.8f),
            _ => BioMint,
        };

        public static void Initialize()
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            int totalPixels = screenWidth * screenHeight;
            // Detect low-end: low resolution or old model
            bool isOldDevice = SystemInfo.graphicsDeviceName.Contains("Adreno 5") ||
                               SystemInfo.graphicsDeviceName.Contains("Mali-400") ||
                               SystemInfo.graphicsDeviceName.Contains("PowerVR SGX") ||
                               SystemInfo.systemMemorySize < 2048;
            LowEndDevice = totalPixels < 720 * 1280 || isOldDevice;
            if (LowEndDevice)
                QualitySettings.SetQualityLevel(0, true); // Lowest quality
        }

        public static bool MeetsContrastRatio(Color fg, Color bg, bool isLargeText = false)
        {
            float ratio = CalcContrastRatio(fg, bg);
            return isLargeText ? ratio >= 3f : ratio >= 4.5f;
        }

        private static float CalcContrastRatio(Color fg, Color bg)
        {
            float l1 = RelativeLuminance(fg);
            float l2 = RelativeLuminance(bg);
            float lighter = Mathf.Max(l1, l2);
            float darker = Mathf.Min(l1, l2);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance(Color c)
        {
            float r = Linearize(c.r);
            float g = Linearize(c.g);
            float b = Linearize(c.b);
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        private static float Linearize(float channel)
        {
            return channel <= 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        public static void SetMode(ColorblindMode mode)
        {
            CurrentMode = mode;
            PlayerPrefs.SetInt("ColorblindMode", (int)mode);
            OnModeChanged?.Invoke();
        }

        public static void SetReduceMotion(bool enabled)
        {
            ReduceMotion = enabled;
            PlayerPrefs.SetInt("ReduceMotion", enabled ? 1 : 0);
            OnReduceMotionChanged?.Invoke();
        }

        public static void LoadSavedMode()
        {
            CurrentMode = (ColorblindMode)PlayerPrefs.GetInt("ColorblindMode", 0);
            ReduceMotion = PlayerPrefs.GetInt("ReduceMotion", 0) == 1;
        }
    }
}
