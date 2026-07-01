using System;
using System.Collections;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using SlitherRoyale.Client.Monetization;
using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Home Screen â€” the main hub.
    /// Doc 05 Â§3.2: PLAY button, worm preview, currency bar, BP bar,
    /// quest badge, settings/friends/bell icons in top bar.
    /// Built entirely in code â€” no scene assets required.
    /// </summary>
    public class HomeScreen : UIScreen
    {
        // â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Text  _coinsLabel, _gemsLabel, _bpLabel, _questLabel;
        private Image _bpBarFill, _playBtnImg;
        private Text  _playLabel;
        private Button _playBtn;
        private Text  _loginDot;
        private Text  _wormPreviewLabel; // animated worm preview name
        private Image _modeIndicator;

        // Mode selection state
        // BUG-04 FIX: Names/icons/order now match WormCore.MatchMode enum exactly
        // FreeForAll=0, Duos=1, Ranked1v1=2, BattleRoyale=3
        private static readonly string[]     ModeNames  = { "Free-For-All", "Duos", "1v1 Ranked", "Battle Royale" };
        private static readonly string[]     ModeIcons  = { "âš”", "ðŸ‘¥", "ðŸ†", "ðŸŽ¯" };
        private static readonly MatchMode[]  ModeEnums  = { MatchMode.FreeForAll, MatchMode.Duos, MatchMode.Ranked1v1, MatchMode.BattleRoyale };
        private static readonly Color[]      ModeColors =
        {
            new Color(0.424f, 0.310f, 1.000f), // ArcViolet
            new Color(0.247f, 0.878f, 0.773f), // BioMint
            new Color(1.000f, 0.788f, 0.302f), // GoldYolk
            new Color(1.000f, 0.420f, 0.357f), // EmberCoral
        };
        private int _selectedMode = 0;
        private Image[] _modeDots;

        // Worm preview (animated snake-like dots)
        private Image[] _snakeDots;
        private float   _snakeTime;

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            // â”€â”€ Background â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var bg = AddFullBg(InkVoid);

            // Subtle top gradient overlay (arc-violet tint)
            var topGrad = AddImage("TopGrad",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -320f), new Vector2(0f, 0f),
                new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0.07f));
            topGrad.raycastTarget = false;

            // â”€â”€ Top Bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildTopBar();

            // â”€â”€ Worm Preview Panel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildWormPreview();

            // â”€â”€ Currency Bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildCurrencyBar();

            // â”€â”€ Battle Pass Strip â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildBattlePassBar();

            // â”€â”€ Mode Selector â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildModeSelector();

            // â”€â”€ PLAY Button â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildPlayButton();

            // â”€â”€ Quest / Daily Reward badges â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildBadges();

            // â”€â”€ Bottom Nav Row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            BuildBottomNav();
        }

        // â”€â”€ Top Bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildTopBar()
        {
            // Top bar background
            var barBg = AddImage("TopBarBg",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -100f), new Vector2(0f, 0f),
                new Color(0.04f, 0.05f, 0.08f, 0.95f));

            // Avatar circle â€” left
            MakeAnchoredImage("Avatar", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -90f), new Vector2(82f, -18f),
                MakeCircleSprite(ArcViolet, 64), barBg.transform);

            // "S" letter in avatar
            MakeAnchoredText("S", Color.white, 30, FontStyle.Bold,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -90f), new Vector2(82f, -18f), barBg.transform);

            // Title â€” center
            MakeAnchoredText("SLITHER ROYALE", ArcViolet, 24, FontStyle.Bold,
                new Vector2(0.25f, 1f), new Vector2(0.75f, 1f),
                new Vector2(0f, -84f), new Vector2(0f, -12f), barBg.transform);

            // Icons â€” right side
            MakeIconButton("âš™", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-190f, -88f), new Vector2(-130f, -18f),
                barBg.transform, () => ScreenManager.Instance.NavigateTo<SettingsScreen>());
            MakeIconButton("ðŸ‘¥", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-126f, -88f), new Vector2(-66f, -18f),
                barBg.transform, () => ScreenManager.Instance.NavigateTo<FriendsListScreen>());
            MakeIconButton("ðŸ†", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-62f, -88f), new Vector2(-2f, -18f),
                barBg.transform, () => ScreenManager.Instance.NavigateTo<LeaderboardScreen>());
        }

        // â”€â”€ Worm Preview â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildWormPreview()
        {
            // Card background
            var card = AddImage("WormCard",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -390f), new Vector2(0f, -105f),
                CardBg);
            card.sprite = MakeRoundRectSprite(900, 280, 24);
            card.type = Image.Type.Sliced;

            // Animated snake dots
            _snakeDots = new Image[12];
            for (int i = 0; i < _snakeDots.Length; i++)
            {
                var dot = new GameObject($"SnakeDot{i}");
                dot.transform.SetParent(card.transform, false);
                var rt = dot.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(i == 0 ? 28f : 20f, i == 0 ? 28f : 20f);
                var img = dot.AddComponent<Image>();
                float t = i / (float)(_snakeDots.Length - 1);
                img.color = Color.Lerp(ArcViolet, BioMint, t);
                img.sprite = MakeCircleSprite(Color.white, 32);
                _snakeDots[i] = img;
            }

            // "YOUR WORM" label
            _wormPreviewLabel = MakeAnchoredText("YOUR WORM", FogGrey, 16, FontStyle.Normal,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 10f), new Vector2(0f, 40f), card.transform);

            // Tap to customize hint
            MakeAnchoredText("TAP TO CUSTOMIZE â€º", new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0.7f),
                13, FontStyle.Normal,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-200f, 8f), new Vector2(-8f, 40f), card.transform);

            // Make card tappable â†’ Customize
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card;
            btn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                ScreenManager.Instance.NavigateTo<CustomizeScreen>();
            });
        }

        // â”€â”€ Currency Bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildCurrencyBar()
        {
            var bar = AddImage("CurrencyBar",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -450f), new Vector2(0f, -398f),
                new Color(0.06f, 0.08f, 0.12f, 1f));
            bar.sprite = MakeRoundRectSprite(900, 52, 26);
            bar.type   = Image.Type.Sliced;

            // Coins
            MakeAnchoredText("ðŸª™", GoldYolk, 22, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(16f, 0f), new Vector2(50f, 0f), bar.transform);
            _coinsLabel = MakeAnchoredText("---", GoldYolk, 18, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(54f, 0f), new Vector2(200f, 0f), bar.transform);

            // Divider
            MakeAnchoredImage("Div", new Vector2(0.3f, 0.15f), new Vector2(0.3f, 0.85f),
                Vector2.zero, Vector2.zero, DividerCol, bar.transform);

            // Gems
            MakeAnchoredText("ðŸ’Ž", BioMint, 22, FontStyle.Bold,
                new Vector2(0.35f, 0f), new Vector2(0.35f, 1f),
                new Vector2(0f, 0f), new Vector2(36f, 0f), bar.transform);
            _gemsLabel = MakeAnchoredText("---", BioMint, 18, FontStyle.Bold,
                new Vector2(0.35f, 0f), new Vector2(0.35f, 1f),
                new Vector2(40f, 0f), new Vector2(160f, 0f), bar.transform);

            // Tap whole bar â†’ Shop
            var btn = bar.gameObject.AddComponent<Button>();
            btn.targetGraphic = bar;
            btn.onClick.AddListener(() => {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                ScreenManager.Instance.NavigateTo<ShopScreen>();
            });
        }

        // â”€â”€ Battle Pass Bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildBattlePassBar()
        {
            var strip = AddImage("BPStrip",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -510f), new Vector2(0f, -458f),
                CardBg);
            strip.sprite = MakeRoundRectSprite(900, 52, 16);
            strip.type   = Image.Type.Sliced;

            MakeAnchoredText("BATTLE PASS", FogGrey, 13, FontStyle.Normal,
                new Vector2(0f, 0f), new Vector2(0.35f, 1f),
                new Vector2(12f, 0f), new Vector2(0f, 0f), strip.transform);

            _bpLabel = MakeAnchoredText("Lv. --", ArcViolet, 14, FontStyle.Bold,
                new Vector2(0.35f, 0f), new Vector2(0.55f, 1f),
                Vector2.zero, Vector2.zero, strip.transform);

            // BP progress bar track
            var track = MakeAnchoredImage("Track",
                new Vector2(0.55f, 0.2f), new Vector2(0.92f, 0.8f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.12f, 0.20f), strip.transform);

            _bpBarFill = MakeAnchoredImage("Fill",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero,
                ArcViolet, track.transform);
            _bpBarFill.rectTransform.anchorMax = new Vector2(0f, 1f); // width set in Update

            MakeAnchoredText("â€º", FogGrey, 20, FontStyle.Normal,
                new Vector2(0.93f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, strip.transform);

            var btn = strip.gameObject.AddComponent<Button>();
            btn.targetGraphic = strip;
            btn.onClick.AddListener(() => {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                ScreenManager.Instance.NavigateTo<BattlePassScreen>();
            });
        }

        // â”€â”€ Mode Selector â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildModeSelector()
        {
            var modePanel = AddImage("ModePanel",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -620f), new Vector2(0f, -518f),
                new Color(0f, 0f, 0f, 0f));

            _modeDots = new Image[ModeNames.Length];
            float slotW = 1f / ModeNames.Length;
            for (int i = 0; i < ModeNames.Length; i++)
            {
                int idx = i;
                var pill = MakeAnchoredImage($"Mode{i}",
                    new Vector2(i * slotW + 0.01f, 0.1f),
                    new Vector2((i + 1) * slotW - 0.01f, 0.9f),
                    new Vector2(4f, 4f), new Vector2(-4f, -4f),
                    i == 0 ? ArcViolet : new Color(0.12f, 0.14f, 0.20f), modePanel.transform);
                pill.sprite = MakeRoundRectSprite(200, 72, 36);
                pill.type   = Image.Type.Sliced;
                _modeDots[i] = pill;

                MakeAnchoredText($"{ModeIcons[i]} {ModeNames[i].Split(' ')[0]}",
                    i == 0 ? Color.white : FogGrey, 13, FontStyle.Bold,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, pill.transform);

                var btn = pill.gameObject.AddComponent<Button>();
                btn.targetGraphic = pill;
                btn.onClick.AddListener(() => SelectMode(idx));
            }
        }

        private void SelectMode(int idx)
        {
            _selectedMode = idx;
            AudioManager.Instance?.Play(AudioManager.SfxType.UITransition, 0.05f);
            for (int i = 0; i < _modeDots.Length; i++)
            {
                _modeDots[i].color = i == idx ? ModeColors[idx] : new Color(0.12f, 0.14f, 0.20f);
                var lbl = _modeDots[i].GetComponentInChildren<Text>();
                if (lbl) lbl.color = i == idx ? Color.white : FogGrey;
            }
        }

        // â”€â”€ PLAY Button â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildPlayButton()
        {
            // Large play button â€” arc-violet, center screen
            _playBtnImg = AddImage("PlayBtn",
                new Vector2(0.1f, 1f), new Vector2(0.9f, 1f),
                new Vector2(0f, -780f), new Vector2(0f, -630f),
                ArcViolet);
            _playBtnImg.sprite = MakeRoundRectSprite(800, 150, 36);
            _playBtnImg.type   = Image.Type.Sliced;

            _playLabel = MakeAnchoredText($"{ModeIcons[0]}  PLAY NOW", Color.white, 36, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _playBtnImg.transform);

            _playBtn = _playBtnImg.gameObject.AddComponent<Button>();
            _playBtn.targetGraphic = _playBtnImg;
            var col = _playBtn.colors;
            col.normalColor      = ArcViolet;
            col.highlightedColor = new Color(0.55f, 0.44f, 1f);
            col.pressedColor     = new Color(0.30f, 0.20f, 0.80f);
            col.fadeDuration     = 0.08f;
            _playBtn.colors = col;
            _playBtn.onClick.AddListener(OnPlayClicked);
        }

        // â”€â”€ Badges â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildBadges()
        {
            // Quest progress badge
            _questLabel = AddText("â¬¤ 0/3 Daily Quests", BioMint, 15,
                new Vector2(0f, -830f), FontStyle.Bold);

            // Login reward dot
            _loginDot = AddText("ðŸŽ Daily Reward!", GoldYolk, 14,
                new Vector2(0f, -860f), FontStyle.Normal);
            _loginDot.gameObject.SetActive(false);
        }

        // â”€â”€ Bottom Nav Row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void BuildBottomNav()
        {
            var nav = AddImage("BottomNav",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 120f),
                new Color(0.04f, 0.05f, 0.08f, 0.97f));

            // 4 nav buttons evenly spaced
            string[] icons  = { "ðŸ", "ðŸŽ¨", "ðŸ›’", "ðŸ‘‘" };
            string[] labels = { "Play", "Customize", "Shop", "Battle Pass" };
            System.Action[] actions =
            {
                () => OnPlayClicked(),
                () => ScreenManager.Instance.NavigateTo<CustomizeScreen>(),
                () => ScreenManager.Instance.NavigateTo<ShopScreen>(),
                () => ScreenManager.Instance.NavigateTo<BattlePassScreen>(),
            };

            for (int i = 0; i < icons.Length; i++)
            {
                int idx = i;
                float xMin = i / 4f, xMax = (i + 1) / 4f;
                var tab = MakeAnchoredImage($"Tab{i}",
                    new Vector2(xMin, 0f), new Vector2(xMax, 1f),
                    new Vector2(2f, 2f), new Vector2(-2f, -2f),
                    new Color(0f, 0f, 0f, 0f), nav.transform);
                MakeAnchoredText(icons[i], i == 0 ? ArcViolet : FogGrey, 24, FontStyle.Normal,
                    new Vector2(0f, 0.45f), new Vector2(1f, 1f),
                    Vector2.zero, Vector2.zero, tab.transform);
                MakeAnchoredText(labels[i], i == 0 ? ArcViolet : FogGrey, 11, FontStyle.Normal,
                    new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                    Vector2.zero, Vector2.zero, tab.transform);
                var btn = tab.gameObject.AddComponent<Button>();
                btn.targetGraphic = tab;
                btn.onClick.AddListener(() => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    actions[idx]?.Invoke();
                });
            }
        }

        // â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public override void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            AudioManager.Instance?.PlayMenuMusic();
            StartCoroutine(PulsePlayButton());
            RefreshAsync();
            if (_loginDot && LoginRewardService.Instance != null)
                _loginDot.gameObject.SetActive(LoginRewardService.Instance.HasUnclaimedReward);
        }

        public override void OnExit()
        {
            base.OnExit();
            StopAllCoroutines();
        }

        private void Update()
        {
            // Animate snake preview
            if (_snakeDots == null) return;
            _snakeTime += Time.deltaTime;
            for (int i = 0; i < _snakeDots.Length; i++)
            {
                float t     = _snakeTime - i * 0.12f;
                float x     = Mathf.Cos(t * 1.4f) * (180f - i * 8f);
                float y     = Mathf.Sin(t * 2.1f) * (60f  - i * 3f);
                _snakeDots[i].rectTransform.anchoredPosition = new Vector2(x, y + 60f);
            }
        }

        private IEnumerator PulsePlayButton()
        {
            while (true)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 1.2f;
                    float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.015f;
                    if (_playBtnImg) _playBtnImg.rectTransform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                yield return new WaitForSeconds(0.4f);
            }
        }

        private async void RefreshAsync()
        {
            try { await PlayFabEconomy.RefreshCurrenciesAsync(); }
            catch (Exception e) { Debug.LogWarning($"[HomeScreen] Currency refresh: {e.Message}"); }

            if (_coinsLabel) _coinsLabel.text = $"{PlayFabEconomy.Coins:N0}";
            if (_gemsLabel)  _gemsLabel.text  = $"{PlayFabEconomy.Gems}";

            int bpLevel = PlayFabEconomy.BattlePassLevel;
            if (_bpLabel) _bpLabel.text = $"Lv. {bpLevel}";
            // BUG-11 FIX: Use remote-config XpPerTier instead of hardcoded 100
            float xpPerTier = RemoteConfigService.BattlePassConfig?.XpPerTier ?? 100f;
            if (xpPerTier <= 0f) xpPerTier = 100f;
            float bpFill = Mathf.Clamp01(PlayFabEconomy.BattlePassXP / xpPerTier);
            if (_bpBarFill)
                _bpBarFill.rectTransform.anchorMax = new Vector2(bpFill, 1f);

            int done = 0;
            try { foreach (var q in QuestManager.DailyQuests) if (q.Completed) done++; } catch { }
            if (_questLabel) _questLabel.text = $"â¬¤ {done}/3 Daily Quests";
            if (_questLabel) _questLabel.color = done >= 3 ? GoldYolk : BioMint;
        }

        private void OnPlayClicked()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            // BUG-04 FIX: Use ModeEnums lookup instead of raw int cast to avoid out-of-range
            ScreenManager.Instance.NavigateTo<ModeSelectScreen>(new ModeSelectData
            {
                MapIndex = 0,
                Mode     = ModeEnums[_selectedMode]
            });
        }

        // â”€â”€ Anchored helper builders â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private Image MakeAnchoredImage(string name,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Color color, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Image MakeAnchoredImage(string name,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Sprite sprite, Transform parent)
        {
            var img = MakeAnchoredImage(name, ancMin, ancMax, offMin, offMax, Color.white, parent);
            img.sprite = sprite;
            img.preserveAspect = true;
            return img;
        }

        private Text MakeAnchoredText(string content, Color color, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Transform parent)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = content;
            txt.color = color;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
        }

        private void MakeIconButton(string icon,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Transform parent, System.Action onClick)
        {
            var go = new GameObject("IconBtn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // transparent hit area
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                onClick?.Invoke();
            });
            var lbl = MakeAnchoredText(icon, FogGrey, 26, FontStyle.Normal,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, go.transform);
        }
    }
}
