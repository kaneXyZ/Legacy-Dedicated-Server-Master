using Legacy.DedicatedServer.Auth;

namespace Legacy.DedicatedServer.Bootstrap
{
    public static class ServerConfigResolver
    {
        public static ServerResolvedConfig Resolve(
            ServerRuntimeConfig runtimeDefaults,
            MasterTicketAuthValidator authConfig
        )
        {
            var resolved = new ServerResolvedConfig();

            // 1. Mapear valores por defecto de la configuración local (Red y Rendimiento)
            if (runtimeDefaults != null)
            {
                resolved.Port = runtimeDefaults.port;
                resolved.MaxClientCount = runtimeDefaults.maxClientCount;
                resolved.TickRate = runtimeDefaults.tickRate;
                resolved.BindAddress = runtimeDefaults.bindAddress;
                resolved.AdvertisedAddress = runtimeDefaults.advertisedAddress;

                resolved.ConsoleTitle = runtimeDefaults.consoleTitle;
                resolved.ClearConsoleOnBoot = runtimeDefaults.clearConsoleOnBoot;
                resolved.RunInBackground = runtimeDefaults.runInBackground;
                resolved.VerboseLogs = runtimeDefaults.verboseLogs;
                resolved.ShowTimestamps = runtimeDefaults.showTimestamps;
                resolved.AutoStartOnAwake = runtimeDefaults.autoStartOnAwake;

                resolved.DisconnectPreviousSessionOnDuplicateLogin =
                    runtimeDefaults.disconnectPreviousSessionOnDuplicateLogin;
                resolved.UnauthenticatedKickDelay = runtimeDefaults.unauthenticatedKickDelay;
            }

            // 2. Extraer de forma semántica los endpoints del Validador Maestro (Evita duplicar URLs)
            if (authConfig != null)
            {
                resolved.ServerId = authConfig.serverId;
                resolved.MasterServerUrl = authConfig.GetActiveBaseUrl();
                resolved.RegisterEndpointUrl = authConfig.GetFullRegisterUrl();
                resolved.MasterApiKey = authConfig.GetActiveApiKey();
            }

            // 3. Aplicar los Overrides desde la consola de Linux / Terminal
            // Si ejecutas el servidor con "-port 7778", esto pisará de forma segura el puerto por defecto (7777)
            ServerCommandLineOverrides.Apply(resolved);

            return resolved;
        }
    }
}
