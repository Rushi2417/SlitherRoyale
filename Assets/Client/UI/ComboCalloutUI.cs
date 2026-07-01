using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    public class ComboCalloutUI : MonoBehaviour
    {
        private Text _text;
        private CanvasGroup _group;
        private Coroutine _hideRoutine;

        private static readonly Color ArcViolet = new Color(0.42f, 0.31f, 1f);
        private static readonly Color EmberCoral = new Color(1f, 0.42f, 0.36f);
        private static readonly Color GoldYolk = new Color(1f, 0.79f, 0.3f);

        private void Awake()
        {
            _text = GetComponent<Text>();
            if (_text == null)
                _text = gameObject.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 36;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.fontStyle = FontStyle.Bold;

            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        public void ShowCallout(string text, int streak)
        {
            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _text.text = text;
            _text.color = streak >= 5 ? EmberCoral : (streak >= 3 ? GoldYolk : ArcViolet);
            _group.alpha = 1f;

            _hideRoutine = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float timer = 1.2f;
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                float alpha = timer > 0.5f ? 1f : timer / 0.5f;
                _group.alpha = alpha;
                yield return null;
            }
            _group.alpha = 0f;
            _text.text = "";
        }
    }
}
