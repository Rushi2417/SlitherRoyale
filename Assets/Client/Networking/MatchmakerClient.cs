using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SlitherRoyale.Client.Networking
{
    public struct MatchmakingTicket
    {
        public int PlayerId;
        public string Mode;
        public int MapIndex;
        public string Region;
    }

    public struct ServerAllocation
    {
        public string Host;
        public int Port;
        public int SessionToken;
        public bool Success;
        public string ErrorMessage;
    }

    public static class MatchmakerClient
    {
        private static string _edgegapApiUrl = "https://api.edgegap.com/v1";
        private static string _apiToken = "";
        private static string _appName = "slither-royale";
        private static string _versionName = "1";

        private static string _overrideHost;

        private static bool _useSimulated = true;

        public static void Configure(string apiUrl, string apiToken, string appName, string versionName)
        {
            _edgegapApiUrl = apiUrl;
            _apiToken = apiToken;
            _appName = appName;
            _versionName = versionName;
            _useSimulated = string.IsNullOrEmpty(apiToken);
        }

        public static void SetOverrideHost(string host)
        {
            _overrideHost = host;
            _useSimulated = false;
        }

        public static async Task<ServerAllocation> RequestServerAsync(MatchmakingTicket ticket)
        {
            if (!string.IsNullOrEmpty(_overrideHost))
            {
                return new ServerAllocation
                {
                    Host = _overrideHost,
                    Port = 12345,
                    SessionToken = UnityEngine.Random.Range(100000, 999999),
                    Success = true,
                };
            }

            if (_useSimulated)
                return SimulateAllocation(ticket);

            var allocation = await CallEdgegapApi(ticket);
            return allocation;
        }

        private static ServerAllocation SimulateAllocation(MatchmakingTicket ticket)
        {
            Debug.Log($"[Matchmaker] Simulating server allocation for mode={ticket.Mode}");
            return new ServerAllocation
            {
                Host = "127.0.0.1",
                Port = 12345,
                SessionToken = UnityEngine.Random.Range(100000, 999999),
                Success = true,
            };
        }

        private static async Task<ServerAllocation> CallEdgegapApi(MatchmakingTicket ticket)
        {
            const int maxRetries = 3;
            const double retryDelaySeconds = 1.0;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    string jsonPayload = JsonUtility.ToJson(new EdgegapDeployRequest
                    {
                        app_name = _appName,
                        version_name = _versionName,
                    });

                    using var request = new UnityWebRequest($"{_edgegapApiUrl}/deploy", "POST")
                    {
                        uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload)),
                        downloadHandler = new DownloadHandlerBuffer()
                    };
                    request.SetRequestHeader("Authorization", _apiToken);
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 10;

                    var tcs = new TaskCompletionSource<ServerAllocation>();
                    request.SendWebRequest().completed += _ =>
                    {
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            var response = JsonUtility.FromJson<EdgegapDeployResponse>(request.downloadHandler.text);
                            if (response != null && response.success)
                            {
                                tcs.TrySetResult(new ServerAllocation
                                {
                                    Host = response.public_ip,
                                    Port = response.ports?.game_port ?? 12345,
                                    SessionToken = UnityEngine.Random.Range(100000, 999999),
                                    Success = true,
                                });
                            }
                            else
                            {
                                tcs.TrySetResult(new ServerAllocation
                                {
                                    Success = false,
                                    ErrorMessage = response?.message ?? "Unknown Edgegap error"
                                });
                            }
                        }
                        else
                        {
                            tcs.TrySetResult(new ServerAllocation
                            {
                                Success = false,
                                ErrorMessage = request.error
                            });
                        }
                    };

                    var result = await tcs.Task;
                    if (result.Success || attempt >= maxRetries - 1)
                        return result;

                    Debug.LogWarning($"[Matchmaker] Edgegap deploy attempt {attempt + 1} failed, retrying in {retryDelaySeconds}s...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
                catch (Exception e)
                {
                    if (attempt >= maxRetries - 1)
                    {
                        Debug.LogError($"[Matchmaker] Edgegap API error after {maxRetries} retries: {e.Message}");
                        return new ServerAllocation
                        {
                            Success = false,
                            ErrorMessage = e.Message
                        };
                    }
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
            }

            return new ServerAllocation { Success = false, ErrorMessage = "Max retries exceeded" };
        }

        public static async Task<bool> DeleteServerAsync(string requestId)
        {
            if (_useSimulated) return true;

            using var request = UnityWebRequest.Delete($"{_edgegapApiUrl}/delete/{requestId}");
            request.SetRequestHeader("Authorization", _apiToken);

            var tcs = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ =>
            {
                tcs.TrySetResult(request.result == UnityWebRequest.Result.Success);
            };

            return await tcs.Task;
        }

        [Serializable]
        private class EdgegapDeployRequest
        {
            public string app_name;
            public string version_name;
        }

        [Serializable]
        private class EdgegapDeployResponse
        {
            public bool success;
            public string message;
            public string public_ip;
            public string request_id;
            public EdgegapPorts ports;
        }

        [Serializable]
        private class EdgegapPorts
        {
            public int game_port;
        }
    }
}
