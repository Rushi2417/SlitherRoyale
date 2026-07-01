using System;
using System.Collections.Generic;
using System.Linq;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Customize/Cosmetics screen.
    /// Doc 05 Â§3.7: Grid of owned skins/trails/emotes.
    /// Locked items shown greyed out with unlock methods visible.
    /// </summary>
    public class CustomizeScreen : UIScreen
    {
        private Text _skinName;
        private Text _rarityLabel;
        private Image _swatch;
        private Image _equipBtnImg;
        private Text _equipBtnText;
        private Button _equipBtn;
        private int _currentIndex;
        private List<Backend.ShopItem> _skins = new List<Backend.ShopItem>();

        // Preview rendering (moving preview snake)
        private Image[] _previewDots;
        private float _previewTime;

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

            // Header
            var header = AddImage("Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), Vector2.zero,
                new Color(0.04f, 0.05f, 0.08f, 0.95f));
            
            AddAnchoredText("WARDROBE", Color.white, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button
            MakeBtn("â† BACK", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(110f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 16, header.transform,
                () => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    ScreenManager.Instance.NavigateTo<HomeScreen>();
                });

            // â”€â”€ Live Preview Area â”€â”€
            var previewFrame = AddImage("PreviewFrame",
                new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.9f),
                new Vector2(0f, -500f), new Vector2(0f, -130f),
                CardBg);
            previewFrame.sprite = MakeRoundRectSprite(900, 360, 24);
            previewFrame.type = Image.Type.Sliced;

            // Next / Prev buttons flanking the preview frame
            MakeArrowBtn("â—€", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, -30f), new Vector2(66f, 30f),
                previewFrame.transform, Prev);
            MakeArrowBtn("â–¶", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-66f, -30f), new Vector2(-16f, 30f),
                previewFrame.transform, Next);

            // Preview Snake Dots (8 segments)
            _previewDots = new Image[8];
            for (int i = 0; i < _previewDots.Length; i++)
            {
                var dot = new GameObject($"PreviewDot{i}");
                dot.transform.SetParent(previewFrame.transform, false);
                var rt = dot.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(i == 0 ? 32f : 22f, i == 0 ? 32f : 22f);
                var img = dot.AddComponent<Image>();
                img.sprite = MakeCircleSprite(Color.white, 32);
                _previewDots[i] = img;
            }

            // Skin Details Box
            var detailsBox = AddImage("DetailsBox",
                new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.5f),
                new Vector2(0f, -230f), new Vector2(0f, -400f),
                new Color(0.06f, 0.08f, 0.12f, 1f));
            detailsBox.sprite = MakeRoundRectSprite(900, 160, 16);
            detailsBox.type = Image.Type.Sliced;

            _skinName = AddAnchoredText("Arc Viper", Color.white, 22, FontStyle.Bold,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.9f),
                Vector2.zero, Vector2.zero, detailsBox.transform);

            _rarityLabel = AddAnchoredText("COMMON SKIN", BioMint, 14, FontStyle.Bold,
                new Vector2(0f, 0.15f), new Vector2(1f, 0.5f),
                Vector2.zero, Vector2.zero, detailsBox.transform);

            // Equip Button
            _equipBtnImg = AddImage("EquipBtn",
                new Vector2(0.1f, 0f), new Vector2(0.9f, 0f),
                new Vector2(0f, 100f), new Vector2(0f, 200f),
                ArcViolet);
            _equipBtnImg.sprite = MakeRoundRectSprite(800, 100, 24);
            _equipBtnImg.type = Image.Type.Sliced;

            _equipBtnText = AddAnchoredText("EQUIP", Color.white, 20, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _equipBtnImg.transform);

            _equipBtn = _equipBtnImg.gameObject.AddComponent<Button>();
            _equipBtn.targetGraphic = _equipBtnImg;
            _equipBtn.onClick.AddListener(ToggleEquip);

            // Extra Info (Secondary selection items: Trails, Emotes placeholder)
            var secondaryGrid = AddImage("SecGrid",
                new Vector2(0.05f, 0f), new Vector2(0.95f, 0f),
                new Vector2(0f, 20f), new Vector2(0f, 85f),
                new Color(0f, 0f, 0f, 0f));
            AddAnchoredText("ðŸŒˆ TRAILS UNLOCKED: ALL", FogGrey, 13, FontStyle.Normal,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero, secondaryGrid.transform);
            AddAnchoredText("ðŸ’¬ EMOTES ACTIVE: 6/6", FogGrey, 13, FontStyle.Normal,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, secondaryGrid.transform);
        }

        public override async void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            _skins.Clear();

            try
            {
                await PlayFabEconomy.RefreshInventoryAsync();
                var catalog = await PlayFabEconomy.GetShopCatalogAsync();
                foreach (var item in catalog.Where(i => i.Category == "Skin" || string.IsNullOrEmpty(i.Category)))
                {
                    _skins.Add(item);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CustomizeScreen] Catalog load failed: {e.Message}");
            }

            if (_skins.Count == 0)
            {
                _skins.Add(new Backend.ShopItem { Id = "default", Name = "Neon Stalker", Rarity = "Common", CoinCost = 0, GemCost = 0 });
            }

            _currentIndex = 0;
            UpdateDisplay();
        }

        private void Prev()
        {
            _currentIndex = (_currentIndex - 1 + _skins.Count) % _skins.Count;
            AudioManager.Instance?.Play(AudioManager.SfxType.UITransition, 0.05f);
            UpdateDisplay();
        }

        private void Next()
        {
            _currentIndex = (_currentIndex + 1) % _skins.Count;
            AudioManager.Instance?.Play(AudioManager.SfxType.UITransition, 0.05f);
            UpdateDisplay();
        }

        private async void ToggleEquip()
        {
            if (_currentIndex < 0 || _currentIndex >= _skins.Count) return;
            var s = _skins[_currentIndex];
            bool owned = PlayFabEconomy.OwnedCosmeticIds.Contains(s.Id) ||
                         PlayFabEconomy.OwnedCosmeticIds.Contains(s.Name) ||
                         s.Id == "default";

            if (!owned && (s.CoinCost > 0 || s.GemCost > 0))
            {
                // Try purchase
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                bool ok = await PlayFabEconomy.PurchaseItemAsync(s.Id, s.CoinCost, s.GemCost);
                if (ok)
                {
                    AudioManager.Instance?.Play(AudioManager.SfxType.DoubleKill); // celebrate sound
                    await PlayFabEconomy.RefreshInventoryAsync();
                    UpdateDisplay();
                }
            }
            else
            {
                // Equip (Local setting or PlayFab key update)
                AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                PlayerPrefs.SetString("EquippedSkin", s.Id);
                PlayerPrefs.Save();
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_currentIndex < 0 || _currentIndex >= _skins.Count) return;
            var s = _skins[_currentIndex];
            bool owned = PlayFabEconomy.OwnedCosmeticIds.Contains(s.Id) ||
                         PlayFabEconomy.OwnedCosmeticIds.Contains(s.Name) ||
                         s.Id == "default";

            _skinName.text = s.Name.ToUpper();
            string rarity = (s.Rarity ?? "Common").ToUpper();
            _rarityLabel.text = $"{rarity} SKIN";

            Color rarityColor = rarity == "LEGENDARY" ? GoldYolk : rarity == "EPIC" ? ArcViolet : rarity == "RARE" ? BioMint : FogGrey;
            _rarityLabel.color = rarityColor;

            // Setup colors on preview snake
            Color previewColor = s.Id == "default" ? ArcViolet : rarityColor;
            for (int i = 0; i < _previewDots.Length; i++)
            {
                float t = i / (float)(_previewDots.Length - 1);
                _previewDots[i].color = Color.Lerp(previewColor, Color.white, t * 0.4f);
            }

            if (owned)
            {
                string eq = PlayerPrefs.GetString("EquippedSkin", "default");
                if (eq == s.Id)
                {
                    _equipBtnText.text = "EQUIPPED";
                    _equipBtnImg.color = BioMint;
                    _equipBtn.interactable = false;
                }
                else
                {
                    _equipBtnText.text = "EQUIP";
                    _equipBtnImg.color = ArcViolet;
                    _equipBtn.interactable = true;
                }
            }
            else
            {
                string costStr = s.GemCost > 0 ? $"UNLOCK FOR {s.GemCost} ðŸ’Ž" : $"UNLOCK FOR {s.CoinCost} ðŸª™";
                _equipBtnText.text = costStr;
                _equipBtnImg.color = GoldYolk;
                _equipBtn.interactable = true;
            }
        }

        private void Update()
        {
            if (_previewDots == null) return;
            _previewTime += Time.deltaTime;
            for (int i = 0; i < _previewDots.Length; i++)
            {
                float t = _previewTime - i * 0.14f;
                float x = Mathf.Cos(t * 2.2f) * (180f - i * 12f);
                float y = Mathf.Sin(t * 3.1f) * (40f - i * 3f);
                _previewDots[i].rectTransform.anchoredPosition = new Vector2(x, y);
            }
        }

        // â”€â”€ Anchored helpers â”€â”€

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

        private void MakeArrowBtn(string icon, Vector2 ancMin, Vector2 ancMax,
            Vector2 offMin, Vector2 offMax, Transform parent, Action onClick)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.05f);
            img.sprite = MakeCircleSprite(Color.white, 64);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            AddAnchoredText(icon, FogGrey, 22, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, go.transform);
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
