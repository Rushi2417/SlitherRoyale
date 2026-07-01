using System.Collections.Generic;
using UnityEngine;
using WormCore;

namespace SlitherRoyale.Server
{
    public class ServerMatchmaker : MonoBehaviour
    {
        private HashSet<int> _botIds;
        private int _humanCount;
        private int _targetPlayerCount = 16;
        private int _maxPlayerCount = 20;
        private float _fillTimer;
        private float _fillTime = 5f;
        private int _nextBotId = 1000;
        private MatchMode _mode = MatchMode.FreeForAll;

        public int TargetPlayerCount => _targetPlayerCount;
        public int HumanCount => _humanCount;
        public int BotCount => _botIds?.Count ?? 0;
        public int TotalCount => _humanCount + BotCount;

        public delegate void OnBotSpawnDelegate(int botId);
        public delegate void OnBotRemovedDelegate(int botId);
        public event OnBotSpawnDelegate OnBotSpawn;
        public event OnBotRemovedDelegate OnBotRemoved;

        public void Initialize(int humanCount, MatchMode mode = MatchMode.FreeForAll)
        {
            _mode = mode;
            var cfg = ModeConfig.GetDefault(mode);
            _targetPlayerCount = cfg.MaxPlayers;
            _maxPlayerCount = cfg.MaxPlayers;
            _botIds = new HashSet<int>();
            _humanCount = humanCount;
            _fillTimer = _fillTime;
        }

        public void OnPlayerConnected()
        {
            _humanCount++;
            _fillTimer = _fillTime;
            TryRemoveBots();
        }

        public void OnPlayerDisconnected()
        {
            _humanCount--;
            if (_humanCount < 0) _humanCount = 0;
        }

        public void Tick(float deltaTime)
        {
            if (TotalCount >= _maxPlayerCount) return;

            _fillTimer -= deltaTime;
            if (_fillTimer <= 0f && TotalCount < _targetPlayerCount)
            {
                _fillTimer = _fillTime * 0.5f;
                int needed = _targetPlayerCount - TotalCount;
                if (_mode == MatchMode.Ranked1v1) needed = Mathf.Min(needed, 2 - TotalCount);
                else if (needed > 3) needed = 3;
                if (needed <= 0) return;
                for (int i = 0; i < needed; i++)
                {
                    int botId = _nextBotId++;
                    _botIds.Add(botId);
                    OnBotSpawn?.Invoke(botId);
                }
            }
        }

        // ── Natural attrition: remove bots gradually so they "die" in gameplay ──────
        /// <summary>
        /// Fired when a bot should be naturally removed — i.e. marked as a target-for-death.
        /// The server game loop should stop stepping this bot so it sits still and gets killed
        /// by other worms, rather than being abruptly despawned. This matches doc 06 §2 step 6.
        /// </summary>
        public event System.Action<int> OnBotScheduleRemoval;

        private float _attritionTimer;
        private const float AttritionInterval = 8f; // remove at most 1 bot per 8 seconds

        private void TryRemoveBots()
        {
            if (_botIds.Count == 0) return;
            int excess = (_humanCount + _botIds.Count) - _targetPlayerCount;
            if (excess <= 0) return;

            _attritionTimer -= 1f; // called once per real-player-join event
            if (_attritionTimer > 0f) return;
            _attritionTimer = AttritionInterval;

            // Schedule the first excess bot for natural attrition (not instant removal)
            foreach (int botId in _botIds)
            {
                OnBotScheduleRemoval?.Invoke(botId);
                // Note: _botIds.Remove(botId) is called by ServerGameLoop
                // when the bot's WormState.IsDead becomes true naturally.
                break;
            }
        }


        public bool IsBot(int playerId) => _botIds.Contains(playerId);

        public void RemoveBotFromSet(int botId)
        {
            _botIds.Remove(botId);
            OnBotRemoved?.Invoke(botId);
        }
    }
}
