using System;
using System.Collections.Generic;

namespace WormCore
{
    public class ComboSystem
    {
        private readonly Dictionary<int, List<float>> _killTimestamps = new Dictionary<int, List<float>>();
        private readonly Dictionary<int, int> _currentStreaks = new Dictionary<int, int>();

        public float ComboWindowSeconds { get; set; } = 10f;

        public event Action<int, string, int> OnComboEvent;

        public void RegisterKill(int killerId, float gameTime)
        {
            if (!_killTimestamps.ContainsKey(killerId))
                _killTimestamps[killerId] = new List<float>();

            var timestamps = _killTimestamps[killerId];
            timestamps.Add(gameTime);
            PruneExpired(killerId, gameTime);

            int streak = timestamps.Count;
            _currentStreaks[killerId] = streak;

            if (streak >= 2)
            {
                string callout = GetCalloutText(streak);
                OnComboEvent?.Invoke(killerId, callout, streak);
            }
        }

        public void Reset(int wormId)
        {
            _killTimestamps.Remove(wormId);
            _currentStreaks.Remove(wormId);
        }

        public int GetCurrentStreak(int wormId)
        {
            return _currentStreaks.TryGetValue(wormId, out var streak) ? streak : 0;
        }

        public void Update(float gameTime)
        {
            var keys = new List<int>(_killTimestamps.Keys);
            foreach (var id in keys)
                PruneExpired(id, gameTime);
        }

        private void PruneExpired(int wormId, float gameTime)
        {
            if (!_killTimestamps.TryGetValue(wormId, out var timestamps)) return;
            timestamps.RemoveAll(t => gameTime - t > ComboWindowSeconds);
            if (timestamps.Count == 0)
                _killTimestamps.Remove(wormId);
            else
                _currentStreaks[wormId] = timestamps.Count;
        }

        public static string GetCalloutText(int streak)
        {
            return streak switch
            {
                2 => "Double Kill",
                3 => "Triple Kill",
                4 => "Multi Kill",
                5 => "Rampage",
                6 => "Dominating",
                7 => "Unstoppable",
                >= 8 => "GODLIKE",
                _ => ""
            };
        }
    }
}
