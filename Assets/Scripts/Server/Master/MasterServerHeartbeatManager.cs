using System.Collections;
using System.Text;
using Legacy.DedicatedServer.Bootstrap;
using Legacy.DedicatedServer.Master.Dtos;
using Legacy.DedicatedServer.Services;
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

            ServerServices.Logger.LogWarning(
                LogCategory.MasterServer,
                $"Iniciando Heartbeat Manager. Endpoint: {serverConfig.HeartbeatEndpointUrl}"
            );

            // Verificamos la ruta correcta del Heartbeat
            if (string.IsNullOrWhiteSpace(serverConfig.HeartbeatEndpointUrl))
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.MasterServer,
                    "URL de Heartbeat inválida. Abortando Heartbeats."
                );
                return;
            }

            isReporting = true;
            StartCoroutine(HeartbeatRoutine());
        }

        private IEnumerator HeartbeatRoutine()
        {
            // Pequeña pausa antes de iniciar para asegurar que todo el servidor haya cargado
            yield return new WaitForSecondsRealtime(2f);

            while (isReporting)
            {
                yield return StartCoroutine(SendHeartbeatRequest());
                yield return new WaitForSecondsRealtime(serverConfig.HeartbeatIntervalSeconds);
            }
        }

        private IEnumerator SendHeartbeatRequest()
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
                jugadores_max = serverConfig.MaxClientCount, // Corregido: Ahora envía el límite real configurado
                mapa = serverConfig.MapName,
                version = serverConfig.GameVersion,
                platform = Application.platform.ToString(),
            };

            string json = JsonUtility.ToJson(payload);
            byte[] rawBody = Encoding.UTF8.GetBytes(json);

            // Corregido: Apuntando al endpoint de Heartbeat, no al de registro
            using (
                UnityWebRequest webRequest = new UnityWebRequest(
                    serverConfig.HeartbeatEndpointUrl,
                    "POST"
                )
            )
            {
                webRequest.uploadHandler = new UploadHandlerRaw(rawBody);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = 15; // Se mantiene tu buena práctica de timeout

                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("x-api-key", serverConfig.MasterApiKey);

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    /**
                                        if (serverConfig.VerboseLogs)
                                        {
                                            ServerServices.Logger.LogInfo(
                                                LogCategory.MasterServer,
                                                $"Sincronización de Heartbeat exitosa. Jugadores: {activePlayers}/{serverConfig.MaxClientCount}"
                                            );
                                            
                                            
                                        }*/
                }
                else
                {
                    // Loguea tanto el error de red como la respuesta que Node.js haya devuelto (ej. "Invalid Token")
                    ServerServices.Logger.LogError(
                        LogCategory.MasterServer,
                        $"Falla al enviar Heartbeat: {webRequest.error} | Detalle: {webRequest.downloadHandler.text}"
                    );
                }
            }
        }
    }
}
