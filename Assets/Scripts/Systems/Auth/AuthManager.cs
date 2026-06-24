// Archivo: Assets/Scripts/Systems/Auth/AuthManager.cs
using System.Threading.Tasks;
using Legacy.DedicatedServer.Players; // Referencia al namespace del validador y manager
using Legacy.DedicatedServer.Services; // Para los logs y el Hub global
using UnityEngine;

namespace Legacy.DedicatedServer.Auth
{
    public class AuthManager : MonoBehaviour
    {
        [Header("Configuración de Autenticación")]
        [Tooltip("Arrastra aquí el ScriptableObject de MasterTicketAuthValidator")]
        [SerializeField]
        private MasterTicketAuthValidator authValidator;
        public static System.Collections.Generic.Dictionary<ushort, string> ValidatedClients =
            new System.Collections.Generic.Dictionary<ushort, string>();

        private void Awake()
        {
            // Arquitectura Limpia: Registramos este manager en el Hub Global de Servicios
            // en lugar de usar un Singleton estático.
            ServerServices.RegisterAuth(this);
        }

        public async void ProcessAuthentication(ushort clientId, string ticketId)
        {
            if (authValidator == null)
            {
                ServerServices.Logger.LogError(
                    LogCategory.Auth,
                    "Falta el AuthValidator en el AuthManager. Cerrando conexión."
                );
                AuthNetworkHandler.SendAuthResult(clientId, false, "Error interno del servidor.");
                ServerServices.Network.Server.DisconnectClient(clientId);
                return;
            }

            // Llamada asíncrona real. El servidor no se congela aquí.
            AuthValidationResult result = await authValidator.ValidateTicketAsync(ticketId);
            if (result.IsValid)
            {
                ServerServices.Logger.LogInfo(
                    LogCategory.Auth,
                    $"[Éxito] Jugador (UID: {result.AccountId}) ha verificado su ticket."
                );

                // 1. Éxito: Notificar al cliente que puede cargar el mundo
                AuthNetworkHandler.SendAuthResult(clientId, true, "Acceso concedido.");

                // 2. ¡EL RELOJ! Le enviamos el Tick inicial de sincronización al cliente validado
                if (ServerServices.Network != null)
                {
                    ServerServices.Network.SendInitialSync(clientId);
                }

                ValidatedClients[clientId] = result.AccountId;

                // 3. Inicializar al jugador y asociar su cuenta con Riptide
                /*if (ServerPlayerManager.Instance != null)
                {
                    // Nota: Asegúrate de que el nombre del método coincida con el de tu ServerPlayerManager
                    // En los ejemplos anteriores lo llamamos SpawnPlayer(clientId, username)
                    ServerPlayerManager.Instance.SpawnPlayerInWorld(clientId, result.AccountId);
                }
                else
                {
                    ServerServices.Logger.LogError(
                        LogCategory.Player,
                        $"No se pudo hacer spawn del jugador {result.AccountId} porque ServerPlayerManager es nulo."
                    );
                }*/
            }
            else
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.Auth,
                    $"[Rechazado] Cliente {clientId} intentó entrar. Razón: {result.ErrorReason}"
                );

                // Fallo: Notificar la razón del rechazo y desconectar
                AuthNetworkHandler.SendAuthResult(clientId, false, result.ErrorReason);
                ServerServices.Network.Server.DisconnectClient(clientId);
            }
        }
    }
}
