using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    [Serializable]
    public struct ShopItem
    {
        public string Id;
        public string Name;
        public string Description;
        public string Rarity;
        public int CoinCost;
        public int GemCost;
        public string Category;
    }

    [Serializable]
    public struct BattlePassConfig
    {
        public int TotalTiers;
        public int XpPerTier;
        public string SeasonName;
        public List<BattlePassReward> FreeRewards;
        public List<BattlePassReward> PremiumRewards;
    }

    [Serializable]
    public struct BattlePassReward
    {
        public int Tier;
        public string ItemId;
        public string ItemName;
        public string Rarity;
    }

    public static class PlayFabEconomy
    {
        public static event Action OnCurrenciesUpdated;
        public static event Action OnInventoryUpdated;

        public static int Coins { get; private set; }
        public static int Gems { get; private set; }
        public static int BattlePassXP { get; private set; }
        public static int BattlePassLevel { get; private set; }
        public static bool HasPremiumBattlePass { get; private set; }
        public static List<string> OwnedCosmeticIds { get; private set; } = new List<string>();

        public static async Task InitializeAsync()
        {
            await Task.WhenAll(RefreshCurrenciesAsync(), RefreshInventoryAsync());
        }

        public static async Task RefreshCurrenciesAsync()
        {
            var invTcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
            {
                Coins = result.VirtualCurrency.TryGetValue("CN", out var c) ? c : 0;
                Gems = result.VirtualCurrency.TryGetValue("GM", out var g) ? g : 0;
                OnCurrenciesUpdated?.Invoke();
                invTcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] RefreshCurrencies failed: {error.GenerateErrorReport()}");
                invTcs.TrySetResult(false);
            });

            await invTcs.Task;

            var statTcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), result =>
            {
                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == "BattlePassXP")
                        BattlePassXP = stat.Value;
                }
                statTcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetPlayerStatistics failed: {error.GenerateErrorReport()}");
                statTcs.TrySetResult(false);
            });

            await statTcs.Task;

            var dataTcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.GetUserData(new GetUserDataRequest
            {
                Keys = new List<string> { "HasPremiumBP", "BattlePassLevel" }
            }, result =>
            {
                bool hasBp = false;
                if (result.Data.TryGetValue("HasPremiumBP", out var bpRec))
                    bool.TryParse(bpRec.Value, out hasBp);
                HasPremiumBattlePass = hasBp;

                int level = 0;
                if (result.Data.TryGetValue("BattlePassLevel", out var lvRec))
                    int.TryParse(lvRec.Value, out level);
                BattlePassLevel = level;

                dataTcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetUserData failed: {error.GenerateErrorReport()}");
                dataTcs.TrySetResult(false);
            });

            await dataTcs.Task;
        }

        public static async Task RefreshInventoryAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
            {
                OwnedCosmeticIds.Clear();
                foreach (var item in result.Inventory)
                {
                    if (!string.IsNullOrEmpty(item.ItemId))
                        OwnedCosmeticIds.Add(item.ItemId);
                }
                OnInventoryUpdated?.Invoke();
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] RefreshInventory failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(false);
            });

            await tcs.Task;
        }

        public static async Task<List<ShopItem>> GetShopCatalogAsync()
        {
            var tcs = new TaskCompletionSource<List<CatalogItem>>();

            PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest { CatalogVersion = "MainShop" }, result =>
            {
                tcs.TrySetResult(result.Catalog);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetCatalog failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(null);
            });

            var catalog = await tcs.Task;
            if (catalog == null)
                return new List<ShopItem>();

            var shopItems = new List<ShopItem>(catalog.Count);

            foreach (var item in catalog)
            {
                string rarity = "Common";
                string category = "Skin";

                if (item.Tags != null)
                {
                    foreach (var tag in item.Tags)
                    {
                        if (tag.StartsWith("rarity:")) rarity = tag.Substring(7);
                        if (tag.StartsWith("category:")) category = tag.Substring(9);
                    }
                }

                int coinCost = 0;
                int gemCost = 0;

                if (item.VirtualCurrencyPrices != null)
                {
                    if (item.VirtualCurrencyPrices.TryGetValue("CN", out var cc))
                        coinCost = (int)cc;
                    if (item.VirtualCurrencyPrices.TryGetValue("GM", out var gc))
                        gemCost = (int)gc;
                }

                shopItems.Add(new ShopItem
                {
                    Id = item.ItemId,
                    Name = item.DisplayName,
                    Description = item.Description,
                    Rarity = rarity,
                    Category = category,
                    CoinCost = coinCost,
                    GemCost = gemCost
                });
            }

            return shopItems;
        }

        public static async Task<bool> PurchaseItemAsync(string itemId, int coinCost, int gemCost)
        {
            var tcs = new TaskCompletionSource<bool>();

            var request = new PurchaseItemRequest
            {
                ItemId = itemId,
                CatalogVersion = "MainShop"
            };

            if (coinCost > 0)
            {
                request.VirtualCurrency = "CN";
                request.Price = coinCost;
            }
            else
            {
                request.VirtualCurrency = "GM";
                request.Price = gemCost;
            }

            PlayFabClientAPI.PurchaseItem(request, result =>
            {
                Debug.Log($"[PlayFabEconomy] Purchased {itemId} successfully");
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] Purchase {itemId} failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(false);
            });

            return await tcs.Task;
        }

        public static async Task<BattlePassConfig> GetBattlePassConfigAsync()
        {
            var tcs = new TaskCompletionSource<Dictionary<string, string>>();

            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest
            {
                Keys = new List<string>
                {
                    "BattlePassSeasonName",
                    "BattlePassTotalTiers",
                    "BattlePassXpPerTier",
                    "BattlePassFreeRewards",
                    "BattlePassPremiumRewards"
                }
            }, result =>
            {
                tcs.TrySetResult(result.Data);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetTitleData failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(null);
            });

            var data = await tcs.Task;
            if (data == null)
                return default;

            var config = new BattlePassConfig
            {
                SeasonName = data.TryGetValue("BattlePassSeasonName", out var sn) ? sn : "Season 1",
                TotalTiers = data.TryGetValue("BattlePassTotalTiers", out var tt) && int.TryParse(tt, out var tiers) ? tiers : 50,
                XpPerTier = data.TryGetValue("BattlePassXpPerTier", out var xp) && int.TryParse(xp, out var xpt) ? xpt : 1000,
                FreeRewards = new List<BattlePassReward>(),
                PremiumRewards = new List<BattlePassReward>()
            };

            if (data.TryGetValue("BattlePassFreeRewards", out var freeJson) && !string.IsNullOrEmpty(freeJson))
            {
                var wrapper = JsonUtility.FromJson<BattlePassRewardListWrapper>(freeJson);
                if (wrapper?.Rewards != null)
                    config.FreeRewards = new List<BattlePassReward>(wrapper.Rewards);
            }

            if (data.TryGetValue("BattlePassPremiumRewards", out var premJson) && !string.IsNullOrEmpty(premJson))
            {
                var wrapper = JsonUtility.FromJson<BattlePassRewardListWrapper>(premJson);
                if (wrapper?.Rewards != null)
                    config.PremiumRewards = new List<BattlePassReward>(wrapper.Rewards);
            }

            return config;
        }

        public static async Task SubmitMatchResultAsync(string mode, int kills, float score, int rank)
        {
            var statTcs = new TaskCompletionSource<List<StatisticValue>>();

            PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), result =>
            {
                statTcs.TrySetResult(result.Statistics);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] SubmitMatchResult failed: {error.GenerateErrorReport()}");
                statTcs.TrySetResult(null);
            });

            var existing = await statTcs.Task;
            if (existing == null)
                return;

            int totalKills = kills;
            int totalScore = (int)score;
            int matchesPlayed = 1;

            foreach (var stat in existing)
            {
                if (stat.StatisticName == "TotalKills")
                    totalKills += stat.Value;
                else if (stat.StatisticName == "TotalScore")
                    totalScore += stat.Value;
                else if (stat.StatisticName == "MatchesPlayed")
                    matchesPlayed += stat.Value;
            }

            var updateTcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate { StatisticName = "TotalKills", Value = totalKills },
                    new StatisticUpdate { StatisticName = "TotalScore", Value = totalScore },
                    new StatisticUpdate { StatisticName = "MatchesPlayed", Value = matchesPlayed }
                }
            }, result =>
            {
                Debug.Log($"[PlayFabEconomy] Match result submitted: mode={mode}, kills={kills}, score={score}, rank={rank}");
                updateTcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] UpdatePlayerStatistics failed: {error.GenerateErrorReport()}");
                updateTcs.TrySetResult(false);
            });

            await updateTcs.Task;

            int coinReward = Mathf.RoundToInt(score * 0.5f + kills * 10f + Mathf.Max(0, 20 - rank) * 5f);
            int gemReward = rank == 1 ? 5 : 0;
            var curTcs = new TaskCompletionSource<bool>();
            PlayFabClientAPI.AddUserVirtualCurrency(new AddUserVirtualCurrencyRequest
            {
                VirtualCurrency = "CN",
                Amount = coinReward
            }, r =>
            {
                Coins += coinReward;
                if (gemReward > 0)
                {
                    PlayFabClientAPI.AddUserVirtualCurrency(new AddUserVirtualCurrencyRequest
                    {
                        VirtualCurrency = "GM",
                        Amount = gemReward
                    }, gr => { Gems += gemReward; curTcs.TrySetResult(true); },
                       ge => { Debug.LogError($"[PlayFabEconomy] Gem grant failed: {ge.GenerateErrorReport()}"); curTcs.TrySetResult(true); });
                }
                else curTcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] Coin grant failed: {error.GenerateErrorReport()}");
                curTcs.TrySetResult(false);
            });
            await curTcs.Task;
        }

        public static async Task<List<PlayerLeaderboardEntry>> GetLeaderboardAsync(string statName, int maxResults = 100)
        {
            var tcs = new TaskCompletionSource<List<PlayerLeaderboardEntry>>();

            PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
            {
                StatisticName = statName,
                MaxResultsCount = maxResults
            }, result =>
            {
                tcs.TrySetResult(result.Leaderboard);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetLeaderboard failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(null);
            });

            var leaderboard = await tcs.Task;
            return leaderboard ?? new List<PlayerLeaderboardEntry>();
        }

        public static async Task<List<PlayerLeaderboardEntry>> GetGlobalLeaderboardAsync()
        {
            return await GetLeaderboardAsync("TotalScore", 100);
        }

        public static async Task<List<PlayerLeaderboardEntry>> GetFriendsLeaderboardAsync()
        {
            var tcs = new TaskCompletionSource<List<PlayerLeaderboardEntry>>();

            PlayFabClientAPI.GetFriendLeaderboard(new GetFriendLeaderboardRequest
            {
                StatisticName = "TotalScore",
                MaxResultsCount = 100
            }, result =>
            {
                tcs.TrySetResult(result.Leaderboard);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetFriendLeaderboard failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(null);
            });

            var leaderboard = await tcs.Task;
            return leaderboard ?? new List<PlayerLeaderboardEntry>();
        }

        public static async Task<List<FriendInfo>> GetFriendsListAsync()
        {
            return await GetFriendsAsync();
        }

        public static async Task<bool> RemoveFriendAsync(string friendPlayFabId)
        {
            var tcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.RemoveFriend(new RemoveFriendRequest
            {
                FriendPlayFabId = friendPlayFabId
            }, result =>
            {
                Debug.Log($"[PlayFabEconomy] Removed friend: {friendPlayFabId}");
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] RemoveFriend failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(false);
            });

            return await tcs.Task;
        }

        public static async Task<bool> AddFriendAsync(string friendPlayFabId)
        {
            var tcs = new TaskCompletionSource<bool>();

            PlayFabClientAPI.AddFriend(new AddFriendRequest
            {
                FriendPlayFabId = friendPlayFabId
            }, result =>
            {
                Debug.Log($"[PlayFabEconomy] Added friend: {friendPlayFabId}");
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] AddFriend failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(false);
            });

            return await tcs.Task;
        }

        public static async Task<List<FriendInfo>> GetFriendsAsync()
        {
            var tcs = new TaskCompletionSource<List<FriendInfo>>();

            PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest(), result =>
            {
                tcs.TrySetResult(result.Friends);
            }, error =>
            {
                Debug.LogError($"[PlayFabEconomy] GetFriendsList failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(null);
            });

            var friends = await tcs.Task;
            return friends ?? new List<FriendInfo>();
        }

        [Serializable]
        private class BattlePassRewardListWrapper
        {
            public BattlePassReward[] Rewards;
        }
    }
}
