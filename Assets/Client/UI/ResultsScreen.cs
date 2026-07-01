using System.Collections;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using SlitherRoyale.Client.Monetization;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Post-match Results screen.
    /// Doc 05 §3.6: final length, kills, time, rewards count-up, double-coins ad, rematch/home.
    /// </summary>
    public class ResultsScreen : UIScreen
    {
        private Text   _headerText;
        private Text   _reasonText;
        private Text   _killsText;
        private Text   _scoreText;
        private Text   _coinsText;
        private Text   _bpText;
        private Button _doubleBtn;
        private Text   _doubleBtnLabel;
        private Button _rematchBtn;
        private Button _homeBtn;

        private int  _baseCoins;
        private int  _baseBPXP;
        private bool _rewardsDoubled;

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            AddFullBg(InkVoid);

            // Subtle gradient overlay at top
            var topGrad = AddImage("TopGrad",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -300f), Vector2.zero,
                new Color(EmberCoral.r, EmberCoral.g, EmberCoral.b, 0.06f));
            topGrad.raycastTarget = false;

            // ── Card ─────────────────────────────────────────────────────────
            var card = AddImage("Card",
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 1f),
                new Vector2(0f, -760f), new Vector2(0f, -110f),
                CardBg);
            card.sprite = MakeRoundRectSprite(900, 640, 28);
            card.type   = Image.Type.Sliced;
            card.raycastTarget = false;

            // Header (YOU DIED / VICTORY! / etc.)
            _headerText = AddChildText("Header", "YOU DIED", EmberCoral, 52, FontStyle.Bold,
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), card.transform);

            // Death reason
            _reasonText = AddChildText("Reason", "", FogGrey, 14, FontStyle.Normal,
                new Vector2(0f, 0.78f), new Vector2(1f, 0.87f), card.transform);

            // Divider
            var div = new GameObject("Div"); div.transform.SetParent(card.transform, false);
            var drt = div.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.05f, 0.76f); drt.anchorMax = new Vector2(0.95f, 0.76f);
            drt.offsetMin = Vector2.zero; drt.offsetMax = new Vector2(0f, 2f);
            div.AddComponent<Image>().color = DividerCol;

            // Stats row
            _killsText = AddChildText("Kills", "⚔ 0 Kills", ArcViolet, 20, FontStyle.Bold,
                new Vector2(0f, 0.60f), new Vector2(0.5f, 0.76f), card.transform);
            _scoreText = AddChildText("Score", "📏 0", FogGrey, 20, FontStyle.Normal,
                new Vector2(0.5f, 0.60f), new Vector2(1f, 0.76f), card.transform);

            // Rewards section
            AddChildText("RewardsLbl", "REWARDS EARNED", FogGrey, 13, FontStyle.Normal,
                new Vector2(0f, 0.54f), new Vector2(1f, 0.60f), card.transform);

            _coinsText = AddChildText("Coins", "+0 Coins", GoldYolk, 28, FontStyle.Bold,
                new Vector2(0f, 0.42f), new Vector2(1f, 0.54f), card.transform);
            _bpText = AddChildText("BPXP", "+0 BP XP", BioMint, 18, FontStyle.Normal,
                new Vector2(0f, 0.35f), new Vector2(1f, 0.42f), card.transform);

            // Double rewards ad button
            _doubleBtn = MakeCardButton("DoubleBtn",
                new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.35f),
                GoldYolk, DoubleRewards, card.transform);
            _doubleBtnLabel = _doubleBtn.GetComponentInChildren<Text>();
            if (_doubleBtnLabel) _doubleBtnLabel.text = "📺  WATCH AD — DOUBLE COINS";

            // Rematch + Home
            _rematchBtn = MakeCardButton("RematchBtn",
                new Vector2(0.05f, 0.08f), new Vector2(0.5f, 0.21f),
                ArcViolet, Rematch, card.transform);
            var remLbl = _rematchBtn.GetComponentInChildren<Text>();
            if (remLbl) remLbl.text = "↺  REMATCH";

            _homeBtn = MakeCardButton("HomeBtn",
                new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.21f),
                new Color(0.14f, 0.16f, 0.24f), GoHome, card.transform);
            var homeLbl = _homeBtn.GetComponentInChildren<Text>();
            if (homeLbl) { homeLbl.text = "🏠  HOME"; homeLbl.color = FogGrey; }
        }

        public override async void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            AudioManager.Instance?.PlayResultsMusic();
            _rewardsDoubled = false;

            if (data is ResultsData d)
            {
                bool isVictory = d.DeathReason?.StartsWith("VICTORY") == true;

                _headerText.text  = isVictory ? "VICTORY!" :
                                    d.Kills >= 5 ? "DOMINATION!" :
                                    d.Kills >= 2 ? "SOLID FIGHT" : "YOU DIED";
                _headerText.color = isVictory || d.Kills >= 5 ? GoldYolk :
                                    d.Kills >= 2 ? ArcViolet : EmberCoral;

                _reasonText.text = d.DeathReason ?? "";
                _killsText.text  = $"⚔ {d.Kills} Kill{(d.Kills == 1 ? "" : "s")}";
                _scoreText.text  = $"📏 {d.Score:N0}";

                _baseCoins = isVictory ? 75 : d.Kills >= 5 ? 45 : d.Kills >= 2 ? 25 : 10;
                _baseBPXP  = isVictory ? 200 : d.Kills >= 5 ? 150 : d.Kills >= 2 ? 80 : 40;

                _doubleBtn.interactable = true;
                if (_doubleBtnLabel) _doubleBtnLabel.text = "📺  WATCH AD — DOUBLE COINS";

                // Count-up animation
                StartCoroutine(CountUp(_baseCoins, _baseBPXP));

                // Backend
                try
                {
                    await PlayFabEconomy.SubmitMatchResultAsync(d.Mode, d.Kills, d.Score, d.Rank);
                    if (d.Rank <= 3) QuestManager.ReportProgress("PlaceTop3", 1);
                    AnalyticsService.LogMatchComplete(d.Mode, d.Kills, d.Score, d.Rank, _baseCoins, _baseBPXP);
                }
                catch (System.Exception e) { Debug.LogWarning($"[Results] Backend: {e.Message}"); }
            }
        }

        private IEnumerator CountUp(int targetCoins, int targetBP)
        {
            float t = 0f, duration = 1.2f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                _coinsText.text = $"+{Mathf.RoundToInt(targetCoins * p):N0} Coins";
                _bpText.text    = $"+{Mathf.RoundToInt(targetBP * p):N0} BP XP";
                yield return null;
            }
            _coinsText.text = $"+{targetCoins:N0} Coins";
            _bpText.text    = $"+{targetBP:N0} BP XP";
        }

        private void DoubleRewards()
        {
            _doubleBtn.interactable = false;
            AdService.ShowRewardedVideo("match_reward",
                onRewarded: () =>
                {
                    _baseCoins *= 2; _baseBPXP *= 2;
                    _rewardsDoubled = true;
                    StartCoroutine(CountUp(_baseCoins, _baseBPXP));
                    if (_doubleBtnLabel) _doubleBtnLabel.text = "✓  DOUBLED!";
                },
                onFailed: _ => { _doubleBtn.interactable = true; });
        }

        private void Rematch()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            ScreenManager.Instance.NavigateTo<ModeSelectScreen>();
        }

        private void GoHome()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            AdService.ShowInterstitial("post_match");
            ScreenManager.Instance.NavigateTo<HomeScreen>();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Text AddChildText(string name, string content, Color color, int size,
            FontStyle style, Vector2 ancMin, Vector2 ancMax, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content; txt.color = color; txt.fontSize = size;
            txt.fontStyle = style; txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
        }

        private Button MakeCardButton(string name,
            Vector2 ancMin, Vector2 ancMax, Color color,
            System.Action onClick, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = new Vector2(0f, 4f); rt.offsetMax = new Vector2(0f, -4f);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = MakeRoundRectSprite(800, 80, 24);
            img.type   = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var col = btn.colors;
            col.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
            col.fadeDuration = 0.08f;
            btn.colors = col;
            btn.onClick.AddListener(() => onClick?.Invoke());
            AddChildText("Lbl", name, Color.white, 18, FontStyle.Bold,
                Vector2.zero, Vector2.one, go.transform);
            return btn;
        }
    }

    public class ResultsData
    {
        public int    Kills       { get; set; }
        public float  Score       { get; set; }
        public string DeathReason { get; set; }
        public string Mode        { get; set; } = "FFA";
        public int    Rank        { get; set; } = 1;
    }
}
