using System;
using System.Collections.Generic;
using PlayFab.ClientModels;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Leaderboard Screen.
    /// Shows top scores (Global vs Friends) in a clean leaderboard list.
    /// Uses PlayerLeaderboardEntry from PlayFab.ClientModels.
    /// </summary>
    public class LeaderboardScreen : UIScreen
    {
        private Text _title;
        private Transform _scrollContent;
        private Image _globalBtnImg;
        private Text  _globalBtnTxt;
        private Image _friendsBtnImg;
        private Text  _friendsBtnTxt;
        private bool  _showFriends;
        private List<GameObject> _entryRows = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            AddFullBg(InkVoid);

            // Subtle top gradient
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

            _title = MakeChildText("GLOBAL LEADERBOARDS", Color.white, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button
            MakeChildBtn("â† BACK",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(110f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 16, header.transform,
                () => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    ScreenManager.Instance.NavigateTo<HomeScreen>();
                });

            // â”€â”€ Tabs Row â”€â”€
            var tabsRow = AddImage("TabsRow",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -180f), new Vector2(0f, -120f),
                new Color(0f, 0f, 0f, 0f));

            // Global Tab
            var gBtnGo = new GameObject("GlobalBtn");
            gBtnGo.transform.SetParent(tabsRow.transform, false);
            var grt = gBtnGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 0f); grt.anchorMax = new Vector2(0.48f, 1f);
            grt.offsetMin = grt.offsetMax = Vector2.zero;
            _globalBtnImg = gBtnGo.AddComponent<Image>();
            _globalBtnImg.color = ArcViolet;
            _globalBtnImg.sprite = MakeRoundRectSprite(400, 60, 16);
            _globalBtnImg.type = Image.Type.Sliced;
            var gBtn = gBtnGo.AddComponent<Button>();
            gBtn.targetGraphic = _globalBtnImg;
            gBtn.onClick.AddListener(() => { _showFriends = false; Refresh(); });
            _globalBtnTxt = MakeChildText("GLOBAL", Color.white, 15, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, gBtnGo.transform);

            // Friends Tab
            var fBtnGo = new GameObject("FriendsBtn");
            fBtnGo.transform.SetParent(tabsRow.transform, false);
            var frt = fBtnGo.AddComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.52f, 0f); frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            _friendsBtnImg = fBtnGo.AddComponent<Image>();
            _friendsBtnImg.color = CardBg;
            _friendsBtnImg.sprite = MakeRoundRectSprite(400, 60, 16);
            _friendsBtnImg.type = Image.Type.Sliced;
            var fBtn = fBtnGo.AddComponent<Button>();
            fBtn.targetGraphic = _friendsBtnImg;
            fBtn.onClick.AddListener(() => { _showFriends = true; Refresh(); });
            _friendsBtnTxt = MakeChildText("FRIENDS", FogGrey, 15, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fBtnGo.transform);

            // â”€â”€ Scroll List â”€â”€
            var scrollGo = new GameObject("LeaderboardScrollView");
            scrollGo.transform.SetParent(transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f);
            srt.anchorMax = new Vector2(0.95f, 0.87f);
            srt.offsetMin = new Vector2(0f, 40f);
            srt.offsetMax = new Vector2(0f, -40f);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vrt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot     = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            _scrollContent = content.transform;
            scrollRect.content = crt;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.padding = new RectOffset(8, 8, 16, 16);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public override void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            _showFriends = false;
            Refresh();
        }

        private async void Refresh()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            _title.text = _showFriends ? "FRIENDS LEADERBOARD" : "GLOBAL LEADERBOARDS";
            _globalBtnImg.color  = _showFriends ? CardBg : ArcViolet;
            _globalBtnTxt.color  = _showFriends ? FogGrey : Color.white;
            _friendsBtnImg.color = _showFriends ? ArcViolet : CardBg;
            _friendsBtnTxt.color = _showFriends ? Color.white : FogGrey;

            // Clear old rows
            foreach (var row in _entryRows) Destroy(row);
            _entryRows.Clear();

            try
            {
                List<PlayerLeaderboardEntry> entries;
                entries = _showFriends
                    ? await PlayFabEconomy.GetFriendsLeaderboardAsync()
                    : await PlayFabEconomy.GetGlobalLeaderboardAsync();

                if (entries == null || entries.Count == 0)
                    BuildEmptyRow("No rankings yet â€” play a match to get on the board!");
                else
                    foreach (var e in entries) BuildEntryRow(e);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardScreen] Refresh: {ex.Message}");
                BuildEmptyRow("Offline â€” connect to the internet to view rankings.");
            }
        }

        private void BuildEntryRow(PlayerLeaderboardEntry entry)
        {
            // Position is 0-indexed in PlayFab
            int rank = (entry.Position) + 1;

            var row = new GameObject($"Row_{rank}");
            row.transform.SetParent(_scrollContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 76f);

            var bgImg = row.AddComponent<Image>();
            bgImg.color = CardBg;
            bgImg.sprite = MakeRoundRectSprite(860, 76, 12);
            bgImg.type = Image.Type.Sliced;
            _entryRows.Add(row);

            Color rankColor = rank == 1 ? GoldYolk : rank == 2 ? ArcViolet : rank == 3 ? BioMint : FogGrey;

            // Rank badge
            var badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(row.transform, false);
            var brt = badgeGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.02f, 0.15f); brt.anchorMax = new Vector2(0.12f, 0.85f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bImg = badgeGo.AddComponent<Image>();
            bImg.color  = rank <= 3 ? rankColor : new Color(0.15f, 0.15f, 0.22f);
            bImg.sprite = MakeCircleSprite(Color.white, 48);

            MakeChildText(rank.ToString(), rank <= 3 ? Color.black : Color.white, 14, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, badgeGo.transform);

            // Name
            var nameTxt = MakeChildText(entry.DisplayName ?? "Anonymous", Color.white, 15, FontStyle.Bold,
                new Vector2(0.15f, 0f), new Vector2(0.65f, 1f),
                Vector2.zero, Vector2.zero, row.transform);
            nameTxt.alignment = TextAnchor.MiddleLeft;

            // Score
            var scoreTxt = MakeChildText($"{entry.StatValue:N0} pts", rankColor, 14, FontStyle.Bold,
                new Vector2(0.65f, 0f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero, row.transform);
            scoreTxt.alignment = TextAnchor.MiddleRight;
        }

        private void BuildEmptyRow(string message)
        {
            var row = new GameObject("EmptyRow");
            row.transform.SetParent(_scrollContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 120f);

            var bgImg = row.AddComponent<Image>();
            bgImg.color = CardBg;
            bgImg.sprite = MakeRoundRectSprite(860, 120, 16);
            bgImg.type = Image.Type.Sliced;
            _entryRows.Add(row);

            MakeChildText(message, FogGrey, 13, FontStyle.Normal,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                Vector2.zero, Vector2.zero, row.transform);
        }

        // â”€â”€ Local layout helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private Text MakeChildText(string content, Color color, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Transform parent)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var txt = go.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = content; txt.color = color; txt.fontSize = size;
            txt.fontStyle = style;  txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
        }

        private void MakeChildBtn(string label,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Color bg, Color textColor, int fontSize, Transform parent, Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>(); img.color = bg;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            MakeChildText(label, textColor, fontSize, FontStyle.Normal,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, go.transform);
        }
    }
}
