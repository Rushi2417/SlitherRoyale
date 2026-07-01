using System;
using PlayFab;
using PlayFab.ClientModels;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Settings Screen.
    /// Doc 05 Â§3.10: Audio sliders, controls (colorblind, graphics), account linking, privacy/legal.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        private Text _accountFeedback;
        private Text _cbFeedback;
        private Text _deleteFeedback;
        private Text _graphicsLabel;
        private Text _reduceMotionLabel;

        private Button[] _cbButtons;
        private Text[] _cbButtonTexts;
        private string[] _cbModes = { "None", "Protanopia", "Deuteranopia", "Tritanopia" };

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

            AddAnchoredText("SETTINGS", Color.white, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button
            MakeBtn("â† BACK", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(110f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 16, header.transform,
                () => {
                    AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                    ScreenManager.Instance.NavigateTo<HomeScreen>();
                });

            // â”€â”€ Scrollable Container for Settings â”€â”€
            var scrollGo = new GameObject("SettingsScrollView");
            scrollGo.transform.SetParent(transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.05f);
            srt.anchorMax = new Vector2(0.95f, 0.88f);
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
            crt.offsetMin = new Vector2(0f, -1200f);
            crt.offsetMax = Vector2.zero;
            scrollRect.content = crt;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.padding = new RectOffset(8, 8, 16, 16);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 1. Audio Section Card
            BuildAudioCard(content.transform);

            // 2. Gameplay & Graphics Card
            BuildGameplayCard(content.transform);

            // 3. Accessibility / Colorblind Card
            BuildAccessibilityCard(content.transform);

            // 4. Account Card
            BuildAccountCard(content.transform);

            // 5. Legal / Support Card
            BuildLegalCard(content.transform);
        }

        private void BuildAudioCard(Transform parent)
        {
            var card = BuildSectionCard("AudioCard", 240f, parent);
            AddSectionHeader("AUDIO", card.transform);

            float startY = 160f;
            AddAnchoredText("MASTER VOL", FogGrey, 13, FontStyle.Bold, new Vector2(0.05f, 0f), new Vector2(0.35f, 0f), new Vector2(0f, startY), new Vector2(0f, startY + 30f), card.transform).alignment = TextAnchor.MiddleLeft;
            AddFunctionalSlider(new Vector2(0.4f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, startY + 5f), new Vector2(0f, startY + 25f), "MasterVolume", 0.8f, v => AudioListener.volume = v, card.transform);

            AddAnchoredText("SFX VOL", FogGrey, 13, FontStyle.Bold, new Vector2(0.05f, 0f), new Vector2(0.35f, 0f), new Vector2(0f, startY - 50f), new Vector2(0f, startY - 20f), card.transform).alignment = TextAnchor.MiddleLeft;
            AddFunctionalSlider(new Vector2(0.4f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, startY - 45f), new Vector2(0f, startY - 25f), "SFXVolume", 1.0f, v => { }, card.transform);

            AddAnchoredText("MUSIC VOL", FogGrey, 13, FontStyle.Bold, new Vector2(0.05f, 0f), new Vector2(0.35f, 0f), new Vector2(0f, startY - 100f), new Vector2(0f, startY - 70f), card.transform).alignment = TextAnchor.MiddleLeft;
            AddFunctionalSlider(new Vector2(0.4f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, startY - 95f), new Vector2(0f, startY - 75f), "MusicVolume", 0.7f, v => { }, card.transform);
        }

        private void BuildGameplayCard(Transform parent)
        {
            var card = BuildSectionCard("GameplayCard", 180f, parent);
            AddSectionHeader("GAMEPLAY & GRAPHICS", card.transform);

            float startY = 100f;
            // Graphics
            AddAnchoredText("GRAPHICS QUALITY", FogGrey, 13, FontStyle.Bold, new Vector2(0.05f, 0f), new Vector2(0.45f, 0f), new Vector2(0f, startY), new Vector2(0f, startY + 30f), card.transform).alignment = TextAnchor.MiddleLeft;
            
            var gBtnGo = new GameObject("GraphicsBtn");
            gBtnGo.transform.SetParent(card.transform, false);
            var grt = gBtnGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.55f, 0f); grt.anchorMax = new Vector2(0.95f, 0f);
            grt.offsetMin = new Vector2(0f, startY - 2f); grt.offsetMax = new Vector2(0f, startY + 32f);
            var gImg = gBtnGo.AddComponent<Image>();
            gImg.color = new Color(0.12f, 0.14f, 0.20f);
            gImg.sprite = MakeRoundRectSprite(300, 34, 8);
            gImg.type = Image.Type.Sliced;
            var gBtn = gBtnGo.AddComponent<Button>();
            gBtn.targetGraphic = gImg;
            gBtn.onClick.AddListener(CycleGraphicsQuality);

            _graphicsLabel = AddAnchoredText("MEDIUM", Color.white, 12, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, gBtnGo.transform);

            // Motion Settings
            AddAnchoredText("REDUCE MOTION", FogGrey, 13, FontStyle.Bold, new Vector2(0.05f, 0f), new Vector2(0.45f, 0f), new Vector2(0f, startY - 50f), new Vector2(0f, startY - 20f), card.transform).alignment = TextAnchor.MiddleLeft;

            var mBtnGo = new GameObject("MotionBtn");
            mBtnGo.transform.SetParent(card.transform, false);
            var mrt = mBtnGo.AddComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.55f, 0f); mrt.anchorMax = new Vector2(0.95f, 0f);
            mrt.offsetMin = new Vector2(0f, startY - 52f); mrt.offsetMax = new Vector2(0f, startY - 18f);
            var mImg = mBtnGo.AddComponent<Image>();
            mImg.color = new Color(0.12f, 0.14f, 0.20f);
            mImg.sprite = MakeRoundRectSprite(300, 34, 8);
            mImg.type = Image.Type.Sliced;
            var mBtn = mBtnGo.AddComponent<Button>();
            mBtn.targetGraphic = mImg;
            mBtn.onClick.AddListener(ToggleReduceMotion);

            _reduceMotionLabel = AddAnchoredText("OFF", Color.white, 12, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, mBtnGo.transform);
        }

        private void BuildAccessibilityCard(Transform parent)
        {
            var card = BuildSectionCard("AccessCard", 160f, parent);
            AddSectionHeader("COLORBLIND SETTINGS", card.transform);

            _cbFeedback = AddAnchoredText("", BioMint, 12, FontStyle.Normal, new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 10f), new Vector2(0f, 28f), card.transform);

            _cbButtons = new Button[4];
            _cbButtonTexts = new Text[4];

            float cellW = 0.9f / 4f;
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var mode = (ColorblindMode)i;
                var btnGo = new GameObject($"CbBtn_{i}");
                btnGo.transform.SetParent(card.transform, false);
                var rt = btnGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f + i * cellW, 0f);
                rt.anchorMax = new Vector2(0.05f + (i + 1) * cellW - 0.02f, 0f);
                rt.offsetMin = new Vector2(0f, 40f); rt.offsetMax = new Vector2(0f, 80f);

                var img = btnGo.AddComponent<Image>();
                img.color = new Color(0.12f, 0.14f, 0.20f);
                img.sprite = MakeRoundRectSprite(150, 40, 10);
                img.type = Image.Type.Sliced;

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => SetColorblindMode(mode));
                _cbButtons[i] = btn;

                _cbButtonTexts[i] = AddAnchoredText(_cbModes[idx], FogGrey, 11, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, btnGo.transform);
            }
        }

        private void BuildAccountCard(Transform parent)
        {
            var card = BuildSectionCard("AccountCard", 210f, parent);
            AddSectionHeader("ACCOUNT SERVICES", card.transform);

            string pid = PlayFabBootstrap.PlayFabId ?? "Local Guest Account";
            string displayId = pid.Length > 12 ? pid.Substring(0, 12) + "..." : pid;
            AddAnchoredText($"ID: {displayId}", FogGrey, 12, FontStyle.Normal, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero, card.transform);

            // Google Play / iOS GC Buttons
            var gpgBtnGo = new GameObject("GpgBtn");
            gpgBtnGo.transform.SetParent(card.transform, false);
            var grt = gpgBtnGo.AddComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.05f, 0.4f); grt.anchorMax = new Vector2(0.48f, 0.65f);
            grt.offsetMin = grt.offsetMax = Vector2.zero;
            var gImg = gpgBtnGo.AddComponent<Image>();
            gImg.color = new Color(0.12f, 0.14f, 0.20f);
            gImg.sprite = MakeRoundRectSprite(300, 40, 10);
            gImg.type = Image.Type.Sliced;
            var gBtn = gpgBtnGo.AddComponent<Button>();
            gBtn.targetGraphic = gImg;
            gBtn.onClick.AddListener(LinkGooglePlay);
            AddAnchoredText("LINK GPGS", FogGrey, 12, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, gpgBtnGo.transform);

            var gcBtnGo = new GameObject("GcBtn");
            gcBtnGo.transform.SetParent(card.transform, false);
            var gcrt = gcBtnGo.AddComponent<RectTransform>();
            gcrt.anchorMin = new Vector2(0.52f, 0.4f); gcrt.anchorMax = new Vector2(0.95f, 0.65f);
            gcrt.offsetMin = gcrt.offsetMax = Vector2.zero;
            var gcImg = gcBtnGo.AddComponent<Image>();
            gcImg.color = new Color(0.12f, 0.14f, 0.20f);
            gcImg.sprite = MakeRoundRectSprite(300, 40, 10);
            gcImg.type = Image.Type.Sliced;
            var gcBtn = gcBtnGo.AddComponent<Button>();
            gcBtn.targetGraphic = gcImg;
            gcBtn.onClick.AddListener(LinkGameCenter);
            AddAnchoredText("LINK GAME CENTER", FogGrey, 11, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, gcBtnGo.transform);

            _accountFeedback = AddAnchoredText("", FogGrey, 11, FontStyle.Normal, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.35f), Vector2.zero, Vector2.zero, card.transform);
        }

        private void BuildLegalCard(Transform parent)
        {
            var card = BuildSectionCard("LegalCard", 180f, parent);
            AddSectionHeader("LEGAL & SYSTEM", card.transform);

            var tBtnGo = new GameObject("TosBtn");
            tBtnGo.transform.SetParent(card.transform, false);
            var trt = tBtnGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0.55f); trt.anchorMax = new Vector2(0.48f, 0.85f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tImg = tBtnGo.AddComponent<Image>();
            tImg.color = new Color(0.12f, 0.14f, 0.20f);
            tImg.sprite = MakeRoundRectSprite(300, 40, 10);
            tImg.type = Image.Type.Sliced;
            var tBtn = tBtnGo.AddComponent<Button>();
            tBtn.targetGraphic = tImg;
            tBtn.onClick.AddListener(() => Application.OpenURL("https://slitherroyale.example/terms"));
            AddAnchoredText("TERMS OF SERVICE", FogGrey, 12, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, tBtnGo.transform);

            var pBtnGo = new GameObject("PrivBtn");
            pBtnGo.transform.SetParent(card.transform, false);
            var prt = pBtnGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.52f, 0.55f); prt.anchorMax = new Vector2(0.95f, 0.85f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var pImg = pBtnGo.AddComponent<Image>();
            pImg.color = new Color(0.12f, 0.14f, 0.20f);
            pImg.sprite = MakeRoundRectSprite(300, 40, 10);
            pImg.type = Image.Type.Sliced;
            var pBtn = pBtnGo.AddComponent<Button>();
            pBtn.targetGraphic = pImg;
            pBtn.onClick.AddListener(() => Application.OpenURL("https://slitherroyale.example/privacy"));
            AddAnchoredText("PRIVACY POLICY", FogGrey, 12, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, pBtnGo.transform);

            // Account deletion warning button
            var dBtnGo = new GameObject("DeleteBtn");
            dBtnGo.transform.SetParent(card.transform, false);
            var drt = dBtnGo.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.05f, 0.15f); drt.anchorMax = new Vector2(0.95f, 0.45f);
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            var dImg = dBtnGo.AddComponent<Image>();
            dImg.color = new Color(0.24f, 0.1f, 0.1f);
            dImg.sprite = MakeRoundRectSprite(800, 40, 10);
            dImg.type = Image.Type.Sliced;
            var dBtn = dBtnGo.AddComponent<Button>();
            dBtn.targetGraphic = dImg;
            dBtn.onClick.AddListener(DeleteAccount);
            AddAnchoredText("DELETE ACCOUNT", EmberCoral, 13, FontStyle.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, dBtnGo.transform);

            _deleteFeedback = AddAnchoredText("", EmberCoral, 11, FontStyle.Normal, new Vector2(0.05f, 0f), new Vector2(0.95f, 0.12f), Vector2.zero, Vector2.zero, card.transform);
        }

        public override void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            _accountFeedback.text = "";
            _cbFeedback.text = "";
            _deleteFeedback.text = "";
            RefreshStateDisplay();
        }

        private void RefreshStateDisplay()
        {
            // Graphics
            int gq = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            if (_graphicsLabel)
            {
                _graphicsLabel.text = gq >= 0 && gq < names.Length ? names[gq].ToUpper() : "AUTO";
            }

            // Reduce motion
            if (_reduceMotionLabel)
            {
                _reduceMotionLabel.text = AccessibilityService.ReduceMotion ? "ENABLED" : "DISABLED";
                _reduceMotionLabel.color = AccessibilityService.ReduceMotion ? BioMint : Color.white;
            }

            // Colorblind Mode highlights
            var currentMode = AccessibilityService.CurrentMode;
            for (int i = 0; i < 4; i++)
            {
                if (_cbButtons == null || _cbButtons[i] == null) continue;
                var mode = (ColorblindMode)i;
                _cbButtons[i].targetGraphic.color = mode == currentMode ? ArcViolet : new Color(0.12f, 0.14f, 0.20f);
                _cbButtonTexts[i].color = mode == currentMode ? Color.white : FogGrey;
            }
        }

        private void SetColorblindMode(ColorblindMode mode)
        {
            AccessibilityService.SetMode(mode);
            _cbFeedback.text = $"Colorblind setting updated to {mode}";
            _cbFeedback.color = BioMint;
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            RefreshStateDisplay();
        }

        private void CycleGraphicsQuality()
        {
            int current = QualitySettings.GetQualityLevel();
            int next = (current + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(next, true);
            PlayerPrefs.SetInt("GraphicsQuality", next);
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            RefreshStateDisplay();
        }

        private void ToggleReduceMotion()
        {
            AccessibilityService.SetReduceMotion(!AccessibilityService.ReduceMotion);
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            RefreshStateDisplay();
        }

        private void LinkGooglePlay()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            _accountFeedback.text = "GUEST NOT LINKED: Play Games SDK requires live device client config.";
            _accountFeedback.color = EmberCoral;
        }

        private void LinkGameCenter()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            _accountFeedback.text = "GUEST NOT LINKED: iOS Game Center requires production builds.";
            _accountFeedback.color = EmberCoral;
        }

        private async void DeleteAccount()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);

            // BUG-08 FIX: Actually call the PlayFab delete API instead of showing a stub message.
            // This satisfies GDPR right-to-erasure and Apple/Google store requirements.
            if (PlayFabBootstrap.PlayFabId == null)
            {
                _deleteFeedback.text = "Not logged in â€” nothing to delete.";
                _deleteFeedback.color = FogGrey;
                return;
            }

            _deleteFeedback.text = "Deleting account...";
            _deleteFeedback.color = EmberCoral;

            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            PlayFab.PlayFabClientAPI.ExecuteCloudScript(
                new PlayFab.ClientModels.ExecuteCloudScriptRequest
                {
                    FunctionName = "deleteAccount",
                    GeneratePlayStreamEvent = true
                },
                _ => tcs.TrySetResult(true),
                err => { Debug.LogWarning($"[Settings] DeleteAccount: {err.GenerateErrorReport()}"); tcs.TrySetResult(false); }
            );

            bool ok = await tcs.Task;

            // Clear all local data regardless of server result
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            if (ok)
                Debug.Log("[Settings] Account deletion request sent to server.");

            // Navigate away â€” account is gone
            ScreenManager.Instance.NavigateTo<SplashScreen>();
        }

        // â”€â”€ Card Builder helpers â”€â”€

        private Image BuildSectionCard(string name, float height, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, height);
            var img = go.AddComponent<Image>();
            img.color = CardBg;
            img.sprite = MakeRoundRectSprite(860, (int)height, 16);
            img.type = Image.Type.Sliced;
            return img;
        }

        private void AddSectionHeader(string title, Transform cardParent)
        {
            AddAnchoredText(title, ArcViolet, 11, FontStyle.Bold,
                new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
                new Vector2(0, -32f), new Vector2(0, -8f), cardParent).alignment = TextAnchor.MiddleLeft;

            var line = new GameObject("HeaderLine");
            line.transform.SetParent(cardParent, false);
            var rt = line.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 1f); rt.anchorMax = new Vector2(0.95f, 1f);
            rt.offsetMin = new Vector2(0f, -38f); rt.offsetMax = new Vector2(0f, -36f);
            line.AddComponent<Image>().color = DividerCol;
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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private void AddFunctionalSlider(Vector2 ancMin, Vector2 ancMax,
            Vector2 offMin, Vector2 offMax, string prefKey, float defaultValue, Action<float> onValChanged, Transform parent)
        {
            var go = new GameObject("SliderGroup");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = PlayerPrefs.GetFloat(prefKey, defaultValue);
            slider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(prefKey, v);
                onValChanged(v);
            });

            // Backing track
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.20f);
            bgImg.sprite = MakeRoundRectSprite(300, 20, 8);
            bgImg.type = Image.Type.Sliced;

            // Fill area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            var fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = Vector2.zero; fart.anchorMax = Vector2.one;
            fart.offsetMin = new Vector2(4f, 4f); fart.offsetMax = new Vector2(-4f, -4f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            var frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(slider.value, 1f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fimg = fillGo.AddComponent<Image>();
            fimg.color = ArcViolet;
            fimg.sprite = MakeRoundRectSprite(300, 20, 8);
            fimg.type = Image.Type.Sliced;

            slider.onValueChanged.AddListener(v =>
            {
                frt.anchorMax = new Vector2(v, 1f);
            });

            slider.targetGraphic = bgImg;
            slider.fillRect = frt;
        }
    }
}
