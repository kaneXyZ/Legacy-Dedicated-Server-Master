using System;

namespace Legacy.DedicatedServer.Bootstrap
{
    public static class ServerCommandLineOverrides
    {
        public static void Apply(ServerResolvedConfig config)
        {
            if (config == null)
                return;

            // Lee los argumentos puros pasados al ejecutable por la terminal de Linux/Windows
            string[] args = Environment.GetCommandLineArgs();

            // --- Overrides de Red ---
            config.Port = GetInt(args, "-port", config.Port);
            config.MaxClientCount = GetInt(args, "-maxPlayers", config.MaxClientCount);
            config.TickRate = GetInt(args, "-tickRate", config.TickRate);
            config.BindAddress = GetString(args, "-bind", config.BindAddress);
            config.AdvertisedAddress = GetString(args, "-advertise", config.AdvertisedAddress);

            // --- Overrides de Identidad del Servidor ---
            config.ServerId = GetString(args, "-serverId", config.ServerId);
            config.ServerName = GetString(args, "-serverName", config.ServerName);
            config.PublicIp = GetString(args, "-ip", config.PublicIp);
            config.Region = GetString(args, "-region", config.Region);
            config.MapName = GetString(args, "-map", config.MapName);
            config.GameVersion = GetString(args, "-version", config.GameVersion);

            // --- Overrides de Comunicación con el Master Server ---
            config.MasterServerUrl = GetString(args, "-masterUrl", config.MasterServerUrl);
            config.MasterApiKey = GetString(args, "-apiKey", config.MasterApiKey);
            config.HeartbeatIntervalSeconds = GetFloat(
                args,
                "-heartbeat",
                config.HeartbeatIntervalSeconds
            );

            // --- Overrides de Rendimiento y Comportamiento Local ---
            config.ConsoleTitle = GetString(args, "-title", config.ConsoleTitle);
            config.VerboseLogs = GetBool(args, "-verboseLogs", config.VerboseLogs);
            config.ShowTimestamps = GetBool(args, "-showTimestamps", config.ShowTimestamps);
            config.ClearConsoleOnBoot = GetBool(args, "-clearConsole", config.ClearConsoleOnBoot);
            config.RunInBackground = GetBool(args, "-runInBackground", config.RunInBackground);
            config.AutoStartOnAwake = GetBool(args, "-autoStart", config.AutoStartOnAwake);

            // --- Overrides de Políticas de Autenticación ---
            config.DisconnectPreviousSessionOnDuplicateLogin = GetBool(
                args,
                "-kickPreviousSession",
                config.DisconnectPreviousSessionOnDuplicateLogin
            );
            config.UnauthenticatedKickDelay = GetFloat(
                args,
                "-authTimeout",
                config.UnauthenticatedKickDelay
            );
        }

        #region Parsers Seguros (Evitan Crashes)

        private static int GetInt(string[] args, string key, int fallback)
        {
            int index = Array.IndexOf(args, key);
            if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int val))
            {
                return val;
            }
            return fallback;
        }

        private static float GetFloat(string[] args, string key, float fallback)
        {
            int index = Array.IndexOf(args, key);
            if (
                index >= 0
                && index + 1 < args.Length
                && float.TryParse(args[index + 1], out float val)
            )
            {
                return val;
            }
            return fallback;
        }

        private static string GetString(string[] args, string key, string fallback)
        {
            int index = Array.IndexOf(args, key);
            if (index >= 0 && index + 1 < args.Length)
            {
                return args[index + 1];
            }
            return fallback;
        }

        private static bool GetBool(string[] args, string key, bool fallback)
        {
            int index = Array.IndexOf(args, key);
            if (
                index >= 0
                && index + 1 < args.Length
                && bool.TryParse(args[index + 1], out bool val)
            )
            {
                return val;
            }
            return fallback;
        }

        #endregion
    }
}
