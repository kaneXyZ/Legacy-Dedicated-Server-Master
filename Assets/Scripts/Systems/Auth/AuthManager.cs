// Archivo: Assets/Scripts/Systems/Auth/AuthManager.cs
using System.Threading.Tasks;
using Legacy.DedicatedServer.Auth; // Referencia al namespace del validador
using Legacy.DedicatedServer.Services; // Para los logs que hicimos antes
using UnityEngine;

namespace Legacy.DedicatedServer.Auth
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [Header("Configuración de Autenticación")]
        [Tooltip("Arrastra aquí el ScriptableObject de MasterTicketAuthValidator")]
        [SerializeField]
        private MasterTicketAuthValidator authValidator;

        private void Awake()
        {
            Instance = this;
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
                    $"[Éxito] Jugador  (UID: {result.AccountId}) ha verificado su ticket."
                );

                // Éxito: Notificar a través del Network Handler
                AuthNetworkHandler.SendAuthResult(clientId, true, "Acceso concedido.");

                // Inicializar al jugador y asociar su cuenta de base de datos con su cliente de Riptide
                //ServerPlayerManager.Instance.SpawnPlayerInWorld(clientId, playerName);
                ServerServices.Logger.LogError(
                    LogCategory.Auth,
                    "¡Función de spawn de jugador aún no implementada! El cliente quedará autenticado pero sin personaje en el mundo."
                );
            }
            else
            {
                ServerServices.Logger.LogWarning(
                    LogCategory.Auth,
                    $"[Rechazado] Cliente {clientId} ) intentó entrar. Razón: {result.ErrorReason}"
                );

                // Fallo: Notificar y desconectar
                AuthNetworkHandler.SendAuthResult(clientId, false, result.ErrorReason);
                ServerServices.Network.Server.DisconnectClient(clientId);
            }
        }
    }
}
