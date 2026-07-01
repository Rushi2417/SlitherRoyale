using System.Collections;
using SlitherRoyale.Client.Audio;
using WormCore;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Mode + Map select screen.
    /// Doc 05 Â§3.3: swipeable mode carousel, map preview, server-controlled rotation.
    /// </summary>
    public class ModeSelectScreen : UIScreen
    {
        private int _selMode;
        private int _selMap;

        private static readonly string[] ModeNames  = { "Free-For-All", "Duos", "1v1 Ranked", "Battle Royale" };
        private static readonly string[] ModeDescs  =
        {
            "Every worm for itself.\nLast one growing wins.",
            "Team up with a partner.\nShare score together.",
            "Best-of-3 duels.\nSmall arena, high skill.",
            "Shrinking zone â€¢ 20 players.\nLast worm alive wins."
        };
        private static readonly string[]    ModeIcons  = { "âš”", "ðŸ‘¥", "ðŸ†", "ðŸŽ¯" };
        // BUG-17 FIX: explicit enum lookup so no out-of-range cast
        private static readonly MatchMode[] ModeEnums  = { MatchMode.FreeForAll, MatchMode.Duos, MatchMode.Ranked1v1, MatchMode.BattleRoyale };
        private static readonly Color[]     ModeColors =
        {
            new Color(0.424f, 0.310f, 1.000f),
            new Color(0.247f, 0.878f, 0.773f),
            new Color(1.000f, 0.788f, 0.302f),
            new Color(1.000f, 0.420f, 0.357f),
        };

        private static readonly string[] MapNames  = { "Neon Grid", "Coral Reef", "Magma Core", "Candy Kingdom", "Space Station", "Haunted Forest" };
        private static readonly string[] MapIcons  = { "âš¡", "ðŸŒŠ", "ðŸŒ‹", "ðŸ¬", "ðŸ›¸", "ðŸ‘»" };
        private static readonly string[] MapDescs  =
        {
            "Speed pads  â€¢  Laser fences",
            "Ocean currents  â€¢  Jellyfish",
            "Shrinking zone  â€¢  Lava pools",
            "Syrup zones  â€¢  Giant pellets",
            "Low gravity  â€¢  Airlock zones",
            "Darkness events  â€¢  Wisps",
        };
        private static readonly Color[] MapColors =
        {
            new Color(0.07f, 0.09f, 0.22f),
            new Color(0.01f, 0.12f, 0.18f),
            new Color(0.18f, 0.04f, 0.01f),
            new Color(0.14f, 0.05f, 0.16f),
            new Color(0.01f, 0.01f, 0.05f),
            new Color(0.03f, 0.01f, 0.04f),
        };

        // Dynamic elements
        private Image  _modeCard;
        private Text   _modeIcon, _modeName, _modeDesc;
        private Image  _mapCard;
        private Text   _mapIcon, _mapName, _mapDesc;
        private Image[] _modeDots, _mapDots;
        private Button  _playBtn;
        private Image   _playBtnImg;

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            AddFullBg(InkVoid);

            // â”€â”€ Header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var header = AddImage("Header",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), new Vector2(0f, 0f),
                new Color(0.04f, 0.05f, 0.08f, 0.95f));
            AddAnchoredText("SELECT MODE & MAP", Color.white, 22, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0f, -10f), Vector2.zero, header.transform);

            // Back button in header
            MakeBtn("â† BACK", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(8f, 8f), new Vector2(100f, -8f),
                new Color(0f, 0f, 0f, 0f), FogGrey, 18, header.transform,
                () => { AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
                        ScreenManager.Instance.NavigateTo<HomeScreen>(); });

            // â”€â”€ Mode Card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _modeCard = AddImage("ModeCard",
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, -390f), new Vector2(0f, -120f),
                CardBg);
            _modeCard.sprite = MakeRoundRectSprite(900, 260, 24);
            _modeCard.type = Image.Type.Sliced;

            _modeIcon = AddAnchoredText("âš”", Color.white, 52, FontStyle.Normal,
                new Vector2(0f, 0.6f), new Vector2(0.2f, 1f),
                Vector2.zero, Vector2.zero, _modeCard.transform);
            _modeName = AddAnchoredText("Free-For-All", ModeColors[0], 24, FontStyle.Bold,
                new Vector2(0.2f, 0.55f), new Vector2(1f, 1f),
                new Vector2(8f, 0f), Vector2.zero, _modeCard.transform);
            _modeDesc = AddAnchoredText("", FogGrey, 15, FontStyle.Normal,
                new Vector2(0.2f, 0.05f), new Vector2(1f, 0.55f),
                new Vector2(8f, 0f), Vector2.zero, _modeCard.transform);

            // Arrow buttons
            MakeArrowBtn("â—€", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(50f, 0f),
                _modeCard.transform, () => ShiftMode(-1));
            MakeArrowBtn("â–¶", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-50f, 0f), new Vector2(0f, 0f),
                _modeCard.transform, () => ShiftMode(1));

            // Mode dots
            _modeDots = BuildDots(_modeCard.transform, ModeNames.Length,
                new Vector2(0.5f, 0f), new Vector2(0f, 16f));

            // â”€â”€ Map Card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _mapCard = AddImage("MapCard",
                new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
                new Vector2(0f, -640f), new Vector2(0f, -398f),
                CardBg);
            _mapCard.sprite = MakeRoundRectSprite(900, 230, 24);
            _mapCard.type = Image.Type.Sliced;

            _mapIcon = AddAnchoredText("âš¡", Color.white, 44, FontStyle.Normal,
                new Vector2(0f, 0.5f), new Vector2(0.18f, 1f),
                Vector2.zero, Vector2.zero, _mapCard.transform);
            _mapName = AddAnchoredText("Neon Grid", ArcViolet, 20, FontStyle.Bold,
                new Vector2(0.18f, 0.55f), new Vector2(1f, 1f),
                new Vector2(8f, 0f), Vector2.zero, _mapCard.transform);
            _mapDesc = AddAnchoredText("", FogGrey, 13, FontStyle.Normal,
                new Vector2(0.18f, 0.05f), new Vector2(1f, 0.55f),
                new Vector2(8f, 0f), Vector2.zero, _mapCard.transform);

            MakeArrowBtn("â—€", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(50f, 0f),
                _mapCard.transform, () => ShiftMap(-1));
            MakeArrowBtn("â–¶", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-50f, 0f), new Vector2(0f, 0f),
                _mapCard.transform, () => ShiftMap(1));

            _mapDots = BuildDots(_mapCard.transform, MapNames.Length,
                new Vector2(0.5f, 0f), new Vector2(0f, 16f));

            // â”€â”€ PLAY Button â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _playBtnImg = AddImage("PlayBtn",
                new Vector2(0.08f, 0f), new Vector2(0.92f, 0f),
                new Vector2(0f, 130f), new Vector2(0f, 260f),
                ArcViolet);
            _playBtnImg.sprite = MakeRoundRectSprite(800, 130, 36);
            _playBtnImg.type = Image.Type.Sliced;
            AddAnchoredText("â–¶  MATCHMAKING", Color.white, 28, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _playBtnImg.transform);
            _playBtn = _playBtnImg.gameObject.AddComponent<Button>();
            _playBtn.targetGraphic = _playBtnImg;
            var col = _playBtn.colors;
            col.normalColor  = ArcViolet;
            col.pressedColor = new Color(0.3f, 0.2f, 0.8f);
            col.fadeDuration = 0.08f;
            _playBtn.colors = col;
            _playBtn.onClick.AddListener(Play);

            RefreshDisplay();
        }

        public override void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);
            if (data is ModeSelectData d) { _selMode = (int)d.Mode; _selMap = d.MapIndex; }
            RefreshDisplay();
        }

        private void ShiftMode(int dir)
        {
            _selMode = (_selMode + dir + ModeNames.Length) % ModeNames.Length;
            RefreshDisplay();
            AudioManager.Instance?.Play(AudioManager.SfxType.UITransition, 0.05f);
            StartCoroutine(CardPop(_modeCard));
        }

        private void ShiftMap(int dir)
        {
            _selMap = (_selMap + dir + MapNames.Length) % MapNames.Length;
            RefreshDisplay();
            AudioManager.Instance?.Play(AudioManager.SfxType.UITransition, 0.05f);
            StartCoroutine(CardPop(_mapCard));
        }

        private void RefreshDisplay()
        {
            // Mode card
            var mc = ModeColors[_selMode];
            _modeCard.color = new Color(mc.r * 0.25f, mc.g * 0.25f, mc.b * 0.35f, 1f);
            _modeIcon.text  = ModeIcons[_selMode];
            _modeName.text  = ModeNames[_selMode];
            _modeName.color = mc;
            _modeDesc.text  = ModeDescs[_selMode];
            for (int i = 0; i < _modeDots.Length; i++)
                _modeDots[i].color = i == _selMode ? mc : new Color(0.3f, 0.3f, 0.4f);

            // Map card
            var map = MapColors[_selMap];
            _mapCard.color = new Color(map.r + 0.04f, map.g + 0.04f, map.b + 0.06f, 1f);
            _mapIcon.text  = MapIcons[_selMap];
            _mapName.text  = MapNames[_selMap];
            _mapDesc.text  = MapDescs[_selMap];
            for (int i = 0; i < _mapDots.Length; i++)
                _mapDots[i].color = i == _selMap ? ArcViolet : new Color(0.3f, 0.3f, 0.4f);
        }

        private IEnumerator CardPop(Image card)
        {
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(t / 0.15f * Mathf.PI) * 0.04f;
                card.rectTransform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            card.rectTransform.localScale = Vector3.one;
        }

        private void Play()
        {
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            // BUG-17 FIX: Use ModeEnums lookup array instead of direct intâ†’MatchMode cast
            ScreenManager.Instance.NavigateTo<MatchmakingScreen>(new ModeSelectData
            {
                MapIndex = _selMap,
                Mode     = ModeEnums[_selMode],
            });
        }

        // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private Image[] BuildDots(Transform parent, int count, Vector2 center, Vector2 size)
        {
            var dots = new Image[count];
            float total = count * (size.x + 6f) - 6f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Dot{i}");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = center;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-total / 2f + i * (size.x + 6f), -10f);
                rt.sizeDelta = size;
                dots[i] = go.AddComponent<Image>();
                dots[i].sprite = MakeCircleSprite(Color.white, 16);
                dots[i].color  = i == 0 ? ArcViolet : new Color(0.3f, 0.3f, 0.4f);
            }
            return dots;
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

        private void MakeArrowBtn(string icon,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Transform parent, System.Action onClick)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.04f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = AddAnchoredText(icon, FogGrey, 22, FontStyle.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, go.transform);
        }

        private void MakeBtn(string label,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            Color bg, Color textColor, int fontSize, Transform parent, System.Action onClick)
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
