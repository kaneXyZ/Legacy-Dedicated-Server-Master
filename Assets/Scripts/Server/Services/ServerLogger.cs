using UnityEngine;

namespace Legacy.DedicatedServer.Services
{
    public enum LogCategory
    {
        System,
        Network,
        Riptide,
        Auth,
        MasterServer,
        Gameplay,
        Chat,
        Player,
    }

    public class ServerLogger
    {
        private readonly bool isHeadless;

        // Códigos de color ANSI para la terminal de Linux
        private const string ANSI_RESET = "\u001b[0m";
        private const string ANSI_TIMESTAMP = "\u001b[90m"; // Gris oscuro

        private const string ANSI_WHITE_BOLD = "\u001b[1;37m";
        private const string ANSI_CYAN = "\u001b[36m";
        private const string ANSI_BLUE_BOLD = "\u001b[1;34m";
        private const string ANSI_MAGENTA = "\u001b[35m";
        private const string ANSI_YELLOW = "\u001b[33m";
        private const string ANSI_GREEN = "\u001b[32m";
        private const string ANSI_RED = "\u001b[31m";

        public ServerLogger()
        {
            isHeadless = Application.isBatchMode;
        }

        public void LogInfo(LogCategory category, string message)
        {
            Debug.Log(FormatMessage(category, "INFO", message));
        }

        public void LogWarning(LogCategory category, string message)
        {
            Debug.LogWarning(FormatMessage(category, "WARN", message));
        }

        public void LogError(LogCategory category, string message)
        {
            Debug.LogError(FormatMessage(category, "ERROR", message));
        }

        private string FormatMessage(LogCategory category, string logType, string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string categoryStr = category.ToString().ToUpper();

            if (isHeadless)
            {
                // --- COLORES EN LINUX (ANSI ESCAPE CODES) ---
                string catColor = GetCategoryAnsi(category);
                string typeColor = GetTypeAnsi(logType);

                return $"{ANSI_TIMESTAMP}[{timestamp}]{ANSI_RESET} {catColor}[{categoryStr}]{ANSI_RESET} {typeColor}[{logType}]{ANSI_RESET} {message}";
            }
            else
            {
                // --- COLORES EN UNITY EDITOR (HTML) ---
                string colorHex = GetCategoryColorHex(category);
                string typeColor = GetTypeColorHex(logType);
                return $"<color=#888888>[{timestamp}]</color> <color={colorHex}><b>[{categoryStr}]</b></color> <color={typeColor}>[{logType}]</color> {message}";
            }
        }

        private string GetCategoryAnsi(LogCategory category)
        {
            return category switch
            {
                LogCategory.System => ANSI_WHITE_BOLD,
                LogCategory.Network => ANSI_CYAN,
                LogCategory.Riptide => ANSI_BLUE_BOLD,
                LogCategory.Auth => ANSI_MAGENTA,
                LogCategory.MasterServer => ANSI_YELLOW,
                LogCategory.Gameplay => ANSI_GREEN,
                LogCategory.Chat => ANSI_YELLOW,
                LogCategory.Player => ANSI_GREEN,
                _ => ANSI_RESET,
            };
        }

        private string GetTypeAnsi(string logType)
        {
            return logType switch
            {
                "INFO" => ANSI_GREEN,
                "WARN" => ANSI_YELLOW,
                "ERROR" => ANSI_RED,
                _ => ANSI_RESET,
            };
        }

        private string GetCategoryColorHex(LogCategory category)
        {
            return category switch
            {
                LogCategory.System => "#FFFFFF",
                LogCategory.Network => "#00FFFF",
                LogCategory.Riptide => "#00BCFF",
                LogCategory.Auth => "#FF00FF",
                LogCategory.MasterServer => "#FFA500",
                LogCategory.Gameplay => "#00FF00",
                LogCategory.Chat => "#FFFF00",
                _ => "#FFFFFF",
            };
        }

        private string GetTypeColorHex(string logType)
        {
            return logType switch
            {
                "INFO" => "#76D7C4",
                "WARN" => "#F4D03F",
                "ERROR" => "#E74C3C",
                _ => "#FFFFFF",
            };
        }
    }
}
