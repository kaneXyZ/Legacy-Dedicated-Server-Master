using System;
using System.Collections;
using System.Text;
using Legacy.DedicatedServer.Bootstrap;
using Legacy.DedicatedServer.Master.Dtos;
using Legacy.DedicatedServer.Services; // Importamos servicios
using UnityEngine;
using UnityEngine.Networking;

namespace Legacy.DedicatedServer.Master
{
    public class MasterServerHeartbeatManager : MonoBehaviour
    {
        private ServerResolvedConfig serverConfig;
        private bool isReporting = false;

        private void Awake()
        {
            // Registrarse en el Hub Global
            ServerServices.RegisterHeartbeat(this);
        }

        public void StartHeartbeatLoop(ServerResolvedConfig config)
        {
            serverConfig = config;
            if (string.IsNullOrWhiteSpace(serverConfig.MasterServerUrl))
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.MasterServer,
                    "URL del servidor maestro inválida. Abortando Heartbeats."
                );
                return;
            }

            isReporting = true;
            StartCoroutine(HeartbeatRoutine());
        }

        private IEnumerator HeartbeatRoutine()
        {
            yield return null;
            while (isReporting)
            {
                yield return StartCoroutine(SendHeartbeat());
                yield return new WaitForSecondsRealtime(serverConfig.HeartbeatIntervalSeconds);
            }
        }

        private IEnumerator SendHeartbeat()
        {
            int activePlayers =
                ServerServices.Network != null && ServerServices.Network.IsRunning
                    ? ServerServices.Network.Server.ClientCount
                    : 0;

            var payload = new MasterServerRegisterRequest
            {
                id = serverConfig.ServerId,
                nombre = serverConfig.ServerName,
                ip = serverConfig.PublicIp,
                puerto = serverConfig.Port,
                region = serverConfig.Region,
                jugadores_actuales = Mathf.Max(0, activePlayers),
                jugadores_max = Mathf.Max(1, activePlayers), // Evitar 0 para max
                mapa = serverConfig.MapName,
                version = serverConfig.GameVersion,
                platform = Application.platform.ToString(),
            };

            string json = JsonUtility.ToJson(payload);
            //string url = $"{config.MasterServerUrl.TrimEnd('/')}/servers/register";

            using var request = new UnityWebRequest(
                serverConfig.RegisterEndpointUrl,
                UnityWebRequest.kHttpVerbPOST
            );
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 15;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", serverConfig.MasterApiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[MASTER] Heartbeat ERROR: code={request.responseCode} error={request.error} body={request.downloadHandler.text}"
                );
                yield break;
            }

            //Debug.Log($"[MASTER] Heartbeat OK -> {request.downloadHandler.text}");
        }

        private IEnumerator SendHeartbeatRequest()
        {
            int activePlayers =
                ServerServices.Network != null && ServerServices.Network.IsRunning
                    ? ServerServices.Network.Server.ClientCount
                    : 0;

            // string json = $"{{\"serverId\":\"{serverConfig.ServerId}\",\"currentPlayers\":{activePlayers},\"status\":\"online\"}}";

            var payload = new MasterServerRegisterRequest
            {
                id = serverConfig.ServerId,
                nombre = serverConfig.ServerName,
                ip = serverConfig.PublicIp,
                puerto = serverConfig.Port,
                region = serverConfig.Region,
                jugadores_actuales = Mathf.Max(0, activePlayers),
                jugadores_max = Mathf.Max(1, activePlayers), // Evitar 0 para max
                mapa = serverConfig.MapName,
                version = serverConfig.GameVersion,
                platform = Application.platform.ToString(),
            };

            string json = JsonUtility.ToJson(payload);

            byte[] rawBody = Encoding.UTF8.GetBytes(json);
            using (
                UnityWebRequest webRequest = new UnityWebRequest(
                    serverConfig.RegisterEndpointUrl,
                    "POST"
                )
            )
            {
                webRequest.uploadHandler = new UploadHandlerRaw(rawBody);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-api-key", serverConfig.MasterApiKey);

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    if (serverConfig.VerboseLogs)
                        ServerServices.Logger.LogInfo(
                            LogCategory.MasterServer,
                            $"Sincronización de Heartbeat exitosa. Jugadores: {activePlayers}"
                        );
                }
                else
                {
                    ServerServices.Logger.LogError(
                        LogCategory.MasterServer,
                        $"Falla al enviar Heartbeat: {webRequest.error}"
                    );
                }
            }
        }
    }
}
