using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    /// <summary>
    /// Remote config keys for soft-launch tuning (set via PlayFab TitleData):
    /// - ad_interstitial_max_per_session (int, default 3)
    /// - ad_interstitial_max_per_hour (int, default 5)
    /// - matchmaking_fill_time (float, default 5.0)
    /// - matchmaking_target_players (int, default 16)
    /// - battle_pass_xp_per_tier (int, default 1000)
    /// - BattlePassSeasonName, BattlePassTotalTiers, BattlePassFreeRewards, BattlePassPremiumRewards
    /// - economy_coin_reward_multiplier (float, default 1.0)
    /// - economy_gem_reward_top1 (int, default 5)
    /// </summary>
    public static class RemoteConfigService
    {
        private static Dictionary<string, string> _config = new Dictionary<string, string>();
        public static bool IsLoaded { get; private set; }

        public static event Action OnConfigUpdated;

        public static async Task LoadAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), result =>
            {
                _config = result.Data ?? new Dictionary<string, string>();
                IsLoaded = true;
                OnConfigUpdated?.Invoke();
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[RemoteConfig] Failed: {error.GenerateErrorReport()}");
                IsLoaded = true;
                tcs.TrySetResult(false);
            });
            await tcs.Task;
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            if (_config.TryGetValue(key, out string val) && int.TryParse(val, out int result))
                return result;
            return defaultValue;
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (_config.TryGetValue(key, out string val) && float.TryParse(val, out float result))
                return result;
            return defaultValue;
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return _config.TryGetValue(key, out string val) ? val : defaultValue;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (_config.TryGetValue(key, out string val) && bool.TryParse(val, out bool result))
                return result;
            return defaultValue;
        }

        // ── Convenience typed configs ──────────────────────────────────────────

        /// <summary>
        /// BUG-11 FIX: Typed config for Battle Pass so screens don't hard-code
        /// the divisor "100" — reads XpPerTier from PlayFab TitleData key
        /// "battle_pass_xp_per_tier" (default 1000 as per doc 16).
        /// </summary>
        public static BattlePassRemoteConfig BattlePassConfig => new BattlePassRemoteConfig
        {
            XpPerTier  = GetInt("battle_pass_xp_per_tier", 1000),
            TotalTiers = GetInt("BattlePassTotalTiers", 10),
        };
    }

    /// <summary>Typed view over the battle-pass remote config keys.</summary>
    public class BattlePassRemoteConfig
    {
        public float XpPerTier  { get; set; }
        public int   TotalTiers { get; set; }
    }
}
