using Legacy.DedicatedServer.Auth;
using Legacy.DedicatedServer.Master;
using Legacy.DedicatedServer.Networking;

namespace Legacy.DedicatedServer.Services
{
    public static class ServerServices
    {
        public static ServerLogger Logger { get; private set; }
        public static AuthManager Auth { get; private set; }

        public static RiptideServerManager Network { get; private set; }
        public static MasterServerHeartbeatManager Heartbeat { get; private set; }

        static ServerServices()
        {
            // El Logger se inicializa inmediatamente de forma segura
            Logger = new ServerLogger();
        }

        public static void RegisterAuth(AuthManager authManager) => Auth = authManager;

        public static void RegisterNetwork(RiptideServerManager networkManager) =>
            Network = networkManager;

        public static void RegisterHeartbeat(MasterServerHeartbeatManager heartbeatManager) =>
            Heartbeat = heartbeatManager;
    }
}
