using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        private Text _text;
        private StringBuilder _sb;

        private void Awake()
        {
            _sb = new StringBuilder(256);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
                if (canvas == null)
                    canvas = FindAnyObjectByType<Canvas>();
            }
            _text = GetComponent<Text>();
            if (_text == null)
                _text = gameObject.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 16;
            _text.color = new Color(0.66f, 0.69f, 0.76f);
            _text.alignment = TextAnchor.UpperLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.fontStyle = FontStyle.Bold;
            _text.text = "";
        }

        public void UpdateLeaderboard(List<WormState> allWorms, int localPlayerId, List<string> names)
        {
            _sb.Clear();
            _sb.AppendLine("<color=#FFC94D>â”€â”€ LEADERBOARD â”€â”€</color>");

            var sorted = new List<(float mass, int id, int idx)>(allWorms.Count);
            for (int i = 0; i < allWorms.Count; i++)
            {
                if (!allWorms[i].IsDead)
                    sorted.Add((allWorms[i].Mass, allWorms[i].Id, i));
            }
            sorted.Sort((a, b) => b.mass.CompareTo(a.mass));

            int displayCount = Mathf.Min(10, sorted.Count);
            int playerRank = -1;

            for (int i = 0; i < displayCount; i++)
            {
                int wormIdx = sorted[i].idx;
                int id = sorted[i].id;
                string name = (id == localPlayerId) ? "YOU" :
                    (names != null && wormIdx < names.Count ? names[wormIdx] : $"Worm {id}");

                if (id == localPlayerId) playerRank = i;

                string colorTag = id == localPlayerId ? "#6C4FFF" : "#A9B0C3";
                string rankStr = i == 0 ? "ðŸ‘‘" : $"#{i + 1}";
                _sb.AppendLine($"<color={colorTag}>{rankStr} {name} - {sorted[i].mass:F0}</color>");
            }

            if (playerRank < 0 || playerRank >= 10)
            {
                var playerState = allWorms.Find(w => w.Id == localPlayerId);
                if (!playerState.IsDead)
                {
                    _sb.AppendLine($"<color=#6C4FFF>... #{displayCount + 1}+ YOU - {playerState.Mass:F0}</color>");
                }
            }

            _text.text = _sb.ToString();
        }
    }
}
