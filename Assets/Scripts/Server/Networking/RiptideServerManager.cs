// Archivo: Assets/Legacy/DedicatedServer/Networking/RiptideServerManager.cs
using Legacy.DedicatedServer.Bootstrap;
using Legacy.DedicatedServer.Services; // Integración con el Hub de Servicios y Logs
using Legacy.Shared.Core; // Asegúrate de incluir el namespace donde está tu Enum ServerToClientId
using Riptide;
using Riptide.Utils;
using UnityEngine;

namespace Legacy.DedicatedServer.Networking
{
    public class RiptideServerManager : MonoBehaviour
    {
        public Server Server { get; private set; }
        public bool IsRunning => Server != null && Server.IsRunning;

        // --- SISTEMA DE TICKS (Reloj Autoritativo del Servidor) ---
        public ushort CurrentTick { get; private set; } = 0;

        [Tooltip("Cada cuántos ticks sincronizamos el reloj con los clientes (Mantenimiento)")]
        [SerializeField]
        private int syncTickInterval = 200;

        private void Awake()
        {
            // ¡ARQUITECTURA LIMPIA! Nos registramos en el Hub Global (Service Locator) sin usar Singletons
            ServerServices.RegisterNetwork(this);
            DontDestroyOnLoad(gameObject);
        }

        public void InitializeAndStart(ServerResolvedConfig config)
        {
            // Sincronizamos las físicas de Unity con nuestro TickRate
            Application.targetFrameRate = config.TickRate;
            Time.fixedDeltaTime = 1f / config.TickRate;

            // Redirigimos el Logger interno de Riptide a nuestro formato personalizado
            RiptideLogger.Initialize(
                (msg) => ServerServices.Logger.LogInfo(LogCategory.Riptide, msg),
                (msg) => ServerServices.Logger.LogInfo(LogCategory.Riptide, msg),
                (msg) => ServerServices.Logger.LogWarning(LogCategory.Riptide, msg),
                (msg) => ServerServices.Logger.LogError(LogCategory.Riptide, msg),
                false
            );

            Server = new Server();

            // Nos suscribimos a los eventos nativos de Riptide
            Server.ClientConnected += OnPlayerJoined;
            Server.ClientDisconnected += OnPlayerLeft;

            Server.Start((ushort)config.Port, (ushort)config.MaxClientCount);

            ServerServices.Logger.LogInfo(
                LogCategory.Network,
                $"Servidor Riptide encendido en puerto: {config.Port} a {config.TickRate} Ticks/s. Max Clientes: {config.MaxClientCount}. ID: {config.ServerId}"
            );
        }

        private void FixedUpdate()
        {
            if (Server != null && Server.IsRunning)
            {
                Server.Update();

                // 1. Incrementamos el reloj oficial del servidor en cada frame físico
                CurrentTick++;

                // 2. Broadcast de mantenimiento de Ticks a todos los clientes
                if (CurrentTick % syncTickInterval == 0)
                {
                    SendSync();
                }
            }
        }

        private void OnPlayerJoined(object sender, ServerConnectedEventArgs e)
        {
            ServerServices.Logger.LogInfo(
                LogCategory.Network,
                $"Cliente [{e.Client.Id}] conectó a nivel de red. Esperando autenticación..."
            );
        }

        // ¡CORREGIDO Y FUSIONADO! Un solo método para gestionar la desconexión
        private void OnPlayerLeft(object sender, ServerDisconnectedEventArgs e)
        {
            ServerServices.Logger.LogWarning(
                LogCategory.Network,
                $"Cliente [{e.Client.Id}] se ha desconectado. Razón: {e.Reason}. Limpiando datos..."
            );

            // Avisamos al Manager de jugadores que elimine a este jugador
            if (Legacy.DedicatedServer.Players.ServerPlayerManager.Instance != null)
            {
                // Asegúrate de que el método en tu Manager se llame RemovePlayer o DisconnectPlayer
                Legacy.DedicatedServer.Players.ServerPlayerManager.Instance.RemovePlayer(
                    e.Client.Id
                );
            }
            else
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.Player,
                    $"No se pudo limpiar al jugador [{e.Client.Id}] porque ServerPlayerManager.Instance es nulo."
                );
            }
        }

        #region SISTEMA DE SINCRONIZACIÓN DE TICKS

        // Mantenimiento general: Envía la hora actual a todos los conectados
        private void SendSync()
        {
            Message message = Message.Create(
                MessageSendMode.Unreliable,
                (ushort)ServerToClientId.Sync
            );
            message.AddUShort(CurrentTick);
            Server.SendToAll(message);
        }

        // Se llama desde tu Handler de Autenticación JUSTO DESPUÉS de validar el JoinTicket
        public void SendInitialSync(ushort toClientId)
        {
            Message message = Message.Create(
                MessageSendMode.Unreliable,
                (ushort)ServerToClientId.Sync
            );
            message.AddUShort(CurrentTick);
            Server.Send(message, toClientId);

            ServerServices.Logger.LogInfo(
                LogCategory.Auth,
                $"Reloj inicial (Tick: {CurrentTick}) sincronizado con el Cliente [{toClientId}]."
            );
        }

        #endregion

        #region LIMPIEZA DE MEMORIA (PREVENCIÓN DE LEAKS)
        private void OnDestroy()
        {
            if (Server != null)
            {
                Server.ClientConnected -= OnPlayerJoined;
                Server.ClientDisconnected -= OnPlayerLeft;
            }
        }

        private void OnApplicationQuit()
        {
            if (Server != null)
            {
                Server.ClientConnected -= OnPlayerJoined;
                Server.ClientDisconnected -= OnPlayerLeft;

                if (Server.IsRunning)
                {
                    Server.Stop();
                    ServerServices.Logger.LogInfo(
                        LogCategory.Network,
                        "Servidor Riptide apagado limpiamente."
                    );
                }
            }
        }
        #endregion
    }
}
