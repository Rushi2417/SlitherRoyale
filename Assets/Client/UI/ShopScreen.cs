using System;
using System.Collections.Generic;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Shop Screen.
    /// Doc 05 §3.8: Currency display, tabs/items scrollable list, IAP and cosmetics purchase.
    /// </summary>
    public class ShopScreen : UIScreen
    {
        private Text _coinsLabel;
        private Text _gemsLabel;
        private Transform _scrollContent;
        private List<Backend.ShopItem> _items = new List<Backend.ShopItem>();
        private List<GameObject> _itemRows = new List<GameObject>();

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

            // ── Header ──
            var header = AddImage("Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), Vector2.zero,
                new Color(0.04f, 0.05f, 0.08f, 0.95f));

            AddAnchoredText("ARCADE SHOP", Color.white, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button
            MakeBtn("← BACK", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(110f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 16, header.transform,
                () => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    ScreenManager.Instance.NavigateTo<HomeScreen>();
                });

            // ── Currency HUD Row ──
            var hud = AddImage("Hud",
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0f, -180f), new Vector2(0f, -120f),
                CardBg);
            hud.sprite = MakeRoundRectSprite(900, 60, 16);
            hud.type   = Image.Type.Sliced;

            // Coins
            AddAnchoredText("🪙", GoldYolk, 22, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(0.2f, 1f),
                Vector2.zero, Vector2.zero, hud.transform);
            _coinsLabel = AddAnchoredText("---", GoldYolk, 18, FontStyle.Bold,
                new Vector2(0.2f, 0f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero, hud.transform);

            // Gems
            AddAnchoredText("💎", BioMint, 22, FontStyle.Bold,
                new Vector2(0.5f, 0f), new Vector2(0.7f, 1f),
                Vector2.zero, Vector2.zero, hud.transform);
            _gemsLabel = AddAnchoredText("---", BioMint, 18, FontStyle.Bold,
                new Vector2(0.7f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, hud.transform);

            // ── Scroll View for Items ──
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f);
            srt.anchorMax = new Vector2(0.95f, 0.88f);
            srt.offsetMin = new Vector2(0f, 40f);
            srt.offsetMax = new Vector2(0f, -40f);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            // Mask viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vrt;

            // Content container
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
            layout.spacing = 16f;
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
            UpdateCurrencies();

            // Clear old rows
            foreach (var r in _itemRows) Destroy(r);
            _itemRows.Clear();

            try
            {
                _items = await PlayFabEconomy.GetShopCatalogAsync();
                foreach (var item in _items)
                {
                    BuildShopRow(item);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ShopScreen] Catalog load failed: {e.Message}");
            }
        }

        private void UpdateCurrencies()
        {
            if (_coinsLabel) _coinsLabel.text = $"{PlayFabEconomy.Coins:N0}";
            if (_gemsLabel)  _gemsLabel.text  = $"{PlayFabEconomy.Gems}";
        }

        private void BuildShopRow(Backend.ShopItem item)
        {
            var row = new GameObject("ShopRow");
            row.transform.SetParent(_scrollContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 96f);

            var bgImg = row.AddComponent<Image>();
            bool isPremium = item.GemCost > 0;
            bgImg.color = isPremium ? new Color(0.12f, 0.12f, 0.20f) : CardBg;
            bgImg.sprite = MakeRoundRectSprite(860, 96, 16);
            bgImg.type = Image.Type.Sliced;
            _itemRows.Add(row);

            // Icon Prefix based on type
            string iconStr = item.Category == "Skin" ? "🎨" : item.Id.Contains("pass") ? "👑" : "💎";
            AddAnchoredText(iconStr, Color.white, 30, FontStyle.Normal,
                new Vector2(0f, 0f), new Vector2(0.15f, 1f),
                Vector2.zero, Vector2.zero, row.transform);

            // Item details
            string desc = item.Category == "Skin" ? $"Skins: {item.Rarity ?? "Common"}" : "Utility Upgrade";
            AddAnchoredText(item.Name.ToUpper(), Color.white, 16, FontStyle.Bold,
                new Vector2(0.15f, 0.45f), new Vector2(0.6f, 0.9f),
                new Vector2(8f, 0f), Vector2.zero, row.transform).alignment = TextAnchor.MiddleLeft;

            AddAnchoredText(desc, FogGrey, 12, FontStyle.Normal,
                new Vector2(0.15f, 0.1f), new Vector2(0.6f, 0.45f),
                new Vector2(8f, 0f), Vector2.zero, row.transform).alignment = TextAnchor.MiddleLeft;

            // Purchase Button on right
            bool hasCoinCost = item.CoinCost > 0;
            bool hasGemCost = item.GemCost > 0;
            string costStr = hasGemCost ? $"{item.GemCost} 💎" : hasCoinCost ? $"{item.CoinCost} 🪙" : "FREE";
            Color btnColor = hasGemCost ? BioMint : hasCoinCost ? GoldYolk : ArcViolet;

            var buyBtnGo = new GameObject("BuyBtn");
            buyBtnGo.transform.SetParent(row.transform, false);
            var brt = buyBtnGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.65f, 0.15f);
            brt.anchorMax = new Vector2(0.96f, 0.85f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var bImg = buyBtnGo.AddComponent<Image>();
            bImg.color = btnColor;
            bImg.sprite = MakeRoundRectSprite(220, 60, 16);
            bImg.type = Image.Type.Sliced;

            var btn = buyBtnGo.AddComponent<Button>();
            btn.targetGraphic = bImg;

            AddAnchoredText(costStr, isPremium ? Color.black : Color.white, 14, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, buyBtnGo.transform);

            btn.onClick.AddListener(async () =>
            {
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                bImg.color = new Color(btnColor.r * 0.6f, btnColor.g * 0.6f, btnColor.b * 0.6f);
                btn.interactable = false;

                bool ok = await PlayFabEconomy.PurchaseItemAsync(item.Id, item.CoinCost, item.GemCost);
                if (ok)
                {
                    AudioManager.Instance?.Play(AudioManager.SfxType.DoubleKill); // celebrate purchase sound
                    await PlayFabEconomy.RefreshCurrenciesAsync();
                    UpdateCurrencies();
                }

                bImg.color = btnColor;
                btn.interactable = true;
            });
        }

        private Text AddAnchoredText(string content, Color color, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Transform parent)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content; txt.color = color; txt.fontSize = size;
            txt.fontStyle = style; txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
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
