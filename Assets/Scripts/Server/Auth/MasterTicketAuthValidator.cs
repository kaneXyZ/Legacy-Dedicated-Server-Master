using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Legacy.DedicatedServer.Auth
{
    public enum EnvironmentType
    {
        Development,
        Production,
    }

    // Objeto que transporta el resultado de la validación sin usar parámetros 'out' (que bloquean async)
    public class AuthValidationResult
    {
        public bool IsValid;
        public string AccountId;
        public string ErrorReason;
    }

    [CreateAssetMenu(
        fileName = "MasterTicketAuthValidator",
        menuName = "DedicatedServer/Auth/MasterTicketAuthValidator"
    )]
    public class MasterTicketAuthValidator : ScriptableObject
    {
        [Header("Environment")]
        public EnvironmentType currentEnvironment = EnvironmentType.Development;

        [Header("Master Server Configuration")]
        public string devMasterServerBaseUrl = "http://localhost:3000";
        public string prodMasterServerBaseUrl =
            "https://masterserverlegacy-896525629469.southamerica-west1.run.app/";

        [Header("API Paths")]
        public string registerPath = "api/register";
        public string consumeTicketPath = "join/consume";
        public string heartbeatPath = "api/heartbeat";

        [Header("Security Keys")]
        public string devApiKey = "test_key_123";

        [Tooltip("Se inyecta por consola en prod (-apikey), dejar vacío.")]
        public string prodApiKey = "";

        [Header("Dedicated Identity")]
        public string serverId = "legacy-sa-1";

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // --- Helpers de Configuración ---
        public string GetActiveBaseUrl() =>
            currentEnvironment == EnvironmentType.Production
                ? prodMasterServerBaseUrl
                : devMasterServerBaseUrl;

        public string GetActiveApiKey() =>
            currentEnvironment == EnvironmentType.Production ? prodApiKey : devApiKey;

        public string GetFullRegisterUrl() => CombineUrl(GetActiveBaseUrl(), registerPath);

        public string GetFullConsumeUrl() => CombineUrl(GetActiveBaseUrl(), consumeTicketPath);

        public string GetFullHeartbeatUrl() => CombineUrl(GetActiveBaseUrl(), heartbeatPath);

        #region Serialización JSON
        [Serializable]
        private class ConsumeTicketRequest
        {
            public string ticketId;
            public string serverId;
        }

        [Serializable]
        private class ConsumeTicketPlayer
        {
            public string uid;
            public string sessionId;
        }

        [Serializable]
        private class ConsumeTicketResponse
        {
            public bool ok;
            public string error;
            public string ticketId;
            public string serverId;
            public ConsumeTicketPlayer player;
        }
        #endregion

        // --- Lógica de Validación 100% Asíncrona (NO BLOQUEA EL SERVIDOR) ---
        public async Task<AuthValidationResult> ValidateTicketAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new AuthValidationResult
                {
                    IsValid = false,
                    ErrorReason = "Join ticket vacío.",
                };

            try
            {
                ConsumeTicketResponse response = await ConsumeTicketAsync(token.Trim());

                if (response == null || !response.ok)
                    return new AuthValidationResult
                    {
                        IsValid = false,
                        ErrorReason = response?.error ?? "El master server rechazó el ticket.",
                    };

                if (response.player == null || string.IsNullOrWhiteSpace(response.player.uid))
                    return new AuthValidationResult
                    {
                        IsValid = false,
                        ErrorReason = "El master server no devolvió un UID válido.",
                    };

                return new AuthValidationResult
                {
                    IsValid = true,
                    AccountId = response.player.uid.Trim(),
                };
            }
            catch (Exception ex)
            {
                return new AuthValidationResult
                {
                    IsValid = false,
                    ErrorReason = $"Error de red: {ex.Message}",
                };
            }
        }

        private async Task<ConsumeTicketResponse> ConsumeTicketAsync(string ticketId)
        {
            string url = GetFullConsumeUrl();
            // --- DIAGNÓSTICO ---
            Debug.Log($"[AUTH] Intentando validar ticket: {ticketId}");
            Debug.Log($"[AUTH] Servidor ID enviado al Master: '{serverId}'");
            Debug.Log($"[AUTH] URL del Master: {url}");
            // -------------------
            string json = JsonUtility.ToJson(
                new ConsumeTicketRequest { ticketId = ticketId, serverId = serverId }
            );

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-api-key", GetActiveApiKey());
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // await asegura que el framerate de Unity no se congele esperando a Node.js
            using HttpResponseMessage response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(body))
                return new ConsumeTicketResponse { ok = false, error = "Respuesta vacía." };

            ConsumeTicketResponse parsed = JsonUtility.FromJson<ConsumeTicketResponse>(body);
            if (parsed != null && !response.IsSuccessStatusCode)
            {
                parsed.ok = false;
                if (string.IsNullOrWhiteSpace(parsed.error))
                    parsed.error = $"HTTP {(int)response.StatusCode}";
            }

            return parsed;
        }

        private static string CombineUrl(string baseUrl, string path) =>
            $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
