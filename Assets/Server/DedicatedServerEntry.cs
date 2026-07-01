using System;
using SlitherRoyale.Client.Networking;
using UnityEngine;
using WormCore;

#if UNITY_SERVER
namespace SlitherRoyale.Server
{
    public class DedicatedServerEntry : MonoBehaviour
    {
        private ServerNetworkManager _serverManager;
        private bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (!Application.isBatchMode) return;
            var go = new GameObject("DedicatedServerEntry");
            go.AddComponent<DedicatedServerEntry>();
            DontDestroyOnLoad(go);
        }

        private void Start()
        {
            Application.targetFrameRate = 60;

            int port = ParseArg("-port", 12345);
            string modeStr = ParseArg("-mode", "ffa").ToLower();
            MatchMode mode = modeStr switch
            {
                "duos" => MatchMode.Duos,
                "1v1" => MatchMode.Ranked1v1,
                "br" => MatchMode.BattleRoyale,
                _ => MatchMode.FreeForAll
            };

            var modeConfig = ModeConfig.GetDefault(mode);
            Debug.Log($"[DedicatedServer] Starting on port {port}, mode {mode}");

            _serverManager = gameObject.AddComponent<ServerNetworkManager>();
            _serverManager.SetModeConfig(modeConfig);
            _serverManager.StartServer(port);

            _initialized = true;
        }

        private void Update()
        {
            if (_initialized && _serverManager != null)
                _serverManager.Poll();
        }

        private static string ParseArg(string key, string defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return defaultValue;
        }

        private static int ParseArg(string key, int defaultValue)
        {
            string val = ParseArg(key, null);
            return val != null && int.TryParse(val, out int parsed) ? parsed : defaultValue;
        }
    }
}
#endif
