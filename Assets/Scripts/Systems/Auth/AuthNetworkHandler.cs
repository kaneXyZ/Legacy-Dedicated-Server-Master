// Archivo: Assets/Scripts/Systems/Auth/AuthNetworkHandler.cs
using Legacy.DedicatedServer.Players;
using Legacy.DedicatedServer.Services;
using Legacy.Shared.Core;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Auth
{
    public static class AuthNetworkHandler
    {
        [MessageHandler((ushort)ClientToServerId.Authenticate)]
        public static void HandleAuthenticate(ushort fromClientId, Message message)
        {
            string ticketId = message.GetString();
            //string playerName = message.GetString();
            // Delegar de inmediato al administrador de lógica de negocio (asíncrono)
            if (ServerServices.Auth != null)
            {
                ServerServices.Auth.ProcessAuthentication(fromClientId, ticketId);
            }
            else
            {
                ServerServices.Logger.LogError(
                    LogCategory.Auth,
                    $"Se recibió un ticket del cliente {fromClientId}, pero el servicio de Autenticación no está registrado."
                );
            }
        }

        [MessageHandler((ushort)ClientToServerId.ClientReady)]
        private static void HandleClientReady(ushort fromClientId, Message message)
        {
            // El cliente nos avisa que ya cargó la escena. Buscamos su AccountId en la lista de espera.
            if (AuthManager.ValidatedClients.TryGetValue(fromClientId, out string accountId))
            {
                Debug.Log(
                    $"[ServerPlayerManager] El cliente {fromClientId} cargó la escena. Instanciando su jugador..."
                );

                // AHORA SÍ le hacemos spawn
                //Instance.SpawnPlayer(fromClientId, accountId);
                if (ServerPlayerManager.Instance != null)
                {
                    // Nota: Asegúrate de que el nombre del método coincida con el de tu ServerPlayerManager
                    // En los ejemplos anteriores lo llamamos SpawnPlayer(clientId, username)
                    ServerPlayerManager.Instance.SpawnPlayerInWorld(fromClientId, accountId);
                }
                else
                {
                    ServerServices.Logger.LogError(
                        LogCategory.Player,
                        $"No se pudo hacer spawn del jugador {accountId} porque ServerPlayerManager es nulo."
                    );
                }

                // Lo sacamos de la lista de espera
                AuthManager.ValidatedClients.Remove(fromClientId);
            }
        }

        public static void SendAuthResult(ushort toClientId, bool success, string messageContent)
        {
            Message msg = Message.Create(
                MessageSendMode.Reliable,
                (ushort)ServerToClientId.AuthResult
            );
            msg.AddBool(success);
            msg.AddString(messageContent);
            // Envío seguro a través del Hub de Servicios global
            if (ServerServices.Network != null && ServerServices.Network.Server != null)
            {
                ServerServices.Network.Server.Send(msg, toClientId);

                string status = success ? "Aceptado" : "Rechazado";
                ServerServices.Logger.LogInfo(
                    LogCategory.Auth,
                    $"Resultado de autenticación enviado al Cliente [{toClientId}]: {status}"
                );
            }
            else
            {
                ServerServices.Logger.LogError(
                    LogCategory.Network,
                    "Intento de enviar paquete AuthResult, pero el servidor de red no está inicializado."
                );
            }
        }
    }
}
