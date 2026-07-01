using Firebase.Analytics;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    public static class AnalyticsService
    {
        private static bool _enabled;

        public static void Initialize()
        {
            _enabled = FirebaseBootstrap.IsInitialized;
        }

        public static void LogMatchStart(string mode, int mapIndex)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("match_started",
                new Parameter("mode", mode),
                new Parameter("map_index", mapIndex));
        }

        public static void LogMatchComplete(string mode, int kills, float score, int rank, int coinsEarned, int bpXpEarned)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("match_completed",
                new Parameter("mode", mode),
                new Parameter("kills", kills),
                new Parameter("score", Mathf.RoundToInt(score)),
                new Parameter("rank", rank),
                new Parameter("coins", coinsEarned),
                new Parameter("bp_xp", bpXpEarned));
        }

        public static void LogPurchase(string productId, string productName, string currency, int amount)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("purchase_completed",
                new Parameter("product_id", productId),
                new Parameter("product_name", productName),
                new Parameter("currency", currency),
                new Parameter(FirebaseAnalytics.ParameterValue, amount));
        }

        public static void LogAdImpression(string placement, string adType)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("ad_impression",
                new Parameter("placement", placement),
                new Parameter("ad_type", adType));
        }

        public static void LogAdRewardGranted(string placement)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("ad_reward_granted",
                new Parameter("placement", placement));
        }

        public static void LogQuestCompleted(string questId, string questType, int rewardCoins)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("quest_completed",
                new Parameter("quest_id", questId),
                new Parameter("quest_type", questType),
                new Parameter("reward_coins", rewardCoins));
        }

        public static void LogBattlePassTierUnlocked(int tier, bool isPremium)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent("bp_tier_unlocked",
                new Parameter("tier", tier),
                new Parameter("is_premium", isPremium ? 1 : 0));
        }
    }
}
