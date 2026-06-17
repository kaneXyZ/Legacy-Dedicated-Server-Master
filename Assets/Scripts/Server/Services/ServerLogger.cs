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
    }

    public class ServerLogger
    {
        private readonly bool isHeadless;

        public ServerLogger()
        {
            // Detecta si está ejecutándose en Linux sin gráficos / modo batch
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
                // Formato limpio para la consola de Linux / Servidor Dedicado
                return $"[{timestamp}] [{categoryStr}] [{logType}] {message}";
            }
            else
            {
                // Formato con colores vistosos exclusivo para el Editor de Unity
                string colorHex = GetCategoryColorHex(category);
                string typeColor = GetTypeColorHex(logType);
                return $"<color=#888888>[{timestamp}]</color> <color={colorHex}><b>[{categoryStr}]</b></color> <color={typeColor}>[{logType}]</color> {message}";
            }
        }

        private string GetCategoryColorHex(LogCategory category)
        {
            return category switch
            {
                LogCategory.System => "#FFFFFF", // Blanco
                LogCategory.Network => "#00FFFF", // Cían
                LogCategory.Riptide => "#00BCFF", // Azul Eléctrico
                LogCategory.Auth => "#FF00FF", // Magenta
                LogCategory.MasterServer => "#FFA500", // Naranja
                LogCategory.Gameplay => "#00FF00", // Verde
                LogCategory.Chat => "#FFFF00", // Amarillo
                _ => "#FFFFFF",
            };
        }

        private string GetTypeColorHex(string logType)
        {
            return logType switch
            {
                "INFO" => "#76D7C4", // Verde menta
                "WARN" => "#F4D03F", // Amarillo advertencia
                "ERROR" => "#E74C3C", // Rojo crítico
                _ => "#FFFFFF",
            };
        }
    }
}
