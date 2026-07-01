using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    public class QuestDefinition
    {
        public string Id;
        public string QuestType;
        public int Target;
        public string Description;
        public int CoinReward;
        public int XPReward;
        public int BPXPReward;
        public bool IsDaily;
    }

    public static class QuestManager
    {
        public static List<QuestProgress> DailyQuests { get; private set; } = new List<QuestProgress>();
        public static List<QuestProgress> WeeklyQuests { get; private set; } = new List<QuestProgress>();
        public static event Action OnQuestsUpdated;

        private static List<QuestDefinition> _allDailyDefs;
        private static List<QuestDefinition> _allWeeklyDefs;
        private static HashSet<string> _completedQuestIds = new HashSet<string>();

        private static void EnsureDefinitions()
        {
            if (_allDailyDefs != null) return;
            _allDailyDefs = new List<QuestDefinition>
            {
                new QuestDefinition { Id = "daily_eat_100", QuestType = "EatPellet", Target = 100, Description = "quest_eat_100", CoinReward = 50, XPReward = 20, BPXPReward = 10, IsDaily = true },
                new QuestDefinition { Id = "daily_eat_300", QuestType = "EatPellet", Target = 300, Description = "quest_eat_300", CoinReward = 100, XPReward = 40, BPXPReward = 20, IsDaily = true },
                new QuestDefinition { Id = "daily_kill_1", QuestType = "KillWorm", Target = 1, Description = "quest_kill_1", CoinReward = 75, XPReward = 30, BPXPReward = 15, IsDaily = true },
                new QuestDefinition { Id = "daily_kill_3", QuestType = "KillWorm", Target = 3, Description = "quest_kill_3", CoinReward = 150, XPReward = 60, BPXPReward = 30, IsDaily = true },
                new QuestDefinition { Id = "daily_survive_60", QuestType = "SurviveTime", Target = 60, Description = "quest_survive_60", CoinReward = 30, XPReward = 15, BPXPReward = 5, IsDaily = true },
                new QuestDefinition { Id = "daily_survive_180", QuestType = "SurviveTime", Target = 180, Description = "quest_survive_180", CoinReward = 80, XPReward = 35, BPXPReward = 15, IsDaily = true },
                new QuestDefinition { Id = "daily_boost_30", QuestType = "UseBoost", Target = 30, Description = "quest_boost_30", CoinReward = 40, XPReward = 20, BPXPReward = 10, IsDaily = true },
                new QuestDefinition { Id = "daily_place_top3", QuestType = "PlaceTop3", Target = 1, Description = "quest_place_top3", CoinReward = 120, XPReward = 50, BPXPReward = 25, IsDaily = true },
            };
            _allWeeklyDefs = new List<QuestDefinition>
            {
                new QuestDefinition { Id = "weekly_eat_1000", QuestType = "EatPellet", Target = 1000, Description = "quest_eat_1000", CoinReward = 300, XPReward = 150, BPXPReward = 75, IsDaily = false },
                new QuestDefinition { Id = "weekly_kill_10", QuestType = "KillWorm", Target = 10, Description = "quest_kill_10", CoinReward = 500, XPReward = 250, BPXPReward = 125, IsDaily = false },
                new QuestDefinition { Id = "weekly_survive_600", QuestType = "SurviveTime", Target = 600, Description = "quest_survive_600", CoinReward = 200, XPReward = 100, BPXPReward = 50, IsDaily = false },
                new QuestDefinition { Id = "weekly_win_5", QuestType = "WinMatch", Target = 5, Description = "quest_win_5", CoinReward = 600, XPReward = 300, BPXPReward = 150, IsDaily = false },
                new QuestDefinition { Id = "weekly_boost_300", QuestType = "UseBoost", Target = 300, Description = "quest_boost_300", CoinReward = 250, XPReward = 125, BPXPReward = 60, IsDaily = false },
            };
        }

        public static async Task LoadProgressAsync()
        {
            EnsureDefinitions();
            var tcs = new TaskCompletionSource<bool>();
            PlayFabClientAPI.GetUserData(new GetUserDataRequest
            {
                Keys = new List<string> { "QuestProgress", "CompletedQuests" }
            }, result =>
            {
                DailyQuests.Clear();
                WeeklyQuests.Clear();
                _completedQuestIds.Clear();

                // BUG-10 FIX: load CompletedQuests from proper JSON array
                if (result.Data.TryGetValue("CompletedQuests", out var cq) && !string.IsNullOrEmpty(cq.Value))
                {
                    try
                    {
                        var arr = JsonUtility.FromJson<StringListWrapper>(cq.Value);
                        if (arr?.items != null)
                            foreach (var id in arr.items)
                                if (!string.IsNullOrEmpty(id)) _completedQuestIds.Add(id);
                    }
                    catch
                    {
                        // Fallback: old comma-delimited format for backwards compatibility
                        foreach (var id in cq.Value.Split(','))
                            if (!string.IsNullOrEmpty(id)) _completedQuestIds.Add(id);
                    }
                }

                // BUG-10 FIX: load QuestProgress from proper JSON object
                Dictionary<string, int> savedProgress = null;
                if (result.Data.TryGetValue("QuestProgress", out var qp) && !string.IsNullOrEmpty(qp.Value))
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<QuestProgressWrapper>(qp.Value);
                        if (wrapper?.entries != null)
                        {
                            savedProgress = new Dictionary<string, int>();
                            foreach (var e in wrapper.entries)
                                savedProgress[e.key] = e.value;
                        }
                    }
                    catch { savedProgress = null; }
                }

                InitializeDailyQuests(savedProgress);
                InitializeWeeklyQuests(savedProgress);
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[QuestManager] Load failed: {error.GenerateErrorReport()}");
                InitializeDailyQuests();
                InitializeWeeklyQuests();
                tcs.TrySetResult(false);
            });
            await tcs.Task;
        }

        private static void InitializeDailyQuests(Dictionary<string, int> savedProgress = null)
        {
            DailyQuests.Clear();
            int dayOfYear = DateTime.Now.DayOfYear;
            var rng = new System.Random(dayOfYear);
            var available = _allDailyDefs.FindAll(d => !_completedQuestIds.Contains(d.Id));
            if (available.Count < 3) available = _allDailyDefs;
            for (int i = 0; i < 3 && i < available.Count; i++)
            {
                int idx = rng.Next(available.Count);
                var def = available[idx];
                available.RemoveAt(idx);
                int saved = 0;
                if (savedProgress != null && savedProgress.TryGetValue(def.Id, out int sp)) saved = sp;
                DailyQuests.Add(new QuestProgress
                {
                    Definition = def,
                    Current = saved,
                    Target = def.Target,
                    Completed = _completedQuestIds.Contains(def.Id) || saved >= def.Target,
                    Claimed = false,
                });
            }
        }

        private static void InitializeWeeklyQuests(Dictionary<string, int> savedProgress = null)
        {
            WeeklyQuests.Clear();
            int weekOfYear = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                DateTime.Now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            var rng = new System.Random(weekOfYear);
            var available = _allWeeklyDefs.FindAll(d => !_completedQuestIds.Contains(d.Id));
            if (available.Count < 3) available = _allWeeklyDefs;
            for (int i = 0; i < 3 && i < available.Count; i++)
            {
                int idx = rng.Next(available.Count);
                var def = available[idx];
                available.RemoveAt(idx);
                int saved = 0;
                if (savedProgress != null && savedProgress.TryGetValue(def.Id, out int sp)) saved = sp;
                WeeklyQuests.Add(new QuestProgress
                {
                    Definition = def,
                    Current = saved,
                    Target = def.Target,
                    Completed = _completedQuestIds.Contains(def.Id) || saved >= def.Target,
                    Claimed = false,
                });
            }
        }

        public static void ReportProgress(string questType, int amount)
        {
            foreach (var q in DailyQuests)
                if (!q.Completed && q.Definition.QuestType == questType)
                    q.Current = Math.Min(q.Current + amount, q.Target);
            foreach (var q in WeeklyQuests)
                if (!q.Completed && q.Definition.QuestType == questType)
                    q.Current = Math.Min(q.Current + amount, q.Target);
            CheckCompletions();
            OnQuestsUpdated?.Invoke();
        }

        private static void CheckCompletions()
        {
            foreach (var q in DailyQuests)
                if (!q.Completed && q.Current >= q.Target) q.Completed = true;
            foreach (var q in WeeklyQuests)
                if (!q.Completed && q.Current >= q.Target) q.Completed = true;
        }

        public static async Task ClaimReward(QuestProgress quest)
        {
            if (!quest.Completed || quest.Claimed) return;
            quest.Claimed = true;
            _completedQuestIds.Add(quest.Definition.Id);
            AnalyticsService.LogQuestCompleted(quest.Definition.Id, quest.Definition.QuestType, quest.Definition.CoinReward);
            await SaveProgressAsync();
            OnQuestsUpdated?.Invoke();
        }

        private static async Task SaveProgressAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            // BUG-10 FIX: Use JsonUtility-based serialization instead of hand-rolled
            // string concatenation which corrupts data when quest IDs contain ':' or ','.

            // Serialize CompletedQuests as a JSON array
            var completedWrapper = new StringListWrapper { items = new List<string>(_completedQuestIds) };
            string completedJson = JsonUtility.ToJson(completedWrapper);

            // Serialize QuestProgress as a JSON object with key-value pairs
            var progressWrapper = new QuestProgressWrapper { entries = new List<QuestProgressEntry>() };
            foreach (var q in DailyQuests)
                progressWrapper.entries.Add(new QuestProgressEntry { key = q.Definition.Id, value = q.Current });
            foreach (var q in WeeklyQuests)
                progressWrapper.entries.Add(new QuestProgressEntry { key = q.Definition.Id, value = q.Current });
            string progressJson = JsonUtility.ToJson(progressWrapper);

            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    ["CompletedQuests"] = completedJson,
                    ["QuestProgress"]   = progressJson,
                }
            }, _ => tcs.TrySetResult(true), _ => tcs.TrySetResult(false));
            await tcs.Task;
        }

        // ── JsonUtility-compatible serialization helpers ────────────────────────

        [Serializable]
        private class StringListWrapper
        {
            public List<string> items;
        }

        [Serializable]
        private class QuestProgressWrapper
        {
            public List<QuestProgressEntry> entries;
        }

        [Serializable]
        private class QuestProgressEntry
        {
            public string key;
            public int value;
        }

    public class QuestProgress
    {
        public QuestDefinition Definition;
        public int Current;
        public int Target;
        public bool Completed;
        public bool Claimed;
        public float Progress => Target > 0 ? (float)Current / Target : 0f;
    }
}
