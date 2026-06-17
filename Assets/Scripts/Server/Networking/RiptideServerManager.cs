using Legacy.DedicatedServer.Bootstrap;
using Legacy.DedicatedServer.Services; // Integración con el Hub de Servicios y Logs
using Riptide;
using Riptide.Utils;
using UnityEngine;

namespace Legacy.DedicatedServer.Networking
{
    public class RiptideServerManager : MonoBehaviour
    {
        public Server Server { get; private set; }
        public bool IsRunning => Server != null && Server.IsRunning;

        private void Awake()
        {
            // Registrarse en el Hub Global
            ServerServices.RegisterNetwork(this);
            DontDestroyOnLoad(gameObject);
        }

        public void InitializeAndStart(ServerResolvedConfig config)
        {
            Application.targetFrameRate = config.TickRate;
            Time.fixedDeltaTime = 1f / config.TickRate;

            // ¡MAGIA! Redirigimos el Logger interno de Riptide a nuestro formato personalizado de colores
            RiptideLogger.Initialize(
                (msg) => ServerServices.Logger.LogInfo(LogCategory.Riptide, msg),
                (msg) => ServerServices.Logger.LogInfo(LogCategory.Riptide, msg),
                (msg) => ServerServices.Logger.LogWarning(LogCategory.Riptide, msg),
                (msg) => ServerServices.Logger.LogError(LogCategory.Riptide, msg),
                false
            );

            Server = new Server();
            Server.Start((ushort)config.Port, (ushort)config.MaxClientCount);

            ServerServices.Logger.LogInfo(
                LogCategory.Network,
                $"Servidor Riptide encendido en puerto: {config.Port}. Max Clientes: {config.MaxClientCount}"
            );
        }

        private void FixedUpdate()
        {
            if (Server != null && Server.IsRunning)
                Server.Update();
        }

        private void OnApplicationQuit()
        {
            if (Server != null && Server.IsRunning)
            {
                Server.Stop();
                ServerServices.Logger.LogInfo(
                    LogCategory.Network,
                    "Servidor Riptide apagado limpiamente."
                );
            }
        }
    }
}
