using System.Collections;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Networking;
using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Matchmaking / lobby screen.
    /// Doc 05 §3.4: live count, rotating tips, cancel button.
    /// </summary>
    public class MatchmakingScreen : UIScreen
    {
        private Text   _titleText;
        private Text   _statusText;
        private Text   _countText;
        private Text   _tipText;
        private Image  _spinnerImg;
        private Button _cancelBtn;

        private int       _mapIndex;
        private MatchMode _mode;
        private bool      _cancelled;

        private static readonly string[] Tips =
        {
            "Boost to escape, but watch your length!",
            "Bigger worms move slower — use speed wisely.",
            "Cut off smaller worms to claim their mass.",
            "Death burst pellets attract crowds — stay alert!",
            "Encircle a worm to force them into you.",
            "The edge of the map is your friend when cornered.",
        };

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
        }

        private void BuildUI()
        {
            AddFullBg(InkVoid);

            // Spinner (animated ring, shown via coroutine)
            _spinnerImg = AddImage("Spinner", Vector2.zero, new Vector2(120f, 120f));
            _spinnerImg.sprite = MakeRingSprite(128, 0.65f, 0.85f);
            _spinnerImg.color  = ArcViolet;

            // Title
            _titleText = AddText("SEARCHING", ArcViolet, 36, new Vector2(0f, 200f), FontStyle.Bold);

            // Count — large, BioMint
            _countText = AddText("0 / 20", BioMint, 32, new Vector2(0f, 130f), FontStyle.Bold);
            _countText.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 60f);

            // Status
            _statusText = AddText("Finding players...", FogGrey, 18, new Vector2(0f, 70f));
            _statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 50f);

            // Tip box
            var tipBg = AddImage("TipBg", new Vector2(0f, -80f), new Vector2(800f, 80f));
            tipBg.color = CardBg;
            tipBg.sprite = MakeRoundRectSprite(800, 80, 16);
            tipBg.type   = Image.Type.Sliced;

            _tipText = AddText("", FogGrey, 15, new Vector2(0f, -80f));
            _tipText.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 70f);

            // Cancel button
            _cancelBtn = AddButton("CANCEL", new Vector2(0f, -240f), new Vector2(220f, 56f),
                new Color(0.15f, 0.15f, 0.22f), OnCancel, 18);
        }

        public override void OnEnter(ScreenManager sm, object data)
        {
            base.OnEnter(sm, data);

            _mapIndex = 0;
            _mode     = MatchMode.FreeForAll;
            if (data is ModeSelectData msd) { _mapIndex = msd.MapIndex; _mode = msd.Mode; }

            _cancelled = false;
            _tipText.text = Tips[UnityEngine.Random.Range(0, Tips.Length)];
            _countText.text = "0 / 20";
            StartCoroutine(SpinnerLoop());
            StartCoroutine(TipRotator());
            StartCoroutine(DoMatchmaking(sm));
        }

        public override void OnExit()
        {
            _cancelled = true;
            base.OnExit();
            StopAllCoroutines();
        }

        private IEnumerator SpinnerLoop()
        {
            while (true)
            {
                _spinnerImg.rectTransform.Rotate(0f, 0f, -200f * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator TipRotator()
        {
            int idx = UnityEngine.Random.Range(0, Tips.Length);
            while (true)
            {
                yield return new WaitForSeconds(4f);
                idx = (idx + 1) % Tips.Length;
                // Fade out → swap → fade in
                float t = 0f;
                while (t < 0.2f) { t += Time.deltaTime; _tipText.color = new Color(FogGrey.r, FogGrey.g, FogGrey.b, 1f - t / 0.2f); yield return null; }
                _tipText.text = Tips[idx];
                t = 0f;
                while (t < 0.2f) { t += Time.deltaTime; _tipText.color = new Color(FogGrey.r, FogGrey.g, FogGrey.b, t / 0.2f); yield return null; }
            }
        }

        private IEnumerator DoMatchmaking(ScreenManager sm)
        {
            // Try real server first
            var ticket = new MatchmakingTicket
            {
                Mode = _mode.ToString(), MapIndex = _mapIndex, Region = "us-east",
            };

            var allocTask = MatchmakerClient.RequestServerAsync(ticket);
            float elapsed = 0f;

            while (!allocTask.IsCompleted && !_cancelled)
            {
                elapsed += 0.3f;
                _statusText.text = allocTask.IsFaulted ? "Connection error — using local server..." : "Finding players...";
                int count = Mathf.RoundToInt(elapsed * 3.7f) % 17 + 3;
                count = Mathf.Min(count, 20);
                _countText.text = $"{count} / 20";
                yield return new WaitForSeconds(0.3f);
            }

            if (_cancelled) yield break;

            if (allocTask.IsFaulted)
            {
                _statusText.text = "Using offline mode...";
                yield return new WaitForSeconds(0.5f);
            }

            _countText.text = "20 / 20";
            _statusText.text = "Match found!";
            yield return new WaitForSeconds(0.4f);

            // Hide the UI screens overlay completely
            ScreenManager.Instance.HideAll();

            // Start arena
            Bootstrapper.Instance.StartArena(_mapIndex, _mode);
        }

        private void OnCancel()
        {
            _cancelled = true;
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            ScreenManager.Instance.NavigateTo<HomeScreen>();
        }

        private static Sprite MakeRingSprite(int res, float innerFrac, float outerFrac)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float h = res / 2f;
            float inner = h * innerFrac, outer = h * outerFrac;
            var pix = new Color32[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = x - h, dy = y - h;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    float a  = 0f;
                    if (d >= inner && d <= outer)
                    {
                        float tt = (d - inner) / (outer - inner);
                        a = 1f - Mathf.Abs(tt * 2f - 1f);
                        a = Mathf.Pow(a, 0.5f);
                    }
                    pix[y * res + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), (float)res);
        }
    }
}
