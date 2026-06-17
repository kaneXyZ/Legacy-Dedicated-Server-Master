using System;

namespace Legacy.DedicatedServer.Bootstrap
{
    [Serializable]
    public sealed class ServerResolvedConfig
    {
        // --- Propiedades de Red (Riptide) ---
        public int Port;
        public int MaxClientCount;
        public int TickRate;
        public string BindAddress;
        public string AdvertisedAddress;

        // --- Configuración de Ejecución ---
        public string ConsoleTitle;
        public bool ClearConsoleOnBoot;
        public bool RunInBackground;
        public bool VerboseLogs;
        public bool ShowTimestamps;
        public bool AutoStartOnAwake;

        // --- Políticas de Autenticación de Sesiones ---
        public bool DisconnectPreviousSessionOnDuplicateLogin;
        public float UnauthenticatedKickDelay;

        // --- Master Server (Extraídos de MasterTicketAuthValidator) ---
        public string MasterServerUrl;
        public string RegisterEndpointUrl;
        public string MasterApiKey;
        public float HeartbeatIntervalSeconds = 15f;

        // --- Identidad Única de la Instancia de Servidor ---
        public string ServerId;
        public string ServerName = "Legacy Linux Instance";
        public string PublicIp = "127.0.0.1";
        public string Region = "SouthAmerica-West";
        public string MapName = "World_Main";
        public string GameVersion = "1.0.0";
    }
}
