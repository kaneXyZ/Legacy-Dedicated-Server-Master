using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;

namespace Legacy.DedicatedServer.Auth
{
    public enum EnvironmentType
    {
        Development,
        Production,
    }

    [CreateAssetMenu(
        fileName = "MasterTicketAuthValidator",
        menuName = "DedicatedServer/Auth/MasterTicketAuthValidator"
    )]
    public class MasterTicketAuthValidator : ServerAuthValidator
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

        private string GetFullConsumeUrl() => CombineUrl(GetActiveBaseUrl(), consumeTicketPath);

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

        // --- Lógica de Validación ---
        public override bool TryValidate(
            string token,
            out string accountId,
            out string displayName,
            out string failureReason
        )
        {
            accountId = string.Empty;
            displayName = string.Empty;
            failureReason = string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                failureReason = "Join ticket vacío.";
                return false;
            }

            try
            {
                ConsumeTicketResponse response = ConsumeTicket(token.Trim());

                if (response == null || !response.ok)
                {
                    failureReason = response?.error ?? "El master server rechazó el ticket.";
                    return false;
                }

                if (response.player == null || string.IsNullOrWhiteSpace(response.player.uid))
                {
                    failureReason = "El master server no devolvió un UID válido.";
                    return false;
                }

                accountId = response.player.uid.Trim();
                displayName = accountId;
                return true;
            }
            catch (Exception ex)
            {
                failureReason = $"Error validando ticket: {ex.Message}";
                return false;
            }
        }

        private ConsumeTicketResponse ConsumeTicket(string ticketId)
        {
            string url = GetFullConsumeUrl();
            ConsumeTicketRequest payload = new ConsumeTicketRequest
            {
                ticketId = ticketId,
                serverId = serverId,
            };
            string json = JsonUtility.ToJson(payload);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-api-key", GetActiveApiKey());
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = Http.SendAsync(request).GetAwaiter().GetResult();
            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

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

        private static string CombineUrl(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return path ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                return baseUrl;
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }
    }
}
