using System;
using System.Collections.Generic;
using SlitherRoyale.Client.Backend;
using UnityEngine;

namespace SlitherRoyale.Client.Monetization
{
    public static class IAPService
    {
        public static bool IsInitialized { get; private set; }

        public static event Action<string> OnPurchaseComplete;
        public static event Action<string, string> OnPurchaseFailed;

        public static readonly Dictionary<string, IAPProduct> Products = new Dictionary<string, IAPProduct>
        {
            ["gem_pack_small"] = new IAPProduct { Id = "gem_pack_small", Name = "10 Gems", Price = "$0.99", GemReward = 10, ProductType = IAPProductType.Consumable },
            ["gem_pack_medium"] = new IAPProduct { Id = "gem_pack_medium", Name = "50 Gems", Price = "$3.99", GemReward = 50, ProductType = IAPProductType.Consumable },
            ["gem_pack_large"] = new IAPProduct { Id = "gem_pack_large", Name = "150 Gems", Price = "$9.99", GemReward = 150, ProductType = IAPProductType.Consumable },
            ["battle_pass"] = new IAPProduct { Id = "battle_pass", Name = "Battle Pass Season 1", Price = "$4.99", ProductType = IAPProductType.Consumable },
            ["remove_ads"] = new IAPProduct { Id = "remove_ads", Name = "Remove Ads", Price = "$2.99", ProductType = IAPProductType.NonConsumable },
            ["starter_pack"] = new IAPProduct { Id = "starter_pack", Name = "Starter Pack", Price = "$1.99", GemReward = 50, ProductType = IAPProductType.Consumable },
        };

        public static void Initialize()
        {
            Debug.Log("[IAPService] Initialize - requires Unity IAP SDK");
            IsInitialized = true;
        }

        public static void Purchase(string productId)
        {
            if (!IsInitialized || !Products.TryGetValue(productId, out var product))
            {
                OnPurchaseFailed?.Invoke(productId, "Product not found");
                return;
            }

            Debug.Log($"[IAPService] Purchase: {product.Name} ({product.Price})");
            SimulatePurchase(product);
        }

        private static async void SimulatePurchase(IAPProduct product)
        {
            await System.Threading.Tasks.Task.Delay(500);

            bool verified = await ServerReceiptValidation.ValidatePurchaseAsync(product.Id, "simulated_receipt_12345");
            if (verified)
            {
                Debug.Log($"[IAPService] Purchase verified: {product.Name}");
                OnPurchaseComplete?.Invoke(product.Id);
                GrantProduct(product);
            }
            else
            {
                Debug.LogError($"[IAPService] Purchase validation FAILED: {product.Name}");
                OnPurchaseFailed?.Invoke(product.Id, "Server validation failed");
            }
        }

        private static async void GrantProduct(IAPProduct product)
        {
            AnalyticsService.LogPurchase(product.Id, product.Name, "GM", product.GemReward);

            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            if (product.GemReward > 0)
            {
                PlayFab.PlayFabClientAPI.AddUserVirtualCurrency(new PlayFab.ClientModels.AddUserVirtualCurrencyRequest
                {
                    VirtualCurrency = "GM",
                    Amount = product.GemReward
                }, result =>
                {
                    Debug.Log($"[IAPService] Granted {product.GemReward} Gems");
                    tcs.TrySetResult(true);
                }, error =>
                {
                    Debug.LogError($"[IAPService] Failed to grant gems: {error.GenerateErrorReport()}");
                    tcs.TrySetResult(true);
                });
                await tcs.Task;
            }
            if (product.Id == "remove_ads")
            {
                AdService.SetAdsRemoved(true);
                Debug.Log("[IAPService] Ads removed");
            }
            if (product.Id == "battle_pass")
            {
                PlayFab.PlayFabClientAPI.ExecuteCloudScript(new PlayFab.ClientModels.ExecuteCloudScriptRequest
                {
                    FunctionName = "UnlockPremiumBattlePass"
                }, result =>
                {
                    Debug.Log("[IAPService] Premium battle pass unlocked");
                }, error =>
                {
                    Debug.LogError($"[IAPService] Failed to unlock battle pass: {error.GenerateErrorReport()}");
                });
            }
            if (product.Id == "starter_pack")
            {
                PlayFab.PlayFabClientAPI.AddUserVirtualCurrency(new PlayFab.ClientModels.AddUserVirtualCurrencyRequest
                {
                    VirtualCurrency = "CN",
                    Amount = 200
                }, result =>
                {
                    Debug.Log("[IAPService] Granted starter pack coins");
                }, error =>
                {
                    Debug.LogError($"[IAPService] Failed to grant starter pack coins: {error.GenerateErrorReport()}");
                });
            }

            await PlayFabEconomy.RefreshCurrenciesAsync();
        }

        public static void RestorePurchases()
        {
            Debug.Log("[IAPService] Restore purchases - requires platform-specific restoration");
        }
    }

    public struct IAPProduct
    {
        public string Id;
        public string Name;
        public string Price;
        public int GemReward;
        public IAPProductType ProductType;
    }

    public enum IAPProductType
    {
        Consumable,
        NonConsumable,
        Subscription,
    }

    public static class ServerReceiptValidation
    {
        public static async System.Threading.Tasks.Task<bool> ValidatePurchaseAsync(string productId, string receipt)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            PlayFab.PlayFabClientAPI.ExecuteCloudScript(new PlayFab.ClientModels.ExecuteCloudScriptRequest
            {
                FunctionName = "ValidatePurchase",
                FunctionParameter = new { productId, receipt }
            }, result =>
            {
                bool validated = false;
                if (result.FunctionResult != null)
                {
                    bool.TryParse(result.FunctionResult.ToString(), out validated);
                }
                Debug.Log($"[ReceiptValidation] PlayFab CloudScript returned: {validated}");
                tcs.TrySetResult(validated);
            }, error =>
            {
                Debug.LogError($"[ReceiptValidation] CloudScript failed: {error.GenerateErrorReport()}");
                tcs.TrySetResult(false);
            });
            return await tcs.Task;
        }
    }
}
