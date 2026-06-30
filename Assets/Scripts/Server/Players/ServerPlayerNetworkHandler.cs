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
            Vector2 inputAxis = message.GetVector2();
            Vector3 camForward = message.GetVector3();
            bool jumpPressed = message.GetBool(); // <-- El cliente debe añadir esto a su mensaje

            if (ServerPlayerManager.Instance != null)
            {
                if (
                    ServerPlayerManager.Instance != null
                    && ServerPlayerManager.Instance.players.TryGetValue(
                        fromClientId,
                        out ServerPlayer player
                    )
                )
                {
                    player.MovementModule.SetInputs(inputAxis, camForward, jumpPressed);
                }
            }
        }

        [MessageHandler((ushort)ClientToServerId.EquipWeapon)]
        private static void HandleEquipWeapon(ushort fromClientId, Message message)
        {
            int slotToEquip = message.GetInt(); // 0 = Primaria, 1 = Secundaria, -1 = Guardar arma

            if (
                ServerPlayerManager.Instance.players.TryGetValue(
                    fromClientId,
                    out ServerPlayer player
                )
            )
            {
                player.EquipmentModule.SetActiveWeaponSlot(slotToEquip);
            }
        }

        [MessageHandler((ushort)ClientToServerId.ReloadWeapon)] // Asegúrate de agregarlo a tu Enum
        private static void HandleReloadWeapon(ushort fromClientId, Message message)
        {
            if (
                ServerPlayerManager.Instance.players.TryGetValue(
                    fromClientId,
                    out ServerPlayer player
                )
            )
            {
                player.EquipmentModule.ReloadWeapon();
            }
        }

        [MessageHandler((ushort)ClientToServerId.FireWeapon)]
        private static void HandleFireWeapon(ushort fromClientId, Message message)
        {
            Vector3 origin = message.GetVector3();
            Vector3 direction = message.GetVector3();

            if (
                ServerPlayerManager.Instance.players.TryGetValue(
                    fromClientId,
                    out ServerPlayer player
                )
            )
            {
                player.EquipmentModule.FireWeapon(origin, direction);
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
