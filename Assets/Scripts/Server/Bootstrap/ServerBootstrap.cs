using Legacy.DedicatedServer.Auth;
using Legacy.DedicatedServer.Master;
using Legacy.DedicatedServer.Networking;
using Legacy.DedicatedServer.Services; // Integración con el Hub de Servicios y Logs
using UnityEngine;

namespace Legacy.DedicatedServer.Bootstrap
{
    [DefaultExecutionOrder(-50)] // Garantiza ejecutarse antes que cualquier otro script en la escena
    public class ServerBootstrap : MonoBehaviour
    {
        [Header("Configuration Asset Sources")]
        [SerializeField]
        private ServerRuntimeConfig runtimeDefaults;

        [SerializeField]
        private MasterTicketAuthValidator authConfig;

        [Header("Core System Managers Links")]
        [SerializeField]
        private RiptideServerManager riptideServerManager;

        [SerializeField]
        private MasterServerHeartbeatManager heartbeatManager;

        private ServerResolvedConfig resolvedConfig;

        private void Awake()
        {
            // 1. Vincular componentes automáticamente si se dejaron vacíos en el Inspector
            if (riptideServerManager == null)
                riptideServerManager = FindAnyObjectByType<RiptideServerManager>();
            if (heartbeatManager == null)
                heartbeatManager = FindAnyObjectByType<MasterServerHeartbeatManager>();

            // El Logger ya está disponible de forma estática gracias al constructor de ServerServices
            ServerServices.Logger.LogInfo(
                LogCategory.System,
                "Iniciando proceso de resolución de configuraciones..."
            );

            // 2. Resolver la configuración final mezclando ScriptableObjects y comandos de consola (-port, etc.)
            resolvedConfig = ServerConfigResolver.Resolve(runtimeDefaults, authConfig);

            if (resolvedConfig == null)
            {
                ServerServices.Logger.LogError(
                    LogCategory.System,
                    "Fallo crítico: No se pudo resolver la configuración del servidor."
                );
                enabled = false;
                return;
            }

            // 3. Aplicar configuración de ejecución en segundo plano
            if (resolvedConfig.RunInBackground)
            {
                Application.runInBackground = true;
                ServerServices.Logger.LogInfo(
                    LogCategory.System,
                    "Servidor configurado para ejecutarse en segundo plano (Run In Background = True)."
                );
            }
        }

        private void Start()
        {
            // 4. Si la configuración dictamina arranque automático, encendemos los motores
            if (resolvedConfig.AutoStartOnAwake)
            {
                BootDedicatedServerSystems();
            }
            else
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.System,
                    "Arranque automático desactivado (AutoStartOnAwake = False). Esperando orden externa para iniciar."
                );
            }
        }

        /// <summary>
        /// Orquesta el encendido secuencial y correcto de todas las capas del servidor dedicado.
        /// </summary>
        public void BootDedicatedServerSystems()
        {
            ServerServices.Logger.LogInfo(
                LogCategory.System,
                $"========================================================================="
            );
            ServerServices.Logger.LogInfo(
                LogCategory.System,
                $"INICIANDO SECUENCIA DE ARRANQUE: {resolvedConfig.ConsoleTitle}"
            );
            ServerServices.Logger.LogInfo(
                LogCategory.System,
                $"========================================================================="
            );

            // PASO A: Encender el motor de red UDP (Riptide)
            if (riptideServerManager != null)
            {
                ServerServices.Logger.LogInfo(
                    LogCategory.System,
                    "Inicializando capa de red de bajo nivel (Riptide)..."
                );
                riptideServerManager.InitializeAndStart(resolvedConfig);
            }
            else
            {
                ServerServices.Logger.LogError(
                    LogCategory.System,
                    "Error fatal al arrancar: RiptideServerManager no fue encontrado en la escena."
                );
                return;
            }

            // PASO B: Lanzar el servicio HTTP del Master Server (Heartbeats)
            if (heartbeatManager != null)
            {
                ServerServices.Logger.LogInfo(
                    LogCategory.System,
                    "Inicializando bucle asíncrono de comunicación con el Servidor Maestro..."
                );
                heartbeatManager.StartHeartbeatLoop(resolvedConfig);
            }
            else
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.System,
                    "MasterServerHeartbeatManager no asignado. El servidor operará de forma aislada sin reportar estado al backend."
                );
            }

            ServerServices.Logger.LogInfo(
                LogCategory.System,
                $"[BOOTSTRAP] Servidor listo y escuchando conexiones en la región: '{resolvedConfig.Region}' | Mapa: '{resolvedConfig.MapName}'"
            );
        }
    }
}
