using System;
using System.Collections.Generic;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using SlitherRoyale.Client.Monetization;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Battle Pass Screen.
    /// Doc 05 Â§3.9: Horizontal/Vertical scrollable tier track, free vs premium rows,
    /// current tier marked, purchase premium track.
    /// </summary>
    public class BattlePassScreen : UIScreen
    {
        private Text _levelText;
        private Text _xpLabel;
        private Image _barFill;
        private Transform _scrollContent;
        private List<GameObject> _tierRows = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            AddFullBg(InkVoid);

            // Top gradient overlay
            var topGrad = AddImage("TopGrad",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -240f), Vector2.zero,
                new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0.08f));
            topGrad.raycastTarget = false;

            // â”€â”€ Header â”€â”€
            var header = AddImage("Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), Vector2.zero,
                new Color(0.04f, 0.05f, 0.08f, 0.95f));

            AddAnchoredText("BATTLE PASS", Color.white, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button
            MakeBtn("â† BACK", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(110f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 16, header.transform,
                () => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    ScreenManager.Instance.NavigateTo<HomeScreen>();
                });

            // â”€â”€ Progress Block (Tier Info & XP) â”€â”€
            var progBox = AddImage("ProgBox",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -300f), new Vector2(0f, -120f),
                CardBg);
            progBox.sprite = MakeRoundRectSprite(900, 180, 24);
            progBox.type   = Image.Type.Sliced;

            _levelText = AddAnchoredText("LEVEL 1", GoldYolk, 30, FontStyle.Bold,
                new Vector2(0f, 0.6f), new Vector2(0.5f, 0.9f),
                Vector2.zero, Vector2.zero, progBox.transform);

            _xpLabel = AddAnchoredText("0 / 100 XP", FogGrey, 13, FontStyle.Normal,
                new Vector2(0f, 0.38f), new Vector2(0.5f, 0.6f),
                Vector2.zero, Vector2.zero, progBox.transform);

            // XP Progress Bar track
            var track = MakeAnchoredImage("Track",
                new Vector2(0.05f, 0.15f), new Vector2(0.45f, 0.3f),
                Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.12f, 0.20f), progBox.transform);
            track.sprite = MakeRoundRectSprite(400, 15, 6);
            track.type   = Image.Type.Sliced;

            _barFill = MakeAnchoredImage("Fill",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero,
                ArcViolet, track.transform);
            _barFill.sprite = MakeRoundRectSprite(400, 15, 6);
            _barFill.type   = Image.Type.Sliced;

            // Purchase Premium Button
            var premBtnGo = new GameObject("PremBtn");
            premBtnGo.transform.SetParent(progBox.transform, false);
            var prt = premBtnGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.55f, 0.2f);
            prt.anchorMax = new Vector2(0.95f, 0.8f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            var pbImg = premBtnGo.AddComponent<Image>();
            pbImg.color = GoldYolk;
            pbImg.sprite = MakeRoundRectSprite(360, 108, 20);
            pbImg.type = Image.Type.Sliced;

            var pBtn = premBtnGo.AddComponent<Button>();
            pBtn.targetGraphic = pbImg;
            pBtn.onClick.AddListener(() => {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                IAPService.Purchase("battle_pass");
            });

            AddAnchoredText("GET PREMIUM\n$4.99", Color.black, 15, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, premBtnGo.transform);

            // â”€â”€ Scroll View for Tiers â”€â”€
            var scrollGo = new GameObject("BPScrollView");
            scrollGo.transform.SetParent(transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f);
            srt.anchorMax = new Vector2(0.95f, 0.82f);
            srt.offsetMin = new Vector2(0f, 40f);
            srt.offsetMax = new Vector2(0f, -40f);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vrt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot     = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(0f, -800f);
            crt.offsetMax = Vector2.zero;
            _scrollContent = content.transform;
            scrollRect.content = crt;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.padding = new RectOffset(8, 8, 16, 16);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public override async void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            await PlayFabEconomy.RefreshCurrenciesAsync();
            int level = PlayFabEconomy.BattlePassLevel;
            int xp    = PlayFabEconomy.BattlePassXP;

            _levelText.text = $"LEVEL {level}";

            var cfg = await PlayFabEconomy.GetBattlePassConfigAsync();
            int requiredXp = cfg.XpPerTier > 0 ? cfg.XpPerTier : 100;
            _xpLabel.text   = $"{xp} / {requiredXp} XP";

            float fill = requiredXp > 0 ? (float)xp / requiredXp : 0f;
            _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);

            // Clear old tier rows
            foreach (var tr in _tierRows) Destroy(tr);
            _tierRows.Clear();

            // Populate ~10 mock/real tiers for display
            for (int i = 1; i <= 10; i++)
            {
                BuildTierRow(i, level);
            }
        }

        private void BuildTierRow(int tier, int playerLevel)
        {
            var row = new GameObject($"TierRow_{tier}");
            row.transform.SetParent(_scrollContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 110f);

            var bgImg = row.AddComponent<Image>();
            bool isCurrent = tier == playerLevel;
            bool isClaimed = tier < playerLevel;
            
            bgImg.color = isCurrent ? new Color(0.12f, 0.12f, 0.22f) : CardBg;
            bgImg.sprite = MakeRoundRectSprite(860, 110, 16);
            bgImg.type = Image.Type.Sliced;
            _tierRows.Add(row);

            // Tier number
            var badge = MakeAnchoredImage("Badge",
                new Vector2(0.03f, 0.15f), new Vector2(0.15f, 0.85f),
                Vector2.zero, Vector2.zero,
                isClaimed ? BioMint : isCurrent ? GoldYolk : new Color(0.2f, 0.2f, 0.25f), row.transform);
            badge.sprite = MakeCircleSprite(Color.white, 64);
            
            AddAnchoredText(tier.ToString(), isClaimed ? Color.black : Color.white, 16, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, badge.transform);

            // Free Reward details
            string freeReward = GetFreeReward(tier);
            AddAnchoredText("FREE TRACK", FogGrey, 10, FontStyle.Bold,
                new Vector2(0.18f, 0.55f), new Vector2(0.55f, 0.9f),
                Vector2.zero, Vector2.zero, row.transform).alignment = TextAnchor.MiddleLeft;

            AddAnchoredText(freeReward, Color.white, 14, FontStyle.Bold,
                new Vector2(0.18f, 0.15f), new Vector2(0.55f, 0.55f),
                Vector2.zero, Vector2.zero, row.transform).alignment = TextAnchor.MiddleLeft;

            // Premium Reward details
            string premReward = GetPremiumReward(tier);
            AddAnchoredText("â˜… PREMIUM TRACK", GoldYolk, 10, FontStyle.Bold,
                new Vector2(0.58f, 0.55f), new Vector2(0.95f, 0.9f),
                Vector2.zero, Vector2.zero, row.transform).alignment = TextAnchor.MiddleLeft;

            AddAnchoredText(premReward, BioMint, 14, FontStyle.Bold,
                new Vector2(0.58f, 0.15f), new Vector2(0.95f, 0.55f),
                Vector2.zero, Vector2.zero, row.transform).alignment = TextAnchor.MiddleLeft;
        }

        private string GetFreeReward(int tier) => tier switch
        {
            1 => "100 ðŸª™",
            2 => "5 ðŸ’Ž",
            3 => "Neon Glow Trail",
            4 => "150 ðŸª™",
            5 => "Common Skin Reroll",
            6 => "200 ðŸª™",
            7 => "10 ðŸ’Ž",
            8 => "Angry Emote",
            9 => "300 ðŸª™",
            10 => "Epic Gold Skin",
            _ => "100 ðŸª™"
        };

        private string GetPremiumReward(int tier) => tier switch
        {
            1 => "500 ðŸª™",
            2 => "Cyber Snake Skin",
            3 => "30 ðŸ’Ž",
            4 => "Animated Rainbow Trail",
            5 => "Epic Loot Box",
            6 => "1,000 ðŸª™",
            7 => "75 ðŸ’Ž",
            8 => "Flex Victory Emote",
            9 => "2,000 ðŸª™",
            10 => "Legendary Dragon Skin",
            _ => "500 ðŸª™"
        };

        private Text AddAnchoredText(string content, Color color, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Transform parent)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = content; txt.color = color; txt.fontSize = size;
            txt.fontStyle = style; txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
        }

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

        private void MakeBtn(string label, Vector2 ancMin, Vector2 ancMax,
            Vector2 offMin, Vector2 offMax, Color bg, Color textColor, int fontSize, Transform parent, Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            AddAnchoredText(label, textColor, fontSize, FontStyle.Normal,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, go.transform);
        }
    }
}
