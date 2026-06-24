// Archivo: Assets/Scripts/Server/Networking/Handlers/ServerPlayerNetworkHandler.cs
using Legacy.DedicatedServer.Players;
using Legacy.DedicatedServer.Services; // Para acceder a nuestro Hub de red y el Tick
using Legacy.Shared.Core;
using Riptide;
using UnityEngine;

namespace Legacy.DedicatedServer.Networking // O tu namespace correspondiente
{
    public class ServerPlayerNetworkHandler : MonoBehaviour
    {
        [MessageHandler((ushort)ClientToServerId.PlayerInput)]
        private static void HandlePlayerInput(ushort fromClientId, Message message)
        {
            // 1. Leer los datos de movimiento mandados por el cliente (intenciones)
            Vector2 inputAxis = message.GetVector2();
            Vector3 camForward = message.GetVector3();

            // 2. Verificar que el Manager exista
            if (ServerPlayerManager.Instance != null)
            {
                // 3. Obtener el componente físico del jugador en el servidor
                if (
                    ServerPlayerManager.Instance.players.TryGetValue(
                        fromClientId,
                        out ServerPlayer player
                    )
                )
                {
                    // Inyectamos la intención de movimiento; las físicas y colisiones correrán por cuenta de ServerPlayer (FixedUpdate)
                    player.SetInputs(inputAxis, camForward);
                }
            }
        }

        /// <summary>
        /// Método para hacer broadcast de la posición oficial calculada por el servidor.
        /// </summary>
        public static void BroadcastPlayerMovement(
            ushort movedClientId,
            Vector3 pos,
            Quaternion rot
        )
        {
            if (ServerServices.Network == null || !ServerServices.Network.IsRunning)
                return;

            Message message = Message.Create(
                MessageSendMode.Unreliable,
                (ushort)ServerToClientId.PlayerMovement
            );

            // Adjuntamos el Tick actual del servidor
            message.AddUShort(ServerServices.Network.CurrentTick);

            message.AddUShort(movedClientId);
            message.AddVector3(pos);
            message.AddQuaternion(rot);

            // Excluimos al cliente que ejecutó la acción (Predicción del lado del cliente)
            ServerServices.Network.Server.SendToAll(message);
        }

        public static void SendStatsToClient(ushort toClientId, float health, float food)
        {
            Message message = Message.Create(
                MessageSendMode.Reliable,
                (ushort)ServerToClientId.PlayerStats
            );

            // Estructura del mensaje: ID del jugador, Vida, Comida
            message.AddUShort(toClientId);
            message.AddFloat(health);
            message.AddFloat(food);

            ServerServices.Network.Server.Send(message, toClientId); // Enviar al cliente objetivo
        }
    }
}
