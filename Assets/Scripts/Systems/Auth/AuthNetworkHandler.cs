// Archivo: Assets/Scripts/Systems/Auth/AuthNetworkHandler.cs
using Legacy.DedicatedServer.Services;
using Legacy.Shared;
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
            AuthManager.Instance.ProcessAuthentication(fromClientId, ticketId);
        }

        public static void SendAuthResult(ushort toClientId, bool success, string messageContent)
        {
            Message msg = Message.Create(MessageSendMode.Reliable, ServerToClientId.AuthResult);
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
