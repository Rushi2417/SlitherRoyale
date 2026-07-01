using System;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    /// <summary>
    /// Daily Login Reward Calendar — as specified in doc 02 LAUNCH features.
    /// Tracks consecutive login days, grants escalating Coin/Gem rewards.
    /// Backed by PlayerPrefs for now; can be migrated to PlayFab Title Data.
    /// </summary>
    public class LoginRewardService : MonoBehaviour
    {
        public static LoginRewardService Instance { get; private set; }

        private const string LastLoginKey   = "LoginReward_LastDate";
        private const string StreakKey       = "LoginReward_Streak";
        private const string ClaimedTodayKey = "LoginReward_ClaimedToday";

        // Reward schedule (repeats after day 7)
        private static readonly (int coins, int gems)[] DayRewards =
        {
            (50,   0),  // Day 1
            (75,   0),  // Day 2
            (100,  1),  // Day 3
            (125,  0),  // Day 4
            (150,  2),  // Day 5
            (200,  0),  // Day 6
            (300,  5),  // Day 7 (streak bonus)
        };

        public bool HasUnclaimedReward { get; private set; }
        public int  CurrentStreak      { get; private set; }
        public int  TodayCoins         { get; private set; }
        public int  TodayGems          { get; private set; }
        public int  DayInCycle         { get; private set; } // 1-7

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            CheckLoginDay();
        }

        private void CheckLoginDay()
        {
            string todayStr = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastLogin = PlayerPrefs.GetString(LastLoginKey, "");
            CurrentStreak = PlayerPrefs.GetInt(StreakKey, 0);

            if (lastLogin == todayStr)
            {
                // Already logged in today — check if claimed
                HasUnclaimedReward = PlayerPrefs.GetInt(ClaimedTodayKey, 0) == 0;
            }
            else
            {
                // New day — advance streak
                bool isConsecutive = false;
                if (!string.IsNullOrEmpty(lastLogin))
                {
                    if (System.DateTime.TryParse(lastLogin, out var last))
                    {
                        isConsecutive = (System.DateTime.UtcNow.Date - last.Date).Days == 1;
                    }
                }

                if (!isConsecutive)
                    CurrentStreak = 0;

                CurrentStreak = Mathf.Min(CurrentStreak + 1, 365);
                PlayerPrefs.SetInt(StreakKey, CurrentStreak);
                PlayerPrefs.SetString(LastLoginKey, todayStr);
                PlayerPrefs.SetInt(ClaimedTodayKey, 0);
                PlayerPrefs.Save();
                HasUnclaimedReward = true;
            }

            // Compute today's reward
            DayInCycle = ((CurrentStreak - 1) % 7) + 1; // 1-7
            int idx = DayInCycle - 1;
            TodayCoins = DayRewards[idx].coins;
            TodayGems  = DayRewards[idx].gems;
        }

        /// <summary>Claim today's reward. Returns false if already claimed.</summary>
        public bool ClaimTodayReward(System.Action<int, int> onGranted = null)
        {
            if (!HasUnclaimedReward) return false;

            HasUnclaimedReward = false;
            PlayerPrefs.SetInt(ClaimedTodayKey, 1);
            PlayerPrefs.Save();

            onGranted?.Invoke(TodayCoins, TodayGems);
            return true;
        }

        public (int coins, int gems) GetRewardForDay(int dayInCycle)
        {
            int idx = Mathf.Clamp(dayInCycle - 1, 0, DayRewards.Length - 1);
            return DayRewards[idx];
        }
    }
}
